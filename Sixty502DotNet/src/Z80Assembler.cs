using System;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using Antlr4.Runtime.Tree;
using CSharpFunctionalExtensions;

namespace Sixty502DotNet;

public class Z80Assembler
{
    private CodeGenVisitor _codeGenVisitor;
    private readonly AssemblyServices _services = new(Options.FromArgs(new[] { "-c", "z80" }));

    public Result<byte[]> Assemble(string input)
    {
        var parsedSource = ParseSource(input);
        if (_services.Log.HasErrors)
        {
            _services.Log.DumpErrors(_services.Options.NoHighlighting);
            return Result.Failure<byte[]>(_services.Log.ToString());
        }

        var passNeeded = true;
        while (passNeeded && !_services.Log.HasErrors)
        {
            if (_services.State.CurrentPass > 4)
            {
                _services.Log.LogEntrySimple("Too many passes attempted.");
                break;
            }

            DoPass(parsedSource!);
            _services.State.CurrentPass++;
            passNeeded = _services.State.PassNeeded;
        }

        return _services.Output.GetCompilation().ToArray();
    }

    private void DoPass(IParseTree parse)
    {
        var currentPassVar = _services.Symbols.GlobalScope.Resolve("CURRENT_PASS") as Constant;
        currentPassVar!.Value.SetAs(new Value(_services.State.CurrentPass + 1));
        _codeGenVisitor.Reset();
        _services.Output.Reset();
        _services.StatementListings.Clear();
        _services.LabelListing.Clear();
        _services.Symbols.Reset();
        _services.State.PassNeeded = false;
        _ = _codeGenVisitor?.Visit(parse);
    }

    private Sixty502DotNetParser.SourceContext? ParseSource(string input)
    {
        try
        {
            var preprocessor = new Preprocessor(_services, CharStreams.fromString(input));
            var lexer = preprocessor.Lexer;
            if (lexer == null)
            {
                // this could happen if the source input file was not found.
                return null;
            }

            var stream = new CommonTokenStream(lexer);

            _codeGenVisitor = new CodeGenVisitor(_services, lexer.InstructionSet!);

            var tokens = stream.GetTokens();
            var errorTokens = tokens.Where(t => t.Channel == Sixty502DotNetLexer.ERROR).ToList();
            for (var i = 0; i < errorTokens.Count; ++i)
            {
                _services.Log.LogEntry((Token)errorTokens[i], Errors.UnexpectedExpression);
            }

            if (!_services.Log.HasErrors)
            {
                stream.Reset();
                var parser = new Sixty502DotNetParser(stream)
                {
                    Symbols = _services.Symbols
                };
                parser.Interpreter.PredictionMode = PredictionMode.SLL;
                parser.RemoveErrorListeners();
                parser.AddErrorListener(_services.Log);
                var parse = parser.source();
                // parse tree first before constructing code gen visitor to ensure the lexer's
                // instruction set will be initialized to the correct one (this happens during lexical phase).
                _codeGenVisitor = new CodeGenVisitor(_services, lexer.InstructionSet!);
                if (_services.Options.WarnLeft)
                {
                    foreach (var label in parser.LabelsAfterWhitespace)
                    {
                        _services.Log.LogEntry((Token)label, "Whitespace precedes label.", false);
                    }
                }

                return parse;
            }

            return null;
        }
        catch (Exception ex)
        {
            _services.Log.LogEntrySimple(ex.Message);
            return null;
        }
    }
}