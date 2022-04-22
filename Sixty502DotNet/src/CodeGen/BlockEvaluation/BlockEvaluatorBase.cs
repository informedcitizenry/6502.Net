//-----------------------------------------------------------------------------
// Copyright (c) 2017-2022 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

namespace Sixty502DotNet
{
    /// <summary>
    /// Evaluates a statement at the head of a block, such as a for or repeat loop, and
    /// determines if the block is activated. This class must be inherited.
    /// </summary>
    public abstract class BlockEvaluatorBase
    {
        /// <summary>
        /// Construct a new instance of the <see cref="BlockEvaluatorBase"/>
        /// class.
        /// </summary>
        /// <param name="visitor">The block visitor to visit the block's
        /// parsed statement nodes.</param>
        /// <param name="services">The shared <see cref="AssemblyServices"
        /// object.</param>
        protected BlockEvaluatorBase(BlockVisitor visitor, AssemblyServices services)
            => (Visitor, Services) = (visitor, services);

        /// <summary>
        /// Evaluates the statement at the head of a block, and determines if the
        /// block is activated.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public abstract BlockState Evaluate(Sixty502DotNetParser.BlockStatContext context);

        /// <summary>
        /// Get the shared <see cref="AssemblyServices"/> object.
        /// </summary>
        protected AssemblyServices Services { get; init; }

        /// <summary>
        /// Get the <see cref="BlockVisitor"/> object.
        /// </summary>
        protected BlockVisitor Visitor { get; init; }
    }
}
