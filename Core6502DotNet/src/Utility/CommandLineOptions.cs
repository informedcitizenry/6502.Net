//-----------------------------------------------------------------------------
// Copyright (c) 2017-2020 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
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
        #region Members

        readonly List<string> _source;
        readonly List<string> _defines;
        string _arch;
        string _cpu;
        string _listingFile;
        string _labelFile;
        string _outputFile;
        bool _bigEndian;
        bool _quiet;
        bool _verboseDasm;
        bool _werror;
        bool _noWarn;
        bool _caseSensitive;
        bool _noAssembly;
        bool _noSource;
        bool _printVersion;
        bool _noDisassembly;
        bool _warnLeft;
        bool _showChecksums;

        #endregion

        static readonly string _helpString =
            "Usage: {0} [options...] <inputs> [output]\r\n\r\n" +
            "    -a, --no-assembly        Suppress assembled bytes from assembly\r\n" +
            "    --arch <arg>             Specify architecture-specific options\r\n" +
            "    -b, --big-endian         Set byte order of output to big-endian\r\n" +
            "                             listing\r\n" +
            "    -C, --case-sensitive     Treat all symbols as case-sensitive\r\n" +
            "    --checksum               Display checksum information on assembly\r\n" +
            "    -c, --cpu <arg>          Specify the target CPU and instruction set\r\n" +
            "    -D, --define <args>      Assign value to a global symbol/label in\r\n" +
            "                             <args>\r\n" +
            "    -d, --no-dissassembly    Suppress disassembly from assembly listing\r\n" +
            "    -l, --labels <arg>       Output label definitions to <arg>\r\n" +
            "    -L, --list <arg>         Output listing to <arg>\r\n" +
            "    -o, --output <arg>       Output assembly to <arg>\r\n" +
            "    -q, --quiet              Assemble in quiet mode (no console\r\n" +
            "    -s, --no-source          Suppress original source from assembly\r\n" +
            "                             listing\r\n" +
            "    -V, --version            Print current version\r\n" +
            "                             messages)\r\n" +
            "    --verbose-asm            Expand listing to include all directives\r\n" +
            "                             and comments\r\n" +
            "    -w, --no-warn            Suppress all warnings\r\n" +
            "    --werror                 Treat all warnings as error\r\n" +
            "    --wleft                  Issue warnings about whitespaces before\r\n" +
            "                             labels\r\n" +
            "    <inputs>                 The source files to assemble";

        #region Constructors

        /// <summary>
        /// Constructs a new instance of the AsmCommandLineOptions.
        /// </summary>
        public CommandLineOptions()
        {
            _source = new List<string>();
            _defines = new List<string>();
            _arch =
            _cpu =
            _listingFile =
            _labelFile =
            _outputFile = string.Empty;
            _bigEndian =
            _showChecksums =
            _verboseDasm =
            _werror =
            _noWarn =
            _warnLeft =
            _noDisassembly =
            _noSource =
            _noAssembly =
            _quiet =
            _printVersion =
            _caseSensitive = false;
        }

        #endregion

        #region Methods

        static string GetHelpText() =>
            string.Format(_helpString, Assembly.GetEntryAssembly().GetName().Name);

        /// <summary>
        /// Process the command-line arguments passed by the end-user.
        /// </summary>
        /// <param name="passedArgs">The argument array.</param>
        public void ParseArgs(string[] passedArgs)
        {
            if (passedArgs.Length == 0)
            {
                throw new Exception("One or more arguments expected. Try '-?|-h|help' for usage.");
            }
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
                    var optionName = GetOptionName(args[i]);
                    var eqix = arg.IndexOf('=');
                    if (eqix > -1 && eqix == arg.Length - 1)
                        throw new Exception();
                    var nextArg = string.Empty;
                    if (i < args.Count - 1 && args[i + 1][0] != '-')
                        nextArg = args[i + 1];
                    try
                    {
                        switch (optionName)
                        {
                            case "-a":
                            case "--no-assembly":
                                SetFlag(ref _noAssembly);
                                break;
                            case "--arch":
                                SetOneOption(ref _arch);
                                break;
                            case "-b":
                            case "--big-endian":
                                SetFlag(ref _bigEndian);
                                break;
                            case "-C":
                            case "case-sensitive":
                                SetFlag(ref _caseSensitive);
                                break;
                            case "-c":
                            case "--cpu":
                                SetOneOption(ref _cpu);
                                break;
                            case "--checksum":
                                SetFlag(ref _showChecksums);
                                break;
                            case "-D":
                            case "--define":
                                if (eqix > -1)
                                    _defines.Add(arg.Substring(eqix));
                                i = ConsumeArgs(++i, _defines, eqix > -1);
                                break;
                            case "-d":
                            case "--no-disassembly":
                                SetFlag(ref _noDisassembly);
                                break;
                            case "-?":
                            case "-h":
                            case "--help":
                                throw new Exception(GetHelpText());
                            case "-l":
                            case "--labels":
                                SetOneOption(ref _labelFile);
                                break;
                            case "-L":
                            case "--list":
                                SetOneOption(ref _listingFile);
                                break;
                            case "-o":
                            case "--output":
                                SetOneOption(ref _outputFile);
                                break;
                            case "-q":
                            case "--quiet":
                                SetFlag(ref _quiet);
                                break;
                            case "-s":
                            case "--no-source":
                                SetFlag(ref _noSource);
                                break;
                            case "-V":
                            case "--version":
                                SetFlag(ref _printVersion);
                                break;
                            case "--verbose-asm":
                                SetFlag(ref _verboseDasm);
                                break;
                            case "-w":
                            case "--no-warn":
                                SetFlag(ref _noWarn);
                                break;
                            case "--werror":
                                SetFlag(ref _werror);
                                break;
                            case "--wleft":
                                SetFlag(ref _warnLeft);
                                break;
                            default:
                                throw new Exception(string.Format("Invalid option '{0}'. Try '-?|-h|help' for usage.", optionName));
                        }
                    }
                    catch (ArgumentException)
                    {
                        throw new Exception(string.Format("Invalid argument or arguments for option '{0}'. Try '-?|-h|help' for usage.", optionName));
                    }

                    void SetFlag(ref bool opt)
                    {
                        if (!string.IsNullOrEmpty(nextArg))
                            throw new ArgumentException();
                        opt = true;
                    }

                    void SetOneOption(ref string opt)
                    {
                        if (eqix > -1)
                        {
                            if (!string.IsNullOrEmpty(nextArg))
                                throw new ArgumentException();
                            opt = arg.Substring(eqix + 1);
                        }
                        else
                        {
                            if (string.IsNullOrEmpty(nextArg))
                                throw new ArgumentException();
                            opt = nextArg;
                            i++;
                        }
                        if (i + 1 < args.Count - 1 && args[i + 1][0] != '-')
                            throw new ArgumentException();
                    }
                }
                else
                {
                    i = ConsumeArgs(i, _source, true);
                }
            }

            int ConsumeArgs(int i, List<string> optionArgs, bool allowNone)
            {
                while (i < args.Count)
                {
                    if (args[i][0] == '-')
                        break;
                    optionArgs.Add(args[i++]);
                    allowNone = true;
                }
                if (!allowNone)
                    throw new ArgumentException();
                return i - 1;
            }

            string GetOptionName(string argument)
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
                    else if (char.IsLetterOrDigit(c))
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
        public string Architecture { get => _arch; set => _arch = value; }

        /// <summary>
        /// Gets the selected CPU.
        /// </summary>
        /// <value>The cpu.</value>
        public string CPU => _cpu;

        /// <summary>
        /// Gets the value determining whether output file should be generated, 
        /// based on the criterion that input files were specified and either
        /// 1) an output file was also, or that 2) no listing nor label file was.
        /// </summary>
        public bool GenerateOutput => _source.Count > 0 &&
                     (
                      !string.IsNullOrEmpty(_outputFile) ||
                      (string.IsNullOrEmpty(_labelFile) && string.IsNullOrEmpty(_listingFile))
                     );

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
        public string OutputFile => _outputFile;

        /// <summary>
        /// The assembly listing filename.
        /// </summary>
        public string ListingFile => _listingFile;

        /// <summary>
        /// Gets the label listing filename.
        /// </summary>
        public string LabelFile => _labelFile;

        /// <summary>
        /// Gets the flag that indicates assembly should be quiet.
        /// </summary>
        public bool Quiet => _quiet;

        /// <summary>
        /// Gets the flag that indicates warnings should be suppressed.
        /// </summary>
        public bool NoWarnings => _noWarn;

        /// <summary>
        /// Gets a value indicating whether to suppress warnings for whitespaces 
        /// before labels.
        /// </summary>
        /// <value>If <c>true</c> warn left; otherwise, suppress the warning.</value>
        public bool WarnLeft => _warnLeft;

        /// <summary>
        /// Gets a flag that treats warnings as errors.
        /// </summary>
        public bool WarningsAsErrors => !_noWarn && _werror;

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
        public bool VerboseList => _verboseDasm;

        /// <summary>
        /// Gets a flag that indicates the source should be processed as
        /// case-sensitive.
        /// </summary>
        public bool CaseSensitive => _caseSensitive;

        /// <summary>
        /// Gets a flag indicating if assembly listing should suppress original source.
        /// </summary>
        public bool NoSource => _noSource;

        /// <summary>
        /// Gets a flag indicating if assembly listing should suppress 6502 disassembly.
        /// </summary>
        public bool NoDissasembly => _noDisassembly;

        /// <summary>
        /// Gets a flag indicating if assembly listing should suppress assembly bytes.
        /// </summary>
        public bool NoAssembly => _noAssembly;

        /// <summary>
        /// Gets a flag indicating the full version of the assembler should be printed.
        /// </summary>
        public bool PrintVersion => _printVersion;

        /// <summary>
        /// Gets a flag indicating that checksum information should be printed after 
        /// assembly.
        /// </summary>
        public bool ShowChecksums => _showChecksums;

        #endregion
    }
}