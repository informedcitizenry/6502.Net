//-----------------------------------------------------------------------------
// Copyright (c) 2017-2022 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

namespace Sixty502DotNet
{
    /// <summary>
    /// Evaluates a <c>.while</c> statement and determines if its block should
    /// be activated based on the condition expression's evaluation.
    /// </summary>
    public class WhileBlockEvaluator : BlockEvaluatorBase
    {
        /// <summary>
        /// Construct a new instance of the <see cref="WhileBlockEvaluator"/>
        /// class.
        /// </summary>
        /// <param name="visitor">The block visitor to visit the block's
        /// parsed statement nodes.</param>
        /// <param name="services">The shared <see cref="AssemblyServices"/>
        /// object.</param>
        public WhileBlockEvaluator(BlockVisitor visitor, AssemblyServices services)
            : base(visitor, services)
        { }

        public override BlockState Evaluate(Sixty502DotNetParser.BlockStatContext context)
        {
            var cond = Services.ExpressionVisitor.Visit(context.enterBlock().cond);
            var state = new BlockState();
            if (Evaluator.IsCondition(cond))
            {
                while (cond.ToBool() && state.IsContinuing())
                {
                    state = Visitor.Visit(context.block());
                    cond = Services.ExpressionVisitor.Visit(context.enterBlock().cond);
                }
            }
            return state;
        }
    }
}
