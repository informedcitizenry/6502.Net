//-----------------------------------------------------------------------------
// Copyright (c) 2017-2022 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

namespace Sixty502DotNet
{
    /// <summary>
    /// A class that tracks whether a block of statements all generate output
    /// in the same page boundary.
    /// </summary>
    public class PageBlockEvaluator : BlockEvaluatorBase
    {
        /// <summary>
        /// Construct a new instance of the <see cref="PageBlockEvaluator"/>
        /// class.
        /// </summary>
        /// <param name="visitor">The block visitor to visit the block's
        /// parsed statement nodes.</param>
        /// <param name="services">The shared <see cref="AssemblyServices"/>
        /// object.</param>
        public PageBlockEvaluator(BlockVisitor visitor, AssemblyServices services)
            : base(visitor, services)
        { }

        public override BlockState Evaluate(Sixty502DotNetParser.BlockStatContext context)
        {
            int currPage = Services.Output.LogicalPC & 0xff00;
            var state = Visitor.Visit(context.block());
            int nextPage = Services.Output.LogicalPC & 0xff00;
            if (!Services.State.PassNeeded && nextPage != currPage)
            {
                Services.Log.LogEntry(context.exitBlock(), "Crossed page boundary.");
            }
            return state;
        }
    }
}
