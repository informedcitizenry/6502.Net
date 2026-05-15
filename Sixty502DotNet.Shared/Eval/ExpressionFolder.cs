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
using Sixty502DotNet.Shared.Lex;
using Sixty502DotNet.Shared.Parse.Ast;

namespace Sixty502DotNet.Shared.Eval;

public sealed class ExpressionFolder : IExpressionVisitor<Value?>
{
    private static string GetStringText(PrimaryExpression primary)
    {
        if (primary.Expr.Text[0] == '"')
        {
            return primary.Expr.Text.Trim('"').ToString();
        }
        var start = primary.Expr.Type switch
        {
            TokenType.Utf8StringLiteral when primary.Expr.Text[1] == '8' => 3,
            TokenType.AtaScreenStringLiteral or
            TokenType.CbmScreenStringLiteral or
            TokenType.InterpolationStart or 
            TokenType.PetsciiStringLiteral or
            TokenType.Utf16StringLiteral or 
            TokenType.Utf32StringLiteral => 2,
            _ => 1
        };
        var end = primary.Expr.Text.Length - (start + 1);
        var text = primary.Expr.Text[start..end].ToString();
        
        return text;
    }
    
    public static string EvalStringLiteral(Expression expression)
    {
        if (expression is not PrimaryExpression primary || !primary.LeftToken.Type.IsStringLiteral())
            throw new CompileException(CompileExceptionType.StringLiteralExpected, expression);
        return GetStringText(primary);
    }

    public Value? VisitPrimaryExpression(PrimaryExpression expression)
    {
        var token = expression.Expr;
        var text = token.Text.ToString();
        switch (token.Type)
        {
            case TokenType.CharLiteral:
            {
                var chr = ValueHelper.GetChar(text);
                return chr == null
                    ? throw new CompileException(CompileExceptionType.InvalidCharLiteral, expression)
                    : new Value(chr.Value);
            }
            case TokenType.StringLiteral:
            case TokenType.AtaScreenStringLiteral:
            case TokenType.CbmScreenStringLiteral:
            case TokenType.PetsciiStringLiteral:
            case TokenType.Utf8StringLiteral:
            case TokenType.Utf16StringLiteral:
            case TokenType.Utf32StringLiteral:
            case TokenType.InterpolationStart:
            case TokenType.InterpolationEnd:
            {
                var encodingType = token.Type.ToTextEncodingType();
                var s = ValueHelper.GetString(text);
                return s == null
                    ? throw new CompileException(CompileExceptionType.InvalidEscapeSequence, expression)
                    : new Value(s, encodingType);
            }
            default:
                return expression.Expr.Type switch
                {
                    TokenType.False => new Value(false),
                    TokenType.True => new Value(true),
                    TokenType.AltBinLiteral => ValueHelper.ParseAltBinary(text[1..]) ??
                                            throw new CompileException(CompileExceptionType
                                                .InvalidStringLiteral, expression),
                    TokenType.IntLiteral => ValueHelper.ParseInt(text) ??
                                            throw new CompileException(CompileExceptionType
                                                .InvalidIntLiteral, expression),
                    TokenType.FloatLiteral => ValueHelper.ParseFloat(text) ??
                                              throw new CompileException(CompileExceptionType
                                                  .InvalidFloatLiteral, expression),
                    _ => null
                };
        }
    }

    public Value? VisitAnonymousRefExpression(AnonymousRefExpression expression)
        => null;

    public Value? VisitBinaryOpExpression(BinaryOpExpression expression)
    {
        var left = Visit(expression.Left);
        var right = Visit(expression.Right);
        if (expression.Operator.Type != TokenType.InterpolationStart)
        {
            return EvalValues.BinaryOp(left, right, expression, null);
        }
        var interpol = ValueHelper.GetString(expression.Operator.Text.ToString());
        return interpol == null 
            ? throw new CompileException(CompileExceptionType.InvalidStringLiteral, expression.Operator) 
            : EvalValues.ConcatStrings(left, interpol, right, null);
    }

    public Value? VisitTernaryExpression(TernaryExpression expression) => null;

    public Value? VisitUnaryOpExpression(UnaryOpExpression expression) 
        => EvalValues.UnaryOp(Visit(expression.Expr), expression, null);

    public Value? VisitSubscriptExpression(SubscriptExpression expression)
    {
        var target = Visit(expression.Left);
        var rangeStartValue = expression.Index.Start != null 
            ? Visit(expression.Index.Start) 
            : null;
        var rangeEndValue = expression.Index.End != null
            ? Visit(expression.Index.End)
            : null;
        return EvalValues.Subscript
        (
            target, 
            rangeStartValue, 
            rangeEndValue, 
            expression, 
            null,
            false
        );
    }

    public Value? VisitCallExpression(CallExpression expression) => null;
    
    public Value? VisitMemberExpression(MemberExpression expression) => null;
    
    public Value? VisitFunctionExpression(FunctionExpression expression) => null;
    
    public Value? VisitArrayInitExpression(ArrayInitExpression expression)
    {
        List<Value?> array = [];
        array.AddRange(expression.Expressions.Select(Visit));
        return EvalValues.ArrayInit(array, expression, true);
    }

    public Value? VisitDictionaryInitExpression(DictionaryInitExpression expression)
    {
        var keyValuePair = new List<(Value?, Value?)>();
        for (var i = 0; i < expression.Members.Count; i++)
        {
            var kvp = expression.Members[i];
            keyValuePair.Add((Visit(kvp.Key), Visit(kvp.Value)));
        }
        return EvalValues.DictionaryInit(keyValuePair, expression, true);
    }

    public Value? VisitInterpolationExpression(InterpolationExpression expression) => null;

    public Value? Visit(Expression expression) =>
        expression.Value.IsDefined 
            ? expression.Value 
            : expression.Accept(this);
}
