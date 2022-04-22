//-----------------------------------------------------------------------------
// Copyright (c) 2017-2022 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Sixty502DotNet
{
    /// <summary>
    /// A service for all shared assembly resources and state, such as
    /// <see cref="ErrorLog"/>, <see cref="CodeOutput"/> , and
    /// <see cref="SymbolManager"/> support classes.
    /// </summary>
    public class AssemblyServices
    {
        /// <summary>
        /// Construct a new instance of the <see cref="AssemblyServices"/>
        /// class.
        /// </summary>
        /// <param name="options">The parsed command-line options.</param>
        public AssemblyServices(Options options)
        {
            Options = options;
            Encoding = new AsmEncoding();
            Output = new CodeOutput();
            Log = new ErrorLog(Options.WarningsAsErrors);
            Symbols = new SymbolManager(Options.CaseSensitive);
            BuiltInSymbols.Define(this);
            ExpressionVisitor = new ExpressionVisitor(this);
            StatementListings = new List<string>();
            LabelListing = new LabelListing();
            CPU = OutputFormat = string.Empty;
            State = new AssemblyState();
        }

        /// <summary>
        /// The <see cref="ErrorLog"/> object used for all error and warning
        /// logging.
        /// </summary>
        public ErrorLog Log { get; }

        /// <summary>
        /// The <see cref="AsmEncoding"/> responsible for all encoding.
        /// </summary>
        public AsmEncoding Encoding { get; init; }

        /// <summary>
        /// The <see cref="ExpressionVisitor"/> responsible for visitng 
        /// expression parse trees and returning a resulting <see cref="IValue"/>.
        /// </summary>
        public ExpressionVisitor ExpressionVisitor { get; init; }

        /// <summary>
        /// The <see cref="CodeOutput"/> object managing all output of assembly.
        /// </summary
        public CodeOutput Output { get; init; }

        /// <summary>
        /// Gets the output format name.
        /// </summary>
        public string OutputFormat { get; private set; }

        /// <summary>
        /// Gets or sets the CPU.
        /// </summary>
        public string CPU { get; set; }

        /// <summary>
        /// Gets the <see cref="SymbolManager"/> responsible for tracking all
        /// defined symbols.
        /// </summary>
        public SymbolManager Symbols { get; init; }

        /// <summary>
        /// Get the current assembly state.
        /// </summary>
        public AssemblyState State { get; init; }

        /// <summary>
        /// The <see cref="Sixty502DotNet.Options"/> object responsible for parsing
        /// and enumerating all command line options.
        /// </summary>
        public Options Options { get; }

        /// <summary>
        /// Get the statement listings as a list of strings.
        /// </summary>
        public List<string> StatementListings { get; init; }

        /// <summary>
        /// Get the <see cref="LabelListing"/> object.
        /// </summary>
        public LabelListing LabelListing { get; init; }

        /// <summary>
        /// Gets the <see cref="System.StringComparer"/> on the case-sensitive
        /// flag of the set <see cref="Sixty502DotNet.Options"/>.
        /// </summary>
        public StringComparer StringComparer
            => Options.CaseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;

        /// <summary>
        /// Gets the <see cref="System.StringComparison"/> on the case-sensitive
        /// flag of the set <see cref="Sixty502DotNet.Options"/>.
        /// </summary>
        public StringComparison StringComparison
            => Options.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
    }
}
