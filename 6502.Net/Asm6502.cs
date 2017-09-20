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
    public class Asm6502 : AssemblerBase, ILineAssembler
    {
        #region Members

        private FormatBuilder[] _builders;

        private string[] _opcodeFormats = 
            {
                "brk",              // 00
                "ora (${0:x2},x)",  // 01
                null,               // 02
                null,               // 03
                null,               // 04
                "ora ${0:x2}",      // 05
                "asl ${0:x2}",      // 06
                null,               // 07
                "php",              // 08
                "ora #${0:x2}",     // 09
                "asl",              // 0a
                null,               // 0b
                null,               // 0c
                "ora ${0:x4}",      // 0d
                "asl ${0:x4}",      // 0e
                null,               // 0f
                "bpl ${0:x4}",      // 10
                "ora (${0:x2}),y",  // 11
                null,               // 12
                null,               // 13
                null,               // 14
                "ora ${0:x2},x",    // 15
                "asl ${0:x2},x",    // 16
                null,               // 17
                "clc",              // 18
                "ora ${0:x4},y",    // 19
                null,               // 1a
                null,               // 1b
                null,               // 1c
                "ora ${0:x4},x",    // 1d
                "asl ${0:x4},x",    // 1e
                null,               // 1f
                "jsr ${0:x4}",      // 20
                "and (${0:x2},x)",  // 21
                null,               // 22
                null,               // 23
                "bit ${0:x2}",      // 24
                "and ${0:x2}",      // 25
                "rol ${0:x2}",      // 26
                null,               // 27
                "plp",              // 28
                "and #${0:x2}",     // 29
                "rol",              // 2a
                null,               // 2b
                "bit ${0:x4}",      // 2c
                "and ${0:x4}",      // 2d
                "rol ${0:x4}",      // 2e
                null,               // 2f
                "bmi ${0:x4}",      // 30
                "and (${0:x2}),y",  // 31
                null,               // 32
                null,               // 33
                null,               // 34
                "and ${0:x2},x",    // 35
                "rol ${0:x2},x",    // 36
                null,               // 37
                "sec",              // 38
                "and ${0:x4},y",    // 39
                null,               // 3a
                null,               // 3b
                null,               // 3c
                "and ${0:x4},x",    // 3d
                "rol ${0:x4},x",    // 3e
                null,               // 3f
                "rti",              // 40
                "eor (${0:x2},x)",  // 41
                null,               // 42
                null,               // 43
                null,               // 44
                "eor ${0:x2}",      // 45
                "lsr ${0:x2}",      // 46
                null,               // 47
                "pha",              // 48
                "eor #${0:x2}",     // 49
                "lsr",              // 4a
                null,               // 4b
                "jmp ${0:x4}",      // 4c
                "eor ${0:x4}",      // 4d
                "lsr ${0:x4}",      // 4e
                null,               // 4f
                "bvc ${0:x4}",      // 50
                "eor (${0:x2}),y",  // 51
                null,               // 52
                null,               // 53
                null,               // 54
                "eor ${0:x2},x",    // 55
                "lsr ${0:x2},x",    // 56
                null,               // 57
                "cli",              // 58
                "eor ${0:x4},y",    // 59
                null,               // 5a
                null,               // 5b
                null,               // 5c
                "eor ${0:x4},x",    // 5d
                "lsr ${0:x4},x",    // 5e
                null,               // 5f
                "rts",              // 60
                "adc (${0:x2},x)",  // 61
                null,               // 62
                null,               // 63
                null,               // 64
                "adc ${0:x2}",      // 65
                "ror ${0:x2}",      // 66
                null,               // 67
                "pla",              // 68
                "adc #${0:x2}",     // 69
                "ror",              // 6a
                null,               // 6b
                "jmp (${0:x4})",    // 6c
                "adc ${0:x4}",      // 6d
                "ror ${0:x4}",      // 6e
                null,               // 6f
                "bvs ${0:x4}",      // 70
                "adc (${0:x2}),y",  // 71
                null,               // 72
                null,               // 73
                null,               // 74
                "adc ${0:x2},x",    // 75
                "ror ${0:x2},x",    // 76
                null,               // 77
                "sei",              // 78
                "adc ${0:x4},y",    // 79
                null,               // 7a
                null,               // 7b
                null,               // 7c
                "adc ${0:x4},x",    // 7d
                "ror ${0:x4},x",    // 7e
                null,               // 7f
                null,               // 80
                "sta (${0:x2},x)",  // 81
                null,               // 82
                null,               // 83
                "sty ${0:x2}",      // 84
                "sta ${0:x2}",      // 85
                "stx ${0:x2}",      // 86
                null,               // 87
                "dey",              // 88
                null,               // 89
                "txa",              // 8a
                null,               // 8b
                "sty ${0:x4}",      // 8c
                "sta ${0:x4}",      // 8d
                "stx ${0:x4}",      // 8e
                null,               // 8f
                "bcc ${0:x4}",      // 90
                "sta (${0:x2}),y",  // 91
                null,               // 92
                null,               // 93
                "sty ${0:x2},x",    // 94
                "sta ${0:x2},x",    // 95
                "stx ${0:x2},y",    // 96
                null,               // 97
                "tya",              // 98
                "sta ${0:x4},y",    // 99
                "txs",              // 9a
                null,               // 9b
                null,               // 9c
                "sta ${0:x4},x",    // 9d
                null,               // 9e
                null,               // 9f
                "ldy #${0:x2}",     // a0
                "lda (${0:x2},x)",  // a1
                "ldx #${0:x2}",     // a2
                null,               // a3
                "ldy ${0:x2}",      // a4
                "lda ${0:x2}",      // a5
                "ldx ${0:x2}",      // a6
                null,               // a7
                "tay",              // a8
                "lda #${0:x2}",     // a9
                "tax",              // aa
                null,               // ab
                "ldy ${0:x4}",      // ac
                "lda ${0:x4}",      // ad
                "ldx ${0:x4}",      // ae
                null,               // af
                "bcs ${0:x4}",      // b0
                "lda (${0:x2}),y",  // b1
                null,               // b2
                null,               // b3
                "ldy ${0:x2},x",    // b4
                "lda ${0:x2},x",    // b5
                "ldx ${0:x2},y",    // b6
                null,               // b7
                "clv",              // b8
                "lda ${0:x4},y",    // b9
                "tsx",              // ba
                null,               // bb
                "ldy ${0:x4},x",    // bc
                "lda ${0:x4},x",    // bd
                "ldx ${0:x4},y",    // be
                null,               // bf
                "cpy #${0:x2}",     // c0
                "cmp (${0:x2},x)",  // c1
                null,               // c2
                null,               // c3
                "cpy ${0:x2}",      // c4
                "cmp ${0:x2}",      // c5
                "dec ${0:x2}",      // c6
                null,               // c7
                "iny",              // c8
                "cmp #${0:x2}",     // c9
                "dex",              // ca
                null,               // cb
                "cpy ${0:x4}",      // cc
                "cmp ${0:x4}",      // cd
                "dec ${0:x4}",      // ce
                null,               // cf
                "bne ${0:x4}",      // d0
                "cmp (${0:x2}),y",  // d1
                null,               // d2
                null,               // d3
                null,               // d4
                "cmp ${0:x2},x",    // d5
                "dec ${0:x2},x",    // d6
                null,               // d7
                "cld",              // d8
                "cmp ${0:x4},y",    // d9
                null,               // da
                null,               // db
                null,               // dc
                "cmp ${0:x4},x",    // dd
                "dec ${0:x4},x",    // de
                null,               // df
                "cpx #${0:x2}",     // e0
                "sbc (${0:x2},x)",  // e1
                null,               // e2
                null,               // e3
                "cpx ${0:x2}",      // e4
                "sbc ${0:x2}",      // e5
                "inc ${0:x2}",      // e6
                null,               // e7
                "inx",              // e8
                "sbc #${0:x2}",     // e9
                "nop",              // ea
                null,               // eb
                "cpx ${0:x4}",      // ec
                "sbc ${0:x4}",      // ed
                "inc ${0:x4}",      // ee
                null,               // ef
                "beq ${0:x4}",      // f0
                "sbc (${0:x2}),y",  // f1
                null,               // f2
                null,               // f3
                null,               // f4
                "sbc ${0:x2},x",    // f5
                "inc ${0:x2},x",    // f6
                null,               // f7
                "sed",              // f8
                "sbc ${0:x4},y",    // f9
                null,               // fa
                null,               // fb
                null,               // fc
                "sbc ${0:x4},x",    // fd
                "inc ${0:x4},x",    // fe
                null                // ff
            };

        private Regex _regInd;
        private Regex _regXY;

        #endregion

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

            RegexOptions ignore = Controller.Options.CaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;

            _regInd  = new Regex(@"^(\(.*\))$",     RegexOptions.Compiled | ignore);
            _regXY   = new Regex(@"(.+)(,[xy])$", RegexOptions.Compiled | ignore);

            _builders = new FormatBuilder[]
            {
                new FormatBuilder(@"^#(.+)$()", "#{2}", "${0:x2}", string.Empty, 2, 2, 1, 2, Controller.Options.CaseSensitive),
                new FormatBuilder(@"^\(\s*(.+),\s*x\s*\)$()", "({2},x)", "${0:x2}", string.Empty, 2,2,1,2, Controller.Options.CaseSensitive ),
                new FormatBuilder(@"^(.+)\s*,\s*y$()", "{2},y", "${0:x2}", string.Empty, 2, 2, 1, 2, Controller.Options.CaseSensitive, true ),
                new FormatBuilder(@"^(.+)\s*,\s*x$()", "{2},x", "${0:x2}", string.Empty, 2, 2, 1, 2, Controller.Options.CaseSensitive),
                new FormatBuilder(@"^(.+)$()", "{2}", "${0:x2}", string.Empty, 2, 2, 1, 2, Controller.Options.CaseSensitive, true )
            };
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
                                        Controller.Output.GetPC().ToString());
                return;
            }
            string instruction = Controller.Options.CaseSensitive ? line.Instruction : line.Instruction.ToLower();
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
                opc = Opcode.LookupOpcodeIndex(instruction, _opcodeFormats);
            }
            else
            {
                foreach (FormatBuilder builder in _builders)
                {
                    fmt = builder.GetFormat(operand);
                    if (fmt == null)
                        continue;
                    string instrFmt = string.Format("{0} {1}", instruction, fmt.FormatString);
                    opc = Opcode.LookupOpcodeIndex(instrFmt, _opcodeFormats);
                    if (opc != -1 && fmt.FormatString.Contains("${0:x2}"))
                    {
                        eval = Controller.Evaluator.Eval(fmt.Expression1, short.MinValue, ushort.MaxValue);
                        if (eval.Size() == 2)
                        {
                            instrFmt = instrFmt.Replace("${0:x2}", "${0:x4}");
                            opc = Opcode.LookupOpcodeIndex(instrFmt, _opcodeFormats);
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

                        opc = Opcode.LookupOpcodeIndex(instrFmt, _opcodeFormats);
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
                            eval = Convert.ToSByte(Controller.Output.GetRelativeOffset((ushort)evalAbs, Controller.Output.GetPC() + 2));
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
            Controller.Output.Add(opc | (int)eval << 8, size);
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

                        if (operand.Equals(ExpressionEvaluator.FirstParenGroup(operand)))
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

        protected override bool IsReserved(string token)
        {
            return Reserved.IsReserved(token);
        }

        #endregion

        #endregion
    }
}
