//-----------------------------------------------------------------------------
// Copyright (c) 2017-2021 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;
using System.IO;
using System.Linq;

namespace Core6502DotNet
{
    /// <summary>
    /// Helper class that responds to and logs errors to the error log.
    /// </summary>
    public class AssembleErrorHandler
    {
        readonly AssemblyServices _services;

        /// <summary>
        /// Creates a new instnace of an exception logger.
        /// </summary>
        /// <param name="services">The shared <see cref="AssemblyServices"/> object.</param>
        public AssembleErrorHandler(AssemblyServices services) => _services = services;

        /// <summary>
        /// Log an error to the logger from a raised error event.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="args">The <see cref="AssemblyErrorEventArgs"/> containing error details.</param>
        /// <returns><c>true</c> if assembly should terminate based on the exception and the overall assembly state,
        /// <c>false</c> otherwise.</returns>
        public void HandleError(object sender, AssemblyErrorEventArgs args)
        {
            if (args.ErrorReason == AssemblyErrorReason.NotFound)
            {
                _services.Log.LogEntry(args.Line.Instruction,
                                    $"Unknown instruction \"{args.Line.Instruction.Name}\".");
                return;
            }
            if (args.Exception is ErrorLogFullException logExc)
            {
                logExc.Log.LogEntrySimple(logExc.Message);
            }
            else if (args.Exception is SymbolException symbEx)
            {
                if (symbEx.SymbolToken != null)
                    _services.Log.LogEntry(symbEx.SymbolToken, symbEx.Message);
                else
                    _services.Log.LogEntry(args.Line, symbEx.Position, symbEx.Message, symbEx.SymbolName.ToString());
                return;
            }
            else if (args.Exception is SyntaxException synEx)
            {
                if (synEx.Token != null)
                    _services.Log.LogEntry(synEx.Token, synEx.Message, false, true);
                else if (args.Line != null)
                    _services.Log.LogEntry(args.Line, synEx.Position, synEx.Message);
                else
                    _services.Log.LogEntrySimple(synEx.Message);
            }
            else if (args.Exception is InvalidCpuException cpuEx)
            {
                _services.Log.LogEntry(args.Line.Instruction, cpuEx.Message);
            }
            else if (args.Exception is ReturnException ||
                        args.Exception is BlockAssemblerException ||
                        args.Exception is SectionException)
            {
                if (args.Exception is ReturnException retEx)
                    _services.Log.LogEntry(retEx.Token, retEx.Message, false, true);
                else
                    _services.Log.LogEntry(args.Line.Instruction, args.Exception.Message);
            }
            else if (args.Exception is BlockClosureException blockEx)
            {
                if (blockEx.LineExpectingClosure.Instruction != null)
                    _services.Log.LogEntry(blockEx.LineExpectingClosure.Instruction, blockEx.Message);
                else
                    _services.Log.LogEntry(blockEx.LineExpectingClosure, 1, blockEx.Message);
            }
            else
            {
                if (_services.CurrentPass <= 0 || _services.PassNeeded)
                {
                    if (args.Assembler != null && !(args.Exception is ProgramOverflowException))
                    {
                        var instructionSize = args.Assembler.GetInstructionSize(args.Line);
                        if (_services.Output.AddressIsValid(instructionSize + _services.Output.LogicalPC))
                            _services.Output.AddUninitialized(instructionSize);

                    }
                    _services.PassNeeded = true;
                    return;
                }
                else
                {
                    if (args.Exception is ExpressionException expEx)
                    {
                        var firstTokenPos = args.Line.Operands.FirstOrDefault(t => t.Position == expEx.Position);

                        if (args.Exception is IllegalQuantityException illegalExp)
                        {
                            if (firstTokenPos != null)
                                _services.Log.LogEntry(firstTokenPos, $"Illegal quantity for \"{args.Line.Instruction.Name}\"", false, true);
                            else
                                _services.Log.LogEntry(args.Line, illegalExp.Position,
                                    $"Illegal quantity for \"{args.Line.Instruction.Name}\" ({illegalExp.Quantity}).");
                            return;
                        }
                        else if (firstTokenPos != null)
                            _services.Log.LogEntry(firstTokenPos, args.Exception.Message, false, true);
                        else
                            _services.Log.LogEntry(args.Line, expEx.Position, args.Exception.Message);
                    }
                    else if (args.Exception is ProgramOverflowException)
                    {
                        _services.Log.LogEntry(args.Line.Instruction, args.Exception.Message);
                    }
                    else if (args.Exception is InvalidPCAssignmentException pcEx)
                    {
                        if (pcEx.SectionNotUsedError)
                            _services.Log.LogEntry(args.Line.Instruction,
                                pcEx.Message);
                        else
                            _services.Log.LogEntry(args.Line.Operands[0],
                                $"Invalid Program Counter assignment {pcEx.Message} in expression.", false, true);
                    }
                    else
                    {
                        if (args.Line != null)
                        {
                            if (args.Line.Operands.Count > 0)
                                _services.Log.LogEntry(args.Line.Operands[0], args.Exception.Message, false, true);
                            else
                                _services.Log.LogEntry(args.Line, 1, args.Exception.Message);
                        }
                        else
                        {
                            _services.Log.LogEntrySimple(args.Exception.Message);
                        }
                    }
                }
                var mla = sender as MultiLineAssembler;
                mla.Returning = true;
            }
        }

        /// <summary>
        /// Dump the error log.
        /// </summary>
        /// <param name="clearLog">Indicate whether to clear the log after dumping.</param>
        public void DumpErrors(bool clearLog)
        {
            _services.Log.DumpErrors();
            if (!string.IsNullOrEmpty(_services.Options.ErrorFile))
            {
                using FileStream fs = new FileStream(_services.Options.ErrorFile, FileMode.Create);
                using StreamWriter writer = new StreamWriter(fs);
                writer.WriteLine($"{Assembler.AssemblerNameSimple}");
                writer.WriteLine($"Error file generated {DateTime.Now:f}");
                writer.WriteLine($"{_services.Log.ErrorCount} error(s):\n");
                _services.Log.DumpErrors(writer);
            }
            if (clearLog) _services.Log.ClearAll();
        }
    }
}
