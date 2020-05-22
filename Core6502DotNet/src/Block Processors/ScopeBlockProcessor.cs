//-----------------------------------------------------------------------------
// Copyright (c) 2017-2020 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

namespace Core6502DotNet
{
    /// <summary>
    /// A class responsible for processing .block/.endblock blocks.
    /// </summary>
    public class ScopeBlock : BlockProcessorBase
    {
        #region Constructors

        public ScopeBlock(SourceLine line, BlockType type)
            : base(line, type)
        {
        }

        #endregion

        #region Methods

        public override void ExecuteDirective()
        {
            var line = Assembler.LineIterator.Current;
            var scopeName = line.LabelName;
            if (line.InstructionName.Equals(".block"))
            {
                if (string.IsNullOrEmpty(scopeName))
                    scopeName = Assembler.LineIterator.Index.ToString();
                else if (scopeName[0].IsSpecialOperator())
                {
                    throw new ExpressionException(line.Label.Position,
                        $"Invalid use of character \'{scopeName[0]}\' as a named scope block.");
                }

                Assembler.SymbolManager.PushScope(scopeName);
            }
            else
            {
                Assembler.SymbolManager.PopScope();
            }
        }

        #endregion

        #region Properties

        public override bool AllowBreak => false;

        public override bool AllowContinue => false;

        #endregion
    }

}
