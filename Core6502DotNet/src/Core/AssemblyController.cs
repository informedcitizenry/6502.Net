﻿//-----------------------------------------------------------------------------
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

        readonly List<AssemblerBase> _assemblers;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs an instance of a <see cref="AssemblyController"/>, which controls the 
        /// assembly process.
        /// </summary>
        public AssemblyController() => _assemblers = new List<AssemblerBase>();

        /// <summary>
        /// Constructs an instance of a <see cref="AssemblyController"/>, which controls the 
        /// assembly process.
        /// </summary>
        /// <param name="lineAssembler">A <see cref="AssemblerBase"/> object responsible for
        /// assembling directives not in the base assembler package.</param>
        public AssemblyController(AssemblerBase lineAssembler)
            : this() => _assemblers.Add(lineAssembler);

        /// <summary>
        /// Constructs an instance of a <see cref="AssemblyController"/>, which controls the 
        /// assembly process.
        /// </summary>
        /// <param name="lineAssemblers">An <see cref="AssemblerBase"/> collection
        /// responsible for assembling directives not in the base assembler package.</param>
        public AssemblyController(IEnumerable<AssemblerBase> lineAssemblers)
            : this() => _assemblers.AddRange(lineAssemblers);

        #endregion

        public void AssembleFromCommandLine()
        {
            // init all line assemblers
            var multiLineAssembler = new MultiLineAssembler();

            _assemblers.Add(multiLineAssembler);
            _assemblers.Add(new AssignmentAssembler());
            _assemblers.Add(new EncodingAssembler());
            _assemblers.Add(new PseudoAssembler());
            _assemblers.Add(new MiscAssembler());

            var run = true;
            while (run)
            {
                Console.Clear();
                Console.WriteLine("Enter valid assembly or type \"END\" to stop.");
                var srcBuilder = new StringBuilder();
                while (true)
                {
                    var inputted = Console.ReadLine();
                    if (inputted.Equals("END"))
                        break;
                    srcBuilder.AppendLine(inputted);
                }
                var preprocessor = new Preprocessor();
                IEnumerable<SourceLine> processed = preprocessor.Preprocess("test.a65", "test.a65", srcBuilder.ToString());
                Assembler.LineIterator = processed.Where(l => !string.IsNullOrWhiteSpace(l.UnparsedSource)).GetIterator();
                StringBuilder disassembly = null;
                while (Assembler.PassNeeded && !Assembler.Log.HasErrors)
                {
                    disassembly = new StringBuilder();
                    if (Assembler.CurrentPass++ == 4)
                    {
                        Console.WriteLine("Too many cooks!");
                        return;
                    }
                    var i = 0;
                    foreach (SourceLine line in Assembler.LineIterator)
                    {
                        try
                        {
                            if (!string.IsNullOrEmpty(line.ParsedSource))
                            {
                                var asm = _assemblers.FirstOrDefault(a => a.AssemblesLine(line));
                                if (asm == null && line.Instruction != null)
                                {
                                    Assembler.Log.LogEntry(line, line.Instruction.Position, $"Unknown instruction \"{line.InstructionName}\".");
                                }
                                else
                                {
                                    if (asm != null)
                                    {
                                        disassembly.AppendLine(asm.AssembleLine(line));
                                        Console.WriteLine($"{line.Assembly.ToString(0)} {line.UnparsedSource}");
                                        if (++i == 25)
                                        {
                                            Console.WriteLine("Press to continue...");
                                            Console.ReadKey();
                                            i = 0;
                                        }
                                    }
                                }
                            }
                            else if (Assembler.Options.VerboseList)
                            {
                                disassembly.AppendLine(line.UnparsedSource.PadLeft(50, ' '));
                            }
                        }
                        catch (SymbolException symbEx)
                        {
                            Assembler.Log.LogEntry(line, symbEx.Position, symbEx.Message);
                            //Console.WriteLine($"{line.Filename}({line.LineNumber}):{symbEx.Message}");
                        }
                        catch (ExpressionException ex)
                        {
                            var illegalQuant = ex is IllegalQuantityException;
                            if (illegalQuant)
                            {
                                Assembler.Log.LogEntry(line, ex.Position, $"Illegal quantity for \"{line.Instruction}\" in expression \"{line.Operand}\".");
                                continue;
                            }
                            if (!(ex is IllegalQuantityException) || Assembler.CurrentPass > 0)
                                Console.WriteLine($"{line.Filename}({line.LineNumber},{ex.Position}): {ex.Message}");
                        }
                        catch (Exception ex)
                        {
                            Assembler.Log.LogEntry(line, ex.Message, true);
                            Assembler.Log.LogEntry(line, ex.StackTrace, true);
                            //Console.WriteLine("OVAAAFLOWWAAA!!!!");
                        }
                    }
                    if (multiLineAssembler.InAnActiveBlock)
                    {
                        SourceLine activeLine = multiLineAssembler.ActiveBlockLine;
                        //Console.WriteLine($"Block \"{activeLine.InstructionName}\" missing closure.");
                        Assembler.Log.LogEntry(activeLine, $"Block \"{activeLine.InstructionName}\" missing closure.");
                    }
                    Assembler.LineIterator.Reset();
                }
                if (!Assembler.Log.HasErrors)
                {
                    if (!Assembler.Options.NoWarnings && Assembler.Log.HasWarnings)
                        Assembler.Log.DumpWarnings();
                    Console.WriteLine($"I sang this song in {Assembler.CurrentPass + 1} notes:");
                    Console.WriteLine(disassembly.ToString());
                    Console.WriteLine("Well that was fun, wasn't it? Go again? (Y)");
                    ConsoleKeyInfo k = Console.ReadKey();
                    Assembler.CurrentPass = -1;
                    Assembler.Output.Reset();
                    Assembler.PassNeeded = run = k.Key == ConsoleKey.Y;
                }
                else
                {
                    Assembler.Log.DumpAll();
                    run = false;
                }
            }
        }

        /// <summary>
        /// Begin the assembly process.
        /// </summary>
        /// <exception cref="OperationCanceledException"></exception>
        public void Assemble()
        {
            if (Assembler.Options.ArgsPassed == 0)
            {
                throw new OperationCanceledException("Arguments must be parsed before assembly can continue. " +
                    "Call Assembler.Initialize(string[] args) first.");
            }

            var stopWatch = new Stopwatch();
            stopWatch.Start();
            try
            {
                // process cmd line args
                if (Assembler.Options.PrintVersion || Assembler.Options.InputFiles.Count == 0)
                {
                    if (Assembler.Options.PrintVersion)
                    {
                        Console.WriteLine(Assembler.AssemblerNameSimple);
                        Console.WriteLine(Assembler.AssemblerVersion);
                    }
                    return;
                }
                if (Assembler.Options.Quiet)
                    Console.SetOut(TextWriter.Null);

                Console.WriteLine(Assembler.AssemblerName);
                Console.WriteLine(Assembler.AssemblerVersion);

                // init all line assemblers
                var multiLineAssembler = new MultiLineAssembler();

                _assemblers.Add(multiLineAssembler);
                _assemblers.Add(new AssignmentAssembler());
                _assemblers.Add(new EncodingAssembler());
                _assemblers.Add(new PseudoAssembler());
                _assemblers.Add(new MiscAssembler());


                // preprocess all input files 
                var preprocessor = new Preprocessor();
                var processed = new List<SourceLine>();

                // define all passed option defines 
                if (Assembler.Options.LabelDefines.Count > 0)
                {
                    foreach (var define in Assembler.Options.LabelDefines)
                        processed.AddRange(preprocessor.Preprocess(string.Empty, string.Empty, define));
                }
                foreach (var path in Assembler.Options.InputFiles)
                {
                    var srcFile = Path.GetFileName(path);
                    var srcPath = Path.GetFullPath(path);
                    processed.AddRange(preprocessor.Preprocess(srcPath, srcFile, File.ReadAllText(srcPath)));
                }

                // set the iterator
                Assembler.LineIterator = processed.GetIterator();

                var exec = Process.GetCurrentProcess().MainModule.ModuleName;
                var argsPassed = string.Join(' ', Assembler.Options.Arguments);
                var inputFiles = string.Join("\n// ", preprocessor.GetInputFiles());
                var disasmHeader = $"// {Assembler.AssemblerNameSimple}\n// {exec} {argsPassed}\n// {DateTime.Now:f}\n\n// Input files:" +
                                        $"\n\n// {inputFiles}\n\n";
                StringBuilder disassembly = null;
                // while passes are needed
                while (Assembler.PassNeeded && !Assembler.Log.HasErrors)
                {
                    if (Assembler.CurrentPass++ == 4)
                        throw new Exception("Too many passes attempted.");

                    if (!string.IsNullOrEmpty(Assembler.Options.ListingFile))
                        disassembly = new StringBuilder(disasmHeader);

                    foreach (SourceLine line in Assembler.LineIterator)
                    {
                        try
                        {
                            if (!string.IsNullOrEmpty(line.ParsedSource))
                            {
                                var asm = _assemblers.FirstOrDefault(a => a.AssemblesLine(line));
                                if (asm == null && line.Instruction != null)
                                {
                                    Assembler.Log.LogEntry(line,
                                                           line.Instruction.Position,
                                                           $"Unknown instruction \"{line.InstructionName}\".");
                                }
                                else if (Assembler.Output.PCOverflow)
                                {
                                    if (Assembler.CurrentPass == 0)
                                        Assembler.PassNeeded = true;
                                    else
                                        Assembler.Log.LogEntry(line, $"Program overflow: ${Assembler.Output.LogicalPC}");
                                }
                                else
                                {
                                    if (asm != null)
                                    {
                                        var disasm = asm.AssembleLine(line);
                                        if (!string.IsNullOrWhiteSpace(disasm) && !Assembler.PrintOff)
                                            disassembly.AppendLine(disasm);
                                    }
                                }
                            }
                            else if (Assembler.Options.VerboseList)
                            {
                                disassembly.AppendLine(line.UnparsedSource.PadLeft(50, ' '));
                            }
                        }
                        catch (Exception ex)
                        {
                            if (ex is SymbolException symbEx)
                            {
                                Assembler.Log.LogEntry(line, symbEx.Position, symbEx.Message, true);
                            }
                            else if (ex is FormatException fmtEx)
                            {
                                Assembler.Log.LogEntry(line,
                                                      line.Operand,
                                                      $"There was a problem with the format string:\n{fmtEx.Message}.",
                                                      true);
                            }
                            else if (Assembler.CurrentPass > 0)
                            {
                                if (ex is ExpressionException expEx)
                                {
                                    if (ex is IllegalQuantityException)
                                        Assembler.Log.LogEntry(line, expEx.Position,
                                            $"Illegal quantity for \"{line.Instruction}\" in expression \"{line.Operand}\".");
                                    else
                                        Assembler.Log.LogEntry(line, expEx.Position, ex.Message);
                                }
                                else if (ex is OverflowException)
                                {
                                    Assembler.Log.LogEntry(line, line.Operand.Position,
                                                $"Illegal quantity for \"{line.Instruction}\" in expression \"{line.Operand}\".");
                                }
                                else if (ex is InvalidPCAssignmentException pcEx)
                                {
                                    Assembler.Log.LogEntry(line, line.Instruction,
                                            $"Invalid Program Counter assignment in expression \"{line.Operand}\".");
                                }
                                else
                                {
                                    Assembler.Log.LogEntry(line, line.Operand.Position, ex.Message);
                                    Assembler.Log.LogEntry(line, ex.StackTrace);
                                }
                            }
                            else
                            {
                                Assembler.PassNeeded = true;
                            }
                        }
                    }
                    if (multiLineAssembler.InAnActiveBlock)
                    {
                        SourceLine activeLine = multiLineAssembler.ActiveBlockLine;
                        Assembler.Log.LogEntry(activeLine, activeLine.Instruction,
                            $"End of source file reached before finding block closure for \"{activeLine.InstructionName}\".");
                    }
                    Assembler.LineIterator.Reset();
                }
                if (!Assembler.Options.NoWarnings && Assembler.Log.HasWarnings)
                    Assembler.Log.DumpWarnings();

                if (Assembler.Log.HasErrors)
                    Assembler.Log.DumpErrors();
                else
                    WriteOutput(disassembly.ToString());

                Console.WriteLine($"Number of errors: {Assembler.Log.ErrorCount}");
                Console.WriteLine($"Number of warnings: {Assembler.Log.WarningCount}");

                if (!Assembler.Log.HasErrors)
                {
                    stopWatch.Stop();
                    var ts = stopWatch.Elapsed.TotalSeconds;

                    Console.WriteLine($"{Assembler.Output.GetCompilation().Count} bytes, {ts} sec.");
                    if (Assembler.Options.ShowChecksums)
                        Console.WriteLine($"Checksum: {Assembler.Output.GetOutputHash()}");
                    Console.WriteLine("*********************************");
                    Console.WriteLine("Assembly completed successfully.");
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
            }
        }

        static void WriteOutput(string disassembly)
        {
            // no errors finish up
            if (Assembler.Options.GenerateOutput)
            {
                // save to disk
                var outputFile = "a.out";
                if (!string.IsNullOrEmpty(Assembler.Options.OutputFile))
                    outputFile = Assembler.Options.OutputFile;

                var outputBytes = new List<byte>();

                // write header bytes
                if (Assembler.HeaderWriter != null)
                    outputBytes.AddRange(Assembler.HeaderWriter());

                outputBytes.AddRange(Assembler.Output.GetCompilation());

                File.WriteAllBytes(outputFile, outputBytes.ToArray());
            }
            // write disassembly
            if (disassembly != null)
                File.WriteAllText(Assembler.Options.ListingFile, disassembly);

            // write listings
            if (!string.IsNullOrEmpty(Assembler.Options.LabelFile))
                File.WriteAllText(Assembler.Options.LabelFile, Assembler.SymbolManager.ListLabels());

            Console.WriteLine("\n*********************************");
            Console.WriteLine($"Assembly start: ${Assembler.Output.ProgramStart:X4}");
            Console.WriteLine($"Assembly end:   ${Assembler.Output.ProgramEnd:X4}");
            Console.WriteLine($"Passes: {Assembler.CurrentPass + 1}");
        }
    }
}