// Copyright (c) 2026 informedcitizenry
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using Sixty502DotNet.Shared;
using Sixty502DotNet.CLI;
using Sixty502DotNet.Shared.Lex;
using Sixty502DotNet.Shared.Json;
using Sixty502DotNet.Shared.Error;
using Sixty502DotNet.Shared.Parse;
using System.Diagnostics;

namespace Sixty502DotNet;

public static class Assemble
{
    public static int FromBuildFile(string buildFile)
    {
        string configSchema;
        try
        {
            configSchema = File.ReadAllText("Json/ConfigSchema.json");
        }
        catch
        {
            WriteFatalError("Build file schema could not be validated.");
            return 1;
        }
        var fileReaderFactory = new FileSourceReaderFactory(null);
        var fileReader = fileReaderFactory.CreateReader();
        var json = fileReader.GetSource(buildFile);
        if (json == null)
        {
            if (!buildFile.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                json = fileReader.GetSource($"{buildFile}.json");
            }
            if (json == null)
            {
                WriteFatalError($"Could not read `{buildFile}`.");
                return 1;
            }
        }
        var parser = new Parser(buildFile, json, LexerBehavior.Json);
        
        var jsonValue = parser.Json();
        var jsonObj = jsonValue.AsDictionary();
        if (jsonObj == null)
        {
            WriteFatalError("Build file not valid format");
            return 1;
        }
        try
        {
            var validator = new JsonValidator(configSchema);
            var errors = validator.Validate(jsonValue).ToArray();
            if (errors.Length > 0)
            {
                WriteFatalError($"Build file `{buildFile}` contained one or more invalid configuration errors:");
                foreach (var error in errors)
                {
                    Console.Error.WriteLine(error);   
                }
                return 1;
            }
        }
        catch (JsonSchemaException e)
        {
            WriteFatalError(e.Message);
            return 1;
        }
        
        var flattenedSections = new List<string>();
        if (jsonObj.TryGetValue("sections", out var sectionValue) &&
            sectionValue?.AsDictionary() is { } sectionObj)
        {
            foreach (var member in sectionObj)
            {
                var csv = new List<string>
                {
                    member.Key.AsString()
                };
                if (member.Value.AsDictionary() is { } csvObj)
                {
                    csv.Add(csvObj.GetStringValueFromPath("starts") ?? "0");
                    if (csvObj.TryGetValue("ends", out var endsValue))
                    {
                        csv.Add(endsValue?.AsString() ?? string.Empty);
                    }
                }
                flattenedSections.Add(string.Join(',', csv));
            }
        }
        var assemblyOptions = new AssemblyOptions
        {
            AutosizeRegisters = jsonObj.GetBoolValueFromPath("/target/autoSizeRegisters"),
            BraFor6502 = jsonObj.GetBoolValueFromPath("/target/branchAlways"),
            CaseSensitive = jsonObj.GetBoolValueFromPath("/caseSensitive"),
            Checksum = jsonObj.GetBoolValueFromPath("/loggingOptions/checksum"),
            Cpu = jsonObj.GetStringValueFromPath("/target/cpu"),
            Defines = jsonObj.GetListFromPath("/defines"),
            DoNotWarnOnUnusedSections = jsonObj.GetBoolValueFromPath("/loggingOptions/suppressUnusedSectionWarning"),
            DoNotWarnBankCrossed = jsonObj.GetBoolValueFromPath("/loggingOptions/suppressedBankCrossWarning"),
            DoNotWarOnIntToFloat = jsonObj.GetBoolValueFromPath("/loggingOptions/suppressIntToFloatWarning"),
            PseudoBranches6502 = jsonObj.GetBoolValueFromPath("/target/enablePseudoBranches"),
            Format = jsonObj.GetStringValueFromPath("/target/binaryFormat"),
            IncludePath = jsonObj.GetStringValueFromPath("/includePath"),
            LabelsAddressesOnly = jsonObj.GetBoolValueFromPath("/listingOptions/labelsAddressesOnly"),
            ListLineNumber = jsonObj.GetBoolValueFromPath("/listingOptions/lineNumbers"),
            ListMonitorCode = !jsonObj.GetBoolValueFromPath("/listingOptions/noAssembly"),
            ListDisassembly = !jsonObj.GetBoolValueFromPath("/listingOptions/noDisassembly"),
            ListSourceCode = !jsonObj.GetBoolValueFromPath("/listingOptions/noSource"),
            M16 = jsonObj.GetBoolValueFromPath("/target/m16"),
            OutputSection = jsonObj.GetStringValueFromPath("/outputSection"),
            Sections = flattenedSections,
            TruncateListing = jsonObj.GetBoolValueFromPath("/listingOptions/truncateAssembly"),
            UseLegacyBlocks = jsonObj.GetBoolValueFromPath("/useLegacyBlocks"),
            VerboseList = jsonObj.GetBoolValueFromPath("/listingOptions/verbose"),
            ViceLabels = jsonObj.GetBoolValueFromPath("/listingOptions/viceLabels"),
            WarnCaseMismatch = jsonObj.GetBoolValueFromPath("/loggingOptions/warnCaseMismatch"),
            WarningsAsErrors = jsonObj.GetBoolValueFromPath("/loggingOptions/warningsAsErrors"),
            WarnJumpBug =  jsonObj.GetBoolValueFromPath("/loggingOptions/warnAboutJumpBug"),
            WarnLeftSpaceOfLabel = jsonObj.GetBoolValueFromPath("/loggingOptions/warnLeft"),
            WarnRegistersAsIdent = jsonObj.GetBoolValueFromPath("/loggingOptions/warnRegistersAsIdentifiers"),
            WarnSimplifyCallReturn = jsonObj.GetBoolValueFromPath("/loggingOptions/warnSimplifyCallReturn"),
            WarnOptimizeZ80AccToZero = jsonObj.GetBoolValueFromPath("/loggingOptions/warnAboutOptimizingZ80AccumulatorReset"),
            WarnTextInNonTextPseudoOp = jsonObj.GetBoolValueFromPath("/loggingOptions/WarnAboutUsingTextInNonTextPseudoOps"),
            WarnUnreferencedSymbols = jsonObj.GetBoolValueFromPath("/loggingOptions/warnUnreferencedSymbols"),
            X16 =  jsonObj.GetBoolValueFromPath("/target/x16")
        };
        var cliOptions = new ParsedCliOptions
        {
            PassedOptions = [buildFile],
            AssemblyOptions = assemblyOptions,
            Cpu = jsonObj.GetStringValueFromPath("/target/cpu"),
            InputPaths = jsonObj.GetListFromPath("/sources"),
            ErrorFile = jsonObj.GetStringValueFromPath("/loggingOptions/errorPath"),
            LabelFile = jsonObj.GetStringValueFromPath("/listingOptions/labelPath"),
            ListingFile = jsonObj.GetStringValueFromPath("/listingOptions/listPath"),
            NoHighlighting = jsonObj.GetBoolValueFromPath("/loggingOptions/noHighlighting"),
            NoStats =  jsonObj.GetBoolValueFromPath("/loggingOptions/noStats"),
            NoWarnings = jsonObj.GetBoolValueFromPath("/loggingOptions/noWarnings"),
            Output = jsonObj.GetStringValueFromPath("/outputFile") ?? "a.out",
            Patch = jsonObj.GetStringValueFromPath("/patch"),
            Quiet = jsonObj.GetBoolValueFromPath("/loggingOptions/quietMode"),
        };
        return WithOptions(cliOptions);
    }
    
    public static int WithOptions(ParsedCliOptions options)
    {
        Debug.Assert(options.AssemblyOptions != null);
        if (options.Quiet)
        {
            Console.SetOut(TextWriter.Null);
        }
        Trace.Listeners.Add(new TextWriterTraceListener(Console.Out));
        Console.WriteLine(App.Name);
        Console.WriteLine(App.Version);
        var fileSourceFactory = new FileSourceReaderFactory(options.AssemblyOptions.IncludePath);
        try
        {
            var sw = new Stopwatch();
            sw.Start();
            var assemblyResult = Assembler.Assemble
            (
                options.InputPaths.ToList(), 
                fileSourceFactory, 
                options.AssemblyOptions
            );
            if (assemblyResult.Errors.Count == 0)
            {
                if (!OutputCompilation(assemblyResult, options))
                {
                    return 1;
                }
                sw.Stop();
            }
            return ReportResult(assemblyResult, options, sw.Elapsed.TotalSeconds);
        }
        catch (Exception ex)
        {
            Console.ResetColor();
            Console.Error.WriteLine($"Internal error (it's not you, it's us). Here is some more info about what happened:\n{ex.StackTrace??string.Empty} {ex.Message}\n");
            return 1;
        }
    }

    private static bool OutputCompilation(AssemblyResult result, ParsedCliOptions options)
    {
        try
        {
            if (!string.IsNullOrEmpty(options.Patch))
            {
                if (!int.TryParse(options.Patch, out var patchAddress) || patchAddress < 0 || patchAddress > 0xffffff)
                {
                    WriteFatalError("Option `--patch` requires a 24-bit unsigned integer.");
                    return false;
                }
                var fileSourceFactory = new FileSourceReaderFactory(options.AssemblyOptions?.IncludePath);
                var fileReader = fileSourceFactory.CreateReader();
                var fileBytes = fileReader.GetBytes(options.Output) ?? throw new FileNotFoundException();
                Array.Copy(result.ObjectCode.ToArray(), 0, fileBytes, patchAddress, result.ObjectCode.Count);
                File.WriteAllBytes(options.Output, fileBytes);
            }
            else
            {
                File.WriteAllBytes(options.Output, result.ObjectCode.ToArray());
            }
        }
        catch
        {
            WriteFatalError($"Could not write to `{options.Output}`.");
            return false;
        }
        if (!string.IsNullOrEmpty(options.ListingFile) && !string.IsNullOrEmpty(result.Listing))
        {
            try
            {
                var header = $"// Input files: {string.Join("\n// ", options.InputPaths)}\n" +
                                $"// Options: {string.Join(' ', options.PassedOptions)}";
                var listingContents = $"// {App.ShortName} {App.Version}\n" +
                                      $"// Build time (UTC): {DateTime.Now.ToUniversalTime():s}\n" +
                                      header + "\n\n" +
                                      string.Join(Environment.NewLine, result.Listing);
                File.WriteAllText(options.ListingFile, listingContents);
            }
            catch
            {
                WriteFatalError($"Could not write program listing to `{options.ListingFile}`.");
                return false;
            }
        }
        if (string.IsNullOrEmpty(options.LabelFile)) return true;
        try
        {
            File.WriteAllText(options.LabelFile, result.Labels);
        }
        catch
        {
            WriteFatalError($"Could not write labels to `{options.LabelFile}`.");
            return false;
        }
        return true;
    }
    
    private static int ReportResult(AssemblyResult assemblyResult, ParsedCliOptions options, double elapsed)
    {
        
        if (options.NoStats) return assemblyResult.Errors.Count == 0 ? 0 : 1;
        
        if (!string.IsNullOrEmpty(options.ErrorFile) && assemblyResult.Errors.Count > 0)
        {
            using StreamWriter writer = new(options.ErrorFile);
            foreach (var error in assemblyResult.Errors)
            {
                writer.WriteLine(error.Report(options.NoHighlighting));
            }
            foreach (var warning in assemblyResult.Warnings)
            {
                writer.WriteLine(warning.Report(options.NoHighlighting));
            }
            writer.Close();
            return 1;
        }
        foreach (var error in assemblyResult.Errors)
        {
            error.ReportToConsole(options.NoHighlighting);
            if (error.IsFatal) return 1;
        }
        if (assemblyResult.Warnings.Count > 0 && !options.NoWarnings)
        {
            foreach (var warning in assemblyResult.Warnings)
            {
                warning.ReportToConsole(options.NoHighlighting);
            }
        }
        if (assemblyResult.Errors.Count == 0)
        {
            var showPcStartAndEnd = assemblyResult.PcStart != assemblyResult.Start ||
                                    assemblyResult.PcEnd != assemblyResult.End;
            Console.WriteLine("-------------------------------------");
            Console.Write($"Start: {assemblyResult.Start:x4}");
            if (showPcStartAndEnd)
            {
                Console.Write($" [{assemblyResult.PcStart:x4}]");
            }
            Console.WriteLine();
            Console.Write($"End:   {assemblyResult.End:x4}");
            if (showPcStartAndEnd)
            {
                Console.Write($" [{assemblyResult.PcEnd:x4}]");
            }
            Console.WriteLine();
            if (!string.IsNullOrEmpty(options.Patch))
            {
                Console.WriteLine($"Patched '{options.Output}' at Offs:{options.Patch}");
            }
            if (options.AssemblyOptions?.Checksum == true)
            {
                Console.WriteLine($"Checksum: {assemblyResult.Checksum}");
            }
            if (!string.IsNullOrEmpty(options.AssemblyOptions?.OutputSection))
            {
                Console.WriteLine($"[{options.AssemblyOptions.OutputSection}]");
            }
            Console.WriteLine($"Passes: {assemblyResult.Passes}");
            Debug.WriteLine($"Sec: {elapsed}");
            Debug.WriteLine($"(Parse time: {assemblyResult.ParseTime})");
            Console.WriteLine("-------------------------------------");
        }
        else
        {
            Console.Error.WriteLine();
        }
        Console.Write($"{assemblyResult.Errors.Count} error(s), ");
        Console.WriteLine($"{assemblyResult.Warnings.Count} warning(s)");
        
        return assemblyResult.Errors.Count == 0 ? 0 : 1;
    }

    public static void WriteFatalError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Error.Write("fatal error: ");
        Console.ResetColor();
        Console.Error.WriteLine(message);
    }
}