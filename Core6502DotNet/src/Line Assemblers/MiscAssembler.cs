//-----------------------------------------------------------------------------
// Copyright (c) 2017-2020 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;
using System.Linq;

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
                    ".assert", ".bank",
                    ".eor", ".echo", ".format",
                    ".initmem", ".target",
                    ".error", ".errorif",
                    ".pron", ".proff",
                    ".warnif", ".warn"
                );
        }

        #endregion

        #region Methods

        void ThrowConditional(SourceLine line)
        {
            if (line.Operand.Children == null ||
                line.Operand.Children.Count < 2 ||
                !line.Operand.Children[1].HasChildren)
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
                case ".pron":
                    Assembler.PrintOff = false;
                    break;
                case ".proff":
                    Assembler.PrintOff = true;
                    break;
                case ".format":
                case ".target":
                    if (!line.OperandHasToken || !line.OperandExpression.EnclosedInQuotes())
                        Assembler.Log.LogEntry(line, line.Operand, "Expression must be a string.");
                    else
                        Assembler.Options.Format = line.OperandExpression.TrimOnce('"');
                    if (instruction.Equals(".target"))
                        Assembler.Log.LogEntry(line, line.Instruction, "\".target\" is deprecated. Use \".format\" instead.", false);
                    break;
                default:
                    InitMem(line);
                    break;
            }
            return string.Empty;
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
            if (line.Operand.Children == null || !line.Operand.HasChildren)
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