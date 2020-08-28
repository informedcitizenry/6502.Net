//-----------------------------------------------------------------------------
// Copyright (c) 2017-2020 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System.Runtime.CompilerServices;

namespace Core6502DotNet
{
    /// <summary>
    /// A class responsible for processing .block/.endblock blocks.
    /// </summary>
    public class ScopeBlock : BlockProcessorBase
    {
        #region Constructors

        /// <summary>
        /// Creates a new instance of a scoped block processor.
        /// </summary>
        /// <param name="line">The <see cref="SourceLine"/> containing the instruction
        /// and operands invoking or creating the block.</param>
        /// <param name="type">The <see cref="BlockType"/>.</param>
        public ScopeBlock(SourceLine line, BlockType type)
            : base(line, type, false)
        {
        }

        /// <summary>
        /// Creates a new instance of a scoped block processor.
        /// </summary>
        /// <param name="iterator">The <see cref="SourceLine"/> containing the instruction
        /// and operands invoking or creating the block.</param>
        /// <param name="type">The <see cref="BlockType"/>.</param>
        public ScopeBlock(RandomAccessIterator<SourceLine> iterator,
                          BlockType type)
            : base(iterator, type, false)
        {
        }

        #endregion

        #region Methods

        public override bool ExecuteDirective()
        {
            var line = LineIterator.Current;
            var scopeName = line.LabelName;
            if (line.InstructionName.Equals(".block"))
            {
                if (string.IsNullOrEmpty(scopeName))
                    scopeName = LineIterator.Index.ToString();
                else if (!char.IsLetter(scopeName[0]))
                {
                    throw new SyntaxException(line.Label.Position,
                        $"Invalid name \"{scopeName}\" for scope block.");
                }
                Assembler.SymbolManager.PushScope(scopeName);
                return true;
            }
            else if (line.InstructionName.Equals(".endblock"))
            {
                Assembler.SymbolManager.PopScope();
                return true;
            }
            return false;
        }

        public override void PopScope() { }

        #endregion

        #region Properties

        public override bool AllowBreak => false;

        public override bool AllowContinue => false;

        #endregion
    }

}
