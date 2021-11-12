//-----------------------------------------------------------------------------
// Copyright (c) 2017-2021 informedcitizenry <informedcitizenry@gmail.com>
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
        ExceptionRaised
    }

    /// <summary>
    /// Represents event data for the AssemblyExceptionEvent.
    /// </summary>
    public sealed class AssemblyErrorEventArgs : EventArgs
    {
        /// <summary>
        /// Creates a new instance of an <see cref="AssemblyErrorEventArgs"/> object.
        /// </summary>
        /// <param name="assembler">The assembler raising the error.</param>
        /// <param name="line">The line where the error occurred.</param>
        /// <param name="errorReason">The error reason.</param>
        /// <param name="exception">The exception, if it raised the error.</param>
        public AssemblyErrorEventArgs(AssemblerBase assembler,
                                          SourceLine line,
                                          AssemblyErrorReason errorReason,
                                          Exception exception) : base() =>
            (Assembler, Line, ErrorReason, Exception) = (assembler, line, errorReason, exception);

        /// <summary>
        /// Gets the <see cref="AssemblerBase"/> assembler that raised the error.
        /// </summary>
        public AssemblerBase Assembler { get; }

        /// <summary>
        /// Gets the line where the error occurred.
        /// </summary>
        public SourceLine Line { get; }

        /// <summary>
        /// Gets the error reason for the error.
        /// </summary>
        public AssemblyErrorReason ErrorReason { get;}
        
        /// <summary>
        /// Gets the exception that raised the error.
        /// </summary>
        public Exception Exception { get; }
    }

    /// <summary>
    /// A utility for providing multi line assembly functionality.
    /// </summary>
    public sealed class MultiLineAssembler
    {
        #region Members

        IEnumerable<AssemblerBase> _assemblers;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a new instance of multi-line assembler.
        /// </summary>
        /// <param name="diassembleAll">Determines whether disassembly processing should occur 
        /// during assembly.</param>
        /// <param name="stopDisassemblyFunc">The function that determines when assembly 
        /// should stop disassembly processing.</param>
        public MultiLineAssembler(bool diassembleAll, Func<bool> stopDisassemblyFunc)
            => (DisassembleAll, StopDisassembly, _assemblers) =
            (diassembleAll, stopDisassemblyFunc, new List<AssemblerBase>());

        #endregion

        #region Methods


        /// <summary>
        /// Set the <see cref="AssemblerBase"/>es to use when assembling.
        /// </summary>
        /// <param name="assemblers"></param>
        /// <returns>This <see cref="MultiLineAssembler"/> object.</returns>
        public MultiLineAssembler WithAssemblers(IEnumerable<AssemblerBase> assemblers)
        {
            _assemblers = assemblers;
            return this;
        }

        /// <summary>
        /// Assembles a collection of <see cref="SourceLine"/>s.
        /// </summary>
        /// <param name="iterator">An iterator to the collection of lines.</param>
        /// <returns>A string representation of the disassembly of all assembled lines.</returns>
        public string AssembleLines(RandomAccessIterator<SourceLine> iterator)
        {
            var disasmBuilder = new StringBuilder();
            SourceLine line;
            AssemblerBase asm = null;
            ReturnValue = double.NaN;
            Returning = false;
            while ((line = iterator.GetNext()) != null && !Returning)
            {
                try
                {
                    if (line.Label != null || line.Instruction != null)
                    {
                        if ((asm = _assemblers.FirstOrDefault(a => a.AssemblesLine(line))) != null)
                        {
                            var disasm = asm.Assemble(iterator);
                            if (!StopDisassembly())
                            {
                                if (DisassembleAll && !string.IsNullOrEmpty(line.Filename))
                                    disasmBuilder.Append($"\"{line.Filename}\"(")
                                                 .Append($"{line.LineNumber}): ".PadLeft(8));
                                if (!string.IsNullOrEmpty(disasm))
                                    disasmBuilder.AppendLine(disasm);
                                else if (DisassembleAll)
                                    disasmBuilder.AppendLine();
                            }
                        }
                        else if (line.Instruction != null)
                        {
                            AssemblyError?.Invoke(this, new AssemblyErrorEventArgs(null, line, AssemblyErrorReason.NotFound, null));
                        }
                    }
                    else if (DisassembleAll && !StopDisassembly())
                    {
                        disasmBuilder.Append($"\"{line.Filename}\"(")
                                     .Append($"{line.LineNumber}): ".PadLeft(8))
                                     .Append(" ".PadLeft(43))
                                     .AppendLine(line.Source);
                    }
                }
                catch (Exception ex)
                {
                    line = iterator.Current;
                    AssemblyError?.Invoke(this, new AssemblyErrorEventArgs(asm, line, AssemblyErrorReason.ExceptionRaised, ex));
                }
            }
            return disasmBuilder.ToString();
        }
        #endregion

        #region Properties

        /// <summary>
        /// Get or set the return value associated to the return result of the multi-line assembler.
        /// </summary>
        public double ReturnValue { get; set; }

        /// <summary>
        /// Gets or sets the flag indicating the multi-line assembler should cease assembling lines
        /// and return control to its caller.
        /// </summary>
        public bool Returning { get; set; }

        /// <summary>
        /// Emit disassembly for all lines.
        /// </summary>

        public bool DisassembleAll { get; set; }

        /// <summary>
        /// The callback function that determines whether disassembly should stop.
        /// </summary>
        public Func<bool> StopDisassembly { get; set; }

        #endregion

        #region Events

        /// <summary>
        /// The event when an error occurred during assembling.
        /// </summary>
        public event EventHandler<AssemblyErrorEventArgs> AssemblyError;

        #endregion
    }
}
