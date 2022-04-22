//-----------------------------------------------------------------------------
// Copyright (c) 2017-2022 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

namespace Sixty502DotNet
{
    /// <summary>
    /// Evaluates an <c>.if</c> type statement and determine whether its block
    /// or a block of any alternatives following should be activated based on
    /// the condition in the <c>.if</c> statement or its alternatives is true.
    /// </summary>
    public class IfBlockSelector : BlockSelectorBase
    {
        /// <summary>
        /// Construct a new instance of the <see cref="IfBlockSelector"/>
        /// class.
        /// </summary>
        /// <param name="services">The shared <see cref="AssemblyServices"/>
        /// object.</param>
        public IfBlockSelector(AssemblyServices services)
            : base(services)
        {
        }

        private Value EvalIf(int directive, Sixty502DotNetParser.ExprContext expr)
        {
            if (directive == Sixty502DotNetParser.If ||
                directive == Sixty502DotNetParser.Elseif)
            {
                return Services.ExpressionVisitor.Visit(expr);
            }
            if (directive == Sixty502DotNetParser.Ifconst ||
                directive == Sixty502DotNetParser.Ifnconst ||
                directive == Sixty502DotNetParser.Elseifconst ||
                directive == Sixty502DotNetParser.Elseifnconst)
            {
                var ifConstExpr = Services.ExpressionVisitor.TryGetPrimaryExpression(expr, out var ifVal) && ifVal.IsDefined;
                var isConstIdent = expr.refExpr()?.identifier()?.Ident() != null &&
                     Services.Symbols.GlobalScope.Resolve(expr.refExpr().identifier().Ident().GetText())
                     is IValueResolver resolver && resolver.IsConst;
                return directive is Sixty502DotNetParser.Ifconst or
                       Sixty502DotNetParser.Elseifconst
                    ? new Value(ifConstExpr || isConstIdent)
                    : new Value(!ifConstExpr && !isConstIdent);
            }
            var symName = expr.refExpr()?.identifier()?.name.Text;
            if (symName != null)
            {
                // a symbol is considered defined if it's a label that occurs before the
                // current '.if' statement
                var isDefined = Services.Symbols.Scope.Resolve(symName) is Label sym &&
                    sym.DefinedAt?.Start.StartIndex < expr.Start.StartIndex;

                if (directive == Sixty502DotNetParser.Ifdef ||
                    directive == Sixty502DotNetParser.Elseifdef)
                {
                    return new Value(isDefined);
                }
                return new Value(!isDefined);
            }
            Services.Log.LogEntry(expr, "Symbol name expected.");
            return new Value();
        }

        public override int Select(Sixty502DotNetParser.BlockStatContext context)
        {
            var ifContext = context.enterIf();
            var blockContext = context.ifBlock();
            bool evaluated = true;
            var ifCond = EvalIf(ifContext.directive.Type, ifContext.expr());
            if (Evaluator.IsCondition(ifCond))
            {
                var evalExpressions = new ArrayValue() { ifCond };
                foreach (var elseIf in blockContext.enterElseIf())
                {
                    var elseIfCond = EvalIf(elseIf.directive.Type, elseIf.expr());
                    if (!Evaluator.IsCondition(elseIfCond))
                    {
                        return -1;
                    }
                    evalExpressions.Add(elseIfCond);
                }
                if (!evalExpressions.ContainsUndefinedElement && evaluated)
                {
                    var firstTrue = evalExpressions.FindIndex(v => v.ToBool());
                    var blocks = blockContext.block();
                    if (firstTrue > -1 || blockContext.enterElse() != null)
                    {
                        if (firstTrue < 0)
                        {
                            firstTrue = blocks.Length - 1;
                        }
                        return firstTrue;
                    }
                }
            }
            return -1;
        }
    }
}
