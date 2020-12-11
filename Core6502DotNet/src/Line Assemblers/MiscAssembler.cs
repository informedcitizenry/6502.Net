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
            : base(services)
        {
            Reserved.DefineType("Directives",
                    ".assert", ".bank", ".end",
                    ".eor", ".echo", ".forcepass",
                    ".format", ".invoke",
                    ".initmem", ".target",
                    ".error", ".errorif",
                    ".pron", ".proff",
                    ".warnif", ".warn",
                    ".dsection", ".section"
                );
            ExcludedInstructionsForLabelDefines.Add(".section");
            Services.PassChanged += (s, a) => Services.PrintOff = false;
        }

        #endregion

        #region Methods

        void ThrowConditional(SourceLine line)
        {
            var iterator = line.Operands.GetIterator();
            if (!iterator.MoveNext())
            {
                Services.Log.LogEntry(line.Filename, line.LineNumber, line.Instruction.Position,
                    "Expected expression.");
            }
            else
            {
                if (Services.Evaluator.EvaluateCondition(iterator, false))
                {
                    if (iterator.Current == null || iterator.PeekNext() == null)
                    {
                        Output(line, $"Expression \"{Token.Join(line.Operands)}\" evaluated to true.");
                    }
                    else
                    {
                        iterator.MoveNext();
                        Output(line, StringHelper.GetString(iterator, Services));
                        if (iterator.Current != null)
                            Services.Log.LogEntry(line.Filename, line.LineNumber, iterator.Current.Position,
                                "Unexpected expression.");
                    }
                }

            }
        }

        void SetEor(SourceLine line)
        {
            if (line.Operands.Count == 0)
            {
                Services.Log.LogEntry(line.Instruction, "Expected expression.");
            }
            else
            {
                var iterator = line.Operands.GetIterator();
                var eor = Services.Evaluator.Evaluate(iterator, sbyte.MinValue, byte.MaxValue);
                var eor_b = Convert.ToByte(eor);
                Services.Output.Transform = (delegate (byte b)
                {
                    b ^= eor_b;
                    return b;
                });
                if (iterator.Current != null)
                    Services.Log.LogEntry(iterator.Current, "Unexpected expression.");
            }
        }

        void SetBank(SourceLine line)
        {
            if (line.Operands.Count == 0)
            {
                Services.Log.LogEntry(line.Instruction, "Expected expression.");
            }
            else
            {
                var iterator = line.Operands.GetIterator();
                Services.Output.SetBank((int)Services.Evaluator.Evaluate(iterator, sbyte.MinValue, byte.MaxValue));
                if (iterator.Current != null)
                    Services.Log.LogEntry(iterator.Current, "Unexpected expression.");
            }
        }

        protected override string OnAssemble(RandomAccessIterator<SourceLine> lines)
        {
            var line = lines.Current;
            var instruction = line.Instruction.Name.ToLower();
            var iterator = line.Operands.GetIterator();
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
                    if (iterator.MoveNext() && StringHelper.ExpressionIsAString(iterator, Services))
                    {
                        Output(line, StringHelper.GetString(iterator, Services));
                    }
                    else if (instruction.Equals(".echo"))
                    {
                        if (Services.Evaluator.ExpressionIsCondition(iterator))
                            Output(line, Services.Evaluator.EvaluateCondition(iterator).ToString());
                        else
                            Output(line, Services.Evaluator.Evaluate(iterator, Evaluator.CbmFloatMinValue, Evaluator.CbmFloatMaxValue).ToString());
                    }
                    else
                    {
                        Services.Log.LogEntry(line.Instruction, "String expression expected.");
                    }
                    break;
                case ".eor":
                    SetEor(line);
                    break;
                case ".forcepass":
                    if (line.Operands.Count > 0)
                        Services.Log.LogEntry(line.Operands[0], "Unexpected expression.");
                    else if (Services.CurrentPass == 0)
                        Services.PassNeeded = true;
                    break;
                case ".invoke":
                    InvokeFunction(line);
                    break;
                case ".proff":
                case ".pron":
                    if (line.Operands.Count > 0)
                        Services.Log.LogEntry(line.Operands[0], "Unexpected expression.");
                    else
                        Services.PrintOff = instruction.Equals(".proff");
                    break;
                case ".dsection":
                    DefineSection(line);
                    break;
                case ".section":
                    return SetSection(line);
                case ".format":
                case ".target":
                    if (Services.CurrentPass == 0)
                    {
                        if (Services.Output.HasOutput)
                        {
                            Services.Log.LogEntry(line.Instruction, "Cannot specify target format after assembly has started.");
                        }
                        else
                        {
                            if (!iterator.MoveNext() || !StringHelper.ExpressionIsAString(iterator, Services))
                                Services.Log.LogEntry(line.Filename, line.LineNumber, line.Instruction.Position,
                                    "Expression must be a string.");
                            else if (!string.IsNullOrEmpty(Services.OutputFormat))
                                Services.Log.LogEntry(line.Filename, line.LineNumber, line.Instruction.Position,
                                    "Output format was previously specified.");
                            else
                                Services.SelectFormat(StringHelper.GetString(iterator, Services));
                            if (iterator.Current != null)
                                Services.Log.LogEntry(iterator.Current, "Unexpected expression.");
                        }
                        if (instruction.Equals(".target"))
                            Services.Log.LogEntry(line.Instruction,
                                "\".target\" is deprecated. Use \".format\" instead.", false);
                    }
                    break;
                default:
                    InitMem(line);
                    break;
            }
            return string.Empty;
        }

        void DefineSection(SourceLine line)
        {
            if (line.Label != null)
                throw new SyntaxException(line.Label, "Label definitions are not allowed for directive \".dsection\".");
            if (Services.CurrentPass == 0)
            {
                var parms = line.Operands.GetIterator();
                if (!parms.MoveNext() || !StringHelper.IsStringLiteral(parms))
                    throw new SyntaxException(line.Instruction.Position,
                        "Section name must be a string.");
                var name = parms.Current.Name;
                if (!parms.MoveNext() || Token.IsEnd(parms.PeekNext()))
                    throw new SyntaxException(line.Operands[0],
                        "Expected expression.");
                if (!Token.IsEnd(parms.Current))
                    throw new SyntaxException(parms.Current,
                        "Unexpected expression.");

                var starts = Convert.ToInt32(Services.Evaluator.Evaluate(parms, ushort.MinValue, ushort.MaxValue));
                var ends = BinaryOutput.MaxAddress + 1;
                if (!Token.IsEnd(parms.PeekNext()))
                    ends = Convert.ToInt32(Services.Evaluator.Evaluate(parms, ushort.MinValue, ushort.MaxValue));
                if (parms.MoveNext())
                    throw new SyntaxException(parms.Current, "Unexpected expression.");
                Services.Output.DefineSection(name, starts, ends);
            }
        }

        void InvokeFunction(SourceLine line)
        {
            var iterator = line.Operands.GetIterator();
            if (!iterator.MoveNext() || iterator.Current.Type != TokenType.Function)
            {
                Services.Log.LogEntry(line.Instruction, "Invalid or missing function call.");
            }
            else
            {
                Services.Evaluator.Invoke(iterator.Current, iterator);
                if (iterator.PeekNext() != null)
                    Services.Log.LogEntry(iterator.PeekNext(), "Unexpected expression.");
            }
        }

        string SetSection(SourceLine line)
        {
            var iterator = line.Operands.GetIterator();
            if (!iterator.MoveNext() || !iterator.Current.IsDoubleQuote() || iterator.PeekNext() != null)
                throw new SyntaxException(line.Instruction, "Directive expects a string expression.");
            if (!Services.Output.SetSection(iterator.Current.Name))
                throw new SyntaxException(line.Operands[0], $"Section {line.Operands[0].Name} not defined or previously selected.");
            if (line.Label != null)
            {
                if (line.Label.Name.Equals("*"))
                    Services.Log.LogEntry(line.Label, "Redundant assignment of program counter for directive \".section\".", false);
                else if (line.Label.Name[0].IsSpecialOperator())
                    Services.SymbolManager.DefineLineReference(line.Label, Services.Output.LogicalPC);
                else
                    Services.SymbolManager.DefineSymbolicAddress(line.Label.Name, Services.Output.LogicalPC, Services.Output.CurrentBank);
                return $"=${Services.Output.LogicalPC:x4}                  {line.Label}  // section {line.Operands[0]}";
            }
            return $"* = ${Services.Output.LogicalPC:x4}  // section {line.Operands[0]}";
        }

        void InitMem(SourceLine line)
        {
            if (line.Operands.Count == 0)
            {
                Services.Log.LogEntry(line.Instruction, "Expected expression.");
            }
            else
            {
                var iterator = line.Operands.GetIterator();
                var amount = Services.Evaluator.Evaluate(iterator, sbyte.MinValue, byte.MaxValue);
                if (iterator.Current != null || iterator.PeekNext() != null)
                    Services.Log.LogEntry(iterator.Current, "Unexpected expression.");
                else
                    Services.Output.InitMemory(Convert.ToByte((int)amount & 0xFF));

            }
        }

        void Output(SourceLine line, string output)
        {
            var type = line.Instruction.Name.Substring(0, 5).ToLower();
            var doEcho = type.Equals(".echo") && (Services.CurrentPass == 0 || Services.Options.EchoEachPass);
            if (doEcho)
                Console.WriteLine(output);
            else if (Services.CurrentPass == 0)
                Services.Log.LogEntry(line.Operands[0], output, !type.Equals(".warn"));
        }

        void DoAssert(SourceLine line)
        {
            if (line.Operands.Count == 0)
            {
                Services.Log.LogEntry(line.Instruction, "Expected expression.");
            }
            else
            {
                var iterator = line.Operands.GetIterator();
                if (!Services.Evaluator.EvaluateCondition(iterator))
                {
                    if (iterator.MoveNext())
                    {
                        if (!StringHelper.ExpressionIsAString(iterator, Services))
                        {
                            Services.Log.LogEntry(iterator.Current, "Expression must be a string");
                        }
                        else
                        {
                            var message = StringHelper.GetString(iterator, Services);
                            if (iterator.Current != null || iterator.PeekNext() != null)
                                Services.Log.LogEntry(iterator.Current, "Unexpected expression.");
                            else
                                Output(line, message);
                        }
                    }
                    else
                    {
                        Output(line, "Assertion failed.");
                    }
                }
            }
        }
        #endregion
    }
}