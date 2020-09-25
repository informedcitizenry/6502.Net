//-----------------------------------------------------------------------------
// Copyright (c) 2017-2020 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Core6502DotNet.z80
{
    /// <summary>
    /// A class responsible for assembling Z80 source.
    /// </summary>
    public sealed partial class Z80Asm : AssemblerBase
    {
        /// <summary>
        /// Creates a new instance of the Z80 assembler.
        /// </summary>
        /// <param name="services">The shared <see cref="AssemblyServices"/> object.</param>
        public Z80Asm(AssemblyServices services)
            :base(services)
        {
            Reserved.DefineType("Mnemonics",
                    "adc", "add", "ccf", "cpd", "cpdr", "cpi", "cpir", "cpl",
                    "daa", "dec", "di", "ei", "ex", "exx", "halt", "in",
                    "inc", "ind", "indr", "ini", "inir", "ld", "ldd", "lddr",
                    "ldi", "ldir", "neg", "nop", "otdr", "otir", "out", "outd",
                    "outi", "pop", "push", "reti", "retn", "rl", "rla", "rlc",
                    "rlca", "rld", "rr", "rra", "rrc", "rrca", "rrd", "rst",
                    "sbc", "scf", "sla", "sll", "slr", "sra", "srl", "xor"
                );

            Reserved.DefineType("Bits",
                    "bit", "res", "set"
                );

            Reserved.DefineType("Shifts",
                    "rl", "rla", "rlc", "rld", "rr", "rra", "rrc",
                    "rrd", "sla", "sll", "slr", "sra", "srl"
                );

            Reserved.DefineType("ImpliedA",
                    "and", "cp", "or", "sub", "xor"
                );

            Reserved.DefineType("Interrupt",
                    "im"
                );

            Reserved.DefineType("Branches",
                    "call", "jp", "jr", "ret"
                );

            Reserved.DefineType("Relatives",
                    "djnz", "jr"
                );

            Services.SymbolManager.AddValidSymbolNameCriterion(s => !s_namedModes.ContainsKey(s));
            _evals = new double[3];
        }

        Z80Mode GetValueMode(IEnumerable<Token> tokens, int i)
        {
            var value = Services.Evaluator.Evaluate(tokens, short.MinValue, ushort.MaxValue);
            _evals[i] = value;
            if (value < sbyte.MinValue || value > byte.MaxValue)
                return Z80Mode.Extended;
            return Z80Mode.PageZero;
        }

        Z80Mode GetValueMode(Token token, int i)
        {
            var value = Services.Evaluator.Evaluate(token, short.MinValue, ushort.MaxValue);
            _evals[i] = value;
            if (value < sbyte.MinValue || value > byte.MaxValue)
                return Z80Mode.Extended;
            return Z80Mode.PageZero;
        }

        Z80Mode[] ParseExpressionToModes(SourceLine line)
        {
            _evals[0] = _evals[1] = _evals[2] = double.NaN;
            var modes = new Z80Mode[3];
            if (line.OperandHasToken)
            {
                for (var i = 0; i < 3; i++)
                {
                    var mode = Z80Mode.Implied;
                    if (line.Operand.Children.Count > i)
                    {
                        var child = line.Operand.Children[i];
                        if (child.Children.Count > 0)
                        {
                            if (child.Children[0].Name.Equals("("))
                            {
                                if (child.Children.Count == 1)
                                {
                                    mode |= Z80Mode.Indirect;
                                    if (child.Children[0].Children.Count > 0 && child.Children[0].Children[0].Children.Count > 0)
                                    {
                                        var firstExInParen = child.Children[0].Children[0];
                                        var firstInParen = firstExInParen.Children[0];
                                        if (s_namedModes.ContainsKey(firstInParen.Name))
                                        {
                                            mode |= s_namedModes[firstInParen.Name];
                                            if (firstExInParen.Children.Count > 1)
                                            {
                                                var nextInParen = firstExInParen.Children[1];
                                                if (!nextInParen.Name.Equals("+") && !nextInParen.Name.Equals("-"))
                                                {
                                                    Services.Log.LogEntry(line, nextInParen.Position,
                                                        $"Unexpected operation \"{nextInParen.Name}\" found in index expression.");
                                                }
                                                else if (firstExInParen.Children.Count < 3)
                                                {
                                                    Services.Log.LogEntry(line, nextInParen.Position,
                                                        "Index expression is incomplete.");
                                                }
                                                else
                                                {
                                                    mode |= Z80Mode.Indexed;
                                                    var expression = new List<Token>(firstExInParen.Children.Skip(1));
                                                    expression[0] = new Token(expression[0].Name, TokenType.Operator, OperatorType.Unary);
                                                    mode |= GetValueMode(expression, i);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            mode |= GetValueMode(child.Children[0].Children[0], i);
                                            if (Reserved.IsOneOf("Relatives", line.InstructionName))
                                                mode |= Z80Mode.Extended;
                                        }
                                    }
                                    else
                                    {
                                        Services.Log.LogEntry(line, child.Children[0].Position,
                                            "Expected expression not given.");
                                    }
                                }
                                else
                                {
                                    mode |= GetValueMode(child, i);
                                    if (Reserved.IsOneOf("Relatives", line.InstructionName))
                                        mode |= Z80Mode.Extended;
                                }

                            }
                            else if (s_namedModes.ContainsKey(child.Children[0].Name))
                            {
                                if (child.Children.Count > 1)
                                    Services.Log.LogEntry(line, child.Children[1].Parent,
                                        $"Unexpected expression \"{child.Children[1].ToString().Trim()}\".");
                                else
                                    mode |= s_namedModes[child.Children[0].Name];
                            }
                            else if (line.InstructionName.Equals("rst"))
                            {
                                var value = Services.Evaluator.Evaluate(child, 0, 0x38);
                                if ((value % 8) != 0)
                                {
                                    Services.Log.LogEntry(line, $"Expression \"{child.ToString().Trim()}\" not valid for instruction \"rst\".");
                                }
                                else
                                {
                                    mode |= (Z80Mode)(value / 8) | Z80Mode.BitOp;
                                }
                            }
                            else if (Reserved.IsOneOf("Bits", line.InstructionName) ||
                                (line.InstructionName.Equals("out") && i == 1))
                            {
                                mode = (Z80Mode)Services.Evaluator.Evaluate(child, 0, 7) | Z80Mode.BitOp;
                            }
                            else
                            {
                                mode |= GetValueMode(child, i);
                                if (Reserved.IsOneOf("Relatives", line.InstructionName))
                                    mode |= Z80Mode.Extended;
                            }
                        }
                        else
                        {
                            Services.Log.LogEntry(line, child.Position, "Expected expression not given.");
                        }
                    }
                    modes[i] = mode;
                }
            }
            return modes;
        }

        (Z80Mode[] modes, CpuInstruction instruction) GetModeAndInstruction(SourceLine line)
        {
            var modes = ParseExpressionToModes(line);
            var mnemMode = (line.Instruction.Name, modes[0], modes[1], modes[2]);
            if (s_z80Instructions.ContainsKey(mnemMode))
                return (modes, s_z80Instructions[mnemMode]);

            if (modes.Any(m => (m & Z80Mode.SizeMask) == Z80Mode.PageZero))
            {
                modes = modes.Select(m =>
                {
                    if ((m & Z80Mode.SizeMask) == Z80Mode.PageZero)
                        return m | Z80Mode.Extended;
                    return m;
                }).ToArray();
                mnemMode = (line.Instruction.Name, modes[0], modes[1], modes[2]);
                if (s_z80Instructions.ContainsKey(mnemMode))
                    return (modes, s_z80Instructions[mnemMode]);
            }
            return (null, default);
        }

        protected override string OnAssembleLine(SourceLine line)
        {
            var mnemMode = GetModeAndInstruction(line);

            if (!string.IsNullOrEmpty(mnemMode.instruction.CPU))
            {
                var modes = mnemMode.modes;
                var instruction = mnemMode.instruction;
                var isCb00 = (instruction.Opcode & 0xFF00) == 0xCB00;
                if ((instruction.Opcode & 0xFF) == 0xCB || isCb00)
                    Services.Output.Add(instruction.Opcode, 2);
                else
                    Services.Output.Add(instruction.Opcode, instruction.Opcode.Size());

                var displayEvals = new int[3];
                for (var i = 0; i < 3; i++)
                {
                    if (!double.IsNaN(_evals[i]))
                    {
                        var modeSize = modes[i] & Z80Mode.SizeMask;
                        if (modeSize == Z80Mode.Extended)
                            displayEvals[i] = (int)_evals[i] & 0xFFFF;
                        else if (modeSize == Z80Mode.PageZero)
                            displayEvals[i] = (int)_evals[i] & 0xFF;
                        if (Reserved.IsOneOf("Relatives", line.InstructionName))
                        {
                            _evals[i] = Convert.ToSByte(Services.Output.GetRelativeOffset((int)_evals[i], 1));
                            Services.Output.Add(_evals[i], 1);
                        }
                        else
                        {
                            Services.Output.Add(_evals[i], modeSize == Z80Mode.PageZero ? 1 : 2);
                        }
                    }
                }
                if (isCb00)
                    Services.Output.Add(instruction.Opcode >> 16, 1);
                if (Services.Output.LogicalPC - PCOnAssemble != instruction.Size && !Services.PassNeeded)
                {
                    Services.Log.LogEntry(line, 
                                           line.Instruction, 
                                           $"Mode not supported for instruction \"{line.InstructionName}\".");
                }
                else
                {
                    if (Services.PassNeeded || string.IsNullOrEmpty(Services.Options.ListingFile))
                        return string.Empty;
                    var disasmBuilder = new StringBuilder();
                    if (!Services.Options.NoAssembly)
                    {
                        var byteString = Services.Output.GetBytesFrom(PCOnAssemble).ToString(PCOnAssemble, '.', true);
                        disasmBuilder.Append(byteString.PadRight(25));
                    }
                    else
                    {
                        disasmBuilder.Append($".{PCOnAssemble:x4}                        ");
                    }
                    if (!Services.Options.NoDisassembly)
                    {
                        var asmBuilder = new StringBuilder($"{line.InstructionName} ");
                        for (var i = 0; i < 3; i++)
                        {
                            if (modes[i] == Z80Mode.Implied)
                                break;
                            if (i > 0)
                                asmBuilder.Append(',');
                            if (line.InstructionName.Equals("rst"))
                            {
                                asmBuilder.Append($"${(int)(modes[i] & Z80Mode.Bit7) * 8:x2}");
                            }
                            else
                            {
                                if (modes[i].HasFlag(Z80Mode.BitOp))
                                {
                                    asmBuilder.Append((int)(modes[i] & Z80Mode.Bit7));
                                }
                                else
                                {
                                    var format = s_disassemblyFormats[modes[i]];
                                    asmBuilder.AppendFormat(format, displayEvals[i]);
                                }
                            }
                        }
                        disasmBuilder.Append(asmBuilder.ToString().PadRight(18));
                    }
                    else
                    {
                        disasmBuilder.Append("                  ");
                    }
                    if (!Services.Options.NoSource)
                        disasmBuilder.Append(line.UnparsedSource);
                    return disasmBuilder.ToString();
                }
            }
            else if (!Services.PassNeeded)
            {
                Services.Log.LogEntry(line, line.Instruction,
                    $"Mode not supported for instruction \"{line.InstructionName}\".");
            }
            return string.Empty;
        }
    }
}