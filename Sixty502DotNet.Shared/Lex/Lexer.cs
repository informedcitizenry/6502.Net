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

namespace Sixty502DotNet.Shared.Lex;

public sealed class Lexer
{
    private LexerState _state;
    
    private readonly Stack<LexerState> _marks = new();
    
    public Source Source { get; private set; }

    private readonly bool _bra6502;

    private readonly bool _pseudoBra6502;

    public Lexer(Source source, Cpu cpu, bool bra6502, bool pseudoBra6502, LexerBehavior behavior)
        : this(source, cpu, bra6502, pseudoBra6502, behavior, [])
    {
        
    }
    
    public Lexer(Source source, Cpu cpu, bool bra6502, bool pseudoBra6502, LexerBehavior behavior, List<Inclusion> inclusions)
    {
        _state = new LexerState
        {
            Cpu = cpu
        };
        Source = source;
        Cpu = cpu;
        Behavior = behavior;
        _bra6502 = bra6502;
        _pseudoBra6502 = pseudoBra6502;
        _state.CurrentChar = PeekNth(0);
        _state.InterpolatedStringIsMultiline = false;
        _state.InterpolationMode = false;
        _state.LexCommand = behavior != LexerBehavior.Json;
        Inclusions = new List<Inclusion>(inclusions);
    }

    public void SetSource(Source source, int line = 1)
    {
        var currentCpu = _state.Cpu;
        _state = new LexerState();
        Source = source;
        _state.Line = line;
        _state.Cpu = currentCpu;
        _state.CurrentChar = PeekNth(0);
    }

    public void RestoreSource(Source source) => Source = source;

    public void Mark() => _marks.Push(new LexerState(_state));

    public void Release() => _ = _marks.TryPop(out _);
    
    public void Restore()
    {
        if (_marks.Count > 0)
        {
            _state = _marks.Pop();
        }
    }

    public void StartRecovery()
    {
        _state.InterpolatedStringIsMultiline = false;
        _state.Groups = 0;
        _state.InterpolationMode = false;
        _state.LexCommand = true;
    }

    public void End()
    {
        while (NextChar() != '\0')
        {
            
        }
    }

    public bool LexCommand
    {
        set
        {
            if (Behavior != LexerBehavior.Json)
            {
                _state.LexCommand = value;
            }
        }
    }

    public bool LexPercentAsOperator
    {
        set => _state.LexPercentAsOperator = value;
    }
    
    public Token NextToken()
    {
        SkipWhitespace();
        _state.Cursor.Start = _state.Cursor.End;
        _state.ColumnCursor.Start = _state.ColumnCursor.End;
        NextChar();
        return (_state.PreviousChar) switch
        {
            '\0' => CreateToken(TokenType.Eof),
            '~' => CreateToken(TokenType.Tilde),
            '?' => CreateToken(TokenType.QuestionMark),
            ',' => CreateToken(TokenType.Comma),
            '(' => OpenGroup(TokenType.OpenParen),
            ')' => CloseGroup(TokenType.CloseParen),
            '[' => OpenGroup(TokenType.OpenBracket),
            ']' => CloseGroup(TokenType.CloseBracket),
            '{' => OpenBrace(),
            '}' => ClosedBrace(),
            '#' => CreateToken(TokenType.Pound),
            '$' => Dollar(),
            '.' => Dot(),
            ':' => CreateToken(Match('=') ? TokenType.ColonEq : TokenType.Colon),
            '\u2254' => CreateToken(TokenType.ColonEq),
            '*' => CreateToken(!_state.LexCommand && Match('=') ? TokenType.StarEq : TokenType.Star),
            '/' => CreateToken(Match('=') ? TokenType.SlashEq : TokenType.Slash),
            '%' => Percent(),
            '+' => CreateToken(Match('=') ? TokenType.PlusEq : TokenType.Plus),
            '-' => CreateToken(Match('=') ? TokenType.MinusEq : TokenType.Minus),
            '<' => LeftAngle(),
            '≤' or
            '⩽' or
            '≦' => CreateToken(TokenType.Le),
            '>' => RightAngle(),
            '≥' or
            '⩾' or
            '≧' => CreateToken(TokenType.Ge),
            '=' => Equals(),
            '≡' => CreateToken(TokenType.EqEqEq),
            '≠' => CreateToken(TokenType.BangEq),
            '!' => CharChar('=', TokenType.Bang, TokenType.BangEq, TokenType.BangEqEq),
            '&' => CharChar('&', TokenType.BitwiseAnd, TokenType.AndEq, TokenType.AndAnd),
            '^' => CharChar('^', TokenType.Caret, TokenType.CaretEq, TokenType.CaretCaret),
            '|' => CharChar('|', TokenType.BitwiseOr, TokenType.OrEq, TokenType.OrOr),
            '\n' or '\r' => Newline(),
            '\\' => BackSlash(),
            'A' or 'a' or
            'P' or 'p' or
            'S' or 's' or
            'U' or 'u' => EncodedStringLiteral(),
            var c when c.IsIdentHead() => Ident(),
            '0' => Zero(),
            var c when char.IsDigit(c) => Digit(),
            '"' => StringLiteral(),
            '\'' => CharLiteral(),
            _ => CreateToken(TokenType.Unrecognized)
        };
    }
    
    public TokenType CheckDirectiveType(string text)
    {
        var directives = Behavior switch
        {
            LexerBehavior.LegacyDelimitersCaseInsensitive => Keywords.CaseInsensitiveLegacyDirectives,
            LexerBehavior.LegacyDelimitersCaseSensitive => Keywords.CaseSensitiveLegacyDirectives,
            LexerBehavior.DefaultCaseSensitive => Keywords.CaseSensitiveDirectives,
            _ => Keywords.CaseInsensitiveDirectives
        };
        return directives.GetValueOrDefault(text, TokenType.MacroName);
    }

    public Cpu Cpu
    {
        get => _state.Cpu;
        set => _state.Cpu = value;
    }

    public void SetLogicalNewline(int lineStart) => _state.AdjustedLineStart = lineStart;

    private Token OpenGroup(TokenType type)
    {
        _state.Groups++;
        return CreateToken(type);
    }

    private Token CloseGroup(TokenType type)
    {
        if (_state.Groups > 0)
        {
            _state.Groups--;
        }
        return CreateToken(type);
    }
    
    private Token OpenBrace()
    {
        _state.OpenBraces++;
        _state.SavedInterpolatedStringsModes.Push(_state.InterpolatedStringIsMultiline);
        _state.SavedGroups.Push(_state.Groups);
        _state.Groups = 0;
        return CreateToken(TokenType.OpenBrace);
    }
    
    private Token ClosedBrace()
    {
        if (_state.OpenBraces <= 0)
        {
            return _state.InterpolationMode
                ? InterpolatedStringLiteral()
                : CreateToken(TokenType.CloseBrace);
        }
        _state.OpenBraces--;
        if (_state.SavedGroups.TryPop(out var groups))
        {
            _state.Groups = groups;
        }
        if (_state.SavedInterpolatedStringsModes.TryPop(out var interpolatedStrings))
        {
            _state.InterpolatedStringIsMultiline = interpolatedStrings;
        }
        return _state.InterpolationMode 
            ? InterpolatedStringLiteral() 
            : CreateToken(TokenType.CloseBrace);
    }

    private Token Newline() 
        => _state.Groups > 0 ? NextToken() : CreateToken(TokenType.Newline);

    private Token LeftAngle()
    {
        if (_state.CurrentChar != '=' || Peek() != '>')
        {
            return CharCharEq('<', TokenType.Lt, TokenType.Le, TokenType.Shl, TokenType.ShlEq);
        }
        NextChar();
        NextChar();
        return CreateToken(TokenType.Spaceship);
    }

    private Token RightAngle()
    {
        if (_state.CurrentChar != '>' || Peek() != '>')
        {
            return CharCharEq('>', TokenType.Gt, TokenType.Ge, TokenType.Shr, TokenType.ShrEq);
        }
        NextChar();
        NextChar();
        return CreateToken(Match('=') ? TokenType.AshrEq : TokenType.Ashr);
    }

    private Token Equals()
    {
        if (Match('='))
        {
            return CreateToken(Match('=') ? TokenType.EqEqEq : TokenType.EqEq);
        }
        return Match('>')
            ? CreateToken(TokenType.Arrow)
            : CreateToken(TokenType.Eq);
    }

    private Token Percent()
    {
        if (_state.LexPercentAsOperator ||
            (_state.CurrentChar != '0' && _state.CurrentChar != '1' &&
             _state.CurrentChar != '#' && _state.CurrentChar != '.'))
        {
            return CreateToken(Match('=') ? TokenType.PercentEq : TokenType.Percent);
        }
        if (char.IsDigit(_state.CurrentChar))
        {
            return IntLiteral(2, false);
        }
        while (Match('#') || Match('.')) { }
        return CreateToken(TokenType.AltBinLiteral);
    }

    private Token Dollar()
    {
        if (Behavior == LexerBehavior.Json) return CreateToken(TokenType.Unrecognized);
        if (char.IsAsciiHexDigit(_state.CurrentChar))
        {
            return IntLiteral(16, false);
        }
        if (Match('$'))
        {
            return CreateToken(TokenType.DollarDollar);
        }
        return Match('"') 
            ? InterpolatedStringLiteral() 
            : CreateToken(TokenType.Unrecognized);
    }

    private Token Dot()
    {
        if (Match('.')) return CreateToken(TokenType.DotDot);

        if (!_state.CurrentChar.IsIdentHead() || !_state.LexCommand)
        {
            return CreateToken(TokenType.Dot);
        }
        while (NextChar().IsIdent()) { }
        var text = Source.Text[_state.Cursor.Start.._state.Cursor.End].ToString();
        var type = CheckDirectiveType(text);
        if (type != TokenType.MacroName)
        {
            return CreateToken(type);
        }
        var caseSensitive = Behavior 
            is LexerBehavior.DefaultCaseSensitive 
            or LexerBehavior.LegacyDelimitersCaseSensitive;
        var isKeyword = (caseSensitive && Keywords.GetCaseSensitive(text, Cpu, _bra6502, _pseudoBra6502, out type)) ||
                        (!caseSensitive && Keywords.GetCaseInsensitive(text, Cpu, _bra6502, _pseudoBra6502, out type));
        return CreateToken(!isKeyword ? TokenType.MacroName : type);
    }

    
    private Token BackSlash()
    {
        if (Behavior != LexerBehavior.Json && (Match('\n') || Match('\r')))
        {
            return NextToken();    
        }
        if (_state.CurrentChar.IsIdentHead())
        {
            while (NextChar().IsIdent()) { }
            return CreateToken(TokenType.NamedArg);
        }
        if (!char.IsDigit(_state.CurrentChar) || _state.CurrentChar <= '0')
        {
            return CreateToken(Match('*') ? TokenType.VarArg : TokenType.Unrecognized);
        }
        while (char.IsDigit(NextChar())) { }
        return CreateToken(TokenType.MacroArg);
    }
    
    private Token Zero()
    {
        if (Behavior == LexerBehavior.Json)
        {
            return Check('.') 
                ? IntLiteral(10, false) 
                : CreateToken(TokenType.IntLiteral);
        }
        if (!Match('_'))
        {
            return _state.CurrentChar switch
            {
                'B' or 'b' => IntLiteral(2, true),
                >= '0' and <= '7'=> IntLiteral(8, false),
                '.' => FloatLiteral(10),
                'O' or 'o' => IntLiteral(8, true),
                'X' or 'x' => IntLiteral(16, true),
                _ => CreateToken(TokenType.IntLiteral)
            };
        }
        while (Match('_')) { }
        if (_state.CurrentChar >= 0 && _state.CurrentChar <= 7)
        {
            return IntLiteral(8, false);
        }
        return CreateToken(TokenType.IntLiteral);
    }

    private Token Digit()
    {
        while (Match('_')) {}
        return IntLiteral(10, false);
    }
    
    private Token IntLiteral(int radix, bool prefixed)
    {
        if (prefixed)
        {
            if (!char.IsAsciiHexDigit(Peek()))
            {
                return CreateToken(TokenType.IntLiteral);
            }
            NextChar();
        }
        EatDigits(radix);
        if (Check('.') || _state.CurrentChar.IsExponent())
        {
            return FloatLiteral(radix);
        }
        if (!char.IsAsciiHexDigit(_state.CurrentChar) || radix == 16)
        {
            return CreateToken(TokenType.IntLiteral);
        }
        EatDigits(16);
        throw new LexerException
        (
            LexerExceptionType.InvalidIntLiteral,
            CreateToken(TokenType.IntLiteral)
        );
    }

    private Token FloatLiteral(int radix)
    {
        var numLiteralType = TokenType.IntLiteral;
        if (Check('.') && char.IsAsciiHexDigit(Peek()))
        {
            NextChar();
            numLiteralType = TokenType.FloatLiteral;
            EatDigits(radix);
        }
        if (_state.CurrentChar.IsExponent())
        {
            var peek = 1;
            if (Peek() is '+' or '-')
            {
                peek++;
            }
            if (!char.IsDigit(PeekNth(peek)))
            {
                return CreateToken(numLiteralType);
            }
            while (peek-- > 0)
            {
                NextChar();
            }
            EatDigits(10);
            numLiteralType = TokenType.FloatLiteral;
        }
        return CreateToken(numLiteralType);
    }

    private Token CharLiteral()
    {
        var lexerCharLiteral = true;
        while (lexerCharLiteral)
        {
            switch (_state.CurrentChar)
            {
                case '\0':
                case '\n':
                case '\r':
                case '\'':
                    lexerCharLiteral = false;
                    break;
                case '\\':
                    NextChar();
                    NextChar();
                    break;
                default:
                    NextChar();
                    break;
            }
        }
        if (_state.CurrentChar != '\'')
        {
            throw new LexerException
            (
                LexerExceptionType.UnclosedChar,
                CreateToken(TokenType.CharLiteral)
            );
        }
        NextChar();
        return CreateToken(TokenType.CharLiteral);
    }

    private Token StringLiteral()
    {
        var multiline = Behavior != LexerBehavior.Json && Check('"') && Peek() == '"';
        var lexingLiteral = true;
        while (lexingLiteral)
        {
            switch (_state.CurrentChar)
            {
                case '"':
                    NextChar();
                    if (multiline)
                    {
                        if (!Check('"') || Peek() != '"') continue;
                        NextChar();
                        NextChar();
                    }
                    lexingLiteral = false;
                    continue;
                case '\0':
                    throw new LexerException
                    (
                        LexerExceptionType.UnclosedString,
                        CreateToken(TokenType.StringLiteral)
                    );
                case '\n':
                case '\r':
                    if (!multiline) 
                        throw new LexerException
                        (
                            LexerExceptionType.UnclosedString, 
                            CreateToken(TokenType.StringLiteral)
                        );
                    break;
                case '\\':
                    NextChar();
                    break;
            }
            NextChar();
        }
        return CreateToken(TokenType.StringLiteral);
    }

    private Token InterpolatedStringLiteral()
    {
        var multiline = Check('"') && Peek() == '"' && PeekNth(1) == '"';
        if (multiline)
        {
            NextChar();
            NextChar();
        }
        var lexingLiteral = true;
        while (lexingLiteral)
        {
            switch (_state.CurrentChar)
            {
                case '\0': 
                    throw new LexerException
                    (
                        LexerExceptionType.UnclosedString,
                        CreateToken(TokenType.StringLiteral)
                    );
                case '"':
                    if (multiline)
                    {
                        if (_state.CurrentChar != '"' || Peek() != '"') continue;
                        NextChar();
                        NextChar();
                    }
                    lexingLiteral = false;
                    break;
                case '\\':
                    NextChar();
                    break;
                case '{':
                    if (NextChar() != '{')
                    {
                        _state.InterpolatedStringIsMultiline = multiline;
                        _state.InterpolationMode = true;
                        _state.OpenBraces++;
                        return CreateToken(TokenType.InterpolationStart);
                    }
                    break;
                case '}':
                    if (!Match('}'))
                    {
                        throw new LexerException(LexerExceptionType.UnclosedString, CreateToken(TokenType.InterpolationEnd));
                    }
                    break;
            }
            NextChar();
        }
        if (!_state.InterpolationMode)
        {
            return CreateToken(TokenType.StringLiteral);
        }
        _state.InterpolatedStringIsMultiline = false;
        _state.InterpolationMode = _state.OpenBraces > 0;
        return CreateToken(TokenType.InterpolationEnd);
    }

    private Token EncodedStringLiteral()
    {
        if (Behavior == LexerBehavior.Json ||
            (_state.PreviousChar == 'u' && 
             !(_state.CurrentChar == '"' || (_state.CurrentChar == '8' && Peek() == '"'))) || 
            (_state.PreviousChar != 'u' && _state.CurrentChar != '"'))
        {
            return Ident();
        }

        var type = _state.PreviousChar switch
        {
            'A' or 'a' => TokenType.AtaScreenStringLiteral,
            'P' or 'p' => TokenType.PetsciiStringLiteral,
            'S' or 's' => TokenType.CbmScreenStringLiteral,
            'u' when _state.CurrentChar == '8' =>  TokenType.Utf8StringLiteral,
            'u' => TokenType.Utf16StringLiteral,
            'U' => TokenType.Utf32StringLiteral,
            _ => TokenType.StringLiteral
        };
        if (_state is { PreviousChar: 'u', CurrentChar: '8' })
        {
            NextChar();
        }
        NextChar();
        while (_state.CurrentChar != '"')
        {
            switch (_state.CurrentChar)
            {
                case '\0':
                case '\n':
                case '\r':
                    throw new LexerException
                    (
                        LexerExceptionType.UnclosedString,
                        CreateToken(TokenType.StringLiteral)
                    );
                case '\\':
                    NextChar();
                    break;
            }
            NextChar();
        }
        NextChar();
        return CreateToken(type);
    }
    
    private Token Ident()
    {
        while (_state.CurrentChar.IsIdent())
        {
            NextChar();
        }
        var text = Source.Text[_state.Cursor.Start.._state.Cursor.End].ToString();
        var caseSensitive = Behavior is LexerBehavior.DefaultCaseSensitive or LexerBehavior.LegacyDelimitersCaseSensitive;

        if (text.Equals("false", caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase))
        {
            return CreateToken(TokenType.False);
        }
        if (text.Equals("true", caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase))
        {
            return CreateToken(TokenType.True);
        }
        var type = TokenType.Ident;
        var isKeyword = (caseSensitive && Keywords.GetCaseSensitive(text, Cpu, _bra6502, _pseudoBra6502, out type)) ||
            (!caseSensitive && Keywords.GetCaseInsensitive(text, Cpu, _bra6502, _pseudoBra6502, out type));
        if (!isKeyword)
        {
            type = TokenType.Ident;
        }
        return type switch
        {
            TokenType.Af when Match('\'') => CreateToken(TokenType.ShadowAf),
            TokenType.St when _state.Cpu == Cpu.I86 => St(),
            _ => CreateToken(type)
        };
    }

    private Token St()
    {
        Mark();
        SkipWhitespace();
        if (Match('('))
        {
            SkipWhitespace();
            if (_state.CurrentChar is >= '0' and <= '7')
            {
                var stNum = _state.CurrentChar;
                NextChar();
                SkipWhitespace();
                if (Match(')'))
                {
                    var type = stNum switch
                    {
                        '0' => TokenType.StParen0Reg,
                        '1' => TokenType.StParen1Reg,
                        '2' => TokenType.StParen2Reg,
                        '3' => TokenType.StParen3Reg,
                        '4' => TokenType.StParen4Reg,
                        '5' => TokenType.StParen5Reg,
                        '6' => TokenType.StParen6Reg,
                        _ => TokenType.StParen7Reg
                    };
                    Release();
                    return CreateToken(type);
                }
            }
        }
        Restore();
        return CreateToken(TokenType.St);
    }
    
    private Token CharChar(char c, TokenType charType, TokenType eqType, TokenType charCharType)
    {
        if (Match(c))
        {
            return CreateToken(charCharType);
        }
        return CreateToken(Match('=') ? eqType : charType);
    }

    private Token CharCharEq
    (
        char c,
        TokenType charType, 
        TokenType eqType, 
        TokenType charCharType, 
        TokenType charCharEqType
    )
    {
        return Match(c) 
            ? CreateToken(Match('=') ? charCharEqType : charCharType) 
            : CreateToken(Match('=') ? eqType : charType);
    }
    
    private Token CreateToken(TokenType tokenType)
    {
        _state.LexPercentAsOperator = tokenType.EndsExpression();
        _state.LastType = tokenType;
        var adjustedColumn = _state.AdjustedLineStart != _state.LineStart
            ? _state.ColumnCursor.Start - _state.AdjustedLineStart - _state.LineStart
            : _state.ColumnCursor.Start;
        return new Token
            (
                Source, 
                _state.Cursor, 
                tokenType, 
                _state.Line, 
                _state.ColumnCursor.Start, 
                adjustedColumn,
                _state.AdjustedLineStart,
                [..Inclusions]
            );
    }
    
    private void SkipWhitespace()
    {
        if (Behavior == LexerBehavior.Json)
        {
            while (char.IsWhiteSpace(_state.CurrentChar))
            {
                NextChar();
            }
            return;
        }
        while (_state.CurrentChar.IsHorizontalWhitespace() ||
               _state.CurrentChar == ';' ||
               _state.CurrentChar == '/')
        {
            if (Check(';') || (Check('/') && Peek() == '/'))
            {
                while (!_state.CurrentChar.IsVerticalWhitespace() && _state.CurrentChar != '\0')
                {
                    NextChar();
                }
                continue;
            }
            if (Check('/'))
            {
                if (Peek() != '*') return;
                NextChar();
                Mark();
                while (true)
                {
                    if (Check('\0'))
                    {
                        Restore();
                        _state.ColumnCursor.Start = _state.ColumnCursor.End - 1;
                        _state.Cursor.Start = _state.Cursor.End - 1;
                        throw new LexerException
                        (
                            LexerExceptionType.UnclosedBlockComment, 
                            CreateToken(TokenType.Slash)
                        );
                    }
                    if (NextChar() == '*' && NextChar() == '/')
                    {
                        break;
                    }
                }
                Release();
                NextChar(); // consume final `/`
                continue;
            }
            NextChar();
        }
    }

    private void EatDigits(int radix)
    {
        if (Behavior == LexerBehavior.Json) radix = 10;
        while (_state.CurrentChar.IsBaseDigit(radix))
        {
            NextChar();
            while (Check('_') && Behavior != LexerBehavior.Json)
            {
                NextChar();
            }
        }
    }

    private bool Check(char c) => _state.CurrentChar == c;
    
    private bool Match(char c)
    {
        if (!Check(c)) return false;
        NextChar();
        return true;
    }
    
    private char NextChar()
    {
        var previous = _state.PreviousChar;
        _state.PreviousChar = _state.CurrentChar;
        if (previous.IsVerticalWhitespace())
        {
            // handle the `\r\n` case
            if (_state is { PreviousChar: '\r', CurrentChar: '\n' })
            {
                _state.PreviousChar = '\n';
                _state.Cursor.End++;
                _state.CurrentChar = Source.Text.Length > _state.Cursor.End 
                    ? Source.Text[_state.Cursor.End] 
                    : '\0';
            }
            _state.Line++;
            _state.ColumnCursor.Start = 0;
            _state.ColumnCursor.End = 1;
            _state.AdjustedLineStart = _state.Cursor.End;
            _state.LineStart = _state.Cursor.End;
        }
        else
        {
            _state.ColumnCursor.End++;
        }
        if (_state.Cursor.End < Source.Text.Length)
        {
            _state.Cursor.End++;
        }
        _state.CurrentChar = Source.Text.Length > _state.Cursor.End 
            ? Source.Text[_state.Cursor.End] 
            : '\0';
        return _state.CurrentChar;
    }

    private char Peek() => PeekNth(1);

    private char PeekNth(int n)
    {
        return _state.Cursor.End + n < Source.Text.Length
            ? Source.Text[_state.Cursor.End + n]
            : '\0';
    }
    
    internal List<Inclusion> Inclusions { get; }
    
    internal LexerBehavior Behavior { get; }
}