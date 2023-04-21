//-----------------------------------------------------------------------------
// Copyright (c) 2017-2023 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using Sixty502DotNet.CLI;
using Sixty502DotNet.Shared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

using AssemblerError = Sixty502DotNet.Shared.Error;

CommandLineOptions? cliOptions = null;
try
{
    cliOptions = CommandLineOptions.FromArgs(args);
    if (cliOptions == null)
    {
        return;
    }
    if (cliOptions.Disassemble)
    {
        Disassembler.Disassemble(cliOptions);
        return;
    }
    if (cliOptions.Quiet)
    {
        Console.SetOut(TextWriter.Null);
    }
    Stopwatch stopwatch = new();
    stopwatch.Start();

    int warnings = 0;
    if (cliOptions.DisassemblyStart != null || cliOptions.DisassemblyOffset != null)
    {
        if (cliOptions.WarningsAsErrors)
        {
            throw new AssemblerError("Disassembly options ignored");
        }
        warnings++;
        Output.OutputError(new Warning("Disassembly options ignored"), false);
    }
    if (cliOptions.CreateConfig)
    {
        OptionsFactory.OptionsToConfigFile(cliOptions);
        Console.WriteLine("Cnofig file created.");
        return;
    }
    Options options;
    if (!string.IsNullOrEmpty(cliOptions.ConfigFile))
    {
        int confIx = args.ToList().FindIndex(s => s.StartsWith("--config", StringComparison.Ordinal));
        if (confIx != 0 && confIx != args.ToList().FindLastIndex(s => s.StartsWith('-')))
        {
            if (cliOptions.WarningsAsErrors)
            {
                throw new AssemblerError("Option '--config' ignores all other options");
            }
            warnings++;
            Output.OutputError(new Warning("Option '--config' ignores all other options"), false);
        }
        string configSchema = File.ReadAllText("JSON/ConfigSchema.json");
        string? config = FileSystemTextReader.ReadFile(cliOptions.ConfigFile, cliOptions.IncludePath);
        if (string.IsNullOrEmpty(config))
        {
            throw new AssemblerError($"Could not read config file '{cliOptions.ConfigFile}'");
        }
        JsonValidator validator = new(configSchema);
        if (validator.ValidateAndDeserialize(config, out IEnumerable<string> errors) is not JsonObject parsedConfig ||
            errors.Any())
        {
            Output.OutputError(new AssemblerError($"Config file '{cliOptions.ConfigFile}' contained one or more invalid parameters:"), true);
            foreach (string error in errors)
            {
                Console.Error.WriteLine($" :{error}");
            }
            return;
        }
        string parsedJson = parsedConfig.ToString();
        cliOptions = OptionsFactory.FromConfig(parsedConfig);
        if (cliOptions.Disassemble)
        {
            Disassembler.Disassemble(cliOptions);
            return;
        }
    }
    if (cliOptions.InputFiles?.Count == 0)
    {
        throw new AssemblerError("Input file(s) not specified");
    }
    options = OptionsFactory.FromCLIOptions(cliOptions);
    Output.OutputProductInfo();

    int patchAddress = 0;
    if (!string.IsNullOrEmpty(cliOptions.Patch))
    {
        try
        {
            SyntaxParser.DefineAssignContext parsedPatch = ParserBase.ParseDefineAssign($"patch={cliOptions.Patch}");
            patchAddress = Evaluator.EvalConstant(parsedPatch.expr()).AsInt();
            if (patchAddress < 0 || patchAddress > UInt24.MaxValue)
            {
                throw new AssemblerError("Option `--patch` requires a 24-bit unsigned decimal integer");
            }
        }
        catch (AssemblerError err)
        {
            throw err;
        }
        catch
        {
            throw new AssemblerError($"Cannot patch output file '{cliOptions.OutputFile}' due to invalid arguments");
        }
    }
    if (!string.IsNullOrEmpty(cliOptions.LabelFile))
    {
        if (cliOptions.ViceLabels)
        {
            if (cliOptions.LabelsAddressesOnly)
            {
                if (cliOptions.WarningsAsErrors)
                {
                    throw new AssemblerError("Option '--labels-addresses-only' ignored when '--vice-labels' is set");
                }
                warnings++;
                Output.OutputError(
                    new Warning("Option '--labels-addresses-only' ignored when '--vice-labels' is set"), true);
            }
        }
    }

    Interpreter interpreter = new(options, new FileSystemBinaryReader(cliOptions!.IncludePath));
    AssemblyState state = interpreter.Exec(cliOptions.InputFiles!, new FileSystemCharStreamFactory(cliOptions!.IncludePath));
    stopwatch.Stop();

    Output.OutputErrorsAndWarnings(state.Errors, state.Warnings, cliOptions.ErrorFile, cliOptions.NoHighlighting);

    if (state.Errors.Count == 0)
    {
        Output.OutputCode(cliOptions, patchAddress, state.Output);
        if (!string.IsNullOrEmpty(cliOptions.ListingFile) && state.StatementListings.Count > 0)
        {
            string header = $"// Input files: {string.Join("\n// ", cliOptions.InputFiles!)}\n" +
                             $"// Options: {string.Join(' ', args)}";
            Output.WriteListing(header, cliOptions.ListingFile, state.StatementListings);
        }
        Output.WriteLabelFile(cliOptions.LabelFile, state.Symbols, cliOptions.LabelsAddressesOnly, cliOptions.ViceLabels);
        if (!cliOptions.NoStats)
        {
            Console.WriteLine("-------------------------------------");
            Console.WriteLine(state.Output.ToString());
            if (!string.IsNullOrEmpty(cliOptions.Patch))
            {
                Console.WriteLine($"Patched '{cliOptions.OutputFile}' at Offs:{cliOptions.Patch}");
            }
            if (cliOptions.ShowChecksums)
            {
                Console.WriteLine($"Checksum: {state.Output.GetOutputHash()}");
            }
            if (!string.IsNullOrEmpty(cliOptions.OutputSection))
            {
                Console.WriteLine($"[{cliOptions.OutputSection}]");
            }
            Console.WriteLine($"Passes: {state.CurrentPass}");
            Console.WriteLine($"Sec: {stopwatch.Elapsed.TotalSeconds}");
        }
    }
    if (!cliOptions.NoStats)
    {
        Console.WriteLine("-------------------------------------");
        Console.Write($"{state.Errors.Count} error(s), ");
        Console.WriteLine($"{state.Warnings.Count + warnings} warning(s)");
    }
}
catch (Exception ex)
{
    if (ex is AssemblerError err)
    {
        Output.OutputError(err, cliOptions?.NoHighlighting == true);
    }
    else
    {
        Console.WriteLine(ex.Message);
    }
}
