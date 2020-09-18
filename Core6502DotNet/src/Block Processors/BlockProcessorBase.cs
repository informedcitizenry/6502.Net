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
    /// An enumeration of a block type.
    /// </summary>
    public enum BlockType
    {
        Scope,
        Conditional,
        ConditionalDef,
        ConditionalNdef,
        ForNext,
        Functional,
        Page,
        Repeat,
        Switch,
        While,
        Goto
    };

    /// <summary>
    /// A class that defines a block opening and closure.
    /// </summary>
    public readonly struct BlockDirective
    {
        /// <summary>
        /// Gets the defined list of block directives by block type.
        /// </summary>
        public static readonly Dictionary<BlockType, BlockDirective> Directives = new Dictionary<BlockType, BlockDirective>
            {
                { BlockType.Scope,           new BlockDirective(".block",   ".endblock"     ) },
                { BlockType.Conditional,     new BlockDirective(".if",      ".endif"        ) },
                { BlockType.ConditionalDef,  new BlockDirective(".ifdef",   ".endif"        ) },
                { BlockType.ConditionalNdef, new BlockDirective(".ifndef",  ".endif"        ) },
                { BlockType.ForNext,         new BlockDirective(".for",     ".next"         ) },
                { BlockType.Functional,      new BlockDirective(".function",".endfunction"  ) },
                { BlockType.Page,            new BlockDirective(".page",    ".endpage"      ) },
                { BlockType.Repeat,          new BlockDirective(".repeat",  ".endrepeat"    ) },
                { BlockType.Switch,          new BlockDirective(".switch",  ".endswitch"    ) },
                { BlockType.While,           new BlockDirective(".while",   ".endwhile"     ) }

            };

        /// <summary>
        /// Creates a new instance of the block closure definition.
        /// </summary>
        /// <param name="open"></param>
        /// <param name="closure"></param>
        public BlockDirective(string open, string closure) 
        {

            Open = open;
            Closure = closure;
        }

        /// <summary>
        /// Gets the block's open directive.
        /// </summary>
        public string Open { get; }

        /// <summary>
        /// Gets the block's closure directive.
        /// </summary>
        public string Closure { get; }
    };

    /// <summary>
    /// A class responsible for the processing of source code blocks for a 
    /// <see cref="BlockAssembler"/>. This class must be inherited.
    /// </summary>
    public abstract class BlockProcessorBase : Core6502Base
    {
        #region Constructors

        /// <summary>
        /// Creates a new instance of a block processor. 
        /// </summary>
        /// <param name="services">The shared <see cref="AssemblyServices"/> object.</param>
        /// <param name="iterator">The <see cref="SourceLine"/> iterator to traverse when
        /// processing the block.</param>
        /// <param name="type">The <see cref="BlockType"/>.</param>
        public BlockProcessorBase(AssemblyServices services,
                                  RandomAccessIterator<SourceLine> iterator, 
                                  BlockType type)
            : this(services, iterator, type, true) { }

        /// <summary>
        /// Creates a new instance of a block processor.
        /// </summary>
        /// <param name="iterator">The <see cref="SourceLine"/> iterator to traverse when
        /// processing the block.</param>
        /// <param name="type">The <see cref="BlockType"/>.</param>
        /// <param name="createScope">Automatically create a scope when initialized.</param>
        protected BlockProcessorBase(AssemblyServices services,
                                     RandomAccessIterator<SourceLine> iterator, 
                                     BlockType type, 
                                     bool createScope)
            :base(services)
        {
            LineIterator = iterator;
            Line = iterator.Current;
            Index = iterator.Index;
            Type = type;
            if (createScope)
                Services.SymbolManager.PushScope(Index.ToString());
        }

        #endregion

        #region Methods

        /// <summary>
        /// Executes the block directive. This method must be inherited.
        /// </summary>
        /// <returns><c>true</c> if the block processor successfully executed the
        /// directive, <c>false</c> otherwise.</returns>
        public abstract bool ExecuteDirective();

        public virtual void PopScope()
        {
            SeekBlockEnd();
            Services.SymbolManager.PopScope();
        }

        //public abstract bool ExecuteDirective();

        /// <summary>
        /// Seeks the <see cref="SourceLine"/> containing the 
        /// first instance one of the directives in the block.
        /// </summary>
        /// <param name="iterator">The source line iterator.</param>
        /// <param name="directives">An array of directives, one of which to seek in the block.</param>
        protected void SeekBlockDirectives(RandomAccessIterator<SourceLine> iterator, string[] directives)
        {
            var line = iterator.Current;
            var instruction = line.InstructionName;
            if (!directives.Contains(instruction))
            {
                var blockOpen = BlockDirective.Directives[Type].Open;
                var blockClose = BlockDirective.Directives[Type].Closure;

                var keywordsNotToSkip = new HashSet<string>(directives)
                {
                    blockOpen,
                    blockClose
                };
                var opens = 1;
                while (opens != 0)
                {
                    line = iterator.FirstOrDefault(l => keywordsNotToSkip.Contains(l.InstructionName));
                    if (line == null)
                        throw new BlockClosureException(blockOpen);

                    if (line.InstructionName.Equals(blockOpen))
                        opens++;

                    if (opens < 2 && directives.Contains(line.InstructionName))
                        break;

                    if (line.InstructionName.Equals(blockClose))
                        opens--;
                }
            }
        }

        /// <summary>
        /// Seeks the <see cref="SourceLine"/> containing the 
        /// first instance one of the directives in the block.
        /// </summary>
        /// <param name="directives">An array of directives, one of which to seek in the block.</param>
        protected void SeekBlockDirectives(string[] directives)
            => SeekBlockDirectives(LineIterator, directives);

        /// <summary>
        /// Seeks the <see cref="SourceLine"/> containing the directive that completes the block.
        /// </summary>
        /// <param name="iterator">The iterator through which to seek the line.</param>
        public void SeekBlockEnd(RandomAccessIterator<SourceLine> iterator)
            => SeekBlockDirectives(iterator, new string[] { Directive.Closure });

        /// <summary>
        /// Seeks the <see cref="SourceLine"/> containing the directive that completes the block.
        /// </summary>
        public void SeekBlockEnd()
            => SeekBlockDirectives(LineIterator, new string[] { Directive.Closure });

        #endregion

        #region Properties

        /// <summary>
        /// Gets the block's directive.
        /// </summary>
        public BlockDirective Directive => BlockDirective.Directives[Type];

        /// <summary>
        /// Gets the index in the source listing where the block is defined.
        /// </summary>
        public int Index { get; }

        /// <summary>
        /// Gets the <see cref="SourceLine"/> that defined the blcok.
        /// </summary>
        public SourceLine Line { get; }

        /// <summary>
        /// Gets the block's <see cref="BlockType"/>.
        /// </summary>
        public BlockType Type { get; }

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
        /// Gets this block processor's line iterator.
        /// </summary>
        protected RandomAccessIterator<SourceLine> LineIterator { get; }

        #endregion
    }
}