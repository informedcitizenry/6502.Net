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

using Sixty502DotNet.CLI;
using Sixty502DotNet.Shared.Eval;
using Sixty502DotNet.Shared.Eval.Scope;
using Sixty502DotNet.Shared.Eval.String;

namespace Sixty502DotNet;

public static class ConfigureBuild
{
    public static void FromOptions(ParseResult parseResult)
    {
        var buildFile = parseResult.GetStringOption("build-file") ?? "build.json";
        var root = new Dictionary();
        AddListOption(root, "sources", parseResult, "input");
        AddBoolOption(root, "caseSensitive", parseResult, "--case-sensitive", "-C");
        AddListOption(root, "defines", parseResult, "--define", "-D");
        AddStringOption(root, "includePath", parseResult, "--include-path", "-I");
        AddStringOption(root, "outputFile", parseResult, "--output", "-o");
        AddStringOption(root, "outputSection", parseResult, "--output-section");
        AddNumberOption(root, "patch", parseResult, "--patch");
        AddBoolOption(root, "resetPcOnBank", parseResult, "--reset-on-bank");
        AddBoolOption(root, "useLegacyBlocks", parseResult, "--legacy-blocks");
        
        var listingOptions = new Dictionary();
        AddBoolOption(listingOptions, "lineNumbers", parseResult, "--line-numbers");
        AddBoolOption(listingOptions, "noAssembly",  parseResult, "--no-assembly", "-a");
        AddBoolOption(listingOptions, "noDisassembly",  parseResult, "--no-disassembly", "-d");
        AddBoolOption(listingOptions, "noSource",  parseResult, "--no-source", "-s");
        AddBoolOption(listingOptions, "labelsAddressesOnly",  parseResult, "--labels-addresses-only");
        AddBoolOption(listingOptions, "truncateAssembly",      parseResult, "--truncate-assembly", "-t");
        AddBoolOption(listingOptions, "verbose", parseResult, "--verbose-asm");
        AddBoolOption(listingOptions, "viceLabels", parseResult, "--vice-labels");
        AddStringOption(listingOptions, "labelPath", parseResult, "--labels", "-l");
        AddStringOption(listingOptions, "listPath",  parseResult, "--list", "-L");

        if (listingOptions.Count > 0)
        {
            root.Add("listingOptions", new Value(listingOptions));
        }
        var wall = parseResult.GetBoolOption("--Wall");
        var loggingOptions = new Dictionary();
        AddBoolOption(loggingOptions, "checksum", parseResult, "--checksum");
        AddStringOption(loggingOptions, "errorPath", parseResult, "--error", "-e");
        AddBoolOption(loggingOptions, "noHighlighting", parseResult, "--no-highlighting");
        AddBoolOption(loggingOptions, "noStats", parseResult, "--no-stats", "-n");
        AddBoolOption(loggingOptions, "quietMode",  parseResult, "--quiet", "-q");
        AddBoolOption(loggingOptions, "warningsAsErrors", parseResult, "--Werror");
        AddBoolOption(loggingOptions, "noWarnings", parseResult, "--no-warn", "-w");
        
        if (wall)
        {
            loggingOptions.Add("warnAboutAmbiguousZp", new Value(true));
            loggingOptions.Add("warnAboutJumpBug", new Value(true));
            loggingOptions.Add("warnCaseMismatch", new Value(true));
            loggingOptions.Add("warnAboutOptimizingZ80AccumulatorReset", new Value(true));
            loggingOptions.Add("WarnAboutUsingTextInNonTextPseudoOps", new Value(true));
            loggingOptions.Add("warnLeft", new Value(true));
            loggingOptions.Add("warnRegistersAsIdentifiers", new Value(true));
            loggingOptions.Add("warnSimplifyCallReturn", new Value(true));
            loggingOptions.Add("warnUnreferencedSymbols", new Value(true));
            loggingOptions.Add("warnUnusedSections", new Value(true));
        }
        else
        {
            AddBoolOption(loggingOptions, "warnAboutAmbiguousZp", parseResult, "--Wambiguous-zp");
            AddBoolOption(loggingOptions, "warnAboutJumpBug", parseResult, "--Wjump-bug");
            AddBoolOption(loggingOptions, "warnCaseMismatch", parseResult, "--Wcase-mismatch");
            AddBoolOption(loggingOptions, "warnAboutOptimizingZ80AccumulatorReset", parseResult, "--Woptimize-z80-acc-to-zero");
            AddBoolOption(loggingOptions, "WarnAboutUsingTextInNonTextPseudoOps", parseResult, "--Wtext-in-non-text-pseudo-ops");
            AddBoolOption(loggingOptions, "warnLeft", parseResult, "--Wleft");
            AddBoolOption(loggingOptions, "warnRegistersAsIdentifiers", parseResult, "--Wregister-as-identifier");
            AddBoolOption(loggingOptions, "warnSimplifyCallReturn", parseResult, "--Wsimplify-call-return");
            AddBoolOption(loggingOptions, "warnUnreferencedSymbols", parseResult,  "--Wunreferenced-symbols");
            AddBoolOption(loggingOptions, "suppressUnusedSectionWarning", parseResult, "--Wno-unused-sections");
            AddBoolOption(loggingOptions, "suppressedBankCrossWarning", parseResult, "--Wno-bank-crossed");
            AddBoolOption(loggingOptions, "suppressIntToFloatWarning", parseResult, "--Wno-int-to-float");
        }

        if (loggingOptions.Count > 0)
        {
            root.Add("loggingOptions", new Value(loggingOptions));
        }
        var targetOptions = new Dictionary();
        AddBoolOption(targetOptions, "autoSizeRegister", parseResult, "--auto-size-registers", "-r");
        AddStringOption(targetOptions, "binaryFormat", parseResult, "--format", "-f");
        AddBoolOption(targetOptions, "branchAlways", parseResult, "--enable-branch-always", "-b");
        AddStringOption(targetOptions, "cpu", parseResult, "--cpu", "-c");
        AddBoolOption(targetOptions, "enablePseudoBranches", parseResult, "--enable-pseudo-long-branches", "-P");
        AddBoolOption(targetOptions, "m16", parseResult, "--m16", "--mx16");
        AddBoolOption(targetOptions, "x16", parseResult, "--mx16", "--x16");
        var sections = parseResult.GetListOption("--dsections");
        var sectionOptions = new Dictionary();
        for (var i = 0; i < sections.Count; i++)
        {
            var section = sections[i].Split(',');
            long end = int.MinValue;
            if (section.Length is < 2 or > 3 || 
                !char.IsLetter(section[0][0]) ||
                !long.TryParse(section[1], out var start) || 
                (sections.Count == 3 && !long.TryParse(section[2], out end)))
            {
                Console.Error.WriteLine($"error: Section option `{sections[i]}` is invalid");
                return;
            }
            var sectionObj = new Dictionary { { "starts", new Value(start) } };
            if (end > int.MinValue)
            {
                sectionObj.Add("ends", new Value(end));
            }
            sectionOptions.Add(section[0], new Value(sectionObj));
        }
        if (sectionOptions.Count > 0)
        {
            root.Add("sections", new Value(sectionOptions));
        }
        var json = new Value(root);
        var pretty = json.PrettyPrint();
        File.WriteAllText(buildFile, pretty);
        Console.WriteLine(App.Name);
        Console.WriteLine(App.Version);
        Console.WriteLine($"Build file `{buildFile}` successfully generated.");
    }
    
    
    private static void AddBoolOption
    (
        Dictionary obj, 
        string property,
        ParseResult parseResult, 
        params string[] options
    )
    {
        var boolOption = parseResult.GetBoolOption(options);
        if (boolOption)
        {
            obj.Add(property, new Value(true));
        }
    }

    private static void AddStringOption
    (
        Dictionary obj, 
        string property,
        ParseResult parseResult, 
        params string[] options
    )
    {
        var strOption = parseResult.GetStringOption(options);
        if (!string.IsNullOrEmpty(strOption))
        {
            obj.Add(property, new Value(strOption, TextEncodingType.Default));
        }
    }
    
    private static void AddListOption
    (
        Dictionary obj, 
        string property, 
        ParseResult parseResult, 
        params string[] options
    )
    {
        var listOption = parseResult
            .GetListOption(options)
            .Select(o => new Value(o, TextEncodingType.Default))
            .ToList();
        if (listOption.Count > 0)
        {
            obj.Add(property, new Value(listOption, TypeTag.Array));
        }
    }

    private static void AddNumberOption
    (
        Dictionary obj, 
        string property,
        ParseResult parseResult,
        params string[] options
    )
    {
        foreach (var option in options)
        {
            if (!parseResult.Options.TryGetValue(option, out var parsedOption)) continue;
            obj.Add(property, new Value(parsedOption.GetIntValue()));
            return;
        }
    }
    
}