//-----------------------------------------------------------------------------
// Copyright (c) 2017-2022 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

namespace Sixty502DotNet
{
    /// <summary>
    /// Analyzes parsed blocks of code and selects the active
    /// block to assemble. This class must be inherited.
    /// </summary>
    public abstract class BlockSelectorBase
    {
        /// <summary>
        /// Construct a new instance of the <see cref="BlockSelectorBase"/>
        /// class.
        /// </summary>
        /// <param name="services">The shared <see cref="AssemblyServices"/>
        /// object.</param>
        protected BlockSelectorBase(AssemblyServices services)
            => Services = services;

        /// <summary>
        /// Analyze the parsed set of blocks and select the active block, if
        /// eligible to be selected.
        /// </summary>
        /// <param name="context">The
        /// <see cref="Sixty502DotNetParser.BlockStatContext"/>.</param>
        /// <returns>The <see cref="BlockState"/> resulting from the processing
        /// of the block.</returns>
        public abstract int Select(Sixty502DotNetParser.BlockStatContext context);

        /// <summary>
        /// Get the shared <see cref="AssemblyServices"/> object.
        /// </summary>
        protected AssemblyServices Services { get; init; }
    }
}
