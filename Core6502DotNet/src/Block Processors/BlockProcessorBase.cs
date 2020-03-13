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
    public class BlockClosureException : Exception
    {
        public BlockClosureException(string blockType)
            : base($"Missing closure for \"{blockType}\" directive.")
        {

        }
    }

    public enum BlockType
    {
        Scope,
        Conditional,
        ConditionalDef,
        ConditionalNdef,
        ForNext,
        Functional,
        Repeat,
        Switch,
        While,
        Goto
    };

    public class BlockDirective
    {
        public static readonly Dictionary<BlockType, BlockDirective> Directives = new Dictionary<BlockType, BlockDirective>
            {
                { BlockType.Scope,           new BlockDirective(".block",   ".endblock"     ) },
                { BlockType.Conditional,     new BlockDirective(".if",      ".endif"        ) },
                { BlockType.ConditionalDef,  new BlockDirective(".ifdef",   ".endif"        ) },
                { BlockType.ConditionalNdef, new BlockDirective(".ifndef",  ".endif"        ) },
                { BlockType.ForNext,         new BlockDirective(".for",     ".next"         ) },
                { BlockType.Functional,      new BlockDirective(".function",".endfunction"  ) },
                { BlockType.Repeat,          new BlockDirective(".repeat",  ".endrepeat"    ) },
                { BlockType.Switch,          new BlockDirective(".switch",  ".endswitch"    ) },
                { BlockType.While,           new BlockDirective(".while",   ".endwhile"     ) }

            };

        public BlockDirective(string open, string closure)
        {

            Open = open;
            Closure = closure;
        }

        public string Open { get; set; }

        public string Closure { get; set; }
    };

    /// <summary>
    /// A class responsible for the processing of source code blocks for a 
    /// <see cref="MultiLineAssembler"/>. This class must be inherited.
    /// </summary>
    public abstract class BlockProcessorBase
    {
        #region Constructors

        /// <summary>
        /// Creates a new instance of a block processor.
        /// </summary>
        /// <param name="line">The <see cref="SourceLine"/> containing the instruction
        /// and operands invoking or creating the block.</param>
        /// <param name="type">The <see cref="BlockType"/>.</param>
        public BlockProcessorBase(SourceLine line, BlockType type)
        {
            Index = Assembler.LineIterator.Index;
            Line = line;
            Type = type;
            //if (WrapInScope) <=== this messes with ephemerals, turning off for now. also probably not necessary
            //  Assembler.SymbolManager.PushScope(string.Empty);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Executes the block directive. This method must be defined by the inherited class.
        /// </summary>
        public abstract void ExecuteDirective();

        /// <summary>
        /// Seeks the <see cref="SourceLine"/> containing the 
        /// first instance one of the directives in the block.
        /// </summary>
        /// <param name="directives">An array of directives, one of which to seek in the block.</param>
        protected void SeekBlockDirectives(string[] directives)
        {
            var line = Assembler.LineIterator.Current;
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
                    line = Assembler.LineIterator.Skip(l => !keywordsNotToSkip.Contains(l.InstructionName));
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
        /// Seeks the <see cref="SourceLine"/> containing the directive that completes the block.
        /// </summary>
        public void SeekBlockEnd()
            => SeekBlockDirectives(new string[] { Directive.Closure });

        /// <summary>
        /// Performs a pop off the symbol stack, if the block's WrapInScope flag
        /// is set.
        /// </summary>
        public void Pop()
        {
            //if (WrapInScope)
            //  Assembler.SymbolManager.PopScope();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the block's directive.
        /// </summary>
        public BlockDirective Directive => BlockDirective.Directives[Type];


        /// <summary>
        /// Gets the index in the source listing where the block is defined.
        /// </summary>
        public int Index { get; private set; }

        /// <summary>
        /// Gets the <see cref="SourceLine"/> that defined the blcok.
        /// </summary>
        public SourceLine Line { get; private set; }

        /// <summary>
        /// Gets the block's <see cref="BlockType"/>.
        /// </summary>
        public BlockType Type { get; private set; }

        /// <summary>
        /// Gets the flag indicating whether a .break directive is allowed
        /// inside this block. Inherited class must define this flag.
        /// </summary>
        public abstract bool AllowBreak { get; }

        /// <summary>
        /// Gets a flag indicating whether the block's symbols should be
        /// local in its own scope.
        /// </summary>
        public virtual bool WrapInScope => false;

        #endregion
    }
}
