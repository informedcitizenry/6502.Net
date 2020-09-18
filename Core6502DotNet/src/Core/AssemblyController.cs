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
using System.Text;

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
        readonly Func<string, AssemblyServices, AssemblerBase> _cpuSetHandler;

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
                                  Func<string, AssemblyServices, IBinaryFormatProvider> formatSelector)
        {
            if (args == null || cpuSetHandler == null || formatSelector == null)
                throw new ArgumentNullException();

            _services = new AssemblyServices(Options.FromArgs(args));
            _services.PassChanged += OnPassChanged;
            _services.FormatSelector = formatSelector;
            _services.CPUAssemblerSelector = SetCpuAssembler;
            _cpuSetHandler = cpuSetHandler;
            _assemblers = new List<AssemblerBase>
            {
                new AssignmentAssembler(_services),
                new EncodingAssembler(_services),
                new PseudoAssembler(_services),
                new MiscAssembler(_services)
            };
        }

        #endregion

        #region Methods

        void SetCpuAssembler(string cpu) 
            => _assemblers.Add(_cpuSetHandler(cpu, _services));

        /// <summary>
        /// Begin the assembly process.
        /// </summary>
        /// <returns>The time in seconds for the assembly to complete.</returns>
        /// <exception cref="Exception"></exception>
        public double Assemble()
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            // process cmd line args
            if (_services.Options.Quiet)
                Console.SetOut(TextWriter.Null);

            var preprocessor = new Preprocessor(_services);
            var processed = new List<SourceLine>();

            // preprocess all passed option defines and sections
            foreach (var define in _services.Options.LabelDefines)
                processed.Add(preprocessor.PreprocessDefine(define));
            foreach (var section in _services.Options.Sections)
                processed.AddRange(LexerParser.Parse(string.Empty, $".dsection {section}", _services, true));

            if (_services.Options.InputFiles.Count == 0)
                throw new Exception("One or more required input files was not specified.");

            // preprocess all input files 
            foreach (var path in _services.Options.InputFiles)
                processed.AddRange(preprocessor.PreprocessFile(path));

            Console.WriteLine($"{Assembler.AssemblerName}");
            Console.WriteLine($"{Assembler.AssemblerVersion}");

            // set the iterator
            var iterator = processed.TakeWhile(l => !l.InstructionName.Equals(".end"))
                                    .GetIterator();

            _services.SymbolManager.LineIterator = iterator;

            // add the block assembler.
            _assemblers.Add(new BlockAssembler(_services, iterator));

            var disassembly = new StringBuilder();

            // while passes are needed
            while (_services.PassNeeded && !_services.Log.HasErrors)
            {
                if (_services.DoNewPass() == 4)
                    throw new Exception("Too many passes attempted.");
                disassembly.Clear();
                _ = MultiLineAssembler.AssembleLines(iterator,
                                                     _assemblers,
                                                     false,
                                                     disassembly,
                                                     _services.Options.VerboseList,
                                                     AssemblyErrorHandler,
                                                     _services);
            }
            var unused = _services.Output.UnusedSections;
            if (unused.Count() > 0)
            {
                foreach (var section in unused)
                    _services.Log.LogEntry(null, 1, 1, $"Section {section} was defined but never used.", false);
            }
            if (!_services.Options.NoWarnings && _services.Log.HasWarnings)
                _services.Log.DumpWarnings();

            if (_services.Log.HasErrors)
            {
                _services.Log.DumpErrors();
            }
            else
            {
                var passedArgs = _services.Options.GetPassedArgs();
                var exec = Process.GetCurrentProcess().MainModule.ModuleName;
                var inputFiles = string.Join("\n// ", preprocessor.GetInputFiles());
                var disasmHeader = $"// {Assembler.AssemblerNameSimple}\n" +
                                   $"// {exec} {string.Join(' ', passedArgs)}\n" +
                                   $"// {DateTime.Now:f}\n\n// Input files:\n\n" +
                                   $"// {inputFiles}\n\n";
                disassembly.Insert(0, disasmHeader);
                WriteOutput(disassembly.ToString());
            }
            Console.WriteLine($"Number of errors: {_services.Log.ErrorCount}");
            Console.WriteLine($"Number of warnings: {_services.Log.WarningCount}");

            stopWatch.Stop();
            var ts = stopWatch.Elapsed.TotalSeconds;
            if (!_services.Log.HasErrors)
            {
                Console.WriteLine($"{_services.Output.GetCompilation().Count} bytes, {ts} sec.");
                if (_services.Options.ShowChecksums)
                    Console.WriteLine($"Checksum: {_services.Output.GetOutputHash()}");
                Console.WriteLine("*********************************");
                Console.WriteLine("Assembly completed successfully.");
            }
            return ts;
        }

        bool AssemblyErrorHandler(SourceLine line, AssemblyErrorReason reason, Exception ex)
        {
            switch (reason)
            {
                case AssemblyErrorReason.NotFound:
                    _services.Log.LogEntry(line,
                                           line.Instruction.Position,
                                           $"Unknown instruction \"{line.InstructionName}\".");
                    return true;
                case AssemblyErrorReason.ReturnNotAllowed:
                    _services.Log.LogEntry(line, 
                                           line.Instruction.Position,
                                           "Directive \".return\" not valid outside of function block.");
                    return true;
                case AssemblyErrorReason.ExceptionRaised:
                    {
                        if (ex is SymbolException symbEx)
                        {
                            _services.Log.LogEntry(line, symbEx.Position, symbEx.Message, true);
                        }
                        else if (ex is SyntaxException synEx)
                        {
                            _services.Log.LogEntry(line, synEx.Position, synEx.Message, true);
                        }
                        else if (ex is FormatException ||
                                 ex is ReturnException ||
                                 ex is BlockAssemblerException ||
                                 ex is SectionException)
                        {
                            if (ex is FormatException fmtEx)
                                _services.Log.LogEntry(line,
                                                  line.Operand,
                                                  $"There was a problem with the format string:\n{fmtEx.Message}.",
                                                  true);
                            else if (ex is ReturnException retEx)
                                _services.Log.LogEntry(line, retEx.Position, retEx.Message);
                            else
                                _services.Log.LogEntry(line, line.Instruction.Position, ex.Message);
                        }
                        else
                        {
                            if (_services.CurrentPass <= 0 || _services.PassNeeded)
                            {
                                if (ex is IllegalQuantityException)
                                    _services.Output.AddUninitialized(2);
                                _services.PassNeeded = true;
                                return true;
                            }
                            else
                            {
                                if (ex is ExpressionException expEx)
                                {
                                    if (ex is IllegalQuantityException illegalExp)
                                        _services.Log.LogEntry(line, illegalExp.Position,
                                            $"Illegal quantity for \"{line.InstructionName}\" in expression \"{line.Operand.ToString().Trim()}\" ({illegalExp.Quantity}).");
                                    else
                                        _services.Log.LogEntry(line, expEx.Position, ex.Message);
                                }
                                else if (ex is ProgramOverflowException)
                                {
                                    _services.Log.LogEntry(line, line.Instruction.Position,
                                              ex.Message);
                                }
                                else if (ex is InvalidPCAssignmentException pcEx)
                                {
                                    _services.Log.LogEntry(line, line.Instruction,
                                            $"Invalid Program Counter assignment {pcEx.Message} in expression \"{line.Operand.ToString().Trim()}\".");
                                }
                                else
                                {
                                    _services.Log.LogEntry(line, line.Operand.Position, ex.Message);
                                }
                            }
                        }  
                    }
                    return false;
                default:
                    return true;
            }
        }

        void OnPassChanged(object sender, EventArgs args) => _services.Output.Reset();

        void WriteOutput(string disassembly)
        {
            // no errors finish up
            // save to disk
            var outputFile = _services.Options.OutputFile;
            var formatProvider = _services.FormatSelector?.Invoke(_services.OutputFormat, _services);
            if (formatProvider != null)
                File.WriteAllBytes(outputFile, formatProvider.GetFormat().ToArray());
            else
                File.WriteAllBytes(outputFile, _services.Output.GetCompilation().ToArray());

            // write disassembly
            if (!string.IsNullOrEmpty(disassembly) && !string.IsNullOrEmpty(_services.Options.ListingFile))
                File.WriteAllText(_services.Options.ListingFile, disassembly);

            // write listings
            if (!string.IsNullOrEmpty(_services.Options.LabelFile))
                File.WriteAllText(_services.Options.LabelFile, _services.SymbolManager.ListLabels());

            Console.WriteLine("\n*********************************");
            Console.WriteLine($"Assembly start: ${_services.Output.ProgramStart:X4}");
            Console.WriteLine($"Assembly end:   ${_services.Output.ProgramEnd & BinaryOutput.MaxAddress:X4}");
            Console.WriteLine($"Passes: {_services.CurrentPass + 1}");
        }
        #endregion
    }
}
