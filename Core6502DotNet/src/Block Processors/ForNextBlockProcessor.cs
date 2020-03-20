//-----------------------------------------------------------------------------
// Copyright (c) 2017-2020 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;

namespace Core6502DotNet
{
    /// <summary>
    /// A class responsible for processing .for/.next code blocks.
    /// </summary>
    public sealed class ForNextBlock : BlockProcessorBase
    {
        #region Members

        readonly IEnumerable<Token> _condition;
        readonly Token _iterations;

        #endregion

        #region Constructors

        public ForNextBlock(SourceLine line, BlockType type)
            : base(line, type)
        {
            if (line.Operand == null ||
                line.Operand.Children.Count < 3)
            {
                throw new ExpressionException(line.Operand.Position, "Missing operands for \".for\" directive.");
            }

            if (line.Operand.Children[1].HasChildren)
                _condition = line.Operand.Children[1].Children;
            _iterations = new Token
            {
                Children = line.Operand.Children.Skip(2).ToList()
            };
            if (line.Operand.Children[0].HasChildren)
                Assembler.SymbolManager.Define(line.Operand.Children[0].Children, true);

        }

        #endregion

        #region Methods

        public override void ExecuteDirective()
        {
            if (Assembler.CurrentLine.InstructionName.Equals(".next"))
            {
                foreach (Token child in _iterations.Children)
                {
                    if (!child.HasChildren)
                        throw new ExpressionException(child.Position, "Iteration expression cannot be empty.");

                    if (!Assembler.SymbolManager.SymbolExists(child.Children[0].Name))
                    {
                        throw new ExpressionException(child.Children[0].Position,
                            $"Variable \"{child.Children[0].Name}\" must be defined before it is used.");
                    }

                    Assembler.SymbolManager.Define(child.Children, true);
                }

                if (_condition == null || Evaluator.EvaluateCondition(_condition))
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
