//-----------------------------------------------------------------------------
// Copyright (c) 2017-2019 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using DotNetAsm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpcodeTable = System.Collections.Generic.Dictionary<string, DotNetAsm.Instruction>;

namespace Asm6502.Net
{
    /// <summary>
    /// A line assembler that will assemble into 6502 instructions.
    /// </summary>
    public sealed partial class Asm6502 : AssemblerBase, ILineAssembler
    {
        #region Constructors

        /// <summary>
        /// Constructs an instance of a 6502 line assembler. This assembler will output valid
        /// 6502 assembly to instructions.
        /// </summary>
        /// <param name="controller">The <see cref="DotNetAsm.IAssemblyController"/> of this assembler.</param>
        public Asm6502(IAssemblyController controller)
        {
            Reserved.DefineType("OneBytes",
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
                    "bvs", "bra"
                );

            Reserved.DefineType("Branches16",
                    "brl", "per", "blt", "bge"
                );

            Reserved.DefineType("Rockwell",
                    "bbr", "bbs", "rmb", "smb"
                );

            Reserved.DefineType("RockwellBranches",
                    "bbr", "bbs"
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

            Reserved.DefineType("ReturnAddress",
                    ".rta"
                );

            Reserved.DefineType("LongShort",
                    ".m16", ".m8", ".x16", ".x8", ".mx16", ".mx8"
                );

            Reserved.DefineType("Mnemonics",
                    "adc", "anc", "and", "ane", "arr", "asl", "asr", "asw",
                    "bit", "bsr", "cmp", "cop", "cpx", "cpy", "cpz", "dcp",
                    "dec", "dew", "dop", "eor", "inc", "inw", "isb", "jml",
                    "jmp", "jsl", "jsr", "las", "lax", "lda", "ldx", "ldy",
                    "ldz", "lsr", "neg", "ora", "pea", "pei", "phw", "rep",
                    "rla", "rol", "ror", "row", "rra", "sax", "sbc", "sep",
                    "sha", "shx", "shy", "slo", "sre", "st1", "st2", "sta",
                    "stx", "sty", "stz", "tam", "tas", "tma", "top", "trb",
                    "tsb"
                );

            _controller = controller;

            _controller.AddSymbol("a");
            _controller.CpuChanged += SetCpu;

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

            ConstructOpcodeTable();
            _filteredOpcodes = new OpcodeTable(_opcodes6502, Assembler.Options.StringComparar);

            _cpu = "6502";
        }

        #endregion

        #region Methods

        public void SetCpu(object sender, CpuChangedEventArgs args)
        {
            if (args.Line.Operand.EnclosedInQuotes(out string cpu) == false &&
                !args.Line.SourceString.Equals(ConstStrings.COMMANDLINE_ARG))
            {
                Assembler.Log.LogEntry(args.Line, ErrorStrings.QuoteStringNotEnclosed);
                return;
            }
            if (!SupportedCPUs.Contains(cpu))
            {
                var error = string.Format($"Invalid CPU '{cpu}' specified");
                if (args.Line.SourceString.Equals(ConstStrings.COMMANDLINE_ARG))
                    throw new Exception(error);

                Assembler.Log.LogEntry(args.Line, error);
                return;
            }
            _cpu = cpu;

            switch (_cpu)
            {
                case "65816":
                    _filteredOpcodes = _opcodes6502.Concat(_opcodes65C02)
                                                   .Concat(_opcodes65816)
                                                   .ToDictionary(k => k.Key, k => k.Value, Assembler.Options.StringComparar);
                    break;
                case "HuC6280":
                    _filteredOpcodes = _opcodes6502.Concat(_opcodes65C02.Where(o => (o.Value.Opcode & 0x0f) != 0x02))
                                                   .Concat(_opcodesR65C02)
                                                   .Concat(_opcodesHuC6280)
                                                   .ToDictionary(k => k.Key, k => k.Value, Assembler.Options.StringComparar);
                    break;
                case "65CE02":
                    _filteredOpcodes = _opcodes6502.Where(o => (o.Value.Opcode & 0x1f) != 0x10) // exclude 6502 branch instructions
                                                   .Concat(_opcodes65C02.Where(o => o.Value.Opcode != 0x80 && (o.Value.Opcode & 0x0f) != 0x02))
                                                   .Concat(_opcodesR65C02)
                                                   .Concat(_opcodes65CE02)
                                                   .ToDictionary(k => k.Key, k => k.Value, Assembler.Options.StringComparar);
                    break;
                case "R65C02":
                    _filteredOpcodes = _opcodes6502.Concat(_opcodes65C02)
                                                   .Concat(_opcodesR65C02)
                                                   .ToDictionary(k => k.Key, k => k.Value, Assembler.Options.StringComparar);
                    break;
                case "65C02":
                    _filteredOpcodes = _opcodes6502.Concat(_opcodes65C02)
                                                   .ToDictionary(k => k.Key, k => k.Value, Assembler.Options.StringComparar);
                    break;
                case "6502i":
                    _filteredOpcodes = _opcodes6502.Concat(_opcodes6502i)
                                                   .ToDictionary(k => k.Key, k => k.Value, Assembler.Options.StringComparar);
                    break;
                default:
                    _filteredOpcodes = new OpcodeTable(_opcodes6502, Assembler.Options.StringComparar);
                    break;

            }
            if (_m16)
                SetImmediateA(3);
            if (_x16)
                SetImmediateXY(3);

        }

        private void SetImmediateA(int size)
        {
            if (size == 3 && !_cpu.Equals("65816"))
                return;

            var fmt = size == 3 ? " #${0:x4}" : " #${0:x2}";
            var prv = size == 3 ? " #${0:x2}" : " #${0:x4}";

            _filteredOpcodes.Remove("ora" + prv);
            _filteredOpcodes.Remove("and" + prv);
            _filteredOpcodes.Remove("eor" + prv);
            _filteredOpcodes.Remove("adc" + prv);
            _filteredOpcodes.Remove("bit" + prv);
            _filteredOpcodes.Remove("lda" + prv);
            _filteredOpcodes.Remove("cmb" + prv);
            _filteredOpcodes.Remove("sbc" + prv);

            _filteredOpcodes["ora" + fmt] = new Instruction { CPU =  "6502", Size = size, Opcode = 0x09 };
            _filteredOpcodes["and" + fmt] = new Instruction { CPU =  "6502", Size = size, Opcode = 0x29 };
            _filteredOpcodes["eor" + fmt] = new Instruction { CPU =  "6502", Size = size, Opcode = 0x49 };
            _filteredOpcodes["adc" + fmt] = new Instruction { CPU =  "6502", Size = size, Opcode = 0x69 };
            _filteredOpcodes["bit" + fmt] = new Instruction { CPU = "65C02", Size = size, Opcode = 0x89 };
            _filteredOpcodes["lda" + fmt] = new Instruction { CPU =  "6502", Size = size, Opcode = 0xa9 };
            _filteredOpcodes["cmp" + fmt] = new Instruction { CPU =  "6502", Size = size, Opcode = 0xc9 };
            _filteredOpcodes["sbc" + fmt] = new Instruction { CPU =  "6502", Size = size, Opcode = 0xe9 };
        }

        private void SetImmediateXY(int size)
        {
            if (size == 3 && !_cpu.Equals("65816"))
                return;

            var fmt = size == 3 ? " #${0:x4}" : " #${0:x2}";
            var prv = size == 3 ? " #${0:x2}" : " #${0:x4}";

            _filteredOpcodes.Remove("ldy" + prv);
            _filteredOpcodes.Remove("ldx" + prv);
            _filteredOpcodes.Remove("cpy" + prv);
            _filteredOpcodes.Remove("cpx" + prv);

            _filteredOpcodes["ldy" + fmt] = new Instruction { CPU = "6502", Size = size, Opcode = 0xa0 };
            _filteredOpcodes["ldx" + fmt] = new Instruction { CPU = "6502", Size = size, Opcode = 0xa2 };
            _filteredOpcodes["cpy" + fmt] = new Instruction { CPU = "6502", Size = size, Opcode = 0xc0 };
            _filteredOpcodes["cpx" + fmt] = new Instruction { CPU = "6502", Size = size, Opcode = 0xe0 };
        }

        private void SetRegLongShort(string instruction)
        {
            if (instruction.StartsWith(".x", Assembler.Options.StringComparison))
            {
                var x16 = instruction.Equals(".x16", Assembler.Options.StringComparison);
                if (x16 != _x16)
                {
                    _x16 = x16;
                    SetImmediateXY(_x16 ? 3 : 2);
                }
            }
            else
            {

                var m16 = instruction.EndsWith("16", Assembler.Options.StringComparison);
                if (m16 != _m16)
                {
                    _m16 = m16;
                    SetImmediateA(_m16 ? 3 : 2);
                }
                if (instruction.StartsWith(".mx", Assembler.Options.StringComparison))
                {
                    var x16 = instruction.EndsWith("16", Assembler.Options.StringComparison);
                    if (x16 != _x16)
                    {
                        _x16 = x16;
                        SetImmediateXY(_x16 ? 3 : 2);
                    }
                }
            }
        }

        private void AssembleRta(SourceLine line)
        {
            List<string> csv = line.Operand.CommaSeparate();

            foreach (var rta in csv)
            {
                if (rta.Equals("?"))
                {
                    Assembler.Output.AddUninitialized(2);
                }
                else
                {
                    var val = Assembler.Evaluator.Eval(rta, ushort.MinValue, ushort.MaxValue + 1);
                    line.Assembly.AddRange(Assembler.Output.Add(val - 1, 2));
                }
            }
        }

        private (OperandFormat fmt, Instruction instruction) ParseToInstruction(SourceLine line)
        {
            var mnemonic = line.Instruction.ToLower();
            var operand = line.Operand;
            if (operand.Equals("a", Assembler.Options.StringComparison) &&
                !Assembler.Symbols.IsSymbol("a"))
            {
                operand = string.Empty;
            }

            var fmt = new OperandFormat();
            var formatBuilder = new StringBuilder(mnemonic);
            Instruction instruction;
            if (!string.IsNullOrEmpty(operand))
            {
                byte forcedWidth = 0;
                formatBuilder.Append(' ');
                List<string> csv = operand.CommaSeparate();
                var firstElement = csv.First();
                var firstChar = firstElement[0];
                var isRockwell = Reserved.IsOneOf("Rockwell", mnemonic);
                if (firstChar == '[' || firstChar == '(')
                {
                    var firstParen = firstElement.GetNextParenEnclosure();
                    var firstParenLength = firstParen.Length;
                    if (firstElement[0] == '[' && firstElement.Length > firstParenLength)
                    {
                        // differentiate between 'and [16]' and 'and [16] 16'
                        if (!char.IsWhiteSpace(firstElement[firstParenLength]))
                            throw new Exception(ErrorStrings.None);
                        forcedWidth = Convert.ToByte(Assembler.Evaluator.Eval(firstParen.Substring(1, firstParenLength - 2)));
                        if (forcedWidth == 0 || (forcedWidth & 0b1110_0111) != 0)
                            throw new Exception(string.Format(ErrorStrings.IllegalQuantity, firstElement));
                        forcedWidth /= 8;
                        firstElement = firstElement.Substring(firstParenLength + 1).TrimStart();
                        firstChar = firstElement[0];
                        if (firstChar == '[' || firstChar == '(')
                        {
                            firstParen = firstElement.GetNextParenEnclosure();
                            firstParenLength = firstParen.Length;
                            if (firstElement.Length == firstParenLength)
                                evaluateFirstParen();
                            else
                                addElementToFormat(firstElement);
                        }
                        else
                        {
                            addElementToFormat(firstElement);
                        }
                    }
                    else if (firstElement.Length == firstParenLength)
                    {
                        evaluateFirstParen();
                    }
                    else
                    {
                        addElementToFormat(firstElement);
                    }

                    void evaluateFirstParen()
                    {
                        if (firstParenLength < 3)
                            throw new ExpressionException(operand);
                        firstParen = firstParen.Substring(1, firstParen.Length - 2);
                        List<string> parenCsv = firstParen.CommaSeparate();

                        formatBuilder.Append(firstChar);

                        addElementToFormat(parenCsv.First());

                        var parenLast = parenCsv.Last();
                        if (parenCsv.Count > 1 &&
                            (parenLast.Equals("s", Assembler.Options.StringComparison) ||
                             parenLast.Equals("sp", Assembler.Options.StringComparison) ||
                             parenLast.Equals("x", Assembler.Options.StringComparison))
                            )
                        {
                            // indexed indirect
                            formatBuilder.AppendFormat($",{parenLast}");
                        }
                        formatBuilder.Append(firstElement[firstElement.Length - 1]);
                    }
                }
                else if (isRockwell)
                {
                    var bit = Assembler.Evaluator.Eval(firstElement, 0, 7);
                    formatBuilder.Append(bit);
                }
                else
                {
                    if (firstChar == '#')
                    {
                        if (firstElement.Length < 2 || char.IsWhiteSpace(firstElement[1]))
                            throw new ExpressionException(firstElement);
                        formatBuilder.Append(firstChar);
                        firstElement = firstElement.Substring(1);
                        csv[0] = firstElement;
                    }
                    addElementToFormat(firstElement);
                }
                var csvCount = csv.Count;
                for (var i = 1; i < csvCount; i++)
                {
                    formatBuilder.Append(',');
                    var currElement = csv[i];
                    if (i == csvCount - 1 &&
                        (currElement.Equals("x", Assembler.Options.StringComparison) ||
                         currElement.Equals("y", Assembler.Options.StringComparison) ||
                         currElement.Equals("s", Assembler.Options.StringComparison) ||
                         currElement.Equals("z", Assembler.Options.StringComparison)
                        )
                       )
                    {
                        formatBuilder.Append(currElement);
                    }
                    else
                    {
                        // account for leading bits for Rockwell instructions
                        var index = isRockwell ? i - 1 : i;
                        addElementToFormat(currElement, index);
                    }
                }
                void addElementToFormat(string element, int index = 0)
                {
                    var eval = Assembler.Evaluator.Eval(element);
                    var evalSize = eval.Size();
                    if (forcedWidth > 0 && evalSize > forcedWidth)
                        throw new OverflowException(element);
                    if (forcedWidth > evalSize)
                        evalSize = forcedWidth;
                    formatBuilder.Append($"${{{index}:x{evalSize * 2}}}");
                    fmt.Evaluations.Add(eval);
                    fmt.EvaluationSizes.Add(evalSize);
                }
            }
            var finalFormat = formatBuilder.ToString();
            while (!_filteredOpcodes.TryGetValue(finalFormat, out instruction))
            {
                if (fmt.Evaluations.Count == 0)
                    return (null, null); // not a valid implied mode instruction

                // some instructions the size is bigger than the component expressions may come out to, so
                // make the expression sizes larger
                if (fmt.Evaluations.Count == 3)
                {
                    // Hudson support
                    for (var i = 0; i < 3; i++)
                    {
                        if (fmt.EvaluationSizes[i] == 2)
                            continue;
                        if (fmt.EvaluationSizes[i] > 2)
                            return (null, null);
                        finalFormat = finalFormat.Replace($"{i}:x2", $"{i}:x4");
                        fmt.EvaluationSizes[i]++;
                    }
                }
                else
                {
                    if (fmt.Evaluations.Count > 3)
                        return (null, null); // too many evaluations
                    var newSize = fmt.EvaluationSizes[0] + 1;
                    if (newSize > 3)
                        return (null, null); // we didn't find it
                    finalFormat = finalFormat.Replace($"0:x{fmt.EvaluationSizes[0] * 2}", $"0:x{newSize * 2}");
                    fmt.EvaluationSizes[0] = newSize;
                }
            }
            fmt.FormatString = finalFormat;
            return (fmt, instruction);
        }

        #region ILineAssembler.Methods

        public void AssembleLine(SourceLine line)
        {
            if (Assembler.Output.PCOverflow)
            {
                Assembler.Log.LogEntry(line,
                                        ErrorStrings.PCOverflow,
                                        Assembler.Output.LogicalPC);
                return;
            }
            if (line.Instruction.Equals(".rta", Assembler.Options.StringComparison))
            {
                AssembleRta(line);
                return;
            }
            if (Reserved.IsOneOf("LongShort", line.Instruction))
            {
                if (!string.IsNullOrEmpty(line.Operand))
                {
                    Assembler.Log.LogEntry(line, ErrorStrings.TooManyArguments, line.Instruction);
                }
                else
                {
                    if (_cpu == null || !_cpu.Equals("65816"))
                    {
                        Assembler.Log.LogEntry(line,
                            $"The current CPU supports only 8-bit immediate mode instructions. The directive '{line.Instruction}' will not affect assembly",
                            Assembler.Options.WarningsAsErrors);
                    }
                    else
                        SetRegLongShort(line.Instruction);
                }
                return;
            }
            (OperandFormat fmt, Instruction instruction) = ParseToInstruction(line);
            if (fmt == null)
            {
                if (!_filteredOpcodes.Any(kvp => kvp.Key.StartsWith(line.Instruction, Assembler.Options.StringComparison)))
                    Assembler.Log.LogEntry(line, ErrorStrings.InstructionNotSupported, line.Instruction);
                else
                    throw new Exception(string.Format(ErrorStrings.AddressingModeNotSupported, line.Instruction));//Assembler.Log.LogEntry(line, ErrorStrings.AddressingModeNotSupported, line.Instruction);
                return;
            }
            long opcode = instruction.Opcode;

            var evals = fmt.Evaluations;

            // how the evaluated expressions will display in disassembly
            var evalDisplays = evals.ToList();
            var numEvals = evals.Count;

            var isRockwell = Reserved.IsOneOf("RockwellBranches", line.Instruction);
            if (Reserved.IsOneOf("Branches", line.Instruction) ||
                Reserved.IsOneOf("Branches16", line.Instruction) ||
                isRockwell)
            {
                var displ = Reserved.IsOneOf("RockwellBranches", line.Instruction) ? evals[1] :
                                                                                     evals[0];
                if (displ > 0xFFFF)
                    throw new OverflowException(displ.ToString());

                long rel8 = Assembler.Output.GetRelativeOffset((int)displ, Assembler.Output.LogicalPC + 2);
                if (Reserved.IsOneOf("Branches16", line.Instruction) ||
                        (_cpu.Equals("65CE02") && Reserved.IsOneOf("Branches", line.Instruction)
                            && (rel8 < sbyte.MinValue || rel8 > sbyte.MaxValue)))
                {
                    evalDisplays[0] = displ & 0xFFFF;
                    evals[0] = Convert.ToInt16(Assembler.Output.GetRelativeOffset((int)displ, Assembler.Output.LogicalPC + 3));
                }
                else
                {
                    if (isRockwell)
                    {
                        evals[1] = Convert.ToSByte(rel8);
                        evalDisplays[0] = evals[0] & 0xFF;
                        evalDisplays[1] = displ & 0xFFFF;
                    }
                    else
                    {
                        evalDisplays[0] = displ & 0xFFFF;
                        evals[0] = Convert.ToSByte(rel8);
                        if (_cpu.Equals("65CE02"))
                        {
                            // change 16-bit relative to 8-bit version
                            opcode -= 3;
                        }
                    }
                    fmt.EvaluationSizes[0] = 1;
                }
            }
            else
            {
                if (numEvals > 0)
                {
                    var totalSize = 0;
                    for (var i = 0; i < numEvals; i++)
                    {
                        var operandSize = fmt.EvaluationSizes[i];
                        if (evalDisplays[i] < 0)
                        {
                            // for negative numbers we need to "lop off" the leading binary 1s
                            // to display in the disassembly correctly
                            switch (operandSize)
                            {
                                case 3:
                                    evalDisplays[i] &= 0xFFFFFF;
                                    break;
                                case 2:
                                    evalDisplays[i] &= 0xFFFF;
                                    break;
                                default:
                                    evalDisplays[i] &= 0xFF;
                                    break;
                            }
                        }
                        totalSize += operandSize;
                    }
                    if (totalSize >= instruction.Size)
                        throw new OverflowException(line.Operand);
                }
                if (numEvals > 1 && Reserved.IsOneOf("MoveMemory", line.Instruction))
                    evals.Reverse();
            }
            // create a new list of bytes for each attempted pass
            var instrBytes = new List<byte>(Assembler.Output.Add(opcode, 1));
            for (var i = 0; i < numEvals; i++)
                instrBytes.AddRange(Assembler.Output.Add(evals[i], fmt.EvaluationSizes[i]));

            line.Disassembly = string.Format(fmt.FormatString, evalDisplays.Cast<object>().ToArray());
            line.Assembly = instrBytes;
        }

        public int GetInstructionSize(SourceLine line)
        {
            if (Reserved.IsOneOf("LongShort", line.Instruction))
                return 0;

            if (string.IsNullOrEmpty(line.Operand))
                return 1;

            if (line.Operand[0] == '#' && !line.Instruction.Equals("tst", Assembler.Options.StringComparison))
            {
                if (_m16 && line.Instruction.EndsWith("a", Assembler.Options.StringComparison))
                    return 3;
                if (_x16 && (line.Instruction.EndsWith("x", Assembler.Options.StringComparison) ||
                             line.Instruction.EndsWith("y", Assembler.Options.StringComparison)))
                    return 3;
                return 2;
            }
            if (Reserved.IsOneOf("ReturnAddress", line.Instruction))
                return 2 * line.Operand.CommaSeparate().Count;

            if (Reserved.IsOneOf("Branches", line.Instruction))
            {
                if (_cpu.Equals("65CE02")) return 3;
                return 2;
            }
            if (Reserved.IsOneOf("Rockwell", line.Instruction))
            {
                if (Reserved.IsOneOf("RockwellBranches", line.Instruction))
                    return 3;
                return 2;
            }
            if (Reserved.IsOneOf("Branches16", line.Instruction))
                return 3;

            if (Reserved.IsOneOf("Jumps", line.Instruction))
                return _cpu.Equals("65816") ? 4 : 3;

            if (Reserved.IsOneOf("JumpsLong", line.Instruction))
                return 4;

            if (Reserved.IsOneOf("MoveMemory", line.Instruction))
                return 3;

            if (Reserved.IsOneOf("MoveMemory16", line.Instruction))
                return 7;

            // not perfect, but again we are just getting most approximate...
            if (line.Operand[0] == '(' &&
                (line.Operand.EndsWith("),y", Assembler.Options.StringComparison) ||
                line.Operand.EndsWith(",x)", Assembler.Options.StringComparison) ||
                line.Operand.EndsWith("),z", Assembler.Options.StringComparison)))
                return 2;

            try
            {
                // oh well, now we have to try to parse
                (OperandFormat fmt, Instruction instruction) formatOpcode = ParseToInstruction(line);
                if (formatOpcode.instruction != null)
                    return formatOpcode.instruction.Size;
                return 0;
            }
            catch
            {
                return 3;
            }
        }

        public bool AssemblesInstruction(string instruction)
                    => Reserved.IsReserved(instruction);

        #endregion

        #endregion
    }
}