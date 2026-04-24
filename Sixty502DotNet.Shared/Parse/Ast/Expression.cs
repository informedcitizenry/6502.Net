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

using Sixty502DotNet.Shared.Eval;
using Sixty502DotNet.Shared.Lex;
using System.Text;

namespace Sixty502DotNet.Shared.Parse.Ast;

public abstract class Expression : Ast
{
    public abstract bool IsConstant();

    public abstract bool IsLValue();
    
    public Value Value { get; set; } = new Value();

    public abstract Value? Accept(IExpressionVisitor visitor);
    
    public bool Grouped { get; set; }
}

public sealed class PrimaryExpression : Expression
{
    public PrimaryExpression(Token token)
    {
        Expr = token;
        LeftToken = token;
        RightToken = token;
    }

    public override bool IsConstant()
    {
        return LeftToken.Type is 
            TokenType.False or
            TokenType.IntLiteral or 
            TokenType.FloatLiteral or 
            TokenType.CharLiteral or 
            TokenType.InterpolationEnd or
            TokenType.InterpolationStart or 
            TokenType.StringLiteral or 
            TokenType.True;
    }

    public override bool IsLValue() 
        => LeftToken.Type.IsIdent() || LeftToken.Type == TokenType.Star;

    public override Value? Accept(IExpressionVisitor visitor)
        => visitor.VisitPrimaryExpression(this);
    
    public Token Expr { get; }

    public override string ToString() => Expr.Text.ToString();
}

public sealed class GroupedExpression : Expression
{
    public GroupedExpression(Token openParen, Expression inner, Token closeParen)
    {
        Inner = inner;
        LeftToken = openParen;
        RightToken = closeParen;
    }

    public override bool IsConstant() => Inner.IsConstant();

    public override bool IsLValue() => Inner.IsLValue();

    public Expression Inner { get; }

    public override Value? Accept(IExpressionVisitor visitor)
        => Inner.Accept(visitor);
}

public sealed class AnonymousRefExpression : Expression
{
    public AnonymousRefExpression(Token leftToken, Token rightToken, int places)
    {
        LeftToken = leftToken;
        RightToken = rightToken;
        Type = leftToken.Type == TokenType.Plus ? '+' : '-';
        Places = places;
    }

    public override bool IsConstant() => false;
    
    public override bool IsLValue() => false;

    public override string ToString() => new (Type, Places);

    public override Value? Accept(IExpressionVisitor visitor)
        => visitor.VisitAnonymousRefExpression(this);
    
    public char Type { get; }
    
    public int Places { get; }
}

public sealed class BinaryOpExpression : Expression
{
    public BinaryOpExpression(Expression left, Token op, Expression right)
    {
        LeftToken = left.LeftToken;
        RightToken = right.RightToken;
        Left =  left;
        Right = right;
        Operator = op;
    }

    public override bool IsConstant() => Left.IsConstant() && Right.IsConstant();
    
    public override bool IsLValue() => false;

    public override Value? Accept(IExpressionVisitor visitor)
        => visitor.VisitBinaryOpExpression(this);

    public override string ToString() => $"({Operator.Text} {Left} {Right})";

    public Expression Left { get; }
    
    public Expression Right { get; }
    
    public Token Operator { get; }
}

public sealed class TernaryExpression : Expression
{
    public TernaryExpression(Expression condition, Expression then, Expression @else)
    {
        LeftToken = condition.LeftToken;
        RightToken = @else.RightToken;
        Condition = condition;
        Then = then;
        Else = @else;
    }

    public override bool IsLValue() => false;

    public override bool IsConstant() 
        => Condition.IsConstant() && Then.IsConstant() && Else.IsConstant();

    public override Value? Accept(IExpressionVisitor visitor)
        => visitor.VisitTernaryExpression(this);

    public override string ToString() => $"(? {Condition} (: {Then} {Else}))";

    public Expression Condition { get; }
    
    public Expression Then { get; }
    
    public Expression Else { get; }
}

public sealed class UnaryOpExpression : Expression
{
    public UnaryOpExpression(Token op, Expression expr, bool isPostFix)
    {
        LeftToken = isPostFix ? expr.LeftToken : op;
        RightToken = isPostFix ? op : expr.RightToken;
        Expr = expr;
        Operator = op;
        IsPostfix = isPostFix;
    }

    public override bool IsLValue() => false;

    public override bool IsConstant() => Expr.IsConstant();

    public override Value? Accept(IExpressionVisitor visitor)
        => visitor.VisitUnaryOpExpression(this);
    
    public Token Operator { get; }

    public override string ToString() 
        => IsPostfix ? $"({Expr} {Operator.Text})" : $"({Operator.Text} {Expr})";

    public Expression Expr { get; }
    
    public bool IsPostfix { get; }
}

public sealed class ArrayInitExpression : Expression
{
    public ArrayInitExpression(bool isTuple, Token openBracket)
    {
        LeftToken = openBracket;
        Expressions = new List<Expression>();
        IsTuple = isTuple;
    }

    public ArrayInitExpression(bool isTuple, Token openBracket, List<Expression> expressions)
    {
        LeftToken = openBracket;
        Expressions = expressions;
        IsTuple = isTuple;
    }
    
    public override bool IsConstant() 
        => Expressions.All(t => t.IsConstant());

    public override bool IsLValue() 
        => IsTuple && Expressions.All(t => t.IsLValue());

    public override Value? Accept(IExpressionVisitor visitor)
        => visitor.VisitArrayInitExpression(this);

    public override string ToString()
    {
        var sb = new StringBuilder(IsTuple ? "(" : "[");
        for (var i = 0; i < Expressions.Count; i++)
        {
            if (i > 0) sb.Append(' ');
            sb.Append(Expressions[i]);
        }

        sb.Append(IsTuple ? ")" : "]");
        return sb.ToString();
    }

    public bool IsTuple { get; }

    public IList<Expression> Expressions { get; }
}

public sealed class DictionaryInitExpression : Expression
{
    public DictionaryInitExpression(Expression firstKey, Expression firstValue)
    {
        LeftToken = firstKey.LeftToken;
        Members = (List<KeyValuePair>)[new KeyValuePair(firstKey, firstValue)];
    }

    public override bool IsConstant()
    {
        for (var i = 0; i < Members.Count; i++)
        {
            if (!Members[i].Key.IsConstant() || !Members[i].Value.IsConstant())
            {
                return false;
            }
        }
        return true;
    }
    
    public override bool IsLValue() => false;

    public override Value? Accept(IExpressionVisitor visitor)
        => visitor.VisitDictionaryInitExpression(this);

    public override string ToString()
    {
        var sb = new StringBuilder("{");
        for (var i = 0; i < Members.Count; i++)
        {
            if (i > 0) sb.Append(' ');
            sb.Append($"(: {Members[i].Key} {Members[i].Value})");
        }
        sb.Append('}');
        return sb.ToString();
    }
    
    public IList<KeyValuePair> Members { get; }
}

public sealed class SubscriptExpression : Expression
{
    public SubscriptExpression(Expression left, Range index)
    {
        LeftToken = left.LeftToken;
        Left = left;
        Index =  index;
    }

    public override bool IsLValue() => Left.IsLValue();

    public override bool IsConstant()
    {
        if (!Left.IsConstant())
        {
            return false;
        }
        return (Index.Start == null || Index.Start.IsConstant()) &&
               (Index.End == null || Index.End.IsConstant());
    }

    public override Value? Accept(IExpressionVisitor visitor)
        => visitor.VisitSubscriptExpression(this);

    public override string ToString()
    {
        return Index.Type switch
        {
            RangeType.IsIndex when Index.Start != null => $"([] {Left} {Index.Start})",
            RangeType.IsRange => Index.Start != null
                ? Index.End != null ? $"([] {Left} (.. {Index.Start} {Index.End}))" : $"([] {Left} (.. {Index.Start}))"
                : $"([] {Left} (.. {Index.End}))",
            RangeType.IsRangeIncludesEnd => Index.Start != null
                ? $"([] {Left} (..^ {Index.Start} {Index.End}))"
                : $"([] {Left} (..^ {Index.End}))",
            _ => throw new ArgumentOutOfRangeException()
        };
    }
    
    public Expression Left { get; }
    
    public Range Index { get; }
}

public sealed class MemberExpression : Expression
{
    public MemberExpression(Expression left, Token right)
    {
        LeftToken = left.LeftToken;
        RightToken = right;
        Left = left;
    }

    public override bool IsConstant() => false;

    public override bool IsLValue() => Left.IsLValue();

    public override Value? Accept(IExpressionVisitor visitor)
        => visitor.VisitMemberExpression(this);

    public override string ToString() => $"(. {Left} {Member.Text})";
    
    public Expression Left { get; }

    public Token Member => RightToken;
}

public sealed class CallExpression : Expression
{
    public CallExpression(Expression callee)
    {
        Callee = callee;
        LeftToken = callee.LeftToken;
        RightToken = callee.RightToken;
        Arguments = new List<Expression>();
    }

    public override bool IsConstant() => false;
    
    public override bool IsLValue() => false;

    public override Value? Accept(IExpressionVisitor visitor)
        => visitor.VisitCallExpression(this);
    
    public Expression Callee { get; }

    public override string ToString()
    {
        var sb = new StringBuilder("(");
        if (Callee is MemberExpression)
        {
            sb.Append("method");
        }
        sb.Append($"() {Callee} (");
        for (var i = 0; i < Arguments.Count; i++)
        {
            if (i < Arguments.Count - 1) sb.Append(' ');
            sb.Append(Arguments[i]);
        }
        sb.Append("))");
        return sb.ToString();
    }

    public IList<Expression> Arguments { get; }
}

public sealed class FunctionExpression : Expression
{
    public FunctionExpression
    (
        IList<PrimaryExpression> parameters, 
        IList<Expression> defaultValues, 
        Expression simpleExpr
    )
    {
        Parameters = parameters;
        DefaultValues = defaultValues;
        SimpleExpr = simpleExpr;
    }

    public FunctionExpression
    (
        IList<PrimaryExpression> parameters, 
        IList<Expression> defaultValues, 
        IList<Statement> body
    )
    {
        Parameters = parameters;
        DefaultValues = defaultValues;
        Body = body;
    }
    
    public override bool IsConstant() => false;
    
    public override bool IsLValue() => false;

    public override Value? Accept(IExpressionVisitor visitor)
        => visitor.VisitFunctionExpression(this);

    public override string ToString() => "function()";
    
    public IList<PrimaryExpression> Parameters { get; }
    
    public IList<Expression> DefaultValues { get; }

    public Expression? SimpleExpr { get; }
    
    public IList<Statement> Body { get; } = new  List<Statement>();
}

public sealed class InterpolationExpression : Expression
{
    public InterpolationExpression(Expression expr, Expression? width, string? format)
    {
        LeftToken = expr.LeftToken;
        Expr = expr;
        Width = width;
        FormatString = format;
    }
    
    public override bool IsConstant() => false;

    public override bool IsLValue() => false;

    public override Value? Accept(IExpressionVisitor visitor)
        => visitor.VisitInterpolationExpression(this);
    
    public override string ToString() => Expr.ToString() ?? string.Empty;

    public Expression Expr { get; }
    
    public Expression? Width { get; }

    public string? FormatString { get; }
}

public sealed class KeyValuePair : Ast
{
    public KeyValuePair(Expression key, Expression value)
    {
        LeftToken = key.LeftToken;
        RightToken = value.RightToken;
        Key = key;
        Value = value;
    }
    
    public Expression Key { get; }
    
    public Expression Value { get; }
}