//-----------------------------------------------------------------------------
// Copyright (c) 2017-2022 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

namespace Sixty502DotNet
{
    /// <summary>
    /// Evaluates a repeat block statement and determines if the block should
    /// be activated based on the <c>.repeat</c> statement count.
    /// </summary>
    public class RepeatBlockEvaluator : BlockEvaluatorBase
    {
        /// <summary>
        /// Construct a new instance of the <see cref="RepeatBlockEvaluator"/>
        /// object.
        /// </summary>
        /// <param name="visitor">The block visitor to visit the block's
        /// parsed statement nodes.</param>
        /// <param name="services">The shared <see cref="AssemblyServices"/>
        /// object.</param>
        public RepeatBlockEvaluator(BlockVisitor visitor, AssemblyServices services)
            : base(visitor, services)
        { }

        public override BlockState Evaluate(Sixty502DotNetParser.BlockStatContext context)
        {
            var times = Services.ExpressionVisitor.Visit(context.enterBlock().times);
            var state = new BlockState();
            if (!times.IsNumeric)
            {
                if (times.IsDefined)
                {
                    Services.Log.LogEntry(context.enterBlock().times, "Repeat expression not valid.");
                }
                return state;
            }
            var i = times.ToInt();
            if (i < 0 && !Services.State.PassNeeded)
            {
                Services.Log.LogEntry(context.enterBlock().times, "Invalid repetition value.");
                return state;
            }
            while (i-- > 0 && state.IsContinuing())
            {
                state = Visitor.Visit(context.block());
            }
            return state;
        }
    }
}
