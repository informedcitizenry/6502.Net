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

namespace Asm6502.Net
{
    /// <summary>
    /// A helper class to parse and present strongly-typed options from the command-line.
    /// </summary>
    public class AsmCommandLineOptions
    {
        #region Members

        private IReadOnlyList<string> source_;
        private IReadOnlyList<string> defines_;
        private string listingFile_;
        private string labelFile_;
        private string outputFile_;
        private bool cbmheader_;
        private bool quiet_;
        private bool verbose_;
        private bool werror_;
        private bool nowarn_;
        private bool casesensitive_;
        private bool noassembly_;
        private bool nosource_;
        private bool nodisassembly_;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a new instance of the AsmCommandLineOptions.
        /// </summary>
        public AsmCommandLineOptions()
        {
            source_ = new List<string>();
            defines_ = new List<string>();
            listingFile_ =
            labelFile_ =
            outputFile_ = string.Empty;
            cbmheader_ =
            verbose_ =
            werror_ =
            nowarn_ = 
            nodisassembly_ =
            nosource_ =
            noassembly_ =
            quiet_ =
            casesensitive_ = false;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Process the command-line arguments passed by the end-user.
        /// </summary>
        /// <param name="args">The argument string.</param>
        /// <returns>True if version info was requested, otherwise false.</returns>
        public bool ProcessArgs(string[] args)
        {
            if (args.Length == 0)
            {
                throw new Exception("One or more arguments expected. Try '-?|-h|help' for usage.");
            }
            Arguments = new string[args.Length];

            args.CopyTo(Arguments, 0);

            bool printVersion = false;

            ArgumentSyntax.Parse(args, syntax =>
            {
                syntax.DefineOption("o|output", ref outputFile_, "Output assembly to <arg>");
                syntax.DefineOption("b|nostart", ref cbmheader_, "No starting address output (CBM PRG format)");
                syntax.DefineOptionList("D|define", ref defines_, "Assign value to a global symbol/label in <arg>");
                syntax.DefineOption("q|quiet", ref quiet_, "Assemble in quiet mode (no console messages)");
                syntax.DefineOption("w|no-warn", ref nowarn_, "Suppress all warnings");
                syntax.DefineOption("werror", ref werror_, "Treat all warnings as errors");
                syntax.DefineOption("l|labels", ref labelFile_, "Output label definitions to <arg>");
                syntax.DefineOption("L|list", ref listingFile_, "Output listing to <arg>");
                syntax.DefineOption("a|no-assembly", ref noassembly_, "Suppress assembled bytes from assembly listing");
                syntax.DefineOption("d|no-disassembly", ref nodisassembly_, "Suppress disassembly from assembly listing");
                syntax.DefineOption("s|no-source", ref nosource_, "Suppress original source from assembly listing");
                syntax.DefineOption("verbose-asm", ref verbose_, "Expand listing to include all directives and comments");
                syntax.DefineOption("C|case-sensitive", ref casesensitive_, "Treat all symbols as case sensitive");
                syntax.DefineOption("V|version", ref printVersion, "Print current version");
                syntax.DefineParameterList("source", ref source_, "The source files to assemble");
            });

            return printVersion;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the argument string array passed.
        /// </summary>
        public string[] Arguments { get; private set; }


        /// <summary>
        /// Gets the value determining whether output file should be generated, 
        /// based on the criterion that input files were specified and either
        /// 1) an output file was also, or that 2) no listing nor label file was.
        /// </summary>
        public bool GenerateOutput
        {
            get
            {
                return source_.Count > 0 &&
                     (
                      !string.IsNullOrEmpty(outputFile_) ||
                      (string.IsNullOrEmpty(labelFile_) && string.IsNullOrEmpty(listingFile_))
                     );
            }
        }

        /// <summary>
        /// Gets the read-only list of input filenames.
        /// </summary>
        public IReadOnlyList<string> InputFiles { get { return source_; } }

        /// <summary>
        /// Gets the read-only list of label defines.
        /// </summary>
        public IReadOnlyList<string> LabelDefines { get { return defines_; } }

        /// <summary>
        /// Gets the output filename.
        /// </summary>
        public string OutputFile { get { return outputFile_; } }

        /// <summary>
        /// The assembly listing filename.
        /// </summary>
        public string ListingFile { get { return listingFile_; } }

        /// <summary>
        /// Gets the label listing filename.
        /// </summary>
        public string LabelFile { get { return labelFile_; } }

        /// <summary>
        /// Gets the flag that indicates assembly should be quiet.
        /// </summary>
        public bool Quiet { get { return quiet_; } }

        /// <summary>
        /// Gets the flag that indicates warnings should be suppressed.
        /// </summary>
        public bool NoWarnings { get { return nowarn_; } }

        /// <summary>
        /// Gets a flag that treats warnings as errors.
        /// </summary>
        public bool WarningsAsErrors 
        { 
            get 
            { 
                if (!nowarn_) 
                    return werror_;
                return false;
            } 
        }

        /// <summary>
        /// Gets a flag indicating that assembly listing should be 
        /// verbose.
        /// </summary>
        public bool VerboseList { get { return verbose_; } }

        /// <summary>
        /// Gets a flag that indicates the source should be processed as
        /// case-sensitive.
        /// </summary>
        public bool CaseSensitive { get { return casesensitive_; } }

        /// <summary>
        /// Gets the System.StringComparison, which is based on the case-sensitive flag.
        /// </summary>
        public StringComparison StringComparison
        {
            get
            {
                return casesensitive_ ? StringComparison.InvariantCulture : StringComparison.InvariantCultureIgnoreCase;
            }
        }

        /// <summary>
        /// Gets the System.StringComparer, which is based on the case-sensitive flag.
        /// </summary>
        public StringComparer StringComparar
        {
            get
            {
                return casesensitive_ ? StringComparer.InvariantCulture : StringComparer.InvariantCultureIgnoreCase;
            }
        }

        /// <summary>
        /// Gets the RegexOption flag indicating case-sensitivity based on the case-sensitive flag.
        /// </summary>
        public System.Text.RegularExpressions.RegexOptions RegexOption
        {
            get
            {
                return casesensitive_ ? System.Text.RegularExpressions.RegexOptions.None : System.Text.RegularExpressions.RegexOptions.IgnoreCase;
            }
        }

        /// <summary>
        /// Gets a flag that indicates that the output should suppress the
        /// program starting address (CBM DOS format)
        /// </summary>
        public bool SuppressCbmHeader { get { return cbmheader_; } }

        /// <summary>
        /// Gets a flag indicating if assembly listing should suppress original source.
        /// </summary>
        public bool NoSource { get { return nosource_; } }

        /// <summary>
        /// Gets a flag indicating if assembly listing should suppress 6502 disassembly.
        /// </summary>
        public bool NoDissasembly { get { return nodisassembly_; } }

        /// <summary>
        /// Gets a flag indicating if assembly listing should suppress assembly bytes.
        /// </summary>
        public bool NoAssembly { get { return noassembly_; } }

        #endregion
    }
}