//-----------------------------------------------------------------------------
// Copyright (c) 2017 informedcitizenry <informedcitizenry@gmail.com>
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to 
// deal in the Software without restriction, including without limitation the 
// rights to use, copy, modify, merge, publish, distribute, sublicense, and/or 
// sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in 
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS 
// IN THE SOFTWARE.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.CommandLine;
namespace DotNetAsm
{
    /// <summary>
    /// A helper class to parse and present strongly-typed options from the command-line.
    /// </summary>
    public class AsmCommandLineOptions
    {
        #region Members

        private IReadOnlyList<string> _source;
        private IReadOnlyList<string> _defines;
        private string _arch;
        private string _listingFile;
        private string _labelFile;
        private string _outputFile;
        private bool _bigEndian;
        private bool _quiet;
        private bool _verboseDasm;
        private bool _werror;
        private bool _noWarn;
        private bool _caseSensitive;
        private bool _noAssembly;
        private bool _noSource;
        private bool _printVersion;
        private bool _noDisassembly;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a new instance of the AsmCommandLineOptions.
        /// </summary>
        public AsmCommandLineOptions()
        {
            _source = new List<string>();
            _defines = new List<string>();
            _arch =
            _listingFile =
            _labelFile =
            _outputFile = string.Empty;
            _bigEndian =
            _verboseDasm =
            _werror =
            _noWarn =
            _noDisassembly =
            _noSource =
            _noAssembly =
            _quiet =
            _printVersion =
            _caseSensitive = false;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Process the command-line arguments passed by the end-user.
        /// </summary>
        /// <param name="args">The argument string.</param>
        /// <returns>True if version info was requested, otherwise false.</returns>
        public void ProcessArgs(string[] args)
        {
            if (args.Length == 0)
            {
                throw new Exception("One or more arguments expected. Try '-?|-h|help' for usage.");
            }
            Arguments = new string[args.Length];

            args.CopyTo(Arguments, 0);
            var result = ArgumentSyntax.Parse(args, syntax =>
            {
                syntax.DefineOption("o|output", ref _outputFile, "Output assembly to <arg>");
                syntax.DefineOption("b|big-endian", ref _bigEndian, "Set byte order of output to big-endian");
                syntax.DefineOption("arch", ref _arch, "Specify architecture-specific options");
                syntax.DefineOptionList("D|define", ref _defines, "Assign value to a global symbol/label in <arg>");
                syntax.DefineOption("q|quiet", ref _quiet, "Assemble in quiet mode (no console messages)");
                syntax.DefineOption("w|no-warn", ref _noWarn, "Suppress all warnings");
                syntax.DefineOption("werror", ref _werror, "Treat all warnings as errors");
                syntax.DefineOption("l|labels", ref _labelFile, "Output label definitions to <arg>");
                syntax.DefineOption("L|list", ref _listingFile, "Output listing to <arg>");
                syntax.DefineOption("a|no-assembly", ref _noAssembly, "Suppress assembled bytes from assembly listing");
                syntax.DefineOption("d|no-disassembly", ref _noDisassembly, "Suppress disassembly from assembly listing");
                syntax.DefineOption("s|no-source", ref _noSource, "Suppress original source from assembly listing");
                syntax.DefineOption("verbose-asm", ref _verboseDasm, "Expand listing to include all directives and comments");
                syntax.DefineOption("C|case-sensitive", ref _caseSensitive, "Treat all symbols as case sensitive");
                syntax.DefineOption("V|version", ref _printVersion, "Print current version");
                syntax.DefineParameterList("source", ref _source, "The source files to assemble");
            });
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the argument string array passed.
        /// </summary>
        public string[] Arguments { get; private set; }

        /// <summary>
        /// Gets the target architecture information.
        /// </summary>
        public string Architecture { get { return _arch; } set { _arch = value; } }

        /// <summary>
        /// Gets the value determining whether output file should be generated, 
        /// based on the criterion that input files were specified and either
        /// 1) an output file was also, or that 2) no listing nor label file was.
        /// </summary>
        public bool GenerateOutput
        {
            get
            {
                return _source.Count > 0 &&
                     (
                      !string.IsNullOrEmpty(_outputFile) ||
                      (string.IsNullOrEmpty(_labelFile) && string.IsNullOrEmpty(_listingFile))
                     );
            }
        }

        /// <summary>
        /// Gets the read-only list of input filenames.
        /// </summary>
        public IReadOnlyList<string> InputFiles { get { return _source; } }

        /// <summary>
        /// Gets the read-only list of label defines.
        /// </summary>
        public IReadOnlyList<string> LabelDefines { get { return _defines; } }

        /// <summary>
        /// Gets the output filename.
        /// </summary>
        public string OutputFile { get { return _outputFile; } }

        /// <summary>
        /// The assembly listing filename.
        /// </summary>
        public string ListingFile { get { return _listingFile; } }

        /// <summary>
        /// Gets the label listing filename.
        /// </summary>
        public string LabelFile { get { return _labelFile; } }

        /// <summary>
        /// Gets the flag that indicates assembly should be quiet.
        /// </summary>
        public bool Quiet { get { return _quiet; } }

        /// <summary>
        /// Gets the flag that indicates warnings should be suppressed.
        /// </summary>
        public bool NoWarnings { get { return _noWarn; } }

        /// <summary>
        /// Gets a flag that treats warnings as errors.
        /// </summary>
        public bool WarningsAsErrors
        {
            get
            {
                if (!_noWarn)
                    return _werror;
                return false;
            }
        }

        /// <summary>
        /// Gets a flag indicating that assembly listing should be 
        /// verbose.
        /// </summary>
        public bool VerboseList { get { return _verboseDasm; } }

        /// <summary>
        /// Gets a flag that indicates the source should be processed as
        /// case-sensitive.
        /// </summary>
        public bool CaseSensitive { get { return _caseSensitive; } }

        /// <summary>
        /// Gets the System.StringComparison, which is based on the case-sensitive flag.
        /// </summary>
        public StringComparison StringComparison
        {
            get
            {
                return _caseSensitive ? StringComparison.InvariantCulture : StringComparison.InvariantCultureIgnoreCase;
            }
        }

        /// <summary>
        /// Gets the System.StringComparer, which is based on the case-sensitive flag.
        /// </summary>
        public StringComparer StringComparar
        {
            get
            {
                return _caseSensitive ? StringComparer.InvariantCulture : StringComparer.InvariantCultureIgnoreCase;
            }
        }

        /// <summary>
        /// Gets the RegexOption flag indicating case-sensitivity based on the case-sensitive flag.
        /// </summary>
        public System.Text.RegularExpressions.RegexOptions RegexOption
        {
            get
            {
                return _caseSensitive ? System.Text.RegularExpressions.RegexOptions.None : System.Text.RegularExpressions.RegexOptions.IgnoreCase;
            }
        }

        /// <summary>
        /// Gets a flag that indicates that the output should be in big-endian byte order.
        /// </summary>
        public bool BigEndian { get { return _bigEndian; } }

        /// <summary>
        /// Gets a flag indicating if assembly listing should suppress original source.
        /// </summary>
        public bool NoSource { get { return _noSource; } }

        /// <summary>
        /// Gets a flag indicating if assembly listing should suppress 6502 disassembly.
        /// </summary>
        public bool NoDissasembly { get { return _noDisassembly; } }

        /// <summary>
        /// Gets a flag indicating if assembly listing should suppress assembly bytes.
        /// </summary>
        public bool NoAssembly { get { return _noAssembly; } }

        /// <summary>
        /// Gets a flag indicating the full version of the assembler should be printed.
        /// </summary>
        public bool PrintVersion { get { return _printVersion; } }

        #endregion
    }
}