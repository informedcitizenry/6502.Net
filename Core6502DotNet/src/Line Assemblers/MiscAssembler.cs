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
        /// <param name="services">The shared <see cref="AssemblyServices"/> object.</param>
        public MiscAssembler(AssemblyServices services)
            :base(services)
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
                Services.Log.LogEntry(line, line.Operand, $"Missing arguments for directive \"{line.InstructionName}\".");
            }
            else if (line.Operand.Children.Count > 2 || !line.Operand.Children[1].Children[0].ToString().Trim().EnclosedInDoubleQuotes())
            {
                Services.Log.LogEntry(line, line.Operand, $"Argument error for directive \"{line.InstructionName}\".");
            }
            else if (Services.Evaluator.EvaluateCondition(line.Operand.Children[0].Children))
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
                Services.Log.LogEntry(line, line.Instruction, "Too few arguments for directive \".eor\".");
                return;
            }
            var eor = Services.Evaluator.Evaluate(line.Operand.Children, sbyte.MinValue, byte.MaxValue);
            var eor_b = Convert.ToByte(eor);
            Services.Output.Transform = (delegate (byte b)
            {
                b ^= eor_b;
                return b;
            });
        }

        void SetBank(SourceLine line)
        {
            if (!line.OperandHasToken)
                Services.Log.LogEntry(line, "Too few arguments for directive \".bankmode\".");
            else
                Services.Output.SetBank((int)Services.Evaluator.Evaluate(line.Operand, sbyte.MinValue, byte.MaxValue));
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
                        StringHelper.ExpressionIsAString(line.Operand.Children[0], Services.SymbolManager))
                    {
                        Output(line, line.Operand.Children[0]);
                    }
                    else if (instruction.Equals(".echo"))
                    {
                        if (Services.Evaluator.ExpressionIsCondition(line.Operand.Children))
                            Output(line, Services.Evaluator.EvaluateCondition(line.Operand).ToString());
                        else
                            Output(line, Services.Evaluator.Evaluate(line.Operand).ToString());
                    }
                    else
                    {
                        Services.Log.LogEntry(line, line.Operand,
                            "String expression expected.");
                    }
                    break;
                case ".eor":
                    SetEor(line);
                    break;
                case ".invoke":
                    InvokeFunction(line);
                    break;
                case ".pron":
                    Services.PrintOff = false;
                    break;
                case ".proff":
                    Services.PrintOff = true;
                    break;
                case ".dsection":
                    DefineSection(line);
                    break;
                case ".section":
                    return SetSection(line);
                case ".format":
                case ".target":
                    if (Services.Output.HasOutput)
                        Services.Log.LogEntry(line, line.Instruction, "Cannot specify target format after assembly has started.");
                    else if (!line.OperandHasToken || !line.OperandExpression.Trim().EnclosedInDoubleQuotes())
                        Services.Log.LogEntry(line, line.Operand, "Expression must be a string.");
                    else if (!string.IsNullOrEmpty(Services.OutputFormat))
                        Services.Log.LogEntry(line, line.Operand, "Output format was previously specified.");
                    else
                        Services.SelectFormat(line.OperandExpression.Trim().TrimOnce('"'));
                    if (instruction.Equals(".target"))
                        Services.Log.LogEntry(line, line.Instruction, "\".target\" is deprecated. Use \".format\" instead.", false);
                    break;
                default:
                    InitMem(line);
                    break;
            }
            return string.Empty;
        }

        void DefineSection(SourceLine line)
        {
            if (!string.IsNullOrEmpty(line.LabelName))
                throw new SyntaxException(line.Label.Position, "Label definitions are not allowed for directive \".dsection\".");
            if (Services.CurrentPass == 0)
            {
                var parms = line.Operand.Children;
                if (parms.Count < 3)
                    throw new SyntaxException(line.Operand.Position, 
                        "Section definition missing one or more parameters.");
                if (parms.Count > 3)
                    throw new SyntaxException(line.Operand.LastChild.Position,
                        $"Unexpected parameter \"{parms[3]}\" in section definition.");

                var name = parms[0].ToString().Trim();
                if (!name.EnclosedInDoubleQuotes())
                    throw new SyntaxException(parms[0].Position, "Section name must be a string.");
                var starts = Convert.ToInt32(Services.Evaluator.Evaluate(parms[1]));
                var ends = Convert.ToInt32(Services.Evaluator.Evaluate(parms[2]));
                
                Services.Output.DefineSection(name, starts, ends);
            }
        }

        void InvokeFunction(SourceLine line)
        {
            if (!line.OperandHasToken || line.Operand.Children[0].Children.Count == 0)
                Services.Log.LogEntry(line, line.Operand, "Missing function name from invocation directive.");
            else if (line.Operand.Children[0].Children.Count < 2)
                Services.Log.LogEntry(line, line.Operand, "Missing function parameters.");
            else if (line.Operand.Children[0].Children.Count > 2)
                Services.Log.LogEntry(line, line.Operand.Children[0].Children[2],
                    "Unexpected expression.");
            else
                Services.Evaluator.Invoke(line.Operand.Children[0].Children[0], line.Operand.Children[0].Children[1]);
        }

        string SetSection(SourceLine line)
        {
            if (!line.OperandExpression.EnclosedInDoubleQuotes())
                throw new SyntaxException(line.Operand.Position, "Directive expects a string expression.");
            if (!Services.Output.SetSection(line.Operand))
                throw new SyntaxException(line.Operand.Position, $"Section {line.Operand} not defined.");
            if (!string.IsNullOrEmpty(line.LabelName))
            {
                if (line.LabelName.Equals("*"))
                    Services.Log.LogEntry(line, "Redundant assignment of program counter for directive \".section\".", false);
                else if (line.LabelName[0].IsSpecialOperator())
                    Services.SymbolManager.DefineLineReference(line.LabelName, Services.Output.LogicalPC);
                else
                    Services.SymbolManager.DefineSymbolicAddress(line.LabelName);
                return $"=${Services.Output.LogicalPC:x4}                  {line.LabelName}  // section {line.Operand}";
            }
            return $"* = ${Services.Output.LogicalPC:x4}  // section {line.Operand}";
        }

        void InitMem(SourceLine line)
        {
            if (!line.OperandHasToken)
            {
                Services.Log.LogEntry(line, line.Instruction, "Expected expression.");
            }
            else if (line.Operand.Children.Count > 1)
            {
                Services.Log.LogEntry(line, line.Operand.Children[1].Position, "Too many arguments for instruction.");
            }
            else
            {
                var amount = Services.Evaluator.Evaluate(line.Operand.Children, sbyte.MinValue, byte.MaxValue);
                Services.Output.InitMemory(Convert.ToByte((int)amount & 0xFF));
            }
        }

        void Output(SourceLine line, string output)
        {
            if (!Services.PassNeeded)
            {
                var type = line.InstructionName.Substring(0, 5);
                switch (type)
                {
                    case ".echo":
                        Console.WriteLine(output);
                        break;
                    case ".warn":
                        Services.Log.LogEntry(line, line.Operand, output, false);
                        break;
                    default:
                        Services.Log.LogEntry(line, line.Operand, output);
                        break;
                }
            }
        }

        void Output(SourceLine line, Token operand)
        {
            if (StringHelper.ExpressionIsAString(operand, Services.SymbolManager))
                Output(line, StringHelper.GetString(operand, Services.SymbolManager, Services.Evaluator));
            else
                Output(line, Services.Evaluator.Evaluate(operand).ToString());
        }

        void DoAssert(SourceLine line)
        {
            if (line.Operand.Children.Count == 0)
            {
                Services.Log.LogEntry(line, line.Operand, "One or more arguments expected for assertion directive.");
            }
            else if (line.Operand.Children.Count > 2)
            {
                Services.Log.LogEntry(line, line.Operand.Children[2], "Unexpected expression found.");
            }
            else if (!Services.Evaluator.EvaluateCondition(line.Operand.Children[0]))
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