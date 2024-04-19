//-----------------------------------------------------------------------------
// Copyright (c) 2017-2024 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using Sixty502DotNet.Shared;

namespace Sixty502DotNet.CLI;

public static class OptionsFactory
{
    public static CommandLineOptions FromConfig(JsonObject json)
    {
        JsonObject? logging = json["loggingOptions"] as JsonObject;
        JsonObject? listing = json["listingOptions"] as JsonObject;
        JsonObject? target = json["target"] as JsonObject;
        List<string> sectionDefines = new();

        if (json["sections"] is JsonObject sections)
        {
            foreach (var section in sections)
            {
                List<string> csv = new()
                {
                    section.Key
                };
                if (section.Value is JsonObject obj)
                {
                    csv.Add(obj["starts"]!.ToString()!);
                    string? ends = obj["ends"]?.ToString();
                    if (!string.IsNullOrEmpty(ends))
                    {
                        csv.Add(ends);
                    }
                }
                sectionDefines.Add(string.Join(',', csv));
            }
        }
        List<string> constantDefines = new();
        if (json["defines"] is JsonArray defineArray)
        {
            foreach (var define in defineArray)
            {
                if (define != null)
                    constantDefines.Add(define.AsString());
            }
        }
        List<string> inputs = new();
        if (json["sources"] is JsonArray sourceArray)
        {
            foreach(var source in sourceArray)
            {
                if (source != null)
                    inputs.Add(source.AsString());
            }
        }
        return new CommandLineOptions()
        {
            InputFiles = inputs,
            Autosize = target?["autoSizeRegisters"]?.AsBool() ?? false,
            BranchAlways = target?["branchAlways"]?.AsBool() ?? false,
            CaseSensitive = json["caseSensitive"]?.AsBool() ?? false,
            CPU = target?["cpu"]?.AsString(),
            Defines = constantDefines,
            Disassemble = listing?["disassemble"]?.AsBool() ?? false,
            DisassemblyStart = listing?["disassemblyStart"]?.AsInt(),
            DisassemblyOffset = listing?["disassemblyOffset"]?.AsInt(),
            EchoEachPass = logging?["echoEachPass"]?.AsBool() ?? false,
            ErrorFile = logging?["errorPath"]?.AsString(),
            Format = target?["binaryFormat"]?.AsString(),
            IncludePath = json["includePath"]?.AsString(),
            LabelFile = listing?["labelPath"]?.AsString(),
            LabelsAddressesOnly = listing?["labelsAddressesOnly"]?.AsBool() ?? false,
            ListingFile = listing?["listPath"]?.AsString(),
            LongAddressing = target?["longAddressing"]?.AsBool() ?? false,
            NoAssembly = listing?["noAssembly"]?.AsBool() ?? false,
            NoDisassembly = listing?["noDisassembly"]?.AsBool() ?? false,
            NoHighlighting = logging?["noHighlighting"]?.AsBool() ?? false,
            NoSource = listing?["noSource"]?.AsBool() ?? false,
            NoStats = logging?["noStats"]?.AsBool() ?? false,
            NoWarnings = logging?["noWarnings"]?.AsBool() ?? false,
            OutputFile = json["outputFile"]!.AsString(),
            OutputSection = json["outputSection"]?.AsString(),
            Patch = json["patch"]?.AsString(),
            Quiet = logging?["quietMode"]?.AsBool() ?? false,
            ResetPCOnBank = json["resetPCOnBank"]?.AsBool() ?? false,
            Sections = sectionDefines,
            ShowChecksums = logging?["checksum"]?.AsBool() ?? false,
            TruncateAssembly = listing?["truncateAssembly"]?.AsBool() ?? false,
            VerboseList = listing?["verbose"]?.AsBool() ?? false,
            ViceLabels = listing?["viceLabels"]?.AsBool() ?? false,
            WarnAboutJumpBug = logging?["warnAboutJumpBug"]?.AsBool() ?? false,
            WarnAboutUsingTextInNonTextPseudoOps = logging?["WarnAboutUsingTextInNonTextPseudoOps"]?.AsBool() ?? false,
            WarnCaseMismatch = logging?["warnCaseMismatch"]?.AsBool() ?? false,
            WarningsAsErrors = logging?["warningsAsErrors"]?.AsBool() ?? false,
            WarnLeft = logging?["warnLeft"]?.AsBool() ?? false,
            WarnNotUnusedSections = logging?["suppressUnusedSectionWarning"]?.AsBool() ?? false,
            WarnRegistersAsIdentifiers = logging?["warnRegistersAsIdentifiers"]?.AsBool() ?? false,
            WarnSimplifyCallReturn = logging?["warnSimplifyCallReturn"]?.AsBool() ?? false,
            WarnUnreferencedSymbols = logging?["warnUnreferencedSymbols"]?.AsBool() ?? false
        };
    }

    public static void OptionsToConfigFile(CommandLineOptions cLIOptions)
    {
        JsonObject jobject = ConfigFromCLIOptions(cLIOptions);
        string json = jobject.ToString(4);
        File.WriteAllText("config.json", json);
    }

    private static JsonObject ConfigFromCLIOptions(CommandLineOptions cLIOptions)
    {
        if (cLIOptions.InputFiles?.Count == 0)
        {
            throw new Error("Input file(s) not specified");
        }
        JsonObject obj = new();
        if (cLIOptions.CaseSensitive) obj.Add("caseSensitive", new BoolValue(true));
        if (cLIOptions.Disassemble || !string.IsNullOrEmpty(cLIOptions.LabelFile) || !string.IsNullOrEmpty(cLIOptions.ListingFile) ||
            cLIOptions.LabelsAddressesOnly || cLIOptions.NoAssembly || cLIOptions.NoDisassembly || cLIOptions.NoSource ||
            cLIOptions.TruncateAssembly || cLIOptions.VerboseList)
        {
            JsonObject listingOptions = [];
            if (cLIOptions.Disassemble)
            {
                listingOptions.Add("disassemble", new BoolValue(true));
                if (cLIOptions.DisassemblyStart.HasValue) listingOptions.Add("disassemblyStart", new NumericValue(cLIOptions.DisassemblyStart.Value));
                if (cLIOptions.DisassemblyOffset.HasValue) listingOptions.Add("disassemblyOffset", new NumericValue(cLIOptions.DisassemblyOffset.Value));
                if (cLIOptions.NoAssembly) listingOptions.Add("noAssembly", new BoolValue(true));
            }
            else
            {
                if (!string.IsNullOrEmpty(cLIOptions.LabelFile)) listingOptions.Add("labelPath", new StringValue($"\"{cLIOptions.LabelFile}\""));
                if (!string.IsNullOrEmpty(cLIOptions.ListingFile)) listingOptions.Add("listPath", new StringValue($"\"{cLIOptions.ListingFile}\""));
                if (cLIOptions.LabelsAddressesOnly) listingOptions.Add("labelsAddressesOnly", new BoolValue(true));
                if (cLIOptions.ViceLabels) listingOptions.Add("viceLabels", new BoolValue(true));
                if (cLIOptions.NoAssembly) listingOptions.Add("noAssembly", new BoolValue(true));
                if (cLIOptions.NoDisassembly) listingOptions.Add("noDisassembly", new BoolValue(true));
                if (cLIOptions.NoSource) listingOptions.Add("noSource", new BoolValue(true));
                if (cLIOptions.TruncateAssembly) listingOptions.Add("truncateAssembly", new BoolValue(true));
                if (cLIOptions.VerboseList) listingOptions.Add("verbose", new BoolValue(true));
                obj.Add("listingOptions", listingOptions);
            }
        }
        if (!string.IsNullOrEmpty(cLIOptions.ErrorFile) || cLIOptions.EchoEachPass || cLIOptions.EnableAllWarnings ||
            cLIOptions.NoHighlighting || cLIOptions.NoStats || cLIOptions.NoWarnings || cLIOptions.Quiet ||
            cLIOptions.ShowChecksums || cLIOptions.WarnAboutJumpBug || cLIOptions.WarnAboutUsingTextInNonTextPseudoOps ||
            cLIOptions.WarnCaseMismatch || cLIOptions.WarningsAsErrors || cLIOptions.WarnLeft ||
            cLIOptions.WarnNotUnusedSections || cLIOptions.WarnSimplifyCallReturn || cLIOptions.WarnUnreferencedSymbols)
        {
            JsonObject loggingOptions = [];
            if (!string.IsNullOrEmpty(cLIOptions.ErrorFile)) loggingOptions.Add("errorPath", new StringValue($"\"{cLIOptions.ErrorFile}\""));
            if (cLIOptions.EchoEachPass) loggingOptions.Add("echoEachPass", new BoolValue(true));
            if (cLIOptions.NoHighlighting) loggingOptions.Add("noHighlighting", new BoolValue(true));
            if (cLIOptions.NoStats) loggingOptions.Add("noStats", new BoolValue(true));
            if (cLIOptions.NoWarnings) loggingOptions.Add("noWarnings", new BoolValue(true));
            if (cLIOptions.Quiet) loggingOptions.Add("quietMode", new BoolValue(true));
            if (cLIOptions.ShowChecksums) loggingOptions.Add("checksum", new BoolValue(true));
            if (cLIOptions.EnableAllWarnings || cLIOptions.WarnAboutJumpBug) loggingOptions.Add("warnAboutJumpBug", new BoolValue(true));
            if (cLIOptions.EnableAllWarnings || cLIOptions.WarnAboutUsingTextInNonTextPseudoOps) loggingOptions.Add("WarnAboutUsingTextInNonTextPseudoOps", new BoolValue(true));
            if (cLIOptions.EnableAllWarnings || cLIOptions.WarnCaseMismatch) loggingOptions.Add("warnCaseMismatch", new BoolValue(true));
            if (cLIOptions.EnableAllWarnings || cLIOptions.WarningsAsErrors) loggingOptions.Add("warningsAsErrors", new BoolValue(true));
            if (cLIOptions.EnableAllWarnings || cLIOptions.WarnLeft) loggingOptions.Add("warnLeft", new BoolValue(true));
            if (cLIOptions.EnableAllWarnings || cLIOptions.WarnNotUnusedSections) loggingOptions.Add("suppressUnusedSectionWarning", new BoolValue(true));
            if (cLIOptions.EnableAllWarnings || cLIOptions.WarnRegistersAsIdentifiers) loggingOptions.Add("warnRegistersAsIdentifiers", new BoolValue(true));
            if (cLIOptions.EnableAllWarnings || cLIOptions.WarnSimplifyCallReturn) loggingOptions.Add("warnSimplifyCallReturn", new BoolValue(true));
            if (cLIOptions.EnableAllWarnings || cLIOptions.WarnUnreferencedSymbols) loggingOptions.Add("warnUnreferencedSymbols", new BoolValue(true));
            
            obj.Add("loggingOptions", loggingOptions);
        }
        if (!string.IsNullOrEmpty(cLIOptions.IncludePath)) obj.Add("includePath", new StringValue($"\"{cLIOptions.IncludePath}\""));
        obj.Add("outputFile", new StringValue($"\"{cLIOptions.OutputFile}\""));
        if (!string.IsNullOrEmpty(cLIOptions.OutputSection)) obj.Add("outputSection", new StringValue($"\"{cLIOptions.OutputSection}\""));
        if (!string.IsNullOrEmpty(cLIOptions.Patch)) obj.Add("patch", new StringValue($"\"{cLIOptions.Patch}\""));
        if (cLIOptions.ResetPCOnBank) obj.Add("resetPCOnBank", new BoolValue(true));
        JsonArray sources = [];
        for (int i = 0; i < cLIOptions.InputFiles?.Count; i++)
        {
            sources.Add(new StringValue(cLIOptions.InputFiles![i].ToString()));
        }
        obj.Add("sources", sources);
        if (cLIOptions.Sections?.Count > 0)
        {
            JsonObject sections = [];
            for (int i = 0; i < cLIOptions.Sections.Count; i++)
            {
                (string sectionName, int start, int? end) = ParserBase.ParseDefineSection(cLIOptions.Sections[i]);
                JsonObject section = new()
                {
                    { "start", new NumericValue(start) }
                };
                if (end != null)
                {
                    section.Add("end", new NumericValue(end.Value));
                }
                sections.Add(sectionName, section);
            }
        }

        if (!string.IsNullOrEmpty(cLIOptions.CPU) || !string.IsNullOrEmpty(cLIOptions.Format) ||
            cLIOptions.Autosize || cLIOptions.BranchAlways || cLIOptions.LongAddressing)
        {
            JsonObject target = new()
            {
                { "cpu", new StringValue($"\"{cLIOptions.CPU ?? "6502"}\"") },
                { "binaryFormat", new StringValue($"\"{cLIOptions.Format ?? "cbm"}\"") }
            };
            if (cLIOptions.Autosize) target.Add("autoSizeRegisters", new BoolValue(true));
            if (cLIOptions.BranchAlways) target.Add("branchAlways", new BoolValue(true));
            if (cLIOptions.LongAddressing) target.Add("longAddressing", new BoolValue(true));
            obj.Add("target", target);
        }
        return obj;
    }

    public static Options FromCLIOptions(CommandLineOptions cLIOptions)
    {
        return new Options()
        {
            DiagnosticOptions = new DiagnosticOptions()
            {
                DoNotWarnAboutUnusedSections = !cLIOptions.WarnNotUnusedSections,
                EchoEachPass = cLIOptions.EchoEachPass,
                NoHighlighting = cLIOptions.NoHighlighting,
                WarningsAsErrors = cLIOptions.WarningsAsErrors,
                WarnCaseMismatch = cLIOptions.EnableAllWarnings || cLIOptions.WarnCaseMismatch,
                WarnJumpBug = cLIOptions.EnableAllWarnings || cLIOptions.WarnAboutJumpBug,
                WarnOfUnreferencedSymbols = cLIOptions.EnableAllWarnings || cLIOptions.WarnUnreferencedSymbols,
                WarnTextInNonTextPseudoOp = cLIOptions.EnableAllWarnings || cLIOptions.WarnAboutUsingTextInNonTextPseudoOps,
                WarnRegistersAsIdentifiers = cLIOptions.EnableAllWarnings || cLIOptions.WarnRegistersAsIdentifiers,
                WarnSimplifyCallReturn = cLIOptions.EnableAllWarnings || cLIOptions.WarnSimplifyCallReturn,
                WarnWhitespaceBeforeLabels = cLIOptions.EnableAllWarnings || cLIOptions.WarnLeft
            },
            ArchitectureOptions = new ArchitectureOptions()
            {
                Cpuid = cLIOptions.CPU,
                AutosizeRegisters = cLIOptions.Autosize,
                BranchAlways = cLIOptions.BranchAlways,
                LongAddressing = cLIOptions.LongAddressing
            },
            OutputOptions = new OutputOptions()
            {
                Format = cLIOptions.Format,
                LabelsOnly = cLIOptions.LabelsAddressesOnly,
                ViceLabels = cLIOptions.ViceLabels,
                NoAssembly = cLIOptions.NoAssembly,
                NoDisassembly = cLIOptions.NoDisassembly,
                NoSource = cLIOptions.NoSource,
                TruncateAssembly = cLIOptions.TruncateAssembly,
                VerboseList = cLIOptions.VerboseList
            },
            GeneralOptions = new GeneralOptions()
            {
                CaseSensitive = cLIOptions.CaseSensitive,
                Defines = cLIOptions.Defines,
                IncludePath = cLIOptions.IncludePath,
                Sections = cLIOptions.Sections,
                ResetPCOnBank = cLIOptions.ResetPCOnBank
            }
        };
    }
}
