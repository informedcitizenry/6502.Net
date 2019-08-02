//-----------------------------------------------------------------------------
// Copyright (c) 2017-2019 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;
using System.Linq;
using System.Text;

namespace DotNetAsm
{
    public sealed class Disassembler : AssemblerBase, ILineDisassembler
    {
        #region Constructors

        /// <summary>
        /// Constructs an instance of the <see cref="T:DotNetAsm.Disassembler"/> class.
        /// </summary>
        public Disassembler()
        {
            PrintingOn = true;
            Reserved.DefineType("Blocks", ConstStrings.OPEN_SCOPE, ConstStrings.CLOSE_SCOPE);
            Reserved.DefineType("Directives",
                    ".cpu", ".elif", ".else", ".endif", ".eor", ".error", ".errorif", ".if", ".ifdef",
                    ".warnif", ".relocate", ".pseudopc", ".realpc", ".endrelocate", ".warn"
                );
        }

        #endregion

        #region Methods

        /// <summary>
        /// Add the file info to the disassembly, if verbose option is set.
        /// </summary>
        /// <param name="line">The The <see cref="T:DotNetAsm.SourceLine"/>.</param>
        /// <returns>A formatted representation of the filename and 
        /// line number of the source.</returns>
        string DisassembleFileLine(SourceLine line)
        {
            string lineinfo = line.Filename;
            if (string.IsNullOrEmpty(lineinfo) == false)
            {
                if (lineinfo.Length > 14)
                    lineinfo = lineinfo.Substring(0, 11) + "...";
                lineinfo += $"({line.LineNumber})";
            }
            return $"{lineinfo,-20}:";
        }

        /// <summary>
        /// Add the address of the source to the disassembly. The first method
        /// called in the DisassembleLine method.
        /// </summary>
        /// <param name="line">The <see cref="T:DotNetAsm.SourceLine"/>.</param>
        /// <returns>A hex representation of the source line address.</returns>
        string DisassembleAddress(SourceLine line)
        {
            if ((string.IsNullOrEmpty(line.Label) && (string.IsNullOrEmpty(line.Instruction) ||
                Reserved.IsReserved(line.Instruction))) ||
                line.DoNotAssemble)
                return string.Empty;

            if (line.Instruction == "=" ||
                line.Instruction.Equals(".let", Assembler.Options.StringComparison) ||
                line.Instruction.Equals(".equ", Assembler.Options.StringComparison))
            {
                Int64 value = 0;
                if (line.Label == "*" || Assembler.Options.NoSource)
                    return string.Empty;
                if (line.Label == "-" || line.Label == "+")
                {
                    value = line.PC;
                }
                else if (line.Instruction.Equals(".let", Assembler.Options.StringComparison))
                {
                    var variable = Assembler.Symbols.Variables.GetVariableFromExpression(line.Operand, line.Scope);
                    value = Assembler.Symbols.Variables.GetSymbolValue(variable);
                }
                else
                {
                    value = Assembler.Symbols.Labels.GetSymbolValue(line.Scope + line.Label);
                }
                return string.Format($"=${{0:x{value.Size() & 2}}}", value);
            }
            if (line.Instruction.StartsWith(".", Assembler.Options.StringComparison) &&
                    !Reserved.IsReserved(line.Instruction))
                return $">{line.PC:x4}";

            return $".{line.PC:x4}";
        }

        /// <summary>
        /// Add the assembled bytes in the <see cref="T:DotNetAsm.SourceLine"/> to the disassembly.
        /// </summary>
        /// <param name="line">The source line</param>
        /// <returns>A string representation of the hex bytes of
        /// the source assembly.</returns>
        string DisassembleAsm(SourceLine line, string source)
        {
            if (line.Assembly.Count == 0 || Assembler.Options.NoAssembly)
                return string.Empty;

            var sb = new StringBuilder();
            line.Assembly.ForEach(b => sb.Append($" {b:x2}"));

            if (sb.Length > 24)
            {
                long pc = line.PC;

                var subdisasms = sb.ToString().SplitByLength(24).ToList();
                sb.Clear();

                for (int i = 0; i < subdisasms.Count; i++)
                {
                    sb.AppendLine($"{subdisasms[i],-29}{source,-10}".TrimEnd());
                    pc += 8;
                    if (i < subdisasms.Count - 1)
                    {
                        string pcHex = $">{pc:x4}    ";
                        if (Assembler.Options.VerboseList)
                            sb.Append("                    :");
                        sb.Append(pcHex);
                    }
                    source = string.Empty;
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Disassemble a line of assembly source into a supplied 
        /// <see cref="T:System.Text.StringBuilder"/> object.
        /// </summary>
        /// <param name="line">The source line to disassemble.</param>
        /// <param name="sb">The <see cref="T:System.Text.StringBuilder"/> to output disassembly.</param>
        public void DisassembleLine(SourceLine line, StringBuilder sb)
        {
            if (!line.DoNotAssemble)
            {
                if (line.Instruction.Equals(".pron"))
                    PrintingOn = true;
                else PrintingOn &= !line.Instruction.Equals(".proff");
            }
            if (!PrintingOn || line.SourceString.Equals(ConstStrings.SHADOW_SOURCE))
                return;

            string sourcestr = line.SourceString;
            if (!Assembler.Options.VerboseList)
            {
                if (line.DoNotAssemble || Reserved.IsReserved(line.Instruction))
                {
                    if (line.DoNotAssemble) return;
                    // skip directives (e.g., macro definitions, etc.) and anonymous blocks
                    if (Reserved.IsReserved(line.Instruction) && string.IsNullOrEmpty(line.Label))
                        return;
                    sourcestr = line.Label;
                }
                else if (Assembler.Options.NoSource)
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
                if (Assembler.Options.NoSource)
                    sourcestr = string.Empty;
                sb.Append(DisassembleFileLine(line));
            }
            sb.Append($"{DisassembleAddress(line),-9}");

            if (Assembler.Options.NoAssembly == false)
            {
                var asm = DisassembleAsm(line, sourcestr);
                if (asm.Length > 24)
                {
                    sb.Append(asm);
                    return;
                }

                if (string.IsNullOrEmpty(line.Disassembly) && Assembler.Options.NoDissasembly == false)
                    sb.Append($"{asm,-29}");
                else
                    sb.Append($"{asm,-13}");
            }

            if (Assembler.Options.NoDissasembly == false)
            {
                if (string.IsNullOrEmpty(line.Disassembly) == false)
                    sb.Append($"{line.Disassembly,-16}");
                else if (Assembler.Options.NoAssembly)
                    sb.Append($"{line.Disassembly,-28}");
            }

            if (Assembler.Options.NoSource == false)
                sb.Append($"{sourcestr,-10}");
            else if (string.IsNullOrEmpty(line.Disassembly) && line.Assembly.Count == 0)
                sb.TrimEnd();

            sb.AppendLine();
        }

        public override bool IsReserved(string token) => Reserved.IsReserved(token);

        /// <summary>
        /// Disassemble a line of assembly source.
        /// </summary>
        /// <param name="line">The The <see cref="T:DotNetAsm.SourceLine"/>.</param>
        /// <returns>A string representation of the source.</returns>
        public string DisassembleLine(SourceLine line)
        {
            var sb = new StringBuilder();
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
