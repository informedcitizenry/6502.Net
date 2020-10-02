//-----------------------------------------------------------------------------
// Copyright (c) 2017-2020 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

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
    /// A service for all shared assembly resources and state, such as
    /// <see cref="ErrorLog"/> and <see cref="BinaryOutput"/> support classes.
    /// </summary>
    public class AssemblyServices
    {
        #region Constructors

        /// <summary>
        /// Creates a new instance of the assembly service.
        /// </summary>
        /// <param name="options">The parsed options.</param>
        public AssemblyServices(Options options)
        {
            PassNeeded = true;
            CurrentPass = -1;
            IsReserved = new List<Func<string, bool>>();
            InstructionLookupRules = new List<Func<string, bool>>();
            Options = options;
            OutputFormat = Options.Format;
            CPU = Options.CPU;
            Encoding = new AsmEncoding(Options.CaseSensitive);
            SymbolManager = new SymbolManager(this);
            Evaluator = new Evaluator(Options.CaseSensitive, EvaluateSymbol);
            SymbolManager.AddValidSymbolNameCriterion(s => !Evaluator.IsReserved(s));
            Evaluator.AddFunctionEvaluator(SymbolManager);
            Log = new ErrorLog(Options.WarningsAsErrors);
            Output = new BinaryOutput();
        }

        #endregion

        #region Methods

        double EvaluateSymbol(Token token, Token subscript)
        {
            // no, is it a named symbol?
            var converted = double.NaN;
            if (char.IsLetter(token.Name[0]) || token.Name[0] == '_')
            {
                if (subscript != null)
                    converted = SymbolManager.GetNumericVectorElementValue(token, subscript);
                else
                    converted = SymbolManager.GetNumericValue(token);
                // on first pass this will be true if symbol not yet defined
                if (double.IsNaN(converted) && CurrentPass == 0) 
                    converted = 0xffff;
            }
            else if (token.Name.EnclosedInQuotes())
            {
                // is it a string literal?
                var literal = token.Name.TrimOnce(token.Name[0]);
                if (string.IsNullOrEmpty(literal))
                    throw new SyntaxException(token.Position, $"Cannot evaluate empty string.");

                // get the integral equivalent from the code points in the string
                converted = Encoding.GetEncodedValue(token.Name.TrimOnce(token.Name[0]));
            }
            else if (token.Name.Equals("*"))
            {    // get the program counter
                converted = Output.LogicalPC;
            }
            else if (token.Name[0].IsSpecialOperator())
            {    // get the special character value
                converted = SymbolManager.GetLineReference(token);
            }
            if (double.IsNaN(converted))
                throw new ExpressionException(token.Position,
                    $"\"{token.Name}\" is not a expression.");
            return converted;
        }


        /// <summary>
        /// Selects the binary format of the output.
        /// </summary>
        /// <param name="format">The output format.</param>
        public void SelectFormat(string format)
        {
            if (!Options.CaseSensitive) format = format.ToLower();
            OutputFormat = format;
        }

        /// <summary>
        /// Selects the CPU.
        /// </summary>
        /// <param name="cpu">The CPU family/name.</param>
        public void SelectCPU(string cpu)
        {
            if (!Options.CaseSensitive) cpu = cpu.ToLower();
            CPU = cpu;
            CPUAssemblerSelector?.Invoke(cpu);
        }

        /// <summary>
        /// Increment the pass counter.
        /// </summary>
        /// <returns>The new value of the pass counter.</returns>
        public int DoNewPass()
        {
            CurrentPass++;
            SymbolManager.Define("CURRENT_PASS", CurrentPass + 1, false);
            PassChanged?.Invoke(this, new EventArgs());
            PassNeeded = false;
            return CurrentPass;
        }

        #endregion

        #region Properties

        /// <summary>
        /// The <see cref="SymbolManager"/> responsible for all symbol definitions
        /// and references.
        /// </summary>
        public SymbolManager SymbolManager { get; }

        /// <summary>
        /// Gets or sets the number of passes attempted. Setting this
        /// property resets the PassNeeded property.
        /// </summary>
        public int CurrentPass { get; private set; }

        /// <summary>
        /// An event that signals to subscribing handlers that a pass has changed.
        /// </summary>
        public event PassesChangedEventHandler PassChanged;

        /// <summary>
        /// Gets or sets the flag that determines if another pass is needed. 
        /// This field is reset when the Passes property changes.
        /// </summary>
        public bool PassNeeded { get; set; }

        /// <summary>
        /// The <see cref="global::Core6502DotNet.Options"/> object responsible for parsing
        /// and enumerating all command line options.
        /// </summary>
        public Options Options { get; }

        /// <summary>
        /// Gets the list of functions that
        /// determine whether a given keyword is reserved.
        /// </summary>
        public List<Func<string, bool>> IsReserved { get; private set; }

        /// <summary>
        /// The <see cref="ErrorLog"/> object used for all error and warning
        /// logging.
        /// </summary>
        public ErrorLog Log { get; }

        /// <summary>
        /// Gets or sets the flag that determines whether disassembly should print.
        /// </summary>
        public bool PrintOff { get; set; }

        /// <summary>
        /// The <see cref="BinaryOutput"/> object managing all output of assembly.
        /// </summary>
        public BinaryOutput Output { get; }
        
        /// <summary>
        /// The CPU Assembler selector method.
        /// </summary>
        public Action<string> CPUAssemblerSelector { get; set; }

        /// <summary>
        /// The format selector invoked when the output format is changed.
        /// </summary>
        public Func<string, string, IBinaryFormatProvider> FormatSelector { get; set; }

        /// <summary>
        /// Gets the output format name.
        /// </summary>
        public string OutputFormat { get; private set; }

        /// <summary>
        /// Gets the CPU.
        /// </summary>
        public string CPU { get; private set; }

        /// <summary>
        /// Gets a list of functions that determine
        /// whether a given keyword is a mnemonic, pseudo-op or other assembler directive.
        /// </summary>
        public List<Func<string, bool>> InstructionLookupRules { get; private set; }

        /// <summary>
        /// The <see cref="AsmEncoding"/> responsible for all encoding.
        /// </summary>
        public AsmEncoding Encoding { get; }

        /// <summary>
        /// The <see cref="Evaluator"/> responsible for evaluating all 
        /// mathematical and logical operations.
        /// </summary>
        public Evaluator Evaluator { get; }

        /// <summary>
        /// Gets the <see cref="StringComparer"/> on the case-sensitive flag of
        /// the set <see cref="Options"/>.
        /// </summary>
        public StringComparer StringComparer
            => Options.CaseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;

        /// <summary>
        /// Gets the <see cref="StringComparison"/> on the case-sensitive flag of the set <see cref="Options"/>.
        /// </summary>
        public StringComparison StringComparison
            => Options.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

        #endregion
    }
}
