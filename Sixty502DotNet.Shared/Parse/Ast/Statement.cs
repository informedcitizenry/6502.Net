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

using Sixty502DotNet.Shared.Lex;
using System.Text;

namespace Sixty502DotNet.Shared.Parse.Ast;

public abstract class Statement : Ast
{
    public abstract T Accept<T>(IStatementVisitor<T> visitor);
}

public sealed class Define : Ast
{
    public Define(Token left, Expression? value)
    {
        LeftToken = left;
        Value = value;
    }
    
    public Token Label =>  LeftToken;
    
    public Expression? Value { get; }
}

public sealed class EofStatement : Statement
{
    public override T Accept<T>(IStatementVisitor<T> visitor) 
        => visitor.VisitEofStatement(this);
}

public sealed class ConstantAssignStatement : Statement
{
    public ConstantAssignStatement(Expression constSymbol, Token op, Expression right)
    {
        ConstSymbol = constSymbol;
        Operator = op;
        LeftToken = constSymbol.LeftToken;
        RightToken = right.RightToken;
        Value = right;
    }

    public override T Accept<T>(IStatementVisitor<T> visitor)
        => visitor.VisitConstantAssignStatement(this);

    public override string ToString() => $"{ConstSymbol} {Operator.Text} {Value}";

    public Token Operator { get; }
    
    public Expression ConstSymbol { get; }

    public Expression Value { get; }
}

public sealed class VarAssignmentStatement : Statement
{
    public VarAssignmentStatement(Expression left, Token op, Expression right)
    {
        LeftToken = left.LeftToken;
        RightToken = right.RightToken;
        Left = left;
        Operator = op;
        Right = right;
    }
    
    public override T Accept<T>(IStatementVisitor<T> visitor)
        => visitor.VisitVarAssignmentStatement(this);

    public override string ToString() => $"{Left} {Operator.Text} {Right}";

    public Expression Left { get; }
    
    public Token Operator { get; }
    
    public Expression Right { get; }
}

public sealed class LabelStatement : Statement
{
    public LabelStatement(Token token, bool beginsStatement)
    {
        LeftToken = token;
        RightToken = token;
        BeginsStatement = beginsStatement;
    }
    
    public Token Label => LeftToken;
    
    public override T Accept<T>(IStatementVisitor<T> visitor)
        => visitor.VisitLabelStatement(this);

    public override string ToString() => $"{Label.Text}:";
    
    public bool BeginsStatement { get; }
}

public class CpuInstructionStatement : Statement
{
    public CpuInstructionStatement(Token mnemonic, Operand operand)
    {
        LeftToken = mnemonic;
        RightToken = operand.RightToken;
        Operand = operand;
    }
    
    public override T Accept<T>(IStatementVisitor<T> visitor)
        => visitor.VisitCpuInstructionStatement(this);

    public override string ToString() 
        => Operand.Type == OperandType.Implied 
            ? Mnemonic.Type.Stringified()
            : $"{Mnemonic.Type.Stringified()} {Operand}";

    public Token Mnemonic => LeftToken;
    
    public Operand Operand { get; }
}

public sealed class I86RepInstructionStatement : Statement
{
    public I86RepInstructionStatement(Token mnemonic, CpuInstructionStatement repetition)
    {
        LeftToken = mnemonic;
        RepOpcode = new CpuInstructionStatement
        (
            mnemonic, 
            new Operand(OperandType.Implied, mnemonic)
            {
                RightToken = repetition.RightToken
            }
        );
        RightToken = repetition.RightToken;
        Repetition = repetition;
    }

    public CpuInstructionStatement RepOpcode { get; }
    
    public CpuInstructionStatement Repetition { get; }
    
    public override T Accept<T>(IStatementVisitor<T> visitor)
    {
        return visitor.VisitI86RepInstructionStatement(this);
    }
}

public sealed class PseudoOpStatement : Statement
{
    public PseudoOpStatement(Token directive)
    {
        LeftToken = directive;
    }
    
    public override T Accept<T>(IStatementVisitor<T> visitor)
        => visitor.VisitPseudoOpStatement(this);

    public override string ToString()
    {
        var sb = new StringBuilder($"{PseudoOp.Type.Stringified()} ");
        for (var i = 0; i < Expressions.Count; i++)
        {
            if (i > 0) sb.Append(", ");
            sb.Append(Expressions[i]?.ToString() ?? "?");
        }
        return sb.ToString();
    }

    public Token PseudoOp => LeftToken;
    
    public IList<Expression?> Expressions { get; } = new List<Expression?>();
}

public sealed class SimpleDirectiveStatement : Statement
{
    public SimpleDirectiveStatement(Token directive)
    {
        LeftToken = directive;
        RightToken = directive;
    }
    
    public override T Accept<T>(IStatementVisitor<T> visitor)
        => visitor.VisitSimpleDirectiveStatement(this);
    
    public override string ToString() => Directive.Type.Stringified();
    
    public Token Directive => LeftToken;
}

public sealed class SingleExpressionDirectiveStatement : Statement
{
    public SingleExpressionDirectiveStatement(Token directive, Expression expression)
    {
        LeftToken = directive;
        
        Expression = expression;
    }
    
    public override T Accept<T>(IStatementVisitor<T> visitor)
        => visitor.VisitSingleExpressionDirectiveStatement(this);
    
    public override string ToString() => $"{Directive.Type.Stringified()} {Expression}";
    
    public Token Directive => LeftToken;
    
    public Expression Expression { get; }
}

public sealed class MultiExpressionDirectiveStatement : Statement
{
    public MultiExpressionDirectiveStatement(Token directive)
    {
        LeftToken = directive;    
    }
    
    public override T Accept<T>(IStatementVisitor<T> visitor)
        => visitor.VisitMultiExpressionDirectiveStatement(this);

    public override string ToString()
    {
        var sb = new StringBuilder($"{Directive.Type.Stringified()} ");
        for (var i = 0; i < Expressions.Count; i++)
        {
            if (i > 0) sb.Append(", ");
            sb.Append(Expressions[i]);
        }

        return sb.ToString();
    }
    
    public Token Directive => LeftToken;
    
    public IList<Expression> Expressions { get; init; } = new List<Expression>();
}

public sealed class BlockStatement : Statement
{
    public IList<Statement> Statements { get; } = new List<Statement>();

    public override T Accept<T>(IStatementVisitor<T> visitor)
        => visitor.VisitModule(this);

    public override string ToString() => "{ ... }";
}

public sealed class LabeledBlockStatement : Statement
{
    public LabeledBlockStatement(Token label, Token directive)
    {
        LeftToken = label;
        Directive = directive;
    }
    
    public override T Accept<T>(IStatementVisitor<T> visitor)
        => visitor.VisitLabeledBlockStatement(this);

    public override string ToString() => $"{Label.Text} {Directive.Type.Stringified()} {{ ... }}";

    public Token Directive { get; }
    
    public Token Label => LeftToken;
    
    public IList<Statement> Statements { get; init; } = new List<Statement>();
}

public sealed class NamespaceBlockStatement : Statement
{
    public override T Accept<T>(IStatementVisitor<T> visitor)
        => visitor.VisitNamespaceBlockStatement(this);
    
    public override string ToString() => $"`.namespace` {Namespace.Text} {{ ... }}";
    
    public Token Namespace { get; init; }
    
    public IList<Statement> Statements { get; init; } = new List<Statement>();
}

public sealed class EnumDeclaration : Statement
{
    public Token Enum { get; init; }
    
    public IList<Enumerator> Enumerators { get; } = new List<Enumerator>();

    public override T Accept<T>(IStatementVisitor<T> visitor)
        => visitor.VisitEnumDeclaration(this);

    public override string ToString()
    {
        var sb = new StringBuilder($"{Enum.Text} `.enum` {{");
        for (var i = 0; i < Enumerators.Count; i++)
        {
            if (i > 0) sb.Append(", ");
            sb.Append(Enumerators[i].Name.Text);
            if (Enumerators[i].DefaultValue != null)
            {
                sb.Append($"={Enumerators[i].DefaultValue}");
            }
        }
        sb.Append('}');
        return sb.ToString();
    }
}

public sealed class PageBlockStatement : Statement
{
    public override T Accept<T>(IStatementVisitor<T> visitor)
        => visitor.VisitPageBlockStatement(this);

    public override string ToString() => "`.page` { ... }";
    
    public IList<Statement> Statements { get; init; } = new List<Statement>();
}

public sealed class AnonymousBlockStatement : Statement
{
    public AnonymousBlockStatement(Token directive)
    {
        LeftToken = directive;    
    }
    
    public override T Accept<T>(IStatementVisitor<T> visitor)
        => visitor.VisitAnonymousBlockStatement(this);
    
    public override  string ToString() => $"{Directive.Type.Stringified()} {{ ... }}";
    
    public Token Directive => LeftToken;
    
    public IList<Statement> Statements { get; init; } = new List<Statement>();
}

public sealed class ExpressionBlockStatement : Statement
{
    public ExpressionBlockStatement(Token directive, Expression expression)
    {
        LeftToken = directive;
        
        Expression = expression;
    }

    public override T Accept<T>(IStatementVisitor<T> visitor)
        => visitor.VisitExpressionBlockStatement(this);

    public override string ToString() =>
        Directive.Type == TokenType.DoKw 
            ? $"`.do` {{ ... }} `.whiletrue` {Expression}" 
            : $"{Directive.Type.Stringified()} {Expression} {{ ... }}";

    public Token Directive => LeftToken;
    
    public Expression Expression { get; }
    
    public IList<Statement> Block { get; init; } = new List<Statement>();
}

public sealed class ForStatement : Statement
{
    public ForStatement
        (
            Token forDirective, 
            VarAssignmentStatement? init, 
            Expression? condition, 
            IList<Statement> iterators
        )
    {
        LeftToken = forDirective;
        Init = init;
        Condition = condition;
        Iterators = iterators;
    }

    public override T Accept<T>(IStatementVisitor<T> visitor)
        => visitor.VisitForStatement(this);

    public override string ToString()
    {
        var sb = new StringBuilder("`.for` ");
        if (Init != null)
        {
            sb.Append(Init);
        }
        sb.Append(", ");
        if (Condition != null)
        {
            sb.Append(Condition);
        }
        for (var i = 0; i < Iterators.Count; i++)
        {
            sb.Append($", {Iterators[i]}");
        }
        sb.Append("{ ... }");
        return sb.ToString();
    }

    public VarAssignmentStatement? Init { get; }
    
    public Expression? Condition { get; }
    
    public IList<Statement> Iterators { get; }
    
    public IList<Statement> Block { get; init; } = new List<Statement>();
}

public sealed class ForeachStatement : Statement
{
    public ForeachStatement
    (
        Token forDirective,
        Token enumerator,
        Expression enumerable
    )
    {
        LeftToken = forDirective;
        Enumerator = enumerator;
        Enumerable = enumerable;
    }

    public override T Accept<T>(IStatementVisitor<T> visitor)
        => visitor.VisitForeachStatement(this);
    
    public Token Enumerator { get; }
    
    public Expression Enumerable { get; }
    
    public IList<Statement> Block { get; init; } = new List<Statement>();
}

public sealed class FunctionDefinitionStatement : Statement
{
    public FunctionDefinitionStatement
    (
        Token name, 
        IList<PrimaryExpression> parameters,
        IList<Expression> defaultValues
    )
    {
        LeftToken = name;
        Parameters = parameters;
        DefaultValues = defaultValues;
    }

    public override T Accept<T>(IStatementVisitor<T> visitor)
        => visitor.VisitFunctionDefinitionStatement(this);

    public override string ToString()
    {
        var sb = new StringBuilder($"{Name.Text.ToString()} `.function` ");
        for (var i = 0; i < Parameters.Count; i++)
        {
            if (i > 0) sb.Append(", ");
            sb.Append(Parameters[i]);
        }
        sb.Append("{ ... }");
        return sb.ToString();
    }
    
    public Token Name => LeftToken;
    
    public IList<PrimaryExpression> Parameters { get; }
    
    public IList<Expression> DefaultValues { get; }
    
    public IList<Statement> Body { get; init; } = new List<Statement>();
}

public sealed class IfStatement : Statement
{
    public IfStatement(IfBlock ifBlock)
    {
        LeftToken = ifBlock.LeftToken;
        IfBlock = ifBlock;
    }
    
    public override T Accept<T>(IStatementVisitor<T> visitor)
        => visitor.VisitIfStatement(this);

    public override string ToString() 
        => $"{IfBlock.IfDirective.Type.Stringified()} {IfBlock.Condition} {{ ... }}";
    
    public IfBlock IfBlock { get; }
    
    public IList<IfBlock> ElseIfBlocks { get; init; } = new List<IfBlock>();
    
    public IList<Statement> ElseBlock { get; init; } = new List<Statement>();
}

public sealed class SwitchStatement : Statement
{
    public SwitchStatement(Token directive, Expression condition)
    {
        LeftToken = directive;
        Condition = condition;
    }

    public override T Accept<T>(IStatementVisitor<T> visitor)
        => visitor.VisitSwitchStatement(this);

    public override string ToString() => $"`.switch` {Condition} {{ ... }}";

    public Token Directive => LeftToken;
    
    public Expression Condition { get; }
    
    public IList<SwitchCaseBlock> Cases { get; init; } = new List<SwitchCaseBlock>();
}

public sealed class IfBlock : Ast
{
    public IfBlock(Token ifDirective, Expression condition)
    {
        LeftToken = ifDirective;
        Condition = condition;
    }

    public Token IfDirective => LeftToken;
    
    public Expression Condition { get; }
    
    public IList<Statement> Block { get; init; } = new List<Statement>();
}

public sealed class SwitchCaseBlock : Ast
{
    public IList<Expression> Cases { get; init; } = new List<Expression>();
    
    public IList<Statement> Block { get; init; } = new List<Statement>();
    
    public bool IsDefault { get; init; }
}

public sealed class Enumerator : Ast
{
    public Token Name { get; init; }
    
    public Expression? DefaultValue { get; init; }
}
