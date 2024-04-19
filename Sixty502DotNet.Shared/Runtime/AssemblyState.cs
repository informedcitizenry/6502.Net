//-----------------------------------------------------------------------------
// Copyright (c) 2017-2024 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

namespace Sixty502DotNet.Shared;

/// <summary>
/// A record of the runtime state of various assembly attributes, such as
/// the current pass, statement, and starting program counter value.
/// </summary>
public sealed class AssemblyState
{
    /// <summary>
    /// Create a new <see cref="AssemblyState"/> object, which tracks various
    /// assembly runtime state elements, such as code output, statement listings,
    /// and defined symbols.
    /// </summary>
    /// <param name="symbols">The <see cref="SymbolManager"/> for the state.</param>
    public AssemblyState(SymbolManager symbols)
    {
        Symbols = symbols;
        Output = new();
        Errors = new();
        Warnings = new();
        PrintOff = false;
        StatementListings = new List<string>();
    }

    /// <summary>
    /// Reset the current assembly state and increment the current pass value.
    /// </summary>
    public void Reset()
    {
        PrintOff = false;
        CurrentPass++;
        PassNeeded = false;
        Output.Reset();
        Symbols.Reset();
        StatementListings.Clear();
        StatementIndex = 0;
        Warnings.Clear();
    }

    /// <summary>
    /// Gets or sets the flag that determines whether disassembly should print.
    /// </summary>
    public bool PrintOff { get; set; }

    /// <summary>
    /// Gets or sets the number of passes attempted. Setting this
    /// property resets the PassNeeded property.
    /// </summary>
    public int CurrentPass { get; set; }

    /// <summary>
    /// Gets whether the state of the assembler is in the first pass still.
    /// </summary>
    public bool InFirstPass => CurrentPass <= 1;

    /// <summary>
    /// Gets or sets the flag that determines if another pass is needed. 
    /// This field is reset when the Passes property changes.
    /// </summary>
    public bool PassNeeded { get; set; }

    /// <summary>
    /// Get or set the initial long logical program counter at the start
    /// of the current statement assembly.
    /// </summary>
    public int LongLogicalPCOnAssemble { get; set; }

    /// <summary>
    /// Get or set the initial logical program counter at the start of the
    /// current statement assembly.
    /// </summary>
    public int LogicalPCOnAssemble { get; set; }

    /// <summary>
    /// Get the symbol manager responsible for resolving all symbols.
    /// </summary>
    public SymbolManager Symbols { get; }

    /// <summary>
    /// Get the <see cref="CodeOutput"/> object managing all output of assembly.
    /// </summary
    public CodeOutput Output { get; }

    /// <summary>
    /// Get the statement listings as a list of strings.
    /// </summary>
    public IList<string> StatementListings { get; }

    /// <summary>
    /// Get or set the index of the current statement being executed. 
    /// </summary>
    public int StatementIndex { get; set; }

    /// <summary>
    /// Get all errors that occurred during assembly.
    /// </summary>
    public List<Error> Errors { get; }

    /// <summary>
    /// Get all warnings that occurred during assembly.
    /// </summary>
    public List<Warning> Warnings { get; }
}
