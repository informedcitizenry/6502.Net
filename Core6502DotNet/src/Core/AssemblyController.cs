//-----------------------------------------------------------------------------
// Copyright (c) 2017-2021 informedcitizenry <informedcitizenry@gmail.com>
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
        readonly AssembleErrorHandler _errorHandler;
        readonly Func<string, AssemblyServices, CpuAssembler> _cpuSetHandler;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a new instance of an <see cref="AssemblyController"/>, which controls
        /// the assembly process.
        /// </summary>
        /// <param name="args">The commandline arguments.</param>
        /// <param name="cpuSetHandler">The <see cref="CpuAssembler"/> selection handler.</param>
        /// <param name="formatSelector">The format selector.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public AssemblyController(IEnumerable<string> args,
                                  Func<string, AssemblyServices, CpuAssembler> cpuSetHandler,
                                  Func<string, string, IBinaryFormatProvider> formatSelector)
        {
            if (args == null || cpuSetHandler == null || formatSelector == null)
                throw new ArgumentNullException(nameof(args), "One or more arguments was null.");
            _services = new AssemblyServices(Options.FromArgs(args));
            _services.PassChanged += (s, a) => _services.Output.Reset();
            _services.PassChanged += (s, a) => _services.SymbolManager.Reset();
            _services.FormatSelector = formatSelector;
            _errorHandler = new AssembleErrorHandler(_services);
            _processorOptions = new ProcessorOptions
            {
                CaseSensitive = _services.Options.CaseSensitive,
                Log = _services.Log,
                IncludePath = _services.Options.IncludePath,
                IgnoreCommentColons = _services.Options.IgnoreColons,
                WarnOnLabelLeft = _services.Options.WarnLeft,
                InstructionLookup = symbol => _services.InstructionLookupRules.Any(ilr => ilr(symbol)),
                IsMacroNameValid = symbol => !_assemblers.Any(asm => asm.Assembles(symbol))
            };
            _assemblers = new List<AssemblerBase>
            {
                new AssignmentAssembler(_services),
                new BlockAssembler(_services),
                new EncodingAssembler(_services),
                new PseudoAssembler(_services),
                new MiscAssembler(_services)
            };
            _cpuSetHandler = cpuSetHandler;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Begin the assembly process.
        /// </summary>
        /// <returns>The time in seconds for the assembly to complete.</returns>
        /// <exception cref="Exception"></exception>
        public double Assemble()
        {
            if (!(_services.Options.InputFiles?.Count >= 1))
                _services.Log.LogEntrySimple("One or more required input files was not specified.");
            else
                Initialize();

            var stopWatch = new Stopwatch();
            stopWatch.Start();

            if (_services.Options.Quiet)
                Console.SetOut(TextWriter.Null);

            var preprocessor = new Preprocessor(_processorOptions);
            var processed = new List<SourceLine>();
            try
            {
                if (!_services.Log.HasErrors)
                {
                    // preprocess all passed option defines and sections
                    if (_services.Options.LabelDefines != null)
                        foreach (var define in _services.Options.LabelDefines)
                            processed.Add(preprocessor.ProcessDefine(define));
                    if (_services.Options.Sections != null)
                        foreach (var section in _services.Options.Sections)
                            processed.AddRange(preprocessor.Process(string.Empty, processed.Count, $".dsection {section}"));

                    // preprocess all input files 
                    foreach (var inputFile in _services.Options.InputFiles)
                        processed.AddRange(preprocessor.Process(inputFile));
                }
                if (!_services.Options.NoStats)
                {
                    Console.WriteLine($"{Assembler.AssemblerName}");
                    Console.WriteLine($"{Assembler.AssemblerVersion}");
                }
                var multiLineAssembler = new MultiLineAssembler(_services.Options.VerboseList, () => _services.PrintOff)
                    .WithAssemblers(_assemblers);
                multiLineAssembler.AssemblyError += _errorHandler.HandleError;

                var disassembly = string.Empty;

                // while passes are needed
                while (_services.PassNeeded && !_services.Log.HasErrors)
                {
                    if (_services.DoNewPass() == 4)
                        return _services.Log.LogEntrySimple<double>("Too many passes attempted.");
                    disassembly = multiLineAssembler.AssembleLines(processed.GetIterator());
                    if (!_services.PassNeeded && _services.CurrentPass == 0)
                        _services.PassNeeded = _services.SymbolManager.SearchedNotFound;
                }
                if (!_services.Options.WarnNotUnusedSections && !_services.Log.HasErrors)
                {
                    var unused = _services.Output.UnusedSections;
                    if (unused.Any())
                    {
                        foreach (var section in unused)
                            _services.Log.LogEntrySimple($"Section {section} was defined but never used.", false);
                    }
                }
                var byteCount = 0;
                if (_services.Log.HasErrors)
                {
                    if (!_services.Options.NoWarnings && _services.Log.HasWarnings)
                        _services.Log.DumpWarnings();
                    _errorHandler.DumpErrors(false);
                }
                else
                {
                    var passedArgs = _services.Options.GetPassedArgs();
                    var inputFiles = string.Join("\n// ", preprocessor.GetInputFiles());
                    var fullDisasm = $"// {Assembler.ProductSummary}\n" +
                                     $"// Options: {string.Join(' ', passedArgs)}\n" +
                                     $"// {DateTime.Now:f}\n// Input files:\n\n" +
                                     $"// {inputFiles}\n\n" + disassembly;
                    byteCount = WriteOutput(fullDisasm);
                    if (!_services.Options.NoWarnings && _services.Log.HasWarnings)
                        _services.Log.DumpWarnings();
                    if (!_services.Options.NoStats)
                    {
                        Console.WriteLine("\n*********************************");
                        Console.WriteLine($"Assembly start: ${_services.Output.ProgramStart:X4}");
                        if (_services.Output.ProgramEnd > BinaryOutput.MaxAddress && _services.Options.LongAddressing)
                            Console.WriteLine($"Assembly end:   ${_services.Output.ProgramEnd:X6}");
                        else
                            Console.WriteLine($"Assembly end:   ${_services.Output.ProgramEnd & BinaryOutput.MaxAddress:X4}");
                        Console.WriteLine($"Passes: {_services.CurrentPass + 1}");
                    }
                }
                if (!_services.Options.NoStats)
                {   
                    Console.WriteLine($"Number of errors: {_services.Log.ErrorCount}");
                    Console.WriteLine($"Number of warnings: {_services.Log.WarningCount}");
                }
                stopWatch.Stop();
                var ts = stopWatch.Elapsed.TotalSeconds;
                if (!_services.Log.HasErrors && !_services.Options.NoStats)
                {
                    var section = _services.Options.OutputSection;
                    if (!string.IsNullOrEmpty(section))
                        Console.Write($"[{section}] ");

                    if (!string.IsNullOrEmpty(_services.Options.Patch))
                        Console.WriteLine($"{byteCount} (Offs:{_services.Options.Patch}), {ts} sec.");
                    else
                        Console.WriteLine($"{byteCount} bytes, {ts} sec.");
                    if (_services.Options.ShowChecksums)
                        Console.WriteLine($"Checksum: {_services.Output.GetOutputHash(section)}");
                    Console.WriteLine("*********************************");
                    Console.Write("Assembly completed successfully.");
                }
                _services.Log.ClearAll();
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
                    _errorHandler.DumpErrors(true);
            }
        }

        void Initialize()
        {
            CpuAssembler cpuAssembler = null;
            var cpu = _services.Options.CPU;
            if (!string.IsNullOrEmpty(cpu))
                cpuAssembler = _cpuSetHandler(cpu, _services);
            try
            {
                var src = new Preprocessor(_processorOptions).ProcessToFirstDirective(_services.Options.InputFiles.First());
                if (src != null && src.Instruction != null && src.Instruction.Name.Equals(".cpu", _services.StringViewComparer))
                    cpu = CpuAssembler.GetCpuName(src, _services);
            }
            catch (Exception ex)
            {
                _services.Log.LogEntrySimple(ex.Message);
            }
            _services.CPU = cpu;
            if (!string.IsNullOrEmpty(cpu) && cpuAssembler != null && !cpuAssembler.IsCpuValid(cpu))
            {
                _services.Log.LogEntrySimple($"Invalid CPU \"{cpu}\" specified.");
            }
            else
            {
                if (cpuAssembler == null)
                    cpuAssembler = _cpuSetHandler(cpu, _services);
                _assemblers.Add(cpuAssembler);
                _processorOptions.LineTerminates = _services.LineTerminates;
            }
        }

        int WriteOutput(string disassembly)
        {
            // no errors finish up
            // save to disk
            var section = _services.Options.OutputSection;
            var outputFile = _services.Options.OutputFile;
            var objectCode = _services.Output.GetCompilation(section);
            if (!string.IsNullOrEmpty(_services.Options.Patch))
            {
                if (!string.IsNullOrEmpty(_services.Options.Format))
                    _services.Log.LogEntrySimple("Format option ignored for patch mode.", false);
                try
                {
                    var offsetLine = new Preprocessor(_processorOptions).ProcessDefine("patch=" + _services.Options.Patch);
                    var patchExp = offsetLine.Operands.GetIterator();
                    var offset = _services.Evaluator.Evaluate(patchExp, 0, ushort.MaxValue);
                    if (patchExp.Current != null || _services.PassNeeded)
                        return _services.Log.LogEntrySimple<int>($"Patch offset specified in the expression \"{_services.Options.Patch}\" is not valid.");
                    var filePath = Preprocessor.GetFullPath(outputFile, _services.Options.IncludePath);
                    var fileBytes = File.ReadAllBytes(filePath);
                    Array.Copy(objectCode.ToArray(), 0, fileBytes, (int)offset, objectCode.Count);
                    File.WriteAllBytes(filePath, fileBytes);
                }
                catch
                {
                    return _services.Log.LogEntrySimple<int>($"Cannot patch file \"{outputFile}\". One or more arguments is not valid, or the file does not exist.");
                }
            }
            else
            {
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
            }   
            // write disassembly
            if (!string.IsNullOrEmpty(disassembly) && !string.IsNullOrEmpty(_services.Options.ListingFile))
                File.WriteAllText(_services.Options.ListingFile, disassembly);

            // write labels
            if (!string.IsNullOrEmpty(_services.Options.LabelFile))
                File.WriteAllText(_services.Options.LabelFile, _services.SymbolManager.ListLabels(!_services.Options.LabelsAddressesOnly));
            else if (_services.Options.LabelsAddressesOnly && string.IsNullOrEmpty(_services.Options.LabelFile))
                _services.Log.LogEntrySimple("Label listing not specified; option '-labels-addresses-only' ignored.",
                    false);
            return objectCode.Count;
        }
        #endregion
    }
}