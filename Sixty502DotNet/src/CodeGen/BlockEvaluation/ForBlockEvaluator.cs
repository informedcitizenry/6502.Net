//-----------------------------------------------------------------------------
// Copyright (c) 2017-2022 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

namespace Sixty502DotNet
{
    /// <summary>
    /// Performs a for loop operation over a block based on the head <c>.for</c>
    /// directive's operands.
    /// </summary>
    public class ForBlockEvaluator : BlockEvaluatorBase
    {
        /// <summary>
        /// Construct a new instance of the <see cref="ForBlockEvaluator"/>
        /// class.
        /// </summary>
        /// <param name="visitor">The block visitor to visit the block's
        /// parsed statement nodes.</param>
        /// <param name="services">The shared <see cref="AssemblyServices"/>
        /// object.</param>
        public ForBlockEvaluator(BlockVisitor visitor, AssemblyServices services)
            : base(visitor, services)
        { }

        public override BlockState Evaluate(Sixty502DotNetParser.BlockStatContext context)
        {
            var state = BlockState.Evaluating;
            var induction = context.enterBlock().induction;
            if (induction == null || Services.ExpressionVisitor.Visit(induction).IsDefined)
            {
                var cond = context.enterBlock().cond;
                Value condResult;
                if (cond == null)
                {
                    condResult = new Value(true);
                }
                else
                {
                    condResult = Services.ExpressionVisitor.Visit(cond);
                }
                if (!Evaluator.IsCondition(condResult))
                {
                    Services.Log.LogEntry(context.enterBlock().cond, Errors.ExpressionNotCondition);
                    return state;
                }
                while (condResult.ToBool())
                {
                    state = Visitor.Visit(context.block());
                    if (!state.IsContinuing())
                    {
                        break;
                    }
                    foreach (var iteration in context.enterBlock().assignExprList().assignExpr())
                    {
                        if (!Services.ExpressionVisitor.Visit(iteration).IsDefined)
                        {
                            return new BlockState();
                        }
                    }
                    if (cond != null)
                    {
                        condResult = Services.ExpressionVisitor.Visit(cond);
                    }
                }
            }
            return state;
        }
    }
}
