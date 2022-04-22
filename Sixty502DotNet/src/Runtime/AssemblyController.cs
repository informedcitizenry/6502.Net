//-----------------------------------------------------------------------------
// Copyright (c) 2017-2022 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
namespace Sixty502DotNet
{
    /// <summary>
    /// A class that controls the overall assembly process, from parsing to
    /// code generation to output.
    /// </summary>
    public class AssemblyController
    {
        private readonly AssemblyServices _services;
        private CodeGenVisitor? _codeGenVisitor;
        private int _patchAddress;
        private SortedSet<string>? _inputFiles;
        private IReadOnlyCollection<Token>? _unreferencedMacros;
        private string? _preprocessedSource;

        /// <summary>
        /// Construct a new instance of the <see cref="AssemblyController"/>
        /// class.
        /// </summary>
        /// <param name="args">The command-line arguments setting the
        /// assembly options.</param>
        public AssemblyController(string[] args)
            => _services = new AssemblyServices(Options.FromArgs(args));

        private Sixty502DotNetParser ParseOption(string code)
        {
            var lexer = new Sixty502DotNetLexer(CharStreams.fromString(code))
            {
                TokenFactory = new TokenFactory("Option error(s)")
            };
            var parser = new Sixty502DotNetParser(new CommonTokenStream(lexer));
            parser.RemoveErrorListeners();
            parser.AddErrorListener(_services.Log);
            return parser;
        }

        private bool ProcessDefines()
        {
            if (_services.Options.Defines == null)
            {
                return true;
            }
            foreach (var define in _services.Options.Defines)
            {
                var parser = ParseOption(define);
                var defineExprs = parser.expressionList().expr();
                foreach (var defineExpr in defineExprs)
                {
                    // defineExpr: Ident ('=' expr)?;
                    var symName = defineExpr.refExpr()?.identifier()?.name?.Text;
                    if (!string.IsNullOrEmpty(symName))
                    {
                        _services.Symbols.GlobalScope.Define(symName, new Constant(symName, new Value(1)));
                        continue;
                    }
                    var assignExpr = defineExpr.assignExpr();
                    if (assignExpr == null ||
                        assignExpr.identifier()?.name == null ||
                        assignExpr.assignOp()?.Equal() == null)
                    {
                        _services.Log.LogEntry(defineExpr, "Invalid constant definition.");
                        return false;
                    }
                    try
                    {
                        if (!_services.ExpressionVisitor.TryGetPrimaryExpression(assignExpr.expr(), out var symValue))
                        {
                            return false;
                        }
                        if (!symValue.IsDefined)
                        {
                            _services.Log.LogEntry(defineExpr, "Invalid constant definition.");
                            return false;
                        }
                        symName = assignExpr.identifier().name.Text;
                        var defSymbol = new Constant(symName, symValue);
                        _services.Symbols.GlobalScope.Define(symName, defSymbol);
                    }
                    catch (Exception ex)
                    {
                        _services.Log.LogEntry(assignExpr.expr(), ex.Message);
                        return false;
                    }
                }
            }
            return !_services.Log.HasErrors;
        }

        private bool ProcessSections()
        {
            if (_services.Options.Sections == null)
                return true;
            foreach (var section in _services.Options.Sections)
            {
                // section: expr ',' expr (',' expr)? ;
                var parser = ParseOption(section);
                var sectionParse = parser.expressionList();
                var expressions = sectionParse.expr();
                if (!SectionDefiner.Define(sectionParse, expressions, _services))
                {
                    return false;
                }
            }
            return true;
        }

        private void GetPatchAddress()
        {
            if (!string.IsNullOrEmpty(_services.Options.Patch))
            {
                if (_services.Options.PreprocessOnly)
                {
                    _services.Log.LogEntrySimple("Option '-E' causes option '--patch' to be ignored.", false);
                    return;
                }
                var parser = ParseOption(_services.Options.Patch);
                var patchParse = parser.expr();
                if (!_services.Log.HasErrors)
                {
                    if (!_services.ExpressionVisitor.TryGetPrimaryExpression(patchParse, out var patchVal))
                    {
                        return;
                    }
                    if (patchVal.IsDefined)
                    {
                        if (patchVal.IsIntegral)
                        {
                            _patchAddress = patchVal.ToInt();
                            if (_patchAddress >= Int24.MinValue && _patchAddress <= UInt24.MaxValue)
                            {
                                _patchAddress &= 0xffffff;
                                return;
                            }
                        }
                    }
                }
                _services.Log.LogEntry(patchParse, "Patch address specified in '--patch' option invalid.");
            }
        }

        private void SetOutputFormatFromOptions()
        {
            if (_services.Options.PreprocessOnly && !string.IsNullOrEmpty(_services.Options.Format))
            {
                _services.Log.LogEntrySimple("Option '-E' causes option '--format' to be ignored.", false);
                return;
            }
            try
            {
                _services.Output.OutputFormat = OutputFormatSelector.DefaultProvider;
                if (!string.IsNullOrEmpty(_services.Options.Format) ||
                    !string.IsNullOrEmpty(_services.Options.CPU))
                {
                    _services.Output.OutputFormat =
                        OutputFormatSelector.Select(_services.Options.Format ?? "",
                                                          _services.Options.CPU ?? "");
                }
            }
            catch (Error err)
            {
                _services.Log.LogEntrySimple(err.Message);
            }
        }

        private void ProcessOptions()
        {
            if (_services.Options.Quiet)
            {
                Console.SetOut(TextWriter.Null);
            }
            GetPatchAddress();
            if (!_services.Log.HasErrors &&
                ProcessDefines() && ProcessSections())
            {
                SetOutputFormatFromOptions();
                if (_services.Options.IgnoreColons)
                {
                    _services.Log.LogEntrySimple(
                        "Option '--ignore-colons' is deprecated since colons no longer terminate semi-colon comments.",
                        false);
                }
                if (_services.Options.BranchAlways &&
                    !(string.IsNullOrEmpty(_services.Options.CPU) ||
                    _services.Options.CPU.StartsWith("6502")))
                {
                    _services.Log.LogEntrySimple("Option '--enable-branch-always' ignored; target CPU is not a 6502.", false);
                }
            }
            if (_services.Options.PreprocessOnly &&
                (!string.IsNullOrEmpty(_services.Options.OutputFile) || !string.IsNullOrEmpty(_services.Options.OutputSection)))
            {
                _services.Log.LogEntrySimple("Option '-E' causes all other output options to be ignored.", false);
            }
        }

        private Sixty502DotNetParser.SourceContext? ParseSource()
        {
            try
            {
                var preprocessor = new Preprocessor(_services);
                var lexer = preprocessor.Lexer;
                if (lexer == null)
                {
                    // this could happen if the source input file was not found.
                    return null;
                }
                var stream = new CommonTokenStream(lexer);
                _preprocessedSource = stream.GetText();
                _inputFiles = preprocessor.InputFilesProcessed;
                if (_services.Options.PreprocessOnly)
                {
                    return null;
                }
                if (!_services.Log.HasErrors)
                {
                    var parser = new Sixty502DotNetParser(stream)
                    {
                        Symbols = _services.Symbols
                    };
                    parser.Interpreter.PredictionMode = PredictionMode.SLL;
                    parser.RemoveErrorListeners();
                    parser.AddErrorListener(_services.Log);
                    var parse = parser.source();
                    // parse tree first before constructing code gen visitor to ensure the lexer's
                    // instruction set will be initialized to the correct one (this happens during lexical phase).
                    _codeGenVisitor = new CodeGenVisitor(_services, lexer.InstructionSet!);
                    _inputFiles = preprocessor.InputFilesProcessed;
                    _unreferencedMacros = preprocessor.GetUnreferencedMacroTokens();
                    if (_services.Options.WarnLeft)
                    {
                        foreach (var label in parser.LabelsAfterWhitespace)
                        {
                            _services.Log.LogEntry((Token)label, "Whitespace precedes label.", false);
                        }
                    }
                    return parse;
                }
                return null;
            }
            catch (Exception ex)
            {
                _services.Log.LogEntrySimple(ex.Message);
                return null;
            }
        }

        private void DoPass(Sixty502DotNetParser.SourceContext parse)
        {
            var currentPassVar = _services.Symbols.GlobalScope.Resolve("CURRENT_PASS") as Constant;
            currentPassVar!.Value.SetAs(new Value(_services.State.CurrentPass + 1));
            _codeGenVisitor?.Reset();
            _services.Output.Reset();
            _services.StatementListings.Clear();
            _services.LabelListing.Clear();
            _services.Symbols.Reset();
            _services.State.PassNeeded = false;
            _ = _codeGenVisitor?.Visit(parse);
        }

        private string GetListingHeader()
        {
            var passedArgs = _services.Options.GetPassedArgs();
            var inputFiles = string.Join("\n// ", _inputFiles!);
            return $"// {Assembler.ProductSummary}\n" +
                   $"// Options: {string.Join(' ', passedArgs!)}\n" +
                   $"// Build time (UTC): {DateTime.Now.ToUniversalTime():s}\n// Input files:\n\n" +
                   $"// {inputFiles}\n\n";
        }

        private void WriteListing()
        {
            if (!string.IsNullOrEmpty(_services.Options.ListingFile) && _services.StatementListings.Count > 0)
            {
                var listing = string.Join(Environment.NewLine, _services.StatementListings);
                var fullDisasm = $"{GetListingHeader()}{listing}";
                File.WriteAllText(_services.Options.ListingFile, fullDisasm);
            }
            if (!string.IsNullOrEmpty(_services.Options.LabelFile))
            {
                File.WriteAllText(_services.Options.LabelFile, _services.LabelListing.ToString());
            }
            else if (_services.Options.LabelsAddressesOnly && string.IsNullOrEmpty(_services.Options.LabelFile))
            {
                _services.Log.LogEntrySimple("Label listing filename not specified; option '-labels-addresses-only' ignored.",
                   false);
            }
        }

        private void CheckUnreferencedSymbols()
        {
            if (_services.Options.WarnUnreferencedSymbols && _unreferencedMacros != null)
            {
                foreach (var macroToken in _unreferencedMacros)
                {
                    _services.Log.LogEntry(macroToken, $"Macro \"{macroToken.Text}\" is defined but never referenced.", false);
                }
                var symbols = _services.Symbols.GetUnreferencedSymbols();
                foreach (var sym in symbols)
                {
                    if (sym.Token != null && sym.Scope is not Enum)
                    {
                        _services.Log.LogEntry(sym.Token, $"Symbol \"{sym.Name}\" is defined but never referenced.", false);
                    }
                }
            }
        }

        private void WriteOutput()
        {
            var section = _services.Options.OutputSection ?? "";
            var outputFile = _services.Options.OutputFile ?? "a.out";
            var codeGenBytes = _services.Output.GetCompilation(outputFile, section);
            if (_patchAddress > 0)
            {
                if (_services.Output.OutputFormat != null)
                    _services.Log.LogEntrySimple("Option '--format' ignored when '--patch' option is specified.", false);
                var filePath = FileHelper.GetPath(outputFile, _services.Options.IncludePath);
                if (string.IsNullOrEmpty(filePath))
                {
                    _services.Log.LogEntrySimple($"Cannot patch file \"{outputFile}\". The file could not be found.");
                    return;
                }
                else
                {
                    var fileBytes = File.ReadAllBytes(filePath);
                    Array.Copy(codeGenBytes.ToArray(), 0, fileBytes, _patchAddress, codeGenBytes.Count);
                    File.WriteAllBytes(filePath, fileBytes);
                }
            }
            else
            {
                File.WriteAllBytes(outputFile, codeGenBytes.ToArray());
            }
            WriteListing();
        }

        private void DumpLog()
        {
            _services.Log.Dump(!_services.Options.NoWarnings, _services.Options.NoHighlighting);
            if (!string.IsNullOrEmpty(_services.Options.ErrorFile))
            {
                using FileStream fs = new(_services.Options.ErrorFile, FileMode.Create);
                using StreamWriter writer = new(fs);
                writer.WriteLine($"{Assembler.AssemblerNameSimple}");
                writer.WriteLine($"Error file generated (UTC): {DateTime.Now.ToUniversalTime():s}");
                writer.WriteLine($"{_services.Log.ErrorCount} error(s):\n");
                _services.Log.Dump(!_services.Options.NoWarnings, _services.Options.NoHighlighting, writer);
            }
        }

        private void PrintStatus(double time)
        {
            if (!_services.Options.NoStats)
            {
                DumpLog();
                if (_services.Log.ErrorCount == 0)
                {
                    Console.WriteLine("*********************************");
                    Console.WriteLine($"Assembly start: ${_services.Output.ProgramStart:X4}");
                    if (_services.Output.ProgramEnd > CodeOutput.MaxAddress && _services.Options.LongAddressing)
                        Console.WriteLine($"Assembly end:   ${_services.Output.ProgramEnd:X6}");
                    else
                        Console.WriteLine($"Assembly end:   ${_services.Output.ProgramEnd & CodeOutput.MaxAddress:X4}");
                    Console.WriteLine($"Passes: {_services.State.CurrentPass}");
                    var bytesWritten = _services.Output.ProgramEnd - _services.Output.ProgramStart;
                    var section = _services.Options.OutputSection;
                    if (!string.IsNullOrEmpty(section))
                        Console.Write($"[{section}] ");
                    if (!string.IsNullOrEmpty(_services.Options.Patch))
                        Console.WriteLine($"{bytesWritten} (Offs:{_services.Options.Patch}), {time} sec.");
                    else
                        Console.WriteLine($"{bytesWritten} bytes.");
                    if (_services.Options.ShowChecksums)
                        Console.WriteLine($"Checksum: {_services.Output.GetOutputHash(section)}");
                    Console.WriteLine("*********************************");
                    Console.WriteLine("Assembly completed successfully.");
                }
            }
        }

        /// <summary>
        /// Profile the generated parser.
        /// </summary>
        public void ProfileParser()
        {
            var stopwatch = new Stopwatch();
            ProcessOptions();
            if (_services.Log.HasErrors)
            {
                _services.Log.DumpErrors(_services.Options.NoHighlighting);
                return;
            }
            var preprocessor = new Preprocessor(_services);
            var lexer = preprocessor.Lexer;
            var stream = new CommonTokenStream(lexer);
            var text = stream.GetText();
            var files = preprocessor.InputFilesProcessed.Count;
            var parser = new Sixty502DotNetParser(stream)
            {
                Symbols = _services.Symbols,
                Profile = true
            };
            parser.Interpreter.PredictionMode = PredictionMode.SLL;
            stopwatch.Start();
            _ = parser.source();
            stopwatch.Stop();

            Console.Write("Rule".PadRight(35));
            Console.Write("Pred. time".PadRight(15));
            Console.Write("Invocations".PadRight(15));
            Console.Write("SLL_Looks".PadRight(15));
            Console.Write("SLL_Max".PadRight(15));
            Console.Write("Ambiguities".PadRight(15));
            Console.WriteLine("Errors");
            Console.WriteLine(
                "----------------------------------------------------------" +
                "----------------------------------------------------------");
            foreach (var decisionInfo in parser.ParseInfo.getDecisionInfo())
            {
                var ds = parser.Atn.GetDecisionState(decisionInfo.decision);
                var rule = parser.RuleNames[ds.ruleIndex];
                if (decisionInfo.timeInPrediction > 0)
                {
                    Console.Write($"{rule,-35}");
                    Console.Write($"{decisionInfo.timeInPrediction,-15}");
                    Console.Write($"{decisionInfo.invocations,-15}");
                    Console.Write($"{decisionInfo.SLL_TotalLook,-15}");
                    Console.Write($"{decisionInfo.SLL_MaxLook,-15}");
                    Console.Write($"{decisionInfo.ambiguities.Count,-15}");
                    Console.WriteLine($"{decisionInfo.errors.Count,-15}");
                }
            }
            Console.WriteLine(
                "----------------------------------------------------------" +
                "----------------------------------------------------------");
            Console.WriteLine($"Files processed: {files}");
            Console.WriteLine($"Characters processed: {text.Length}");
            Console.WriteLine($"Total parse time: {stopwatch.Elapsed.TotalSeconds} sec.");
        }

        /// <summary>
        /// Assemble the input source, generate output, and report any errors.
        /// </summary>
        public void Assemble()
        {
            if (!_services.Options.NoStats && !_services.Options.PreprocessOnly)
            {
                Console.WriteLine($"{Assembler.AssemblerName}");
                Console.WriteLine($"{Assembler.AssemblerVersion}");
            }
            var totalAssemblyStopWatch = new Stopwatch();
            totalAssemblyStopWatch.Start();
            ProcessOptions();
            if (_services.Log.HasErrors)
            {
                _services.Log.DumpErrors(_services.Options.NoHighlighting);
                return;
            }
            var parsedSource = ParseSource();
            if (_services.Log.HasErrors)
            {
                _services.Log.DumpErrors(_services.Options.NoHighlighting);
                return;
            }
#if DEBUG
            Console.WriteLine($"[Debug]: Parse time: {totalAssemblyStopWatch.Elapsed.TotalSeconds}");
#endif
            if (!_services.Options.PreprocessOnly)
            {
                var postParseStopWatch = new Stopwatch();
                var passNeeded = true;
                postParseStopWatch.Start();
                while (passNeeded && !_services.Log.HasErrors)
                {
                    if (_services.State.CurrentPass > 4)
                    {
                        _services.Log.LogEntrySimple("Too many passes attempted.");
                        break;
                    }
                    DoPass(parsedSource!);
                    _services.State.CurrentPass++;
                    passNeeded = _services.State.PassNeeded;
                }
                CheckUnreferencedSymbols();
                if (!_services.Log.HasErrors)
                {
                    if (!_services.Options.WarnNotUnusedSections && _services.Output.UnusedSections.Any())
                    {
                        foreach (var unused in _services.Output.UnusedSections)
                        {
                            _services.Log.LogEntrySimple($"Section \"{unused}\" was declared but not used.");
                        }
                    }
                    WriteOutput();
                    totalAssemblyStopWatch.Stop();
                    postParseStopWatch.Stop();
#if DEBUG
                    Console.WriteLine($"[Debug]: Post-parse time: {postParseStopWatch.Elapsed.TotalSeconds}");
                    Console.WriteLine($"[Debug]: Total assembly time: {totalAssemblyStopWatch.Elapsed.TotalSeconds}");
#endif
                }
                PrintStatus(totalAssemblyStopWatch.Elapsed.TotalSeconds);
            }
            else if (!string.IsNullOrEmpty(_preprocessedSource))
            {
                Console.WriteLine($"{GetListingHeader()}// Pre-processed output\n");
                Console.Write(_preprocessedSource);
            }
        }
    }
}