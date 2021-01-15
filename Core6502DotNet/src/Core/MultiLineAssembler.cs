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
        ReturnNotAllowed,
        ExceptionRaised
    }

    /// <summary>
    /// A utility for providing multi line assembly functionality.
    /// </summary>
    public class MultiLineAssembler
    {
        #region Subclasses

        /// <summary>
        /// The options to pass to a <see cref="MultiLineAssembler"/> for assembly.
        /// </summary>
        public class Options
        {
            #region Properties

            /// <summary>
            /// Allow the <see cref="MultiLineAssembler"/> to return a value when encountering the
            /// .return directive.
            /// </summary>
            public bool AllowReturn { get; set; }

            /// <summary>
            /// Emit disassembly for all lines.
            /// </summary>

            public bool DisassembleAll { get; set; }

            /// <summary>
            /// The <see cref="Evaluator"/> object to help when processing a .return directive.
            /// </summary>
            public Evaluator Evaluator { get; set; }

            /// <summary>
            /// The callback function that determines whether disassembly should stop.
            /// </summary>
            public Func<bool> StopDisassembly { get; set; }

            /// <summary>
            /// The error handler function.
            /// </summary>
            public Func<AssemblerBase,
                        SourceLine,
                        AssemblyErrorReason,
                        Exception,
                        bool> ErrorHandler
            { get; set; }

            #endregion
        };

        #endregion

        #region Members

        readonly Options _options;
        IEnumerable<AssemblerBase> _assemblers;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a new instance of multi-line assembler.
        /// </summary>
        public MultiLineAssembler()
        {
            _options = new Options
            {
                Evaluator = new Evaluator(),
                AllowReturn = false,
                DisassembleAll = false,
                ErrorHandler =
                (AssemblerBase assembler, SourceLine line, AssemblyErrorReason reason, Exception ex) => false,
                StopDisassembly = () => false
            };
            _assemblers = new List<AssemblerBase>();
        }

        /// <summary>
        /// Constructs a new instance of multi-line assembler.
        /// </summary>
        /// <param name="options">The <see cref="Options"/> object.</param>
        public MultiLineAssembler(Options options)
            => _options = options;

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
        /// Set the <see cref="Options"/> when assembling.
        /// </summary>
        /// <param name="options">The multi-line assembly options.</param>
        /// <returns>This <see cref="MultiLineAssembler"/> object.</returns>
        public MultiLineAssembler WithOptions(Options options)
        {
            _options.AllowReturn = options.AllowReturn;
            _options.DisassembleAll = options.DisassembleAll;
            _options.ErrorHandler = options.ErrorHandler;
            _options.StopDisassembly = options.StopDisassembly;
            _options.Evaluator = options.Evaluator;
            return this;
        }

        /// <summary>
        /// Assembles a collection of <see cref="SourceLine"/>s.
        /// </summary>
        /// <param name="iterator">An iterator to the collection of lines.</param>
        /// <param name="disassembly">The resulting disassembly.</param>
        /// <returns>The return value if a .return directive is assembled.</returns>
        public double AssembleLines(RandomAccessIterator<SourceLine> iterator, out string disassembly)
        {
            disassembly = string.Empty;
            var disasmBuilder = new StringBuilder();
            SourceLine line;
            AssemblerBase asm = null;
            while ((line = iterator.GetNext()) != null)
            {
                try
                {
                    if (line.Label != null || line.Instruction != null)
                    {
                        if (line.Instruction != null && line.Instruction.Name.Equals(".return"))
                        {
                            if (_options.AllowReturn)
                            {
                                disassembly = disasmBuilder.ToString();
                                if (line.Operands.Count > 0)
                                {
                                    var it = line.Operands.GetIterator();
                                    var result =  _options.Evaluator.Evaluate(it);
                                    if (it.Current != null)
                                        throw new SyntaxException(it.Current, "Unexpected expression.");
                                    return result;
                                }
                                return double.NaN;
                            }
                            else
                            {
                                if (_options.ErrorHandler(null, line, AssemblyErrorReason.ReturnNotAllowed, null))
                                    continue;
                                break;
                            }
                        }
                        if ((asm = _assemblers.FirstOrDefault(a => a.AssemblesLine(line))) != null)
                        {
                            var disasm = asm.Assemble(iterator);
                            if (!_options.StopDisassembly())
                            {
                                if (_options.DisassembleAll && !string.IsNullOrEmpty(line.Filename))
                                    disasmBuilder.Append($"\"{line.Filename}\"(")
                                                 .Append($"{line.LineNumber}): ".PadLeft(8));
                                if (!string.IsNullOrEmpty(disasm))
                                    disasmBuilder.AppendLine(disasm);
                                else if (_options.DisassembleAll)
                                    disasmBuilder.AppendLine();
                            }
                        }
                        else if (line.Instruction != null && !_options.ErrorHandler(null, line, AssemblyErrorReason.NotFound, null))
                        {
                            break;
                        }
                    }
                    else if (_options.DisassembleAll && !_options.StopDisassembly())
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
                    if (!_options.ErrorHandler(asm, line, AssemblyErrorReason.ExceptionRaised, ex))
                        break;
                }
            }
            disassembly = disasmBuilder.ToString();
            return double.NaN;
        }
        #endregion
    }
}
