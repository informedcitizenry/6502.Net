// Copyright (c) 2026 informedcitizenry
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using Sixty502DotNet.Shared.Arch;
using Sixty502DotNet.Shared.Error;
using Sixty502DotNet.Shared.Eval;
using Sixty502DotNet.Shared.Lex;
using Sixty502DotNet.Shared.Parse.Ast;
using System.Diagnostics;

namespace Sixty502DotNet.Shared.Parse;

public sealed partial class Parser
{
    private readonly struct ParseState
    (
        Token current, 
        Token previous, 
        Token previous2, 
        Stack<TokenType> recoveryDelimiters
    )
    {
        public Token Current { get; } = current;

        public Token Previous { get; } = previous;

        public Token Previous2 { get; } = previous2;

        public Stack<TokenType> RecoveryDelimiters { get; } = new(recoveryDelimiters);
    }
    
    private readonly Lexer _lexer;

    private Token _current;

    private Token _previous;

    private Token _previous2;

    private Stack<TokenType> _recoveryDelimiters;
    
    private readonly Stack<ParseState> _states;
    
    private readonly ISourceFactory _sourceFactory;

    public Parser
    (
        string path,
        string sourceCode,
        LexerBehavior behavior
    ) 
    : this(new StringSourceReaderFactory(string.Empty), path, sourceCode, Cpu.M6502, false, false, behavior, [])
    {
        
    }
    
    public Parser
    (
        ISourceFactory sourceFactory, 
        string path,
        string? sourceCode, 
        Cpu cpu, 
        bool bra6502,
        bool pseudoBra6502,
        LexerBehavior behavior
    )
    : this(sourceFactory, path, sourceCode, cpu, bra6502, pseudoBra6502, behavior, [])
    {
        
    }
    
    public Parser
    (
        ISourceFactory sourceFactory, 
        string path, 
        string? sourceCode, 
        Cpu cpu, 
        bool bra6502,
        bool pseudoBra6502,
        LexerBehavior behavior, 
        List<Inclusion> inclusions
    )
    {
        _sourceFactory = sourceFactory;
        _recoveryDelimiters = new Stack<TokenType>();
        string sourceText;
        if (!string.IsNullOrEmpty(sourceCode))
        {
            sourceText = sourceCode;
        }
        else
        {
            var sourceReader = sourceFactory.CreateReader();
            sourceText = sourceReader.GetSource(path) ?? throw new FileNotFoundException("Source not found");
        }
        _lexer = new Lexer
        (
            new Source(path, sourceText), 
            cpu, 
            bra6502, 
            pseudoBra6502, 
            behavior, 
            inclusions
        );
        _parseLegacyDelimiters =
            behavior is LexerBehavior.LegacyDelimitersCaseInsensitive or
                LexerBehavior.LegacyDelimitersCaseSensitive;
        _states = new Stack<ParseState>();
        _statementIndex = 0;
        _current = new Token();
        var comparer = behavior is LexerBehavior.LegacyDelimitersCaseInsensitive or LexerBehavior.DefaultCaseInsensitive
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;
        Macros = new Dictionary<string, Macro>(comparer);
        Advance();
    }

    public void ChangeSource(string path)
    {
        var reader = _sourceFactory.CreateReader();
        var sourceText = reader.GetSource(path);
        if (sourceText == null) throw new FileNotFoundException("Source not found");
        _lexer.SetSource(new Source(path, sourceText));
        Advance();
    }
    
    public static Define ParseDefine(string defineSource)
    {
        var parser = new Parser
        (
            new StringSourceReaderFactory(defineSource),
            "<unnamed>",
            defineSource,
            Cpu.M6502, 
            false,
            false,
            LexerBehavior.DefaultCaseInsensitive
        );
        var label = parser._current;
        parser.ConsumeIdent();
        return parser.Match(TokenType.Eq) 
            ? new Define(label, parser.Expression()) 
            : new Define(label, null);
    }

    public static bool TryParseTopLevelCpuStatement
    (
        ISourceFactory sourceFactory, 
        string path, 
        LexerBehavior behavior, 
        out SingleExpressionDirectiveStatement? statement
    )
    {
        statement = null;
        Parser parser;
        try
        {
            parser = new Parser(sourceFactory, path, null, Cpu.M6502, false, false, behavior);
        }
        catch
        {
            return false;
        }
        parser.DiscardNewlines();
        // capture preceding label if present
        if (parser._current.Type.IsIdent() || parser._current.Type is TokenType.Plus or TokenType.Minus)
        {
            parser.Advance();
            if (!parser.Match(TokenType.Colon) && 
                !parser.Match(TokenType.Newline) && 
                !parser.Check(TokenType.CpuKw))
            {
                return false;
            }
        }
        var cpuDirective = parser._current;
        if (!parser.Match(TokenType.CpuKw) || !parser.Match(TokenType.StringLiteral) || !parser.CheckEosOrColon())
        {
            return false;
        }
        // we don't need to do a full parse, just enough to know we have a 
        // valid `.cpu` "<cpu name>" statement
        statement = new SingleExpressionDirectiveStatement
        (
            cpuDirective, 
            new PrimaryExpression(parser._previous)
        );
        return true;
    }
    
    public BlockStatement ParseModule(ErrorLogger? logger = null)
    {
        var blockStatement = new BlockStatement
        {
            StatementIndex = _statementIndex
        };
        DiscardNewlines();
        while (!Check(TokenType.Eof) && !Ended)
        {
            try
            {
                var assertCount = _states.Count;
                blockStatement.Statements.Add(Statement(logger)); 
                Debug.Assert(_states.Count == assertCount, 
                    "One or more calls to Mark() in parser did not have a matching Release()/Restore() call");
            }
            catch (Exception ex)
            {
                if (logger == null)
                {
                    throw;
                }
                switch (ex)
                {
                    case UnresolvedDeclException parseBlockEx:
                        logger.LogError(parseBlockEx);
                        break;
                    case ParserException parseEx:
                        logger.LogError(parseEx);
                        break;
                    case InvalidUnaryOperationException unaryEx:
                        logger.LogError(unaryEx);
                        break;
                    case InvalidBinaryOperationException binaryEx:
                        logger.LogError(binaryEx);
                        break;
                    case TypeException typeEx:
                        logger.LogError(typeEx);
                        break;
                    case CompileException compEx:
                        logger.LogError(compEx);
                        break;
                    case LexerException lexEx:
                        logger.LogError(lexEx.Type, new PrimaryExpression(lexEx.Token));
                        break;
                    default:
                        logger.LogError(ex.Message, new PrimaryExpression(_current));
                        break;
                }
                Recover(logger);
            }
        }
        blockStatement.LeftToken = blockStatement.Statements.Count > 0 
            ? blockStatement.Statements[0].LeftToken
            : _previous;
        blockStatement.RightToken = blockStatement.Statements.Count > 0
            ? blockStatement.Statements[^1].RightToken
            : _previous;
        return blockStatement;
    }
    
    public Expression Expression() => ParsePrecedence(this, Precedence.None);

    public Statement Statement(ErrorLogger? logger = null)
    {
        _statementIndex++;
        if (_current.Type.IsIdent())
        {
            return IdentFirst(logger);
        }
        return _current.Type switch
        {
            TokenType.Eof => new EofStatement(),
            TokenType.Star or
            TokenType.OpenParen => VarAssignment(true),
            TokenType.BincludeKw or 
            TokenType.IncludeKw => Include(null, logger),
            TokenType.BlockKw or 
            TokenType.ProcKw => AnonymousBlockStatement(),
            TokenType.CpuKw => CpuDirectiveStatement(),
            TokenType.DoKw or
            TokenType.RepeatKw or
            TokenType.WhileKw => ExpressionBlock(),
            TokenType.EndKw => End(),
            TokenType.ForKw => ForStatement(),
            TokenType.ForeachKw => ForeachStatement(),
            TokenType.EnumKw or
            TokenType.FunctionKw or 
            TokenType.MacroKw => throw new CompileException
            (
                CompileExceptionType.IdentifierExpectedBeforeCommand,
                _current
            ),
            TokenType.IfKw or 
            TokenType.IfdefKw or 
            TokenType.IfndefKw => IfStatement(),
            TokenType.LetKw => LetStatement(),
            TokenType.MacroName => MacroName(null, logger),
            TokenType.NamespaceKw => Namespace(),
            TokenType.PageKw => Page(),
            TokenType.Plus or 
            TokenType.Minus => RefLabelStatement(),
            TokenType.SwitchKw => SwitchStatement(),
            >= TokenType.Adc => CpuInstructionStatement(),
            >= TokenType.AddrKw => PseudoOpStatement(),
            >= TokenType.AlignKw => DirectiveStatement(),
            >= TokenType.ElseKw => throw new CompileException
            (
                CompileExceptionType.DirectiveDoesNotCloseCommand,
                _current
            ),
            >= TokenType.CaseKw => throw new CompileException
            (
                CompileExceptionType.DirectiveNotInSwitch,
                _current
            ),
            _ => throw new CompileException
            (
                CompileExceptionType.CommandRequired, 
                _current
            )
        };
    }
    
    internal Dictionary<string, Macro> Macros { get; }

    public IList<Token> UnexpandedMacros =>
        Macros.Where(kvp => !kvp.Value.Expanded)
            .Select(kvp => kvp.Value.Token).ToList();

    public bool Ended { get; private set; }
    
    public Value Json()
    {
        var rootVal = JsonValue();
        ConsumeEos();
        return rootVal;
    }
    
    private bool CheckEosOrColon() => Check(TokenType.Colon) || CheckEos();

    private bool CheckEos() => Check(TokenType.Newline) || Check(TokenType.Eof);

    private bool AtPreprocessor()
        => _current.Type is >= TokenType.BincludeKw and <= TokenType.MacroKw;
    
    private bool BeginsStatement()
    {
        if (!Check(TokenType.Colon))
        {
            return _current.Type >= TokenType.DoKw;
        }
        var peek = Peek();
        return peek.Type >= TokenType.DoKw && peek.Line == _current.Line;
    }

    private void ConsumeEosOrColon()
    {
        if (!Check(TokenType.Colon))
        {
            ConsumeEos();
        }
        else
        {
            _lexer.LexCommand = true;
            _lexer.SetLogicalNewline(_current.Location.End);
            Advance();
        }
    }

    private void ConsumeEos()
    {
        _lexer.LexCommand = true;
        if (!Match(TokenType.Eof))
            Consume(TokenType.Newline);
        DiscardNewlines();
    }

    private void ConsumeIdent()
    {
        if (!_current.Type.IsIdent())
        {
            Consume(TokenType.Ident);
        }
        Advance();
    }
    
    private void Consume(TokenType type)
    {
        if (!Match(type))
            throw new ParserException(type, _current);
    }
    
    private bool Match(TokenType type)
    {
        if (!Check(type)) return false;
        Advance();
        return true;
    }
    
    private void SetSource(Source source, int line, Inclusion inclusion)
    {
        _lexer.Inclusions.Add(inclusion);
        _lexer.SetSource(source, line);
        Advance();
    }

    private void ReturnToSource(Source source)
    {
        _lexer.RestoreSource(source);
        Restore();
        if (_lexer.Inclusions.Count > 0)
        {
            _lexer.Inclusions.RemoveAt(_lexer.Inclusions.Count - 1);
        }
    }
    
    private Token Peek()
    {
        Mark();
        Advance();
        var peek = _current;
        Restore();
        return peek;
    }

    private void Mark()
    {
        _lexer.Mark();
        _states.Push(new ParseState(_current, _previous, _previous2, _recoveryDelimiters));
    }

    private void Restore()
    {
        _lexer.Restore();
        if (_states.TryPop(out var state))
        {
            _previous2 = state.Previous2;
            _previous = state.Previous;
            _current = state.Current;
            _recoveryDelimiters = new Stack<TokenType>(state.RecoveryDelimiters);
        }
    }

    private void Release()
    {
        _lexer.Release();
        _ = _states.TryPop(out _);
    }
    
    private void DiscardNewlines()
    {
        while (Match(TokenType.Newline))
        {
        }
    }
    
    private bool Check(TokenType type)
        => _current.Type == type;

    
    private void SetRecoverDelimiter(TokenType type)
    {
        _recoveryDelimiters.Push(_parseLegacyDelimiters ? type : TokenType.CloseBrace);
    }

    private void ResetRecoverDelimiter()
    {
        _ = _recoveryDelimiters.TryPop(out _);
    }

    private void Recover(ErrorLogger logger)
    {
        _lexer.StartRecovery();
        while (!CheckEos())
        {
            AdvanceWithLexerException(logger);
        }
        while (_recoveryDelimiters.TryPop(out var delimiter))
        {
            while (!Match(delimiter))
            {
                if (_current.Type == TokenType.Eof)
                {
                    logger.LogError(new ParserException(delimiter, _current));
                    break;
                }
                AdvanceWithLexerException(logger);
            }
        }
        DiscardNewlines();
    }

    private void AdvanceWithLexerException(ErrorLogger logger)
    {
        try
        {
            Advance();
        }
        catch (LexerException ex)
        {
            logger.LogError(ex.Type, new PrimaryExpression(ex.Token));
        }
    }
    
    private void Advance()
    {
        _previous2 = _previous;
        _previous = _current;
        _current = _lexer.NextToken();
        if (_previous.Type == TokenType.Colon)
        {
            // for all syntax cases, a `:` discards subsequent newlines
            DiscardNewlines();
        }
    }
}
