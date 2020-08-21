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

        readonly List<AssemblerBase> _assemblers;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs an instance of a <see cref="AssemblyController"/>, which controls the
        /// assembly process.
        /// </summary>
        public AssemblyController()
        {
            Assembler.Initialize();
            _assemblers = new List<AssemblerBase>();
        }


        #endregion

        #region Methods

        /// <summary>
        /// Adds an <see cref="AssemblerBase"/> to the collection of assemblers that control
        /// assembly.
        /// </summary>
        /// <param name="lineAssembler">An <see cref="AssemblerBase"/> responsible for assembling 
        /// directives not in the base assembler package.</param>
        public void AddAssembler(AssemblerBase lineAssembler)
            => _assemblers.Add(lineAssembler);


        /// <summary>
        /// Begin the assembly process.
        /// </summary>
        /// <exception cref="Exception"></exception>
        public void Assemble()
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            try
            {
                // process cmd line args
                if (Assembler.Options.Quiet)
                    Console.SetOut(TextWriter.Null);

                // init all line assemblers
                var blockAssembler = new BlockAssembler();

                _assemblers.Add(blockAssembler);
                _assemblers.Add(new AssignmentAssembler());
                _assemblers.Add(new EncodingAssembler());
                _assemblers.Add(new PseudoAssembler());
                _assemblers.Add(new MiscAssembler());
     
                var preprocessor = new Preprocessor();
                var processed = new List<SourceLine>();

                // preprocess all passed option defines and sections
                foreach (var define in Assembler.Options.LabelDefines)
                    processed.Add(Preprocessor.PreprocessDefine(define));
                foreach (var section in Assembler.Options.Sections)
                    processed.AddRange(LexerParser.Parse(string.Empty, $".dsection {section}"));

                if (Assembler.Options.InputFiles.Count == 0)
                    throw new Exception("One or more required input files was not specified.");

                // preprocess all input files 
                foreach (var path in Assembler.Options.InputFiles)
                    processed.AddRange(preprocessor.PreprocessFile(path));

                Console.WriteLine(Assembler.AssemblerName);
                Console.WriteLine(Assembler.AssemblerVersion);

                // hunt for the first ".end"
                var endIx = processed.FindIndex(l => l.InstructionName.Equals(".end"));
                if (endIx < 0)
                    endIx = processed.Count;

                // set the iterator
                Assembler.LineIterator = processed.GetRange(0, endIx).GetIterator();

                var exec = Process.GetCurrentProcess().MainModule.ModuleName;
                var argsPassed = string.Join(' ', Environment.GetCommandLineArgs().Skip(1));
                var inputFiles = string.Join("\n// ", preprocessor.GetInputFiles());
                var disasmHeader = $"// {Assembler.AssemblerNameSimple}\n// {exec} {argsPassed}\n// {DateTime.Now:f}\n\n// Input files:" +
                                        $"\n\n// {inputFiles}\n\n";
                StringBuilder disassembly = null;
                // while passes are needed
                while (Assembler.PassNeeded && !Assembler.Log.HasErrors)
                {
                    if (Assembler.IncrementPass() == 4)
                        throw new Exception("Too many passes attempted.");
                    disassembly = new StringBuilder(disasmHeader);

                    _ = MultiLineAssembler.AssembleLines(Assembler.LineIterator,
                                                         _assemblers,
                                                         -1,
                                                         -1,
                                                         false,
                                                         disassembly,
                                                         Assembler.Options.VerboseList,
                                                         AssemblyErrorHandler);
                    if (blockAssembler.InAnActiveBlock)
                    {
                        SourceLine activeLine = blockAssembler.ActiveBlockLine;
                        Assembler.Log.LogEntry(activeLine, activeLine.Instruction,
                            $"End of source file reached before finding block closure for \"{activeLine.InstructionName}\".");
                    }
                    Assembler.LineIterator.Reset();
                }
                var unused = Assembler.Output.UnusedSections;
                if (unused.Count() > 0)
                {
                    foreach (var section in unused)
                        Assembler.Log.LogEntry(null, 1, 1, $"Section {section} was defined but never used.", false);
                }
                if (!Assembler.Options.NoWarnings && Assembler.Log.HasWarnings)
                    Assembler.Log.DumpWarnings();

                if (Assembler.Log.HasErrors)
                    Assembler.Log.DumpErrors();
                else
                    WriteOutput(disassembly?.ToString());

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

        bool AssemblyErrorHandler(SourceLine line, AssemblyErrorReason reason, Exception ex)
        {
            switch (reason)
            {
                case AssemblyErrorReason.NotFound:
                    Assembler.Log.LogEntry(line,
                                           line.Instruction.Position,
                                           $"Unknown instruction \"{line.InstructionName}\".");
                    return true;
                case AssemblyErrorReason.ReturnNotAllowed:
                    Assembler.Log.LogEntry(line, 
                                           line.Instruction.Position,
                                           "Directive \".return\" not valid outside of function block.");
                    return true;
                case AssemblyErrorReason.ExceptionRaised:
                    {
                        if (ex is SymbolException symbEx)
                        {
                            Assembler.Log.LogEntry(line, symbEx.Position, symbEx.Message, true);
                        }
                        else if (ex is SyntaxException synEx)
                        {
                            Assembler.Log.LogEntry(line, synEx.Position, synEx.Message, true);
                        }
                        else if (ex is FormatException ||
                                 ex is ReturnException ||
                                 ex is BlockAssemblerException ||
                                 ex is SectionException)
                        {
                            if (ex is FormatException fmtEx)
                                Assembler.Log.LogEntry(line,
                                                  line.Operand,
                                                  $"There was a problem with the format string:\n{fmtEx.Message}.",
                                                  true);
                            else if (ex is ReturnException retEx)
                                Assembler.Log.LogEntry(line, retEx.Position, retEx.Message);
                            else
                                Assembler.Log.LogEntry(line, line.Instruction.Position, ex.Message);

                            return false;
                        }
                        else
                        {
                            if (Assembler.CurrentPass > 0 && !Assembler.PassNeeded)
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
                                            $"Invalid Program Counter assignment {pcEx} in expression \"{line.Operand}\".");
                                }
                                else
                                {
                                    Assembler.Log.LogEntry(line, line.Operand.Position, ex.Message);
                                }
                            }
                            else
                            {
                                Assembler.PassNeeded = true;
                            }
                        }  
                    }
                    return true;
                default:
                    return true;
            }
        }

        static void WriteOutput(string disassembly)
        {
            // no errors finish up
            // save to disk
            var outputFile = Assembler.Options.OutputFile;
            Assembler.SelectFormat(Assembler.OutputFormat);
            if (Assembler.BinaryFormatProvider != null)
                File.WriteAllBytes(outputFile, Assembler.BinaryFormatProvider.GetFormat().ToArray());
            else
                File.WriteAllBytes(outputFile, Assembler.Output.GetCompilation().ToArray());
            // write disassembly
            if (disassembly != null && !string.IsNullOrEmpty(Assembler.Options.ListingFile))
                File.WriteAllText(Assembler.Options.ListingFile, disassembly);

            // write listings
            if (!string.IsNullOrEmpty(Assembler.Options.LabelFile))
                File.WriteAllText(Assembler.Options.LabelFile, Assembler.SymbolManager.ListLabels());

            Console.WriteLine("\n*********************************");
            Console.WriteLine($"Assembly start: ${Assembler.Output.ProgramStart:X4}");
            Console.WriteLine($"Assembly end:   ${Assembler.Output.ProgramEnd:X4}");
            Console.WriteLine($"Passes: {Assembler.CurrentPass + 1}");
        }
        #endregion
    }
}
