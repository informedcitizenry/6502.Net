//-----------------------------------------------------------------------------
// Copyright (c) 2017-2020 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

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
        public AssignmentAssembler()
        {
            Reserved.DefineType("Assignments", ".equ", ".global");

            Reserved.DefineType("Pseudo",
                ".relocate", ".pseudopc", ".endrelocate", ".realpc");

            Reserved.DefineType("Directives", ".let", ".org");
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
                    Assembler.Log.LogEntry(line, line.Label, "Invalid assignment expression.", true);
                }
                else
                {
                    if (line.LabelName.Equals("*"))
                    {
                        if (line.InstructionName.Equals(".global"))
                            Assembler.Log.LogEntry(line, line.Instruction, "Invalid use of Program Counter in expression.", true);
                        else
                            Assembler.Output.SetPC((int)Evaluator.Evaluate(line.Operand, short.MinValue, ushort.MaxValue));
                    }
                    else if (line.LabelName.Equals("+") || line.LabelName.Equals("-"))
                    {
                        if (line.InstructionName.Equals(".global"))
                        {
                            Assembler.Log.LogEntry(line, line.Instruction, "Invalid use of reference label in expression.", true);
                        }
                        else
                        {
                            var value = Evaluator.Evaluate(line.Operand, short.MinValue, ushort.MaxValue);
                            Assembler.SymbolManager.DefineLineReference(line.LabelName, value);
                        }
                    }
                    else
                    {
                        if (line.InstructionName.Equals(".global"))
                        {
                            if (line.OperandHasToken)
                                Assembler.SymbolManager.DefineGlobal(line.Label, line.Operand, false);
                            else
                                Assembler.SymbolManager.DefineGlobal(line.LabelName, Assembler.Output.LogicalPC);
                        }
                        else
                        {
                            Assembler.SymbolManager.Define(line);
                        }
                    }
                }
            }
            else if (line.InstructionName.Equals(".let"))
            {
                if (!line.OperandHasToken || line.Operand.Children[0].Children.Count < 3)
                    Assembler.Log.LogEntry(line, line.Instruction, $"Directive \".let\" expects an assignment expression.");
                else if (line.Operand.Children.Count > 1)
                    Assembler.Log.LogEntry(line, line.Operand.LastChild, "Extra expression not valid.");
                else
                    Assembler.SymbolManager.Define(line.Operand.Children[0].Children, true);
            }
            else if (line.InstructionName.Equals(".org"))
            {
                if (line.LabelName.Equals("*"))
                    Assembler.Log.LogEntry(line, line.Label, "Program Counter symbol is redundant for \".org\" directive.", false);

                Assembler.Output.SetPC((int)Evaluator.Evaluate(line.Operand, short.MinValue, ushort.MaxValue));
            }
            else if (Reserved.IsOneOf("Pseudo", line.InstructionName))
            {
                if (line.InstructionName.Equals(".pseudopc") || line.InstructionName.Equals(".relocate"))
                    Assembler.Output.SetLogicalPC((int)Evaluator.Evaluate(line.Operand.Children, short.MinValue, ushort.MaxValue));
                else
                    Assembler.Output.SynchPC();
            }
            if (Reserved.IsOneOf("Pseudo", line.InstructionName))
                return $".{Assembler.Output.LogicalPC,-8:x4}";
            if (!line.LabelName.Equals("*") && !Assembler.PassNeeded)
            {
                string symbol;
                if (line.InstructionName.Equals(".let"))
                    symbol = line.Operand.Children[0].Children[0].Name;
                else
                    symbol = line.LabelName;
                if (Assembler.SymbolManager.SymbolIsScalar(symbol))
                {
                    return string.Format("=${0}{1}",
                   ((int)Assembler.SymbolManager.GetNumericValue(symbol)).ToString("x").PadRight(41),
                   line.UnparsedSource);
                }
                return symbol;
            }
            return string.Empty;
        }

        public override bool AssemblesLine(SourceLine line)
            => base.AssemblesLine(line) ||
               line.InstructionName.Equals("=");

        #endregion
    }
}
