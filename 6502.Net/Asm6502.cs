//-----------------------------------------------------------------------------
// Copyright (c) 2017 informedcitizenry <informedcitizenry@gmail.com>
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to 
// deal in the Software without restriction, including without limitation the 
// rights to use, copy, modify, merge, publish, distribute, sublicense, and/or 
// sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in 
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS 
// IN THE SOFTWARE.
//-----------------------------------------------------------------------------

using DotNetAsm;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Asm6502.Net
{
    /// <summary>
    /// A line assembler that will assemble into 6502 instructions.
    /// </summary>
    public partial class Asm6502 : AssemblerBase, ILineAssembler
    {
        #region Constructors

        /// <summary>
        /// Constructs an instance of a 6502 line assembler. This assembler will output valid
        /// 6502 assembly to instructions.
        /// </summary>
        /// <param name="controller">The assembly controller.</param>
        public Asm6502(IAssemblyController controller) :
            base(controller)
        {
            Reserved.DefineType("Accumulator", 
                    "adc", "and", "cmp", "eor", "lda", "ora", "sbc", "sta"
                );

            Reserved.DefineType("Branches", 
                    "bcc","bcs","beq","bmi","bne","bpl","bra","bvc","bvs",
                    "bra"
                );

            Reserved.DefineType("Branches16",
                    "brl", "per"
                );

            Reserved.DefineType("Implied", 
                    "brk","clc","cld","cli","clv","dex","dey","inx","iny","nop","pha","php","pla",
                    "plp","rti","rts","sec","sed","sei","tax","tay","tsx","txa","txs","tya",
                    "tcs", "tsc", "tcd", "tdc", "txy", "tyx", "wai", "stp", "xba", "xce",
                    "phd", "pld", "phk", "phy", "rtl", "ply", "phb", "plb", "phx", "plx"
                );

            Reserved.DefineType("ImpliedAccumulator", 
                    "asl", "lsr", "rol", "ror",
                    "inc", "dec"
                );

            Reserved.DefineType("ImpliedAC02",
                    "inc", "dec"
                );

            Reserved.DefineType("Jumps", 
                    "jmp", "jsr",
                    "jml", "jsl"
                );
            
            Reserved.DefineType("Mnemonics",
                    "asl", "bit", "cpx", "cpy", "dec", "inc", "ldx",
                    "ldy", "lsr", "rol", "ror", "stx", "sty", "cop", 
                    "tsb", "trb", "mvn", "mvp", "stz", "pea", "pei", 
                    "rep", "sep", "anc", "ane", "arr", "asr", "sha",
                    "dcp", "dop", "isb", "jam", "las", "lax", "rla", 
                    "rra", "sax", "shy", "slo", "sre", "tas", "top", 
                    "shx"
                );

            Reserved.DefineType("ReturnAddress", 
                    ".rta"
                );

            Reserved.DefineType("LongShort",
                    ".m16", ".m8", ".x16", ".x8", ".mx16", ".mx8"
                );

            _regWidth = new Regex(@"^\[\s*(16|24)\s*\]\s*", RegexOptions.Compiled);

            _builders = new FormatBuilder[]
            {
                new FormatBuilder(@"^\[\s*([^\]\s]+)\s*\]\s*,\s*y$()", "[{2}],y", "${0:x2}", string.Empty, 2, 2, 1, 2, controller.Options.RegexOption),
                new FormatBuilder(@"^\[\s*([^\]\s]+)\s*\]$()", "[{2}]", "${0:x2}", string.Empty, 2, 2, 1, 2, Controller.Options.RegexOption),
                new FormatBuilder(@"^#([^\s].*)$()", "#{2}", "${0:x2}", string.Empty, 2, 2, 1, 2, Controller.Options.RegexOption),
                new FormatBuilder(@"^\(\s*([^\s]+)\s*,\s*s\s*\)\s*,\s*y$()", "({2},s),y", "${0:x2}", string.Empty, 2, 2, 1, 2, Controller.Options.RegexOption),
                new FormatBuilder(@"^\(\s*([^\s]+)\s*,\s*(s|x)\s*\)$()", "({2},{0})", "${0:x2}", string.Empty, 2, 3, 1, 3, Controller.Options.RegexOption ),
                new FormatBuilder(@"^([^\s]+)\s*,\s*(s|x)$()", "{2},{0}", "${0:x2}", string.Empty, 2, 3, 1, 3, Controller.Options.RegexOption),
                new FormatBuilder(@"^([^\s]+)\s*,\s*y$()", "{2},y", "${0:x2}", string.Empty, 2, 2, 1, 2, Controller.Options.RegexOption, true ),
                new FormatBuilder(@"^([^\s]+)\s*,\s*(?!(x|y)$)([^\s]+)$()", "{2},{3}", "${0:x2}", "${1:x2}", 4, 4, 1, 3, Controller.Options.RegexOption),
                new FormatBuilder(@"^(.+)$()", "{2}", "${0:x2}", string.Empty, 2, 2, 1, 2, Controller.Options.RegexOption, true )
            };

            Controller.AddSymbol("a");
            Controller.CpuChanged += SetCpu;

            // set architecture specific encodings
            Controller.Encoding.SelectEncoding("petscii");
            Controller.Encoding.Map("az", 'A');
            Controller.Encoding.Map("AZ", 0xc1);
            Controller.Encoding.Map('£', '\\');
            Controller.Encoding.Map('↑', '^');
            Controller.Encoding.Map('←', '_');
            Controller.Encoding.Map('▌', 0xa1);
            Controller.Encoding.Map('▄', 0xa2);
            Controller.Encoding.Map('▔', 0xa3);
            Controller.Encoding.Map('▁', 0xa4);
            Controller.Encoding.Map('▏', 0xa5);
            Controller.Encoding.Map('▒', 0xa6);
            Controller.Encoding.Map('▕', 0xa7);
            Controller.Encoding.Map('◤', 0xa9);
            Controller.Encoding.Map('├', 0xab);
            Controller.Encoding.Map('└', 0xad);
            Controller.Encoding.Map('┐', 0xae);
            Controller.Encoding.Map('▂', 0xaf);
            Controller.Encoding.Map('┌', 0xb0);
            Controller.Encoding.Map('┴', 0xb1);
            Controller.Encoding.Map('┬', 0xb2);
            Controller.Encoding.Map('┤', 0xb3);
            Controller.Encoding.Map('▎', 0xb4);
            Controller.Encoding.Map('▍', 0xb5);
            Controller.Encoding.Map('▃', 0xb9);
            Controller.Encoding.Map('✓', 0xba);
            Controller.Encoding.Map('┘', 0xbd);
            Controller.Encoding.Map('━', 0xc0);
            Controller.Encoding.Map('♠', 0xc1);
            Controller.Encoding.Map('│', 0xc2);
            Controller.Encoding.Map('╮', 0xc9);
            Controller.Encoding.Map('╰', 0xca);
            Controller.Encoding.Map('╯', 0xcb);
            Controller.Encoding.Map('╲', 0xcd);
            Controller.Encoding.Map('╱', 0xce);
            Controller.Encoding.Map('●', 0xd1);
            Controller.Encoding.Map('♥', 0xd3);
            Controller.Encoding.Map('╭', 0xd5);
            Controller.Encoding.Map('╳', 0xd6);
            Controller.Encoding.Map('○', 0xd7);
            Controller.Encoding.Map('♣', 0xd8);
            Controller.Encoding.Map('♦', 0xda);
            Controller.Encoding.Map('┼', 0xdb);
            Controller.Encoding.Map('π', 0xde);
            Controller.Encoding.Map('◥', 0xdf);

            Controller.Encoding.SelectEncoding("cbmscreen");
            Controller.Encoding.Map("@Z", '\0');
            Controller.Encoding.Map("az", 'A');
            Controller.Encoding.Map('£', '\\');
            Controller.Encoding.Map('π', '^'); // π is $5e in unshifted
            Controller.Encoding.Map('↑', '^'); // ↑ is $5e in shifted
            Controller.Encoding.Map('←', '_');
            Controller.Encoding.Map('▌', '`');
            Controller.Encoding.Map('▄', 'a');
            Controller.Encoding.Map('▔', 'b');
            Controller.Encoding.Map('▁', 'c');
            Controller.Encoding.Map('▏', 'd');
            Controller.Encoding.Map('▒', 'e');
            Controller.Encoding.Map('▕', 'f');
            Controller.Encoding.Map('◤', 'i');
            Controller.Encoding.Map('├', 'k');
            Controller.Encoding.Map('└', 'm');
            Controller.Encoding.Map('┐', 'n');
            Controller.Encoding.Map('▂', 'o');
            Controller.Encoding.Map('┌', 'p');
            Controller.Encoding.Map('┴', 'q');
            Controller.Encoding.Map('┬', 'r');
            Controller.Encoding.Map('┤', 's');
            Controller.Encoding.Map('▎', 't');
            Controller.Encoding.Map('▍', 'u');
            Controller.Encoding.Map('▃', 'y');
            Controller.Encoding.Map('✓', 'z');
            Controller.Encoding.Map('┘', '}');
            Controller.Encoding.Map('━', '@');
            Controller.Encoding.Map('♠', 'A');
            Controller.Encoding.Map('│', 'B');
            Controller.Encoding.Map('╮', 'I');
            Controller.Encoding.Map('╰', 'J');
            Controller.Encoding.Map('╯', 'K');
            Controller.Encoding.Map('╲', 'M');
            Controller.Encoding.Map('╱', 'N');
            Controller.Encoding.Map('●', 'Q');
            Controller.Encoding.Map('♥', 'S');
            Controller.Encoding.Map('╭', 'U');
            Controller.Encoding.Map('╳', 'V');
            Controller.Encoding.Map('○', 'W');
            Controller.Encoding.Map('♣', 'X');
            Controller.Encoding.Map('♦', 'Z');
            Controller.Encoding.Map('┼', '[');
            Controller.Encoding.Map('◥', '_');
            
            Controller.Encoding.SelectEncoding("atascreen");
            Controller.Encoding.Map(" _", '\0');

            Controller.Encoding.SelectDefaultEncoding();

            _filteredOpcodes = _opcodes.Where(o => o.CPU.Equals("6502")).ToList();
        }

        #endregion

        #region Methods

        Tuple<OperandFormat, Opcode> GetFormatAndOpcode(SourceLine line)
        {
            bool force16 = false, force24 = false;
            OperandFormat fmt = null;
            Opcode opc = null;

            string instruction = line.Instruction.ToLower();

            string operand = line.Operand;
            if (Reserved.IsOneOf("ImpliedAccumulator", instruction) || 
                (Reserved.IsOneOf("ImpliedAC02", instruction) && !_cpu.Equals("6502"))
                && (!Controller.Variables.IsSymbol("a") && !Controller.Labels.IsSymbol("a")))
                    operand = Regex.Replace(operand, @"^a$", string.Empty, Controller.Options.RegexOption);

            operand = ConvertFuncs(operand);

            if (string.IsNullOrEmpty(operand))
            {
                fmt = new OperandFormat
                {
                    FormatString = instruction
                };
                opc = _filteredOpcodes.Find(o => instruction.Equals(o.DisasmFormat, Controller.Options.StringComparison));//Opcode.LookupOpcode(instruction, _filteredOpcodes, Controller.Options.StringComparison, false, _filteredOpcodes.Count);
            }
            else
            {
                if (_regWidth.IsMatch(operand))
                {
                    if (operand.Contains("16"))
                        force16 = true;
                    else
                        force24 = true;
                    operand = _regWidth.Replace(operand, string.Empty);
                }
                foreach (FormatBuilder builder in _builders)
                {
                    fmt = builder.GetFormat(operand);
                    if (fmt == null)
                        continue;

                    string instrFmt = string.Format("{0} {1}", instruction, fmt.FormatString);

                    if (force16 || force24)
                    {
                        if (force16)
                        {
                            instrFmt = instrFmt.Replace("${0:x2}", "${0:x4}");
                            instrFmt = instrFmt.Replace("${0:x6}", "${0:x4}");
                        }
                        else
                        {
                            instrFmt = instrFmt.Replace("${0:x2}", "${0:x6}");
                            instrFmt = instrFmt.Replace("${0:x4}", "${0:x6}");
                        }
                    }
                    else
                    {
                        // adjust the size of the format string to match the size of the operand expression
                        var expVal = Controller.Evaluator.Eval(fmt.Expression1, short.MinValue, UInt24.MaxValue);
                        var size = (expVal.Size() * 2).ToString();
                        if (!instrFmt.Contains(size))
                            instrFmt = Regex.Replace(instrFmt, @"2|4", size);
                    }

                    opc = _filteredOpcodes.Find(o => instrFmt.Equals(o.DisasmFormat, Controller.Options.StringComparison));//Opcode.LookupOpcode(instrFmt, _filteredOpcodes, Controller.Options.StringComparison, false, _filteredOpcodes.Count);
                    if (opc == null)
                    {
                        instrFmt = instrFmt.Replace("${0:x2}", "${0:x4}");
                        opc = _filteredOpcodes.Find(o => instrFmt.Equals(o.DisasmFormat, Controller.Options.StringComparison));//Opcode.LookupOpcode(instrFmt, _filteredOpcodes, Controller.Options.StringComparison, false, _filteredOpcodes.Count);

                        if (opc == null)
                        {
                            instrFmt = instrFmt.Replace("${0:x4}", "${0:x6}");
                            opc = _filteredOpcodes.Find(o => instrFmt.Equals(o.DisasmFormat, Controller.Options.StringComparison));//Opcode.LookupOpcode(instrFmt, _filteredOpcodes, Controller.Options.StringComparison, false, _filteredOpcodes.Count);
                        }
                    }
                    break;
                }
            }
            return new Tuple<OperandFormat, Opcode>(fmt, opc);
        }

        void SetCpu(CpuChangedEventArgs args)
        {
            if (args.Line.Operand.EnclosedInQuotes() == false &&
               !string.IsNullOrEmpty(args.Line.Filename))
            {
                Controller.Log.LogEntry(args.Line, ErrorStrings.QuoteStringNotEnclosed);
                return;
            }
            var cpu = args.Line.Operand.Trim('"');
            if (!cpu.Equals("6502") && !cpu.Equals("65C02") && !cpu.Equals("65816") && !cpu.Equals("6502i"))
            {
                string error = string.Format("Invalid CPU '{0}' specified", cpu);
                if (args.Line.SourceString.Equals(ConstStrings.COMMANDLINE_ARG))
                    throw new Exception(string.Format(error));
                else
                    Controller.Log.LogEntry(args.Line, error);
                return;
            }
            else
            {
                _cpu = cpu;
            }

            var cpuOpcodes = _opcodes.Where(o => o.CPU.Equals(_cpu));

            if (_cpu.Equals("65C02") || _cpu.Equals("65816") || _cpu.Equals("6502i"))
            {
                cpuOpcodes = cpuOpcodes.Concat(_opcodes.Where(o => o.CPU.Equals("6502")));

                if (_cpu.Equals("65816"))
                    cpuOpcodes = cpuOpcodes.Concat(_opcodes.Where(o => o.CPU.Equals("65C02")));
            }

            _filteredOpcodes = cpuOpcodes.ToList();

            if (_m16)
                SetImmediateA(3);
            if (_x16)
                SetImmediateXY(3);

        }

        void SetImmediateA(int size)
        {
            string fmt = size.Equals(3) ? " #${0:x4}" : " #${0:x2}";

            _filteredOpcodes.RemoveAll(o => o.Index.Equals(0x09) ||
                                            o.Index.Equals(0x29) ||
                                            o.Index.Equals(0x49) ||
                                            o.Index.Equals(0x69) ||
                                            o.Index.Equals(0x89) ||
                                            o.Index.Equals(0xa9) ||
                                            o.Index.Equals(0xc9) ||
                                            o.Index.Equals(0xe9));

            _filteredOpcodes.Add(new Opcode() { CPU = "6502",  DisasmFormat = "ora" + fmt, Size = size, Index = 0x09 });
            _filteredOpcodes.Add(new Opcode() { CPU = "6502",  DisasmFormat = "and" + fmt, Size = size, Index = 0x29 });
            _filteredOpcodes.Add(new Opcode() { CPU = "6502",  DisasmFormat = "eor" + fmt, Size = size, Index = 0x49 });
            _filteredOpcodes.Add(new Opcode() { CPU = "6502",  DisasmFormat = "adc" + fmt, Size = size, Index = 0x69 });
            _filteredOpcodes.Add(new Opcode() { CPU = "65C02", DisasmFormat = "bit" + fmt, Size = size, Index = 0x89 });
            _filteredOpcodes.Add(new Opcode() { CPU = "6502",  DisasmFormat = "lda" + fmt, Size = size, Index = 0xa9 });
            _filteredOpcodes.Add(new Opcode() { CPU = "6502",  DisasmFormat = "cmp" + fmt, Size = size, Index = 0xc9 });
            _filteredOpcodes.Add(new Opcode() { CPU = "6502",  DisasmFormat = "sbc" + fmt, Size = size, Index = 0xe9 });
        }

        void SetImmediateXY(int size)
        {
            string fmt = size.Equals(3) ? " #${0:x4}" : " #${0:x2}";

            _filteredOpcodes.RemoveAll(o => o.Index.Equals(0xa0) ||
                                            o.Index.Equals(0xa2) || 
                                            o.Index.Equals(0xc0) || 
                                            o.Index.Equals(0xe0));

            _filteredOpcodes.Add(new Opcode() { CPU = "6502", DisasmFormat = "ldy" + fmt, Size = size, Index = 0xa0 });
            _filteredOpcodes.Add(new Opcode() { CPU = "6502", DisasmFormat = "ldx" + fmt, Size = size, Index = 0xa2 });
            _filteredOpcodes.Add(new Opcode() { CPU = "6502", DisasmFormat = "cpy" + fmt, Size = size, Index = 0xc0 });
            _filteredOpcodes.Add(new Opcode() { CPU = "6502", DisasmFormat = "cpx" + fmt, Size = size, Index = 0xe0 });
        }

        void SetRegLongShort(string instruction)
        {
            if (instruction.StartsWith(".x", Controller.Options.StringComparison))
            {
                bool x16 = instruction.Equals(".x16", Controller.Options.StringComparison);
                if (x16 != _x16)
                {
                    _x16 = x16;
                    SetImmediateXY(_x16 ? 3 : 2);
                }
            }
            else
            {
                
                bool m16 = instruction.EndsWith("16", Controller.Options.StringComparison);
                if (m16 != _m16)
                {
                    _m16 = m16;
                    SetImmediateA(_m16 ? 3 : 2);
                }
                if (instruction.StartsWith(".mx", Controller.Options.StringComparison))
                {
                    bool x16 = instruction.EndsWith("16", Controller.Options.StringComparison);
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
            var csv = line.CommaSeparateOperand();

            foreach (string rta in csv)
            {
                if (rta.Equals("?"))
                {
                    Controller.Output.AddUninitialized(2);
                }
                else
                {
                    long val = Controller.Evaluator.Eval(rta, ushort.MinValue, ushort.MaxValue + 1);
                    line.Assembly.AddRange(Controller.Output.Add(val - 1, 2));
                }
            }
        }


        #region ILineAssembler.Methods

        public void AssembleLine(SourceLine line)
        {
            if (Controller.Output.PCOverflow)
            {
                Controller.Log.LogEntry(line,
                                        ErrorStrings.PCOverflow,
                                        Controller.Output.LogicalPC);
                return;
            }
            if (Reserved.IsOneOf("ReturnAddress", line.Instruction))
            {
                AssembleRta(line);
                return;
            }
            else if (Reserved.IsOneOf("LongShort", line.Instruction))
            {
                if (!string.IsNullOrEmpty(line.Operand))
                    Controller.Log.LogEntry(line, ErrorStrings.TooManyArguments, line.Instruction);
                else
                    SetRegLongShort(line.Instruction);
                return;
            }

            var formatOpcode = GetFormatAndOpcode(line);
            if (formatOpcode.Item1 == null)
            {
                Controller.Log.LogEntry(line, ErrorStrings.BadExpression, line.Operand);
                return;
            }
            if (formatOpcode.Item2 == null)
            {
                Controller.Log.LogEntry(line, ErrorStrings.UnknownInstruction, line.Instruction);
                return;
            }
            long eval1 = long.MinValue, eval2 = long.MinValue;
            long eval1Abs = 0;

            if (Reserved.IsOneOf("Branches", line.Instruction) || Reserved.IsOneOf("Branches16", line.Instruction))
            {
                eval1 = Controller.Evaluator.Eval(formatOpcode.Item1.Expression1, short.MinValue, ushort.MaxValue);
                eval1Abs = eval1 & 0xFFFF;
                try
                {
                    if (Reserved.IsOneOf("Branches", line.Instruction))
                        eval1 = Convert.ToSByte(Controller.Output.GetRelativeOffset((ushort)eval1Abs, Controller.Output.LogicalPC + 2));
                    else
                        eval1 = Convert.ToInt16(Controller.Output.GetRelativeOffset((ushort)eval1Abs, Controller.Output.LogicalPC + 3));
                }
                catch
                {
                    throw new OverflowException(eval1.ToString());
                }

            }
            else
            {
                var operandsize = 0;

                long minval = sbyte.MinValue, maxval = byte.MaxValue;

                if (string.IsNullOrEmpty(formatOpcode.Item1.Expression2))
                {
                    if (formatOpcode.Item2.Size == 4)
                    {
                        minval = Int24.MinValue;
                        maxval = UInt24.MaxValue;
                    }
                    else if (formatOpcode.Item2.Size == 3)
                    {
                        minval = short.MinValue;
                        maxval = ushort.MaxValue;
                    }
                }
                else
                {
                    eval2 = Controller.Evaluator.Eval(formatOpcode.Item1.Expression2, minval, maxval);
                }
                if (!string.IsNullOrEmpty(formatOpcode.Item1.Expression1))
                    eval1 = Controller.Evaluator.Eval(formatOpcode.Item1.Expression1, minval, maxval);
                

                if (!eval1.Equals(long.MinValue))
                {
                    if (formatOpcode.Item2.Size == 4)
                    {
                        eval1 &= 0xFFFFFF;
                    }
                    else if (formatOpcode.Item2.Size == 3 && eval2.Equals(long.MinValue))
                    {
                        eval1 &= 0xFFFF;
                    }
                    else
                    {
                        eval1 &= 0xFF;
                        if (!eval2.Equals(long.MinValue))
                            eval2 &= 0xFF;
                    }
                    eval1Abs = eval1;
                    operandsize = eval1.Size();
                }
                if (!eval2.Equals(long.MinValue))
                    operandsize += eval2.Size();
                
                if (operandsize >= formatOpcode.Item2.Size)
                    throw new OverflowException(line.Operand);
            }
            long operbytes = 0;
            if (!eval1.Equals(long.MinValue))
                operbytes = eval2.Equals(long.MinValue) ? (eval1 << 8) : (((eval1 << 8) | eval2) << 8);

            line.Disassembly = string.Format(formatOpcode.Item2.DisasmFormat, eval1Abs, eval2);
            line.Assembly = Controller.Output.Add(operbytes | (long)formatOpcode.Item2.Index, formatOpcode.Item2.Size);
        }

        string ConvertFuncs(string operand)
        {
            return Regex.Replace(operand, @"([a-zA-Z][a-zA-Z0-9]*)(\(.+\,.+\))", (match) =>
            {
                var fcncall = match.Groups[2].Value.FirstParenEnclosure();
                var post = string.Empty;
                if (!fcncall.Equals(match.Groups[2].Value))
                    post = match.Groups[2].Value.Substring(fcncall.Length);
                var evalfcn = Controller.Evaluator.Eval(match.Groups[1].Value + fcncall);
                return evalfcn.ToString() + ConvertFuncs(post);
            });
        }

        public int GetInstructionSize(SourceLine line)
        {
            if (Reserved.IsOneOf("ReturnAddress", line.Instruction))
                return 2 * line.CommaSeparateOperand().Count;
            
            var formatOpcode = GetFormatAndOpcode(line);
            if (formatOpcode.Item2 != null)
                return formatOpcode.Item2.Size;
            return 0;
        }

        public bool AssemblesInstruction(string instruction)
        {
            return Reserved.IsReserved(instruction);
        }

        #endregion

        #endregion
    }
}