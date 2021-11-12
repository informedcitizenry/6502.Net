//-----------------------------------------------------------------------------
// Copyright (c) 2017-2021 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System.Collections.Generic;

namespace Core6502DotNet
{
    /// <summary>
    /// A class responsible for processing .while/.endwhile blocks.
    /// </summary>
    public sealed class WhileBlock : BlockProcessorBase
    {
        #region Constructors

        IEnumerable<Token> _condition;

        /// <summary>
        /// Creates a new instance of a while block processor.
        /// </summary>
        /// <param name="services">The shared <see cref="AssemblyServices"/> object.</param>
        /// <param name="index">The index at which the block is defined.</param>
        public WhileBlock(AssemblyServices services, int index)
            : base(services, index) => Reserved.DefineType("Directives", ".while", ".endwhile");

        #endregion

        #region Methods

        public override void ExecuteDirective(RandomAccessIterator<SourceLine> lines)
        {
            if (lines.Current.Instruction.Name.Equals(".while", Services.StringComparison))
            {
                if (lines.Current.Operands.Count == 0)
                    Services.Log.LogEntry(lines.Current.Instruction, "Condition expression in .while loop cannot be empty.");
                else
                    _condition = lines.Current.Operands;
            }
            var iterator = _condition.GetIterator();
            if (Services.Evaluator.EvaluateCondition(iterator))
            {
                if (lines.Current.Instruction.Name.Equals(".endwhile", Services.StringComparison))
                    lines.Rewind(Index);
            }
            else
            {
                SeekBlockEnd(lines);
            }
            if (iterator.Current != null)
                throw new SyntaxException(iterator.Current, "Unexpected expression.");
        }

        #endregion

        #region Properties

        public override bool AllowBreak => true;

        public override bool AllowContinue => true;

        public override IEnumerable<string> BlockOpens => new string[] { ".while" };

        public override string BlockClosure => ".endwhile";

        #endregion
    }
}
