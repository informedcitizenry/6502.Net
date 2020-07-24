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

        public WhileBlock(SourceLine line, BlockType type)
            : base(line, type)
        {
        }

        #endregion

        #region Methods

        public override bool ExecuteDirective()
        {
            if (!Assembler.LineIterator.Current.Equals(".while") && !Assembler.LineIterator.Current.Equals(".endwhile"))
                return false;
            if (Evaluator.EvaluateCondition(Line.Operand.Children))
            {
                if (Assembler.CurrentLine.InstructionName.Equals(".endwhile"))
                    Assembler.LineIterator.Rewind(Index);
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
