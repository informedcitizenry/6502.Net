using Sixty502DotNet.Shared.Parse.Ast;

namespace Sixty502DotNet.Shared.Eval.Function;

public class RecursiveStatementChecker(AssemblyState assemblyState, UserFunction function)
    : IStatementVisitor<bool>
{
    private readonly RecursiveExpressionChecker _expressionChecker = new(assemblyState, function);

    public bool VisitConstantAssignStatement(ConstantAssignStatement statement)
        => _expressionChecker.Visit(statement.Value);

    public bool VisitVarAssignmentStatement(VarAssignmentStatement statement)
        => _expressionChecker.Visit(statement.Right);

    public bool VisitCpuInstructionStatement(CpuInstructionStatement statement) => false;

    public bool VisitI86RepInstructionStatement(I86RepInstructionStatement statement) => false;

    public bool VisitSimpleDirectiveStatement(SimpleDirectiveStatement statement) => false;

    public bool VisitMultiExpressionDirectiveStatement(MultiExpressionDirectiveStatement statement) 
        => statement.Expressions.Any(t => _expressionChecker.Visit(t));

    public bool VisitSingleExpressionDirectiveStatement(SingleExpressionDirectiveStatement statement)
        => _expressionChecker.Visit(statement.Expression);

    public bool VisitPseudoOpStatement(PseudoOpStatement statement) => false;

    public bool VisitLabelStatement(LabelStatement statement) => false;

    public bool VisitLabeledBlockStatement(LabeledBlockStatement statement)
        => CheckBlock(statement.Statements);

    public bool VisitNamespaceBlockStatement(NamespaceBlockStatement statement)
        => CheckBlock(statement.Statements);

    public bool VisitEnumDeclaration(EnumDeclaration statement) => false;

    public bool VisitPageBlockStatement(PageBlockStatement statement)
        => CheckBlock(statement.Statements);

    public bool VisitAnonymousBlockStatement(AnonymousBlockStatement statement)
        => CheckBlock(statement.Statements);

    public bool VisitFunctionDefinitionStatement(FunctionDefinitionStatement statement)
        => CheckBlock(statement.Body);

    public bool VisitIfStatement(IfStatement statement)
    {
        if (CheckIfBlock(statement.IfBlock) ||
            statement.ElseIfBlocks.Any(CheckIfBlock)) return true;
        return CheckBlock(statement.ElseBlock);
    }

    public bool VisitForStatement(ForStatement statement)
    {
        if (statement.Init != null && _expressionChecker.Visit(statement.Init.Right) ||
            statement.Condition != null && _expressionChecker.Visit(statement.Condition) ||
            CheckBlock(statement.Block))
        {
            return true;
        }
        return statement.Iterators.Any(Visit);
    }

    public bool VisitForeachStatement(ForeachStatement statement)
        => _expressionChecker.Visit(statement.Enumerable) || CheckBlock(statement.Block);

    public bool VisitSwitchStatement(SwitchStatement statement)
    {
        if (_expressionChecker.Visit(statement.Condition))
        {
            return true;
        }
        return statement.Cases.Any(caseBlock => caseBlock.Cases.Any(t => _expressionChecker.Visit(t)) ||
                                                CheckBlock(caseBlock.Block));
    }

    public bool VisitExpressionBlockStatement(ExpressionBlockStatement statement)
        => _expressionChecker.Visit(statement.Expression) || CheckBlock(statement.Block);

    public bool VisitModule(BlockStatement statement)
        => CheckBlock(statement.Statements);
    
    public bool VisitEofStatement(EofStatement statement) => false;

    public bool Visit(Statement statement)
        => statement.Accept(this);

    private bool CheckIfBlock(IfBlock block)
        => _expressionChecker.Visit(block.Condition) || CheckBlock(block.Block);
    
    public bool CheckBlock(IList<Statement> statements) => statements.Any(Visit);
}

public class RecursiveExpressionChecker(AssemblyState assemblyState, UserFunction function) : IExpressionVisitor<bool>
{
    public bool VisitPrimaryExpression(PrimaryExpression expression) => false;

    public bool VisitAnonymousRefExpression(AnonymousRefExpression expression) => false;

    public bool VisitBinaryOpExpression(BinaryOpExpression expression) 
        => Visit(expression.Left) || Visit(expression.Right);

    public bool VisitTernaryExpression(TernaryExpression expression)
        => Visit(expression.Condition) || Visit(expression.Then) || Visit(expression.Else);

    public bool VisitUnaryOpExpression(UnaryOpExpression expression) => Visit(expression.Expr);

    public bool VisitSubscriptExpression(SubscriptExpression expression)
        => Visit(expression.Left);

    public bool VisitCallExpression(CallExpression expression)
    {
        var eval = new Evaluator(assemblyState);
        var callee = eval.Visit(expression.Callee);
        return callee?.AsFunction() != null && ReferenceEquals(callee.AsFunction(), function);
    }

    public bool VisitMemberExpression(MemberExpression expression) => Visit(expression.Left);

    public bool VisitArrayInitExpression(ArrayInitExpression expression) 
        => expression.Expressions.Any(Visit);

    public bool VisitDictionaryInitExpression(DictionaryInitExpression expression) 
        => expression.Members.Any(t => Visit(t.Value));

    public bool VisitFunctionExpression(FunctionExpression expression)
    {
        if (expression.SimpleExpr != null)
        {
            return Visit(expression.SimpleExpr);
        }
        var recursiveStmtChecker = new RecursiveStatementChecker(assemblyState, function);
        return recursiveStmtChecker.CheckBlock(expression.Body);
    }

    public bool VisitInterpolationExpression(InterpolationExpression expression)
        => Visit(expression.Expr) || (expression.Width != null && Visit(expression.Width));

    public bool Visit(Expression expression) => expression.Accept(this);
}