//-----------------------------------------------------------------------------
// Copyright (c) 2017-2024 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

namespace Sixty502DotNet.Shared;

/// <summary>
/// A service for all shared assembly resources and state.
/// </summary>
public sealed class AssemblyServices
{
    /// <summary>
    /// Construct a new instance of the <see cref="AssemblyServices"/> class,
    /// which provides resources available to all assembly processes.
    /// </summary>
    public AssemblyServices()
    {
        State = new(new SymbolManager(false));
        Evaluator = new(this);
        Encoding = new(false);
    }

    /// <summary>
    /// Construct a new instance of the <see cref="AssemblyServices"/> class,
    /// which provides resources available to all assembly processes.
    /// </summary>
    /// <param name="interpreter">The <see cref="Interpreter"/> that will
    /// execute statements.
    /// <param name="symbolsCaseSensitive">The flag indicating whether all
    /// symbols and keywords should be case-sensitive.</param>
    /// <param name="architectureOptions">The current target architecture
    /// options.</param>
    /// <param name="diagnosticOptions">The diagnostic options.</param>
    public AssemblyServices(Interpreter interpreter,
                            bool symbolsCaseSensitive,
                            ArchitectureOptions architectureOptions,
                            DiagnosticOptions diagnosticOptions)
    {
        State = new(new SymbolManager(symbolsCaseSensitive));
        Interpreter = interpreter;
        Evaluator = new(this);
        Encoding = new(false);
        ArchitectureOptions = architectureOptions;
        DiagnosticOptions = diagnosticOptions;
    }

    /// <summary>
    /// Get whether symbol and keyword recognition should be case-sensitive.
    /// </summary>
    public bool IsCaseSensitive => State.Symbols.GlobalScope.IsCaseSensitive;

    /// <summary>
    /// Get the current assembly state.
    /// </summary>
    public AssemblyState State { get; init; }

    /// <summary>
    /// Get the <see cref="Evaluator"/> responsible for evaluating  
    /// expressions and returning a resulting value.
    /// </summary>
    public Evaluator Evaluator { get; init; }

    /// <summary>
    /// Get the current encoding for generating text data.
    /// </summary>
    public AsmEncoding Encoding { get; init; }

    /// <summary>
    /// Get the options associated to the current target architecture.
    /// </summary>
    public ArchitectureOptions ArchitectureOptions { get; init; }

    /// <summary>
    /// Get the diagnostic options for warning and error handling.
    /// </summary>
    public DiagnosticOptions DiagnosticOptions { get; init; }

    /// <summary>
    /// Get the <see cref="Interpreter"/> responsible for executing
    /// statements and generating code.
    /// </summary>
    public Interpreter? Interpreter { get; init; }
}

