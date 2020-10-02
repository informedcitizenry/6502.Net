//-----------------------------------------------------------------------------
// Copyright (c) 2017-2020 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Core6502DotNet
{
    /// <summary>
    /// An enumeration of possible assembly errors.
    /// </summary>
    public enum AssemblyErrorReason
    {
        NotFound = 0,
        ReturnNotAllowed,
        OutsideBounds,
        ExceptionRaised
    }

    /// <summary>
    /// A utility for providing multi line assembly functionality.
    /// </summary>
    public static class MultiLineAssembler
    {
        /// <summary>
        /// Assembles the lines within the iterator.
        /// </summary>
        /// <param name="lines">The lines.</param>
        /// <param name="withAssemblers">The assemblers to assemble.</param>
        /// <param name="allowReturn">Allow .return directive.</param>
        /// <param name="allowOutput">Allow the assemblers to emit output data.</param>
        /// <param name="disasmBuilder">The disassembly builder.</param>
        /// <param name="disassembleAll">Disassemble all lines, even those without instructions.</param>
        /// <param name="errorHandler">A function or delegate to handle any errors during assembly.</param>
        /// <param name="services">The shared <see cref="AssemblyServices"/> object.</param>
        /// <returns>The return value if a .return directive is assembled.</returns>
        public static double AssembleLines(RandomAccessIterator<SourceLine> lines,
                                           IEnumerable<AssemblerBase> withAssemblers,
                                           bool allowReturn,
                                           StringBuilder disasmBuilder,
                                           bool disassembleAll,
                                           Func<SourceLine,
                                                AssemblyErrorReason, 
                                                Exception, 
                                                bool> errorHandler,
                                           AssemblyServices services)
        {
            foreach(var line in lines)
            {
                try
                {
                    if (line.Label != null || line.Instruction != null)
                    {
                        if (line.InstructionName.Equals(".return"))
                        {
                            if (allowReturn)
                            {
                                if (line.OperandHasToken)
                                    return services.Evaluator.Evaluate(line.Operand);
                                return double.NaN;
                            }
                            else
                            {
                                if (errorHandler(line, AssemblyErrorReason.ReturnNotAllowed, null))
                                    continue;
                                break;
                            }
                        }
                        var asm = withAssemblers.FirstOrDefault(a => a.AssemblesLine(line));
                        if (asm != null)
                        {
                            var disasm = asm.AssembleLine(line);
                            if (disasmBuilder != null && !services.PrintOff)
                            {
                                if (disassembleAll)
                                    disasmBuilder.Append($"\"{line.Filename}\"(")
                                                 .Append($"{line.LineNumber}): ".PadLeft(8));
                                if (!string.IsNullOrEmpty(disasm))
                                    disasmBuilder.AppendLine(disasm);
                            }
                        }
                        else if (line.Instruction != null)
                        {
                            if (!errorHandler(line, AssemblyErrorReason.NotFound, null))
                                break;
                        }
                        else if (disassembleAll && disasmBuilder != null && !services.PrintOff)
                        {
                            disasmBuilder.Append($"\"{line.Filename}\"(")
                                         .Append($"{line.LineNumber}): ".PadLeft(8))
                                         .AppendLine(line.UnparsedSource.PadLeft(50, ' '));
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (!errorHandler(line, AssemblyErrorReason.ExceptionRaised, ex))
                        break;
                }
            }
            return double.NaN;
        }
    }
}
