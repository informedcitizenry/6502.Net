//-----------------------------------------------------------------------------
// Copyright (c) 2017-2020 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System.Linq;

namespace Core6502DotNet
{
    /// <summary>
    /// A class responsible for processing assignment directives in assembly listing.
    /// </summary>
    public sealed class AssignmentAssembler : AssemblerBase
    {
        #region Constructors

        /// <summary>
        /// Constructs a new instance of the assignment assembler class.
        /// </summary>
        /// <param name="services">The shared <see cref="AssemblyServices"/> object.</param>
        public AssignmentAssembler(AssemblyServices services)
            :base(services)
        {
            Reserved.DefineType("Assignments", ".equ", ".global");

            Reserved.DefineType("Pseudo",
                ".relocate", ".pseudopc", ".endrelocate", ".realpc");

            Reserved.DefineType("Directives", ".let", ".org");

            ExcludedInstructionsForLabelDefines.Add(".org");
            ExcludedInstructionsForLabelDefines.Add(".equ");
            ExcludedInstructionsForLabelDefines.Add("=");
            ExcludedInstructionsForLabelDefines.Add(".global");
        }

        #endregion

        #region Methods

        protected override string OnAssembleLine(SourceLine line)
        {
            if (Reserved.IsOneOf("Assignments", line.InstructionName) ||
                line.InstructionName.Equals("="))
            {
                if (string.IsNullOrEmpty(line.LabelName))
                {
                    Services.Log.LogEntry(line, line.Label, "Invalid assignment expression.", true);
                }
                else
                {
                    var isGlobal = line.InstructionName.Equals(".global");
                    if (line.LabelName.Equals("*"))
                    {
                        if (isGlobal)
                            Services.Log.LogEntry(line, line.Instruction, "Invalid use of Program Counter in expression.", true);
                        else
                            Services.Output.SetPC((int)Services.Evaluator.Evaluate(line.Operand, short.MinValue, ushort.MaxValue));
                    }
                    else if (line.LabelName.Equals("+") || line.LabelName.Equals("-"))
                    {
                        if (isGlobal)
                        {
                            Services.Log.LogEntry(line, line.Instruction, "Invalid use of reference label in expression.", true);
                        }
                        else
                        {
                            var value = Services.Evaluator.Evaluate(line.Operand, short.MinValue, ushort.MaxValue);
                            Services.SymbolManager.DefineLineReference(line.LabelName, value);
                        }
                    }
                    else
                    {
                        if (isGlobal)
                        {
                            if (line.OperandHasToken)
                                Services.SymbolManager.DefineGlobal(line.Label, line.Operand, false);
                            else
                                Services.SymbolManager.DefineGlobal(line.LabelName, Services.Output.LogicalPC);
                        }
                        else
                        {
                            Services.SymbolManager.Define(line);
                        }
                    }
                }
            }
            else
            {
                switch (line.InstructionName)
                {
                    case ".let":
                        if (!line.OperandHasToken || line.Operand.Children[0].Children.Count < 3)
                            Services.Log.LogEntry(line, line.Instruction, $"Directive \".let\" expects an assignment expression.");
                        else if (line.Operand.Children.Count > 1)
                            Services.Log.LogEntry(line, line.Operand.LastChild, "Extra expression not valid.");
                        else
                            Services.SymbolManager.Define(line.Operand.Children[0].Children, true);
                        break;
                    case ".org":
                        if (line.LabelName.Equals("*"))
                            Services.Log.LogEntry(line, line.Label, "Program Counter symbol is redundant for \".org\" directive.", false);
                        Services.Output.SetPC((int)Services.Evaluator.Evaluate(line.Operand, short.MinValue, ushort.MaxValue));
                        SetLabel(line);
                        break;
                    case ".pseudopc":
                    case ".relocate":
                        Services.Output.SetLogicalPC((int)Services.Evaluator.Evaluate(line.Operand.Children, short.MinValue, ushort.MaxValue));
                        SetLabel(line);
                        break;
                    case ".endrelocate":
                    case ".realpc":
                        Services.Output.SynchPC();
                        break;
                }
            }
            if (Reserved.IsOneOf("Pseudo", line.InstructionName))
                return $".{Services.Output.LogicalPC,-8:x4}";
            var unparsedSource = Services.Options.NoSource ? string.Empty : line.UnparsedSource;
            if (!line.LabelName.Equals("*") && !Services.PassNeeded)
            {
                if (line.InstructionName.Equals(".org"))
                {
                    return string.Format(".{0}{1}",
                        Services.Output.LogicalPC.ToString("x4").PadRight(41),
                        unparsedSource);
                }
                string symbol;
                if (line.InstructionName.Equals(".let"))
                    symbol = line.Operand.Children[0].Children[0].Name;
                else
                    symbol = line.LabelName;
                if (Services.SymbolManager.SymbolIsScalar(symbol))
                {
                    if (Services.SymbolManager.SymbolIsNumeric(symbol))
                    {
                        var numValue = (long)Services.SymbolManager.GetNumericValue(symbol) & 0xFFFF_FFFF;
                        bool condition;
                        if (line.InstructionName.Equals(".let"))
                            condition = Services.Evaluator.ExpressionIsCondition(line.Operand.Children[0].Children.Skip(2));
                        else
                            condition = Services.Evaluator.ExpressionIsCondition(line.Operand.Children);
                        if (condition)
                            return string.Format("={0}{1}",
                                (numValue == 0 ? "false" : "true").PadRight(42), unparsedSource);
                        return string.Format("=${0}{1}",
                              numValue.ToString("x").PadRight(41),
                              unparsedSource);
                        
                    }
                    var elliptical = $"\"{Services.SymbolManager.GetStringValue(symbol).Elliptical(38)}\"";
                    return string.Format("={0}{1}", elliptical.PadRight(42), unparsedSource);
                }
            }
            return string.Empty;
        }

        void SetLabel(SourceLine line)
        {
            var labelName = line.LabelName;
            if (!string.IsNullOrEmpty(labelName) && !labelName.Equals("*"))
            {
                if (labelName.Equals("+") || labelName.Equals("-"))
                    Services.SymbolManager.DefineLineReference(labelName, Services.Output.LogicalPC);
                else
                    Services.SymbolManager.DefineSymbolicAddress(labelName);
            }
        }

        public override bool AssemblesLine(SourceLine line)
            => base.AssemblesLine(line) ||
               line.InstructionName.Equals("=");

        #endregion
    }
}