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
    public sealed class SwitchBlock : BlockProcessorBase
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
        /// <param name="services">The shared <see cref="AssemblyServices"/> object.</param>
        /// <param name="index">The index at which the block is defined.</param>
        public SwitchBlock(AssemblyServices services,
                           int index)
            : base(services, index)
        {
            Reserved.DefineType("BreakContReturn", ".break", ".continue", ".return");
        }

        #endregion

        #region Methods

        public override void ExecuteDirective(RandomAccessIterator<SourceLine> lines)
        {
            var line = lines.Current;
            if (line.Instruction.Name.Equals(".endswitch", Services.StringComparison))
                return;
            CaseBlock<string> stringBlock = null;
            CaseBlock<double> numericBlock = null;
            SwitchContext context = null;
            var it = line.Operands.GetIterator();
            if (it.MoveNext())
            {
                if (StringHelper.ExpressionIsAString(it, Services))
                    context = new SwitchContext(StringHelper.GetString(it, Services));
                else
                    context = new SwitchContext(Services.Evaluator.Evaluate(it, false));
            }
            if (context == null)
            {
                string error;
                if (line.Operands.Count == 0)
                    error = "Expression must follow \".switch\" directive.";
                else
                    error = "Expression must be a valid symbol or an expression.";
                Services.Log.LogEntry(line.Filename, line.LineNumber, line.Instruction.Position, error);
                return;
            }
            var defaultIndex = -1;
            if (!string.IsNullOrEmpty(context.StringValue))
                stringBlock = new CaseBlock<string>();
            else
                numericBlock = new CaseBlock<double>();
            while ((line = lines.GetNext()) != null &&
                    (line.Instruction == null || !line.Instruction.Name.Equals(".endswitch", Services.StringComparison)))
            {
                if (line.Instruction != null)
                {
                    if (line.Instruction.Name.Equals(".case", Services.StringComparison))
                    {
                        if (defaultIndex > -1)
                        {
                            Services.Log.LogEntry(line.Filename, line.LineNumber, line.Instruction.Position,
                                "\".case\" directive cannot follow a \".default\" directive.");
                        }
                        else if (stringBlock?.FallthroughIndex > -1 || numericBlock?.FallthroughIndex > -1)
                        {
                            Services.Log.LogEntry(line.Filename, line.LineNumber, line.Instruction.Position,
                                "\".case\" does not fall through.");
                        }
                        else if (line.Operands.Count == 0)
                        {
                            Services.Log.LogEntry(line.Filename, line.LineNumber, line.Instruction.Position,
                                "Expression expected.");
                        }
                        else
                        {
                            if (stringBlock != null)
                            {
                                if (!StringHelper.ExpressionIsAString(line.Operands.GetIterator(), Services))
                                    Services.Log.LogEntry(line.Filename, line.LineNumber, line.Operands[0].Position,
                                        "String expression expected.");
                                else
                                    stringBlock.Cases.Add(StringHelper.GetString(line.Operands.GetIterator(), Services));
                            }
                            else
                            {
                                numericBlock?.Cases.Add(Services.Evaluator.Evaluate(line.Operands.GetIterator()));
                            }
                        }
                    }
                    else if (Reserved.IsOneOf("BreakContReturn", line.Instruction.Name))
                    {
                        if ((stringBlock?.Cases.Count == 0 || numericBlock?.Cases.Count == 0)
                            && defaultIndex < 0)
                        {
                            Services.Log.LogEntry(line.Filename, line.LineNumber, line.Instruction.Position,
                                $"\"{line.Instruction}\" directive must follow a \".case\" or \".default\" directive.");
                        }
                        else
                        {
                            if (line.Instruction.Name.Equals(".return", Services.StringComparison) &&
                                (stringBlock?.FallthroughIndex < 0 || numericBlock?.FallthroughIndex < 0))
                            {
                                if (stringBlock != null) stringBlock.FallthroughIndex = lines.Index;
                                if (numericBlock != null) numericBlock.FallthroughIndex = lines.Index;
                            }
                            context.AddBlock(stringBlock);
                            context.AddBlock(numericBlock);
                            Services.SymbolManager.PopScope();
                            if (stringBlock != null)
                                stringBlock = new CaseBlock<string>();
                            else
                                numericBlock = new CaseBlock<double>();
                        }
                    }
                    else if (line.Instruction.Name.Equals(".default", Services.StringComparison))
                    {
                        if (defaultIndex > -1)
                            Services.Log.LogEntry(line.Filename, line.LineNumber, line.Instruction.Position,
                                "There can only be one \".default\" directive in a switch block.");
                        else
                            defaultIndex = lines.Index + 1;
                    }
                    else if (line.Label != null)
                    {
                        Services.Log.LogEntry(line.Filename, line.LineNumber, line.Label.Position,
                            "Label cannot be defined inside a switch block.");
                    }
                    else
                    {
                        if ((stringBlock?.Cases.Count == 0 || numericBlock?.Cases.Count == 0) && defaultIndex < 0)
                        {
                            Services.Log.LogEntry(line.Filename, line.LineNumber, line.Instruction.Position, "\".case\" or \".default\" directive expected");
                        }
                        else if (stringBlock?.FallthroughIndex < 0 || numericBlock?.FallthroughIndex < 0)
                        {
                            if (stringBlock != null) stringBlock.FallthroughIndex = lines.Index;
                            if (numericBlock != null) numericBlock.FallthroughIndex = lines.Index;
                        }
                    }
                }
            }
            if (line != null)
            {
                if (defaultIndex < 0 || !context.AnyCaseDefined())
                {
                    if (defaultIndex >= 0)
                    {
                        Services.Log.LogEntry(line.Filename, line.LineNumber, line.Instruction.Position,
                            "Only a default case was specified.", false);
                    }
                    else if (!context.AnyCaseDefined())
                    {
                        Services.Log.LogEntry(line.Filename, line.LineNumber, line.Instruction.Position,
                            "Switch statement did not encounter any cases to evaluate.");
                        return;
                    }
                    else
                    {
                        Services.Log.LogEntry(line.Filename, line.LineNumber, line.Instruction.Position,
                            "Switch statement does not have a default case.", false);
                    }
                }
                var fallthroughIndex = context.GetFallthroughIndex();
                if (fallthroughIndex < 0)
                    fallthroughIndex = defaultIndex;

                if (fallthroughIndex > -1)
                    lines.Rewind(fallthroughIndex - 1);
                Services.SymbolManager.PushScope(lines.Index.ToString());
            }
        }

        #endregion

        #region Properties

        public override bool AllowBreak => true;

        public override bool AllowContinue => false;

        public override IEnumerable<string> BlockOpens => new string[] { ".switch " };

        public override string BlockClosure => ".endswitch";

        #endregion
    }
}
