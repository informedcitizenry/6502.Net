//-----------------------------------------------------------------------------
// Copyright (c) 2017-2020 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Core6502DotNet
{
    /// <summary>
    /// A class reponsible for processing .if/.endif conditional blocks of code.
    /// </summary>
    public class ConditionalBlock : BlockProcessorBase
    {
        #region Members

        bool _ifEvaluated;
        bool _elseEvaluated;
        bool _ifTrue;

        static readonly string[] _keywords =
        {
                ".if", ".ifdef", ".ifndef",
                ".else", ".elseif", ".elseif", ".elseifdef", ".elseifndef",
                ".endif"
        };

        readonly Dictionary<(string, int), bool> _ifDefEvaluations;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new instance of a conditional block processor.
        /// </summary>
        /// <param name="line">The <see cref="SourceLine"/> containing the instruction
        /// and operands invoking or creating the block.</param>
        /// <param name="type">The <see cref="BlockType"/>.</param>
        public ConditionalBlock(SourceLine line, BlockType type)
            : base(line, type)
        {
            _ifDefEvaluations = new Dictionary<(string, int), bool>();
            _ifTrue =
            _ifEvaluated = _elseEvaluated = false;
        }

        #endregion

        #region Methods

        void EvaluateCondition(SourceLine line)
        {
            if (line.InstructionName.EndsWith("def"))
            {
                if (!_ifDefEvaluations.TryGetValue((line.Filename, line.LineNumber), out _ifTrue))
                {
                    if (line.InstructionName.EndsWith("ndef"))
                        _ifTrue = !Assembler.SymbolManager.SymbolExists(line.OperandExpression);
                    else if (line.InstructionName.EndsWith("def"))
                        _ifTrue = Assembler.SymbolManager.SymbolExists(line.OperandExpression);
                    // save the evaluation
                    _ifDefEvaluations.Add((line.Filename, line.LineNumber), _ifTrue);
                }
            }
            else
            {
                _ifTrue = Evaluator.EvaluateCondition(line.Operand.Children);
            }
        }

        public override bool ExecuteDirective()
        {
            SourceLine line = LineIterator.Current;
            if (!_keywords.Contains(line.InstructionName))
                return false;

            if (_ifTrue)
            {
                SeekBlockEnd();
            }
            else
            {
                while (line != null && !line.InstructionName.Equals(".endif") && !_ifTrue)
                {
                    var instruction = LineIterator.Current.InstructionName;
                    var position = LineIterator.Current.Instruction.Position;
                    if (instruction.StartsWith(".if"))
                    {
                        _ifEvaluated = true;
                        EvaluateCondition(line);
                    }
                    else
                    {
                        if (!_ifEvaluated)
                            throw new SyntaxException(position, $"Missing \".if\" directive.");

                        if (!instruction.Equals(".endif"))
                        {
                            if (_elseEvaluated)
                                throw new SyntaxException(position, $"Invalid use of \"{instruction}\" directive.");

                            _elseEvaluated = instruction.Equals(".else");

                            if (_elseEvaluated)
                                break;

                            EvaluateCondition(line);
                        }
                    }
                    if (_ifTrue)
                    {
                        break;
                    }
                    else
                    {
                        if (!LineIterator.MoveNext())
                            throw new SyntaxException(position, "Missing closure for \".if\" directive.");
                        else if (_keywords.Contains(LineIterator.Current.InstructionName))
                            throw new SyntaxException(position, "Empty sub-block under conditional directive.");
                        SeekBlockDirectives(_keywords);
                    }
                    line = LineIterator.Current;
                }
                if (line == null)
                    throw new SyntaxException(line.Instruction.Position, $"Missing \".endif\" directive.");
            }
            return true;
        }

        #endregion

        #region Properties

        public override bool WrapInScope => true;

        public override bool AllowBreak => false;

        public override bool AllowContinue => false;

        #endregion
    }

}
