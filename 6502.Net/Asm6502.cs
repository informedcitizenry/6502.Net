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

namespace Asm6502.Net
{
    /// <summary>
    /// A line assembler that will assemble into 6502 instructions.
    /// </summary>
    public sealed partial class Asm6502 : AssemblerBase, ILineAssembler
    {
        #region Members

        IAssemblyController _controller;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs an instance of a 6502 line assembler. This assembler will output valid
        /// 6502 assembly to instructions.
        /// </summary>
        /// <param name="controller">The <see cref="DotNetAsm.IAssemblyController"/> of this assembler.</param>
        public Asm6502(IAssemblyController controller)
        {
            Reserved.DefineType("OneBytes",
                    "brk", "clc", "cld", "cli", "clv", "dex", "dey", "inx",
                    "iny", "jam", "nop", "pha", "phb", "phd", "phk", "php",
                    "phx", "phy", "pla", "plb", "pld", "plp", "plx", "ply",
                    "rts", "rti", "rtl", "sec", "sed", "sei", "stp", "tax",
                    "tay", "tcd", "tcs", "tdc", "tsc", "tsx", "txa", "txs",
                    "txy", "tya", "tyx", "wai", "wdm", "xba", "xce"
                );

            Reserved.DefineType("Branches",
                    "bcc", "bcs", "beq", "bmi", "bne", "bpl", "bra", "bvc", 
                    "bvs", "bra"
                );

            Reserved.DefineType("Branches16",
                    "brl", "per"
                );

            Reserved.DefineType("ImpliedAccumulator",
                    "asl", "lsr", "rol", "ror"
                );

            Reserved.DefineType("Jumps",
                    "jmp", "jsr"
                );

            Reserved.DefineType("JumpsLong",
                    "jml", "jsl"
                );

            Reserved.DefineType("ImpliedAC02",
                    "dec", "inc"
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
                    "adc", "anc", "and", "ane", "arr", "asl", "asr", "bit",
                    "cmp", "cop", "cpx", "cpy", "dcp", "dop", "eor", "isb",
                    "jml", "jmp", "jsl", "jsr", "las", "lax", "lda", "ldx",
                    "ldy", "lsr", "ora", "pea", "pei", "rep", "rla", "rol",
                    "ror", "rra", "sbc", "sax", "sep", "shx", "shy", "slo",
                    "sre", "sha", "sta", "stx", "sty", "stz", "tas", "top",
                    "trb", "tsb"
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

            _filteredOpcodes = _opcodes.Where(o => o.Value.CPU.Equals("6502")).ToDictionary(k => k.Key, k => k.Value, Assembler.Options.StringComparar);

            _cpu = "6502";
        }

        #endregion

        #region Methods

        void SetCpu(CpuChangedEventArgs args)
        {
            if (args.Line.Operand.EnclosedInQuotes() == false &&
                !args.Line.SourceString.Equals(ConstStrings.COMMANDLINE_ARG))
            {
                Assembler.Log.LogEntry(args.Line, ErrorStrings.QuoteStringNotEnclosed);
                return;
            }
            var cpu = args.Line.Operand.Trim('"');
            if (!cpu.Equals("6502") && !cpu.Equals("65C02") && !cpu.Equals("65816") && !cpu.Equals("6502i"))
            {
                var error = string.Format("Invalid CPU '{0}' specified", cpu);
                if (args.Line.SourceString.Equals(ConstStrings.COMMANDLINE_ARG))
                    throw new Exception(string.Format(error));
                
                Assembler.Log.LogEntry(args.Line, error);
                return;
            }
            _cpu = cpu;

            switch (_cpu)
            {
                case "65816":
                    _filteredOpcodes = _opcodes.Where(o => o.Value.CPU.Equals("6502") || o.Value.CPU.Equals("65C02") || o.Value.CPU.Equals("65816")).ToDictionary(k => k.Key, k => k.Value, Assembler.Options.StringComparar);
                    _filteredOpcodes.Add("stp", new Instruction { CPU = "65816", Size = 1, Opcode = 0xdb });
                    break;
                case "65C02":
                    _filteredOpcodes = _opcodes.Where(o => o.Value.CPU.Equals("6502") || o.Value.CPU.Equals("65C02")).ToDictionary(k => k.Key, k => k.Value, Assembler.Options.StringComparar);
                    break;
                case "6502i":
                    _filteredOpcodes = _opcodes.Where(o => o.Value.CPU.Equals("6502") || o.Value.CPU.Equals("6502i")).ToDictionary(k => k.Key, k => k.Value, Assembler.Options.StringComparar);
                    break;
                case "6502":
                    _filteredOpcodes = _opcodes.Where(o => o.Value.CPU.Equals("6502")).ToDictionary(k => k.Key, k => k.Value, Assembler.Options.StringComparar);
                    break;
                default:
                    break;

            }

            if (_m16)
                SetImmediateA(3);
            if (_x16)
                SetImmediateXY(3);

        }

        void SetImmediateA(int size)
        {
            if (size == 3 && _cpu.StartsWith("6502", StringComparison.Ordinal))
            {
                return;
            }
            string fmt = size == 3 ? " #${0:x4}" : " #${0:x2}";
            string prv = size == 3 ? " #${0:x2}" : " #${0:x4}";

            _filteredOpcodes.Remove("ora" + prv);
            _filteredOpcodes.Remove("and" + prv);
            _filteredOpcodes.Remove("eor" + prv);
            _filteredOpcodes.Remove("adc" + prv);
            _filteredOpcodes.Remove("bit" + prv);
            _filteredOpcodes.Remove("lda" + prv);
            _filteredOpcodes.Remove("cmb" + prv);
            _filteredOpcodes.Remove("sbc" + prv);

            _filteredOpcodes["ora" + fmt] = new Instruction { CPU = "6502",  Size = size, Opcode = 0x09 };
            _filteredOpcodes["and" + fmt] = new Instruction { CPU = "6502",  Size = size, Opcode = 0x29 };
            _filteredOpcodes["eor" + fmt] = new Instruction { CPU = "6502",  Size = size, Opcode = 0x49 };
            _filteredOpcodes["adc" + fmt] = new Instruction { CPU = "6502",  Size = size, Opcode = 0x69 };
            _filteredOpcodes["bit" + fmt] = new Instruction { CPU = "65C02", Size = size, Opcode = 0x89 };
            _filteredOpcodes["lda" + fmt] = new Instruction { CPU = "6502",  Size = size, Opcode = 0xa9 };
            _filteredOpcodes["cmp" + fmt] = new Instruction { CPU = "6502",  Size = size, Opcode = 0xc9 };
            _filteredOpcodes["sbc" + fmt] = new Instruction { CPU = "6502",  Size = size, Opcode = 0xe9 };
        }

        void SetImmediateXY(int size)
        {
            if (size == 3 && _cpu.StartsWith("6502", StringComparison.Ordinal))
            {
                return;
            }
            string fmt = size == 3 ? " #${0:x4}" : " #${0:x2}";
            string prv = size == 3 ? " #${0:x2}" : " #${0:x4}";

            _filteredOpcodes.Remove("ldy" + prv);
            _filteredOpcodes.Remove("ldx" + prv);
            _filteredOpcodes.Remove("cpy" + prv);
            _filteredOpcodes.Remove("cpx" + prv);

            _filteredOpcodes["ldy" + fmt] = new Instruction { CPU = "6502", Size = size, Opcode = 0xa0 };
            _filteredOpcodes["ldx" + fmt] = new Instruction { CPU = "6502", Size = size, Opcode = 0xa2 };
            _filteredOpcodes["cpy" + fmt] = new Instruction { CPU = "6502", Size = size, Opcode = 0xc0 };
            _filteredOpcodes["cpx" + fmt] = new Instruction { CPU = "6502", Size = size, Opcode = 0xe0 };
        }

        void SetRegLongShort(string instruction)
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

        void AssembleRta(SourceLine line)
        {
            var csv = line.Operand.CommaSeparate();

            foreach (string rta in csv)
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


        long ParseSubExpression(string subexpression, char open, char closure, StringBuilder formatBuilder, bool isAbsolute)
        {
            var commasep = subexpression.Substring(1, subexpression.Length - 2).CommaSeparate(open, closure);
            if (commasep.Count() > 2)
                throw new Exception(ErrorStrings.None);

            string expression = commasep.First().Trim();

            var val = Assembler.Evaluator.Eval(expression);
            var expSize = isAbsolute ? 2 : 1;
            if (val.Size() > expSize)
                throw new OverflowException(val.ToString());


            formatBuilder.Append(open);
            formatBuilder.Append("${0:x" + expSize * 2 + "}");

            if (commasep.Count() > 1)
                formatBuilder.AppendFormat(",{0}{1}", commasep.Last(), closure);
            else
                formatBuilder.Append(closure);
            return val;
        }


        Tuple<OperandFormat, Instruction> GetFormatAndOpcode(SourceLine line)
        {

            var instruction = line.Instruction.ToLower();
            string expression1 = string.Empty, expression2 = string.Empty;
            long eval1 = long.MinValue, eval2 = long.MinValue;
            StringBuilder formatBuilder = new StringBuilder();

            formatBuilder.AppendFormat("{0}", instruction);

            bool impliedA = line.Operand.Equals("a", Assembler.Options.StringComparison);

            if (impliedA &&
                !(Reserved.IsOneOf("ImpliedAccumulator", instruction) ||
                  (Reserved.IsOneOf("ImpliedAC02", instruction) && !_cpu.Equals("6502"))
                 )
                )
                throw new Exception();

            string finalFormat;
            Instruction opcode;

            if (string.IsNullOrEmpty(line.Operand) || impliedA)
            {
                finalFormat = formatBuilder.ToString();
                if (!_filteredOpcodes.TryGetValue(finalFormat, out opcode))
                {
                    return new Tuple<OperandFormat, Instruction>(null, null);
                }
            }
            else
            {
                formatBuilder.Append(' ');
                int expSize = 1;

                string operand = line.Operand;

                if (operand[0] == '[')
                {
                    // differentiate between 'and [16]' and 'and [16] 16'
                    var firstBracket = operand.FirstParenEnclosure(useBracket: true);
                    var firstBracketLength = firstBracket.Length;
                    if (operand.Length > firstBracketLength)
                    {
                        var delim = operand.Substring(firstBracketLength).First(c => !char.IsWhiteSpace(c));
                        if (delim != ',')
                        {
                            if (!char.IsWhiteSpace(operand[firstBracketLength]))
                                throw new Exception(ErrorStrings.None);
                            expSize = Convert.ToInt32(Assembler.Evaluator.Eval(firstBracket.Substring(1, firstBracketLength - 2)));
                            if (expSize != 16 && expSize != 24)
                                throw new Exception(ErrorStrings.None);
                            expSize /= 8;
                            operand = operand.Substring(firstBracketLength + 1);
                        }
                    }
                }
                if (operand[0] == '#')
                {
                    if (operand.Length < 2 || char.IsWhiteSpace(operand[1]))
                        throw new ExpressionException(operand);
                    expression1 = operand.Substring(1);
                    eval1 = Assembler.Evaluator.Eval(expression1);
                    expSize = eval1.Size();
                    formatBuilder.Append("#${0:x" + expSize * 2 + "}");
                }
                else
                {
                    IEnumerable<string> commasep;
                    if (line.Operand[0] == '[')
                        commasep = operand.CommaSeparate('[', ']');
                    else
                        commasep = operand.CommaSeparate();
                    if (commasep.Count() > 2)
                        throw new Exception(ErrorStrings.None);


                    string outerexpression = string.Empty;
                    if (commasep.Count() > 1)
                    {
                        if (Reserved.IsOneOf("MoveMemory", instruction))
                        {
                            outerexpression = "${1:x2}";
                            expression2 = commasep.Last();
                            eval2 = Assembler.Evaluator.Eval(expression2.Trim());
                        }
                        else
                        {
                            outerexpression = commasep.Last();

                        }
                    }

                    var param1 = commasep.First();
                    if (param1[0] == '(' && param1.Last() == ')' && 
                        param1.FirstParenEnclosure().Equals(param1) && 
                        (commasep.Count() == 1 || commasep.Last().Equals("y", Assembler.Options.StringComparison)))
                    {
                        eval1 = ParseSubExpression(param1, '(', ')', formatBuilder, instruction.Equals("jmp"));
                        expression1 = eval1.ToString();
                    }
                    else if (param1[0] == '[')
                    {

                        eval1 = ParseSubExpression(param1, '[', ']', formatBuilder, instruction.Equals("jmp"));
                        expression1 = eval1.ToString();
                    }
                    else
                    {
                        expression1 = param1.TrimEnd();
                        eval1 = Assembler.Evaluator.Eval(expression1);
                        if (expSize == 1)
                        {
                            if (Reserved.IsOneOf("Branches16", instruction))

                                expSize++; // we have to check this too in case the user does a brl $10ffff
                            else
                                expSize = eval1.Size();
                        }
                        formatBuilder.Append("${0:x" + expSize * 2 + "}");
                    }

                    if (commasep.Count() > 1)
                        formatBuilder.AppendFormat(",{0}", outerexpression);
                }
                finalFormat = formatBuilder.ToString();

                while (!_filteredOpcodes.TryGetValue(finalFormat, out opcode))
                {
                    // some instructions the size is bigger than the expression comes out to, so
                    // make the expression size larger
                    if (expSize > 3)
                        return new Tuple<OperandFormat, Instruction>(null, null); // we didn't find it
                    finalFormat = finalFormat.Replace("x" + (expSize++) * 2, "x" + expSize * 2);
                }

            }
            var fmt = new OperandFormat
            {
                FormatString = finalFormat,
                Eval1 = eval1,
                Eval2 = eval2
            };
            return new Tuple<OperandFormat, Instruction>(fmt, opcode);
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
            if (Reserved.IsOneOf("ReturnAddress", line.Instruction))
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
                        Assembler.Log.LogEntry(line, 
                            "The current CPU supports only 8-bit immediate mode instructions. The directive '" + line.Instruction + "' will not affect assembly", 
                            Assembler.Options.WarningsAsErrors);
                    else
                        SetRegLongShort(line.Instruction);
                }
                return;
            }

            var formatOpcode = GetFormatAndOpcode(line);
            if (formatOpcode.Item1 == null)
            {
                Assembler.Log.LogEntry(line, ErrorStrings.BadExpression, line.Operand);
                return;
            }
            if (formatOpcode.Item2 == null)
            {
                Assembler.Log.LogEntry(line, ErrorStrings.UnknownInstruction, line.Instruction);
                return;
            }
            long eval1 = formatOpcode.Item1.Eval1, eval2 = formatOpcode.Item1.Eval2;

            // how the (first) evaluated expression will display in disassembly
            long eval1DisplayValue = 0;

            if (Reserved.IsOneOf("Branches", line.Instruction) || Reserved.IsOneOf("Branches16", line.Instruction))
            {
                eval1DisplayValue = eval1 & 0xFFFF;
                try
                {
                    if (Reserved.IsOneOf("Branches", line.Instruction))
                        eval1 = Convert.ToSByte(Assembler.Output.GetRelativeOffset((ushort)eval1DisplayValue, Assembler.Output.LogicalPC + 2));
                    else
                        eval1 = Convert.ToInt16(Assembler.Output.GetRelativeOffset((ushort)eval1DisplayValue, Assembler.Output.LogicalPC + 3));
                }
                catch
                {
                    throw new OverflowException(eval1.ToString());
                }
            }
            else
            {
                var operandsize = 0;

                if (eval1 != long.MinValue)
                {
                    if (formatOpcode.Item2.Size == 4)
                    {
                        eval1 &= 0xFFFFFF;
                    }
                    else if (formatOpcode.Item2.Size == 3 && eval2 == long.MinValue)
                    {
                        eval1 &= 0xFFFF;
                    }
                    else
                    {
                        eval1 &= 0xFF;
                        if (eval2 != long.MinValue)
                            eval2 &= 0xFF;
                    }
                    eval1DisplayValue = eval1; 
                    operandsize = eval1.Size();
                }
                if (eval2 != long.MinValue)
                    operandsize += eval2.Size();

                if (operandsize >= formatOpcode.Item2.Size)
                {
                    throw new OverflowException(line.Operand);
                }
            }
            long operbytes = 0;
            if (eval1 != long.MinValue)
                operbytes = eval2 == long.MinValue ? (eval1 << 8) : (((eval1 << 8) | eval2) << 8);
            
            line.Disassembly = string.Format(formatOpcode.Item1.FormatString, eval1DisplayValue, eval2);
            line.Assembly = Assembler.Output.Add(Convert.ToInt32(operbytes) | formatOpcode.Item2.Opcode, 
                                                  formatOpcode.Item2.Size);
        }

        public int GetInstructionSize(SourceLine line)
        {
            if (Reserved.IsOneOf("LongShort", line.Instruction))
                return 0;
                
            if (string.IsNullOrEmpty(line.Operand))
                return 1;

            if (line.Operand[0] == '#')
                return 2;

            if (Reserved.IsOneOf("ReturnAddress", line.Instruction))
                return 2 * line.Operand.CommaSeparate().Count;

            if (Reserved.IsOneOf("Branches", line.Instruction))
                return 2;

            if (Reserved.IsOneOf("Branches16", line.Instruction))
                return 3;

            if (Reserved.IsOneOf("Jumps", line.Instruction))
                return _cpu.Equals("65816") ? 4 : 3;

            if (Reserved.IsOneOf("JumpsLong", line.Instruction))
                return 4;

            if (Reserved.IsOneOf("MoveMemory", line.Instruction))
                return 3;

            // not perfect, buta gain we are just getting most approximate...
            if (line.Operand[0] == '(' && 
                (line.Operand.EndsWith("),y", Assembler.Options.StringComparison) ||
                line.Operand.EndsWith(",x)", Assembler.Options.StringComparison)))
                return 2;

            try
            {
                // oh well, now we have to try to parse
                var formatOpcode = GetFormatAndOpcode(line);
                if (formatOpcode.Item2 != null)
                    return formatOpcode.Item2.Size;
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