//-----------------------------------------------------------------------------
// Copyright (c) 2017-2020 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;

namespace Core6502DotNet
{
    /// <summary>
    /// A class that assembles miscellaneous directives, such as error and warn messages.
    /// </summary>
    public sealed class MiscAssembler : AssemblerBase
    {
        #region Constructors

        /// <summary>
        /// Constructs a DotNetAsm.MiscAssembler class.
        /// </summary>
        public MiscAssembler()
        {
            Reserved.DefineType("Directives",
                    ".assert", ".bank", ".end",
                    ".eor", ".echo", ".format",
                    ".invoke",
                    ".initmem", ".target",
                    ".error", ".errorif",
                    ".pron", ".proff",
                    ".warnif", ".warn", 
                    ".dsection", ".section"
                );
            ExcludedInstructionsForLabelDefines.Add(".section");
        }

        #endregion

        #region Methods

        void ThrowConditional(SourceLine line)
        {
            if (line.Operand.Children.Count < 2 ||
                line.Operand.Children[1].Children.Count == 0)
            {
                Assembler.Log.LogEntry(line, line.Operand, $"Missing arguments for directive \"{line.InstructionName}\".");
            }
            else if (line.Operand.Children.Count > 2 || !line.Operand.Children[1].Children[0].ToString().EnclosedInDoubleQuotes())
            {
                Assembler.Log.LogEntry(line, line.Operand, $"Argument error for directive \"{line.InstructionName}\".");
            }
            else if (Evaluator.EvaluateCondition(line.Operand.Children[0].Children))
            {
                if (line.Operand.Children.Count > 1)
                    Output(line, line.Operand.Children[1]);
                else
                    Output(line, $"Expression \"{line.UnparsedSource.Substring(line.Operand.Position)}\" evaluated to true.");
            }
        }

        void SetEor(SourceLine line)
        {
            if (!line.OperandHasToken)
            {
                Assembler.Log.LogEntry(line, line.Instruction, "Too few arguments for directive \".eor\".");
                return;
            }
            var eor = Evaluator.Evaluate(line.Operand.Children, sbyte.MinValue, byte.MaxValue);
            var eor_b = Convert.ToByte(eor);
            Assembler.Output.Transform = (delegate (byte b)
            {
                b ^= eor_b;
                return b;
            });
        }

        void SetBank(SourceLine line)
        {
            if (!line.OperandHasToken)
                Assembler.Log.LogEntry(line, "Too few arguments for directive \".bankmode\".");
            else
                Assembler.Output.SetBank((int)Evaluator.Evaluate(line.Operand, sbyte.MinValue, byte.MaxValue));
        }

        protected override string OnAssembleLine(SourceLine line)
        {
            var instruction = line.InstructionName;
            switch (instruction)
            {
                case ".assert":
                    DoAssert(line);
                    break;
                case ".bank":
                    SetBank(line);
                    break;
                case ".warnif":
                case ".errorif":
                    ThrowConditional(line);
                    break;
                case ".echo":
                case ".error":
                case ".warn":
                    if (line.OperandHasToken && line.Operand.Children.Count == 1 && 
                        StringHelper.ExpressionIsAString(line.Operand.Children[0]))
                        Output(line, line.Operand.Children[0]);
                    else
                        Assembler.Log.LogEntry(line, line.Operand, "String expression expected.");
                    break;
                case ".eor":
                    SetEor(line);
                    break;
                case ".invoke":
                    InvokeFunction(line);
                    break;
                case ".pron":
                    Assembler.PrintOff = false;
                    break;
                case ".proff":
                    Assembler.PrintOff = true;
                    break;
                case ".dsection":
                    if (!string.IsNullOrEmpty(line.LabelName))
                        throw new SyntaxException(line.Label.Position, "Label definitions are not allowed for directive \".dsection\".");
                    if (Assembler.CurrentPass == 0)
                        Assembler.Output.DefineSection(line.Operand);
                    break;
                case ".section":
                    return SetSection(line);
                case ".format":
                case ".target":
                    if (!line.OperandHasToken || !line.OperandExpression.EnclosedInDoubleQuotes())
                        Assembler.Log.LogEntry(line, line.Operand, "Expression must be a string.");
                    else if (!string.IsNullOrEmpty(Assembler.OutputFormat))
                        Assembler.Log.LogEntry(line, line.Operand, "Output format was previously specified.");
                    else
                        Assembler.SelectFormat(line.OperandExpression.TrimOnce('"'));
                    if (instruction.Equals(".target"))
                        Assembler.Log.LogEntry(line, line.Instruction, "\".target\" is deprecated. Use \".format\" instead.", false);
                    break;
                default:
                    InitMem(line);
                    break;
            }
            return string.Empty;
        }

        void InvokeFunction(SourceLine line)
        {
            if (!line.OperandHasToken || line.Operand.Children[0].Children.Count == 0)
                Assembler.Log.LogEntry(line, line.Operand, "Missing function name from invocation directive.");
            else if (line.Operand.Children[0].Children.Count < 2)
                Assembler.Log.LogEntry(line, line.Operand, "Missing function parameters.");
            else if (line.Operand.Children[0].Children.Count > 2)
                Assembler.Log.LogEntry(line, line.Operand.Children[0].Children[2],
                    "Unexpected expression.");
            else
                Evaluator.Invoke(line.Operand.Children[0].Children[0], line.Operand.Children[0].Children[1]);
        }

        string SetSection(SourceLine line)
        {
            if (!line.Operand.ToString().EnclosedInDoubleQuotes())
                throw new SyntaxException(line.Operand.Position, "Directive expects a string expression.");
            if (!Assembler.Output.SetSection(line.Operand))
                throw new SyntaxException(line.Operand.Position, $"Section {line.Operand} not defined.");
            if (!string.IsNullOrEmpty(line.LabelName))
            {
                if (line.LabelName.Equals("*"))
                    Assembler.Log.LogEntry(line, "Redundant assignment of program counter for directive \".section\".", false);
                else if (line.LabelName[0].IsSpecialOperator())
                    Assembler.SymbolManager.DefineLineReference(line.LabelName, Assembler.Output.LogicalPC);
                else
                    Assembler.SymbolManager.DefineSymbolicAddress(line.LabelName);
                return $"=${Assembler.Output.LogicalPC:x4}                  {line.LabelName}  // section {line.Operand}";
            }
            return $"* = ${Assembler.Output.LogicalPC:x4}  // section {line.Operand}";
        }

        void InitMem(SourceLine line)
        {
            if (!line.OperandHasToken)
            {
                Assembler.Log.LogEntry(line, line.Instruction, "Expected expression.");
            }
            else if (line.Operand.Children.Count > 1)
            {
                Assembler.Log.LogEntry(line, line.Operand.Children[1].Position, "Too many arguments for instruction.");
            }
            else
            {
                var amount = Evaluator.Evaluate(line.Operand.Children, sbyte.MinValue, byte.MaxValue);
                Assembler.Output.InitMemory(Convert.ToByte((int)amount & 0xFF));
            }
        }

        void Output(SourceLine line, string output)
        {
            if (!Assembler.PassNeeded)
            {
                var type = line.InstructionName.Substring(0, 5);
                switch (type)
                {
                    case ".echo":
                        Console.WriteLine(output);
                        break;
                    case ".warn":
                        Assembler.Log.LogEntry(line, line.Operand, output, false);
                        break;
                    default:
                        Assembler.Log.LogEntry(line, line.Operand, output);
                        break;
                }
            }
        }

        void Output(SourceLine line, Token operand)
        {
            if (StringHelper.ExpressionIsAString(operand))
                Output(line, StringHelper.GetString(operand));
            else
                Output(line, Evaluator.Evaluate(operand).ToString());
        }

        void DoAssert(SourceLine line)
        {
            if (line.Operand.Children.Count == 0)
            {
                Assembler.Log.LogEntry(line, line.Operand, "One or more arguments expected for assertion directive.");
            }
            else if (line.Operand.Children.Count > 2)
            {
                Assembler.Log.LogEntry(line, line.Operand.Children[2], "Unexpected expression found.");
            }
            else if (!Evaluator.EvaluateCondition(line.Operand.Children[0]))
            {
                if (line.Operand.Children.Count > 1)
                    Output(line, line.Operand.Children[1]);
                else
                    Output(line, "Assertion failed.");
            }
        }

        #endregion
    }
}