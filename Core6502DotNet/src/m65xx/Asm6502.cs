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

namespace Core6502DotNet.m65xx
{
    /// <summary>
    /// A class responsible for assembly of 65xx source.
    /// </summary>
    public sealed partial class Asm6502 : MotorolaBase
    {
        #region Constructors

        /// <summary>
        /// Constructs a new instance of the 6502 assembler object.
        /// </summary>
        /// <param name="services">The shared <see cref="AssemblyServices"/> object.</param>
        public Asm6502(AssemblyServices services)
            :base(services)
        {
            Reserved.DefineType("Indeces",
                "s", "sp", "x", "y", "z");

            Reserved.DefineType("Registers",
                "a", "s", "sp", "x", "y", "z");

            Reserved.DefineType("Implieds",
                    "brk", "clc", "cld", "cla", "cle", "cli", "clv", "clx",
                    "cly", "csh", "dex", "dey", "dez", "inx", "iny", "inz",
                    "jam", "map", "nop", "pha", "phb", "phd", "phk", "php",
                    "phx", "phy", "phz", "pla", "plb", "pld", "plp", "plx",
                    "ply", "plz", "rts", "rti", "rtl", "rtn", "say", "sec",
                    "sed", "see", "sei", "set", "stp", "tax", "tay", "taz",
                    "tcd", "tcs", "tdc", "tsc", "tsx", "tsy", "txa", "txs",
                    "txy", "tya", "tys", "tyx", "tza", "wai", "wdm", "xba",
                    "xce"
                );

            Reserved.DefineType("Branches",
                    "bcc", "bcs", "beq", "bmi", "bne", "bpl", "bra", "bvc",
                    "bvs", "brl", "per", "blt", "bge", "bbr", "bbs"
                );

            Reserved.DefineType("Branches16",
                    "brl", "per", "blt", "bge"
                );

           
            Reserved.AddWord("RelativeSecond", "bbr");
            Reserved.AddWord("RelativeSecond", "bbs");
            Reserved.AddWord("RelativeSecond", "rmb");
            Reserved.AddWord("RelativeSecond", "smb");
            
            Reserved.DefineType("MoveMemory16",
                    "tai", "tdd", "tia", "tii", "tin"
                );


            Reserved.DefineType("Jumps",
                    "jmp", "jsr"
                );

            Reserved.DefineType("JumpsLong",
                    "jml", "jsl"
                );

            Reserved.DefineType("MoveMemory",
                    "mvn", "mvp"
                );

            Reserved.DefineType("LongShort",
                    ".m16", ".m8", ".x16", ".x8", ".mx16", ".mx8"
                );

            Reserved.DefineType("PseudoBranches",
                    "jcc", "jcs", "jeq", "jmi", "jne", "jpl", "jvc",
                    "jvs");

            Reserved.DefineType("Mnemonics",
                    "adc", "anc", "and", "ane", "arr", "asl", "asr", "asw",
                    "bit", "bsr", "cmp", "cop", "cpx", "cpy", "cpz", "dcp",
                    "dec", "dew", "dop", "eor", "inc", "inw", "isb", "jml",
                    "jmp", "jsl", "jsr", "las", "lax", "lda", "ldx", "ldy",
                    "ldz", "lsr", "neg", "ora", "pea", "pei", "phw", "rep",
                    "rla", "rol", "ror", "row", "rra", "sax", "sbc", "sep",
                    "sha", "shx", "shy", "slo", "sre", "st1", "st2", "sta",
                    "stx", "sty", "stz", "tam", "tas", "tma", "top", "trb",
                    "tsb", "tst"
                );

            // set architecture specific encodings
            Services.Encoding.SelectEncoding("petscii");
            Services.Encoding.Map("az", 'A');
            Services.Encoding.Map("AZ", 0xc1);
            Services.Encoding.Map('£', '\\');
            Services.Encoding.Map('↑', '^');
            Services.Encoding.Map('←', '_');
            Services.Encoding.Map('▌', 0xa1);
            Services.Encoding.Map('▄', 0xa2);
            Services.Encoding.Map('▔', 0xa3);
            Services.Encoding.Map('▁', 0xa4);
            Services.Encoding.Map('▏', 0xa5);
            Services.Encoding.Map('▒', 0xa6);
            Services.Encoding.Map('▕', 0xa7);
            Services.Encoding.Map('◤', 0xa9);
            Services.Encoding.Map('├', 0xab);
            Services.Encoding.Map('└', 0xad);
            Services.Encoding.Map('┐', 0xae);
            Services.Encoding.Map('▂', 0xaf);
            Services.Encoding.Map('┌', 0xb0);
            Services.Encoding.Map('┴', 0xb1);
            Services.Encoding.Map('┬', 0xb2);
            Services.Encoding.Map('┤', 0xb3);
            Services.Encoding.Map('▎', 0xb4);
            Services.Encoding.Map('▍', 0xb5);
            Services.Encoding.Map('▃', 0xb9);
            Services.Encoding.Map('✓', 0xba);
            Services.Encoding.Map('┘', 0xbd);
            Services.Encoding.Map('━', 0xc0);
            Services.Encoding.Map('♠', 0xc1);
            Services.Encoding.Map('│', 0xc2);
            Services.Encoding.Map('╮', 0xc9);
            Services.Encoding.Map('╰', 0xca);
            Services.Encoding.Map('╯', 0xcb);
            Services.Encoding.Map('╲', 0xcd);
            Services.Encoding.Map('╱', 0xce);
            Services.Encoding.Map('●', 0xd1);
            Services.Encoding.Map('♥', 0xd3);
            Services.Encoding.Map('╭', 0xd5);
            Services.Encoding.Map('╳', 0xd6);
            Services.Encoding.Map('○', 0xd7);
            Services.Encoding.Map('♣', 0xd8);
            Services.Encoding.Map('♦', 0xda);
            Services.Encoding.Map('┼', 0xdb);
            Services.Encoding.Map('π', 0xde);
            Services.Encoding.Map('◥', 0xdf);

            Services.Encoding.SelectEncoding("cbmscreen");
            Services.Encoding.Map("@Z", '\0');
            Services.Encoding.Map("az", 'A');
            Services.Encoding.Map('£', '\\');
            Services.Encoding.Map('π', '^'); // π is $5e in unshifted
            Services.Encoding.Map('↑', '^'); // ↑ is $5e in shifted
            Services.Encoding.Map('←', '_');
            Services.Encoding.Map('▌', '`');
            Services.Encoding.Map('▄', 'a');
            Services.Encoding.Map('▔', 'b');
            Services.Encoding.Map('▁', 'c');
            Services.Encoding.Map('▏', 'd');
            Services.Encoding.Map('▒', 'e');
            Services.Encoding.Map('▕', 'f');
            Services.Encoding.Map('◤', 'i');
            Services.Encoding.Map('├', 'k');
            Services.Encoding.Map('└', 'm');
            Services.Encoding.Map('┐', 'n');
            Services.Encoding.Map('▂', 'o');
            Services.Encoding.Map('┌', 'p');
            Services.Encoding.Map('┴', 'q');
            Services.Encoding.Map('┬', 'r');
            Services.Encoding.Map('┤', 's');
            Services.Encoding.Map('▎', 't');
            Services.Encoding.Map('▍', 'u');
            Services.Encoding.Map('▃', 'y');
            Services.Encoding.Map('✓', 'z');
            Services.Encoding.Map('┘', '}');
            Services.Encoding.Map('━', '@');
            Services.Encoding.Map('♠', 'A');
            Services.Encoding.Map('│', 'B');
            Services.Encoding.Map('╮', 'I');
            Services.Encoding.Map('╰', 'J');
            Services.Encoding.Map('╯', 'K');
            Services.Encoding.Map('╲', 'M');
            Services.Encoding.Map('╱', 'N');
            Services.Encoding.Map('●', 'Q');
            Services.Encoding.Map('♥', 'S');
            Services.Encoding.Map('╭', 'U');
            Services.Encoding.Map('╳', 'V');
            Services.Encoding.Map('○', 'W');
            Services.Encoding.Map('♣', 'X');
            Services.Encoding.Map('♦', 'Z');
            Services.Encoding.Map('┼', '[');
            Services.Encoding.Map('◥', '_');

            Services.Encoding.SelectEncoding("atascreen");
            Services.Encoding.Map(" _", '\0');

            Services.Encoding.SelectDefaultEncoding();
            _m16 = _x16 = false;
        }

        #endregion

        #region Methods

        protected override void OnReset()
        {
            if (_m16)
                SetImmediate(2, 'a');
            if (_x16)
            {
                SetImmediate(2, 'x');
                SetImmediate(2, 'y');
            }
            _m16 = _x16 = false;
        }

        protected override void OnSetCpu()
        {
            switch (CPU)
            {
                case "65816":
                    ActiveInstructions = s_opcodes6502.Concat(s_opcodes65C02)
                                                .Concat(s_opcodesW65C02)
                                                .Concat(s_opcodes65816)
                                                .ToDictionary(k => k.Key, k => k.Value);
                    break;
                case "HuC6280":
                    ActiveInstructions = s_opcodes6502.Concat(s_opcodes65C02.Where(o => (o.Value.Opcode & 0x0f) != 0x02))
                                                .Concat(s_opcodesR65C02)
                                                .Concat(s_opcodesW65C02)
                                                .Concat(s_opcodesHuC6280)
                                                .ToDictionary(k => k.Key, k => k.Value);
                    break;
                case "65CE02":
                    ActiveInstructions = s_opcodes6502.Where(o => (o.Value.Opcode & 0x1f) != 0x10) // exclude 6502 branch instructions
                                                    .Concat(s_opcodes65C02.Where(o => o.Value.Opcode != 0x80 && (o.Value.Opcode & 0x0f) != 0x02))
                                                    .Concat(s_opcodesR65C02)
                                                    .Concat(s_opcodes65CE02)
                                                    .ToDictionary(k => k.Key, k => k.Value);
                    break;
                case "R65C02":
                    ActiveInstructions = s_opcodes6502.Concat(s_opcodes65C02)
                                                .Concat(s_opcodesR65C02)
                                                .ToDictionary(k => k.Key, k => k.Value);
                    break;
                case "65CS02":
                    ActiveInstructions = s_opcodes6502.Concat(s_opcodes65C02)
                                                .Concat(s_opcodesW65C02)
                                                .ToDictionary(k => k.Key, k => k.Value);
                    break;
                case "W65C02":
                    ActiveInstructions = s_opcodes6502.Concat(s_opcodes65C02)
                                                .Concat(s_opcodesR65C02)
                                                .Concat(s_opcodesW65C02)
                                                .ToDictionary(k => k.Key, k => k.Value);
                    break;
                case "65C02":
                    ActiveInstructions = s_opcodes6502.Concat(s_opcodes65C02)
                                                .ToDictionary(k => k.Key, k => k.Value);
                    break;
                case "6502i":
                    ActiveInstructions = s_opcodes6502.Concat(s_opcodes6502i)
                                                .ToDictionary(k => k.Key, k => k.Value);
                    break;
                default:
                    ActiveInstructions = new Dictionary<(string Mnem, Modes mode), CpuInstruction>(s_opcodes6502);
                    break;

            }
        }

        void SetImmediate(int size, char register)
        {
            Modes currmode, newmode;
            if (size == 2)
            {
                newmode = Modes.Immediate;
                currmode = newmode | Modes.Absolute;
            }
            else
            {
                currmode = Modes.Immediate;
                newmode = currmode | Modes.Absolute;
            }
            var immediates = ActiveInstructions.Where(kvp =>
            {
                if (kvp.Key.Mode == currmode)
                {
                    if (kvp.Key.Mnem[2] == register)
                        return true;
                    if (register == 'a' && (kvp.Value.CPU.Equals("6502") || kvp.Value.CPU.Equals("65C02")))
                        return kvp.Key.Mnem[2] != 'x' && kvp.Key.Mnem[2] != 'y';
                }
                return false;
            });
            var nonImmediates = ActiveInstructions.Except(immediates);

            ActiveInstructions = nonImmediates.Concat(immediates.ToDictionary(kvp => (kvp.Key.Mnem, newmode),
                                         kvp => new CpuInstruction(kvp.Value.CPU, kvp.Value.Opcode, size)))
                                                .ToDictionary(k => k.Key, k => k.Value);
        }

        protected override Modes ParseOperand(SourceLine line)
        {
            var mode = Modes.Implied;
            var instruction = line.InstructionName;

            if (line.OperandHasToken)
            {
                // we are setting these variables because they can change
                var operand = line.Operand;
                var operandChildren = operand.Children;
                var firstDelim = operandChildren[0];
                if (firstDelim.Children.Count == 0)
                    throw new SyntaxException(firstDelim.Position, "Expression expected.");

                var first = firstDelim.Children[0];
                var firstDelimChildren = firstDelim.Children;

                // test if we are setting the operand size or direct page
                var forcedMode = GetForcedModifier(first, firstDelimChildren.Count);
                if (forcedMode != Modes.Implied)
                {
                    mode = forcedMode;
                    first = firstDelim.Children[1];
                    firstDelimChildren = firstDelimChildren.Skip(1).ToList();
                }
                if (first.Name.Equals("a"))
                {
                    // e.g., lsr a
                    if (mode != Modes.Implied)
                        throw new SyntaxException(first.Position, "Forced bit-width applied to an implied mode instruction.");

                    return mode;
                }
                mode |= Modes.ZeroPage;
                if (first.Name.Equals("#"))
                {
                    if (firstDelimChildren.Count < 2)
                        throw new SyntaxException(first.Position, "Missing operand.");

                    mode |= Modes.Immediate;

                    var size = Evaluate(firstDelimChildren.Skip(1), 0);

                    if (!mode.HasFlag(Modes.ForceWidth))
                        mode |= size;
                }
                else if (first.Name.Equals("[") || first.Name.Equals("("))
                {
                    if (first.Children.Count == 0)
                        throw new SyntaxException(first.Position, "Missing one or more operands.");

                    if (first.Name.Equals("["))
                    {
                        mode |= Modes.DirectPage;
                    }
                    else if (firstDelimChildren.Count == 1) // test in fact if we are indirect or dealing with a math expression in parantheses
                    {
                        mode |= Modes.Indirect;

                        // check if there is a ",<register>" at the end of the indirect expression (
                        if (first.Children.Count > 1 && first.Children[1].OperatorType == OperatorType.Separator)
                        {
                            if (first.Children.Count > 2)
                                throw new ExpressionException(first.LastChild.Position, "Unexpected argument found.");

                            if (first.Children[1].Children.Count == 1)
                            {
                                var firstAfterComma = first.Children[1].Children[0];
                                if (Reserved.IsOneOf("Indeces", firstAfterComma.Name))
                                {
                                    if (first.Children[1].Children.Count > 2)
                                        throw new ExpressionException(first.Children[1].LastChild.Position, "Unexpected argument found.");
                                    if (firstAfterComma.Name.Equals("x"))
                                        mode |= Modes.InnerX;
                                    else if (firstAfterComma.Name.Equals("s"))
                                        mode |= Modes.IndexedS;
                                    else if (firstAfterComma.Name.Equals("sp"))
                                        mode |= Modes.IndexedSp;
                                    else
                                        throw new ExpressionException(firstAfterComma.Position, $"Illegal index \"{firstAfterComma.Name}\" specified.");
                                }
                                else
                                {
                                    throw new ExpressionException(firstAfterComma.Position, $"Unexpected expression \"{firstAfterComma.Name}\" encountered.");
                                }
                            }
                            else
                            {
                                throw new ExpressionException(first.LastChild.Position, $"Unexpected expression \"{first.LastChild.Name}\" encountered.");
                            }
                        }
                    }
                }
                IEnumerable<Token> firstOperandExpression = null;
                var memoryMode = mode & Modes.MemModMask;
                if ((memoryMode & Modes.DirIndMask) != 0)
                {
                    if (first.Children.Count == 0 || first.Children[0].Children.Count == 0)
                        throw new ExpressionException(first.Position, "Expression expected.");

                    var firstInnerSep = first.Children[0];
                    firstOperandExpression = firstInnerSep.Children;
                }
                else if (!mode.HasFlag(Modes.Immediate))
                {
                    firstOperandExpression = firstDelimChildren;
                }
                if (firstOperandExpression != null)
                {
                    var resultmode = Evaluate(firstOperandExpression, 0);
                    if (!mode.HasFlag(Modes.ForceWidth))
                        mode |= resultmode;
                    else if ((int)forcedMode < (int)resultmode)
                        throw new ExpressionException(line.Operand.LastChild.Position, "Width specifier does not match expression.");
                }

                // capture the outer ",<register>" if any
                if (operandChildren.Count > 1)
                {
                    var lastCommaDelim = operandChildren[^1];
                    if (operandChildren.Count > 3 || lastCommaDelim.Children.Count == 0)
                        throw new ExpressionException(line.Operand.LastChild.Position, "Bad expression.");

                    var indexerIndex = -1;
                    if (Reserved.IsOneOf("Indeces", lastCommaDelim.Children[0].Name))
                    {
                        if (lastCommaDelim.Children.Count != 1)
                            throw new ExpressionException(lastCommaDelim.Position, "Bad expression.");

                        var firstAfterComma = lastCommaDelim.Children[0];
                        if (firstAfterComma.Name.Equals("x"))
                            mode |= Modes.IndexedX;
                        else if (firstAfterComma.Name.Equals("y"))
                            mode |= Modes.IndexedY;
                        else if (firstAfterComma.Name.Equals("z"))
                            mode |= Modes.IndexedZ;
                        else
                            throw new ExpressionException(firstAfterComma.Position, $"Illegal index \"{firstAfterComma.Name}\" specified.");

                        indexerIndex = operandChildren.Count - 1;
                    }

                    if (operandChildren.Count == 2 && indexerIndex != 1)
                    {
                        mode |= Modes.TwoOperand;
                        mode |= Evaluate(lastCommaDelim.Children, 1);

                    }
                    else if (operandChildren.Count == 3)
                    {
                        mode |= Evaluate(operandChildren[1].Children, 1);
                        if (indexerIndex == 2)
                        {
                            mode |= Modes.TwoOperand;
                        }
                        else
                        {
                            mode |= Modes.ThreeOperand;
                            mode |= Evaluate(operandChildren[2].Children, 2);
                        }
                    }
                }
                if (Reserved.IsOneOf("RelativeSecond", instruction))
                {
                    if (double.IsNaN(Evaluations[0]) ||
                        Evaluations[0] < 0 || Evaluations[0] > 7)
                        throw new ExpressionException(line.Operand.Position, $"First operand for \"{instruction}\" must be 0 to 7.");

                    mode |= (Modes)((int)Evaluations[0] << 14) | Modes.Bit0;
                    if (double.IsNaN(Evaluations[1]))
                        throw new ExpressionException(line.Operand.Position, "Missing direct page operand.");

                    Evaluations[0] = Evaluations[1];

                    if (Reserved.IsOneOf("Branches", instruction))
                    {
                        if (double.IsNaN(Evaluations[2]))
                            throw new ExpressionException(line.Operand.Position, "Missing branch address.");
                        Evaluations[1] = Evaluations[2];
                    }
                    else
                    {
                        Evaluations[1] = double.NaN;
                    }
                    Evaluations[2] = double.NaN;

                }
                if (Reserved.IsOneOf("Branches", instruction))
                {
                    mode = (mode & ~Modes.Absolute) | Modes.Relative;
                    if (Reserved.IsOneOf("Branches16", instruction))
                        mode |= Modes.Absolute;
                }
            }
            return mode;
        }

        void AssembleLongShort(SourceLine line)
        {
            if (!CPU.Equals("65816"))
            {
                Services.Log.LogEntry(line, line.Instruction, $"Directive \"{line.InstructionName}\" is ignored for CPU \"{CPU}\"", false);
                return;
            }
            var size = line.InstructionName.Contains("16") ? 3 : 2;
            if (line.InstructionName[1] == 'm')
            {
                _m16 = size == 3;
                SetImmediate(size, 'a');
            }
            if (line.InstructionName[1] == 'x')
            {
                _x16 = size == 3;
                SetImmediate(size, 'x');
                SetImmediate(size, 'y');
            }
        }

        string AssemblePseudoBranch(SourceLine line)
        {
            if (!line.OperandHasToken)
            {
                Services.Log.LogEntry(line, line.Instruction, "Missing branch location.");
                return string.Empty;
            }
            var offset = Services.Evaluator.Evaluate(line.Operand, short.MinValue, ushort.MaxValue);
            int addrOffs;
            int minValue, maxValue;
            double relative;
            var mode = Modes.Relative;
            if (CPU.Equals("65CE02"))
            {
                addrOffs = 3;
                minValue = short.MinValue;
                maxValue = short.MaxValue;
                mode |= Modes.Absolute;
            }
            else
            {
                addrOffs = 2;
                minValue = sbyte.MinValue;
                maxValue = sbyte.MaxValue;
                mode |= Modes.ZeroPage;
            }
            relative = Services.Output.GetRelativeOffset((int)offset, addrOffs);
            var mnemonic = line.InstructionName;
            if (relative < minValue || relative > maxValue)
            {
                mnemonic = s_pseudoBranchTranslations[mnemonic];
                relative = 3;
            }
            else
            {
                mnemonic = "b" + mnemonic.Substring(1);
                offset = double.NaN;
            }
            var mnmemmode = (mnemonic, mode);
            Services.Output.Add(ActiveInstructions[mnmemmode].Opcode, 1);
            if (double.IsNaN(offset))
            {
                Services.Output.Add(relative, addrOffs - 1);
            }
            else
            {
                Services.Output.Add(relative, 1);
                Services.Output.Add((byte)0x4c);
                Services.Output.Add(offset, 2);
            }
            if (Services.PassNeeded || string.IsNullOrEmpty(Services.Options.ListingFile))
                return string.Empty;
            var sb = new StringBuilder();

            if (!Services.Options.NoAssembly)
                sb.Append(Services.Output.GetBytesFrom(PCOnAssemble).Take(2).ToString(PCOnAssemble, '.', true).PadRight(Padding));
            else
                sb.Append($".{PCOnAssemble:x4}                     ");

            if (!Services.Options.NoDisassembly)
            {
                sb.Append($"{mnemonic} ");
                if (double.IsNaN(offset))
                {
                    sb.Append($"${Services.Output.LogicalPC + (int)relative:x4}");
                    if (!Services.Options.NoSource)
                        sb.Append($"         {line.UnparsedSource}");
                }
                else
                {
                    sb.Append($"${Services.Output.LogicalPC:x4}");
                    if (!Services.Options.NoSource)
                        sb.Append($"         {line.UnparsedSource}");
                    sb.AppendLine();
                    if (!Services.Options.NoAssembly)
                        sb.Append(Services.Output.GetBytesFrom(PCOnAssemble + 2).ToString(PCOnAssemble + addrOffs, '.').PadRight(Padding));
                    else
                        sb.Append($".{PCOnAssemble:x4}                     ");

                    sb.Append($"jmp ${(int)offset:x4}");

                }
            }
            else if (!Services.Options.NoSource)
            {
                sb.Append($"                 {line.UnparsedSource}");
            }
            return sb.ToString();
        }

        protected override string OnAssembleLine(SourceLine line)
        {
            var instruction = line.InstructionName;
            if (Reserved.IsOneOf("LongShort", instruction))
            {
                AssembleLongShort(line);
                return string.Empty;
            }
            if (Reserved.IsOneOf("PseudoBranches", instruction))
            {
                return AssemblePseudoBranch(line);
            }
            return base.OnAssembleLine(line);
        }

        protected override bool IsCpuValid(string cpu) => SupportedCPUs.Contains(cpu);

        public override bool Assembles(string s) => IsReserved(s) && !Reserved.IsOneOf("Registers", s);

        #endregion

        #region Properties

        protected override bool PseudoBranchSupported => true;

        #endregion
    }
}