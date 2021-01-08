//-----------------------------------------------------------------------------
// Copyright (c) 2017-2020 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

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
            IsReserved = new List<Func<StringView, bool>>();
            InstructionLookupRules = new List<Func<StringView, bool>> { sv => sv[0] == '.' };
            Options = options;
            OutputFormat = Options.Format;
            CPU = Options.CPU;
            Encoding = new AsmEncoding(Options.CaseSensitive);
            Evaluator = new Evaluator(Options.CaseSensitive) { SymbolEvaluator = EvaluateSymbol };
            SymbolManager = new SymbolManager(options.CaseSensitive, Evaluator);
            SymbolManager.AddValidSymbolNameCriterion(s => !Evaluator.IsReserved(s));
            Log = new ErrorLog(Options.WarningsAsErrors);
            Output = new BinaryOutput();
        }

        #endregion

        #region Methods

        double EvaluateSymbol(RandomAccessIterator<Token> tokens)
        {
            var token = tokens.Current;
            var subscript = -1;
            var converted = double.NaN;
            var isString = token.IsDoubleQuote();
            if (char.IsLetter(token.Name[0]) || token.Name[0] == '_')
            {
                var next = tokens.GetNext();
                if (next != null && next.IsOpen() && next.Name.Equals("["))
                    subscript = (int)Evaluator.Evaluate(tokens, 0, int.MaxValue);
                var symbol = SymbolManager.GetSymbol(token, CurrentPass > 0);
                if (symbol == null)
                {
                    PassNeeded = true;
                    return 0x100;
                }
                if (subscript >= 0)
                {
                    if (symbol.StorageType != StorageType.Vector)
                        throw new SyntaxException(token.Position, "Type mismatch.");
                    if ((symbol.IsNumeric && subscript >= symbol.NumericVector.Count) ||
                        (!symbol.IsNumeric && subscript >= symbol.StringVector.Count))
                        throw new SyntaxException(token.Position, "Index was out of range.");
                    if (symbol.IsNumeric)
                        return symbol.NumericVector[subscript];
                    token = new Token(symbol.StringVector[subscript], TokenType.Operand);
                    isString = true;
                }
                else if (symbol.IsNumeric)
                {
                    if (symbol.DataType == DataType.Address && symbol.Bank != Output.CurrentBank)
                        return (int)symbol.NumericValue | (symbol.Bank * 0x10000);
                    return symbol.NumericValue;
                }
                else
                {
                    token = new Token(symbol.StringValue, TokenType.Operand);
                    isString = true;
                }
            }
            if (isString || token.IsQuote())
            {
                // is it a string literal?
                var literal = token.IsQuote() ? token.Name.TrimOnce(token.Name[0]).ToString() : token.Name.ToString();
                if (string.IsNullOrEmpty(literal))
                    throw new SyntaxException(token.Position, "Cannot evaluate empty string.");
                literal = Regex.Unescape(literal);
                if (!isString)
                {
                    var charsize = 1;
                    if (char.IsSurrogate(literal[0]))
                        charsize++;
                    if (literal.Length > charsize)
                        throw new SyntaxException(token.Position, "Invalid char literal.");
                }
                // get the integral equivalent from the code points in the string
                converted = Encoding.GetEncodedValue(literal);
            }
            else if (token.Name.Equals("*"))
            {    // get the program counter
                converted = Output.LogicalPC;
            }
            else if (token.Name[0].IsSpecialOperator())
            {    // get the special character value
                if (token.Name[0] == '+' && CurrentPass == 0)
                {
                    converted = Output.LogicalPC;
                    PassNeeded = true;
                }
                else
                {
                    converted = SymbolManager.GetLineReference(token.Name, token);
                    if (double.IsNaN(converted))
                    {
                        var reason = token.Name[0] == '+' ? SymbolException.ExceptionReason.InvalidForwardReference :
                                                            SymbolException.ExceptionReason.InvalidBackReference;
                        throw new SymbolException(token, reason);
                    }
                }
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
        /// Increment the pass counter.
        /// </summary>
        /// <returns>The new value of the pass counter.</returns>
        public int DoNewPass()
        {
            CurrentPass++;
            SymbolManager.DefineGlobal("CURRENT_PASS", CurrentPass + 1);
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
        public List<Func<StringView, bool>> IsReserved { get; private set; }

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
        /// The format selector invoked when the output format is changed.
        /// </summary>
        public Func<string, string, IBinaryFormatProvider> FormatSelector { get; set; }

        /// <summary>
        /// Gets the output format name.
        /// </summary>
        public string OutputFormat { get; private set; }

        /// <summary>
        /// Gets or sets the CPU.
        /// </summary>
        public string CPU { get; set; }

        /// <summary>
        /// Gets a list of functions that determine
        /// whether a given keyword is a mnemonic, pseudo-op or other assembler directive.
        /// </summary>
        public List<Func<StringView, bool>> InstructionLookupRules { get; private set; }

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
        /// Gets or sets the function that determines whether a line 
        /// should terminate during processing.
        /// </summary>
        public Func<List<Token>, bool> LineTerminates { get; set; }


        /// <summary>
        /// Gets the <see cref="StringComparer"/> on the case-sensitive flag of
        /// the set <see cref="Options"/>.
        /// </summary>
        public StringComparer StringComparer
            => Options.CaseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;

        /// <summary>
        /// Gets the <see cref="StringViewComparer"/> on the case-sensitive flag of 
        /// the set <see cref="Options"/>.
        /// </summary>
        public StringViewComparer StringViewComparer
            => Options.CaseSensitive ? StringViewComparer.Ordinal : StringViewComparer.IgnoreCase;

        /// <summary>
        /// Gets the <see cref="StringComparison"/> on the case-sensitive flag of the set <see cref="Options"/>.
        /// </summary>
        public StringComparison StringComparison
            => Options.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

        #endregion
    }
}
