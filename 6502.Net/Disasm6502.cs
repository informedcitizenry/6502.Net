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
                return "       ";
                      //.c000  /
           
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
        private string DisassembleAsm(SourceLine line)
        {
            string asm = string.Empty;
            string listing = string.Empty;
            
            if (line.Assembly.Count == 0)
                return string.Empty;
           
            line.Assembly.ForEach(b => asm += string.Format(" {0:x2}", b));
            if (line.Instruction.StartsWith(".") == false)
            {
                return asm;
            }
            string monitor = asm;

            if (monitor.Length > 24)
            {
                int pc = line.PC;
                string source = line.SourceString;
                var subdisasms = monitor.SplitByLength(24).ToList();

                for (int i = 0; i < subdisasms.Count; i++)
                {
                    listing += string.Format("{0,-29}{1,-10}", 
                                            subdisasms[i], 
                                            source);
                    listing = listing.TrimEnd() + Environment.NewLine;
                    pc += 8;
                    if (i < subdisasms.Count - 1)
                    {
                        string format = ">{0:x4}  ";
                        if (Controller.Options.VerboseList)
                            format = "                    :" + format;
                        listing += string.Format(format, pc);
                    }
                   
                    source = string.Empty;
                }
                return listing;
            }
            else
            {
                listing = asm;
            }
            return listing;
        }

        /// <summary>
        /// Disassemble a line of 6502-source.
        /// </summary>
        /// <param name="line">The SourceLine</param>
        /// <returns>A string representation of the source.</returns>
        public string DisassembleLine(SourceLine line)
        {
            string listing = string.Empty;

            if (!line.DoNotAssemble)
            {
                if (line.Instruction.Equals(".pron", Controller.Options.StringComparison))
                    PrintingOn = true;
                else if (line.Instruction.Equals(".proff", Controller.Options.StringComparison))
                    PrintingOn = false;
            }
            if (!PrintingOn)
                return string.Empty;// printing has been suppressed

            string disassem = string.Empty;
            string sourcestr = line.SourceString;

            if (!Controller.Options.VerboseList)
            {
                if (line.DoNotAssemble)
                {
                    if (line.IsDefinition && string.IsNullOrEmpty(line.Label))
                        return string.Empty; 
                    sourcestr = line.Label;
                }
                if (string.IsNullOrEmpty(line.Label) &&
                    (Reserved.IsReserved(line.Instruction)))
                    return string.Empty; // skip directives (e.g., .if blocks, etc.) and anonymous blocks
                else if (string.IsNullOrWhiteSpace(line.Label + line.Instruction))
                    return string.Empty;
            }
            else
            {
                if (string.IsNullOrEmpty(sourcestr))
                    sourcestr = line.Instruction;
                disassem = DisassembleFileLine(line);
            }
            var collen = string.IsNullOrEmpty(line.Disassembly) ? 36 : 20;
            if (Controller.Options.VerboseList)
                collen += 21;
            
            disassem += DisassembleAddress(line);
           
            string asm = DisassembleAsm(line);
            disassem += asm;

            if (asm.Length > 24)
                return disassem;
           
            if (string.IsNullOrEmpty(line.Disassembly))
            {

                listing += string.Format("{0,-" + collen + "}{1,-10}{2}",
                                                        disassem,
                                                        sourcestr,
                                                        Environment.NewLine);
            }
            else
            {
                listing += string.Format("{0,-" + collen + "}{1,-16}{2,-10}{3}",
                                        disassem,
                                        line.Disassembly,
                                        sourcestr,
                                        Environment.NewLine);
            }
            return listing;
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
