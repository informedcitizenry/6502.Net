//-----------------------------------------------------------------------------
// Copyright (c) 2017-2020 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
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
            caseSensitive,
            defines,
            includePath,
            ignoreColons,
            outputFile,
            sources,
            listingOptions,
            labelPath,
            listPath,
            noAssembly,
            noDisassembly,
            noSource,
            verbose,
            loggingOptions,
            checksum,
            errorPath,
            noWarnings,
            quietMode,
            warningsAsErrors,
            warnLeft,
            target,
            binaryFormat,
            cpu
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
                                    Options.caseSensitive,
                                    Options.ignoreColons,
                                    Options.defines,
                                    Options.includePath,
                                    Options.listingOptions,
                                    Options.loggingOptions,
                                    Options.outputFile,
                                    Options.sources,
                                    Options.target
                                }
            },
            { "listingOptions", new HashSet<Options>
                                {
                                    Options.labelPath,
                                    Options.listPath,
                                    Options.noAssembly,
                                    Options.noDisassembly,
                                    Options.noSource,
                                    Options.verbose
                                }
            },
            { "loggingOptions", new HashSet<Options>
                                {
                                    Options.checksum,
                                    Options.errorPath,
                                    Options.noWarnings,
                                    Options.quietMode,
                                    Options.warningsAsErrors,
                                    Options.warnLeft
                                }
            },
            { "target",         new HashSet<Options>
                                {
                                    Options.binaryFormat,
                                    Options.cpu
                                }
            }
        };

        static readonly Dictionary<Options, OptionType> _optionTypes = new Dictionary<Options, OptionType>
        {
            { Options.caseSensitive,      OptionType.Boolean },
            { Options.ignoreColons,       OptionType.Boolean },
            { Options.defines,            OptionType.Array   },
            { Options.includePath,        OptionType.String  },
            { Options.listingOptions,     OptionType.Object  },
            { Options.loggingOptions,     OptionType.Object  },
            { Options.outputFile,         OptionType.String  },    
            { Options.sources,            OptionType.Array   },
            { Options.target,             OptionType.Object  },
            { Options.labelPath,          OptionType.String  },
            { Options.listPath,           OptionType.String  },
            { Options.noAssembly,         OptionType.Boolean },
            { Options.noDisassembly,      OptionType.Boolean },
            { Options.noSource,           OptionType.Boolean },
            { Options.verbose,            OptionType.Boolean },
            { Options.checksum,           OptionType.Boolean },
            { Options.errorPath,          OptionType.String  },
            { Options.noWarnings,         OptionType.Boolean },
            { Options.quietMode,          OptionType.Boolean },
            { Options.warningsAsErrors,   OptionType.Boolean },
            { Options.warnLeft,           OptionType.Boolean },
            { Options.binaryFormat,       OptionType.String  },
            { Options.cpu,                OptionType.String  }
        };

        static readonly Dictionary<Options, OptionType> _arrayTypes = new Dictionary<Options, OptionType>
        {
            { Options.defines, OptionType.String },
            { Options.sources, OptionType.String }
        };

        static readonly Dictionary<string, Options> _optionAlias = new Dictionary<string, Options>
        {
            { "-a",               Options.noAssembly },
            { "--no-assembly",    Options.noAssembly },
            { "-C",               Options.caseSensitive },
            { "--case-sensitive", Options.caseSensitive },
            { "-c",               Options.cpu },
            { "--cpu",            Options.cpu },
            { "--checksum",       Options.checksum },
            { "-D",               Options.defines },
            { "--define",         Options.defines },
            { "-d",               Options.noDisassembly },
            { "--no-disassembly", Options.noDisassembly },
            { "-E",               Options.errorPath },
            { "--error",          Options.errorPath },
            { "--format",         Options.binaryFormat },
            { "-I",               Options.includePath },
            { "--include-path",   Options.includePath },
            { "--ignore-colons",  Options.ignoreColons },
            { "-L",               Options.listPath },
            { "--list",           Options.listPath },
            { "-l",               Options.labelPath },
            { "--labels",         Options.labelPath },
            { "-o",               Options.outputFile },
            { "--output",         Options.outputFile },
            { "-q",               Options.quietMode },
            { "--quiet",          Options.quietMode },
            { "-s",               Options.noSource },
            { "--no-source",      Options.noSource },
            { "--verbose-asm",    Options.verbose },
            { "-w",               Options.noWarnings },
            { "--no-warn",        Options.noWarnings },
            { "--werror",         Options.warningsAsErrors },
            { "--wleft",          Options.warnLeft }
        };

        static readonly string _helpUsage = " Try '-?|-h|help' for usage.";

        static readonly string _helpList =
            "Usage: {0} [options...] <inputs> [output]\n\n" +
            "    -a, --no-assembly        Suppress assembled bytes from assembly\n" +
            "    -C, --case-sensitive     Treat all symbols as case-sensitive\n" +
            "    -c, --cpu <arg>          Specify the target CPU and instruction set\n" +
            "    --checksum               Display checksum information on assembly\n" +
            "    --config <arg>           Load all settings from a configuration file\n" +
            "    -D, --define <args>      Assign value to a global symbol/label in\n" +
            "                             <args>\n" +
            "    -d, --no-dissassembly    Suppress disassembly from assembly listing\n" +
            "    -E, --error <arg>        Dump errors to <arg>\n" +
            "    --format, --arch <arg>   Specify binary output format\n" +
            "    -I, --include-path <arg> Include search path <arg>\n" +
            "    --ignore-colons          Treat colons in semi-colon comments as comments\n" +
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

        List<string> _source;
        List<string> _defines;
        bool _werror;

        #endregion

        #region Methods

        static string GetHelpText() =>
            string.Format(_helpList, Assembly.GetEntryAssembly().GetName().Name);

        static string GetVersion() =>
            $"{Assembler.AssemblerNameSimple}\n{Assembler.AssemblerVersion}";

        /// <summary>
        /// Process the command-line arguments passed by the end-user.
        /// </summary>
        public void ParseArgs()
        {
            var args = Environment.GetCommandLineArgs().Skip(1).ToList();
            if (args.Count == 0)
                throw new Exception($"One or more arguments expected.{_helpUsage}");

            _source = new List<string>();
            _defines = new List<string>();
            Format =
            CPU =
            ErrorFile =
            IncludePath =
            ListingFile =
            OutputPath =
            LabelFile = string.Empty;
            OutputFile = "a.out";
            IgnoreColons =
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
                            if (args.Count > 1 && (i > 0 || i < args.ToList().FindLastIndex(a => a[0] == '-')))
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
                            while (!string.IsNullOrEmpty(nextArg))
                            {
                                _defines.Add(nextArg);
                                if (++i >= args.Count - 1|| args[i + 1][0] == '-')
                                    break;
                                nextArg = args[i + 1];
                            }
                            if (_defines.Count == 0)
                                throw new Exception("One or more definitions was expected.");
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
            var ident = string.Empty;
            var parsedOption = Options.None;
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
                    if (!Enum.TryParse<Options>(ident, out parsedOption) || !_optionSchema[parent].Contains(parsedOption))
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
                case Options.caseSensitive:      CaseSensitive  = value; break;
                case Options.checksum:           ShowChecksums  = value; break;
                case Options.ignoreColons:       IgnoreColons   = value; break;
                case Options.noAssembly:         NoAssembly     = value; break;
                case Options.noDisassembly:      NoDissasembly  = value; break;
                case Options.noSource:           NoSource       = value; break;
                case Options.noWarnings:         NoWarnings     = value; break;
                case Options.quietMode:          Quiet          = value; break;
                case Options.verbose:            VerboseList    = value; break;
                case Options.warningsAsErrors:   _werror        = value; break;
                case Options.warnLeft:           WarnLeft       = value; break;
            }
        }

        void SetOptionValue(Options option, string value)
        {
            switch (option)
            {
                case Options.includePath:        IncludePath    = value; break;
                case Options.outputFile:         OutputFile     = value; break;
                case Options.labelPath:          LabelFile      = value; break;
                case Options.listPath:           ListingFile    = value; break;
                case Options.errorPath:          ErrorFile      = value; break;
                case Options.binaryFormat:       Format         = value; break;
                case Options.cpu:                CPU            = value; break;
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
                        if (ident == Options.defines)
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
        /// Gets the output file's path (directory).
        /// </summary>
        public string OutputPath { get; private set; }

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
        public bool PrintVersion { get; private set; }

        /// <summary>
        /// Gets a flag indicating that checksum information should be printed after 
        /// assembly.
        /// </summary>
        public bool ShowChecksums { get; private set; }

        /// <summary>
        /// Gets a flag indicating that colons in semi-colon comments should be treated
        /// as comments.
        /// </summary>
        public bool IgnoreColons { get; private set; }

        #endregion
    }
}