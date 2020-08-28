//-----------------------------------------------------------------------------
// Copyright (c) 2017-2020 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace Core6502DotNet
{
    /// <summary>
    /// A delegate that defines a handler for the event when the current
    /// pass count has changed.
    /// </summary>
    /// <param name="sender">The sender object.</param>
    /// <param name="args">The event args.</param>
    public delegate void PassesChangedEventHandler(object sender, EventArgs args);

    /// <summary>
    /// A static class holding all shared assembly resources and state, such as
    /// <see cref="ErrorLog"/> and <see cref="BinaryOutput"/> support classes.
    /// </summary>
    public static class Assembler
    {
        #region Methods

        /// <summary>
        /// Initializes the <see cref="Assembler"/> class for use. Repeated calls will reset symbol labels and variables,
        /// assembling pass and listing printing states, the binary output, and the error log.
        /// </summary>
        /// <param name="args">The collection of option arguments.</param>
        public static void Initialize(IEnumerable<string> args)
        {
            PassChanged = null;
            PrintOff = false;
            PassNeeded = true;
            CurrentPass = -1;
            LineIterator = null;
            IsReserved = new List<Func<string, bool>>();
            InstructionLookupRules = new List<Func<string, bool>>();
            Options = Options.FromArgs(args);
            OutputFormat = Options.Format;
            Encoding = new AsmEncoding(Options.CaseSensitive);

            SymbolManager = new SymbolManager(Options.CaseSensitive);
            SymbolManager.AddValidSymbolNameCriterion(s =>
            {
                if (!Options.CaseSensitive)
                    s = s.ToLower();
                return !Evaluator.IsReserved(s);
            });
            Evaluator.Reset();
            Evaluator.AddFunctionEvaluator(SymbolManager);
            Evaluator.CaseSensitive = Options.CaseSensitive;

            Log = new ErrorLog();
            Output = new BinaryOutput();
        }

        /// <summary>
        /// Selects the <see cref="IBinaryFormatProvider"/> from the specified format
        /// name, and sets the <see cref="OutputFormat"/> property.
        /// </summary>
        /// <param name="format"></param>
        public static void SelectFormat(string format)
        {
            BinaryFormatProvider = (FormatSelector?.Invoke(format));
            OutputFormat = format;
        }

        /// <summary>
        /// Increment the pass counter.
        /// </summary>
        /// <returns>The new value of the pass counter.</returns>
        public static int IncrementPass()
        {
            CurrentPass++;
            PassChanged?.Invoke(null, new EventArgs());
            PassNeeded = false;
            return CurrentPass; 
        }

        #endregion

        #region Properties

        /// <summary>
        /// The <see cref="SymbolManager"/> responsible for all symbol definitions
        /// and references.
        /// </summary>
        public static SymbolManager SymbolManager { get; private set; }

        /// <summary>
        /// The <see cref="AsmEncoding"/> responsible for all encoding.
        /// </summary>
        public static AsmEncoding Encoding { get; private set; }

        /// <summary>
        /// The <see cref="ErrorLog"/> object used for all error and warning
        /// logging.
        /// </summary>
        public static ErrorLog Log { get; private set; }

        /// <summary>
        /// The <see cref="BinaryOutput"/> object managing all output of assembly.
        /// </summary>
        public static BinaryOutput Output { get; private set; }

        /// <summary>
        /// Gets the output format name.
        /// </summary>
        public static string OutputFormat { get; private set; }

        /// <summary>
        /// The <see cref="global::Core6502DotNet.Options"/> object responsible for parsing
        /// and enumerating all command line options.
        /// </summary>
        public static Options Options { get; private set; }

        /// <summary>
        /// Gets the option arguments passed.
        /// </summary>
        public static string OptionArguments { get; private set; }


        /// <summary>
        /// An event that signals to subscribing handlers that a pass has changed.
        /// </summary>
        public static event PassesChangedEventHandler PassChanged;

        /// <summary>
        /// Gets or sets the number of passes attempted. Setting this
        /// property resets the PassNeeded property.
        /// </summary>
        public static int CurrentPass { get; private set; }

        /// <summary>
        /// Gets or sets the flag that determines if another pass is needed. 
        /// This field is reset when the Passes property changes.
        /// </summary>
        public static bool PassNeeded { get; set; }

        /// <summary>
        /// Gets the list of functions that
        /// determine whether a given keyword is reserved.
        /// </summary>
        public static List<Func<string, bool>> IsReserved { get; private set; }

        /// <summary>
        /// Gets or sets the flag that determines whether disassembly should print.
        /// </summary>
        public static bool PrintOff { get; set; }

        /// <summary>
        /// Gets or sets the version of the assembler. This can and should be set
        /// by the client code.
        /// </summary>
        public static string AssemblerVersion
        {
            get
            {
                var assemblyName = Assembly.GetEntryAssembly().GetName();
                return $"Version {assemblyName.Version.Major}.{assemblyName.Version.Minor} Build {assemblyName.Version.Build}";
            }
        }

        /// <summary>
        /// Gets or sets the assembler (product) name. This can and should be set
        /// by the client code.
        /// </summary>
        public static string AssemblerName
        {
            get
            {
                var versionInfo = FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly().Location);
                return $"{versionInfo.Comments}\n{versionInfo.LegalCopyright}";
            }
        }

        /// <summary>
        /// Gets or sets the assembler's simple name, based on the AssemblerName
        /// property.
        /// </summary>
        public static string AssemblerNameSimple
        {
            get
            {
                var versionInfo = FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly().Location);
                return $"{versionInfo.Comments}";
            }
        }

        /// <summary>
        /// Gets a list of <see cref="Func{string, bool}"/> functions that determine
        /// whether a given keyword is a mnemonic, pseudo-op or other assembler directive.
        /// </summary>
        public static List<Func<string, bool>> InstructionLookupRules { get; private set; }

        /// <summary>
        /// Gets or sets the <see cref="SourceLine"/> iterator associated with the assembler session.
        /// </summary>
        public static RandomAccessIterator<SourceLine> LineIterator { get; set; }


        /// <summary>
        /// Gets or sets a custom binary format provider to convert the output before 
        /// assembling to disk.
        /// </summary>
        public static IBinaryFormatProvider BinaryFormatProvider { get; set; }

        public static Func<string, IBinaryFormatProvider> FormatSelector { get; set; }

        /// <summary>
        /// Gets the current <see cref="SourceLine"/> from the iterator.
        /// </summary>
        public static SourceLine CurrentLine => LineIterator.Current;

        /// <summary>
        /// Gets the <see cref="StringComparer"/> on the case-sensitive flag of
        /// the set <see cref="Options"/>.
        /// </summary>
        public static StringComparer StringComparer 
            => Options.CaseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;

        /// <summary>
        /// Gets the <see cref="StringComparison"/> on the case-sensitive flag of the set <see cref="Options"/>.
        /// </summary>
        public static StringComparison StringComparison
            => Options.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

        #endregion
    }

    /// <summary>
    ///The base line assembler class. This class must be inherited.
    /// </summary>
    public abstract class AssemblerBase
    {

        #region Constructors

        /// <summary>
        /// Constructs an instance of the class implementing the base class.
        /// </summary>
        protected AssemblerBase()
        {
            ExcludedInstructionsForLabelDefines = new HashSet<string>(Assembler.StringComparer);

            Reserved = new ReservedWords(Assembler.StringComparer);

            Assembler.IsReserved.Add(Reserved.IsReserved);

            Assembler.SymbolManager.AddValidSymbolNameCriterion(s => !Reserved.IsReserved(s));

            Assembler.InstructionLookupRules.Add(s => Assembles(s));
        }

        #endregion

        #region Methods

        /// <summary>
        /// Determines whether the token is a reserved word to the assembler object.
        /// </summary>
        /// <param name="token">The token to check if reserved</param>
        /// <returns><c>true</c> if reserved, otherwise <c>false</c>.</returns>
        public virtual bool IsReserved(string token) => Reserved.IsReserved(token);

        public virtual bool Assembles(string s) => IsReserved(s);

        public virtual bool AssemblesLine(SourceLine line) => (!string.IsNullOrEmpty(line.LabelName) && line.Instruction == null)
            || IsReserved(line.InstructionName);

        /// <summary>
        /// Assemble the <see cref="SourceLine"/>. This method must be inherited.
        /// </summary>
        /// <param name="line">The source line.</param>
        protected abstract string OnAssembleLine(SourceLine line);

        /// <summary>
        /// Assemble the parsed <see cref="SourceLine"/>.
        /// </summary>
        /// <param name="line">The line to assembly.</param>
        /// <returns>The disassembly output from the assembly operation.</returns>
        /// <exception cref="BlockClosureException"/>
        /// <exception cref="DirectoryNotFoundException"/>
        /// <exception cref="DivideByZeroException"/>
        /// <exception cref="ExpressionException"/>
        /// <exception cref="FileNotFoundException"/>
        /// <exception cref="FormatException"/>
        /// <exception cref="InvalidPCAssignmentException"/>
        /// <exception cref="BlockAssemblerException"/>
        /// <exception cref="OverflowException"/>
        /// <exception cref="ProgramOverflowException"/>
        /// <exception cref="ReturnException"/>
        /// <exception cref="SectionException"/>
        /// <exception cref="SymbolException"/>
        /// <exception cref="SyntaxException"/>
        public string AssembleLine(SourceLine line)
        {
            bool isReference = false;
            PCOnAssemble = Assembler.Output.LogicalPC;
            if (line.Label != null && (line.Instruction == null ||
                !ExcludedInstructionsForLabelDefines.Contains(line.InstructionName)))
            {
                if (line.LabelName.Equals("+") || line.LabelName.Equals("-"))
                {
                    isReference = true;
                    Assembler.SymbolManager.DefineLineReference(line.LabelName, PCOnAssemble);
                }
                else if (!line.LabelName.Equals("*"))
                {
                    Assembler.SymbolManager.DefineSymbolicAddress(line.LabelName);
                }
            }
            if (line.Instruction != null)
                return OnAssembleLine(line);
            if (line.Label != null && !isReference && !line.LabelName.Equals("*"))
            {
                var labelValue = Assembler.SymbolManager.GetNumericValue(line.LabelName);
                if (!double.IsNaN(labelValue))
                    return string.Format(".{0}{1}",
                                 ((int)labelValue).ToString("x4").PadRight(42),
                                 line.UnparsedSource);
            }
            return string.Empty;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the collection of instructions where the <see cref="AssemblerBase"/> should 
        /// not define any line label as a symbolic address or reference when 
        /// performing the <see cref="AssemblerBase.AssembleLine(SourceLine)"/> action.
        /// </summary>
        protected HashSet<string> ExcludedInstructionsForLabelDefines { get; }

        /// <summary>
        /// Gets the reserved keywords of the <see cref="AssemblerBase"/> object.
        /// </summary>
        protected ReservedWords Reserved { get; }

        /// <summary>
        /// Gets the state of the Program Counter for the <see cref="Assembler"/>'s <see cref="BinaryOutput"/>
        /// object when OnAssemble was invoked.
        /// </summary>
        protected int PCOnAssemble { get; private set; }

        #endregion
    }
}