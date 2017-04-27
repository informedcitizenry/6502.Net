//-----------------------------------------------------------------------------
// Copyright (c) 2017 Nate Burnett <informedcitizenry@gmail.com>
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
using System.Text;
using System.Threading.Tasks;

namespace Asm6502.Net
{
    /// <summary>
    /// A helper class to create disassembly of lines of 6502 source.
    /// </summary>
    public class Disasm6502 : AssemblerBase, ILineDisassembler
    {
        #region Constructors

        /// <summary>
        /// Constructs an instance of the Disasm6502 class.
        /// </summary>
        /// <param name="controller">The assembly controller.</param>
        public Disasm6502(IAssemblyController controller)
            : base(controller)
        {
            Reserved.Types.Add("Directives", new HashSet<string>(new string[]
                {
                    ".pron", ".proff"
                }));

            Reserved.Types.Add("SkipOnVerbose", new HashSet<string>(new string[]
                {
                    ".eor", ".error", ".cerror", ".cwarn", ".relocate", ".pseudopc", 
                    ".realpc", ".endrelocate", ".warn",  ".end", ".equ"
                }));

            PrintingOn = true;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Add the file info to the disassembly, if verbose option is set.
        /// </summary>
        /// <param name="line">The SourceLine.</param>
        /// <returns>A formatted representation of the filename and 
        /// line number of the source.</returns>
        private string DisassembleFileLine(SourceLine line)
        {
            string lineinfo = line.Filename;
            if (lineinfo.Length > 14)
                lineinfo = lineinfo.Substring(0, 11) + "...";
            lineinfo += "(" + line.LineNumber.ToString() + ")";
            return string.Format("{0,-20}:", lineinfo);
        }

        /// <summary>
        /// Add the address of the source to the disassembly. The first method
        /// called in the DisassembleLine method.
        /// </summary>
        /// <param name="line">The SourceLine.</param>
        /// <returns>Returns a hex representation of the source line address.</returns>
        private string DisassembleAddress(SourceLine line)
        {
            if (string.IsNullOrEmpty(line.Instruction) ||
                Reserved.IsReserved(line.Instruction) ||
                line.DoNotAssemble)
                return string.Empty;
                      
            if (line.Instruction == "=" || line.Instruction.Equals(".equ", Controller.Options.StringComparison))
            {
                Int64 value = 0;
                if (line.Label == "*")
                    return string.Empty;
                if (string.IsNullOrEmpty(line.Operand) || line.Operand == "*")
                    value = line.PC;
                else
                    value = Controller.Evaluator.Eval(line.Operand);
                return string.Format("=${0:x}  ", value);
            }
            else
            {
                if (line.Instruction.StartsWith("."))
                    return string.Format(">{0:x4}  ", line.PC);
                else
                    return string.Format(".{0:x4}  ", line.PC);
            }
        }

        /// <summary>
        /// Add the assembled bytes in the SourceLine to the disassembly.
        /// </summary>
        /// <param name="line">The SourceLine</param>
        /// <returns>A string representation of the hex bytes of
        /// the source assembly.</returns>
        private string DisassembleAsm(SourceLine line, string source)
        {
            if (line.Assembly.Count == 0 || Controller.Options.NoAssembly)
                return string.Empty;

            StringBuilder sb = new StringBuilder();
            line.Assembly.ForEach(b => sb.AppendFormat(" {0:x2}", b));

            if (sb.Length > 24)
            {
                int pc = line.PC;
                
                var subdisasms = sb.ToString().SplitByLength(24).ToList();
                sb.Clear();

                for (int i = 0; i < subdisasms.Count; i++)
                {
                    sb.AppendFormat("{0,-29}{1,-10}",
                                    subdisasms[i],
                                    source).TrimEnd();
                    sb.AppendLine();
                    pc += 8;
                    if (i < subdisasms.Count - 1)
                    {
                        string format = ">{0:x4}  ";
                        if (Controller.Options.VerboseList)
                            format = "                    :" + format;
                        sb.AppendFormat(format, pc);
                    }
                    source = string.Empty;
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Disassemble a line of 6502 source into a supplied 
        /// System.Text.StringBuilder object.
        /// </summary>
        /// <param name="line">The SourceLine to disassemble.</param>
        /// <param name="sb">The System.Texxt.StringBuilder to output disassembly.</param>
        public void DisassembleLine(SourceLine line, StringBuilder sb)
        {
            if (!line.DoNotAssemble)
            {
                if (line.Instruction.Equals(".pron", Controller.Options.StringComparison))
                    PrintingOn = true;
                else if (line.Instruction.Equals(".proff", Controller.Options.StringComparison))
                    PrintingOn = false;
            }
            if (!PrintingOn)
                return;// printing has been suppressed

            string sourcestr = line.SourceString;

            if (!Controller.Options.VerboseList)
            {
                if (line.DoNotAssemble)
                {
                    if (line.IsDefinition && string.IsNullOrEmpty(line.Label))
                        return;
                    sourcestr = line.Label;
                }
                else if (Controller.Options.NoSource)
                {
                    sourcestr = string.Empty;
                }
                if (string.IsNullOrEmpty(line.Label) &&
                    (Reserved.IsReserved(line.Instruction)))
                    return; // skip directives (e.g., .if blocks, etc.) and anonymous blocks
                else if (string.IsNullOrWhiteSpace(line.Label + line.Instruction))
                    return;
            }
            else
            {
                if (string.IsNullOrEmpty(sourcestr))
                    sourcestr = line.Instruction;
                else if (Controller.Options.NoSource)
                    sourcestr = string.Empty;
                sb.Append(DisassembleFileLine(line));
            }


            sb.AppendFormat("{0,-7}", DisassembleAddress(line));

            string asm = DisassembleAsm(line, sourcestr);

            if (asm.Length > 24)
            {
                sb.Append(asm);
                return;
            }


            if (string.IsNullOrEmpty(line.Disassembly) || Controller.Options.NoDissasembly)
            {
                sb.AppendFormat("{0,-29}{1,-10}", asm, sourcestr).AppendLine();
            }
            else
            {
                sb.AppendFormat("{0,-13}{1,-16}{2,-10}", asm, line.Disassembly, sourcestr)
                  .AppendLine();
            }
        }

        /// <summary>
        /// Disassemble a line of 6502-source.
        /// </summary>
        /// <param name="line">The SourceLine</param>
        /// <returns>A string representation of the source.</returns>
        public string DisassembleLine(SourceLine line)
        {
            StringBuilder sb = new StringBuilder();
            DisassembleLine(line, sb);
            return sb.ToString();
        }
        #endregion

        #region Properties

        /// <summary>
        /// Gets the flag indicating that printing is currently on
        /// </summary>
        public bool PrintingOn { get; private set; }

        /// <summary>
        /// Gets or sets the set of instructions the disassembler
        /// will skip if verbose flag is not set in the controller options.
        /// </summary>
        public HashSet<string> SkipOnVerbose
        {
            get
            {
                return Reserved.Types["SkipOnVerbose"];

            }
            set
            {
                Reserved.Types["SkipOnVerbose"] = value;
            }
        }
        #endregion
    }
}
