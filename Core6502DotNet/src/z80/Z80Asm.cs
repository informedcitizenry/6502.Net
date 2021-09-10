//-----------------------------------------------------------------------------
// Copyright (c) 2017-2021 informedcitizenry <informedcitizenry@gmail.com>
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
    public sealed partial class Z80Asm : CpuAssembler
    {
        /// <summary>
        /// Creates a new instance of the Z80 assembler.
        /// </summary>
        /// <param name="services">The shared <see cref="AssemblyServices"/> object.</param>
        public Z80Asm(AssemblyServices services)
            : base(services)
        {
            Reserved.DefineType("Mnemonics",
                    "adc", "add", "ccf", "cpd", "cpdr", "cpi", "cpir", "cpl",
                    "daa", "dec", "di", "ei", "ex", "exx", "halt", "in",
                    "inc", "ind", "indr", "ini", "inir", "ld", "ldd", "lddr",
                    "ldi", "ldir", "neg", "nop", "otdr", "otir", "out", "outd",
                    "outi", "pop", "push", "reti", "retn", "rl", "rla", "rlc",
                    "rlca", "rld", "rr", "rra", "rrc", "rrca", "rrd", "sbc", 
                    "scf", "sla", "sll", "slr", "sra", "srl", "xor"
                );

            Reserved.DefineType("i80Mnemonics",
                     "aci", "adi", "ana", "ani", "cc", "cm", "cma", "cmc",
                    "cmp", "cnc", "cnz", "cpe", "cpo", "cz", "dad", "dcr",
                    "dcx", "hlt", "inr", "inx", "jc", "jm", "jmp", "jnc",
                    "jnz", "jpe", "jpo", "jz", "lda", "ldax", "lhld", "lxi",
                    "mov", "mvi", "ora", "ori", "pchl", "ral", "rar", "rc",
                    "rm", "rnc", "rnz", "rp", "rpe", "rpo", "rz", "sbb",
                    "sbi", "shld", "sphl", "sta", "stax", "stc", "sui", "xchg",
                    "xra", "xri", "xthl");

            Reserved.DefineType("Bits",
                   "im", "rst",  "bit", "res", "set"
                );

            Reserved.DefineType("Shifts",
                    "rl", "rla", "rlc", "rld", "rr", "rra", "rrc",
                    "rrd", "sla", "sll", "slr", "sra", "srl"
                );

            Reserved.DefineType("ImpliedA",
                    "and", "cp", "or", "sub", "xor"
                );

            Reserved.DefineType("Branches",
                    "call", "jp", "jr", "ret"
                );

            Reserved.DefineType("Relatives",
                    "djnz", "jr"
                );

            _namedModes = new Dictionary<StringView, Z80Mode>(Services.StringViewComparer)
            {
                { "a",   Z80Mode.A         },
                { "af",  Z80Mode.AF        },
                { "af'", Z80Mode.ShadowAF  },
                { "b",   Z80Mode.B         },
                { "bc",  Z80Mode.BC        },
                { "c",   Z80Mode.C         },
                { "d",   Z80Mode.D         },
                { "de",  Z80Mode.DE        },
                { "e",   Z80Mode.E         },
                { "h",   Z80Mode.H         },
                { "hl",  Z80Mode.HL        },
                { "i",   Z80Mode.I         },
                { "ix",  Z80Mode.IX        },
                { "ixh", Z80Mode.XH        },
                { "ixl", Z80Mode.XL        },
                { "iy",  Z80Mode.IY        },
                { "iyh", Z80Mode.YH        },
                { "iyl", Z80Mode.YL        },
                { "l",   Z80Mode.L         },
                { "m",   Z80Mode.M         },
                { "nc",  Z80Mode.NC        },
                { "nz",  Z80Mode.NZ        },
                { "p",   Z80Mode.P         },
                { "pe",  Z80Mode.PE        },
                { "po",  Z80Mode.PO        },
                { "psw", Z80Mode.PSW       },
                { "r",   Z80Mode.R         },
                { "sp",  Z80Mode.SP        },
                { "z",   Z80Mode.Z         }
            };
            Services.SymbolManager.AddValidSymbolNameCriterion(s => !_namedModes.ContainsKey(s));
        }

        Z80Mode GetValueMode(RandomAccessIterator<Token> tokens, bool doNotAdavance, double minValue, double maxValue, int i)
        {
            var value = Services.Evaluator.Evaluate(tokens, doNotAdavance, minValue, maxValue);
            Evaluations[i] = value;
            if (value < sbyte.MinValue || value > byte.MaxValue)
                return Z80Mode.Extended;
            return Z80Mode.PageZero;
        }

        Z80Mode[] ParseExpressionToModes(SourceLine line)
        {
            var modes = new Z80Mode[3];
            var operands = line.Operands.GetIterator();
            Token operand;
            int i = 0;
            while (i < 3 && (operand = operands.GetNext()) != null)
            {
                Z80Mode mode = Z80Mode.Implied;
                if (operand.IsSeparator())
                    throw new SyntaxException(operand, "Expression expected.");
                while (!Token.IsEnd(operand))
                {
                    if (line.Instruction.Name.Equals("rst", Services.StringComparison) && CPU.Equals("z80"))
                    {
                        var rst = Services.Evaluator.Evaluate(operands, false, 0, 0x38);
                        if (!rst.IsInteger() || (rst != 0 && (int)rst % 8 != 0))
                            throw new IllegalQuantityException(operand, rst);
                        mode |= (Z80Mode)(rst / 8) | Z80Mode.BitOp;
                        operand = operands.Current;
                    }
                    else if (Reserved.IsOneOf("Bits", line.Instruction.Name) && i == 0)
                    {
                        mode = (Z80Mode)Services.Evaluator.Evaluate(operands, false, 0, 7) | Z80Mode.BitOp;
                        operand = operands.Current;
                    }
                    else
                    {
                        var firstIndex = operands.Index;
                        if (operand.Name.Equals("("))
                        {
                            mode |= Z80Mode.Indirect;
                            operand = operands.GetNext();
                        }
                        if (_namedModes.TryGetValue(operand.Name, out var regMode))
                        {
                            mode |= regMode;
                            if (!Token.IsEnd((operand = operands.GetNext())))
                            {
                                if (!operand.IsSpecialOperator() || operand.Name.Equals("*"))
                                    throw new SyntaxException(operand, "Unexpected expression.");
                                mode |= Z80Mode.Indexed;
                                var sign = operand.Name.Equals("+") ? 1 : -1;
                                mode |= GetValueMode(operands, true, 0, byte.MaxValue, i);
                                Evaluations[i] *= sign;
                                if (Evaluations[i] < sbyte.MinValue)
                                    throw new IllegalQuantityException(operand, Evaluations[i]);
                            }
                        }
                        else if (line.Instruction.Name.Equals("out", Services.StringComparison) && i == 1)
                        {
                            mode = (Z80Mode)Services.Evaluator.Evaluate(operands, false, 0, 7) | Z80Mode.BitOp0;
                        }
                        else
                        {
                            if (mode.HasFlag(Z80Mode.Indirect))
                            {
                                var isFalseIndirect = Reserved.IsOneOf("Relatives", line.Instruction.Name);
                                if (!isFalseIndirect)
                                {
                                    operands.SetIndex(firstIndex);
                                    _ = Token.GetGroup(operands);
                                    isFalseIndirect = !Token.IsEnd(operands.Current);
                                    operands.SetIndex(firstIndex);
                                }
                                if (isFalseIndirect)
                                    mode &= ~Z80Mode.Indirect;
                            }
                            mode |= GetValueMode(operands, false, short.MinValue, ushort.MaxValue, i);
                            if (Reserved.IsOneOf("Relatives", line.Instruction.Name))
                                mode |= Z80Mode.Extended;
                           
                        }
                        operand = operands.Current;
                        if (operand != null)
                        {
                            if (!Token.IsEnd(operand) || (operand.Name.Equals(")") && !Token.IsEnd(operand = operands.GetNext())))
                                throw new SyntaxException(operand, "Unexpected expression.");
                        }
                    }
                }
                modes[i++] = mode;
            }
            if (operands.Current != null)
                throw new SyntaxException(operands.Current, "Unexpected expression.");
            return modes;
        }

        (Z80Mode[] modes, CpuInstruction instruction) GetModeAndInstruction(SourceLine line)
        {
            var modes = ParseExpressionToModes(line);
            var instruction = Services.Options.CaseSensitive ? line.Instruction.Name.ToString() : line.Instruction.Name.ToLower();
            var mnemMode = (instruction, modes[0], modes[1], modes[2]);
            if (_instructionSet.ContainsKey(mnemMode))
                return (modes, _instructionSet[mnemMode]);
            var modesCopy = modes.ToArray();
            // check first if we need to "extend" ZP expressions
            if (modes.Any(m => (m & Z80Mode.SizeMask) == Z80Mode.PageZero))
            {
                modes = modes.Select(m =>
                {
                    if ((m & Z80Mode.SizeMask) == Z80Mode.PageZero)
                        return m | Z80Mode.Extended;
                    return m;
                }).ToArray();
                mnemMode = (instruction, modes[0], modes[1], modes[2]);
                if (_instructionSet.ContainsKey(mnemMode))
                    return (modes, _instructionSet[mnemMode]);
            }
            modes = modesCopy.ToArray();
            // check for any "false indirect" expressions e.g., ld (hl),($30)
            if (modes.Any(m => (m & Z80Mode.Indirect) == Z80Mode.Indirect))
            {
                for (var i = 0; i < 3; i++)
                {
                    if (modes[i].HasFlag(Z80Mode.Indirect) && !double.IsNaN(Evaluations[i]) && modes[i] < Z80Mode.A)
                        modes[i] &= ~Z80Mode.Indirect;

                }
                mnemMode = (instruction, modes[0], modes[1], modes[2]);
                if (_instructionSet.ContainsKey(mnemMode))
                    return (modes, _instructionSet[mnemMode]);
            }
           
            return (null, default);
        }

        internal override int GetInstructionSize(SourceLine line)
        {
            try
            {
                if (!Reserved.IsOneOf("CPU", line.Instruction.Name))
                    return GetModeAndInstruction(line).instruction.Size;
                return 0;
            }
            catch
            {
                return 2;
            }
        }

        protected override void OnSetCpu()
        {
            if (CPU.Equals("z80"))
                _instructionSet = s_z80Instructions;
            else
                _instructionSet = s_i8080Instructions;
        }

        public override bool IsCpuValid(string cpu)
            => cpu.Equals("z80") || cpu.Equals("i8080");

        protected override string AssembleCpuInstruction(SourceLine line)
        {
            var mnemMode = GetModeAndInstruction(line);

            if (!string.IsNullOrEmpty(mnemMode.instruction.CPU))
            {
                var modes = mnemMode.modes;
                var instruction = mnemMode.instruction;
                var isCb00 = (instruction.Opcode & 0xFF00) == 0xCB00 &&
                    ((instruction.Opcode & 0xFF) == 0xDD || (instruction.Opcode & 0xFF) == 0xFD);
                if ((instruction.Opcode & 0xFF) == 0xCB || isCb00)
                    Services.Output.Add(instruction.Opcode, 2);
                else
                    Services.Output.Add(instruction.Opcode, instruction.Opcode.Size());

                var displayEvals = new int[3];
                for (var i = 0; i < 3; i++)
                {
                    if (!double.IsNaN(Evaluations[i]))
                    {
                        var modeSize = modes[i] & Z80Mode.SizeMask;
                        if (modeSize == Z80Mode.Extended)
                            displayEvals[i] = (int)Evaluations[i] & 0xFFFF;
                        else if (modeSize == Z80Mode.PageZero)
                            displayEvals[i] = (int)Evaluations[i] & 0xFF;
                        if (Reserved.IsOneOf("Relatives", line.Instruction.Name))
                        {
                            try
                            {
                                Evaluations[i] = Convert.ToSByte(Services.Output.GetRelativeOffset((int)Evaluations[i], 1));
                                Services.Output.Add(Evaluations[i], 1);
                            }
                            catch 
                            {
                                if (Services.CurrentPass > 0)
                                    throw new ExpressionException(line.Operands[0].Position, "Relative offset for branch was too far.");
                                Services.PassNeeded = true;
                                Services.Output.AddUninitialized(1);
                            }
                        }
                        else
                        {
                            Services.Output.Add(Evaluations[i], modeSize == Z80Mode.PageZero ? 1 : 2);
                        }
                    }
                }
                if (isCb00)
                    Services.Output.Add(instruction.Opcode >> 16, 1);
                if (Services.Output.LongProgramCounter - LongPCOnAssemble != instruction.Size && !Services.PassNeeded)
                {
                    Services.Log.LogEntry(line.Instruction, $"Mode not supported for instruction \"{line.Instruction}\".");
                }
                else
                {
                    if (Services.PassNeeded || string.IsNullOrEmpty(Services.Options.ListingFile))
                        return string.Empty;
                    var disasmBuilder = new StringBuilder();
                    if (!Services.Options.NoAssembly)
                    {
                        var byteString = Services.Output.GetBytesFrom(LogicalPCOnAssemble).ToString(LogicalPCOnAssemble, '.', true);
                        disasmBuilder.Append(byteString.PadRight(25));
                    }
                    else
                    {
                        disasmBuilder.Append($".{LogicalPCOnAssemble:x4}                        ");
                    }
                    if (!Services.Options.NoDisassembly)
                    {
                        var asmBuilder = new StringBuilder($"{line.Instruction.Name.ToLower()} ");
                        for (var i = 0; i < 3; i++)
                        {
                            if (modes[i] == Z80Mode.Implied ||
                                (CPU.Equals("z80") && i == 1 && 
                                modes[0] == Z80Mode.A && 
                                modes[1] == Z80Mode.A && 
                                Reserved.IsOneOf("ImpliedA", line.Instruction.Name)))
                                break;
                            if (i > 0)
                                asmBuilder.Append(',');
                            if (line.Instruction.Name.Equals("rst", Services.StringComparison))
                            {
                                if (CPU.Equals("z80"))
                                    asmBuilder.Append($"${(int)(modes[i] & Z80Mode.Bit7) * 8:x2}");
                                else
                                    asmBuilder.Append((int)(modes[i] & ~Z80Mode.BitOp));
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
                        disasmBuilder.Append(line.Source);
                    return disasmBuilder.ToString();
                }
            }
            else if (!Services.PassNeeded)
            {
                Services.Log.LogEntry(line.Instruction, $"Mode not supported for instruction \"{line.Instruction}\".");
            }
            return string.Empty;
        }
        protected override string[] Calls => new string[] { "call" };

        protected override string[] Returns => new string[] { "ret", "reti", "retn" };
    }
}