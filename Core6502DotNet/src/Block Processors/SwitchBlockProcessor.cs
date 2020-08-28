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
            readonly List<CaseBlock<string>> _stringBlocks;
            readonly List<CaseBlock<double>> _numericBlocks;

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

        /// <summary>
        /// Creates a new instance of a switch block processor.
        /// </summary>
        /// <param name="line">The <see cref="SourceLine"/> containing the instruction
        /// and operands invoking or creating the block.</param>
        /// <param name="type">The <see cref="BlockType"/>.</param>
        public SwitchBlock(SourceLine line, BlockType type)
            : base(line, type, false)
        {

        }

        /// <summary>
        /// Creates a new instance of a switch block processor.
        /// </summary>
        /// <param name="iterator">The <see cref="SourceLine"/> containing the instruction
        /// and operands invoking or creating the block.</param>
        /// <param name="type">The <see cref="BlockType"/>.</param>
        public SwitchBlock(RandomAccessIterator<SourceLine> iterator,
                           BlockType type)
            : base(iterator, type, false)
        {

        }

        #endregion

        #region Methods

        public override bool ExecuteDirective()
        {
            var line = LineIterator.Current;
            if (line.InstructionName.Equals(".endswitch"))
                return true;
            if (!line.InstructionName.Equals(".switch"))
                return false;
            CaseBlock<string> stringBlock = null;
            CaseBlock<double> numericBlock = null;
            var noParens = line.OperandExpression.TrimStart('(').TrimEnd(')');
            SwitchContext context;
            if (Line.OperandHasToken)
            {
                if (!Assembler.SymbolManager.SymbolExists(noParens))
                {
                    if (noParens.EnclosedInDoubleQuotes())
                        context = new SwitchContext(noParens);
                    else
                        context = new SwitchContext(Evaluator.Evaluate(Line.Operand));
                }
                else
                {
                    context = new SwitchContext(Evaluator.Evaluate(Line.Operand));
                }
            }
            else
            {
                Assembler.Log.LogEntry(Line, Line.Instruction.Position, "Expression must follow \".switch\" directive.");
                return true;
            }
            if (context == null)
            {
                string error;
                if (!line.OperandHasToken)
                    error = "Expression must follow \".switch\" directive.";
                else
                    error = "Expression must be a valid symbol or an expression.";
                Assembler.Log.LogEntry(line, line.Instruction.Position, error);
                return true;
            }
            Token lastInstruction = null;
            var defaultIndex = -1;
            if (!string.IsNullOrEmpty(context.StringValue))
                stringBlock = new CaseBlock<string>();
            else
                numericBlock = new CaseBlock<double>();
            while ((line = LineIterator.GetNext()) != null && 
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
                        Assembler.Log.LogEntry(line, line.Instruction.Position, 
                            "\".case\" directive must follow a \".break\" or \".return\" directive.");
                    }
                    stringBlock?.Cases.Add(line.Operand.Name);
                    numericBlock?.Cases.Add(Evaluator.Evaluate(line.Operand));
                }
                else if (line.InstructionName.Equals(".break") || line.InstructionName.Equals(".return"))
                {
                    if ((stringBlock?.Cases.Count == 0 || numericBlock?.Cases.Count == 0)
                        && defaultIndex < 0)
                    {
                        Assembler.Log.LogEntry(line, line.Instruction.Position,
                            $"\"{line.Instruction}\" directive must follow a \".case\" or \".default\" directive.");
                    }
                    else
                    {
                        if (line.InstructionName.Equals(".return") && (stringBlock?.FallthroughIndex < 0 || numericBlock?.FallthroughIndex < 0))
                        {
                            if (stringBlock != null) stringBlock.FallthroughIndex = LineIterator.Index;
                            if (numericBlock != null) numericBlock.FallthroughIndex = LineIterator.Index;
                        }
                        context.AddBlock(stringBlock);
                        context.AddBlock(numericBlock);
                        Assembler.SymbolManager.PopScope();
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
                        defaultIndex = LineIterator.Index + 1;
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
                            if (stringBlock != null) stringBlock.FallthroughIndex = LineIterator.Index;
                            if (numericBlock != null) numericBlock.FallthroughIndex = LineIterator.Index;
                        }
                    }
                }
            }
            if (lastInstruction == null || 
                (line != null && 
                 !string.IsNullOrEmpty(lastInstruction.Name) && 
                 !lastInstruction.Name.Equals(".break") &&
                 !lastInstruction.Name.Equals(".return")
                )
               )
            {
                if (line == null)
                    line = Line;
                Assembler.Log.LogEntry(line, "Switch statement must end with a \".break\" or \".return\" directive.");
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
                    LineIterator.Rewind(fallthroughIndex - 1);
                Assembler.SymbolManager.PushScope(LineIterator.Index.ToString());
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
