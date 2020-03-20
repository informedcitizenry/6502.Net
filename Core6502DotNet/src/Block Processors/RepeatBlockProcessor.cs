//-----------------------------------------------------------------------------
// Copyright (c) 2017-2020 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

namespace Core6502DotNet
{
    /// <summary>
    /// A class responsible for processing .repeat/.endrepeat blocks.
    /// </summary>
    public class RepeatBlock : BlockProcessorBase
    {
        #region Members

        double _repetition;

        #endregion

        #region Constructors

        public RepeatBlock(SourceLine line, BlockType type)
            : base(line, type)
        {
            _repetition = Evaluator.Evaluate(Line.Operand.Children, 1, uint.MaxValue);
            if (!_repetition.IsInteger())
                throw new ExpressionException(Line.Operand.Position, $"Repetition must be an integer");
        }

        #endregion

        #region Methods

        public override void ExecuteDirective()
        {
            SourceLine line = Assembler.LineIterator.Current;
            if (line.InstructionName.Equals(".endrepeat"))
            {
                if (_repetition < 1)
                    throw new ExpressionException(line.Instruction.Position, $"Missing matching \".repeat\" directive.");

                if (--_repetition > 0)
                    Assembler.LineIterator.Rewind(Index);

            }
        }

        #endregion

        #region Properties

        public override bool AllowBreak => true;

        public override bool AllowContinue => true;

        #endregion
    }
}
