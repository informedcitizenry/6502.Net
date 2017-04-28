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

using System;
using System.Collections.Generic;
using System.Linq;

namespace Asm6502.Net
{
    /// <summary>
    /// A line assembler that will assemble into 6502 instructions.
    /// </summary>
    public class Asm6502 : AssemblerBase, ILineAssembler
    {
        #region Members

        private string[] opcodeFormats_ = 
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
            Reserved.Types.Add("Accumulator", new HashSet<string>(new string[]
                {
                    "adc", "and", "cmp", "eor", "lda", "ora", "sbc", "sta"
                }));

            Reserved.Types.Add("Branches", new HashSet<string>(new string[]
                {
                    "bcc","bcs","beq","bmi","bne","bpl","bvc","bvs"
                }));

            Reserved.Types.Add("Implied", new HashSet<string>(new string[]
                {
                    "brk","clc","cld","cli","clv","dex","dey","inx","iny","nop","pha","php","pla",
                    "plp","rti","rts","sec","sed","sei","tax","tay","tsx","txa","txs","tya"
                }));

            Reserved.Types.Add("ImpliedAccumulator", new HashSet<string>(new string[]
                {
                    "asl", "lsr", "rol", "ror"
                }));

            Reserved.Types.Add("Jumps", new HashSet<string>(new string[]
                {
                    "jmp", "jsr"
                }));
            Reserved.Types.Add("Mnemonics", new HashSet<string>(new string[]
                {
                    "asl", "bit", "cpx", "cpy", "dec", "inc", "ldx",
                    "ldy", "lsr", "rol", "ror", "stx", "sty"
                }));
        }

        #endregion

        #region Methods

        private Tuple<string, string> GetOperandComponents(SourceLine line)
        {
            if (string.IsNullOrEmpty(line.Operand))
                return new Tuple<string, string>(string.Empty, string.Empty);

            if (Reserved.IsOneOf("Implied", line.Instruction) || IsImpliedAccumulator(line))
                return null;

            string opfmt = "${0:x4}";
            string operand = line.Operand;

            if (Reserved.IsOneOf("Branches", line.Instruction))
            {
                return new Tuple<string, string>(operand, opfmt);
            }
            

            if (line.Operand.StartsWith("#"))
            {
                opfmt = "#${0:x2}";
                operand = operand.Substring(1);
                return new Tuple<string, string>(operand, opfmt);
            }
            else if (operand.StartsWith("("))
            {
                string firstparen = ExpressionEvaluator.FirstParenGroup(operand, false);
                if (firstparen == operand)
                {
                    operand = line.Operand.TrimStart('(');
                    if (operand.ToLower().EndsWith(",x)"))
                    {
                        opfmt = "(${0:x2},x)";
                        operand = operand.Substring(0, operand.Length - 3);
                    }
                    else
                    {
                        opfmt = "(${0:x4})";
                        operand = operand.TrimEnd(')');
                    }
                    return new Tuple<string, string>(operand, opfmt);
                }
                else if (firstparen.Length == operand.Length - 2 && operand.ToLower().EndsWith("),y"))
                {
                    operand = operand.TrimStart('(').Substring(0, operand.Length - 4);
                    opfmt = "(${0:x2}),y";
                    return new Tuple<string, string>(operand, opfmt);
                }
            }

            string post = string.Empty;
            if (operand.ToLower().EndsWith(",x") || operand.ToLower().EndsWith(",y"))
            {
                post = operand.Substring(operand.Length - 2);
                operand = operand.Substring(0, operand.Length - 2);
                if (string.IsNullOrEmpty(operand))
                {
                    return null;
                }
            }

            long val = Controller.Evaluator.Eval(operand);

            if (Reserved.IsOneOf("Jumps", line.Instruction) || 
                (post.Equals(",y") && Reserved.IsOneOf("Accumulator", line.Instruction)))
                opfmt = "${0:x4}" + post;
            else
                opfmt = "${0:x" + val.Size() * 2 + "}" + post;

            return new Tuple<string, string>(operand, opfmt);
        }

        // it is an allowed convention to include "a" as an operand in 
        // implied instructions on the accumulator, e.g. lsr a
        private bool IsImpliedAccumulator(SourceLine line)
        {
            if (Reserved.IsOneOf("ImpliedAccumulator",line.Instruction) &&
                line.Operand.Equals("a", Controller.Options.StringComparison))
            {
                string scoped = Controller.GetNearestScope(line.Operand, line.Scope);
                return !Controller.Labels.ContainsKey(scoped);//!Controller.Labels.ContainsKey(scoped);
            }
            return false;
        }

        /// <summary>
        /// Get the actual instruction opcode, including size.
        /// </summary>
        /// <param name="line">The SourceLine to assemble.</param>
        /// <returns>A tuple containing the instruction's opcode bytes, size, 
        /// and a string representation of the disassembly.</returns>
        private Tuple<int, int, string> GetInstruction(SourceLine line)
        {
            string instr = line.Instruction;
            string operand = line.Operand;//string.Empty;
            string opfmt = string.Empty;
            Int64 operval = 0;
            int size = GetInstructionSize(line);

            if (string.IsNullOrEmpty(line.Instruction))
                return null;

            if (string.IsNullOrEmpty(operand) == false)
            {
                if (IsImpliedAccumulator(line))
                {
                    int impl = opcodeFormats_.ToList().IndexOf(line.Instruction);
                    return new Tuple<int, int, string>(impl, 1, opcodeFormats_[impl]);
                }
                Tuple<string, string> components = GetOperandComponents(line);
                if (components == null)
                {
                    Controller.Log.LogEntry(line, Resources.ErrorStrings.None);
                    return null;
                }
                operand = components.Item1;
                if (operand.Trim() != operand)
                {
                    Controller.Log.LogEntry(line, Resources.ErrorStrings.BadExpression, operand);
                    return null;
                }
                opfmt = components.Item2;
                operval = Controller.Evaluator.Eval(operand);
            }
            if (Reserved.IsOneOf("Branches", instr))
            {
                operval = Controller.Output.GetRelativeOffset(Convert.ToUInt16(operval), Controller.Output.GetPC() + 2);//line.PC + 2);
                if (operval > sbyte.MaxValue || operval < sbyte.MinValue)
                {
                    Controller.Log.LogEntry(line, Resources.ErrorStrings.RelativeBranchOutOfRange, operval.ToString());
                    return null;
                }
            }

            string fmt = opcodeFormats_.FirstOrDefault(
                delegate(string op)
                {
                    if (string.IsNullOrEmpty(op))
                        return false;
                    if (string.IsNullOrEmpty(opfmt))
                        return op.Equals(instr, Controller.Options.StringComparison);
                    return op.Equals(instr + " " + opfmt, Controller.Options.StringComparison);
                });

            if (string.IsNullOrEmpty(fmt))
            {
                Controller.Log.LogEntry(line, Resources.ErrorStrings.UnknownInstruction, line.Instruction);
                return null;
            }
           
            int opcode = opcodeFormats_.ToList().IndexOf(fmt.ToLower());
            if (opcode == -1) // just in case??
            {
                Controller.Log.LogEntry(line, Resources.ErrorStrings.UnknownInstruction);
                return null;
            }
            if (operval.Size() > 2 || ((size == 3 || operval.Size() == 2) && fmt.Contains("x2")))
            {
                Controller.Log.LogEntry(line, Resources.ErrorStrings.IllegalQuantity, operval.ToString());
                return null;
            }
            string disasm = string.Format(fmt, operval & (operval.AndMask()));
            if (Reserved.IsOneOf("Branches", instr))
            {
                var rel = (Controller.Output.GetPC() + 2) + operval;
                disasm = string.Format(fmt, (rel & ushort.MaxValue));
            }
            opcode += (Convert.ToInt32(operval) << 8);
            return new Tuple<int, int, string>(opcode, size, disasm);
        }

        #region ILineAssembler.Methods

        /// <summary>
        /// Assemble the line of source into 6502 instructions.
        /// </summary>
        /// <param name="line">The source line to assembler.</param>
        public void AssembleLine(SourceLine line)
        {
            if (Controller.Output.PCOverflow)
            {
                Controller.Log.LogEntry(line, 
                                        Resources.ErrorStrings.PCOverflow, 
                                        Controller.Output.GetPC().ToString());
                return;
            }
            var opcode = GetInstruction(line);
            if (opcode == null)
                return;
            Controller.Output.Add(opcode.Item1, opcode.Item2);
            line.Disassembly = opcode.Item3;
        }

        /// <summary>
        /// Gets the size of the instruction in the source line.
        /// </summary>
        /// <param name="line">The source line to query.</param>
        /// <returns>Returns the size in bytes of the instruction or directive.</returns>
        public int GetInstructionSize(SourceLine line)
        {
            var components = GetOperandComponents(line);
            if (components == null || string.IsNullOrEmpty(components.Item2))
                return 1;
            if (components.Item2.Contains("x4") && !Reserved.IsOneOf("Branches", line.Instruction))
                return 3;
            return 2;
        }

        /// <summary>
        /// Indicates whether this line assembler will assemble the 
        /// given instruction or directive.
        /// </summary>
        /// <param name="instruction">The instruction.</param>
        /// <returns>True, if the line assembler can assemble the source, 
        /// otherwise false.</returns>
        public bool AssemblesInstruction(string instruction)
        {
            return Reserved.IsReserved(instruction);
        }

        public bool HandleFirstPass(SourceLine line)
        {
            throw new NotImplementedException();
        }

        #endregion

        #endregion
    }
}
