//-----------------------------------------------------------------------------
// Copyright (c) 2017-2022 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

namespace Sixty502DotNet
{
    /// <summary>
    /// Evaluates a <c>.whiletrue</c> statement and determines if the block
    /// defined by the corresponding <c>.do</c> directive should be activated
    /// based on the condition expression's evaluation.
    /// </summary>
    public class DoBlockEvaluator : BlockEvaluatorBase
    {
        private readonly Sixty502DotNetParser.ExprContext _whileTrue;

        /// <summary>
        /// Construct a new instance of the <see cref="DoBlockEvaluator"/>
        /// class.
        /// </summary>
        /// <param name="visitor">The block visitor to visit the block's
        /// parsed statement nodes.</param>
        /// <param name="services">The shared <see cref="AssemblyServices"/>
        /// object.</param>
        /// <param name="whileTrue">The parsed condition in the 
        /// <c>.whiletrue</c> statement.</param>
        public DoBlockEvaluator(BlockVisitor visitor,
                                AssemblyServices service,
                                Sixty502DotNetParser.ExprContext whileTrue)
            : base(visitor, service) => _whileTrue = whileTrue;

        public override BlockState Evaluate(Sixty502DotNetParser.BlockStatContext context)
        {
            var whileCond = Services.ExpressionVisitor.Visit(_whileTrue);
            if (Evaluator.IsCondition(whileCond))
            {
                var state = Visitor.Visit(context.block());
                while (state.IsContinuing() && whileCond.ToBool())
                {
                    state = Visitor.Visit(context.block());
                    whileCond = Services.ExpressionVisitor.Visit(_whileTrue);
                }
                return state;
            }
            return new BlockState();
        }
    }
}
