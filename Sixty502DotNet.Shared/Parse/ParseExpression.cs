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


using Sixty502DotNet.Shared.Error;
using Sixty502DotNet.Shared.Eval;
using Sixty502DotNet.Shared.Lex;
using System.Collections.Frozen;
using Sixty502DotNet.Shared.Parse.Ast;
using KeyValuePair = Sixty502DotNet.Shared.Parse.Ast.KeyValuePair;
using Range = Sixty502DotNet.Shared.Parse.Ast.Range;

// ReSharper disable NullableWarningSuppressionIsUsed

namespace Sixty502DotNet.Shared.Parse;

using PrefixParseFn = Func<Parser, Expression>;
using InfixParseFn = Func<Parser, Expression, Expression>;

public sealed partial class Parser
{
    private enum Precedence
    {
        None,
        /// <summary>
        /// <c>expr ? expr : expr </c>
        /// </summary>
        Conditional,
        /// <summary>
        /// <c>expr || expr</c>
        /// </summary>
        LogicalOr,
        /// <summary>
        /// <c>expr &amp;&amp; expr</c>
        /// </summary>
        LogicalAnd,
        /// <summary>
        /// <c>expr | expr</c>
        /// </summary>
        BitwiseOr,
        /// <summary>
        /// <c>expr ^ expr</c>
        /// </summary>
        BitwiseXor,
        /// <summary>
        /// <c>expr &amp; expr</c>
        /// </summary>
        BitwiseAnd,
        /// <summary>
        /// <c>expr == expr</c>, <c>expr != expr</c>
        /// </summary>
        Equality,
        /// <summary>
        /// <c>expr &lt; expr</c>, <c>expr &lt;= expr</c>, <c>expr &gt; expr</c>, <c>
        /// expr &gt;= expr</c>, <c>expr &lt;=&gt; expr</c>
        /// </summary>
        Comparison,
        /// <summary>
        /// <c>expr &lt;&lt; expr</c>, <c>expr &gt;&gt; expr</c>, <c>
        /// expr &gt;&gt;&gt; expr</c>
        /// </summary>
        Shift,
        /// <summary>
        /// <c>expr + expr</c>, <c>expr - expr</c>
        /// </summary>
        Addition,
        /// <summary>
        /// <c>expr * expr</c>, <c>expr / expr</c>, <c>expr % expr</c>
        /// </summary>
        Multiplication,
        /// <summary>
        /// <c>expr ^^ expr</c>
        /// </summary>
        Exponentiation,
        /// <summary>
        /// <c>expr()</c>, <c>expr[]</c>
        /// </summary>
        Call,
        /// <summary>
        /// <c>expr.IDENT</c>
        /// </summary>
        MemberAccess
    }
    
    private readonly struct ParseRule(PrefixParseFn? prefix, InfixParseFn? infix, Precedence precedence)
    {
        public PrefixParseFn? Prefix { get; } = prefix;
    
        public InfixParseFn? Infix { get; } = infix;

        public Precedence Precedence { get; } = precedence;
    }
    
    private static readonly FrozenDictionary<TokenType, ParseRule> s_parseRules
        = new Dictionary<TokenType, ParseRule>
    {
        { TokenType.Dot, new ParseRule(null, Member, Precedence.MemberAccess) }, 
        { TokenType.OpenParen, new ParseRule(OpenParen, Call, Precedence.Call) }, 
        { TokenType.OpenBracket, new ParseRule(ArrayInit, Subscript, Precedence.Call) },
        { TokenType.OpenBrace, new ParseRule(DictionaryInit, null, Precedence.None) }, 
        { TokenType.CaretCaret, new ParseRule(Unary, Binary, Precedence.Exponentiation) }, 
        { TokenType.Star, new ParseRule(Primary, Binary, Precedence.Multiplication) },
        { TokenType.Percent, new ParseRule(null, Binary, Precedence.Multiplication) }, 
        { TokenType.Slash, new ParseRule(null, Binary, Precedence.Multiplication) },
        { TokenType.Plus, new ParseRule(Unary, Binary, Precedence.Addition) },
        { TokenType.Minus, new ParseRule(Unary, Binary, Precedence.Addition) },
        { TokenType.Shl, new ParseRule(null, Binary, Precedence.Shift) },
        { TokenType.Shr, new ParseRule(null, Binary, Precedence.Shift) },
        { TokenType.Ashr, new ParseRule(null, Binary, Precedence.Shift) },
        { TokenType.Lt, new ParseRule(Unary, Binary, Precedence.Comparison) },
        { TokenType.Le, new ParseRule(null, Binary, Precedence.Comparison) },
        { TokenType.Spaceship, new ParseRule(null, Binary, Precedence.Comparison) },
        { TokenType.Gt, new ParseRule(Unary, Binary, Precedence.Comparison) },
        { TokenType.Ge, new ParseRule(null, Binary, Precedence.Comparison) }, 
        { TokenType.EqEqEq, new ParseRule(null, Binary, Precedence.Equality) },
        { TokenType.BangEqEq, new ParseRule(null, Binary, Precedence.Equality) },
        { TokenType.EqEq, new ParseRule(null, Binary, Precedence.Equality) },
        { TokenType.BangEq, new ParseRule(null, Binary, Precedence.Equality) }, 
        { TokenType.BitwiseAnd, new ParseRule(Unary, Binary, Precedence.BitwiseAnd) },
        { TokenType.Caret, new ParseRule(Unary, Binary, Precedence.BitwiseXor) },
        { TokenType.BitwiseOr, new ParseRule(null, Binary, Precedence.BitwiseOr) },
        { TokenType.AndAnd, new ParseRule(null, Binary, Precedence.LogicalAnd) },
        { TokenType.OrOr, new ParseRule(null, Binary, Precedence.LogicalOr) }, 
        { TokenType.DollarDollar, new ParseRule(Primary, null, Precedence.None) },
        { TokenType.QuestionMark, new ParseRule(null, Ternary, Precedence.Conditional) },
        { TokenType.A, new ParseRule(Primary, null, Precedence.None) }, 
        { TokenType.Ah, new ParseRule(Primary, null, Precedence.None) },
        { TokenType.Al, new ParseRule(Primary, null, Precedence.None) },
        { TokenType.Ax, new ParseRule(Primary, null, Precedence.None) },
        { TokenType.B, new ParseRule(Primary, null, Precedence.None) },
        { TokenType.Bc, new ParseRule(Primary, null, Precedence.None) },
        { TokenType.Bh, new ParseRule(Primary, null, Precedence.None) },
        { TokenType.Bl, new ParseRule(Primary, null, Precedence.None) },
        { TokenType.Bp, new ParseRule(Primary, null, Precedence.None) }, 
        { TokenType.Bx, new ParseRule(Primary, null, Precedence.None) },
        { TokenType.Byte, new ParseRule(Primary, null, Precedence.None) },
        { TokenType.C, new ParseRule(Primary, null, Precedence.None) }, 
        { TokenType.Cc, new ParseRule(Primary, null, Precedence.None) },
        { TokenType.Ch, new ParseRule(Primary, null, Precedence.None) },
        { TokenType.Cl, new ParseRule(Primary, null, Precedence.None) },
        { TokenType.Cs, new ParseRule(Primary, null, Precedence.None) },
        { TokenType.Cx, new ParseRule(Primary, null, Precedence.None) },
        { TokenType.D, new ParseRule(Primary, null, Precedence.None) }, 
        { TokenType.De, new ParseRule(Primary, null, Precedence.None) },
        { TokenType.Dh, new ParseRule(Primary, null, Precedence.None) },
        { TokenType.Di, new ParseRule(Primary, null, Precedence.None) },
        { TokenType.Dl, new ParseRule(Primary, null, Precedence.None) },
        { TokenType.Dp, new ParseRule(Primary, null, Precedence.None) },
        { TokenType.Ds, new ParseRule(Primary, null, Precedence.None) },
        { TokenType.Dword, new ParseRule(Primary, null, Precedence.None) },
        { TokenType.Dx, new ParseRule(Primary, null, Precedence.None) },
        { TokenType.E, new ParseRule(Primary, null, Precedence.None) }, 
        { TokenType.Es, new ParseRule(Primary, null, Precedence.None) },
        { TokenType.H, new ParseRule(Primary, null, Precedence.None) }, 
        { TokenType.Hl, new ParseRule(Primary, null, Precedence.None) }, 
        { TokenType.I, new ParseRule(Primary, null, Precedence.None) }, 
        { TokenType.Ix, new ParseRule(Primary, null, Precedence.None) }, 
        { TokenType.Ixh, new ParseRule(Primary, null, Precedence.None) }, 
        { TokenType.Ixl, new ParseRule(Primary, null, Precedence.None) }, 
        { TokenType.Iy, new ParseRule(Primary, null, Precedence.None) }, 
        { TokenType.Iyh, new ParseRule(Primary, null, Precedence.None) }, 
        { TokenType.Iyl, new ParseRule(Primary, null, Precedence.None) }, 
        { TokenType.L, new ParseRule(Primary, null, Precedence.None) }, 
        { TokenType.M, new ParseRule(Primary, null, Precedence.None) }, 
        { TokenType.Nc, new ParseRule(Primary, null, Precedence.None) }, 
        { TokenType.Nz, new ParseRule(Primary, null, Precedence.None) }, 
        { TokenType.Pc, new ParseRule(Primary, null, Precedence.None) }, 
        { TokenType.Pcr, new ParseRule(Primary, null, Precedence.None) }, 
        { TokenType.Pe, new ParseRule(Primary, null, Precedence.None) }, 
        { TokenType.Po, new ParseRule(Primary, null, Precedence.None) }, 
        { TokenType.Psw, new ParseRule(Primary, null, Precedence.None) },
        { TokenType.Ptr, new ParseRule(Primary, null, Precedence.None) },
        { TokenType.Qword, new ParseRule(Primary, null, Precedence.None) },
        { TokenType.R, new ParseRule(Primary, null, Precedence.None) }, 
        { TokenType.S, new ParseRule(Primary, null, Precedence.None) }, 
        { TokenType.Si, new ParseRule(Primary, null, Precedence.None) },
        { TokenType.Sp, new ParseRule(Primary, null, Precedence.None) },
        { TokenType.Ss, new ParseRule(Primary, null, Precedence.None) },
        { TokenType.St, new ParseRule(Primary, null, Precedence.None) },
        { TokenType.St0Reg, new ParseRule(Primary, null, Precedence.None) },
        { TokenType.St1Reg, new ParseRule(Primary, null, Precedence.None) },
        { TokenType.St2Reg, new ParseRule(Primary, null, Precedence.None) },
        { TokenType.St3Reg, new ParseRule(Primary, null, Precedence.None) },
        { TokenType.St4Reg, new ParseRule(Primary, null, Precedence.None) },
        { TokenType.St5Reg, new ParseRule(Primary, null, Precedence.None) },
        { TokenType.St6Reg, new ParseRule(Primary, null, Precedence.None) },
        { TokenType.St7Reg, new ParseRule(Primary, null, Precedence.None) },
        { TokenType.Tbyte, new ParseRule(Primary, null, Precedence.None) },
        { TokenType.U, new ParseRule(Primary, null, Precedence.None) }, 
        { TokenType.Word, new ParseRule(Primary, null, Precedence.None) },
        { TokenType.X, new ParseRule(Primary, null, Precedence.None) }, 
        { TokenType.Y, new ParseRule(Primary, null, Precedence.None) }, 
        { TokenType.Ident, new ParseRule(Primary, null, Precedence.None) },
        { TokenType.AltBinLiteral , new ParseRule(Primary, null, Precedence.None) },
        { TokenType.CharLiteral, new ParseRule(Primary, null, Precedence.None) },
        { TokenType.StringLiteral, new ParseRule(Primary, null, Precedence.None) },
        { TokenType.AtaScreenStringLiteral, new ParseRule(Primary, null, Precedence.None) },
        { TokenType.CbmScreenStringLiteral, new ParseRule(Primary, null, Precedence.None) },
        { TokenType.PetsciiStringLiteral,  new ParseRule(Primary, null, Precedence.None) },
        { TokenType.Utf8StringLiteral, new ParseRule(Primary, null, Precedence.None) },
        { TokenType.Utf16StringLiteral, new ParseRule(Primary, null, Precedence.None ) },
        { TokenType.Utf32StringLiteral, new ParseRule(Primary, null, Precedence.None ) },
        { TokenType.FloatLiteral, new ParseRule(Primary, null, Precedence.None) }, 
        { TokenType.IntLiteral, new ParseRule(Primary, null, Precedence.None) }, 
        { TokenType.True, new ParseRule(Primary, null, Precedence.None) }, 
        { TokenType.False, new ParseRule(Primary, null, Precedence.None) }, 
        { TokenType.InterpolationStart, new ParseRule(InterpolStart, Interpolation, Precedence.None) },
        { TokenType.InterpolationEnd, new ParseRule(Primary, InterpolEnd, Precedence.None) },
        { TokenType.Tilde, new ParseRule(Unary, null, Precedence.None) },
        { TokenType.Bang, new ParseRule(Unary, null, Precedence.None) }, 
    }.ToFrozenDictionary();
    
    private static PrimaryExpression Primary(Parser parser)
    {
        var expr = new PrimaryExpression(parser._current)
        {
            StatementIndex = parser._statementIndex
        };
        parser.Advance();
        return expr;
    }
    
    private static UnaryOpExpression InterpolStart(Parser parser)
    {
        var str = parser._current;
        parser.Advance();
        parser.DiscardNewlines();
        var expr = ParsePrecedence(parser, Precedence.Conditional);
        Expression? width = null;
        if (parser.Match(TokenType.Comma))
        {
            parser.DiscardNewlines();
            width = ParsePrecedence(parser, Precedence.Conditional);
            if (!width.IsConstant())
            {
                throw new CompileException(CompileExceptionType.ValueNotConstant, width);
            }
        }
        string? format = null;
        if (parser.Match(TokenType.Colon))
        {
            var formatExpr = ParsePrecedence(parser, Precedence.Conditional);
            if (formatExpr is not PrimaryExpression primary || !primary.Expr.Type.IsIdent())
            {
                throw new CompileException(CompileExceptionType.UnexpectedExpression, formatExpr);
            }
            format = primary.Expr.Text.ToString();
        }
        var interpol = new InterpolationExpression(expr, width, format)
        {
            RightToken = parser._previous,
            StatementIndex = parser._statementIndex
        };
        return new UnaryOpExpression(str, interpol, false);
    }
    
    private static Expression ParsePrecedence(Parser parser, Precedence precedence)
    {
        parser._lexer.LexCommand = false;
        parser._lexer.LexPercentAsOperator = false;
        if (!s_parseRules.TryGetValue(parser._current.Type, out var rule) || rule.Prefix == null)
            throw new CompileException
            (
                CompileExceptionType.ExpectedExpression, 
                parser._current
            );
        var infixExpr = rule.Prefix(parser);
        if (infixExpr is DictionaryInitExpression or FunctionExpression) return infixExpr;
        var op = parser._current;
        while (s_parseRules.TryGetValue(op.Type, out rule) && precedence <= rule.Precedence)
        {
            if (rule.Infix == null)
            {
                throw new CompileException(CompileExceptionType.UnexpectedToken, op);
            }
            infixExpr = rule.Infix(parser, infixExpr);
            op = parser._current;
        }
        if (infixExpr.IsConstant())
        {
            infixExpr.Value = new ExpressionFolder().Visit(infixExpr) ?? new Value();
        }
        return infixExpr;
    }
    
    private static Expression OpenParen(Parser parser)
    {
        var openParen = parser._current;
        var isArrowFunc = true;
        parser.Advance();
        var parameters = new List<PrimaryExpression>();
        Token? firstDefaultEq = null;
        var innerExpressions = new List<Expression>();
        var defaultValues = new List<Expression>();
        if (!parser.Check(TokenType.CloseParen))
        {
            do
            {
                var expr = parser.Expression();
                innerExpressions.Add(expr);
                if (!isArrowFunc)
                {
                    if (!parser.Match(TokenType.Comma)) break;
                    continue;
                }
                if (expr is PrimaryExpression param && param.Expr.Type.IsIdent())
                {
                    parameters.Add(param);
                    var eq = parser._current;
                    if (parser.Match(TokenType.Eq))
                    {
                        firstDefaultEq ??= parser._previous;
                        defaultValues.Add(parser.Expression());
                        if (!parser.Match(TokenType.Comma)) break;
                    }
                    else if (defaultValues.Count > 0)
                    {
                        throw new CompileException
                        (
                            CompileExceptionType.DefaultValueNotSpecified, 
                            eq
                        );
                    }
                }
                else
                {
                    isArrowFunc = false;
                    if (firstDefaultEq != null)
                    {
                        throw new ParserException(TokenType.CloseParen, firstDefaultEq.Value);
                    }
                }
                if (!parser.Match(TokenType.Comma)) break;
            } while (true);
        }
        parser.Consume(TokenType.CloseParen);
        if (isArrowFunc && parser.Match(TokenType.Arrow))
        {
            var arrow = parser._previous;
            parser.DiscardNewlines();
            if (!parser.Check(TokenType.OpenBrace))
            {
                return new FunctionExpression(parameters, defaultValues, parser.Expression())
                {
                    LeftToken = openParen,
                    RightToken = parser._previous,
                    StatementIndex = parser._statementIndex
                };
            }

            var beginning = parser._current;
            parser.SetRecoverDelimiter(TokenType.CloseBrace);
            parser._lexer.LexCommand = true;
            parser.Advance();
            parser.DiscardNewlines();
            var body = new List<Statement>();
            while (!parser.Match(TokenType.CloseBrace))
            {
                parser.DiscardNewlines();
                if (parser.Check(TokenType.Eof))
                {
                    parser._recoveryDelimiters.Clear();
                    throw new UnresolvedDeclException
                    (
                        CompileExceptionType.ExpectedTokenException,
                        TokenType.CloseBrace, 
                        beginning,
                        arrow, 
                        parser._previous
                    );
                }
                body.Add(parser.Statement());
            }
            return new FunctionExpression(parameters, defaultValues, body)
            {
                LeftToken = openParen,
                RightToken = parser._previous,
                StatementIndex = parser._statementIndex
            };
        }
        switch (innerExpressions.Count)
        {
            case 0:
                throw new CompileException(CompileExceptionType.ExpectedExpression, parser._previous);
            case > 1:
                return new ArrayInitExpression(true, openParen, innerExpressions)
                {
                    RightToken = parser._previous,
                    StatementIndex = parser._statementIndex
                };
        }
        return new GroupedExpression(openParen, innerExpressions[0], parser._previous);
    }
    
    private static ArrayInitExpression ArrayInit(Parser parser)
    {
        var leftBracket = parser._current;
        parser.Advance();
        var array = new ArrayInitExpression(false, leftBracket)
        {
            StatementIndex =  parser._statementIndex
        };
        do
        {
            array.Expressions.Add(parser.Expression());
            if (!parser.Match(TokenType.Comma)) break;
        } 
        while (!parser.Check(TokenType.CloseBracket));
        parser.Consume(TokenType.CloseBracket);
        array.RightToken = parser._previous;
        return array;
    }

    private static DictionaryInitExpression DictionaryInit(Parser parser)
    {
        var openBrace = parser._current;
        DictionaryInitExpression? dict = null;
        parser._lexer.LexCommand = false;
        parser.Advance();
        parser.DiscardNewlines();
        do
        {
            Expression key;
            if (parser.Match(TokenType.Dot))
            {
                var dot = parser._previous;
                var dotKey = parser._current;
                if (dotKey.Column != dot.Column + 1)
                {
                    throw new CompileException(CompileExceptionType.ExpectedExpression, dot);
                }
                parser.ConsumeIdent();
                var keyToken = new Token(dotKey, TokenType.StringLiteral, $"\"{dotKey.Text}\"");
                key = new PrimaryExpression(keyToken);
            }
            else
            {
                key = parser.Expression();
            }
            parser.Consume(TokenType.Colon);
            var value = parser.Expression();
            if (dict == null)
            {
                dict = new DictionaryInitExpression(key, value)
                {
                    LeftToken = openBrace,
                    StatementIndex =  parser._statementIndex
                };
            }
            else
            {
                dict.Members.Add(new KeyValuePair(key, value));
            }
            parser.DiscardNewlines();
            if (!parser.Match(TokenType.Comma)) break;
            parser.DiscardNewlines();
        } 
        while (!parser.Check(TokenType.CloseBrace));
        parser.Consume(TokenType.CloseBrace);
        dict.RightToken = parser._previous;
        return dict;
    }

    private static UnaryOpExpression InterpolEnd(Parser parser, Expression left)
    {
        var str = parser._current;
        parser.Advance();
        return new UnaryOpExpression(str, left, true)
        {
            StatementIndex = parser._statementIndex
        };
    }
    
    private static BinaryOpExpression Interpolation(Parser parser, Expression left)
    {
        var str = parser._current;
        parser.Advance();
        parser.DiscardNewlines();
        var interpolExpr = ParsePrecedence(parser, Precedence.Conditional);
        parser.DiscardNewlines();

        Expression? width = null;
        if (parser.Match(TokenType.Comma))
        {
            parser.DiscardNewlines();
            width = ParsePrecedence(parser, Precedence.Conditional);
            if (!width.IsConstant())
            {
                throw new CompileException(CompileExceptionType.ValueNotConstant, width);
            }
        }
        string? format = null;
        if (parser.Match(TokenType.Colon))
        {
            var formatExpr = ParsePrecedence(parser, Precedence.Conditional);
            if (formatExpr is not PrimaryExpression primary || !primary.Expr.Type.IsIdent())
            {
                throw new CompileException(CompileExceptionType.UnexpectedExpression, formatExpr);
            }
            format = primary.Expr.Text.ToString();
        }
        var interpolExpression = new InterpolationExpression(interpolExpr, width, format)
        {
            RightToken = parser._previous,
            StatementIndex = parser._statementIndex
        };
        return new BinaryOpExpression(left, str, interpolExpression)
        {
            StatementIndex = parser._statementIndex
        };
    }
    
    private static Expression Unary(Parser parser)
    {
        var op = parser._current;
        parser.Advance();
        Expression expr;
        switch (op.Type)
        {
            case TokenType.Plus:
            case TokenType.Minus:
                if (!parser._current.Type.BeginsExpression() && 
                    !parser.Check(TokenType.Tilde) && 
                    !parser.Check(TokenType.Bang) &&
                    !parser.Check(TokenType.Lt) &&
                    !parser.Check(TokenType.Gt) &&
                    !parser.Check(TokenType.BitwiseAnd) &&
                    !parser.Check(TokenType.Caret) &&
                    !parser.Check(TokenType.CaretCaret))
                {
                    return Ref(parser);
                }
                expr = ParsePrecedence(parser, Precedence.Call);
                break;
            case TokenType.Tilde:
            case TokenType.Bang:
                expr = ParsePrecedence(parser, Precedence.Call);
                break;
            case TokenType.BitwiseAnd:
            case TokenType.Caret:
            case TokenType.Lt:
            case TokenType.Gt:
            case TokenType.CaretCaret:
            default:
                expr = parser.Expression();
                break;
        }
        return new UnaryOpExpression(op, expr, false)
        {
            StatementIndex = parser._statementIndex
        };
    }

    private static AnonymousRefExpression Ref(Parser parser)
    {
        var count = 1;
        var type = parser._previous;
        while (parser.Match(type.Type))
        {
            count++;
        }
        return new AnonymousRefExpression(type, parser._previous, count)
        {
            StatementIndex = parser._statementIndex
        };
    }

    private static MemberExpression Member(Parser parser, Expression left)
    {
        parser.Advance();
        parser.DiscardNewlines();
        var member = parser._current;
        parser.ConsumeIdent();
        return new MemberExpression(left, member)
        {
            StatementIndex = parser._statementIndex
        };
    }
    
    private static BinaryOpExpression Binary(Parser parser, Expression left)
    {
        var op = parser._current;
        parser.Advance();
        parser.DiscardNewlines();
        var rule = s_parseRules[op.Type];
        var associativity = op.Type == TokenType.CaretCaret ? 0 : 1;
        var rhs = ParsePrecedence(parser, rule.Precedence + associativity);
        return new BinaryOpExpression(left, op, rhs)
        {
            StatementIndex = parser._statementIndex
        };
    }

    private static SubscriptExpression Subscript(Parser parser, Expression left)
    {
        var op = parser._current;
        parser.Advance();
        Expression? start = null;
        if (!parser.Check(TokenType.DotDot))
        {
            start = parser.Expression();
        }
        if (parser.Match(TokenType.DotDot))
        {
            Expression? end = null;
            var rangeOp = parser._previous;
            var inclusive = parser.Match(TokenType.Caret);
            if (inclusive || start == null || !parser.Check(TokenType.CloseBracket))
            {
                end = parser.Expression();
            }
            parser.Consume(TokenType.CloseBracket);
            var rangeIndex = new Range
            (
                start, 
                end, 
                rangeOp, 
                inclusive 
                    ? RangeType.IsRangeIncludesEnd 
                    : RangeType.IsRange
            )
            {
                LeftToken = op,
                RightToken = parser._previous,
                StatementIndex = parser._statementIndex
            };
            return new SubscriptExpression(left, rangeIndex)
            {
                RightToken = parser._previous,
                StatementIndex =  parser._statementIndex
            };
        }
        var index = new Range(start!);
        parser.Consume(TokenType.CloseBracket);
        return new SubscriptExpression(left, index)
        {
            RightToken = parser._previous,
            StatementIndex = parser._statementIndex
        };
    }

    private static CallExpression Call(Parser parser, Expression left)
    {
        parser.Advance();
        var callExpr = new CallExpression(left)
        {
            LeftToken = left.LeftToken,
            StatementIndex = parser._statementIndex
        };
        if (!parser.Check(TokenType.CloseParen))
        {
            do
            {
                callExpr.Arguments.Add(parser.Expression());
                if (!parser.Match(TokenType.Comma)) break;
            } 
            while (true);
        }
        parser.Consume(TokenType.CloseParen);
        callExpr.RightToken = parser._previous;
        return callExpr;
    }

    private static TernaryExpression Ternary(Parser parser, Expression left)
    {
        parser.Advance();
        parser.DiscardNewlines();
        var then = parser.Expression();
        parser.Consume(TokenType.Colon);
        var @else = parser.Expression();
        return new TernaryExpression(left, then, @else)
        {
            StatementIndex = parser._statementIndex
        };
    }
}