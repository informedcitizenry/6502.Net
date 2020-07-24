//-----------------------------------------------------------------------------
// Copyright (c) 2017-2020 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

namespace Core6502DotNet
{
    /// <summary>
    /// A class responsible for processing .switch/.endswitch blocks.
    /// </summary>
    public class SwitchBlock : BlockProcessorBase
    {
        #region Subclassses

        public class SwitchBlockException : Exception
        {
            public SwitchBlockException(string message)
                : base(message)
            {

            }
        }

        class CaseBlock<T>
        {
            public CaseBlock()
            {
                Cases = new List<T>();
                FallthroughIndex = -1;
            }

            public List<T> Cases { get; private set; }

            public int FallthroughIndex { get; set; }
        }

        class SwitchContext
        {
            List<CaseBlock<string>> _stringBlocks;
            List<CaseBlock<double>> _numericBlocks;

            SwitchContext()
            {
                _stringBlocks = new List<CaseBlock<string>>();
                _numericBlocks = new List<CaseBlock<double>>();
            }

            public SwitchContext(string value)
                : this()
            {
                StringValue = value;
                NumericValue = double.NaN;
            }

            public SwitchContext(double value)
                : this()
            {
                NumericValue = value;
                StringValue = string.Empty;
            }

            public double NumericValue { get; private set; }

            public string StringValue { get; private set; }

            public void AddBlock(CaseBlock<string> caseBlock)
            {
                if (caseBlock != null)
                {
                    if (string.IsNullOrEmpty(StringValue))
                        throw new SwitchBlockException("String literal was expected but not provided in case.");
                    if (_stringBlocks.Any(cb => cb.Cases.Intersect(caseBlock.Cases).Any()))
                    {
                        var blockFound = _stringBlocks.First(cb => cb.Cases.Intersect(caseBlock.Cases).Any());
                        var dupCase = blockFound.Cases.Intersect(caseBlock.Cases);

                        throw new SwitchBlockException($"Multiple instances of cases with comparison of values \"{string.Join(',', dupCase)}\".");
                    }
                    _stringBlocks.Add(caseBlock);
                }
            }

            public void AddBlock(CaseBlock<double> caseBlock)
            {
                if (caseBlock != null)
                {
                    if (double.IsNaN(NumericValue))
                        throw new SwitchBlockException("An expression was expected but provided in case.");
                    if (_numericBlocks.Any(cb => cb.Cases.Intersect(caseBlock.Cases).Any()))
                    {
                        var blockFound = _numericBlocks.First(cb => cb.Cases.Intersect(caseBlock.Cases).Any());
                        var dupCase = blockFound.Cases.Intersect(caseBlock.Cases);
                        throw new SwitchBlockException($"Multiple instances of cases with comparison of values {string.Join(',', dupCase)}.");
                    }
                    _numericBlocks.Add(caseBlock);
                }
            }

            public bool AnyCaseDefined()
            {
                if (_stringBlocks.Count > 0)
                    return _stringBlocks.Sum(c => c.Cases.Count) > 0;
                if (_numericBlocks.Count > 0)
                    return _numericBlocks.Sum(c => c.Cases.Count) > 0;
                return false;
            }

            public int GetFallthroughIndex()
            {
                if (_stringBlocks.Count > 0)
                {
                    var cb = _stringBlocks.FirstOrDefault(cb => cb.Cases.Any(c => c.Equals(StringValue)));
                    if (cb != null)
                        return cb.FallthroughIndex;
                }
                else if (_numericBlocks.Count > 0)
                {
                    var cb = _numericBlocks.FirstOrDefault(cb => cb.Cases.Any(c => c.AlmostEquals(NumericValue)));
                    if (cb != null)
                        return cb.FallthroughIndex;
                }
                return -1;
            }
        }


        #endregion

        #region Constructors

        public SwitchBlock(SourceLine line, BlockType type)
            : base(line, type)
        {

        }

        #endregion

        #region Methods

        public override bool ExecuteDirective()
        {
            var line = Assembler.LineIterator.Current;
            if (!line.InstructionName.Equals(".switch"))
                return false;
            SwitchContext context = null;
            CaseBlock<string> stringBlock = null;
            CaseBlock<double> numericBlock = null;

            if (Line.OperandHasToken)
            {
                if (!Assembler.SymbolManager.SymbolExists(Line.OperandExpression))
                {
                    if (Line.Operand.Children[0].Name.Equals("("))
                    {
                        if (Line.Operand.Children[0].Children.Count > 0
                            && Line.Operand.Children[0].Children[0].ToString().EnclosedInDoubleQuotes())
                            context = new SwitchContext(Line.Operand.Children[0].Children[0].ToString());
                        else
                            context = new SwitchContext(Evaluator.Evaluate(Line.Operand));
                    }
                    else if (line.OperandExpression.Equals("*"))
                    {
                        context = new SwitchContext(Assembler.Output.LogicalPC);
                    }
                }
                else
                {
                    context = new SwitchContext(Evaluator.Evaluate(Line.Operand));
                }
            }
            if (context == null)
            {
                Assembler.Log.LogEntry(Line, Line.Operand, "Expression must be a valid symbol or constant expression.");
                return true;
            }
            Token lastInstruction = null;
            if (!(line.OperandHasToken &&
                (Assembler.SymbolManager.SymbolExists(line.Operand.Children[0].Children[0].Name) || 
                line.OperandExpression.Equals("*") ||
                line.Operand.Children[0].Name.Equals("("))))
            {
                string error;
                if (!line.OperandHasToken)
                    error = "Expression must follow \".switch\" directive.";
                else
                    error = "Expression must be a valid symbol or an expression.";
                Assembler.Log.LogEntry(line, line.Instruction.Position, error);
                return true;
            }
            var defaultIndex = -1;
            if (!string.IsNullOrEmpty(context.StringValue))
                stringBlock = new CaseBlock<string>();
            else
                numericBlock = new CaseBlock<double>();
            while ((line = Assembler.LineIterator.GetNext()) != null &&
                   !line.InstructionName.Equals(".endswitch"))
            {
                if (!string.IsNullOrEmpty(line.InstructionName))
                    lastInstruction = line.Instruction;
                if (line.InstructionName.Equals(".case"))
                {
                    if (defaultIndex > -1)
                    {
                        Assembler.Log.LogEntry(line, line.Instruction.Position, "\".case\" directive cannot follow a \".default\" directive.");
                    }
                    else if (stringBlock?.FallthroughIndex > -1 || numericBlock?.FallthroughIndex > -1)
                    {
                        Assembler.Log.LogEntry(line, line.Instruction.Position, "\".case\" directive must follow a \".break\" directive.");
                    }
                    {
                        stringBlock?.Cases.Add(line.Operand.Name);
                        numericBlock?.Cases.Add(Evaluator.Evaluate(line.Operand));
                    }
                }
                else if (line.InstructionName.Equals(".break"))
                {
                    if ((stringBlock?.Cases.Count == 0 || numericBlock?.Cases.Count == 0)
                        && defaultIndex < 0)
                    {
                        Assembler.Log.LogEntry(line, line.Instruction.Position,
                            "\".break\" directive must follow a \".case\" or \".default\" directive.");
                    }
                    else
                    {
                        context.AddBlock(stringBlock);
                        context.AddBlock(numericBlock);

                        if (stringBlock != null)
                            stringBlock = new CaseBlock<string>();
                        else
                            numericBlock = new CaseBlock<double>();
                    }
                }
                else if (line.InstructionName.Equals(".default"))
                {
                    if (defaultIndex > -1)
                        Assembler.Log.LogEntry(line, line.Instruction.Position,
                            "There can only be one \".default\" directive in a switch block.");
                    else
                        defaultIndex = Assembler.LineIterator.Index + 1;
                }
                else
                {
                    if (line.Label != null)
                    {
                        Assembler.Log.LogEntry(line, line.Label.Position, "Label cannot be defined inside a switch block.");
                    }
                    if (line.Instruction != null)
                    {
                        if ((stringBlock?.Cases.Count == 0 || numericBlock?.Cases.Count == 0) && defaultIndex < 0)
                        {
                            Assembler.Log.LogEntry(line, line.Instruction.Position, "\".case\" or \".default\" directive expected");
                        }
                        else if (stringBlock?.FallthroughIndex < 0 || numericBlock?.FallthroughIndex < 0)
                        {
                            if (stringBlock != null) stringBlock.FallthroughIndex = Assembler.LineIterator.Index;
                            if (numericBlock != null) numericBlock.FallthroughIndex = Assembler.LineIterator.Index;
                        }
                    }
                }
            }
            if (lastInstruction == null || (line != null && !string.IsNullOrEmpty(lastInstruction.Name) && !lastInstruction.Name.Equals(".break")))
            {
                if (line == null)
                    line = Line;
                Assembler.Log.LogEntry(line, "Switch statement must end with a \".break\" directive.");
            }
            else if (line != null)
            {
                if (defaultIndex < 0 || !context.AnyCaseDefined())
                {
                    if (defaultIndex >= 0)
                    {
                        Assembler.Log.LogEntry(line, line.Instruction, "Only a default case was specified.", false);
                    }
                    else if (!context.AnyCaseDefined())
                    {
                        Assembler.Log.LogEntry(line, line.Instruction, "Switch statement did not encounter any cases to evaluate.");
                        return true;
                    }
                    else
                    {
                        Assembler.Log.LogEntry(line, line.Instruction, "Switch statement does not have a default case.", false);
                    }
                }
                var fallthroughIndex = context.GetFallthroughIndex();
                if (fallthroughIndex < 0)
                    fallthroughIndex = defaultIndex;

                if (fallthroughIndex > -1)
                    Assembler.LineIterator.Rewind(fallthroughIndex - 1);
            }
            return true;
        }

        #endregion

        #region Properties

        public override bool AllowBreak => true;

        public override bool AllowContinue => false;

        #endregion
    }
}
