//-----------------------------------------------------------------------------
// Copyright (c) 2017-2020 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System.Collections.Generic;

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

        /// <summary>
        /// Creates a new instance of a repeat block processor.
        /// </summary>
        /// <param name="services">The shared <see cref="AssemblyServices"/> object.</param>
        /// <param name="index">The index at which the block is defined.</param>
        public RepeatBlock(AssemblyServices services, int index)
            : base(services, index) => Reserved.DefineType("Directives", ".repeat", ".endrepeat");

        #endregion

        #region Methods

        public override void ExecuteDirective(RandomAccessIterator<SourceLine> lines)
        {
            SourceLine line = lines.Current;
            if (line.Instruction.Name.Equals(".repeat", Services.StringComparison))
            {
                _repetition = Services.Evaluator.Evaluate(line.Operands.GetIterator(), 1, uint.MaxValue);
                if (!_repetition.IsInteger())
                    throw new ExpressionException(line.Operands[0], $"Repetition must be an integer");
            }
            else if (--_repetition > 0)
            {
                lines.Rewind(Index);
            }
        }

        #endregion

        #region Properties

        public override bool AllowBreak => true;

        public override bool AllowContinue => true;

        public override IEnumerable<string> BlockOpens => new string[] { ".repeat" };

        public override string BlockClosure => ".endrepeat";

        #endregion
    }
}
