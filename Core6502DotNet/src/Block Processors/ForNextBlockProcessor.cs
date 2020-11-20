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
    /// A class responsible for processing .for/.next code blocks.
    /// </summary>
    public sealed class ForNextBlock : BlockProcessorBase
    {
        #region Members

        List<Token> _condition;
        List<Token> _iterations;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new instance of the for next block processor.
        /// </summary>
        /// <param name="services">The shared <see cref="AssemblyServices"/> object.</param>
        /// <param name="index">The index at which the block is defined.</param>
        public ForNextBlock(AssemblyServices services, int index)
            : base(services, index) { }

        #endregion

        #region Methods

        public override void ExecuteDirective(RandomAccessIterator<SourceLine> lines)
        {
            if (lines.Current.Instruction.Name.Equals(".for", Services.StringComparison))
            {
                var line = lines.Current;
                if (line.Operands.Count == 0)
                    throw new SyntaxException(line.Instruction, "Missing operands for \".for\" directive.");
                var it = line.Operands.GetIterator();
                if (!Token.IsEnd(it.GetNext()))
                    Services.SymbolManager.DefineSymbol(it);

                if (!it.MoveNext())
                    throw new SyntaxException(line.Operands[^1], "Missing condition clause.");
                _condition = new List<Token>();
                while (!Token.IsEnd(it.Current))
                {
                    if (it.Current.Name.Equals("("))
                    {
                        _condition.AddRange(Token.GetGroup(it));
                    }
                    else
                    {
                        _condition.Add(it.Current);
                        it.MoveNext();
                    }
                }
                if (_condition.Count > 0 && !Services.Evaluator.EvaluateCondition(_condition.GetIterator()))
                {
                    SeekBlockEnd(lines);
                }
                else
                {
                    if (it.Current == null)
                        throw new SyntaxException(line.Operands[^1], "Expression expected.");
                    _iterations = new List<Token>();
                    while ((it.GetNext()) != null)
                        _iterations.Add(it.Current);
                    if (_iterations.Count == 0 || _iterations[^1].IsSeparator())
                        throw new SyntaxException(line.Operands[^1], "Expression expected.");
                }
            }
            else
            {
                var it = _iterations.GetIterator();
                while (it.GetNext() != null)
                {
                    Services.SymbolManager.DefineSymbol(it);
                    if (it.Current == null)
                        break;
                    if (it.Current.IsSeparator())
                        continue;
                }
                if (_condition.Count > 0 && Services.Evaluator.EvaluateCondition(_condition.GetIterator()))
                    lines.Rewind(Index);
            }
        }
        #endregion

        #region Properties

        public override bool AllowBreak => true;

        public override bool AllowContinue => true;

        public override IEnumerable<string> BlockOpens => new string[] { ".for" };

        public override string BlockClosure => ".next";

        #endregion
    }
}
