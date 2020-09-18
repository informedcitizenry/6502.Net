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

        /// <summary>
        /// Creates a new instance of the for next block processor.
        /// </summary>
        /// <param name="services">The shared <see cref="AssemblyServices"/> object.</param>
        /// <param name="iterator">The <see cref="SourceLine"/> iterator to traverse when
        /// processing the block.</param>
        /// <param name="type">The <see cref="BlockType"/>.</param>
        public ForNextBlock(AssemblyServices services,
                            RandomAccessIterator<SourceLine> iterator, 
                            BlockType type)
            : base(services, iterator, type)
        {
            var line = iterator.Current;
            if (line.Operand == null ||
                line.Operand.Children.Count < 3)
            {
                throw new SyntaxException(line.Operand.Position, "Missing operands for \".for\" directive.");
            }

            if (line.Operand.Children[1].Children.Count > 0)
                _condition = line.Operand.Children[1].Children;
            _iterations = new Token(string.Empty,
                                    string.Empty,
                                    TokenType.Operator,
                                    OperatorType.Separator,
                                    line.Operand.Children[2].Position,
                                    line.Operand.Children.Skip(2));
            if (Line.Operand.Children[0].Children.Count > 0)
                Services.SymbolManager.Define(Line.Operand.Children[0].Children, true);
        }

        #endregion

        #region Methods

        public override bool ExecuteDirective()
        {
        if (LineIterator.Current.InstructionName.Equals(".next"))
            {
                foreach (var child in _iterations.Children)
                {
                    if (child.Children.Count == 0)
                        throw new SyntaxException(child.Position, "Iteration expression cannot be empty.");

                    if (!Services.SymbolManager.SymbolExists(child.Children[0].Name))
                    {
                        throw new SyntaxException(child.Children[0].Position,
                            $"Variable \"{child.Children[0].Name}\" must be defined before it is used.");
                    }
                    Services.SymbolManager.Define(child.Children, true);
                }
                if (_condition == null || Services.Evaluator.EvaluateCondition(_condition))
                    LineIterator.Rewind(Index);
                return true;
            }
            return LineIterator.Current.InstructionName.Equals(".for");
        }
        #endregion

        #region Properties

        public override bool AllowBreak => true;

        public override bool AllowContinue => true;

        #endregion
    }
}
