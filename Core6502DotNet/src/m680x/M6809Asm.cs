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

namespace Core6502DotNet.m680x
{
    /// <summary>
    /// A class responsible for Motorola 6800/6809 assembly.
    /// </summary>
    public sealed partial class M6809Asm : MotorolaBase
    {
        #region Subclasses

        class PushPullComparer : IComparer<StringView>
        {
            readonly IDictionary<StringView, byte> _lookup;

            public PushPullComparer(IDictionary<StringView, byte> lookup)
                => _lookup = lookup;

            public int Compare(StringView x, StringView y) =>
                _lookup == null ? 1 : _lookup[x].CompareTo(_lookup[y]);
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new instance of the M6800/6809 assembler.
        /// </summary>
        /// <param name="services">The shared <see cref="AssemblyServices"/> object.</param>
        public M6809Asm(AssemblyServices services)
            : base(services)
        {
            Reserved.DefineType("Mnemonics",
                "aba", "abx", "adca", "adcb", "adda", "addb",
                "addd", "anda", "andb", "andcc", "asl", "asla",
                "aslb", "asr", "asra", "asrb", "bcc", "bcs",
                "beq", "bge", "bgt", "bhi", "bita", "bitb",
                "ble", "bls", "blt", "bmi", "bne", "bpl",
                "bra", "bsr", "bvc", "bvs", "cba", "clc",
                "cli", "clr", "clra", "clrb", "clv", "cmpa",
                "cmpb", "cmpd", "cmps", "cmpu", "cmpx", "cmpy",
                "com", "coma", "comb", "cpxa", "cwai", "daa",
                "dec", "deca", "decb", "des", "dex", "eora",
                "eorb", "inc", "inca", "incb", "ins", "inx",
                "jmp", "jsr", "lda", "ldaa", "ldab", "ldb",
                "ldd", "lds", "ldu", "ldx", "ldy", "leas",
                "leau", "leax", "leay", "lsl", "lsla", "lslb",
                "lsr", "lsra", "lsrb", "mul", "neg", "nega",
                "negb", "nop", "ora", "oraa", "orab", "orb",
                "orcc", "psha", "pshb", "pshs", "pshu", "pula",
                "pulb", "puls", "pulu", "rol", "rola", "rolb",
                "ror", "rora", "rorb", "rti", "rts", "sba",
                "sbca", "sbcb", "sec", "sei", "sev", "sex",
                "sta", "staa", "stab", "stb", "std", "sts",
                "stu", "stx", "sty", "suba", "subb", "subd",
                "swi", "swi2", "swi3", "sync", "tab", "tap",
                "tba", "tpa", "tst", "tsta", "tstb", "tsx",
                "txs", "wai");

            Reserved.DefineType("PushPullsExchanges",
                "pshs", "pshu", "puls", "pulu", "tfr", "exg");

            Reserved.DefineType("Exchanges",
                "tfr", "exg");

            Reserved.DefineType("Branches",
                "bcc", "bcs", "beq", "bge", "bgt", "bhi",
                "bhs", "ble", "bls", "blo", "bls", "blt",
                "bmi", "bne", "bpl", "bra", "brn", "bsr",
                "bvc", "bvs");

            Reserved.DefineType("LongBranches",
                "lbcc", "lbcs", "lbeq", "lbge", "lbgt", "lbhi",
                "lbhs", "lble", "lbls", "lblo", "lbls", "lblt",
                "lbmi", "lbne", "lbpl", "lbra", "lbrn", "lbsr",
                "lbvc", "lbvs");

            _ixRegisterModes = new Dictionary<StringView, IndexModes>(Services.StringViewComparer)
            {
                { "x", IndexModes.XReg },
                { "y", IndexModes.YReg },
                { "u", IndexModes.UReg },
                { "s", IndexModes.SReg }
            };

            _offsRegisterModes = new Dictionary<StringView, IndexModes>(Services.StringViewComparer)
            {
                { "a", IndexModes.AccA },
                { "b", IndexModes.AccB },
                { "d", IndexModes.AccD }
            };

            _exchangeModes = new Dictionary<StringView, byte>(Services.StringViewComparer)
            {
                { "d",   0b0000 },
                { "x",   0b0001 },
                { "y",   0b0010 },
                { "u",   0b0011 },
                { "s",   0b0100 },
                { "pc",  0b0101 },
                { "a",   0b1000 },
                { "b",   0b1001 },
                { "cc",  0b1010 },
                { "dp",  0b1011 }
            };

            _pushPullModes = new Dictionary<StringView, byte>(Services.StringViewComparer)
            {
                { "pc", 0b1000_0000 },
                { "s",  0b0100_0000 },
                { "u",  0b0100_0000 },
                { "y",  0b0010_0000 },
                { "x",  0b0001_0000 },
                { "dp", 0b0000_1000 },
                { "d",  0b0000_0110 },
                { "b",  0b0000_0100 },
                { "a",  0b0000_0010 },
                { "cc", 0b0000_0001 }
            };

            // for 6809, parsing line terminations is tricky for auto-increment indexing (e.g, ,x++)
            // so we need to redefine for processing.
            if (CPU.Equals("m6809"))
                Services.LineTerminates = TerminatesLine;

            Services.Output.IsLittleEndian = false;
        }
        #endregion

        #region Methods

        bool TerminatesLine(List<Token> tokens)
        {
            if (tokens.Count >= 3)
            {
                var firstAfterInstr = tokens.FindIndex(t => t.Type != TokenType.Instruction && t.Type != TokenType.Label);
                if (tokens[firstAfterInstr].Name.Equals("[") && !tokens[^1].Name.Equals("]"))
                    return false;
                if (tokens.Count >= 4 && // minimum: ld ,y+ is 4 tokens
                    tokens[^1].Name.Equals("+") &&
                    (_ixRegisterModes.ContainsKey(tokens[^2].Name) ||
                    (tokens[^2].Name.Equals("+") && _ixRegisterModes.ContainsKey(tokens[^3].Name))))
                {
                    var instruction = tokens.FirstOrDefault(t => t.Type == TokenType.Instruction);
                    if (instruction != null)
                        return Assembles(instruction.Name);
                }
            }
            return false;
        }

        protected override bool IsCpuValid(string cpu)
            => cpu.Equals("m6800") || cpu.Equals("m6809");

        protected override void OnSetCpu()
        {
            if (CPU.Equals("m6809"))
                ActiveInstructions = new Dictionary<(string Mnem, Modes mode), CpuInstruction>(s_opcodes6809);
            else
                ActiveInstructions = new Dictionary<(string Mnem, Modes mode), CpuInstruction>(s_opcodes6800);
        }

        protected override Modes ParseOperand(SourceLine line)
        {
            var mode = Modes.Implied;

            if (line.Operands.Count > 0)
            {
                var operand = line.Operands.GetIterator();
                mode = GetForcedModifier(operand);
                var first = operand.GetNext();
                if (first.Name.Equals("#"))
                {
                    mode |= Modes.Immediate;
                    if (!operand.MoveNext())
                        throw new SyntaxException(first, "Missing Operand.");

                }
                var size = Evaluate(operand, false, 0);
                if (!mode.HasFlag(Modes.ForceWidth))
                    mode |= size;
                if (operand.MoveNext())
                {
                    if (!operand.Current.Name.Equals("x", Services.StringComparison))
                        throw new SyntaxException(operand.Current, "Unexpected expresion.");
                    mode |= Modes.IndexedX;
                }
                if (Reserved.IsOneOf("Branches", line.Instruction.Name))
                    mode = (mode & ~Modes.Absolute) | Modes.Relative;
                else if (Reserved.IsOneOf("LongBranches", line.Instruction.Name))
                    mode |= Modes.Relative;
            }
            return mode;
        }

        internal override int GetInstructionSize(SourceLine line)
        {
            if (Reserved.IsOneOf("PushPullsExchanges", line.Instruction.Name) || 
                (CPU.Equals("m6809") && line.Operands.Count > 1 &&
                (line.Operands[^1].Name.Equals("]") ||
                 line.Operands[^1].Name.Equals("+") ||
                 line.Operands[^1].Name.Equals("pc", Services.StringComparison) ||
                 _ixRegisterModes.ContainsKey(line.Operands[^1].Name))))
            {
                return 2;
            }
            return base.GetInstructionSize(line);
        }

        protected override string OnAssemble(RandomAccessIterator<SourceLine> lines)
        {
            Evaluations[0] = Evaluations[1] = Evaluations[2] = double.NaN;
            var line = lines.Current;
            if (Reserved.IsOneOf("PushPullsExchanges", line.Instruction.Name))
            {
                try
                {
                    return AssemblePushPullExchange(line);
                }
                catch (SyntaxException synEx)
                {
                    Services.Log.LogEntry(synEx.Token, synEx.Message);
                    return string.Empty;
                }
            }
            if (CPU.Equals("m6809") && line.Operands.Count > 1 &&
                (line.Operands[^1].Name.Equals("]") ||
                 line.Operands[^1].Name.Equals("+") ||
                 line.Operands[^1].Name.Equals("pc", Services.StringComparison) ||
                 _ixRegisterModes.ContainsKey(line.Operands[^1].Name)))
            {
                return AssembleIndexed(line);
            }
            return base.OnAssemble(lines);
        }

        string AssembleIndexed(SourceLine line)
        {
            var instructionName = line.Instruction.Name.ToLower();
            if (!ActiveInstructions.TryGetValue((instructionName, Modes.IndexedX), out var instruction))
                throw new SyntaxException(line.Operands[0], $"Addressing mode not supported for instruction \"{instructionName}\".");
            var modes = IndexModes.None;
            var operand = line.Operands.GetIterator();
            var forcedMod = GetForcedModifier(operand);
            var firstparam = operand.GetNext();
            var disSb = new StringBuilder($"{instructionName} ");
            IndexModes indexMode = IndexModes.None, offsRegMode = IndexModes.None;
            if (firstparam.Name.Equals("["))
            {
                // check for indirect modes
                disSb.Append("[");
                firstparam = operand.GetNext();
                if (!firstparam.Name.Equals("]"))
                    modes |= IndexModes.Indir;
                else
                    throw new SyntaxException(firstparam, "Expected expression.");
            }
            var offsetValue = double.NaN;
            if (!_offsRegisterModes.TryGetValue(firstparam.Name, out offsRegMode))
            {
                if (!firstparam.Name.Equals(","))
                {
                    try
                    {
                        offsetValue = Services.Evaluator.Evaluate(operand, false, short.MinValue, ushort.MaxValue);
                    }
                    catch (IllegalQuantityException ex)
                    {
                        if (!Services.PassNeeded)
                            throw ex;
                        offsetValue = 0;
                    }
                }
            }
            else
            {
                modes |= offsRegMode;
                operand.MoveNext();
            }
            bool autoInc = false, autoDec = false;
            if (operand.Current != null && operand.Current.Name.Equals(","))
            {
                var secondparam = operand.GetNext();
                if (Token.IsEnd(secondparam))
                    throw new SyntaxException(firstparam, "Expression expected.");
                   
                if (secondparam.Name[0] == '-')
                {
                    autoDec = true;
                    modes |= IndexModes.Dec1;
                    secondparam = operand.GetNext();
                    if (Token.IsEnd(secondparam))
                        throw new SyntaxException(firstparam, "Expression expected.");
                    if (secondparam.Name[0] == '-')
                    {
                        modes |= IndexModes.Dec2;
                        if (!operand.MoveNext())
                            throw new SyntaxException(firstparam, "Expression expected.");
                    }
                    else if (modes.HasFlag(IndexModes.Indir))
                    {
                        Services.Log.LogEntry(secondparam, "Addressing mode not supported for selected CPU.");
                        return string.Empty;
                    }
                    secondparam = operand.Current;
                }
                if (!_ixRegisterModes.TryGetValue(secondparam.Name, out indexMode))
                {
                    if (!secondparam.Name.Equals("pc", Services.StringComparison))
                    {
                        Services.Log.LogEntry(secondparam,  $"Invalid index register \"{secondparam}\".");
                        return string.Empty;
                    }
                    indexMode = IndexModes.PC8;
                    modes |= IndexModes.PC8;
                }
                else
                {
                    modes |= indexMode;
                }
                if (operand.MoveNext())
                {
                    if (indexMode == IndexModes.PC8)
                    {
                        if (!Token.IsEnd(operand.Current))
                            throw new SyntaxException(operand.Current, "Unexpected expression.");
                    }
                    else
                    {
                        var thirdparam = operand.Current;
                        if (thirdparam.Name[0] == '+')
                        {
                            if (autoDec)
                            {
                                Services.Log.LogEntry(thirdparam, "Addressing mode not supported for selected CPU.");
                                return string.Empty;
                            }
                            modes |= IndexModes.Inc1;
                            autoInc = true;
                            thirdparam = operand.GetNext();
                            if (!Token.IsEnd(thirdparam))
                            {
                                if (thirdparam.Name[0] == '+')
                                {
                                    modes |= IndexModes.Inc2;
                                    operand.MoveNext();
                                }
                                else
                                {
                                    Services.Log.LogEntry(line.Operands[^1], "Addressing mode not supported for selected CPU.");
                                    return string.Empty;
                                }
                            }
                            else if (modes.HasFlag(IndexModes.Indir))
                            {
                                Services.Log.LogEntry(line.Operands[^1], "Addressing mode not supported for selected CPU.");
                                return string.Empty;
                            }
                        }
                    }
                }
            }
            if ((operand.Current != null && !(operand.Current.Name.Equals("]") && operand.PeekNext() == null)))
                throw new SyntaxException(operand.Current, "Unexpected expression.");
            if (!double.IsNaN(offsetValue))
            {
                var forcedWidth = Modes.Implied;
                if (forcedMod.HasFlag(Modes.ForceWidth))
                {
                    forcedWidth = forcedMod ^ Modes.ForceWidth;
                    if (forcedWidth > Modes.Absolute)
                        throw new ExpressionException(line.Operands[1].Position, "Illegal size for width specifier.");
                }
                if (modes.HasFlag(IndexModes.Indir) && indexMode == IndexModes.None && offsRegMode == IndexModes.None)
                {
                    modes |= IndexModes.ExtInd;
                    Evaluations[0] = offsetValue;
                    disSb.Append($"${(int)Evaluations[0] & 0xFFFF:x4}");
                }
                else if (indexMode == IndexModes.PC8)
                {
                    int relOffIx = instruction.Size + 1;
                    var relative = Services.Output.GetRelativeOffset((int)offsetValue, relOffIx);
                    if (forcedWidth == Modes.Absolute || relative > sbyte.MaxValue || relative < sbyte.MinValue)
                    {
                        if (forcedWidth == Modes.ZeroPage)
                            throw new ExpressionException(line.Operands[3].Position,
                                "Offset is too far.");
                        relative = Services.Output.GetRelativeOffset((int)offsetValue, relOffIx + 1);
                        if (relative < short.MinValue || relative > short.MaxValue)
                        {
                            if (!Services.PassNeeded)
                                throw new ExpressionException(line.Operands[0], "Offset is too far.");
                            relative = short.MaxValue;
                        }
                        modes |= IndexModes.PC16;
                    }
                    disSb.Append($"${(int)offsetValue & 0xFFFF:x4}");
                    Evaluations[0] = relative;
                }
                else if (!modes.HasFlag(IndexModes.Indir) && forcedWidth == Modes.Implied && offsetValue >= -16 && offsetValue <= 15)
                {
                    Evaluations[0] = (int)offsetValue & 0x1f;
                    if (offsetValue < 0)
                        disSb.Append('-');
                    disSb.Append($"${(int)Math.Abs(offsetValue):x2}");
                }
                else if (forcedWidth != Modes.Absolute && offsetValue >= sbyte.MinValue && offsetValue <= sbyte.MaxValue)
                {
                    modes |= IndexModes.Offset8;
                    Evaluations[0] = (int)offsetValue & 0xff;
                    disSb.Append($"${(int)Evaluations[0]:x2}");
                }
                else
                {
                    if (forcedWidth == Modes.ZeroPage || offsetValue < short.MinValue || offsetValue > short.MaxValue)
                        throw new IllegalQuantityException(line.Operands[0], offsetValue);
                    modes |= IndexModes.Offset16;
                    Evaluations[0] = (int)offsetValue & 0xffff;
                    disSb.Append($"${(int)Evaluations[0]:x4}");
                }
            }
            else
            {
                if (forcedMod != Modes.Implied)
                    Services.Log.LogEntry(line.Filename, line.LineNumber, line.Operands[0].Position,
                        "Width specifier does not affect operand size.", false);
                if (offsRegMode != IndexModes.None)
                {
                    var ix = offsRegMode switch
                    {
                        IndexModes.AccA => "a",
                        IndexModes.AccB => "b",
                        _               => "d"
                    };
                    disSb.Append(ix);
                }
                else if (!autoInc && !autoDec)
                    modes |= IndexModes.ZeroOffs;
            }
            if ((modes & IndexModes.ExtInd) != IndexModes.ExtInd)
            {
                disSb.Append(',');
                var ix = indexMode switch
                {
                    IndexModes.PC8  => "pc",
                    IndexModes.XReg => "x",
                    IndexModes.YReg => "y",
                    IndexModes.UReg => "u",
                    _               => "s"
                };

                if (autoDec)
                {
                    disSb.Append('-');
                    if ((modes & IndexModes.OffsMask) == IndexModes.Dec2)
                        disSb.Append('-');
                    disSb.Append(ix);
                }
                else if (autoInc)
                {
                    disSb.Append(ix);
                    disSb.Append('+');
                    if ((modes & IndexModes.OffsMask) == IndexModes.Inc2)
                        disSb.Append('+');
                }
                else
                {
                    disSb.Append(ix);
                }
            }
            if (modes.HasFlag(IndexModes.Indir))
                disSb.Append(']');

            if (offsRegMode == IndexModes.None && !double.IsNaN(Evaluations[0]))
            {
                if (modes.HasFlag(IndexModes.Inc1))
                {
                    if (Evaluations[0] < sbyte.MinValue ||
                        Evaluations[0] > byte.MaxValue ||
                        (modes & IndexModes.Offset16) == IndexModes.Offset16)
                    {
                        Evaluations[1] = ((int)Evaluations[0] & 0xFFFF) / 256;
                        Evaluations[2] = (int)Evaluations[0] & 0xFF;
                    }
                    else if (modes != IndexModes.ExtInd)
                    {
                        Evaluations[1] = Evaluations[0];
                    }
                    Evaluations[0] = (int)modes;
                }
                else
                {
                    Evaluations[0] = (int)modes | (int)Evaluations[0];
                }
            }
            else
            {
                Evaluations[0] = (int)modes;
            }
            Services.Output.Add(instruction.Opcode, instruction.Opcode.Size());

            foreach (var eval in Evaluations.Where(e => !double.IsNaN(e)))
                Services.Output.Add(eval, 1);
            if (!Services.PassNeeded)
            {
                var sb = new StringBuilder();
                if (!Services.Options.NoAssembly)
                {
                    var byteString = Services.Output.GetBytesFrom(PCOnAssemble).ToString(PCOnAssemble, '.');
                    sb.Append(byteString.PadRight(Padding));
                }
                else
                {
                    sb.Append($".{PCOnAssemble:x4}                        ");
                }
                if (!Services.Options.NoDisassembly)
                    sb.Append(disSb.ToString().PadRight(18));
                else
                    sb.Append("                  ");
                if (!Services.Options.NoSource)
                    sb.Append(line.Source);
                return sb.ToString();
            }
            return string.Empty;
        }

        string AssemblePushPullExchange(SourceLine line)
        {
            if (line.Operands.Count == 0)
                throw new SyntaxException(line.Instruction.Position,
                    "Missing register(s).");
            var instruction = line.Instruction.Name.ToLower();
            var iterator = line.Operands.GetIterator();
            var isExchange = Reserved.IsOneOf("Exchanges", instruction);
            var lookup = isExchange ? _exchangeModes : _pushPullModes;
            byte registers = byte.MinValue;
            var registersEvaled = new SortedSet<StringView>(new PushPullComparer(!isExchange ? lookup : null));
            bool expectingSeparator = false;
            Token register;
            while ((register = iterator.GetNext()) != null)
            {
                if (register.IsSeparator() != expectingSeparator)
                    throw new SyntaxException(register, "Missing register.");

                if (!register.IsSeparator())
                {
                    if (!Token.IsEnd(iterator.PeekNext()))
                        throw new SyntaxException(iterator.PeekNext(), "Unexpected expression.");
                    var registerName = Services.Options.CaseSensitive ? register.Name.ToString() : register.Name.ToLower();
                    if (!lookup.TryGetValue(registerName, out var postbyte))
                        throw new SyntaxException(register, "Unexpected expression.");
                    if (registersEvaled.Contains(registerName))
                        throw new SyntaxException(register, $"Duplicate register \"{registerName}\".");
                    if (isExchange && registersEvaled.Count > 1)
                        throw new SyntaxException(register, "Unexpected expression.");
                    if (!isExchange && char.ToLower(registerName[0]) == instruction[^1])
                        throw new SyntaxException(register, $"Cannot use \"{instruction}\" with register \"{registerName}\".");
                    if (isExchange && registersEvaled.Count == 1)
                    {
                        registers <<= 4;
                        registers |= postbyte;
                    }
                    else
                    {
                        registers |= postbyte;
                    }
                    registersEvaled.Add(registerName);
                }
                expectingSeparator = !register.IsSeparator();
            }
            var instr = ActiveInstructions[(instruction, Modes.ZeroPage)];
            Services.Output.Add(instr.Opcode, 1);
            Services.Output.Add(registers);
            if (!Services.PassNeeded)
            {
                var sb = new StringBuilder();
                if (!Services.Options.NoAssembly)
                {
                    var byteString = Services.Output.GetBytesFrom(PCOnAssemble).ToString(PCOnAssemble, '.');
                    sb.Append(byteString.PadRight(Padding));
                }
                else
                {
                    sb.Append($".{PCOnAssemble:x4}                        ");
                }
                if (!Services.Options.NoDisassembly)
                    sb.Append($"{instruction} {string.Join(',', registersEvaled)}".PadRight(18));
                if (!Services.Options.NoSource)
                    sb.Append(line.Source);
                return sb.ToString();
            }
            return string.Empty;
        }

        public override bool Assembles(StringView s) => Reserved.IsReserved(s);

        public override bool IsReserved(StringView token)
            => base.IsReserved(token) || _exchangeModes.ContainsKey(token.ToString());

        #endregion

        #region Properties

        protected override bool PseudoBranchSupported => false;

        protected override bool SupportsDirectPage => CPU.Equals("m6809");

        #endregion
    }
}