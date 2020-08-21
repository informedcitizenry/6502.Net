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
    public sealed partial class Z80Asm : AssemblerBase
    {
        public Z80Asm()
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

            Assembler.SymbolManager.AddValidSymbolNameCriterion(s => !_namedModes.ContainsKey(s));
        }

        Z80Mode GetValueMode(IEnumerable<Token> tokens, int i)
        {
            var value = Evaluator.Evaluate(tokens, short.MinValue, ushort.MaxValue);
            _evals[i] = value;
            if (value < sbyte.MinValue || value > byte.MaxValue)
                return Z80Mode.Extended;
            return Z80Mode.PageZero;
        }

        Z80Mode GetValueMode(Token token, int i)
        {
            var value = Evaluator.Evaluate(token, short.MinValue, ushort.MaxValue);
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
                                        if (_namedModes.ContainsKey(firstInParen.Name))
                                        {
                                            mode |= _namedModes[firstInParen.Name];
                                            if (firstExInParen.Children.Count > 1)
                                            {
                                                var nextInParen = firstExInParen.Children[1];
                                                if (!nextInParen.Name.Equals("+") && !nextInParen.Name.Equals("-"))
                                                {
                                                    Assembler.Log.LogEntry(line, nextInParen.Position,
                                                        $"Unexpected operation \"{nextInParen.Name}\" found in index expression.");
                                                }
                                                else if (firstExInParen.Children.Count < 3)
                                                {
                                                    Assembler.Log.LogEntry(line, nextInParen.Position,
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
                                        Assembler.Log.LogEntry(line, child.Children[0].Position,
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
                            else if (_namedModes.ContainsKey(child.Children[0].Name))
                            {
                                mode |= _namedModes[child.Children[0].Name];
                            }
                            else if (line.InstructionName.Equals("rst"))
                            {
                                var value = Evaluator.Evaluate(child, 0, 0x38);
                                if ((value % 8) != 0)
                                {
                                    Assembler.Log.LogEntry(line, $"Expression \"{child}\" not valid for instruction \"rst\".");
                                }
                                else
                                {
                                    mode |= (Z80Mode)(value / 8) | Z80Mode.BitOp;
                                }
                            }
                            else if (Reserved.IsOneOf("Bits", line.InstructionName) ||
                                (line.InstructionName.Equals("out") && i == 1))
                            {
                                mode = (Z80Mode)Evaluator.Evaluate(child, 0, 7) | Z80Mode.BitOp;
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
                            Assembler.Log.LogEntry(line, child.Position, "Expected expression not given.");
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
            if (_z80Instructions.ContainsKey(mnemMode))
                return (modes, _z80Instructions[mnemMode]);

            if (modes.Any(m => (m & Z80Mode.SizeMask) == Z80Mode.PageZero))
            {
                modes = modes.Select(m =>
                {
                    if ((m & Z80Mode.SizeMask) == Z80Mode.PageZero)
                        return m | Z80Mode.Extended;
                    return m;
                }).ToArray();
                mnemMode = (line.Instruction.Name, modes[0], modes[1], modes[2]);
                if (_z80Instructions.ContainsKey(mnemMode))
                    return (modes, _z80Instructions[mnemMode]);
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
                    Assembler.Output.Add(instruction.Opcode, 2);
                else
                    Assembler.Output.Add((double)instruction.Opcode);

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
                            _evals[i] = Convert.ToSByte(Assembler.Output.GetRelativeOffset((int)_evals[i], 0));
                            Assembler.Output.Add(_evals[i], 1);
                        }
                        else
                        {
                            Assembler.Output.Add(_evals[i], modeSize == Z80Mode.PageZero ? 1 : 2);
                        }
                    }
                }
                if (isCb00)
                    Assembler.Output.Add(instruction.Opcode >> 16, 1);
                if (Assembler.Output.LogicalPC - PCOnAssemble != instruction.Size && !Assembler.PassNeeded)
                {
                    Assembler.Log.LogEntry(line, 
                                           line.Instruction, 
                                           $"Mode not supported for instruction \"{line.InstructionName}\".");
                }
                else
                {
                    if (Assembler.PassNeeded || string.IsNullOrEmpty(Assembler.Options.ListingFile))
                        return string.Empty;
                    var disasmBuilder = new StringBuilder();
                    if (!Assembler.Options.NoAssembly)
                    {
                        var byteString = Assembler.Output.GetBytesFrom(PCOnAssemble).ToString(PCOnAssemble, '.', true);
                        disasmBuilder.Append(byteString.PadRight(25));
                    }
                    else
                    {
                        disasmBuilder.Append($".{PCOnAssemble:x4}                        ");
                    }
                    if (!Assembler.Options.NoDisassembly)
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
                                    var format = _disassemblyFormats[modes[i]];
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
                    if (!Assembler.Options.NoSource)
                        disasmBuilder.Append(line.UnparsedSource);
                    return disasmBuilder.ToString();
                }
            }
            else if (!Assembler.PassNeeded)
            {
                Assembler.Log.LogEntry(line, line.Instruction,
                    $"Mode not supported for instruction \"{line.InstructionName}\".");
            }
            return string.Empty;
        }
    }
}