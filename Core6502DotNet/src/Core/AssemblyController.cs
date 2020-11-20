//-----------------------------------------------------------------------------
// Copyright (c) 2017-2020 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Core6502DotNet
{
    /// <summary>
    /// Implements an assembly controller to process source input and convert 
    /// to assembled output.
    /// </summary>
    public sealed class AssemblyController
    {
        #region Members

        readonly AssemblyServices _services;
        readonly List<AssemblerBase> _assemblers;
        readonly ProcessorOptions _processorOptions;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a new instance of an <see cref="AssemblyController"/>, which controls
        /// the assembly process.
        /// </summary>
        /// <param name="args">The commandline arguments.</param>
        /// <param name="cpuSetHandler">The CPU change handler.</param>
        /// <param name="formatSelector">The format selector.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public AssemblyController(IEnumerable<string> args,
                                  Func<string, AssemblyServices, AssemblerBase> cpuSetHandler,
                                  Func<string, string, IBinaryFormatProvider> formatSelector)
        {
            if (args == null || cpuSetHandler == null || formatSelector == null)
                throw new ArgumentNullException();

            _services = new AssemblyServices(Options.FromArgs(args));
            _services.PassChanged += (s, a) => _services.Output.Reset();
            _services.PassChanged += (s, a) => _services.SymbolManager.Reset();
            _services.FormatSelector = formatSelector;
            _processorOptions = new ProcessorOptions
            {
                CaseSensitive = _services.Options.CaseSensitive,
                Log = _services.Log,
                IgnoreCommentColons = _services.Options.IgnoreColons,
                WarnOnLabelLeft = _services.Options.WarnLeft,
                InstructionLookup = symbol => _services.InstructionLookupRules.Any(ilr => ilr(symbol))
            };
            var cpu = _services.Options.CPU;
            if (_services.Options.InputFiles.Count > 0)
            {
                var src = new Preprocessor(_processorOptions).ProcessToFirstDirective(_services.Options.InputFiles[0]);
                if (src != null && src.Instruction != null && src.Instruction.Name.Equals(".cpu", _services.StringViewComparer))
                {
                    if (src.Operands.Count != 1 || !src.Operands[0].IsDoubleQuote())
                        _services.Log.LogEntry(src.Filename, src.LineNumber, src.Instruction.Position,
                            "Invalid expression for directive \".cpu\".");
                    else
                        cpu = src.Operands[0].Name.ToString().TrimOnce('"');
                }
            }
            _services.CPU = cpu;
            _assemblers = new List<AssemblerBase>
                {
                    new AssignmentAssembler(_services),
                    new BlockAssembler(_services),
                    new EncodingAssembler(_services),
                    new PseudoAssembler(_services),
                    new MiscAssembler(_services),
                    cpuSetHandler(cpu, _services)
                };
            _processorOptions.LineTerminates = _services.LineTerminates;
        }

        #endregion

        #region Methods

        string GetFullPath(string fileName)
        {
            string fullPath = fileName;
            var fileInfo = new FileInfo(fileName);
            if (!fileInfo.Exists)
            {
                if (!string.IsNullOrEmpty(_services.Options.IncludePath))
                {
                    fullPath = Path.Combine(_services.Options.IncludePath, fileName);
                    if (!File.Exists(fullPath))
                        throw new FileNotFoundException($"Source \"{fileInfo.FullName}\" not found.");
                }
                else
                {
                    throw new FileNotFoundException($"Source \"{fileInfo.FullName}\" not found.");
                }
            }
            return fullPath;
        }

        /// <summary>
        /// Begin the assembly process.
        /// </summary>
        /// <returns>The time in seconds for the assembly to complete.</returns>
        /// <exception cref="Exception"></exception>
        public double Assemble()
        {
            if (_services.Log.HasErrors)
            {
                _services.Log.DumpErrors();
                return double.NaN;
            }
            if (_services.Options.InputFiles.Count == 0)
                throw new Exception("One or more required input files was not specified.");

            var stopWatch = new Stopwatch();
            stopWatch.Start();

            // process cmd line args
            if (_services.Options.Quiet)
                Console.SetOut(TextWriter.Null);

            var preprocessor = new Preprocessor(_processorOptions);
            var processed = new List<SourceLine>();

            try
            {
                // preprocess all passed option defines and sections
                foreach (var define in _services.Options.LabelDefines)
                    processed.Add(preprocessor.ProcessDefine(define));
                foreach (var section in _services.Options.Sections)
                    processed.AddRange(preprocessor.Process(string.Empty, processed.Count, $".dsection {section}"));

                // preprocess all input files 
                foreach (var path in _services.Options.InputFiles)
                    processed.AddRange(preprocessor.Process(GetFullPath(path)));

                var anyonelikethis = processed.Any(l => (l.Label != null && l.Label.Line == null) ||
                                                        (l.Instruction != null && l.Instruction.Line == null) ||
                                                         l.Operands.Any(o => o.Line == null));

                Console.WriteLine($"{Assembler.AssemblerName}");
                Console.WriteLine($"{Assembler.AssemblerVersion}");

                var multiLineAssembler = new MultiLineAssembler()
                    .WithAssemblers(_assemblers)
                    .WithOptions(new MultiLineAssembler.Options
                    {
                        AllowReturn = false,
                        DisassembleAll = _services.Options.VerboseList,
                        ErrorHandler = AssemblyErrorHandler,
                        StopDisassembly = () => _services.PrintOff,
                        Evaluator = _services.Evaluator

                    });

                var disassembly = string.Empty;

                // while passes are needed
                while (_services.PassNeeded && !_services.Log.HasErrors)
                {
                    if (_services.DoNewPass() == 4)
                        throw new Exception("Too many passes attempted.");
                    _ = multiLineAssembler.AssembleLines(processed.GetIterator(), out disassembly);
                }
                if (!_services.Options.WarnNotUnusedSections && !_services.Log.HasErrors)
                {
                    var unused = _services.Output.UnusedSections;
                    if (unused.Count() > 0)
                    {
                        foreach (var section in unused)
                            _services.Log.LogEntry(null, 1, 1,
                                $"Section {section} was defined but never used.", false);
                    }
                }
                if (!_services.Options.NoWarnings && _services.Log.HasWarnings)
                    _services.Log.DumpWarnings();
                int byteCount = 0;
                if (_services.Log.HasErrors)
                {
                    _services.Log.DumpErrors();
                }
                else
                {
                    var passedArgs = _services.Options.GetPassedArgs();
                    var exec = Process.GetCurrentProcess().MainModule.ModuleName;
                    var inputFiles = string.Join("\n// ", preprocessor.GetInputFiles());
                    var fullDisasm = $"// {Assembler.AssemblerNameSimple}\n" +
                                     $"// {exec} {string.Join(' ', passedArgs)}\n" +
                                     $"// {DateTime.Now:f}\n\n// Input files:\n\n" +
                                     $"// {inputFiles}\n\n" + disassembly;
                    byteCount = WriteOutput(fullDisasm);
                }
                Console.WriteLine($"Number of errors: {_services.Log.ErrorCount}");
                Console.WriteLine($"Number of warnings: {_services.Log.WarningCount}");

                stopWatch.Stop();
                var ts = stopWatch.Elapsed.TotalSeconds;
                if (!_services.Log.HasErrors)
                {
                    var section = _services.Options.OutputSection;
                    if (!string.IsNullOrEmpty(section))
                        Console.Write($"[{section}] ");

                    Console.WriteLine($"{byteCount} bytes, {ts} sec.");
                    if (_services.Options.ShowChecksums)
                        Console.WriteLine($"Checksum: {_services.Output.GetOutputHash(section)}");
                    Console.WriteLine("*********************************");
                    Console.WriteLine("Assembly completed successfully.");
                }
                else
                {
                    _services.Log.ClearAll();
                }
                return ts;
            }
            catch (Exception ex)
            {
                _services.Log.LogEntrySimple(ex.Message);
                return double.NaN;
            }
            finally
            {
                if (_services.Log.HasErrors)
                    _services.Log.DumpErrors();
            }
        }

        bool AssemblyErrorHandler(AssemblerBase assembler, SourceLine line, AssemblyErrorReason reason, Exception ex)
        {
            switch (reason)
            {
                case AssemblyErrorReason.NotFound:
                    _services.Log.LogEntry(line, line.Instruction.Position,
                                           $"Unknown instruction \"{line.Instruction.Name}\".");
                    return true;
                case AssemblyErrorReason.ReturnNotAllowed:
                    _services.Log.LogEntry(line, line.Instruction.Position,
                                           "Directive \".return\" not valid outside of function block.");
                    return true;
                case AssemblyErrorReason.ExceptionRaised:
                    {
                        if (ex is SymbolException symbEx)
                        {
                            if (symbEx.SymbolToken != null)
                                _services.Log.LogEntry(symbEx.SymbolToken, symbEx.Message);
                            else
                                _services.Log.LogEntry(line, symbEx.Position, symbEx.Message);
                        }
                        else if (ex is SyntaxException synEx)
                        {
                            if (synEx.Token != null)
                                _services.Log.LogEntry(synEx.Token, synEx.Message);
                            else
                                _services.Log.LogEntry(line, synEx.Position, synEx.Message);
                        }
                        else if (ex is FormatException ||
                                 ex is ReturnException ||
                                 ex is BlockAssemblerException ||
                                 ex is SectionException)
                        {
                            if (ex is FormatException fmtEx)
                                _services.Log.LogEntry(line.Operands[0],
                                                  $"There was a problem with the format string:\n{fmtEx.Message}.",
                                                  true);
                            else if (ex is ReturnException retEx)
                                _services.Log.LogEntry(line, retEx.Position, retEx.Message);
                            else
                                _services.Log.LogEntry(line.Instruction, ex.Message);
                        }
                        else
                        {
                            if (_services.CurrentPass <= 0 || _services.PassNeeded)
                            {
                                if (assembler != null)
                                {
                                    var instructionSize = assembler.GetInstructionSize(line);
                                    if (_services.Output.AddressIsValid(instructionSize + _services.Output.LogicalPC))
                                        _services.Output.AddUninitialized(instructionSize);

                                }
                                _services.PassNeeded = true;
                                return true;
                            }
                            else
                            {
                                if (ex is ExpressionException expEx)
                                {
                                    if (ex is IllegalQuantityException illegalExp)
                                        _services.Log.LogEntry(line, illegalExp.Position,
                                            $"Illegal quantity for \"{line.Instruction.Name}\" ({illegalExp.Quantity}).");
                                    else
                                        _services.Log.LogEntry(line.Filename, line.LineNumber, expEx.Position, ex.Message);
                                }
                                else if (ex is ProgramOverflowException)
                                {
                                    _services.Log.LogEntry(line.Instruction, ex.Message);
                                }
                                else if (ex is InvalidPCAssignmentException pcEx)
                                {
                                    _services.Log.LogEntry(line.Instruction,
                                            $"Invalid Program Counter assignment {pcEx.Message} in expression.");
                                }
                                else
                                {
                                    _services.Log.LogEntry(line.Operands[0], ex.Message);
                                }
                            }
                        }
                    }
                    return false;
                default:
                    return true;
            }
        }

        int WriteOutput(string disassembly)
        {
            // no errors finish up
            // save to disk
            var section = _services.Options.OutputSection;
            var outputFile = _services.Options.OutputFile;
            var objectCode = _services.Output.GetCompilation(section);

            var formatProvider = _services.FormatSelector?.Invoke(_services.CPU, _services.OutputFormat);
            if (formatProvider != null)
            {
                var startAddress = _services.Output.ProgramStart;
                if (!string.IsNullOrEmpty(section))
                    startAddress = _services.Output.GetSectionStart(section);
                var format = _services.Options.CaseSensitive ?
                             _services.OutputFormat :
                             _services.OutputFormat.ToLower();
                var info = new FormatInfo(outputFile, format, startAddress, objectCode);
                File.WriteAllBytes(outputFile, formatProvider.GetFormat(info).ToArray());
            }
            else
            {
                File.WriteAllBytes(outputFile, objectCode.ToArray());
            }
            // write disassembly
            if (!string.IsNullOrEmpty(disassembly) && !string.IsNullOrEmpty(_services.Options.ListingFile))
                File.WriteAllText(_services.Options.ListingFile, disassembly);

            // write labels
            if (!string.IsNullOrEmpty(_services.Options.LabelFile))
                File.WriteAllText(_services.Options.LabelFile, _services.SymbolManager.ListLabels());

            Console.WriteLine("\n*********************************");
            Console.WriteLine($"Assembly start: ${_services.Output.ProgramStart:X4}");
            Console.WriteLine($"Assembly end:   ${_services.Output.ProgramEnd & BinaryOutput.MaxAddress:X4}");
            Console.WriteLine($"Passes: {_services.CurrentPass + 1}");
            return objectCode.Count;
        }
        #endregion
    }
}