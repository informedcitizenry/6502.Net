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

using Sixty502DotNet;
using Sixty502DotNet.CLI;
using Sixty502DotNet.Shared;
using Sixty502DotNet.Shared.Arch;

var options = ConfigureOptions();
var parser = new CliParser(options);
var parseResult = parser.Parse(args);
if (!string.IsNullOrEmpty(parseResult.Help))
{
    Console.WriteLine(parseResult.Help);
    return 0;
}
if (parseResult.Errors.Count != 0)
{
    foreach (var error in parseResult.Errors)
    {
        Assemble.WriteFatalError(error);
    }
    return 1;
}

switch (parseResult.CommandName)
{
    case "build":
    {
        var inputFile = parseResult.GetListOption("input");
        if (inputFile.Count > 1)
        {
            Console.Error.WriteLine("Only one build file can be specified.");
            return 1;
        }
        return Assemble.FromBuildFile(inputFile.Count > 0 ? inputFile[0] : "build.json");
    }
    case "config":
    {
        try
        {
            ConfigureBuild.FromOptions(parseResult);
        }
        catch
        {
            Console.Error.WriteLine("Could not write build file.");
            return 1;
        }
        break;
    }
    case "disassemble":
    {
        var disassemblyOptions = new DisassemblyOptions
        {
            Offset = parseResult.GetIntOption("--disassembly-offset"),
            StartAddress = parseResult.GetIntOption("--disassembly-start", -1),
            EndAddress = parseResult.GetIntOption("--disassembly-end", -1),
            IncludePath = parseResult.GetStringOption("--include-path", "-I"),
            ListDisassembly = !parseResult.GetBoolOption("--no-disassembly", "-d"),
            ListMonitorCode = !parseResult.GetBoolOption("--no-assembly", "-a"),
            M16 = parseResult.GetBoolOption("--m16") || parseResult.GetBoolOption("--mx16"),
            Format = parseResult.GetStringOption("--format", "-f"),
            X16 = parseResult.GetBoolOption("--mx16") || parseResult.GetBoolOption("--x16"),
        };
        var inputFile = parseResult.GetListOption("input");
        var parsedCliOptions = new ParsedCliOptions
        {
            Cpu = parseResult.GetStringOption("--cpu", "-c") ?? "6502",
            DisassemblyOptions = disassemblyOptions,
            InputPaths = inputFile,
            Output = parseResult.GetStringOption("--output", "-o") ?? $"{inputFile[0]}.asm",
        
        };
        return Disassemble.WithOptions(parsedCliOptions);
    }
    default:
    {
        var wall = parseResult.GetBoolOption("--Wall");
        var assemblyOptions = new AssemblyOptions
        {
            AutosizeRegisters = parseResult.GetBoolOption("--auto-size-registers", "-r"),
            BraFor6502 = parseResult.GetBoolOption("--enable-branch-always", "-b"),
            CaseSensitive = parseResult.GetBoolOption("--case-sensitive", "-C"),
            Checksum =  parseResult.GetBoolOption("--checksum"),
            Cpu = parseResult.GetStringOption("--cpu", "-c"),
            Defines = parseResult.GetListOption("--define", "-D"),
            DoNotWarnOnUnusedSections = !wall && parseResult.GetBoolOption("--Wno-unused-sections"),
            DoNotWarnBankCrossed = !wall && parseResult.GetBoolOption("--Wno-bank-crossed"),
            DoNotWarOnIntToFloat = !wall && parseResult.GetBoolOption("--Wno-int-to-float"),
            Format = parseResult.GetStringOption("--format", "-f"),
            IncludePath = parseResult.GetStringOption("--include-path", "-I"),
            LabelsAddressesOnly =  parseResult.GetBoolOption("--labels-addresses-only"),
            ListLineNumber = parseResult.GetBoolOption("--line-numbers"),
            ListMonitorCode = !parseResult.GetBoolOption("--no-assembly", "-a"),
            ListDisassembly = !parseResult.GetBoolOption("--no-disassembly", "-d"),
            ListSourceCode = !parseResult.GetBoolOption("--no-source", "-s"),
            M16 = parseResult.GetBoolOption("--m16") || parseResult.GetBoolOption("--mx16"),
            OutputSection =  parseResult.GetStringOption("--output-section"),
            PseudoBranches6502 = parseResult.GetBoolOption("--enable-pseudo-long-branches", "-P"),
            Sections = parseResult.GetListOption("--dsections"),
            TruncateListing =  parseResult.GetBoolOption("--truncate-assembly", "-t"),
            VerboseList =  parseResult.GetBoolOption("--verbose-asm"),
            ViceLabels = parseResult.GetBoolOption("--vice-labels"),
            WarnAmbiguousZp = wall || parseResult.GetBoolOption("--Wambiguous-zp"),
            WarnCaseMismatch = wall || parseResult.GetBoolOption("--Wcase-mismatch"),
            WarningsAsErrors = parseResult.GetBoolOption("--Werror"),
            WarnJumpBug = wall || parseResult.GetBoolOption("--Wjump-bug"),
            WarnLeftSpaceOfLabel = wall || parseResult.GetBoolOption("--Wleft"),
            WarnRegistersAsIdent = wall || parseResult.GetBoolOption("--Wregister-as-identifier"),
            WarnSimplifyCallReturn =  wall || parseResult.GetBoolOption("--Wsimplify-call-return"),
            WarnOptimizeZ80AccToZero =  wall || parseResult.GetBoolOption("--Woptimize-z80-acc-to-zero"),
            WarnTextInNonTextPseudoOp = wall || parseResult.GetBoolOption("--Wtext-in-non-text-pseudo-ops"),
            WarnUnreferencedSymbols = wall || parseResult.GetBoolOption("--Wunreferenced-symbols"),
            UseLegacyBlocks = parseResult.GetBoolOption("--legacy-blocks"),
            X16 = parseResult.GetBoolOption("--mx16") || parseResult.GetBoolOption("--x16"),
        };
        var parsedCliOptions = new ParsedCliOptions
        {
            PassedOptions = args,
            AssemblyOptions = assemblyOptions,
            Cpu = parseResult.GetStringOption("--cpu", "-c"),
            InputPaths =  parseResult.GetListOption("input"),
            ErrorFile = parseResult.GetStringOption("--error", "-e"),
            LabelFile = parseResult.GetStringOption("--labels", "-l"),
            ListingFile = parseResult.GetStringOption("--list", "-L"),
            NoHighlighting = parseResult.GetBoolOption("--no-highlighting"),
            NoStats = parseResult.GetBoolOption("--no-stats", "-n"),
            NoWarnings = parseResult.GetBoolOption("--no-warn", "-w"),
            Output = parseResult.GetStringOption("--output", "-o") ?? "a.out",
            Patch =  parseResult.GetStringOption("--patch"),
            Quiet = parseResult.GetBoolOption("--quiet", "-q")
        };
        return Assemble.WithOptions(parsedCliOptions);
    }
}
return 0;

Command ConfigureOptions()
{
    var rootCommand = new Command
    {
        RequiresInput = true
    };
    var noAssemblyOption = new Option
    {
        HelpText = "Suppress assembled bytes from listing.",
        Name = "no-assembly",
        ShortName = 'a',
        Type = OptionType.Boolean
    };
    var branchAlwaysOption = new Option
    {
        HelpText = "Enable (pseudo) `bra` for the 6502.",
        Name = "enable-branch-always",
        ShortName = 'b',
        Type = OptionType.Boolean
    };
    var pseudoBranch6502Option = new Option
    {
        HelpText = "Enable pseudo-long branches for 65xx CPUs.",
        Name = "enable-pseudo-long-branches",
        ShortName = 'P',
        Type = OptionType.Boolean
    };
    var caseSensitiveOption = new Option
    {
        HelpText = "Treat all symbols as case-sensitive.",
        Name = "case-sensitive",
        ShortName = 'C',
        Type = OptionType.Boolean
    };
    var cpuOption = new Option
    {
        HelpText = "Specify the target CPU and instruction set.",
        Name = "cpu",
        ShortName = 'c',
        Type = OptionType.String,
        ArgName = "arg"
    };
    var checksumOption = new Option
    {
        HelpText = "Display checksum information on assembly.",
        Name = "checksum",
        Type = OptionType.Boolean
    };
    var defineOption = new Option
    {
        HelpText = "Assign value to a global constant in <arg>.",
        Name = "define",
        ShortName = 'D',
        Type = OptionType.List,
        ArgName = "arg"
    };
    var lineNumberOption = new Option
    {
        HelpText = "Include line numbers in assembly listing.",
        Name = "line-numbers",
        Type = OptionType.Boolean
    };
    var noDisassemblyOption = new Option
    {
        HelpText = "Suppress disassembly from assembly listing.",
        Name = "no-disassembly",
        ShortName = 'd',
        Type = OptionType.Boolean
    };
    var dsectionsOption = new Option
    {
        HelpText = "Define one or more sections.",
        ArgName = "section",
        Name = "dsections",
        Type = OptionType.List
    };
    var echoEachPass = new Option
    {
        Name = "echo-each-pass",
        HelpText = "display output from `.echo` (deprecated)",
        Type = OptionType.Boolean,
        Deprecated = true
    };
    var errorOption = new Option
    {
        HelpText = "Dump errors to <file>.",
        ArgName = "file",
        Name = "error",
        ShortName = 'e',
        Type = OptionType.String
    };
    var formatOption = new Option
    {
        HelpText = "Specify binary output format.",
        Name = "format",
        ShortName = 'f',
        Type = OptionType.String,
        ArgName = "format"
    };
    var includePathOption = new Option
    {
        HelpText = "Include search path.",
        Name = "include-path",
        ShortName = 'I',
        Type = OptionType.String,
        ArgName = "path"
    };
    var labelsOption = new Option
    {
        HelpText = "Output label definitions to <arg>.",
        ShortName = 'l',
        Name = "labels",
        Type = OptionType.String,
        ArgName = "arg"
    };
    var labelsAddressesOnlyOption = new Option
    {
        HelpText = "Only include addresses in label definitions.",
        Name = "labels-addresses-only",
        Type = OptionType.Boolean
    };
    var viceLabelsOption = new Option
    {
        HelpText = "Output label listing to VICE debugger format.",
        Name = "vice-labels",
        Type = OptionType.Boolean
    };
    var legacyBlockOption = new Option
    {
        HelpText = "Recognize legacy block delimiters instead of braces.",
        Name = "legacy-blocks",
        Type = OptionType.Boolean
    };
    var listOption = new Option
    {
        ArgName = "file",
        HelpText = "The list of files",
        Name = "list",
        ShortName = 'L',
        Type = OptionType.String
    };
    var longAddressingOption = new Option
    {
        HelpText = "Support 24-bit (long) addressing mode (deprecated).",
        Name = "long-addressing",
        Type = OptionType.Boolean,
        Deprecated = true
    };
    var noHighlightingOption = new Option
    {
        HelpText = "Do not highlight causes of errors and warnings in sources.",
        Name = "no-highlighting",
        Type = OptionType.Boolean
    };
    var m16Option = new Option
    {
        HelpText = "Accumulator is 16-bit in 65816 mode.",
        Name = "m16",
        Type = OptionType.Boolean
    };
    var mx16Option = new Option
    {
        HelpText = "General registers are 16-bit in 65816 mode.",
        Name = "mx16",
        Type = OptionType.Boolean
    };
    var noStatsOption = new Option
    {
        HelpText = "Supress display of statistics from the assembly.",
        Name = "no-stats",
        Type = OptionType.Boolean,
        ShortName = 'n'
    };
    var outputOption = new Option
    {
        ArgName = "file",
        HelpText = "The output file",
        Name = "output",
        ShortName = 'o',
        Type = OptionType.String
    };
    var outputSectionOption = new Option
    {
        HelpText = "Output the specified section only to object file.",
        Name = "output-section",
        ArgName = "section",
        Type = OptionType.String
    };
    var patchOption = new Option
    {
        HelpText = "Patch the output file at <offset>",
        ArgName = "offset",
        ShortName = 'p',
        Name = "patch",
        Type = OptionType.Integer
    };
    var quietOption = new Option
    {
        HelpText = "Assemble in quiet mode (for console).",
        Name = "quiet",
        Type = OptionType.Boolean,
        ShortName = 'q'
    };
    var autosizeRegistersOption = new Option
    {
        HelpText = "Auto-size general registers in 65816 mode.",
        Name = "autosize-registers",
        Type = OptionType.Boolean,
        ShortName = 'r'
    };
    var resetPcOnBankOption = new Option
    {
        HelpText = "Reset program counter on `.bank` directive (deprecated).",
        Name = "reset-pc-on-bank",
        Type = OptionType.Boolean,
        Deprecated = true
    };
    var noSourceOption = new Option
    {
        HelpText = "Suppress original source from listing.",
        Name = "no-source",
        Type = OptionType.Boolean,
        ShortName = 's'
    };
    var truncateAssemblyOption = new Option
    {
        HelpText = "Truncate monitor bytes in listing.",
        Name = "truncate-assembly",
        Type = OptionType.Boolean,
        ShortName = 't'
    };
    var x16Option = new Option
    {
        HelpText = "Index registers are 16-bit in 65816 mode.",
        Name = "x16",
        Type = OptionType.Boolean
    };
    var verboseAsmOption = new Option
    {
        HelpText = "Verbose assembly assemblies.",
        Name = "verbose-asm",
        Type = OptionType.Boolean
    };
    var noWarnOption = new Option
    {
        HelpText = "Suppress all warnings.",
        Name = "no-warn",
        Type = OptionType.Boolean,
        ShortName = 'w'
    };
    var wallOption = new Option
    {
        HelpText = "Enable all warnings.",
        Name = "Wall",
        Type = OptionType.Boolean
    };
    var warnAmbiguousZpOption = new Option
    {
        Name = "Wambiguous-zp",
        HelpText = "Warn if opcode is zero/direct page or absolute addressing",
        Type = OptionType.Boolean
    };
    var warnCaseMismatch = new Option
    {
        HelpText = "Warn on symbol case mismatch.",
        Name = "Wcase-mismatch",
        Type = OptionType.Boolean
    };
    var warningsAsErrorsOption = new Option
    {
        HelpText = "Treat all warnings as errors.",
        Name = "Werror",
        Type = OptionType.Boolean
    };
    var warnAboutJumpBugOption = new Option
    {
        HelpText = "Warn about the `jmp` page crossing bug in 6502 mode.",
        Name = "Wjump-bug",
        Type = OptionType.Boolean
    };
    var warnLeftOption = new Option
    {
        HelpText = "Warn when a whitespace precedes a label.",
        Name = "Wleft",
        Type = OptionType.Boolean
    };
    var warnNotUnusedSections = new Option
    {
        HelpText = "Do not warn about unused sections.",
        Name = "Wno-unused-sections",
        Type = OptionType.Boolean
    };
    var warnNotBankCrossedOption = new Option
    {
        HelpText = "Do not warn about program counter crossing bank boundary.",
        Name = "Wno-bank-crossed",
        Type = OptionType.Boolean
    };
    var warnNotIntToFloatOption = new Option
    {
        HelpText = "Do not warn about precision loss converting large integers to floats.",
        Name = "Wno-int-to-float",
        Type = OptionType.Boolean
    };
    var warnRegisterAsIdentifier = new Option
    {
        HelpText = "Warn when a register is being used as an identifier.",
        Name = "Wregister-as-identifier",
        Type = OptionType.Boolean
    };
    var warnSimplifyCallReturn = new Option
    {
        HelpText = "Warn when a call and return can be simplified to a jump.",
        Name = "Wsimplify-call-return",
        Type = OptionType.Boolean
    };
    var warnAboutTextInNonTextPseudoOp = new Option
    {
        HelpText = "Warn about usage of text in primitive data pseudo-ops.",
        Name = "Wtext-in-non-text-pseudo-ops",
        Type = OptionType.Boolean
    };
    var warnUnreferencedSymbolsOption = new Option
    {
        HelpText = "Warn about unreferenced symbols.",
        Name = "Wunreferenced-symbols",
        Type = OptionType.Boolean
    };
    var warnOptimizeZ80AccToZeroOption = new Option
    {
        HelpText = "Warn when `ld a,0` can be optimized.",
        Name = "Woptimize-z80-acc-to-zero",
        Type = OptionType.Boolean
    };
    branchAlwaysOption.Validators.Add(result =>
    {
        ValidateCpuOptionIsInFamily("--enable-branch-always", Cpu.M6502, result);
    });
    pseudoBranch6502Option.Validators.Add(result =>
    {
        ValidateCpuOptionIsInFamily("--enable-pseudo-long-branches", Cpu.M6502, result);
    });
    warnAboutJumpBugOption.Validators.Add(result =>
    {
        ValidateCpuOptionIsInFamily("--Wjump-bug", Cpu.M6502, result);
    });
    warnOptimizeZ80AccToZeroOption.Validators.Add(result =>
    {
        ValidateCpuOptionIsInFamily("--Woptimize-z80-acc-to-zero", Cpu.Z80, result);
    });
    labelsAddressesOnlyOption.Validators.Add(result =>
    {
        if (result.Options.ContainsKey("--vice-labels") ||
            (!result.Options.ContainsKey("--labels") &&
             !result.Options.ContainsKey("-l")))
        {
            Console.WriteLine("Option `--labels-addresses-only` ignored.");
        }
    });
    lineNumberOption.Validators.Add(result =>
    {
        if (result.Options.ContainsKey("--verbose-asm"))
        {
            Console.WriteLine("Option `--line-numbers` is ignored since `--verbose-asm` option is set.");
        }
    });
    viceLabelsOption.Validators.Add(result =>
    {
        if (!result.Options.ContainsKey("--labels") &&
            !result.Options.ContainsKey("-l"))
        {
            Console.WriteLine("Option `--vice-labels` ignored.");
        }
    });
    rootCommand.AddOption(noAssemblyOption);
    rootCommand.AddOption(noDisassemblyOption);
    rootCommand.AddOption(autosizeRegistersOption);
    rootCommand.AddOption(branchAlwaysOption);
    rootCommand.AddOption(pseudoBranch6502Option);
    rootCommand.AddOption(caseSensitiveOption);
    rootCommand.AddOption(checksumOption);
    rootCommand.AddOption(cpuOption);
    rootCommand.AddOption(dsectionsOption);
    rootCommand.AddOption(defineOption);
    rootCommand.AddOption(errorOption);
    rootCommand.AddOption(echoEachPass);
    rootCommand.AddOption(formatOption);
    rootCommand.AddOption(includePathOption);
    rootCommand.AddOption(labelsOption);
    rootCommand.AddOption(labelsAddressesOnlyOption);
    rootCommand.AddOption(legacyBlockOption);
    rootCommand.AddOption(viceLabelsOption);
    rootCommand.AddOption(longAddressingOption);
    rootCommand.AddOption(m16Option);
    rootCommand.AddOption(mx16Option);
    rootCommand.AddOption(noStatsOption);
    rootCommand.AddOption(noHighlightingOption);
    rootCommand.AddOption(quietOption);
    rootCommand.AddOption(resetPcOnBankOption);
    rootCommand.AddOption(lineNumberOption);
    rootCommand.AddOption(noSourceOption);
    rootCommand.AddOption(outputSectionOption);
    rootCommand.AddOption(patchOption);
    rootCommand.AddOption(listOption);
    rootCommand.AddOption(outputOption);
    rootCommand.AddOption(truncateAssemblyOption);
    rootCommand.AddOption(verboseAsmOption);
    rootCommand.AddOption(noWarnOption);
    rootCommand.AddOption(wallOption);
    rootCommand.AddOption(warnAmbiguousZpOption);
    rootCommand.AddOption(warnCaseMismatch);
    rootCommand.AddOption(warnUnreferencedSymbolsOption);
    rootCommand.AddOption(warnRegisterAsIdentifier);
    rootCommand.AddOption(warnSimplifyCallReturn);
    rootCommand.AddOption(warnAboutTextInNonTextPseudoOp);
    rootCommand.AddOption(warningsAsErrorsOption);
    rootCommand.AddOption(warnAboutJumpBugOption);
    rootCommand.AddOption(warnLeftOption);
    rootCommand.AddOption(warnNotUnusedSections);
    rootCommand.AddOption(warnNotBankCrossedOption);
    rootCommand.AddOption(warnNotIntToFloatOption);
    rootCommand.AddOption(warnOptimizeZ80AccToZeroOption);
    rootCommand.AddOption(x16Option);

    var disasmCommand = new Command
    {
        Name = "disassemble",
        Description = "Disassemble input as machine code to source",
        RequiresInput = true
    };
    var disassemblyStart = new Option
    {
        HelpText = "Provide the disassembly start offset.",
        Name = "disassembly-start",
        ShortName = 's',
        Type = OptionType.Integer
    };
    var disassemblyEnd = new Option
    {
        HelpText = "Provide the disassembly end offset.",
        Name = "disassembly-end",
        ShortName = 'e',
        Type = OptionType.Integer
    };
    var disassemblyOffset = new Option
    {
        HelpText = "Provide the disassembly offset.",
        Name = "disassembly-offset",
        ShortName = 'O',
        Type = OptionType.Integer
    };
    disasmCommand.AddOption(cpuOption);
    disasmCommand.AddOption(formatOption);
    disasmCommand.AddOption(disassemblyStart);
    disasmCommand.AddOption(disassemblyEnd);
    disasmCommand.AddOption(disassemblyOffset);
    disasmCommand.AddOption(includePathOption);
    disasmCommand.AddOption(m16Option);
    disasmCommand.AddOption(mx16Option);
    disasmCommand.AddOption(outputOption);
    disasmCommand.AddOption(x16Option);

    formatOption.Validators.Add(result =>
    {
        if (result.Options.ContainsKey("--disassembly-start"))
        {
            Console.WriteLine("Option `--format` ignores `--disassembly-start`.");
        }
        if (result.Options.ContainsKey("--disassembly-end"))
        {
            Console.WriteLine("Option `--format` ignores `--disassembly-end`.");
        }
    });
    
    var buildFromConfigCommand = new Command
    {
        Name = "build",
        Description = "Build project from a build file",
        HideFromHelp = true,
        RequiresInput = false
    };
    
    var configCommand = new Command
    {
        Name = "config",
        Description = "Configure a build file from options",
        MetaInfo = "(in addition to options specified above)",
        RequiresInput = true
    };
    var buildFileOption = new Option
    {
        HelpText = "The build file name",
        Name = "build-file",
        ArgName = "file",
        Type = OptionType.String
    };
    buildFileOption.Validators.Add(result =>
    {
        if (result.GetStringOption("--build-file")?.EndsWith(".json") != true)
        {
            Console.WriteLine("Build file format is JSON but file extension is not.");
        }
    });
    configCommand.ShowHelpOnlyFor.Add("build-file");
    configCommand.AddOption(buildFileOption);
    configCommand.AddOption(noAssemblyOption);
    configCommand.AddOption(noDisassemblyOption);
    configCommand.AddOption(autosizeRegistersOption);
    configCommand.AddOption(branchAlwaysOption);
    configCommand.AddOption(pseudoBranch6502Option);
    configCommand.AddOption(caseSensitiveOption);
    configCommand.AddOption(checksumOption);
    configCommand.AddOption(cpuOption);
    configCommand.AddOption(dsectionsOption);
    configCommand.AddOption(defineOption);
    configCommand.AddOption(errorOption);
    configCommand.AddOption(echoEachPass);
    configCommand.AddOption(formatOption);
    configCommand.AddOption(includePathOption);
    configCommand.AddOption(labelsOption);
    configCommand.AddOption(labelsAddressesOnlyOption);
    configCommand.AddOption(legacyBlockOption);
    configCommand.AddOption(viceLabelsOption);
    configCommand.AddOption(longAddressingOption);
    configCommand.AddOption(m16Option);
    configCommand.AddOption(mx16Option);
    configCommand.AddOption(noStatsOption);
    configCommand.AddOption(noHighlightingOption);
    configCommand.AddOption(quietOption);
    configCommand.AddOption(resetPcOnBankOption);
    configCommand.AddOption(lineNumberOption);
    configCommand.AddOption(noSourceOption);
    configCommand.AddOption(outputSectionOption);
    configCommand.AddOption(patchOption);
    configCommand.AddOption(listOption);
    configCommand.AddOption(outputOption);
    configCommand.AddOption(truncateAssemblyOption);
    configCommand.AddOption(verboseAsmOption);
    configCommand.AddOption(noWarnOption);
    configCommand.AddOption(wallOption);
    configCommand.AddOption(warnAmbiguousZpOption);
    configCommand.AddOption(warnCaseMismatch);
    configCommand.AddOption(warnUnreferencedSymbolsOption);
    configCommand.AddOption(warnRegisterAsIdentifier);
    configCommand.AddOption(warnSimplifyCallReturn);
    configCommand.AddOption(warnAboutTextInNonTextPseudoOp);
    configCommand.AddOption(warningsAsErrorsOption);
    configCommand.AddOption(warnAboutJumpBugOption);
    configCommand.AddOption(warnLeftOption);
    configCommand.AddOption(warnNotUnusedSections);
    configCommand.AddOption(warnNotBankCrossedOption);
    configCommand.AddOption(warnNotIntToFloatOption);
    configCommand.AddOption(warnOptimizeZ80AccToZeroOption);
    configCommand.AddOption(x16Option);
    
    rootCommand.AddCommand(buildFromConfigCommand);
    rootCommand.AddCommand(disasmCommand);
    rootCommand.AddCommand(configCommand);
    return rootCommand;
}

static void ValidateCpuOptionIsInFamily(string optionName, Cpu family, ParseResult result)
{
    if ((result.Options.TryGetValue("--cpu", out var cpu) ||
         result.Options.TryGetValue("-c", out cpu)) &&
        !(CpuLookup.ByName(cpu.GetStringValue()) ?? family).IsInFamilyWith(family))
    {
        Console.WriteLine($"Argument `{optionName}` is not compatible with `--cpu` option `{cpu}` and will be ignored.");
    }
}
