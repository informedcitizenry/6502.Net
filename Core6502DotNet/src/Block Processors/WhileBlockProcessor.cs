//-----------------------------------------------------------------------------
// Copyright (c) 2017-2020 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

namespace Core6502DotNet
{
    /// <summary>
    /// A class responsible for processing .while/.endwhile blocks.
    /// </summary>
    public class WhileBlock : BlockProcessorBase
    {
        #region Constructors

        /// <summary>
        /// Creates a new instance of a while block processor.
        /// </summary>
        /// <param name="line">The <see cref="SourceLine"/> containing the instruction
        /// and operands invoking or creating the block.</param>
        /// <param name="type">The <see cref="BlockType"/>.</param>
        public WhileBlock(SourceLine line, BlockType type)
            : base(line, type)
        {
        }

        #endregion

        #region Methods

        public override bool ExecuteDirective()
        {
            if (!LineIterator.Current.InstructionName.Equals(".while") && 
                !LineIterator.Current.InstructionName.Equals(".endwhile"))
                return false;
            if (Evaluator.EvaluateCondition(Line.Operand.Children))
            {
                if (LineIterator.Current.InstructionName.Equals(".endwhile"))
                    LineIterator.Rewind(Index);
            }
            else
            {
                SeekBlockEnd();
            }
            return true;
        }

        #endregion

        #region Properties

        public override bool AllowBreak => true;

        public override bool AllowContinue => true;

        #endregion
    }
}
