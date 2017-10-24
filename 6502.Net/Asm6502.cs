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
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            Reserved.DefineType("Accumulator", new string[]
                {
                    "adc", "and", "cmp", "eor", "lda", "ora", "sbc", "sta"
                });

            Reserved.DefineType("Branches", new string[]
                {
                    "bcc","bcs","beq","bmi","bne","bpl","bvc","bvs"
                });

            Reserved.DefineType("Implied", new string[]
                {
                    "brk","clc","cld","cli","clv","dex","dey","inx","iny","nop","pha","php","pla",
                    "plp","rti","rts","sec","sed","sei","tax","tay","tsx","txa","txs","tya"
                });

            Reserved.DefineType("ImpliedAccumulator", new string[]
                {
                    "asl", "lsr", "rol", "ror"
                });

            Reserved.DefineType("Jumps", new string[]
                {
                    "jmp", "jsr"
                });
            Reserved.DefineType("Mnemonics", new string[]
                {
                    "asl", "bit", "cpx", "cpy", "dec", "inc", "ldx",
                    "ldy", "lsr", "rol", "ror", "stx", "sty"
                });

            Reserved.DefineType("ReturnAddress", new string[]
                {
                    ".rta"
                });

            RegexOptions ignore = Controller.Options.CaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;

            _regInd = new Regex(@"^(\(.*\))$", RegexOptions.Compiled | ignore);
            _regXY = new Regex(@"(.+)(,[xy])$", RegexOptions.Compiled | ignore);

            _builders = new FormatBuilder[]
            {
                new FormatBuilder(@"^#([^\s].*)$()", "#{2}", "${0:x2}", string.Empty, 2, 2, 1, 2, Controller.Options.RegexOption),
                new FormatBuilder(@"^\(\s*([^\s]+)\s*,\s*x\s*\)$()", "({2},x)", "${0:x2}", string.Empty, 2,2,1,2, Controller.Options.RegexOption ),
                new FormatBuilder(@"^([^\s]+)\s*,\s*y$()", "{2},y", "${0:x2}", string.Empty, 2, 2, 1, 2, Controller.Options.RegexOption, true ),
                new FormatBuilder(@"^([^\s]+)\s*,\s*x$()", "{2},x", "${0:x2}", string.Empty, 2, 2, 1, 2, Controller.Options.RegexOption),
                new FormatBuilder(@"^(.+)$()", "{2}", "${0:x2}", string.Empty, 2, 2, 1, 2, Controller.Options.RegexOption, true )
            };

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
        }

        #endregion

        #region Methods

        // it is an allowed convention to include "a" as an operand in 
        // implied instructions on the accumulator, e.g. lsr a
        private bool IsImpliedAccumulator(SourceLine line)
        {
            if (Reserved.IsOneOf("ImpliedAccumulator", line.Instruction) &&
                line.Operand.Equals("a", Controller.Options.StringComparison))
                return string.IsNullOrEmpty(Controller.GetScopedLabelValue("a", line));
            return string.IsNullOrEmpty(line.Operand);
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
            string instruction = line.Instruction.ToLower();
            string operand = line.Operand;
            if (Reserved.IsOneOf("ImpliedAccumulator", line.Instruction))
                operand = Regex.Replace(operand, @"^a$", string.Empty);

            OperandFormat fmt = null;
            int opc = -1;
            int size = 1;
            long eval = long.MinValue, evalAbs = 0;
            if (string.IsNullOrEmpty(operand))
            {
                fmt = new OperandFormat();
                fmt.FormatString = instruction;
                opc = Opcode.LookupOpcodeIndex(instruction, _opcodeFormats, Controller.Options.StringComparison);
            }
            else
            {
                foreach (FormatBuilder builder in _builders)
                {
                    fmt = builder.GetFormat(operand);
                    if (fmt == null)
                        continue;
                    string instrFmt = string.Format("{0} {1}", instruction, fmt.FormatString);
                    opc = Opcode.LookupOpcodeIndex(instrFmt, _opcodeFormats, Controller.Options.StringComparison);
                    if (opc != -1 && fmt.FormatString.Contains("${0:x2}"))
                    {
                        eval = Controller.Evaluator.Eval(fmt.Expression1, short.MinValue, ushort.MaxValue);
                        if (eval.Size() == 2)
                        {
                            instrFmt = instrFmt.Replace("${0:x2}", "${0:x4}");
                            opc = Opcode.LookupOpcodeIndex(instrFmt, _opcodeFormats, Controller.Options.StringComparison);
                        }
                    }
                    if (opc == -1)
                    {
                        if (!instruction.Equals("jmp") &&
                            fmt.FormatString.StartsWith("(") &&
                            fmt.FormatString.EndsWith(")") &&
                            !fmt.FormatString.EndsWith(",x)"))
                        {
                            fmt.FormatString = fmt.FormatString.Replace("(", string.Empty).Replace(")", string.Empty);
                            instrFmt = string.Format("{0} {1}", instruction, fmt.FormatString);
                        }
                        else
                        {
                            instrFmt = instrFmt.Replace("${0:x2}", "${0:x4}");
                        }

                        opc = Opcode.LookupOpcodeIndex(instrFmt, _opcodeFormats, Controller.Options.StringComparison);
                    }
                    fmt.FormatString = instrFmt;
                    break;
                }
            }

            if (fmt == null)
            {
                Controller.Log.LogEntry(line, ErrorStrings.BadExpression, line.Operand);
                return;
            }
            if (opc == -1)
            {
                Controller.Log.LogEntry(line, ErrorStrings.UnknownInstruction, line.Instruction);
                return;
            }

            if (string.IsNullOrEmpty(fmt.Expression1) == false)
            {
                size++;
                if (fmt.FormatString.Contains("${0:x4}"))
                {
                    if (eval == long.MinValue)
                        eval = Controller.Evaluator.Eval(fmt.Expression1, short.MinValue, ushort.MaxValue);
                    evalAbs = eval & 0xFFFF;
                    if (Reserved.IsOneOf("Branches", line.Instruction))
                    {
                        try
                        {
                            eval = Convert.ToSByte(Controller.Output.GetRelativeOffset((ushort)evalAbs, Controller.Output.LogicalPC + 2));
                        }
                        catch
                        {
                            throw new OverflowException(eval.ToString());
                        }
                    }
                    else
                    {
                        size++;
                    }
                }
                else
                {
                    eval = Controller.Evaluator.Eval(fmt.Expression1, sbyte.MinValue, byte.MaxValue);
                    evalAbs = eval & 0xFF;
                }

            }
            line.Disassembly = string.Format(_opcodeFormats[opc], evalAbs);
            line.Assembly = Controller.Output.Add(opc | (int)eval << 8, size);
        }

        private void AssembleRta(SourceLine line)
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

        /// <summary>
        /// Determines if the instruction is y-indexed instruction on the accumulator.
        /// </summary>
        /// <param name="line">The DotNetAsm.SourceLine of the instruction</param>
        /// <returns>True, if the instruction is y-indexed for the accumulator</returns>
        private bool AccumY(SourceLine line)
        {
            return _regXY.IsMatch(line.Operand) &&
                   !_regInd.IsMatch(line.Operand) &&
                   line.Operand.EndsWith(",y", Controller.Options.StringComparison) &&
                   Reserved.IsOneOf("Accumulator", line.Instruction);
        }

        public int GetInstructionSize(SourceLine line)
        {
            string instruction = line.Instruction;
            string operand = line.Operand;
            if (Reserved.IsOneOf("ReturnAddress", instruction))
                return line.CommaSeparateOperand().Count * 2;
            if (Reserved.IsOneOf("Implied", instruction) || IsImpliedAccumulator(line))
                return 1;
            if (Reserved.IsOneOf("Branches", instruction) || line.Operand.StartsWith("#"))
                return 2;
            if (Reserved.IsOneOf("Jumps", instruction))
                return 3;
            if (operand.EndsWith(",x)") || operand.EndsWith(",X)"))
                return 2;

            var parts = line.CommaSeparateOperand();
            operand = parts.First();
            if (parts.Count > 1)
            {
                if (parts.Last().Equals("y", Controller.Options.StringComparison))
                {
                    if (_regInd.IsMatch(operand))
                    {
                        operand = _regInd.Match(operand).Groups[1].Value;

                        if (operand.Equals(operand.FirstParenEnclosure()))
                            return 2;
                    }
                    if (AccumY(line))
                        return 3;
                }
            }
            return Controller.Evaluator.Eval(operand).Size() + 1;
        }

        public bool AssemblesInstruction(string instruction)
        {
            return Reserved.IsReserved(instruction);
        }

        #endregion

        #endregion
    }
}