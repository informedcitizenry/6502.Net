//-----------------------------------------------------------------------------
// Copyright (c) 2017-2020 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

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

        #endregion

        #region Constructors

        public ConditionalBlock(SourceLine line, BlockType type)
            : base(line, type)
        {
            _ifTrue =
            _ifEvaluated = _elseEvaluated = false;
        }

        #endregion

        #region Methods

        void EvaluateCondition(SourceLine line)
        {
            if (line.InstructionName.EndsWith("ndef"))
                _ifTrue = !Assembler.SymbolManager.SymbolExists(line.OperandExpression);
            else if (line.InstructionName.EndsWith("def"))
                _ifTrue = Assembler.SymbolManager.SymbolExists(line.OperandExpression);
            else
                _ifTrue = Evaluator.EvaluateCondition(line.Operand.Children);

            if (line.InstructionName.EndsWith("def"))
            {
                // save the evaluation
                line.Instruction.Name = Regex.Replace(line.Instruction.Name, @"n?def", string.Empty);
                line.Operand = new Token
                {
                    Type = TokenType.Operator,
                    OperatorType = OperatorType.Separator,
                    Children = new List<Token>
                        {
                            new Token
                            {
                                Type = TokenType.Operator,
                                OperatorType = OperatorType.Separator,
                                Children = new List<Token>
                                {
                                    new Token
                                    {
                                        Name = _ifTrue.ToString().ToLower(),
                                        Type = TokenType.Operand
                                    }
                                }
                            }
                        }
                };
            }
        }

        public override void ExecuteDirective()
        {
            if (_ifTrue)
            {
                SeekBlockEnd();
            }
            else
            {
                SourceLine line = Assembler.LineIterator.Current;
                while (line != null && !line.InstructionName.Equals(".endif") && !_ifTrue)
                {
                    var instruction = Assembler.LineIterator.Current.InstructionName;
                    var position = Assembler.LineIterator.Current.Instruction.Position;

                    if (instruction.StartsWith(".if"))
                    {
                        _ifEvaluated = true;
                        EvaluateCondition(line);
                    }
                    else
                    {
                        if (!_ifEvaluated)
                            throw new ExpressionException(position, $"Missing \".if\" directive.");

                        if (!instruction.Equals(".endif"))
                        {
                            if (_elseEvaluated)
                                throw new ExpressionException(position, $"Invalid use of \"{instruction}\" directive.");

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
                        if (!Assembler.LineIterator.MoveNext())
                            throw new ExpressionException(position, "Missing closure for \".if\" directive.");
                        else if (_keywords.Contains(Assembler.LineIterator.Current.InstructionName))
                            throw new ExpressionException(position, "Empty sub-block under conditional directive.");
                        SeekBlockDirectives(_keywords);
                    }
                    line = Assembler.LineIterator.Current;
                }
                if (line == null)
                    throw new ExpressionException(line.Instruction.Position, $"Missing \".endif\" directive.");
            }
        }

        #endregion

        #region Properties

        public override bool WrapInScope => true;

        public override bool AllowBreak => false;

        #endregion
    }

}
