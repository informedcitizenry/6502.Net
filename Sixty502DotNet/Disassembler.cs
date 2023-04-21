//-----------------------------------------------------------------------------
// Copyright (c) 2017-2023 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using Sixty502DotNet.Shared;

namespace Sixty502DotNet.CLI;

public static class Disassembler
{
	private static bool CheckOptions(CommandLineOptions cliOptions)
	{
        if (cliOptions.Autosize ||
            cliOptions.BranchAlways ||
            cliOptions.CaseSensitive ||
            !string.IsNullOrEmpty(cliOptions.ConfigFile) ||
            cliOptions.CreateConfig ||
            cliOptions.Defines?.Count > 0 ||
            cliOptions.EchoEachPass ||
            cliOptions.EnableAllWarnings ||
            !string.IsNullOrEmpty(cliOptions.ErrorFile) ||
            !string.IsNullOrEmpty(cliOptions.IncludePath) ||
            !string.IsNullOrEmpty(cliOptions.LabelFile) ||
            cliOptions.LabelsAddressesOnly ||
            !string.IsNullOrEmpty(cliOptions.ListingFile) ||
            cliOptions.LongAddressing ||
            cliOptions.NoDisassembly ||
            cliOptions.NoHighlighting ||
            cliOptions.NoSource ||
            cliOptions.NoStats ||
            cliOptions.NoWarnings ||
            !string.IsNullOrEmpty(cliOptions.OutputSection) ||
            !string.IsNullOrEmpty(cliOptions.Patch) ||
            cliOptions.ResetPCOnBank ||
            cliOptions.Sections?.Count > 0 ||
            cliOptions.ShowChecksums ||
            cliOptions.TruncateAssembly ||
            cliOptions.VerboseList ||
            cliOptions.ViceLabels ||
            cliOptions.WarnAboutJumpBug ||
            cliOptions.WarnAboutUsingTextInNonTextPseudoOps ||
            cliOptions.WarnCaseMismatch ||
            cliOptions.WarningsAsErrors ||
            cliOptions.WarnLeft ||
            cliOptions.WarnNotUnusedSections ||
            cliOptions.WarnSimplifyCallReturn ||
            cliOptions.WarnUnreferencedSymbols)
        {
            Output.OutputError(new Warning("One or more options is ignored in disassembly mode"), false);
        }
        if (cliOptions.InputFiles?.Count < 1)
        {
            Output.OutputError(new Error("Input file(s) not specified"), false);
            return false;
        }
        return true;
    }

	public static void Disassemble(CommandLineOptions cliOptions)
	{
        Output.OutputProductInfo();

        if (!CheckOptions(cliOptions))
        {
            return;
        }
        FileSystemBinaryReader binaryReader = new(cliOptions.IncludePath);

        List<byte[]> objectCode = new();
        for (int i = 0; i < cliOptions.InputFiles!.Count; i++)
        {
            try
            {
                objectCode.Add(binaryReader.ReadAllBytes(cliOptions.InputFiles[i]));
            }
            catch
            {
                Output.OutputError(new Error($"Could not read file '{cliOptions.InputFiles[i]}'"), true);
                return;
            }
        }
        Options options = OptionsFactory.FromCLIOptions(cliOptions);
        Interpreter interpreter = new(options, new FileSystemBinaryReader(null));

        string disassembly = interpreter.Disassemble(objectCode, cliOptions.DisassemblyStart, cliOptions.DisassemblyOffset);
        File.WriteAllText(cliOptions.OutputFile, disassembly);

        Console.WriteLine("-------------------------------------");
        Console.WriteLine("Disassembly file created.");
    }
}

