//-----------------------------------------------------------------------------
// Copyright (c) 2017-2023 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;

namespace Sixty502DotNet.Shared;

/// <summary>
/// A class responsible for preprocessing and tokenizing 6502.Net source code,
/// including expanding macros and inserting included files, the final product
/// of which is a list of <see cref="IToken"/> objects that can later be used
/// as a token stream for regular parsing.
/// </summary>
public sealed partial class Preprocessor : SyntaxParserBaseVisitor<IList<IToken>>
{
    private readonly TokenFactory _tokenFactory;
    private readonly Stack<CommonTokenStream> _tokenStreams;
    private readonly ICharStreamFactory _sourceFactory;
    private readonly IDictionary<string, int> _initialInstructionSet;
    private readonly Dictionary<string, Macro> _macroDefinitions;
    private readonly bool _caseSensitive;
    private BaseErrorListener? _errorListener;

    /// <summary>
    /// Construct a new instance of the <see cref="Preprocessor"/> class.
    /// </summary>
    /// <param name="sourceStreamFactory">The source factory responsible
    /// for outputting the appropriate <see cref="ICharStream"/> from the
    /// specified parameter.</param>
    /// <param name="initialInstructionSet">The initial instruction set (subject
    /// to change by the <c>.cpu</c> directive) the lexer uses when recognizing
    /// reserved words.</param>
    /// <param name="caseSensitive">Sets whether tokenization should treat
    /// input as case-sensitive when recognizing keywords.</param>
    /// </param>
	public Preprocessor(ICharStreamFactory sourceStreamFactory,
                        IDictionary<string, int> initialInstructionSet,
                        bool caseSensitive)
    {
        _sourceFactory = sourceStreamFactory;
        _initialInstructionSet = new Dictionary<string, int>(initialInstructionSet, caseSensitive.ToStringComparer());
        _macroDefinitions = new(caseSensitive.ToStringComparer());
        _tokenFactory = new();
        _errorListener = null;
        _tokenStreams = new();
        _caseSensitive = caseSensitive;
    }

    private static IList<IToken> EndProcessing(SyntaxParser.PreprocStatContext context)
    {
        if (context.label() != null)
        {
            return new List<IToken>
            {
                context.label().Start,
                context.d.ToNew(SyntaxLexer.Eof, "<EOF>")
            };
        }
        return new List<IToken> { context.d.ToNew(SyntaxLexer.Eof, "<EOF>") };
    }

    private IList<IToken> IncludeFile(SyntaxParser.PreprocStatContext context)
    {
        IToken strToken = context.filename;
        int dquoteIndex = strToken.Text.IndexOf('"');
        string fileName = strToken.Text[dquoteIndex..].Trim('"');
        int includeLine = strToken.Line;
        if (_tokenFactory.Inclusions.Any(i => i.Item1.Equals(fileName, StringComparison.OrdinalIgnoreCase)))
        {
            throw new Error(context.filename, $"Recursive inclusion of file '{fileName}'");
        }
        _tokenFactory.Inclusions.Push((fileName, includeLine));
        List<IToken> include = new();
        if (context.d.Type == SyntaxParser.Binclude)
        {
            if (context.label() != null)
            {
                include.Add(context.label().Start);
            }
            include.Add(context.d.ToNew(SyntaxParser.Block, ".block"));
            include.Add(context.d.ToNew(SyntaxParser.NL, "\n"));
        }
        try
        {
            include.AddRange(Preprocess(fileName, false, _errorListener));
            if (include[^1].Type == SyntaxLexer.Eof)
            {
                include.RemoveAt(include.Count - 1);
            }
        }
        catch (IOException)
        {
            throw new Error(context.d, "Cannot include source");
        }
        if (context.d.Type == SyntaxParser.Binclude)
        {
            include.Add(strToken.ToNew(SyntaxParser.NL, "\n"));
            include.Add(strToken.ToNew(SyntaxParser.Endblock, ".endblock"));
        }
        include.Add(strToken.ToNew(SyntaxParser.NL, "\n"));
        _tokenFactory.Inclusions.Pop();
        return include;
    }

    private IList<IToken> DefineMacro(SyntaxParser.PreprocStatContext context)
    {
        string name = context.Start.Text;
        if (string.IsNullOrEmpty(name))
        {
            throw new Error(context.Start, "Macro name not valid");
        }
        if (!_macroDefinitions.TryAdd($".{name}", new Macro(context, CurrentStream)))
        {
            throw new Error(context.d, "Redefinition of macro not permitted");
        }
        return new List<IToken>();
    }

    private IToken Copied(IToken original)
    {
        return _tokenFactory.Create(new Tuple<ITokenSource, ICharStream>(original.TokenSource, original.InputStream),
                    original.Type,
                    original.Text,
                    original.Channel,
                    original.StartIndex,
                    original.StopIndex,
                    original.Line,
                    original.Column);
    }

    private IList<IToken> Copied(IList<IToken> originals, int SubstitutionStartIndexPosition, int substitutionStopIndexPosition)
    {
        List<IToken> copies = new();
        for (int i = 0; i < originals.Count; i++)
        {
            Token copied = (Token)Copied(originals[i]);
            copied.OriginalTokenIndex = originals[i].TokenIndex;
            if (i == 0)
            {
                copied.SubstitutionStartIndex = SubstitutionStartIndexPosition;
            }
            if (i == originals.Count - 1)
            {
                copied.SubstitutionStopIndex = substitutionStopIndexPosition;
            }
            copies.Add(copied);
        }
        return copies;
    }

    private IList<IToken> ExpandMacro(SyntaxParser.PreprocStatContext context)
    {
        IToken macroDef = context.DotIdentifier().Symbol;
        IList<IToken> unparsedParams;
        IList<IList<IToken>> parsedParams;
        if (context.macroParam() != null)
        {
            int paramStart = context.macroParam().Start.TokenIndex;
            int paramEnd = context.macroParam().Stop.TokenIndex;
            unparsedParams = CurrentStream.GetTokens(paramStart, paramEnd);
            if (unparsedParams[0].Type == SyntaxParser.Colon)
            {
                // for .KEY: value in dictionaries
                unparsedParams.Insert(0, macroDef);
                return unparsedParams;
            }
            parsedParams = ParserBase.ParseMacroParams(unparsedParams);
        }
        else
        {
            unparsedParams = new List<IToken>();
            parsedParams = new List<IList<IToken>>();
        }
        bool[] paramUsed = new bool[parsedParams.Count];
        string name = macroDef.Text;
        if (!_macroDefinitions.TryGetValue(name, out Macro? macro))
        {
            // this should never happen but if so just let the parser deal with it later
            return CurrentStream.GetTokens(context.Start.TokenIndex, context.Stop.TokenIndex);
        }
        List<IToken> expanded = new();
        if (!macro.Valid)
        {
            return expanded;
        }
        _tokenFactory.MacroName = name;
        _tokenFactory.MacroLine = context.Start.Line;
        bool expandedAllPassedParams = false;
        for (int i = 0; i < macro.Definition.Count; i++)
        {
            //_tokenFactory.MacroLine = macro.Definition[i].Line;
            if (macro.Definition[i].Type == SyntaxParser.MacroSub)
            {
                _tokenFactory.MacroCharStream = macro.Definition[i].InputStream;
                int substitutionStartIndex = macro.Definition[i].StartIndex;
                int substitutionStopIndex = macro.Definition[i].StopIndex;
                Macro.SubstitutionPoint point = macro.SubstitutionPoints[i];
                if (macro.Definition[i].Text.Equals("\\*"))
                {
                    expandedAllPassedParams = true;
                    expanded.AddRange(unparsedParams);
                }
                else if (point.argumentIndex < parsedParams.Count)
                {
                    expanded.AddRange(Copied(parsedParams[point.argumentIndex], substitutionStartIndex, substitutionStopIndex));
                    paramUsed[point.argumentIndex] = true;
                }
                else
                {
                    int adjusted = point.argumentIndex - macro.Arguments.Count;
                    if (adjusted < 0)
                    {
                        throw new Error(context.DotIdentifier().Symbol, "Too few parameters provided");
                    }
                    IList<IToken> optionals = macro.OptionalArguments[adjusted].Skip(2).ToList();
                    expanded.AddRange(Copied(optionals, substitutionStartIndex, substitutionStopIndex));
                }
                continue;
            }
            expanded.Add(Copied(macro.Definition[i]));
        }
        int unusedIndex = paramUsed.ToList().FindIndex(p => !p);
        if (unusedIndex >= 0 && !expandedAllPassedParams)
        {
            throw new Error(parsedParams[unusedIndex][0], "Unexpected parameter provided");
        }
        _tokenStreams.Push(new CommonTokenStream(new ListTokenSource(expanded)));
        CurrentStream.Fill();
        try
        {
            SyntaxParser parser = new(CurrentStream);
            parser.Interpreter.PredictionMode = PredictionMode.SLL;
            List<IToken> processedExpanded = new();
            if (context.label() != null)
            {
                processedExpanded.Add(Copied(context.label().Start));
            }
            processedExpanded.Add(macroDef.ToNew(SyntaxParser.Block, ".block"));
            processedExpanded.Add(macroDef.ToNew(SyntaxParser.NL, "\n"));
            IList<IToken> processedMacro = Visit(parser.preprocess());
            if (processedMacro[^1].Type == SyntaxLexer.Eof)
            {
                processedMacro.RemoveAt(processedMacro.Count - 1);
            }
            processedExpanded.AddRange(processedMacro);
            processedExpanded.Add(macroDef.ToNew(SyntaxParser.Endblock, ".endblock"));
            processedExpanded.Add(macroDef.ToNew(SyntaxParser.NL, "\n"));
            _tokenFactory.MacroName = null;
            _tokenFactory.MacroLine = 0;
            macro.IsInvoked = true;
            return processedExpanded;
        }
        finally
        {
            _tokenStreams.Pop();
        }
    }

    public override IList<IToken> VisitPreprocStat([NotNull] SyntaxParser.PreprocStatContext context)
    {
        return context.d.Type switch
        {
            SyntaxParser.Binclude or
            SyntaxParser.Include => IncludeFile(context),
            SyntaxParser.End => EndProcessing(context),
            SyntaxParser.Macro => DefineMacro(context),
            SyntaxParser.DotIdentifier => ExpandMacro(context),
            _ => VisitChildren(context)
        };
    }

    private IList<IToken> GetContextTokens(ParserRuleContext context)
    {
        Interval tokenInterval = context.SourceInterval;
        if (tokenInterval.a > tokenInterval.b)
        {
            return new List<IToken> { context.Start };
        }
        return CurrentStream.GetTokens(tokenInterval.a, tokenInterval.b);
    }

    public override IList<IToken> VisitCpuStat([NotNull] SyntaxParser.CpuStatContext context)
    {
        return GetContextTokens(context);
    }

    public override IList<IToken> VisitNonPreProc([NotNull] SyntaxParser.NonPreProcContext context)
    {
        return GetContextTokens(context);
    }

    public override IList<IToken> VisitPreprocEos([NotNull] SyntaxParser.PreprocEosContext context)
    {
        return GetContextTokens(context);
    }

    public override IList<IToken> VisitPreprocess([NotNull] SyntaxParser.PreprocessContext context)
    {
        List<IToken> preprocessed = new();
        for (int i = 0; i < context.ChildCount; i++)
        {
            if (context.GetChild(i) is ParserRuleContext childContext)
            {
                preprocessed.AddRange(Visit(childContext));
            }
            else if (context.GetChild(i) is ITerminalNode terminal)
            {
                if (terminal.Symbol.Type == TokenConstants.EOF)
                {
                    break;
                }
                preprocessed.Add(terminal.Symbol);
            }
        }
        return preprocessed;
    }

    /// <summary>
    /// Detect if a <c>.cpu</c> directive is a top level directive in the
    /// specified source.
    /// </summary>
    /// <param name="source">The source, either code itself or a path.</param>
    /// <param name="sourceFactory">The factory responsible for building a
    /// <see cref="ICharStream"/> for parsing.</param>
    /// <param name="caseSensitive">Get whether parsing the <c>.cpu</c>
    /// directive should be case-sensitive.</param>
    /// <returns>The cpuid operand of the <c>.cpu</c> directive if this is a
    /// top-level directive, otherwise <c>null</c>.</returns>
    public static string? DetectCpuidFromSource(string source, ICharStreamFactory sourceFactory, bool caseSensitive)
    {
        SyntaxLexer headerLexer = new(sourceFactory.GetStream(source))
        {
            ReservedWords = new Dictionary<string, int>(caseSensitive.ToStringComparer())
            {
                { ".cpu", SyntaxLexer.Cpu }
            }
        };
        SyntaxParser headerParser = new(new CommonTokenStream(headerLexer));
        headerParser.RemoveErrorListeners();
        headerParser.ErrorHandler = new BailErrorStrategy();
        try
        {
            SyntaxParser.CpuStatContext context = headerParser.cpuStat();
            return context.StringLiteral().GetText().TrimOnce('"');
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Preprocess source code, adding all included source files and defining and expanding
    /// all macros.
    /// </summary>
    /// <param name="source">The source code.</param>
    /// <param name="appendNewLine">Append a newline token to the preprocessed token list.</param>
    /// <param name="errorListener">The <see cref="BaseErrorListener"/> that will capture
    /// all syntax errors encountered during parsing.</param>
    /// <returns>A list of <see cref="IToken"/> objects representing the scanned
    /// valid source.</returns>
    public IList<IToken> Preprocess(string source, bool appendNewLine, BaseErrorListener? errorListener)
    {
        _errorListener = errorListener;
        try
        {
            SyntaxLexer lexer = new(_sourceFactory.GetStream(source))
            {
                TokenFactory = _tokenFactory,
                IsCaseSensitive = _caseSensitive,
                ReservedWords = new Dictionary<string, int>(_initialInstructionSet, _caseSensitive.ToStringComparer())
            };
            _tokenStreams.Push(new(lexer));
            CurrentStream.Fill();
        }
        catch
        {
            throw new IOException($"Fatal error: Could not read from '{source}'");
        }
        SyntaxParser parser = new(CurrentStream);
        if (_errorListener != null)
        {
            parser.RemoveErrorListeners();
            parser.AddErrorListener(_errorListener);
        }
        try
        {
            parser.Interpreter.PredictionMode = PredictionMode.SLL;
            IList<IToken> preprocessed = VisitPreprocess(parser.preprocess());
            if (appendNewLine)
            {
                preprocessed.Add(preprocessed[^1].ToNew(SyntaxParser.NL, "\n"));
            }
            return preprocessed;
        }
        catch (Error err)
        {
            if (err.Token == null)
            {
                throw err;
            }
            _errorListener?.SyntaxError(TextWriter.Null, parser, err.Token, err.Token.Line, err.Token.Column, err.Message, new CustomParseError());
            return new List<IToken>();
        }
        finally
        {
            _tokenStreams.Pop();
        }
    }

    /// <summary>
    /// Get the list of uninvoked macros.
    /// </summary>
    public IList<KeyValuePair<string, Macro>> UninvokedMacros =>
        _macroDefinitions.Where(m => !m.Value.IsInvoked).ToList();

    /// <summary>
    /// Get the preprocessor's current stream.
    /// </summary>
    public CommonTokenStream CurrentStream => _tokenStreams.Peek();
}
