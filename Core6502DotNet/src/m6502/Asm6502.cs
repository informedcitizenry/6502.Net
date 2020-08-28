//-----------------------------------------------------------------------------
// Copyright (c) 2017-2020 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Core6502DotNet.m6502
{
    /// <summary>
    /// A class responsible for assembly of 65xx source.
    /// </summary>
    public sealed partial class Asm6502 : AssemblerBase
    {
        const int Padding = 25;

        #region Constructors

        /// <summary>
        /// Constructs a new instance of the 6502 assembler object.
        /// </summary>
        public Asm6502()
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

            Reserved.DefineType("Rockwell",
                    "bbr", "bbs", "rmb", "smb"
                );

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

            Reserved.DefineType("CPU",
                    ".cpu");

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
            Assembler.Encoding.SelectEncoding("petscii");
            Assembler.Encoding.Map("az", 'A');
            Assembler.Encoding.Map("AZ", 0xc1);
            Assembler.Encoding.Map('£', '\\');
            Assembler.Encoding.Map('↑', '^');
            Assembler.Encoding.Map('←', '_');
            Assembler.Encoding.Map('▌', 0xa1);
            Assembler.Encoding.Map('▄', 0xa2);
            Assembler.Encoding.Map('▔', 0xa3);
            Assembler.Encoding.Map('▁', 0xa4);
            Assembler.Encoding.Map('▏', 0xa5);
            Assembler.Encoding.Map('▒', 0xa6);
            Assembler.Encoding.Map('▕', 0xa7);
            Assembler.Encoding.Map('◤', 0xa9);
            Assembler.Encoding.Map('├', 0xab);
            Assembler.Encoding.Map('└', 0xad);
            Assembler.Encoding.Map('┐', 0xae);
            Assembler.Encoding.Map('▂', 0xaf);
            Assembler.Encoding.Map('┌', 0xb0);
            Assembler.Encoding.Map('┴', 0xb1);
            Assembler.Encoding.Map('┬', 0xb2);
            Assembler.Encoding.Map('┤', 0xb3);
            Assembler.Encoding.Map('▎', 0xb4);
            Assembler.Encoding.Map('▍', 0xb5);
            Assembler.Encoding.Map('▃', 0xb9);
            Assembler.Encoding.Map('✓', 0xba);
            Assembler.Encoding.Map('┘', 0xbd);
            Assembler.Encoding.Map('━', 0xc0);
            Assembler.Encoding.Map('♠', 0xc1);
            Assembler.Encoding.Map('│', 0xc2);
            Assembler.Encoding.Map('╮', 0xc9);
            Assembler.Encoding.Map('╰', 0xca);
            Assembler.Encoding.Map('╯', 0xcb);
            Assembler.Encoding.Map('╲', 0xcd);
            Assembler.Encoding.Map('╱', 0xce);
            Assembler.Encoding.Map('●', 0xd1);
            Assembler.Encoding.Map('♥', 0xd3);
            Assembler.Encoding.Map('╭', 0xd5);
            Assembler.Encoding.Map('╳', 0xd6);
            Assembler.Encoding.Map('○', 0xd7);
            Assembler.Encoding.Map('♣', 0xd8);
            Assembler.Encoding.Map('♦', 0xda);
            Assembler.Encoding.Map('┼', 0xdb);
            Assembler.Encoding.Map('π', 0xde);
            Assembler.Encoding.Map('◥', 0xdf);

            Assembler.Encoding.SelectEncoding("cbmscreen");
            Assembler.Encoding.Map("@Z", '\0');
            Assembler.Encoding.Map("az", 'A');
            Assembler.Encoding.Map('£', '\\');
            Assembler.Encoding.Map('π', '^'); // π is $5e in unshifted
            Assembler.Encoding.Map('↑', '^'); // ↑ is $5e in shifted
            Assembler.Encoding.Map('←', '_');
            Assembler.Encoding.Map('▌', '`');
            Assembler.Encoding.Map('▄', 'a');
            Assembler.Encoding.Map('▔', 'b');
            Assembler.Encoding.Map('▁', 'c');
            Assembler.Encoding.Map('▏', 'd');
            Assembler.Encoding.Map('▒', 'e');
            Assembler.Encoding.Map('▕', 'f');
            Assembler.Encoding.Map('◤', 'i');
            Assembler.Encoding.Map('├', 'k');
            Assembler.Encoding.Map('└', 'm');
            Assembler.Encoding.Map('┐', 'n');
            Assembler.Encoding.Map('▂', 'o');
            Assembler.Encoding.Map('┌', 'p');
            Assembler.Encoding.Map('┴', 'q');
            Assembler.Encoding.Map('┬', 'r');
            Assembler.Encoding.Map('┤', 's');
            Assembler.Encoding.Map('▎', 't');
            Assembler.Encoding.Map('▍', 'u');
            Assembler.Encoding.Map('▃', 'y');
            Assembler.Encoding.Map('✓', 'z');
            Assembler.Encoding.Map('┘', '}');
            Assembler.Encoding.Map('━', '@');
            Assembler.Encoding.Map('♠', 'A');
            Assembler.Encoding.Map('│', 'B');
            Assembler.Encoding.Map('╮', 'I');
            Assembler.Encoding.Map('╰', 'J');
            Assembler.Encoding.Map('╯', 'K');
            Assembler.Encoding.Map('╲', 'M');
            Assembler.Encoding.Map('╱', 'N');
            Assembler.Encoding.Map('●', 'Q');
            Assembler.Encoding.Map('♥', 'S');
            Assembler.Encoding.Map('╭', 'U');
            Assembler.Encoding.Map('╳', 'V');
            Assembler.Encoding.Map('○', 'W');
            Assembler.Encoding.Map('♣', 'X');
            Assembler.Encoding.Map('♦', 'Z');
            Assembler.Encoding.Map('┼', '[');
            Assembler.Encoding.Map('◥', '_');

            Assembler.Encoding.SelectEncoding("atascreen");
            Assembler.Encoding.Map(" _", '\0');

            Assembler.Encoding.SelectDefaultEncoding();
            Assembler.PassChanged += OnPassChanged;

            Reset();

            s_evaled = new double[3];
        }

        #endregion

        #region Methods

        void OnPassChanged(object sender, EventArgs args) => Reset();

        void Reset()
        {
            _m16 = _x16 = false;
            if (!string.IsNullOrEmpty(Assembler.Options.CPU))
            {
                SetCpu(Assembler.Options.CPU);
            }
            else
            {
                _selectedInstructions = new Dictionary<(string Mnem, Modes Mode), CpuInstruction>(s_opcodes6502);
                _cpu = "6502";
            }
        }

        void SetCpu(string cpu)
        {
            if (!SupportedCPUs.Contains(cpu))
            {
                var error = string.Format($"Invalid CPU \"{cpu}\" specified");
                throw new ArgumentException(error);
            }
            _cpu = cpu;
            switch (_cpu)
            {
                case "65816":
                    _selectedInstructions = s_opcodes6502.Concat(s_opcodes65C02)
                                                        .Concat(s_opcodesW65C02)
                                                        .Concat(s_opcodes65816)
                                                        .ToDictionary(k => k.Key, k => k.Value);
                    break;
                case "HuC6280":
                    _selectedInstructions = s_opcodes6502.Concat(s_opcodes65C02.Where(o => (o.Value.Opcode & 0x0f) != 0x02))
                                                        .Concat(s_opcodesR65C02)
                                                        .Concat(s_opcodesW65C02)
                                                        .Concat(s_opcodesHuC6280)
                                                        .ToDictionary(k => k.Key, k => k.Value);
                    break;
                case "65CE02":
                    _selectedInstructions = s_opcodes6502.Where(o => (o.Value.Opcode & 0x1f) != 0x10) // exclude 6502 branch instructions
                                                        .Concat(s_opcodes65C02.Where(o => o.Value.Opcode != 0x80 && (o.Value.Opcode & 0x0f) != 0x02))
                                                        .Concat(s_opcodesR65C02)
                                                        .Concat(s_opcodes65CE02)
                                                        .ToDictionary(k => k.Key, k => k.Value);
                    break;
                case "R65C02":
                    _selectedInstructions = s_opcodes6502.Concat(s_opcodes65C02)
                                                        .Concat(s_opcodesR65C02)
                                                        .ToDictionary(k => k.Key, k => k.Value);
                    break;
                case "65CS02":
                    _selectedInstructions = s_opcodes6502.Concat(s_opcodes65C02)
                                                        .Concat(s_opcodesW65C02)
                                                        .ToDictionary(k => k.Key, k => k.Value);
                    break;
                case "W65C02":
                    _selectedInstructions = s_opcodes6502.Concat(s_opcodes65C02)
                                                        .Concat(s_opcodesR65C02)
                                                        .Concat(s_opcodesW65C02)
                                                        .ToDictionary(k => k.Key, k => k.Value);
                    break;
                case "65C02":
                    _selectedInstructions = s_opcodes6502.Concat(s_opcodes65C02)
                                                        .ToDictionary(k => k.Key, k => k.Value);
                    break;
                case "6502i":
                    _selectedInstructions = s_opcodes6502.Concat(s_opcodes6502i)
                                                        .ToDictionary(k => k.Key, k => k.Value);
                    break;
                default:
                    _selectedInstructions = new ReadOnlyDictionary<(string Mnem, Modes mode), CpuInstruction>(s_opcodes6502);
                    break;

            }
            // if any cpu change is done within pass, look at saved accumulator and index
            // modes.
            if (_m16)
                SetImmediate(3, 'a');
            if (_x16)
            {
                SetImmediate(3, 'x');
                SetImmediate(3, 'y');
            }
        }

        void SetCpu(SourceLine line)
        {
            if (!line.OperandHasToken)
            {
                Assembler.Log.LogEntry(line, $"One or more parameters expected for directive \".cpu\".");
                return;
            }
            var cpu = line.Operand.Children[0].Children[0].Name;
            if (!cpu.EnclosedInDoubleQuotes())
            {
                Assembler.Log.LogEntry(line, "Directive \".cpu\" expects a string literal expresion.");
                return;
            }
            try
            {
                SetCpu(cpu.TrimOnce('"'));
            }
            catch (ArgumentException argEx)
            {
                Assembler.Log.LogEntry(line, line.Instruction, argEx.Message);
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
            var immediates = _selectedInstructions.Where(kvp =>
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
            var nonImmediates = _selectedInstructions.Except(immediates);

            _selectedInstructions = new ReadOnlyDictionary<(string, Modes), CpuInstruction>(
                nonImmediates.Concat(immediates.ToDictionary(kvp => (kvp.Key.Mnem, newmode),
                                                             kvp => new CpuInstruction(kvp.Value.CPU, kvp.Value.Opcode, size)))
                             .ToDictionary(k => k.Key, k => k.Value));
        }

        Modes Evaluate(IEnumerable<Token> tokens, int operandIndex)
        {
            var mode = Modes.ZeroPage;
            var result = Evaluator.Evaluate(tokens, Int24.MinValue, UInt24.MaxValue);
            if (result < sbyte.MinValue || result > byte.MaxValue)
            {
                mode |= Modes.Absolute;
                if (result < short.MinValue || result > ushort.MaxValue)
                    mode |= Modes.Long;
            }
            s_evaled[operandIndex] = result;
            return mode;
        }

        Modes ParseOperand(SourceLine line)
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
                var forcedMode = Modes.Implied;
                if (first.Name.Equals("[") && firstDelimChildren.Count > 1)
                {
                    var opSize = Evaluator.Evaluate(first.Children, 8, 24);
                    forcedMode = opSize switch
                    {
                        8  => Modes.ZeroPage,
                        16 => Modes.Absolute,
                        24 => Modes.Long,
                        _  => throw new ExpressionException(first.Position, $"Illegal quantity {opSize} for bit-width specifier."),
                    };
                    mode = forcedMode | Modes.ForceWidth;

                    // now we have to re-tokenize the operand string because essentially we've changed it
                    var next = line.ParsedSource.Substring(firstDelimChildren[1].Position - 1);
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
                                throw new SyntaxException(first.LastChild.Position, "Unexpected argument found.");

                            if (first.Children[1].Children.Count == 1)
                            {
                                var firstAfterComma = first.Children[1].Children[0];
                                if (Reserved.IsOneOf("Indeces", firstAfterComma.Name))
                                {
                                    if (first.Children[1].Children.Count > 2)
                                        throw new SyntaxException(first.Children[1].LastChild.Position, "Unexpected argument found.");
                                    if (firstAfterComma.Name.Equals("x"))
                                        mode |= Modes.InnerX;
                                    else if (firstAfterComma.Name.Equals("s"))
                                        mode |= Modes.IndexedS;
                                    else if (firstAfterComma.Name.Equals("sp"))
                                        mode |= Modes.IndexedSp;
                                    else
                                        throw new SyntaxException(firstAfterComma.Position, $"Illegal index \"{firstAfterComma.Name}\" specified.");
                                }
                                else
                                {
                                    throw new SyntaxException(firstAfterComma.Position, $"Unexpected expression \"{firstAfterComma.Name}\" encountered.");
                                }
                            }
                            else
                            {
                                throw new SyntaxException(first.LastChild.Position, $"Unexpected expression \"{first.LastChild.Name}\" encountered.");
                            }
                        }
                    }
                }
                IEnumerable<Token> firstOperandExpression = null;
                var memoryMode = mode & Modes.MemModMask;
                if ((memoryMode & Modes.DirIndMask) != 0)
                {
                    if (first.Children.Count == 0 || first.Children[0].Children.Count == 0)
                        throw new SyntaxException(first.Position, "Expression expected.");

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
                        throw new SyntaxException(line.Operand.LastChild.Position, "Bad expression.");

                    var indexerIndex = -1;
                    if (Reserved.IsOneOf("Indeces", lastCommaDelim.Children[0].Name))
                    {
                        if (lastCommaDelim.Children.Count != 1)
                            throw new SyntaxException(lastCommaDelim.Position, "Bad expression.");

                        var firstAfterComma = lastCommaDelim.Children[0];
                        if (firstAfterComma.Name.Equals("x"))
                            mode |= Modes.IndexedX;
                        else if (firstAfterComma.Name.Equals("y"))
                            mode |= Modes.IndexedY;
                        else if (firstAfterComma.Name.Equals("z"))
                            mode |= Modes.IndexedZ;
                        else
                            throw new SyntaxException(firstAfterComma.Position, $"Illegal index \"{firstAfterComma.Name}\" specified.");

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
                if (Reserved.IsOneOf("Rockwell", instruction))
                {
                    if (double.IsNaN(s_evaled[0]) ||
                        s_evaled[0] < 0 || s_evaled[0] > 7)
                        throw new ExpressionException(line.Operand.Position, $"First operand for \"{instruction}\" must be 0 to 7.");

                    mode |= (Modes)((int)s_evaled[0] << 14) | Modes.Bit0;
                    if (double.IsNaN(s_evaled[1]))
                        throw new SyntaxException(line.Operand.Position, "Missing direct page operand.");

                    s_evaled[0] = s_evaled[1];

                    if (Reserved.IsOneOf("Branches", instruction))
                    {
                        if (double.IsNaN(s_evaled[2]))
                            throw new SyntaxException(line.Operand.Position, "Missing branch address.");
                        s_evaled[1] = s_evaled[2];
                    }
                    else
                    {
                        s_evaled[1] = double.NaN;
                    }
                    s_evaled[2] = double.NaN;

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

        (Modes mode, CpuInstruction instruction) GetModeInstruction(SourceLine line)
        {
            var instruction = line.InstructionName;
            var mnemmode = (Mnem: instruction, Mode: ParseOperand(line));
            
            // remember if force width bit was set
            var forceWidth = mnemmode.Mode.HasFlag(Modes.ForceWidth);

            // drop the force width bit off
            mnemmode.Mode &= Modes.ModeMask;
             if (!_selectedInstructions.TryGetValue(mnemmode, out CpuInstruction foundInstruction) && !forceWidth)
            {
                var sizeModeBit = (int)(Modes.ZeroPage);//(Modes.Indirect | Modes.SizeMask);
                while (!_selectedInstructions.TryGetValue(mnemmode, out foundInstruction))
                {
                    sizeModeBit <<= 1;
                    if (sizeModeBit > 7)
                        break;
                    mnemmode.Mode = (mnemmode.Mode & Modes.MemModMask) | (Modes)sizeModeBit | Modes.ZeroPage;
                }
            }
            return (mnemmode.Mode, foundInstruction);
        }

        void AssembleLongShort(SourceLine line)
        {
            if (!_cpu.Equals("65816"))
            {
                Assembler.Log.LogEntry(line, line.Instruction, $"Directive \"{line.InstructionName}\" is ignored for CPU \"{_cpu}\"", false);
                return;
            }
            var size = line.InstructionName.Contains("16") ? 3 : 2;
            if (line.InstructionName[1] == 'm')
            {
                SetImmediate(size, 'a');
                _m16 = size == 3;
            }
            if (line.InstructionName.Contains("x"))
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
                Assembler.Log.LogEntry(line, line.Instruction, "Missing branch location.");
                return string.Empty;
            }
            var offset = Evaluator.Evaluate(line.Operand, short.MinValue, ushort.MaxValue);
            int addrOffs;
            int minValue, maxValue;
            double relative;
            var mode = Modes.Relative;
            if (_cpu.Equals("65CE02"))
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
            relative = Assembler.Output.GetRelativeOffset((int)offset, addrOffs);
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
            Assembler.Output.Add(_selectedInstructions[mnmemmode].Opcode, 1);
            if (double.IsNaN(offset))
            {
                Assembler.Output.Add(relative, addrOffs - 1);
            }
            else
            {
                Assembler.Output.Add(relative, 1);
                Assembler.Output.Add((byte)0x4c);
                Assembler.Output.Add(offset, 2);
            }
            if (Assembler.PassNeeded || string.IsNullOrEmpty(Assembler.Options.ListingFile))
                return string.Empty;
            var sb = new StringBuilder();

            if (!Assembler.Options.NoAssembly)
                sb.Append(Assembler.Output.GetBytesFrom(PCOnAssemble).Take(2).ToString(PCOnAssemble, '.', true).PadRight(Padding));
            else
                sb.Append($".{PCOnAssemble:x4}                     ");

            if (!Assembler.Options.NoDisassembly)
            {
                sb.Append($"{mnemonic} ");
                if (double.IsNaN(offset))
                {
                    sb.Append($"${Assembler.Output.LogicalPC + (int)relative:x4}");
                    if (!Assembler.Options.NoSource)
                        sb.Append($"         {line.UnparsedSource}");
                }
                else
                {
                    sb.Append($"${Assembler.Output.LogicalPC:x4}");
                    if (!Assembler.Options.NoSource)
                        sb.Append($"         {line.UnparsedSource}");
                    sb.AppendLine();
                    if (!Assembler.Options.NoAssembly)
                        sb.Append(Assembler.Output.GetBytesFrom(PCOnAssemble + 2).ToString(PCOnAssemble + addrOffs, '.').PadRight(Padding));
                    else
                        sb.Append($".{PCOnAssemble:x4}                     ");

                    sb.Append($"jmp ${(int)offset:x4}");

                }
            }
            else if (!Assembler.Options.NoSource)
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
            if (Reserved.IsOneOf("CPU", instruction))
            {
                SetCpu(line);
                return string.Empty;
            }
            if (Reserved.IsOneOf("PseudoBranches", instruction))
            {
                return AssemblePseudoBranch(line);
            }
            s_evaled[0] =
            s_evaled[1] =
            s_evaled[2] = double.NaN;
            var modeInstruction = GetModeInstruction(line);
            if (!string.IsNullOrEmpty(modeInstruction.instruction.CPU))
            {
                if (modeInstruction.mode.HasFlag(Modes.RelativeBit))
                {
                    int offsIx;
                    if (Reserved.IsOneOf("Rockwell", instruction))
                        offsIx = 1;
                    else
                        offsIx = 0;

                    try
                    {
                        s_evaled[offsIx] = modeInstruction.mode == Modes.RelativeAbs
                            ? Convert.ToInt16(Assembler.Output.GetRelativeOffset((int)s_evaled[offsIx], 3))
                            : Convert.ToSByte(Assembler.Output.GetRelativeOffset((int)s_evaled[offsIx], 2 + offsIx));
                    }
                    catch (OverflowException)
                    {
                        // don't worry about overflows for relative offsets if passes are still needed
                        if (!Assembler.PassNeeded)
                        {
                            Assembler.Log.LogEntry(line, line.Operand,
                                "Relative offset for branch was too far. Consider using a pseudo branch directive.");
                            return string.Empty;
                        }
                        s_evaled[offsIx] = 0;
                    }
                }
                var operandSize = (modeInstruction.mode & Modes.SizeMask) switch
                {
                    Modes.Implied   => 0,
                    Modes.ZeroPage  => 1,
                    Modes.Absolute  => 2,
                    _               => 3
                };
                // start adding to the output
                Assembler.Output.Add(modeInstruction.instruction.Opcode, 1);

                if (operandSize > 0)
                {
                    // add operand bytes
                    for (int i = 0; i < 3 && !double.IsNaN(s_evaled[i]); i++)
                    {
                        if ((modeInstruction.mode & Modes.TestBitFlag) != 0 &&
                             modeInstruction.mode.HasFlag(Modes.TwoOpBit) &&
                                i == 0)
                        { // The Hudson test bit instructions
                            if (s_evaled[i] >= sbyte.MinValue && s_evaled[i] <= byte.MaxValue)
                                Assembler.Output.Add(s_evaled[i], 1);
                        }
                        else
                        {
                            if (s_evaled[i].Size() > operandSize)
                                break;
                            else
                                Assembler.Output.Add(s_evaled[i], operandSize);
                        }
                    }
                }
                var instructionSize = Assembler.Output.LogicalPC - PCOnAssemble;
                if (!Assembler.PassNeeded && instructionSize != modeInstruction.instruction.Size)
                    Assembler.Log.LogEntry(line, line.Instruction, $"Mode not supporter for \"{line.Instruction}\" in selected CPU.");
            }
            else
            {
                if (_selectedInstructions.Keys.Any(k => k.Mnem.Equals(line.InstructionName)))
                {
                    if (!Assembler.PassNeeded)
                        Assembler.Log.LogEntry(line, line.Instruction, $"Mode not supported for \"{line.Instruction}\" in selected CPU.");
                }
                else
                {
                    Assembler.Log.LogEntry(line, line.Instruction, $"Mnemonic \"{line.Instruction}\" not supported for selected CPU.");
                }
                return string.Empty;
            }
            if (Assembler.PassNeeded || string.IsNullOrEmpty(Assembler.Options.ListingFile))
                return string.Empty;
            var sb = new StringBuilder();
            if (!Assembler.Options.NoAssembly)
            {
                var byteString = Assembler.Output.GetBytesFrom(PCOnAssemble).ToString(PCOnAssemble, '.');
                sb.Append(byteString.PadRight(Padding));
            }
            else
            {
                sb.Append($".{PCOnAssemble:x4}                        ");
            }

            var disSb = new StringBuilder();
            if (!Assembler.Options.NoDisassembly)
            {
                if (sb.Length > 29)
                    disSb.Append(' ');
                disSb.Append(line.Instruction);
                if (modeInstruction.mode != Modes.Implied)
                {
                    disSb.Append(' ');
                    var memoryMode = modeInstruction.mode & Modes.MemModMask;
                    var size = modeInstruction.mode & Modes.SizeMask;

                    if (memoryMode.HasFlag(Modes.RelativeBit))
                        size |= Modes.Absolute;
                    int eval2 = 0, eval3 = 0;
                    int eval1;
                    if (size == Modes.Long)
                    {
                        eval1 = (int)s_evaled[0] & 0xFFFFFF;
                    }
                    else if (size >= Modes.Absolute)
                    {
                        if (memoryMode.HasFlag(Modes.RelativeBit))
                        {
                            if (Reserved.IsOneOf("Rockwell", instruction))
                            {
                                eval1 = (int)s_evaled[0] & 0xFF;
                                eval2 = Assembler.Output.LogicalPC + (int)s_evaled[1];
                                eval2 &= 0xFFFF;
                            }
                            else
                            {
                                eval1 = Assembler.Output.LogicalPC + (int)s_evaled[0];
                                eval1 &= 0xFFFF;
                            }
                        }
                        else
                        {
                                eval1 = (int)s_evaled[0] & 0xFFFF;
                            if (!double.IsNaN(s_evaled[1]))
                                eval2 = (int)s_evaled[1] & 0xFFFF;
                            if (!double.IsNaN(s_evaled[2]))
                                eval3 = (int)s_evaled[2] & 0xFFFF;
                        }
                    }
                    else
                    {
                        eval1 = (int)s_evaled[0] & 0xFF;
                        if (!double.IsNaN(s_evaled[1]))
                            eval2 = (int)s_evaled[1] & 0xFF;
                        if (!double.IsNaN(s_evaled[2]))
                            eval3 = (int)s_evaled[2] & 0xFF;
                    }
                    if (modeInstruction.mode == Modes.Zp0 && Reserved.IsOneOf("Rockwell", line.InstructionName))
                        disSb.Append($"0,{eval1:x2}");
                    else
                        disSb.AppendFormat(s_modeFormats[modeInstruction.mode], eval1, eval2, eval3);
                }
                sb.Append(disSb.ToString().PadRight(18));
            }
            else
            {
                sb.Append("                  ");
            }
            if (!Assembler.Options.NoSource)
                sb.Append(line.UnparsedSource);
            return sb.ToString();
        }

        public override bool Assembles(string s) => IsReserved(s) && !Reserved.IsOneOf("Registers", s);

        #endregion
    }
}