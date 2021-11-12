//-----------------------------------------------------------------------------
// Copyright (c) 2017-2021 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;

namespace Core6502DotNet
{
    /// <summary>
    /// A class responsible for processing .switch/.endswitch blocks.
    /// </summary>
    public sealed class SwitchBlock : BlockProcessorBase
    {
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
            Reserved.DefineType("Directives", ".switch", ".endswitch");
            Reserved.DefineType("CaseKeywords", ".case", ".default");
            Reserved.DefineType("NonCaseKeywords", ".break", ".continue", ".return");
        }

        #endregion

        #region Methods

        public override void ExecuteDirective(RandomAccessIterator<SourceLine> lines)
        {
            var line = lines.Current;
            var it = line.Operands.GetIterator();
            var stringCases = new Dictionary<string, int>();
            var numCases = new Dictionary<double, int>();
            string stringMatch = null, stringCase = stringMatch;
            double numMatch = double.NaN, numCase = numMatch;
            if (!it.MoveNext())
            {
                if (line.Instruction.Name.Equals(".endswitch", Services.StringViewComparer))
                    return;
                if (line.Instruction.Name.Equals(".switch", Services.StringViewComparer))
                    Services.Log.LogEntry(line.Instruction, "Argument expected for \".switch\".");
                return;
            }
            if (it.Current != null)
            {
                if (StringHelper.ExpressionIsAString(it, Services))
                    stringMatch = StringHelper.GetString(it, Services);
                else
                    numMatch = Services.Evaluator.Evaluate(it, false);
            }
            if (it.Current != null)
                Services.Log.LogEntry(it.Current, "Unexpected expression.", false, true);
            var defaultIndex = -1;
            var caseFound = false;
            var lineProcessed = false;
            var breakFound = true;
            while (lines.MoveNext() && (lines.Current.Instruction == null || !lines.Current.Instruction.Name.Equals(".endswitch", Services.StringViewComparer)))
            {
                line = lines.Current;
                if (line.Instruction != null)
                {
                    if (!Reserved.IsOneOf("CaseKeywords", line.Instruction.Name))
                    {
                        if (!caseFound)
                        {
                            Services.Log.LogEntry(line.Instruction,
                               "Expected a \".case\" or \".default\" directive.");
                        }
                        else
                        {
                            breakFound = Reserved.IsOneOf("NonCaseKeywords", line.Instruction.Name);
                            lineProcessed |= !Reserved.IsReserved(line.Instruction.Name);
                            if (!breakFound && Reserved.IsOneOf("Directives", line.Instruction.Name) &&
                                line.Instruction.Name.Equals(".switch", Services.StringViewComparer))
                                SeekBlockEnd(lines);
                        }
                    }
                    else
                    {
                        caseFound = true;
                        if (!breakFound)
                            throw new SyntaxException(lines.Current.Instruction, "Expected a \".break\" directive.");
                        lineProcessed = false;
                        var nextIt = new RandomAccessIterator<SourceLine>(lines, false);
                        var nextInstr = nextIt.First(l => l.Instruction != null).Instruction.Name;
                        breakFound = Reserved.IsOneOf("CaseKeywords", nextInstr);
                        if (breakFound)
                            _ = nextIt.First(l => l.Instruction != null && !Reserved.IsOneOf("CaseKeywords", l.Instruction.Name));
                        it = line.Operands.GetIterator();
                        if (line.Instruction.Name.Equals(".default", Services.StringViewComparer))
                        {
                            if (it.MoveNext())
                                Services.Log.LogEntry(line.Operands[0], "Unexpected expression.", false, true);
                            else if (defaultIndex > -1)
                                Services.Log.LogEntry(line.Instruction, "Default case already defined.");
                            else
                                defaultIndex = nextIt.Index;
                        }
                        else if (!it.MoveNext())
                        {
                            Services.Log.LogEntry(line.Instruction, "Expression expected.");
                        }
                        else
                        {
                            if (stringMatch != null)
                            {
                                if (!StringHelper.ExpressionIsAString(it, Services))
                                    Services.Log.LogEntry(line.Operands[0], "String expression expected.", false, true);
                                else
                                    stringCase = StringHelper.GetString(it, Services);
                            }
                            else
                            {
                                if (StringHelper.ExpressionIsAString(it, Services))
                                    Services.Log.LogEntry(line.Operands[0], "Numeric expression expected.");
                                else
                                    numCase = Services.Evaluator.Evaluate(it, false);
                            }
                            if (it.Current != null)
                                Services.Log.LogEntry(it.Current, "Unexpected expression.", false, true);
                            else if (!double.IsNaN(numCase) || stringCase != null)
                            {
                                if ((stringCase != null && stringCases.ContainsKey(stringCase)) ||
                                (!double.IsNaN(numCase) && numCases.ContainsKey(numCase)))
                                {
                                    Services.Log.LogEntry(line.Operands[0], "Matching case previously defined.", false, true);
                                }
                                else
                                {
                                    if (stringCase != null)
                                        stringCases[stringCase] = nextIt.Index;
                                    else
                                        numCases[numCase] = nextIt.Index;
                                }
                                stringCase = null;
                                numCase = double.NaN;
                            }
                        }
                    }
                }
            }
            if (caseFound && !lineProcessed && !breakFound)
                throw new SyntaxException(lines.Current.Instruction, "Directive \".endswitch\" terminates a case before it falls through.");
            if (defaultIndex == -1)
                Services.Log.LogEntry(lines.Current.Instruction, "A default case was not defined.", false);
            var lineIndex = lines.Index;
            if ((stringMatch != null && !stringCases.TryGetValue(stringMatch, out lineIndex)) ||
                !numCases.TryGetValue(numMatch, out lineIndex))
                lineIndex = defaultIndex;
            if (lineIndex > -1)
                lines.Rewind(lineIndex - 1);
        }

        #endregion

        #region Properties

        public override bool AllowBreak => true;

        public override bool AllowContinue => false;

        public override IEnumerable<string> BlockOpens => new string[] { ".switch" };

        public override string BlockClosure => ".endswitch";

        #endregion
    }
}