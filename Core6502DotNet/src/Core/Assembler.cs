//-----------------------------------------------------------------------------
// Copyright (c) 2017-2020 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;
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
    /// A delegate that defines a function responsible for writing header data
    /// to assembly output.
    /// </summary>
    /// <returns></returns>
    public delegate IEnumerable<byte> HeaderWriter();

    /// <summary>
    /// A static class holding all shared assembly resources and state, such as
    /// <see cref="ErrorLog"/> and <see cref="BinaryOutput"/> support classes.
    /// </summary>
    public static class Assembler
    {
        #region Members

        static int _pass;

        #endregion

        #region Methods

        /// <summary>
        /// Initialize the <see cref="Assembler"/> class.
        /// </summary>
        /// <param name="args">The command line arguments passed by
        /// the user.</param>
        public static void Initialize(string[] args)
        {
            Options = new CommandLineOptions();
            Options.ParseArgs(args);
            IsReserved = new List<Func<string, bool>>();
            InstructionLookupRules = new List<Func<string, bool>>();
            Log = new ErrorLog();
            Encoding = new AsmEncoding(Options.CaseSensitive);
            SymbolManager = new SymbolManager();
            Output = new BinaryOutput();
            _pass = -1;
            PrintOff = false;
            PassNeeded = true;
        }

        #endregion

        #region Properties

        /// <summary>
        /// The <see cref="SymbolManager"/> responsible for all symbol definitions
        /// and references.
        /// </summary>
        public static SymbolManager SymbolManager { get; set; }

        /// <summary>
        /// The <see cref="AsmEncoding"/> responsible for all encoding.
        /// </summary>
        public static AsmEncoding Encoding { get; set; }

        /// <summary>
        /// The <see cref="ErrorLog"/> object used for all error and warning
        /// logging.
        /// </summary>
        public static ErrorLog Log { get; set; }

        /// <summary>
        /// The <see cref="BinaryOutput"/> object managing all output of assembly.
        /// </summary>
        public static BinaryOutput Output { get; set; }

        /// <summary>
        /// The <see cref="CommandLineOptions"/> object responsible for parsing
        /// and enumerating all command line options.
        /// </summary>
        public static CommandLineOptions Options { get; set; }


        /// <summary>
        /// An event that signals to subscribing handlers that a pass has changed.
        /// </summary>
        public static event PassesChangedEventHandler PassChanged;

        /// <summary>
        /// Gets or sets the number of passes attempted. Setting this
        /// property resets the PassNeeded property.
        /// </summary>
        public static int CurrentPass
        {
            get => _pass;
            set
            {
                _pass = value;
                PassChanged?.Invoke(null, new EventArgs());
                PassNeeded = false;
            }
        }

        /// <summary>
        /// Gets or sets the flag that determines if another pass is needed. 
        /// This field is reset when the Passes property changes.
        /// </summary>
        public static bool PassNeeded { get; set; }

        /// <summary>
        /// The list of functions that
        /// determine whether a given keyword is reserved.
        /// </summary>
        public static List<Func<string, bool>> IsReserved { get; set; }

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
        public static List<Func<string, bool>> InstructionLookupRules { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="SourceLine"/> iterator associated with the assembler session.
        /// </summary>
        public static RandomAccessIterator<SourceLine> LineIterator { get; set; }


        /// <summary>
        /// Gets or sets a custom binary format provider to convert the output before 
        /// assembling to disk.
        /// </summary>
        public static IBinaryFormatProvider BinaryFormatProvider { get; set; }

        /// <summary>
        /// Gets the current <see cref="SourceLine"/> from the iterator.
        /// </summary>
        public static SourceLine CurrentLine => LineIterator.Current;

        /// <summary>
        /// Gets the <see cref="StringComparer"/> base on the case-sensitive flag of
        /// the set <see cref="Options"/>.
        /// </summary>
        public static StringComparer StringComparer 
            => Options.CaseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;

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
        /// <returns><c>True</c> if reserved, otherwise <c>false</c>.</returns>
        public virtual bool IsReserved(string token) => Reserved.IsReserved(token);

        public virtual bool Assembles(string s) => IsReserved(s);

        public virtual bool AssemblesLine(SourceLine line) => (!string.IsNullOrEmpty(line.LabelName) && line.Instruction == null)
            || IsReserved(line.InstructionName);

        /// <summary>
        /// Assemble the <see cref="SourceLine"/>. This method must be inherited.
        /// </summary>
        /// <param name="line">The source line.</param>
        protected abstract string OnAssembleLine(SourceLine line);

        public string AssembleLine(SourceLine line)
        {
            bool isReference = false;
            if (!string.IsNullOrEmpty(line.LabelName) && (line.Instruction == null ||
                (!line.InstructionName.Equals(".equ") && !line.InstructionName.Equals("=") && !line.InstructionName.Equals(".function"))))
            {
                if (line.LabelName.Equals("+") || line.LabelName.Equals("-"))
                {
                    isReference = true;
                    Assembler.SymbolManager.DefineLineReference(line.LabelName,
                                                                  Assembler.Output.LogicalPC);
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
        /// Gets or sets the reserved keywords of the <see cref="AssemblerBase"/> object.
        /// </summary>
        protected ReservedWords Reserved { get; set; }

        #endregion
    }
}