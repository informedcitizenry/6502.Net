//-----------------------------------------------------------------------------
// Copyright (c) 2017-2019 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

namespace DotNetAsm
{
    /// <summary>
    /// Provides utility for common operations during text processing and assembly,
    /// such as logging errors, evaluating expression, and inspecting the 
    /// options passed in the command line.
    /// </summary>
    public static class Assembler
    {
        #region static Constructors

        /// <summary>
        /// Initialize the static members of this instance.
        /// </summary>
        public static void Initialize()
        {
            Options = new AsmCommandLineOptions();

            Evaluator = new Evaluator(false);

            Encoding = new AsmEncoding(false);

            Output = new Compilation();

            Symbols = new SymbolManager();

            Log = new ErrorLog();
        }

        /// <summary>
        /// Initialize the static members of this instance. The specified args
        /// passed will re-initialize the common Options object.
        /// </summary>
        /// <param name="args">The commandline arguments.</param>
        public static void Initialize(string[] args)
        {
            Options = new AsmCommandLineOptions();

            Options.ParseArgs(args);

            Evaluator = new Evaluator(Options.CaseSensitive);

            Encoding = new AsmEncoding(Options.CaseSensitive);

            Output = new Compilation();

            Symbols = new SymbolManager();

            Log = new ErrorLog();
        }

        #endregion

        #region Static Properties

        /// <summary>
        /// The Compilation object to handle binary output.
        /// </summary>
        public static Compilation Output { get; private set; }

        /// <summary>
        /// Gets the command-line arguments passed by the end-user and parses into 
        /// strongly-typed options.
        /// </summary>
        public static AsmCommandLineOptions Options { get; private set; }

        /// <summary>
        /// The controller's error log to track errors and warnings.
        /// </summary>
        public static ErrorLog Log { get; private set; }

        /// <summary>
        /// Gets the symbols for the controller.
        /// </summary>
        /// <value>The symbols.</value>
        public static ISymbolManager Symbols { get; private set; }

        /// <summary>
        /// Gets expression evaluator for the controller.
        /// </summary>
        public static IEvaluator Evaluator { get; private set; }

        /// <summary>
        /// Gets the custom DotNetAsm.AsmEncoding for encoding text strings.
        /// </summary>
        public static AsmEncoding Encoding { get; private set; }

        #endregion
    }

    /// <summary>
    ///The base assembler class. Must be inherited.
    /// </summary>
    public abstract class AssemblerBase
    {

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="T:DotNetAsm.AssemblerBase"/> class.
        /// </summary>
        /// <param name="args">Commandline arguments to initialize the 
        /// <see cref="DotNetAsm.Assembler"/> class, which is a common object for 
        /// various assembler activities.</param>
        protected AssemblerBase(string[] args)
        {
            Assembler.Initialize(args);

            Reserved = new ReservedWords(Assembler.Options.StringComparar);
        }

        /// <summary>
        /// Constructs an instance of the class implementing the base class.
        /// </summary>
        protected AssemblerBase() => Reserved = new ReservedWords(Assembler.Options.StringComparar);

        #endregion

        #region Methods

        /// <summary>
        /// Determines whether the token is a reserved word to the assembler object.
        /// </summary>
        /// <param name="token">The token to check if reserved</param>
        /// <returns><c>True</c> if reserved, otherwise <c>false</c>.</returns>
        public virtual bool IsReserved(string token) => Reserved.IsReserved(token);

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the reserved keywords of the object.
        /// </summary>
        protected ReservedWords Reserved { get; set; }

        #endregion
    }
}