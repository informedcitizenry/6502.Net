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
using System.Text;

namespace DotNetAsm
{
    public class Disassembler: AssemblerBase, ILineDisassembler
    {
        #region Constructors

        /// <summary>
        /// Constructs an instance of the Disasm6502 class.
        /// </summary>
        /// <param name="controller">The assembly controller.</param>
        public Disassembler(IAssemblyController controller)
            : base(controller)
        {
            PrintingOn = true;
            Reserved.DefineType("Blocks", new string[]
                {
                    AssemblyController.OPEN_SCOPE,
                    AssemblyController.CLOSE_SCOPE
                });
            Reserved.DefineType("Directives", new string[]
                {
                    ".elif", ".else", ".endif", ".eor", ".error", ".errorif", ".if", ".ifdef", 
                    ".warnif", ".relocate", ".pseudopc", ".realpc", ".endrelocate", ".warn"
                });
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
            if (string.IsNullOrEmpty(lineinfo) == false)
            {
                if (lineinfo.Length > 14)
                lineinfo = lineinfo.Substring(0, 11) + "...";
                lineinfo += "(" + line.LineNumber.ToString() + ")";
            }
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
            if ((string.IsNullOrEmpty(line.Label) && (string.IsNullOrEmpty(line.Instruction) ||
                Reserved.IsReserved(line.Instruction))) ||
                line.DoNotAssemble)
                return string.Empty;
                      
            if (line.Instruction == "=" || 
                line.Instruction.Equals(".equ", Controller.Options.StringComparison) || 
                line.Instruction.Equals(".var", Controller.Options.StringComparison))
            {
                Int64 value = 0;
                if (line.Label == "*" || Controller.Options.NoSource)
                    return string.Empty;
                if (line.Label == "-" || line.Label == "+")
                {
                    value = line.PC;
                }
                else if (line.Instruction.Equals(".var", Controller.Options.StringComparison))
                {
                    value = Controller.GetVariable(line.Label);
                }
                else
                {
                    var labelval = Controller.GetScopedLabelValue(line.Label, line);
                    value = long.Parse(labelval);
                }
                return string.Format("=${0:x" + value.Size() * 2 + "}", value);
            }
            else
            {
                if (line.Instruction.StartsWith(".") && !Reserved.IsReserved(line.Instruction))
                    return string.Format(">{0:x4}", line.PC);
                else
                    return string.Format(".{0:x4}", line.PC);
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
                long pc = line.PC;
                
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
                        string format = ">{0:x4}    ";
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
        /// <param name="sb">The System.Text.StringBuilder to output disassembly.</param>
        public void DisassembleLine(SourceLine line, StringBuilder sb)
        {
            if (!line.DoNotAssemble)
            {
                if (line.Instruction.Equals(".pron"))
                    PrintingOn = true;
                else if (line.Instruction.Equals(".proff"))
                    PrintingOn = false;
            }
            if (!PrintingOn)
                return;// printing has been suppressed

            if (line.SourceString.Equals(SourceLine.SHADOW_SOURCE))
                return;

            string sourcestr = line.SourceString;
            if (!Controller.Options.VerboseList)
            {
                if (line.DoNotAssemble || Reserved.IsReserved(line.Instruction))
                {
                    if (line.DoNotAssemble) return;
                    // skip directives (e.g., macro definitions, etc.) and anonymous blocks
                    if (Reserved.IsReserved(line.Instruction) && string.IsNullOrEmpty(line.Label))
                        return;
                    sourcestr = line.Label;
                }
                else if (Controller.Options.NoSource)
                {
                    sourcestr = string.Empty;
                }
                else if (string.IsNullOrWhiteSpace(line.Label + line.Instruction))
                {
                    return;
                }
            }
            else
            {
                if (string.IsNullOrEmpty(sourcestr))
                    return;
                else if (Controller.Options.NoSource)
                    sourcestr = string.Empty;
                sb.Append(DisassembleFileLine(line));
            }
            sb.AppendFormat("{0,-9}", DisassembleAddress(line));

            if (Controller.Options.NoAssembly == false)
            {
                string asm = DisassembleAsm(line, sourcestr);
                if (asm.Length > 24)
                {
                    sb.Append(asm);
                    return;
                }
                else if (string.IsNullOrEmpty(line.Disassembly) && Controller.Options.NoDissasembly == false)
                {
                    sb.AppendFormat("{0,-29}", asm);
                }
                else
                {
                    sb.AppendFormat("{0,-13}", asm);
                }
            }
            
            if (Controller.Options.NoDissasembly == false)
            {
                if (string.IsNullOrEmpty(line.Disassembly) == false)
                    sb.AppendFormat("{0,-16}", line.Disassembly);
                else if (Controller.Options.NoAssembly)
                    sb.AppendFormat("{0,-28}", line.Disassembly);
            }

            if (Controller.Options.NoSource == false)
                sb.AppendFormat("{0,-10}", sourcestr);
            else if (string.IsNullOrEmpty(line.Disassembly) && line.Assembly.Count == 0)
                sb.TrimEnd();
            
            sb.AppendLine();
        }

        public override bool IsReserved(string token)
        {
            return Reserved.IsReserved(token);
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

        #endregion
    }
}
