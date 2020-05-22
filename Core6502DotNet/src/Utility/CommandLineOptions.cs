//-----------------------------------------------------------------------------
// Copyright (c) 2017-2020 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Core6502DotNet
{
    /// <summary>
    /// A helper class to parse and present strongly-typed options from the 
    /// command-line.
    /// </summary>
    public class CommandLineOptions
    {
        #region Definition

        enum Options
        {
            None,
            CaseSensitive,
            Defines,
            IncludePath,
            OutputFile,
            Sources,
            ListingOptions,
            LabelPath,
            ListPath,
            NoAssembly,
            NoDisassembly,
            NoSource,
            Verbose,
            LoggingOptions,
            Checksum,
            ErrorPath,
            NoWarnings,
            QuietMode,
            WarningsAsErrors,
            WarnLeft,
            Target,
            BinaryFormat,
            Cpu
        };

        enum OptionType
        {
            Boolean,
            String,
            Array,
            Object
        };

        static readonly Dictionary<string, HashSet<Options>> _optionSchema = new Dictionary<string, HashSet<Options>>
        {
            { "/",              new HashSet<Options>
                                {
                                    Options.CaseSensitive,
                                    Options.Defines,
                                    Options.IncludePath,
                                    Options.ListingOptions,
                                    Options.LoggingOptions,
                                    Options.OutputFile,
                                    Options.Sources,
                                    Options.Target
                                }
            },
            { "listingOptions", new HashSet<Options>
                                {
                                    Options.LabelPath,
                                    Options.ListPath,
                                    Options.NoAssembly,
                                    Options.NoDisassembly,
                                    Options.NoSource,
                                    Options.Verbose
                                }
            },
            { "loggingOptions", new HashSet<Options>
                                {
                                    Options.Checksum,
                                    Options.ErrorPath,
                                    Options.NoWarnings,
                                    Options.QuietMode,
                                    Options.WarningsAsErrors,
                                    Options.WarnLeft
                                }
            },
            { "target",         new HashSet<Options>
                                {
                                    Options.BinaryFormat,
                                    Options.Cpu
                                }
            }
        };

        static readonly Dictionary<Options, OptionType> _optionTypes = new Dictionary<Options, OptionType>
        {
            { Options.CaseSensitive,      OptionType.Boolean },
            { Options.Defines,            OptionType.Array   },
            { Options.IncludePath,        OptionType.String  },
            { Options.ListingOptions,     OptionType.Object  },
            { Options.LoggingOptions,     OptionType.Object  },
            { Options.OutputFile,         OptionType.String  },
            { Options.Sources,            OptionType.Array   },
            { Options.Target,             OptionType.Object  },
            { Options.LabelPath,          OptionType.String  },
            { Options.ListPath,           OptionType.String  },
            { Options.NoAssembly,         OptionType.Boolean },
            { Options.NoDisassembly,      OptionType.Boolean },
            { Options.NoSource,           OptionType.Boolean },
            { Options.Verbose,            OptionType.Boolean },
            { Options.Checksum,           OptionType.Boolean },
            { Options.ErrorPath,          OptionType.String  },
            { Options.NoWarnings,         OptionType.Boolean },
            { Options.QuietMode,          OptionType.Boolean },
            { Options.WarningsAsErrors,   OptionType.Boolean },
            { Options.WarnLeft,           OptionType.Boolean },
            { Options.BinaryFormat,       OptionType.String  },
            { Options.Cpu,                OptionType.String  }
        };

        static readonly Dictionary<Options, OptionType> _arrayTypes = new Dictionary<Options, OptionType>
        {
            { Options.Defines, OptionType.String },
            { Options.Sources, OptionType.String }
        };

        static readonly Dictionary<string, Options> _optionAlias = new Dictionary<string, Options>
        {
            { "-a",               Options.NoAssembly },
            { "--no-assembly",    Options.NoAssembly },
            { "-C",               Options.CaseSensitive },
            { "--case-sensitive", Options.CaseSensitive },
            { "-c",               Options.Cpu },
            { "--cpu",            Options.Cpu },
            { "-D",               Options.Defines },
            { "--define",         Options.Defines },
            { "-d",               Options.NoDisassembly },
            { "--no-disassembly", Options.NoDisassembly },
            { "-E",               Options.ErrorPath },
            { "--error",          Options.ErrorPath },
            { "--format",         Options.BinaryFormat },
            { "-I",               Options.IncludePath },
            { "--include-path",   Options.IncludePath },
            { "-L",               Options.ListPath },
            { "--list",           Options.ListPath },
            { "-l",               Options.LabelPath },
            { "--labels",         Options.LabelPath },
            { "-o",               Options.OutputFile },
            { "--output",         Options.OutputFile },
            { "-q",               Options.QuietMode },
            { "--quiet",          Options.QuietMode },
            { "-s",               Options.NoSource },
            { "--no-source",      Options.NoSource },
            { "--verbose-asm",    Options.Verbose },
            { "-w",               Options.NoWarnings },
            { "--no-warn",        Options.NoWarnings },
            { "--werror",         Options.WarningsAsErrors },
            { "--wleft",          Options.WarnLeft }
        };

        static readonly string _helpUsage = " Try '-?|-h|help' for usage.";

        static readonly string _helpList =
            "Usage: {0} [options...] <inputs> [output]\n\n" +
            "    -a, --no-assembly        Suppress assembled bytes from assembly\n" +
            "    -C, --case-sensitive     Treat all symbols as case-sensitive\n" +
            "    --checksum               Display checksum information on assembly\n" +
            "    -c, --cpu <arg>          Specify the target CPU and instruction set\n" +
            "    --config <arg>           Load all settings from a configuration file\n" +
            "    -D, --define <args>      Assign value to a global symbol/label in\n" +
            "                             <args>\n" +
            "    -d, --no-dissassembly    Suppress disassembly from assembly listing\n" +
            "    -E, --error              Dump errors to <arg>\n" +
            "    --format, --arch <arg>   Specify binary output format\n" +
            "    -I, --include-path <arg> Include search path <arg>\n" +
            "    -l, --labels <arg>       Output label definitions to <arg>\n" +
            "    -L, --list <arg>         Output listing to <arg>\n" +
            "    -o, --output <arg>       Output assembly to <arg>\n" +
            "    -q, --quiet              Assemble in quiet mode (no console\n" +
            "    -s, --no-source          Suppress original source from assembly\n" +
            "                             listing\n" +
            "    -V, --version            Print current version\n" +
            "    --verbose-asm            Expand listing to include all directives\n" +
            "                             and comments\n" +
            "    -w, --no-warn            Suppress all warnings\n" +
            "    --werror                 Treat all warnings as error\n" +
            "    --wleft                  Issue warnings about whitespaces before\n" +
            "                             labels\n" +
            "    <inputs>                 The source files to assemble";

        #endregion

        #region Members

        readonly List<string> _source;
        readonly List<string> _defines;
        bool _werror;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a new instance of the AsmCommandLineOptions.
        /// </summary>
        public CommandLineOptions()
        {
            _source = new List<string>();
            _defines = new List<string>();
            Format =
            CPU =
            ErrorFile =
            IncludePath =
            ListingFile =
            LabelFile = string.Empty;
            OutputFile = "a.out";
            ShowChecksums =
            VerboseList =
            _werror =
            NoWarnings =
            WarnLeft =
            NoDissasembly =
            NoSource =
            NoAssembly =
            Quiet =
            PrintVersion =
            CaseSensitive = false;
        }

        #endregion

        #region Methods

        static string GetHelpText() =>
            string.Format(_helpList, Assembly.GetEntryAssembly().GetName().Name);

        static string GetVersion() =>
            $"{Assembler.AssemblerNameSimple}\n{Assembler.AssemblerVersion}";

        /// <summary>
        /// Process the command-line arguments passed by the end-user.
        /// </summary>
        /// <param name="passedArgs">The argument array.</param>
        public void ParseArgs(string[] passedArgs)
        {
            if (passedArgs.Length == 0)
                throw new Exception($"One or more arguments expected.{_helpUsage}");

            // primarily we're interested in clubbing assignment arguments together, so 
            // { "<option>", "=", "<arg>" } => "<option>=<arg>"
            var args = new List<string>();
            for (var i = 0; i < passedArgs.Length; i++)
            {
                if (passedArgs[i] == "=")
                {
                    if (i == 0 || i == passedArgs.Length - 1)
                        throw new Exception(GetHelpText());
                    args[i - 1] += '=' + passedArgs[++i];
                }
                else
                {
                    args.Add(passedArgs[i]);
                }
            }
            Arguments = args;
            for (var i = 0; i < args.Count; i++)
            {
                var arg = args[i];
                if (arg[0] == '-')
                {
                    var nextArg = string.Empty;
                    var eqix = arg.IndexOf('=');

                    if (eqix > -1)
                    {
                        if (eqix == arg.Length - 1)
                            throw new Exception($"Expected argument for option \'{arg}\'.{_helpUsage}");
                        nextArg = arg.Substring(eqix + 1);
                        arg = arg.Substring(0, eqix);
                    }
                    else
                    {
                        if (i < args.Count - 1 && args[i + 1][0] != '-')
                            nextArg = args[i + 1];
                    }
                    var optionName = GetOptionName(arg);
                    switch (optionName)
                    {
                        case "-?":
                        case "-h":
                        case "--help":
                            throw new Exception(GetHelpText());
                        case "-V":
                        case "--version":
                            throw new Exception(GetVersion());
                        case "--config":
                            if (args.Count > 1 && (i > 0 || i < args.FindLastIndex(a => a[0] == '-')))
                                Console.WriteLine("Option --config ignores all other options.");
                            SetOptionsFromConfig(nextArg);
                            return;
                        default:
                            break;
                    }

                    if (!_optionAlias.ContainsKey(optionName))
                        throw new Exception($"Invalid option '{optionName}'.{_helpUsage}");
                    var parsedOption = _optionAlias[optionName];
                    var type = _optionTypes[parsedOption];
                    if (type == OptionType.Boolean)
                    {
                        SetFlag(parsedOption, true);
                    }
                    else
                    {
                        if (type == OptionType.String)
                        {
                            SetOptionValue(parsedOption, nextArg);
                            if (eqix == -1)
                                i++;
                        }
                        else
                        {
                            _defines.Add(nextArg);
                            while (i + 1 < args.Count && args[i + 1][0] != '-')
                                _defines.Add(args[++i]);
                        }
                    }
                }
                else
                {
                    i = SetInputFiles(args, i);
                }
            }

            if (_source.Count == 0)
                throw new Exception($"One or more input files was expected.{_helpUsage}");
        }

        int SetInputFiles(List<string> args, int index)
        {
            if (_source.Count > 0)
                throw new Exception($"Invalid option '{args[index]}'.{_helpUsage}");
            while (index < args.Count)
            {
                if (args[index][0] == '-')
                    break;
                _source.Add(args[index++]);
            }
            return index - 1;
        }

        static string GetOptionName(string argument)
        {
            int index = 0, length = 0;
            foreach (var c in argument)
            {
                if (c == '-')
                {
                    if (length == 0) // is it a -OPTION
                        index++;
                    else
                        length++;    // must be a --OPT-ION
                }
                else if (c != '=')
                {
                    length++;
                }
                else
                {
                    break;
                }
            }
            if (length > 0)
                return argument.Substring(0, index + length);
            return string.Empty;
        }

        void SetOptionsFromConfig(string configFile)
        {
            try
            {
                string json = File.ReadAllText(configFile).Replace("\r", string.Empty);
                var tokens = LexerParser.TokenizeJson(json);
                if (tokens.Children.Count != 1 || !tokens.Children[0].Name.Equals("{"))
                    throw new Exception("Invalid configuration file.");

                SetChildOptionsFromConfig("/", tokens.Children[0]);
            }
            catch (FileNotFoundException)
            {
                Console.Error.WriteLine($"Config file \"{configFile}\" not found.");
            }
        }

        void SetChildOptionsFromConfig(string parent, Token token)
        {
            var atIdent = true;
            var expectingcolon = !atIdent;
            var expectedType = OptionType.Boolean;
            var childOptions = token.Children;
            string ident = string.Empty;
            Options parsedOption = Options.None;
            foreach (var option in childOptions)
            {
                var optionFirst = option.Name[0];
                if (atIdent)
                {
                    if (option.Type != TokenType.Operand && !option.Name.EnclosedInDoubleQuotes())
                        throw new ExpressionException(option.Position, "Option must be a string.");
                    if (expectingcolon)
                        throw new ExpressionException(option.Position, "Expected ':'.");
                    ident = option.Name.TrimOnce(optionFirst);
                    if (!Enum.TryParse<Options>(ident.ToFirstUpper(), out parsedOption) || !_optionSchema[parent].Contains(parsedOption))
                        throw new ExpressionException(option.Position, $"Option '{ident}' not valid.");
                    expectedType = _optionTypes[parsedOption];
                    atIdent = false;
                    expectingcolon = !atIdent;
                }
                else if (expectingcolon)
                {
                    if (option.OperatorType != OperatorType.Separator && !option.Name.Equals(":"))
                        throw new ExpressionException(option.Position, "Expected ':'.");
                    expectingcolon = false;
                }
                else
                {
                    var exception = $"Option '{ident}' expects a value of type '{expectedType}'.";
                    switch (optionFirst)
                    {
                        case '"':
                            if (expectedType != OptionType.String || !option.Name.EnclosedInDoubleQuotes())
                                throw new ExpressionException(option.Position, exception);
                            SetOptionValue(parsedOption, option.Name.TrimOnce(optionFirst));
                            break;
                        case '{':
                            if (expectedType != OptionType.Object)
                                throw new ExpressionException(option.Position, exception);
                            SetChildOptionsFromConfig(ident, option);
                            break;
                        case '[':
                            if (expectedType != OptionType.Array)
                                throw new ExpressionException(option.Position, exception);
                            SetOptionValuesFromConfig(parsedOption, option.Children);
                            break;
                        case ',':
                            if (atIdent)
                                throw new ExpressionException(option.Position, $"Expected identifier.");
                            atIdent = true;
                            break;
                        default:
                            if (expectedType != OptionType.Boolean || (!option.Name.Equals("true") && !option.Name.Equals("false")))
                                throw new ExpressionException(option.Position, exception);
                            SetFlag(parsedOption, option.Name.Equals("true"));
                            break;
                    }
                }
            }
        }

        void SetFlag(Options option, bool value)
        {
            switch (option)
            {
                case Options.CaseSensitive:      CaseSensitive  = value; break;
                case Options.Checksum:           ShowChecksums  = value; break;
                case Options.NoAssembly:         NoAssembly     = value; break;
                case Options.NoDisassembly:      NoDissasembly  = value; break;
                case Options.NoSource:           NoSource       = value; break;
                case Options.NoWarnings:         NoWarnings     = value; break;
                case Options.QuietMode:          Quiet          = value; break;
                case Options.Verbose:            VerboseList    = value; break;
                case Options.WarningsAsErrors:   _werror        = value; break;
                case Options.WarnLeft:           WarnLeft       = value; break;
            }
        }

        void SetOptionValue(Options option, string value)
        {
            switch (option)
            {
                case Options.IncludePath:        IncludePath    = value; break;
                case Options.OutputFile:         OutputFile     = value; break;
                case Options.LabelPath:          LabelFile      = value; break;
                case Options.ListPath:           ListingFile    = value; break;
                case Options.ErrorPath:          ErrorFile      = value; break;
                case Options.BinaryFormat:       Format         = value; break;
                case Options.Cpu:                CPU            = value; break;
            }
        }
        void SetOptionValuesFromConfig(Options ident, IEnumerable<Token> array)
        {
            var type = _arrayTypes[ident];
            bool commaNeeded = false;
            foreach (var option in array)
            {
                var optionValue = option.Name;
                if (commaNeeded)
                {
                    if (!optionValue.Equals(","))
                        throw new ExpressionException(option.Position, $"Invalid token \"{optionValue}\".");
                    commaNeeded = false;
                }
                else
                {
                    if (type == OptionType.String)
                    {
                        if (!optionValue.EnclosedInDoubleQuotes())
                        {
                            if (!char.IsLetterOrDigit(optionValue[0]))
                                throw new ExpressionException(option.Position, $"Invalid token \"{optionValue}\".");
                            throw new ExpressionException(option.Position, $"Value \"{optionValue}\" was not a string.");
                        }
                        if (ident == Options.Defines)
                            _defines.Add(optionValue.TrimOnce(optionValue[0]));
                        else
                            _source.Add(optionValue.TrimOnce(optionValue[0]));
                    }
                    commaNeeded = true;
                }
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the argument string array passed.
        /// </summary>
        public IEnumerable<string> Arguments { get; set; }

        /// <summary>
        /// Gets or sets the target architecture information.
        /// </summary>
        public string Format { get; set; }

        /// <summary>
        /// Gets the selected CPU.
        /// </summary>
        /// <value>The cpu.</value>
        public string CPU { get; private set; }

        /// <summary>
        /// Gets the error filename.
        /// </summary>
        public string ErrorFile { get; private set; }

        /// <summary>
        /// Gets the path to search to include in sources.
        /// </summary>
        public string IncludePath { get; private set; }

        /// <summary>
        /// Gets the read-only list of input filenames.
        /// </summary>
        public IReadOnlyList<string> InputFiles => _source;

        /// <summary>
        /// Gets the read-only list of label defines.
        /// </summary>
        public IReadOnlyList<string> LabelDefines => _defines;

        /// <summary>
        /// Gets the output filename.
        /// </summary>
        public string OutputFile { get; private set; }

        /// <summary>
        /// The assembly listing filename.
        /// </summary>
        public string ListingFile { get; private set; }

        /// <summary>
        /// Gets the label listing filename.
        /// </summary>
        public string LabelFile { get; private set; }

        /// <summary>
        /// Gets the flag that indicates assembly should be quiet.
        /// </summary>
        public bool Quiet { get; private set; }

        /// <summary>
        /// Gets the flag that indicates warnings should be suppressed.
        /// </summary>
        public bool NoWarnings { get; private set; }

        /// <summary>
        /// Gets a value indicating whether to suppress warnings for whitespaces 
        /// before labels.
        /// </summary>
        /// <value>If <c>true</c> warn left; otherwise, suppress the warning.</value>
        public bool WarnLeft { get; private set; }

        /// <summary>
        /// Gets a flag that treats warnings as errors.
        /// </summary>
        public bool WarningsAsErrors => !NoWarnings && _werror;

        /// <summary>
        /// Gets the number of arguments passed after the call to 
        /// <see cref="CommandLineOptions.ParseArgs(string[])"/>.
        /// </summary>
        /// <value>The arguments passed.</value>
        public int ArgsPassed => Arguments.Count();

        /// <summary>
        /// Gets a flag indicating that assembly listing should be 
        /// verbose.
        /// </summary>
        public bool VerboseList { get; private set; }

        /// <summary>
        /// Gets a flag that indicates the source should be processed as
        /// case-sensitive.
        /// </summary>
        public bool CaseSensitive { get; private set; }

        /// <summary>
        /// Gets a flag indicating if assembly listing should suppress original source.
        /// </summary>
        public bool NoSource { get; private set; }

        /// <summary>
        /// Gets a flag indicating if assembly listing should suppress 6502 disassembly.
        /// </summary>
        public bool NoDissasembly { get; private set; }

        /// <summary>
        /// Gets a flag indicating if assembly listing should suppress assembly bytes.
        /// </summary>
        public bool NoAssembly { get; private set; }

        /// <summary>
        /// Gets a flag indicating the full version of the assembler should be printed.
        /// </summary>
        public bool PrintVersion { get; }

        /// <summary>
        /// Gets a flag indicating that checksum information should be printed after 
        /// assembly.
        /// </summary>
        public bool ShowChecksums { get; private set; }

        #endregion
    }
}