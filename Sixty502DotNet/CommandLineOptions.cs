//-----------------------------------------------------------------------------
// Copyright (c) 2017-2024 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System.Diagnostics;
using System.Reflection;
using CommandLine;
using CommandLine.Text;

namespace Sixty502DotNet.CLI;

/// <summary>
/// Represents a concrete representation of the accepted command line options.
/// </summary>
public sealed class CommandLineOptions
{
    /// <summary>
    /// Construct a new instance of a <see cref="CommandLineOptions"/> class.
    /// </summary>
    public CommandLineOptions()
    {
        OutputFile = "a.out";
    }

    private static void DisplayHelp<T>(ParserResult<T> result, IEnumerable<Error> errs)
    {
        bool isVersion = errs.IsVersion();
        var isHelp = errs.IsHelp();
        var errors = new List<string>();
        if (!isHelp && !isVersion)
        {
            var errorText = SentenceBuilder.Create().FormatError;
            foreach (var err in errs)
            {
                if (err is UnknownOptionError optionError)
                {
                    isVersion = optionError.Token.Equals("V");
                    isHelp = optionError.Token.Equals("?") || optionError.Token.Equals("h");
                    if (isVersion || isHelp)
                        break;
                }
                errors.Add(errorText(err));
            }
        }
        var versionInfo = FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly()!.Location);
        var versionName = $"Version {versionInfo.ProductMajorPart}.{versionInfo.ProductMinorPart} Build {versionInfo.ProductBuildPart}";

        var heading = $"{versionInfo.Comments}\n{versionName}";
        if (isVersion)
            throw new ArgumentException(heading);
        if (isHelp)
        {
            var helpText = HelpText.AutoBuild(result, h =>
            {
                h.MaximumDisplayWidth = 120;
                h.AdditionalNewLineAfterOption = false;
                h.AddEnumValuesToHelpText = false;
                h.AddPostOptionsLine("To log a defect, go to https://github.com/informedcitizenry/6502.Net/issues");
                h.Heading = heading;
                h.Copyright = string.Empty;
                return HelpText.DefaultParsingErrorsHandler(result, h);
            }, e => e);
            var ht = helpText.ToString().Replace("(pos. 0)", "        ")
                                        .Replace("--help                                Display this help screen.",
                                                 "-?, -h, --help                        Display this help screen.")
                                        .Replace("--version                             Display version information.",
                                                 "-V, --version                         Display version information.")
                                        .Replace("ERROR(S):\n  Option 'h' is unknown.", string.Empty)
                                        .Replace("ERROR(S):\r\n  Option 'h' is unknown.", string.Empty)
                                        .Replace("ERROR(S):\n  Option '?' is unknown.", string.Empty)
                                        .Replace("ERROR(S):\r\n  Option '?' is unknown.", string.Empty)
                                        .Replace("\r\n\r\nUSAGE", "USAGE")
                                        .Replace("\n\nUSAGE", "USAGE");
            throw new ArgumentException(ht);
        }
        Console.WriteLine("Option error(s):");
        var consoleColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Red;
        errors.ForEach(err => Console.Error.WriteLine($"  {err}"));
        Console.ForegroundColor = consoleColor;
        throw new ArgumentException("Try '--help' for usage.");
    }

    public static CommandLineOptions? FromArgs(string[] args)
    {
        CommandLineOptions? options = null;

        var parser = new Parser(with =>
        {
            with.HelpWriter = null;
        });
        var result = parser.ParseArguments<CommandLineOptions>(args);
        _ = result.WithParsed(o =>
        {
            options = o;
        })
        .WithNotParsed(errs => DisplayHelp(result, errs));
        return options;
    }

    /// <summary>
    /// Get a flag indicating if assembly listing should suppress assembly bytes.
    /// </summary>
    [Option('a', "no-assembly", Required = false, HelpText = "Suppress assembled bytes from listing.")]
    public bool NoAssembly { get; init; }

    /// <summary>
    /// Get the flag indicating that the BRA mnemonic should be enabled as a pseudo-instruction for 
    /// 6502 assembly.
    /// </summary>
    [Option('b', "enable-branch-always", Required = false, HelpText = "Enable (pseudo) 'bra' for the 6502.")]
    public bool BranchAlways { get; init; }

    /// <summary>
    /// Get a flag that indicates the source should be processed as
    /// case-sensitive.
    /// </summary>
    
    [Option('C', "case-sensitive", Required = false, HelpText = "Treat all symbols as case-sensitive.")]
    public bool CaseSensitive { get; init; }

    /// <summary>
    /// Create a config template.
    /// </summary>
    [Option("createconfig", Required = false, HelpText = "Create a config file from options.")]
    public bool CreateConfig { get; init; }

    /// <summary>
    /// Get the selected CPU.
    /// </summary>
    /// <value>The cpu.</value>
    [Option('c', "cpu", Required = false, HelpText = "Specify the target CPU and instruction set.", MetaValue = "<arg>")]
    public string? CPU { get; init; }

    /// <summary>
    /// Get a flag indicating that checksum information should be printed after 
    /// assembly.
    /// </summary>
    [Option("checksum", Required = false, HelpText = "Display checksum information on assembly.")]
    public bool ShowChecksums { get; init; }

    /// <summary>
    /// Get the config option file.
    /// </summary>
    [Option("config", Required = false, HelpText = "Load all settings from a configuration file.", MetaValue = "<file>")]
    public string? ConfigFile { get; init; }

    /// <summary>
    /// Get the read-only list of defined symbols.
    /// </summary>
    
    [Option('D', "define", Separator = ':', Required = false, HelpText = "Assign value to a global constant in <arg>.", MetaValue = "<arg>")]
    public IList<string>? Defines { get; init; }

    /// <summary>
    /// Get a flag indicating if assembly listing should suppress 6502 disassembly.
    /// </summary>
    [Option('d', "no-dissassembly", Required = false, HelpText = "Suppress disassembly from assembly listing.")]
    public bool NoDisassembly { get; init; }

    /// <summary>
    /// Get the disassemble option.
    /// </summary>
    [Option("disassemble", Required = false, HelpText = "Disassemble input files to output disassembly file.")]
    public bool Disassemble { get; init; }

    /// <summary>
    /// Get the disassembly start address option.
    /// </summary>
    [Option("disassembly-start", Required = false, HelpText = "Provide the disassembly start address.")]
    public int? DisassemblyStart { get; init; }

    /// <summary>
    /// Get the disassembly offset option.
    /// </summary>
    [Option("disassembly-offset", Required = false, HelpText = "Provide the disassembly offset.")]
    public int? DisassemblyOffset { get; init; }

    /// <summary>
    /// Get the list of defined sections.
    /// </summary>
    [Option("dsections", Required = false, HelpText = "Define one or more sections.", MetaValue = "<sections>")]
    public IList<string>? Sections { get; init; }

    /// <summary>
    /// Get the flag indicating whether to output ".echo" directive to console on each
    /// pass.
    /// </summary>
    [Option("echo-each-pass", Required = false, HelpText = "\".echo\" output on each pass.")]
    public bool EchoEachPass { get; init; }

    /// <summary>
    /// Get the error filename.
    /// </summary>
    [Option('e', "error", Required = false, HelpText = "Dump errors to <file>.", MetaValue = "<file>")]
    public string? ErrorFile { get; init; }

    /// <summary>
    /// Get the target binary format.
    /// </summary>
    [Option('f', "format", Required = false, HelpText = "Specify binary output format.", MetaValue = "<format>")]
    public string? Format { get; init; }

    /// <summary>
    /// Get the path to search to include in sources.
    /// </summary>
    [Option('I', "include-path", Required = false, HelpText = "Include search path.", MetaValue = "<path>")]
    public string? IncludePath { get; init; }

    /// <summary>
    /// Get the label listing filename.
    /// </summary>
    [Option('l', "labels", Required = false, HelpText = "Output label definitions to <arg>.", MetaValue = "<arg>")]
    public string? LabelFile { get; init; }

    /// <summary>
    /// Get the label listing addresses only flag.
    /// </summary>
    [Option("labels-addresses-only", Required = false, HelpText = "Only include addresses in label definitions.")]
    public bool LabelsAddressesOnly { get; init; }

    /// <summary>
    /// Get the VICE label listing only flag.
    /// </summary>
    [Option("vice-labels", Required = false, HelpText = "Output label listing to VICE debugger format.")]
    public bool ViceLabels { get; init; }

    /// <summary>
    /// Get the assembly listing filename.
    /// </summary>
    [Option('L', "list", Required = false, HelpText = "Output listing to <file>.", MetaValue = "<file>")]
    public string? ListingFile { get; init; }

    /// <summary>
    /// Get the long addressing flag.
    /// </summary>
    [Option("long-addressing", Required = false, HelpText = "Support 24-bit (long) addressing mode.")]
    public bool LongAddressing { get; init; }

    /// <summary>
    /// Get the flag indicating whether to highlight causes of errors and warnings in source.
    /// </summary>
    [Option("no-highlighting", Required = false, HelpText = "Do not highlight causes of errors and warnings in source.")]
    public bool NoHighlighting { get; init; }

    /// <summary>
    /// Get the flag indicating whether to display statistics after assembly.
    /// </summary>
    [Option('n', "no-stats", Required = false, HelpText = "Supress display of statistics from the assembly.")]
    public bool NoStats { get; init; }

    /// <summary>
    /// Get the output filename.
    /// </summary>
    [Option('o', "output", Required = false, HelpText = "Output assembly to <file>.", MetaValue = "<file>.")]
    public string OutputFile { get; init; }

    /// <summary>
    /// Get the section to output to object file.
    /// </summary>    
    [Option("output-section", Required = false, HelpText = "Output the specified section only to object file.")]
    public string? OutputSection { get; init; }
    /// <summary>
    /// Indicates the output is a patch to an existing object file.
    /// </summary>
    [Option('p', "patch", Required = false, HelpText = "Patch the output file at <offset>.", MetaValue = "<offset>.")]
    public string? Patch { get; init; }
    /// <summary>
    /// Get the flag that indicates assembly should be quiet.
    /// </summary>
    [Option('q', "quiet", Required = false, HelpText = "Assemble in quiet mode (no console).")]
    public bool Quiet { get; init; }

    /// <summary>
    /// Get the flag that indicates whether to autosize the accumulator and index registers 
    /// in 65816 mode for the <c>rep</c> and <c>sep</c> instructions.
    /// </summary>
    [Option('r', "autosize-registers", Required = false, HelpText = "Auto-size .A and .X/.Y registers in 65816 mode.")]
    public bool Autosize { get; init; }

    /// <summary>
    /// Get a flag indicating if the Program Counter should reset on bank switching.
    /// </summary>
    [Option("reset-pc-on-bank", Required = false, HelpText = "Reset the PC on '.bank' directive.")]
    public bool ResetPCOnBank { get; init; }

    /// <summary>
    /// Get a flag indicating if assembly listing should suppress original source.
    /// </summary>
    [Option('s', "no-source", Required = false, HelpText = "Suppress original source from listing.")]
    public bool NoSource { get; init; }

    /// <summary>
    /// Get a flag indicating that assembly listing should truncate
    /// the number of bytes.
    /// </summary>
    [Option('t', "truncate-assembly", Required = false, HelpText = "Truncate assembled bytes in listing.")]
    public bool TruncateAssembly { get; init; }

    /// <summary>
    /// Get a flag indicating that assembly listing should be 
    /// verbose.
    /// </summary>
    [Option("verbose-asm", Required = false, HelpText = "Include all directives/comments in listing.")]
    public bool VerboseList { get; init; }

    /// <summary>
    /// Get the flag that indicates warnings should be suppressed.
    /// </summary>
    [Option('w', "no-warn", Required = false, HelpText = "Suppress all warnings.")]
    public bool NoWarnings { get; init; }

    /// <summary>
    /// Get the flag indicating all other warning options are enabled.
    /// </summary>
    [Option("Wall", Required = false, HelpText = "Enable all warnings.")]
    public bool EnableAllWarnings { get; init; }

    /// <summary>
    /// Get the flag that indicates the assembler should issue a warning on a symbol lookup which does
    /// not match the case of the definition.
    /// </summary>
    [Option("Wcase-mismatch", Required = false, HelpText = "Warn on symbol case mismatch.")]
    public bool WarnCaseMismatch { get; init; }

    /// <summary>
    /// Get a flag that treats warnings as errors.
    /// </summary>
    [Option("Werror", Required = false, HelpText = "Treat all warnings as errors.")]
    public bool WarningsAsErrors { get; init; }

    /// <summary>
    /// Get the flag that indicates the assembler should issue a warning when the JMP bug.
    /// </summary>
    [Option("Wjump-bug", Required = false, HelpText = "Warn about the JMP bug in 6502 mode.")]
    public bool WarnAboutJumpBug { get; init; }

    /// <summary>
    /// Get a value indicating whether to suppress warnings for whitespaces 
    /// before labels.
    /// </summary>
    /// <value>If <c>true</c> warn left; otherwise, suppress the warning.</value>
    [Option("Wleft", Required = false, HelpText = "Warn when a whitespace precedes a label.")]
    public bool WarnLeft { get; init; }

    /// <summary>
    /// Get a flag indicating whether not to warn about unused sections.
    /// </summary>
    [Option("Wno-unused-sections", Required = false, HelpText = "Don't warn about unused sections.")]
    public bool WarnNotUnusedSections { get; init; }

    /// <summary>
    /// Get a flag indicating whether to warn about registers being used as identifiers.
    /// </summary>
    [Option("wregister-as-identifier", Required =false, HelpText = "Warn when a register is being used as an identifier.")]
    public bool WarnRegistersAsIdentifiers { get; init; }

    /// <summary>
    /// Get the flag indicating whether to warn whether a call and return can be simplified to a jump.
    /// </summary>
    [Option("Wsimplify-call-return", Required = false, HelpText = "Warn when call and return can be simplified to jump.")]
    public bool WarnSimplifyCallReturn { get; init; }

    /// <summary>
    /// Get the flag that warns when a non- .Xstring pseudo op contains text arguments.
    /// </summary>
    [Option("Wtext-in-non-text-pseudo-ops", Required = false, HelpText = "Warn about usage of text in pseudo-ops.")]
    public bool WarnAboutUsingTextInNonTextPseudoOps { get; init; }

    /// <summary>
    /// Get the flag that warns about unreferenced symbols.
    /// </summary>
    [Option("Wunreferenced-symbols", Required = false, HelpText = "Warn about unreferenced symbols.")]
    public bool WarnUnreferencedSymbols { get; init; }

    /// <summary>
    /// Get the read-only list of input filenames.
    /// </summary>
    [Value(0, Required = false, HelpText = "The source file(s) to assemble.", MetaName = "<inputs>")]
    public IList<string>? InputFiles { get; init; }

    [Usage(ApplicationAlias = "6502.Net.exe")]
    public static IEnumerable<Example> Examples
    {
        get
        {
            yield return new Example("General",
                new UnParserSettings() { PreferShortName = true },
                new CommandLineOptions()
                {
                    InputFiles = ["inputfile.asm"],
                    OutputFile = "output.bin"
                });
        }
    }
}

