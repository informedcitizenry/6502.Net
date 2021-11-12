//-----------------------------------------------------------------------------
// Copyright (c) 2017-2021 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using CommandLine;
using CommandLine.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Core6502DotNet
{
    /// <summary>
    /// A helper class to parse and present strongly-typed options from the 
    /// command-line.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class Options : Json.JsonSerializable
    {
        #region Subclasses

        /// <summary>
        /// Represents a strongly-typed listing options element.
        /// </summary>
        public sealed class Listing
        {
            #region Constructors
            /// <summary>
            /// Constructs a new instance of a listing options element.
            /// </summary>
            /// <param name="labelPath">The label filename.</param>
            /// <param name="listPath">The list filename.</param>
            /// <param name="noAssembly">The no-assembly flag.</param>
            /// <param name="noDisassembly">The no-disassembly flag.</param>
            /// <param name="noSource">The no-source flag.</param>
            /// <param name="truncateAssembly">Truncate assembled bytes in listing flag.</param>
            /// <param name="verbose">The verbose listing flag.</param>
            /// <param name="labelsAddressesOnly">The labels addresses only flag.</param>
            [JsonConstructor]
            public Listing(string labelPath,
                           string listPath,
                           bool noAssembly,
                           bool noDisassembly,
                           bool noSource,
                           bool truncateAssembly,
                           bool verbose,
                           bool labelsAddressesOnly)
            {
                LabelPath = labelPath;
                ListPath = listPath;
                NoAssembly = noAssembly;
                NoDisassembly = noDisassembly;
                NoSource = noSource;
                TruncateAssembly = truncateAssembly;
                VerboseList = verbose;
                LabelsAddressesOnly = labelsAddressesOnly;
            }
            #endregion

            #region Methods

            public static Listing Factory(string labelPath,
                           string listPath,
                           bool noAssembly,
                           bool noDisassembly,
                           bool noSource,
                           bool truncateAssembly,
                           bool verbose,
                           bool labelsAddressesOnly)
            {
                if (!string.IsNullOrEmpty(labelPath) || 
                    !string.IsNullOrEmpty(listPath) ||
                    noAssembly || 
                    noDisassembly || 
                    noSource ||
                    truncateAssembly || 
                    verbose || 
                    labelsAddressesOnly)
                    return new Listing(labelPath, 
                                       listPath, 
                                       noAssembly, 
                                       noDisassembly, 
                                       noSource, 
                                       truncateAssembly, 
                                       verbose, 
                                       labelsAddressesOnly);

                return null;
            }

            #endregion

            #region Properties

            /// <summary>
            /// Gets the label filename.
            /// </summary>
            public string LabelPath { get; }

            public bool LabelsAddressesOnly { get; }

            /// <summary>
            /// Gets the list filename.
            /// </summary>
            public string ListPath { get; }

            /// <summary>
            /// Gets the no-assembly flag.
            /// </summary>
            public bool NoAssembly { get; }

            /// <summary>
            /// Gets the no-disassembly flag.
            /// </summary>
            public bool NoDisassembly { get; }

            /// <summary>
            /// Gets the no-source flag.
            /// </summary>
            public bool NoSource { get; }

            /// <summary>
            /// Gets the truncate assembly flag.
            /// </summary>
            public bool TruncateAssembly { get; }

            /// <summary>
            /// Gets the verbose list flag.
            /// </summary>
            public bool VerboseList { get; }

            #endregion
        }

        /// <summary>
        /// Represents a strongly-typed logging options element.
        /// </summary>
        public sealed class Logging
        {
            #region Constructors

            /// <summary>
            /// Constructs a new instance of a logging options element.
            /// </summary>
            /// <param name="errorPath">The error filename.</param>
            /// <param name="echoEachPass">The echo each pass flag.</param>
            /// <param name="checksum">The checksum flag.</param>
            /// <param name="noStats">The no-stats flag.</param>
            /// <param name="noWarnings">The no-warnings flag.</param>
            /// <param name="quietMode">The quiet flag.</param>
            /// <param name="warningsAsErrors">The warnings-as-errors flag.</param>
            /// <param name="warnLeft">The warn left flag.</param>
            /// <param name="suppressUnusedSectionWarning">The suppress warning about unused section flag.</param>
            /// <param name="warnCaseMismatch">The warn on case mismatch flag.</param>
            [JsonConstructor]
            public Logging(string errorPath,
                           bool echoEachPass,
                           bool checksum,
                           bool noStats,
                           bool noWarnings,
                           bool quietMode,
                           bool warningsAsErrors,
                           bool warnLeft,
                           bool suppressUnusedSectionWarning,
                           bool warnCaseMismatch)
            {
                EchoEachPass = echoEachPass;
                ErrorPath = errorPath;
                Checksum = checksum;
                NoStats = noStats;
                NoWarnings = noWarnings;
                Quiet = quietMode;
                WarningsAsErrors = !noWarnings && warningsAsErrors;
                WarnLeft = warnLeft;
                SuppressUnusedSectionWarning = suppressUnusedSectionWarning;
                WarnCaseMismatch = warnCaseMismatch;
            }

            #endregion

            #region Methods

            public static Logging Factory(string errorPath,
                           bool echoEachPass,
                           bool checksum,
                           bool noStats,
                           bool noWarnings,
                           bool quietMode,
                           bool warningsAsErrors,
                           bool warnLeft,
                           bool suppressUnusedSectionWarning,
                           bool warnCaseMismatch)
            {
                if (!string.IsNullOrEmpty(errorPath) || 
                    echoEachPass ||
                    checksum ||
                    noStats || 
                    noWarnings || 
                    quietMode ||
                    warningsAsErrors || 
                    warnLeft ||
                    suppressUnusedSectionWarning || 
                    warnCaseMismatch)
                    return new Logging(errorPath,
                                        echoEachPass,
                                        checksum,
                                        noStats,
                                        noWarnings,
                                        quietMode,
                                        warningsAsErrors,
                                        warnLeft,
                                        suppressUnusedSectionWarning,
                                        warnCaseMismatch);
                return null;
            }

            #endregion

            #region Properties

            /// <summary>
            /// Gets the checksum flag.
            /// </summary>
            public bool Checksum { get; }

            /// <summary>
            /// Gets the echo each pass flag.
            /// </summary>
            public bool EchoEachPass { get; }

            /// <summary>
            /// Gets the error filename.
            /// </summary>
            public string ErrorPath { get; }

            /// <summary>
            /// Gets the no-warnings flag.
            /// </summary>
            public bool NoWarnings { get; }

            /// <summary>
            /// Gets the no-stats flag.
            /// </summary>
            public bool NoStats { get; }

            /// <summary>
            /// Gets the quiet flag.
            /// </summary>
            public bool Quiet { get; }

            /// <summary>
            /// Gets the warnings-as-errors flag.
            /// </summary>
            public bool WarningsAsErrors { get; }

            /// <summary>
            /// Gets the warn-left flag.
            /// </summary>
            public bool WarnLeft { get; }

            /// <summary>
            /// Gets the warn not about unused sections flag.
            /// </summary>
            public bool SuppressUnusedSectionWarning { get; }

            /// <summary>
            /// Gets the warn case mismatch flag.
            /// </summary>
            public bool WarnCaseMismatch { get; }

            #endregion
        }

        /// <summary>
        /// Represents a strongly-typed defined section element.
        /// </summary>
        public sealed class Section
        {
            #region Constructors

            /// <summary>
            /// Constructs a new instance of a section element.
            /// </summary>
            /// <param name="starts">The section starting address.</param>
            /// <param name="ends">The section ending address.</param>
            [JsonConstructor]
            public Section(object starts, object ends = null) =>
                (Starts, Ends) = (starts, ends);

            #endregion

            #region Methods

            public static IDictionary<string, Section> FromList(IList<string> sections)
            {
                if (sections == null)
                    return null; 
                var sectionDicts = new Dictionary<string, Section>();
                foreach (var section in sections)
                {
                    var parsed = Regex.Match(section,
                        @"^((""[^""]+"")|([a-zA-Z]\w*))(,((""[^""]+"")|('[^']+')|([^,]+)))(,((""[^""]+"")|('[^']+')|([^,]+)))?$");
                    if (!parsed.Success)
                    {
                        sectionDicts.Add(section, null);
                        continue;
                    }
                    var key = parsed.Groups[1].Value.TrimOnce('"');
                    var starts = parsed.Groups[5].Value;
                    object startValue;
                    if (int.TryParse(starts, out int startsNum))
                        startValue = startsNum;
                    else
                        startValue = starts;
                    if (parsed.Groups.Count >= 10)
                    {
                        var ends = parsed.Groups[10].Value;
                        object endValue;
                        if (int.TryParse(ends, out int endsNum))
                            endValue = endsNum;
                        else
                            endValue = string.IsNullOrEmpty(ends) ? null : ends;
                        sectionDicts.Add(key, new Section(startValue, endValue));
                    }
                    else
                    {
                        sectionDicts.Add(key, new Section(startValue));
                    }
                }
                return sectionDicts;
            }

            #endregion

            #region Properties

            /// <summary>
            /// Gets the section starting address.
            /// </summary>
            public object Starts { get; }

            /// <summary>
            /// Gets the section ending address.
            /// </summary>
            public object Ends { get; }

            #endregion
        }

        /// <summary>
        /// Represents a strongly-typed target options element.
        /// </summary>
        public sealed class Target
        {
            /// <summary>
            /// Constructs a new instance of the target options element.
            /// </summary>
            /// <param name="binaryFormat">The format.</param>
            /// <param name="cpu">The CPU.</param>
            /// <param name="autoSizeRegisters">Autosize registers for 65816 mode.</param>
            /// <param name="longAddressing">Long addressing supported.</param>
            /// <param name="branchAlways">Enable branch-always pseudo-mnemonic for 6502.</param>
            [JsonConstructor]
            public Target(string binaryFormat, 
                          string cpu, 
                          bool autoSizeRegisters, 
                          bool longAddressing,
                          bool branchAlways)
            {
                BinaryFormat = binaryFormat;
                Cpu = cpu;
                Autosize = autoSizeRegisters;
                LongAddressing = longAddressing;
                BranchAlways = branchAlways;
            }

            public static Target Factory(string binaryFormat,
                                         string cpu,
                                         bool autoSizeRegisters,
                                         bool longAddressing,
                                         bool branchAlways)
            {
                if (!string.IsNullOrEmpty(binaryFormat) || 
                    !string.IsNullOrEmpty(cpu) ||
                    autoSizeRegisters || 
                    longAddressing || 
                    branchAlways)
                    return new Target(binaryFormat, cpu, autoSizeRegisters, longAddressing, branchAlways);
                return null;
            }

            /// <summary>
            /// Gets the format.
            /// </summary>
            public string BinaryFormat { get; }

            /// <summary>
            /// Gets the CPU.
            /// </summary>
            public string Cpu { get; }

            /// <summary>
            /// Gets the branch always option.
            /// </summary>
            public bool BranchAlways { get; }

            /// <summary>
            /// Gets the autosize option.
            /// </summary>
            public bool Autosize { get; }

            /// <summary>
            /// Gets the long addressing option.
            /// </summary>
            public bool LongAddressing { get; }
        }

        #endregion

        #region Members

        IEnumerable<string> _passedArgs;

        [JsonProperty(PropertyName = "listingOptions")]
        readonly Listing _listing;

        [JsonProperty(PropertyName = "loggingOptions")]
        readonly Logging _logging;

        [JsonProperty(PropertyName = "sections")]
        readonly IDictionary<string, Section> _sections;

        [JsonProperty(PropertyName = "target")]
        readonly Target _target;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs an instance of the Options class.
        /// </summary>
        /// <param name="inputFiles">The input files.</param>
        /// <param name="noAssembly">The no-assembly flag.</param>
        /// <param name="allowOverflow">The allow overflow flag.</param>
        /// <param name="branchAlways">The branch-always flag.</param>
        /// <param name="caseSensitive">The case-sensitive flag.</param>
        /// <param name="createConfig">The create-config option.</param>
        /// <param name="cpu">The cpu option.</param>
        /// <param name="showChecksums">The show checksums flag.</param>
        /// <param name="configFile">The config filename.</param>
        /// <param name="labelDefines">The label defines.</param>
        /// <param name="labelsAddressesOnly">The label addresses only flag.</param>
        /// <param name="noDisassembly">The no-disassembly flag.</param>
        /// <param name="sections">The defined sections.</param>
        /// <param name="echoEachPass">The echo-each-pass flag.</param>
        /// <param name="errorFile">The error filename.</param>
        /// <param name="format">The format.</param>
        /// <param name="includePath">The include path.</param>
        /// <param name="ignoreColons">The ignore-colons flag.</param>
        /// <param name="labelFile">The label filename.</param>
        /// <param name="listingFile">The listing filename.</param>
        /// <param name="longAddressing">The long addressing flag.</param>
        /// <param name="noStats">The no-stats flag.</param>
        /// <param name="outputFile">The output filename.</param>
        /// <param name="outputSection">The section to output.</param>
        /// <param name="patch">The patch offset.</param>
        /// <param name="quiet">The quiet mode flag.</param>
        /// <param name="autoSize">The register autosize flag.</param>
        /// <param name="noSource">The no-source flag.</param>
        /// <param name="resetPcOnBank">The reset PC on bank flag.</param>
        /// <param name="truncateAssembly">The truncate assembly flag.</param>
        /// <param name="verboseList">The verbose listing flag.</param>
        /// <param name="noWarnings">The no warnings flag.</param>
        /// <param name="warningsAsErrors">The warnings as errors flag.</param>
        /// <param name="warnLeft">The warn left flag.</param>
        /// <param name="warnNotUnusedSections">The warn not about unused sections flag.</param>
        /// <param name="warnCaseMismatch"
        public Options(IList<string> inputFiles,
                       bool noAssembly,
                       bool allowOverflow,
                       bool branchAlways,
                       bool caseSensitive,
                       string createConfig,
                       string cpu,
                       bool showChecksums,
                       string configFile,
                       IList<string> labelDefines,
                       bool noDisassembly,
                       IList<string> sections,
                       bool echoEachPass,
                       string errorFile,
                       string format,
                       string includePath,
                       bool ignoreColons,
                       string labelFile,
                       bool labelsAddressesOnly,
                       string listingFile,
                       bool longAddressing,
                       bool noStats,
                       string outputFile,
                       string outputSection,
                       string patch,
                       bool quiet,
                       bool autoSize,
                       bool noSource,
                       bool resetPcOnBank,
                       bool truncateAssembly,
                       bool verboseList,
                       bool noWarnings,
                       bool warningsAsErrors,
                       bool warnLeft,
                       bool warnNotUnusedSections,
                       bool warnCaseMismatch) : this(inputFiles,
                                             Listing.Factory(labelFile,
                                                         listingFile,
                                                         noAssembly,
                                                         noDisassembly,
                                                         noSource,
                                                         truncateAssembly,
                                                         verboseList,
                                                         labelsAddressesOnly),
                                             Logging.Factory(errorFile,
                                                         echoEachPass,
                                                         showChecksums,
                                                         noStats,
                                                         noWarnings,
                                                         quiet,
                                                         warningsAsErrors,
                                                         warnLeft,
                                                         warnNotUnusedSections,
                                                         warnCaseMismatch),
                                             Section.FromList(sections),
                                             Target.Factory(format, 
                                                         cpu, 
                                                         autoSize, 
                                                         longAddressing,
                                                         branchAlways),
                                             labelDefines,
                                             caseSensitive,
                                             outputFile,
                                             outputSection,
                                             patch,
                                             includePath,
                                             ignoreColons,
                                             resetPcOnBank,
                                             allowOverflow)
        {
            if (!string.IsNullOrEmpty(configFile))
            {
                ConfigFile = configFile;
                OutputFile = string.Empty;
            }
            else
            {
                ConfigFile = string.Empty;
            }
            if (createConfig != null)
                CreateConfig = createConfig;
        }

        /// <summary>
        /// Constructs an instance of the Options class.
        /// </summary>
        /// <param name="listingOptions">The listing options.</param>
        /// <param name="loggingOptions">The logging options.</param>
        /// <param name="sections">The sections.</param>
        /// <param name="target">The target options.</param>
        /// <param name="defines">The defines.</param>
        /// <param name="sources">The input files.</param>
        /// <param name="caseSensitive">The case-sensitive flag.</param>
        /// <param name="outputFile">The output filename.</param>
        /// <param name="outputSection">The section to output.</param>
        /// <param name="patchOffset">The patch offset.</param>
        /// <param name="includePath">The include path.</param>
        /// <param name="ignoreColons">The ignore-colons flag.</param>
        /// <param name="resetPcOnBank">The reset PC on bank flag.</param>
        /// <param name="allowOverflow">The allow overflow flag.</param>
        [JsonConstructor]
        public Options(IList<string> sources,
                       Listing listingOptions,
                       Logging loggingOptions,
                       IDictionary<string, Section> sections,
                       Target target,
                       IList<string> defines,
                       bool caseSensitive,
                       string outputFile,
                       string outputSection,
                       string patchOffset,
                       string includePath,
                       bool ignoreColons,
                       bool resetPcOnBank,
                       bool allowOverflow)
        {
            _listing = listingOptions;
            _logging = loggingOptions;
            _target = target;
            CaseSensitive = caseSensitive;

            OutputFile = outputFile ?? "a.out";
            ConfigFile = null;
            IncludePath = includePath;
            IgnoreColons = ignoreColons;
            ResetPCOnBank = resetPcOnBank;
            OutputSection = outputSection == null ? null : $"\"{outputSection}\"";
            Patch = patchOffset;
            LabelDefines = GetReadOnlyList(defines);
            InputFiles = GetReadOnlyList(sources);
            _sections = sections;
            CreateConfig = null;
            AllowOverflow = allowOverflow;
            Sections = _sections?.Where(kvp => kvp.Value != null).Select(kvp =>
            {
                if (kvp.Value.Ends != null)
                    return $"\"{kvp.Key}\",{kvp.Value.Starts},{kvp.Value.Ends}";
                return $"\"{kvp.Key}\",{kvp.Value.Starts}";
            }).ToList().AsReadOnly();
        }

        #endregion

        #region Methods

        static ReadOnlyCollection<T> GetReadOnlyList<T>(IList<T> set)
            => set == null ? null : new ReadOnlyCollection<T>(set);


        static void CreateConfigFile(Options o)
        {
            var config = o.CreateConfig;
            var typeName = string.Empty;
            var jsonFormatted = string.Empty;
            var json = string.Empty;
            switch (config)
            {
                case "m":
                case "min":
                    json = ConfigConstants.CONFIG_MIN;
                    typeName = "-min";
                    break;
                case "f":
                case "full":
                    json = ConfigConstants.CONFIG_FULL;
                    typeName = "-full";
                    break;
                case "s":
                case "schema":
                    json = ConfigConstants.CONFIG_SCHEMA_202012;
                    typeName = "-schema";
                    break;
                case "a":
                case "args":
                    jsonFormatted = o.ToJson();
                    break;
                default:
                    throw new ArgumentException($"Invalid argument for option --createconfig: {config}.");
            }
            if (string.IsNullOrEmpty(jsonFormatted))
                jsonFormatted = ToFormattedJson(json);
            File.WriteAllText($"config{typeName}.json", jsonFormatted);
            throw new ArgumentException($"Config file \"config{typeName}.json\" created."); 
        }

        static void DisplayHelp<T>(ParserResult<T> result, IEnumerable<Error> errs)
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
            var heading = $"{Assembler.AssemblerNameSimple}\n{Assembler.AssemblerVersion}";
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

        /// <summary>
        /// Parses and fills an <see cref="Options"/>Options object from 
        /// passed command-line arguments.
        /// </summary>
        /// <param name="args">A collection of arguments to parse as arguments.</param>
        /// <returns>Returns an instance of the Options class whose properties are from 
        /// the passed arguments.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static Options FromArgs(IEnumerable<string> args)
        {
            try
            {
                Options options = null;
                var parser = new Parser(with =>
                {
                    with.HelpWriter = null;
                });
                var result = parser.ParseArguments<Options>(args);
                _ = result.WithParsed(o =>
                {
                    if (string.IsNullOrEmpty(o.ConfigFile))
                    {
                        options = o;
                        if (options._sections?.Any(kvp => kvp.Value == null) == true)
                        {
                            Console.WriteLine("Option '--dsection' has one more invalid parameters:");
                            foreach(var section in options._sections)
                            {
                                if (section.Value == null)
                                    Console.WriteLine(section.Key);
                            }
                            throw new ArgumentException("Unable to process options.");
                        }
                    }
                    else
                    {
                        var confIx = args.ToList().FindIndex(s => s.StartsWith("--config", StringComparison.Ordinal));
                        if (confIx != 0 && confIx != args.ToList().FindLastIndex(s => s.StartsWith('-')))
                            Console.WriteLine("Option --config ignores all other options.");
                        var validator = new Json.JsonValidator(ConfigConstants.CONFIG_SCHEMA_202012);
                        options = validator.ValidateAndDeserialize<Options>(o.ConfigFile, out var errors);
                        if (errors.Any())
                            throw new ArgumentException($"One or more errors in config file:\n{string.Join(Environment.NewLine, errors)}");
                    }
                })
                .WithNotParsed(errs => DisplayHelp(result, errs));
                if (!string.IsNullOrEmpty(options.CreateConfig) && string.IsNullOrEmpty(options.ConfigFile))
                    CreateConfigFile(options);
                options._passedArgs = args;
                return options;
            }
            catch (Exception ex)
            {
                if (ex is JsonException)
                    throw new JsonException($"Error parsing config file: {ex.Message}");
                throw;
            }
        }

        public override string ToString() => Parser.Default.FormatCommandLine(this);

        /// <summary>
        /// Gets the original arguments passed to the <see cref="Options"/> object.
        /// </summary>
        /// <returns>The passed command line arguments as a collection of <see cref="string"/> objects.</returns>
        public ReadOnlyCollection<string> GetPassedArgs() => _passedArgs?.ToList().AsReadOnly();

        #endregion

        #region Properties

        /// <summary>
        /// Gets the read-only list of input filenames.
        /// </summary>
        [JsonProperty(PropertyName = "sources")]
        [Value(0, Required = false, HelpText = "The source file(s) to assemble.", MetaName = "<inputs>.")]
        public ReadOnlyCollection<string> InputFiles { get; }

        /// <summary>
        /// Gets a flag indicating if assembly listing should suppress assembly bytes.
        /// </summary>
        [Option('a', "no-assembly", Required = false, HelpText = "Suppress assembled bytes from listing.")]
        public bool NoAssembly => _listing?.NoAssembly == true;

        /// <summary>
        /// Gets a flag indicating all pseudo-ops that output values (<c>.byte</c>, <c>.word</c>, etc.) should allow sign overflows.
        /// </summary>
        [JsonProperty]
        [Option("allow-overflow", Required = false, HelpText = "Allow value overflows for pseudo-op assembly.")]
        public bool AllowOverflow { get; }

        /// <summary>
        /// Gets the flag indicating that the BRA mnemonic should be enabled as a pseudo-instruction for 
        /// 6502 assembly.
        /// </summary>
        [Option('b', "enable-branch-always", Required = false, HelpText = "Enable (pseudo) 'bra' for the 6502.")]
        public bool BranchAlways => _target?.BranchAlways == true;

        /// <summary>
        /// Gets a flag that indicates the source should be processed as
        /// case-sensitive.
        /// </summary>
        [JsonProperty]
        [Option('C', "case-sensitive", Required = false, HelpText = "Treat all symbols as case-sensitive.")]
        public bool CaseSensitive { get; }

        /// <summary>
        /// Create a config template.
        /// </summary>
        [Option("createconfig", Required = false, HelpText = "Create a config file.", MetaValue = "{a|f|m|s}.")]
        public string CreateConfig { get; }

        /// <summary>
        /// Gets the selected CPU.
        /// </summary>
        /// <value>The cpu.</value>
        [Option('c', "cpu", Required = false, HelpText = "Specify the target CPU and instruction set.", MetaValue = "<arg>.")]
        public string CPU => _target?.Cpu;

        /// <summary>
        /// Gets a flag indicating that checksum information should be printed after 
        /// assembly.
        /// </summary>
        [Option("checksum", Required = false, HelpText = "Display checksum information on assembly.")]
        public bool ShowChecksums => _logging?.Checksum == true;

        /// <summary>
        /// Gets the config option file.
        /// </summary>
        [Option("config", Required = false, HelpText = "Load all settings from a configuration file.", MetaValue = "<file>.")]
        public string ConfigFile { get; }

        /// <summary>
        /// Gets the read-only list of label defines.
        /// </summary>
        [JsonProperty(PropertyName = "defines")]
        [Option('D', "define", Separator = ':', Required = false, HelpText = "Assign value to a global label in <args>.", MetaValue = "<arg>.")]
        public ReadOnlyCollection<string> LabelDefines { get; }

        /// <summary>
        /// Gets a flag indicating if assembly listing should suppress 6502 disassembly.
        /// </summary>
        [Option('d', "no-dissassembly", Required = false, HelpText = "Suppress disassembly from assembly listing.")]
        public bool NoDisassembly => _listing?.NoDisassembly == true;

        /// <summary>
        /// Gets the list of defined sections.
        /// </summary>
        [Option("dsections", Required = false, HelpText = "Define one or more sections.", MetaValue = "<sections>.")]
        public ReadOnlyCollection<string> Sections { get; }

        /// <summary>
        /// Gets the flag indicating whether to output ".echo" directive to console on each
        /// pass.
        /// </summary>
        [Option("echo-each-pass", Required = false, HelpText = "\".echo\" output on each pass.")]
        public bool EchoEachPass => _logging?.EchoEachPass == true;

        /// <summary>
        /// Gets the error filename.
        /// </summary>
        [Option('E', "error", Required = false, HelpText = "Dump errors to <file>.", MetaValue = "<file>.")]
        public string ErrorFile => _logging?.ErrorPath;

        /// <summary>
        /// Gets the target binary format.
        /// </summary>
        [Option('f', "format", Required = false, HelpText = "Specify binary output format.", MetaValue = "<format>.")]
        public string Format => _target?.BinaryFormat;

        /// <summary>
        /// Gets the path to search to include in sources.
        /// </summary>
        [Option('I', "include-path", Required = false, HelpText = "Include search path.", MetaValue = "<path>.")]
        public string IncludePath { get; }

        /// <summary>
        /// Gets a flag indicating that colons in semi-colon comments should be treated
        /// as comments.
        /// </summary>
        [JsonProperty]
        [Option("ignore-colons", Required = false, Default = null, HelpText = "Ignore colons in semi-colon comments.")]
        public bool IgnoreColons { get; }

        /// <summary>
        /// Gets the label listing filename.
        /// </summary>
        [Option('l', "labels", Required = false, HelpText = "Output label definitions to <arg>.", MetaValue = "<arg>.")]
        public string LabelFile => _listing?.LabelPath;

        /// <summary>
        /// Gets the label listing addresses only flag.
        /// </summary>
        [Option("labels-addresses-only", Required = false, HelpText = "Only include addresses in label definitions.")]
        public bool LabelsAddressesOnly => _listing?.LabelsAddressesOnly == true;

        /// <summary>
        /// Gets the assembly listing filename.
        /// </summary>
        [Option('L', "list", Required = false, HelpText = "Output listing to <file>.", MetaValue = "<file>.")]
        public string ListingFile => _listing?.ListPath ?? string.Empty;

        /// <summary>
        /// Gets the long addressing flag.
        /// </summary>
        [Option("long-addressing", Required = false, HelpText = "Support 24-bit (long) addressing mode.")]
        public bool LongAddressing => _target?.LongAddressing == true;

        /// <summary>
        /// Gets the flag indicating whether to display statistics after assembly.
        /// </summary>
        [JsonProperty]
        [Option('n', "no-stats", Required = false, HelpText = "Supress display of statistics from the assembly.")]
        public bool NoStats => _logging?.NoStats == true;

        /// <summary>
        /// Gets the output filename.
        /// </summary>
        [JsonProperty]
        [Option('o', "output", Required = false, HelpText = "Output assembly to <file>.", MetaValue = "<file>.")]
        public string OutputFile { get; }

        /// <summary>
        /// Gets the section to output to object file.
        /// </summary>
        [JsonProperty]
        [Option("output-section", Required = false, HelpText = "Output the specified section only to object file.")]
        public string OutputSection { get; }

        /// <summary>
        /// Indicates the output is a patch to an existing object file.
        /// </summary>
        [JsonProperty]
        [Option('p', "patch", Required = false, HelpText = "Patch the output file at <offset>.", MetaValue = "<offset>.")]
        public string Patch { get; }

        /// <summary>
        /// Gets the flag that indicates assembly should be quiet.
        /// </summary>
        [Option('q', "quiet", Required = false, HelpText = "Assemble in quiet mode (no console).")]
        public bool Quiet => _logging?.Quiet == true;

        /// <summary>
        /// Gets the flag that indicates whether to autosize the accumulator and index registers 
        /// in 65816 mode for the <c>rep</c> and <c>sep</c> instructions.
        /// </summary>
        [Option('r', "autosize-registers", Required = false, HelpText = "Auto-size .A and .X/.Y registers in 65816 mode.")]
        public bool Autosize => _target?.Autosize == true;

        /// <summary>
        /// Gets a flag indicating if the Program Counter should reset on bank switching.
        /// </summary>
        [JsonProperty]
        [Option("reset-pc-on-bank", Required = false, HelpText = "Reset the PC on '.bank' directive.")]
        public bool ResetPCOnBank { get; }

        /// <summary>
        /// Gets a flag indicating if assembly listing should suppress original source.
        /// </summary>
        [Option('s', "no-source", Required = false, HelpText = "Suppress original source from listing.")]
        public bool NoSource => _listing?.NoSource == true;

        /// <summary>
        /// Gets a flag indicating that assembly listing should truncate
        /// the number of bytes.
        /// </summary>
        [Option('t', "truncate-assembly", Required = false, HelpText = "Truncate assembled bytes in listing.")]
        public bool TruncateAssembly => _listing?.TruncateAssembly == true;

        /// <summary>
        /// Gets a flag indicating that assembly listing should be 
        /// verbose.
        /// </summary>
        [Option("verbose-asm", Required = false, HelpText = "Include all directives/comments in listing.")]
        public bool VerboseList => _listing?.VerboseList == true;

        /// <summary>
        /// Gets the flag that indicates warnings should be suppressed.
        /// </summary>
        [Option('w', "no-warn", Required = false, HelpText = "Suppress all warnings.")]
        public bool NoWarnings => _logging?.NoWarnings == true;

        /// <summary>
        /// Gets the flag that indicates the assembler should issue a warning on a symbol lookup which does
        /// not match the case of the definition.
        /// </summary>
        [Option("Wcase-mismatch", Required = false, HelpText = "Warn on symbol case mismatch.")]
        public bool WarnCaseMismatch => _logging?.WarnCaseMismatch == true;

        /// <summary>
        /// Gets a flag that treats warnings as errors.
        /// </summary>
        [Option("Werror", Required = false, HelpText = "Treat all warnings as errors.")]
        public bool WarningsAsErrors => _logging?.WarningsAsErrors == true;

        /// <summary>
        /// Gets a value indicating whether to suppress warnings for whitespaces 
        /// before labels.
        /// </summary>
        /// <value>If <c>true</c> warn left; otherwise, suppress the warning.</value>
        [Option("Wleft", Required = false, HelpText = "Warn when a whitespace precedes a label.")]
        public bool WarnLeft => _logging?.WarnLeft == true;

        /// <summary>
        /// Gets a flag indicating whether not to warn about unused sections.
        /// </summary>
        [Option("Wno-unused-sections", Required = false, HelpText = "Don't warn about unused sections.")]
        public bool WarnNotUnusedSections => _logging?.SuppressUnusedSectionWarning == true;


        [Usage(ApplicationAlias = "6502.Net.exe.")]
        public static IEnumerable<Example> Examples
        {
            get
            {
                yield return new Example("General", new UnParserSettings() { PreferShortName = true }, new Options(new string[] { "inputfile.asm" }, null, null, null, null, null, false, "output.bin", null, null, null, false, false, false));
                yield return new Example("From Config", new Options(null, false, false, false, false, null, null, false, "config.json", null, false, null, false, null, null, null, false, null, false, null, false, false, null, null, null, false, false, false, false, false, false, false, false, false, false, false));
            }
        }

        #endregion
    }
}