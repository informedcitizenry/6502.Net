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

using Sixty502DotNet.Shared.Compile;
using Sixty502DotNet.Shared.Eval.Scope;
using Sixty502DotNet.Shared.Lex;
using Sixty502DotNet.Shared.Parse.Ast;

namespace Sixty502DotNet.Shared.Eval.Function;


public class PurityChecker(AssemblyState assemblyState) : IStatementVisitor<bool>
{
    private readonly Compile.Environment _variables = new
    (
        EnvironmentType.Func, 
        null, 
        null, 
        assemblyState.Comparer
    );
    
    public bool VisitConstantAssignStatement(ConstantAssignStatement statement)
        => !statement.ConstSymbol.LeftToken.Text.ToString().Equals("*") &&
           !VariableExistsInOuterScope(statement.ConstSymbol) &&
           !VariableExistsInOuterScope(statement.Value);

    public bool VisitVarAssignmentStatement(VarAssignmentStatement statement)
    {
        if (statement.Left.LeftToken.Text.ToString().Equals("*") ||
            VariableExistsInOuterScope(statement.Left))
        {
            return false;
        }
        if (statement.Operator.Type == TokenType.ColonEq)
        {
            TrackVariable(statement.Left);
        }
        else if (!IsDefinedInFunction(statement.Left))
        {
            return false;
        }
        return !VariableExistsInOuterScope(statement.Right);
    }

    private void TrackVariable(Expression variable)
    {
        if (variable is not ArrayInitExpression { IsTuple: true } tuple)
        {
            _variables.DefineOrUpdateVariable(variable.LeftToken, new Value());
            return;
        }
        for (var i = 0; i < tuple.Expressions.Count; i++)
        {
            TrackVariable(tuple.Expressions[i]);
        }
    }

    private bool IsDefinedInFunction(Expression variable)
    {
        return variable is not ArrayInitExpression { IsTuple: true } tuple 
            ? _variables.SymbolIsVariable(variable.LeftToken.Text.ToString()) 
            : tuple.Expressions.All(IsDefinedInFunction);
    }

    private bool VariableExistsInOuterScope(Expression expr)
    {
        if (expr is not MemberExpression memberExpression)
            return expr switch
            {
                ArrayInitExpression { IsTuple: true } arrayInitExpression
                    => arrayInitExpression.Expressions.Any(VariableExistsInOuterScope),
                SubscriptExpression subscriptExpression
                    => VariableExistsInOuterScope(subscriptExpression.Left),
                PrimaryExpression primary => VariableExistsInOuterScope(primary.Expr),
                _ => false
            };
        var eval = new Evaluator(assemblyState);
        var env = eval.Visit(memberExpression.Left)?.AsResolver() switch
        {
            Namespace ns => ns.Env,
            ScopeLabel sl => sl.Env,
            _ => null
        };
        return env?.SymbolIsVariable(memberExpression.Member.Text.ToString()) == true;
    }
    
    private bool VariableExistsInOuterScope(Token sym) 
        => assemblyState.SymbolTable.SymbolIsVariable(sym.Text.ToString());

    public bool VisitCpuInstructionStatement(CpuInstructionStatement statement) => false;

    public bool VisitI86RepInstructionStatement(I86RepInstructionStatement statement) => false;

    public bool VisitSimpleDirectiveStatement(SimpleDirectiveStatement statement)
        => statement.Directive.Type is not TokenType.EndrelocateKw and TokenType.RealPcKw;

    public bool VisitMultiExpressionDirectiveStatement(MultiExpressionDirectiveStatement statement)
        => statement.Directive.Type is not (TokenType.AlignKw or TokenType.BinaryKw 
            or TokenType.FillKw or TokenType.StringifyKw);

    public bool VisitSingleExpressionDirectiveStatement(SingleExpressionDirectiveStatement statement) 
        => statement.Directive.Type is not (TokenType.AlignKw or TokenType.BinaryKw or TokenType.FillKw or TokenType.OrgKw
            or TokenType.PseudoPcKw or TokenType.RelocateKw or TokenType.SectionKw or TokenType.StringifyKw);

    public bool VisitPseudoOpStatement(PseudoOpStatement statement) => false;

    public bool VisitLabelStatement(LabelStatement statement) => false;

    public bool VisitLabeledBlockStatement(LabeledBlockStatement statement) 
        => CheckBlock(statement.Statements);

    public bool VisitNamespaceBlockStatement(NamespaceBlockStatement statement)
        => CheckBlock(statement.Statements);

    public bool VisitEnumDeclaration(EnumDeclaration statement) => true;

    public bool VisitPageBlockStatement(PageBlockStatement statement)
        => CheckBlock(statement.Statements);

    public bool VisitAnonymousBlockStatement(AnonymousBlockStatement statement)
        => CheckBlock(statement.Statements);

    public bool VisitFunctionDefinitionStatement(FunctionDefinitionStatement statement) => true;

    public bool VisitIfStatement(IfStatement statement)
    {
        if (CheckIfBlock(statement.IfBlock) &&
            CheckBlock(statement.ElseBlock))
        {
            return statement.ElseIfBlocks.All(CheckIfBlock);
        }
        return false;
    }

    private bool CheckIfBlock(IfBlock block) 
        => !VariableExistsInOuterScope(block.Condition) && CheckBlock(block.Block);

    public bool VisitForStatement(ForStatement statement)
    {
        if (statement.Init != null && VariableExistsInOuterScope(statement.Init.Left))
        {
            return false;
        }
        return statement.Iterators.All(Visit) && CheckBlock(statement.Block);
    }

    public bool VisitForeachStatement(ForeachStatement statement) 
        => !VariableExistsInOuterScope(statement.Enumerable) && CheckBlock(statement.Block);

    public bool VisitSwitchStatement(SwitchStatement statement) 
        => !VariableExistsInOuterScope(statement.Condition) &&
           statement.Cases.All(t => CheckBlock(t.Block));

    public bool VisitExpressionBlockStatement(ExpressionBlockStatement statement)
        => !VariableExistsInOuterScope(statement.Expression) &&
           CheckBlock(statement.Block);

    public bool VisitModule(BlockStatement statement)
        => CheckBlock(statement.Statements);

    public bool VisitEofStatement(EofStatement statement) => true;
    
    public bool Visit(Statement statement) => statement.Accept(this);

    public bool CheckBlock(IList<Statement> block) => block.All(Visit);
}