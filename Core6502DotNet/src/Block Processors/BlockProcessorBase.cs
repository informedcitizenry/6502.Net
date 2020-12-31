//-----------------------------------------------------------------------------
// Copyright (c) 2017-2020 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

namespace Core6502DotNet
{
    /// <summary>
    /// Represents an error when a block is not properly closed.
    /// </summary>
    public class BlockClosureException : Exception
    {
        /// <summary>
        /// Creates an instance of a block closure exception.
        /// </summary>
        /// <param name="blockType">The block type.</param>
        public BlockClosureException(string blockType)
            : base($"Missing closure for \"{blockType}\" directive.")
        {

        }
    }

    /// <summary>
    /// A class responsible for the processing of source code blocks for a 
    /// <see cref="BlockAssembler"/>. This class must be inherited.
    /// </summary>
    public abstract class BlockProcessorBase : AssemblerBase
    {
        #region Members

        bool _createScope;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new instance of a block processor. 
        /// </summary>
        /// <param name="services">The shared <see cref="AssemblyServices"/> object.</param>
        /// <param name="index">The index at which the block is defined.</param>
        public BlockProcessorBase(AssemblyServices services, int index)
            : this(services, index, true)
        {

        }

        /// <summary>
        /// Creates a new instance of a block processor. 
        /// </summary>
        /// <param name="services">The shared <see cref="AssemblyServices"/> object.</param>
        /// <param name="index">The index at which the block is defined.</param>
        /// <param name="createScope">Automatically create a scope when initialized.</param>
        protected BlockProcessorBase(AssemblyServices services, int index, bool createScope)
            : base(services)
        {
            Index = index;
            _createScope = createScope;
            if (_createScope)
                services.SymbolManager.PushScope(Index.ToString());
        }

        #endregion

        #region Methods

        /// <summary>
        /// Executes the block directive. This method must be inherited.
        /// </summary>
        /// <param name="iterator">The source line iterator.</param>
        /// <exception cref="SyntaxException"></exception>
        public abstract void ExecuteDirective(RandomAccessIterator<SourceLine> iterator);

        /// <summary>
        /// Cleanup the current block's scope.
        /// </summary>
        /// <param name="iterator">The source line iterator.</param>
        public void PopScope(RandomAccessIterator<SourceLine> iterator)
        {
            SeekBlockEnd(iterator);
            if (_createScope)
                Services.SymbolManager.PopScope();
        }

        /// <summary>
        /// Seeks the <see cref="SourceLine"/> containing the 
        /// first instance one of the directives in the block.
        /// </summary>
        /// <param name="iterator">The source line iterator.</param>
        /// <param name="directives">An array of directives, one of which to seek in the block.</param>
        protected void SeekBlockDirectives(RandomAccessIterator<SourceLine> iterator, StringView[] directives)
        {
            var line = iterator.Current;
            if (!line.Instruction.Name.Equals(BlockClosure, Services.StringComparison))
            {
                var blockClose = BlockClosure;

                var keywordsNotToSkip = new List<StringView>(directives)
                {
                    blockClose
                };
                keywordsNotToSkip.AddRange(BlockOpens.Select(bo => new StringView(bo)));

                var opens = 1;
                while (opens != 0)
                {
                    line = iterator.FirstOrDefault(l => l.Instruction != null && keywordsNotToSkip.Contains(l.Instruction.Name, Services.StringViewComparer));
                    if (line == null)
                        throw new BlockClosureException(BlockOpens.First());

                    if (BlockOpens.Contains(line.Instruction.Name.ToString(), Services.StringComparer))
                        opens++;

                    if (opens < 2 && directives.Contains(line.Instruction.Name, Services.StringViewComparer))
                        break;

                    if (line.Instruction.Name.Equals(blockClose, Services.StringComparison))
                        opens--;
                }
            }
        }

        /// <summary>
        /// Seeks the <see cref="SourceLine"/> containing the directive that completes the block.
        /// </summary>
        /// <param name="iterator">The iterator through which to seek the line.</param>
        public void SeekBlockEnd(RandomAccessIterator<SourceLine> iterator)
            => SeekBlockDirectives(iterator, new StringView[] { BlockClosure });

        protected override string OnAssemble(RandomAccessIterator<SourceLine> lines)
            => throw new NotImplementedException();

        #endregion

        #region Properties

        /// <summary>
        /// The keyword or keywords that mark the block opening.
        /// </summary>
        public abstract IEnumerable<string> BlockOpens { get; }

        /// <summary>
        /// The keyword that marks the block closure.
        /// </summary>
        public abstract string BlockClosure { get; }

        /// <summary>
        /// Gets the flag indicating whether a .break directive is allowed
        /// inside this block. Inherited class must define this flag.
        /// </summary>
        public abstract bool AllowBreak { get; }

        /// <summary>
        /// Gets the flag indicating whether a .continue directive is allowed
        /// inside this block. Inherited class must define this flag.
        /// </summary>
        public abstract bool AllowContinue { get; }

        /// <summary>
        /// Gets a flag indicating whether the block's symbols should be
        /// local in its own scope.
        /// </summary>
        public virtual bool WrapInScope => false;

        /// <summary>
        /// The index at which the block was defined.
        /// </summary>
        public int Index { get; }

        #endregion
    }
}