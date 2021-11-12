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
    /// A class for procesing .do/.whiletrue blocks.
    /// </summary>
    public sealed class DoWhileBlock : BlockProcessorBase
    {
        /// <summary>
        /// Creates a new instance of a <see cref="DoWhileBlock"/>.
        /// </summary>
        /// <param name="services">The shared <see cref="AssemblyServices"/> object.</param>
        /// <param name="index">The index at which the block is defined.</param>       
        public DoWhileBlock(AssemblyServices services, int index)
            : base(services, index) => Reserved.DefineType("Directives", ".do", ".whiletrue");

        #region Methods

        public override void ExecuteDirective(RandomAccessIterator<SourceLine> lines)
        {
            if (lines.Current.Instruction.Name.Equals(".whiletrue"))
            {
                if (lines.Current.Operands.Count == 0)
                {
                    Services.Log.LogEntry(lines.Current.Instruction, "Condition in .while clause cannot be empty.");
                }
                else
                {
                    var iterator = lines.Current.Operands.GetIterator();
                    if (Services.Evaluator.EvaluateCondition(iterator))
                        lines.Rewind(Index);
                    if (iterator.Current != null)
                        throw new SyntaxException(iterator.Current, "Unexpected expression.");
                }
            }
        }

        #endregion

        #region Properties

        public override IEnumerable<string> BlockOpens => new string[] { ".do" };

        public override string BlockClosure => ".whiletrue";

        public override bool AllowBreak => true;

        public override bool AllowContinue => true;

        #endregion
    }
}
