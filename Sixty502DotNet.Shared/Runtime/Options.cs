//-----------------------------------------------------------------------------
// Copyright (c) 2017-2024 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

namespace Sixty502DotNet.Shared;

/// <summary>
/// The various architecture-related options for the assembly.
/// </summary>
public readonly struct ArchitectureOptions
{
    /// <summary>
    /// Get the architecture cpuid.
    /// </summary>
    public string? Cpuid { get; init; }

    /// <summary>
    /// Get the flag indicating registers in 65816 should be auto-sized
    /// for certain addressing modes when a <c>REP</c>/<c>SEP</c> instruction
    /// is processed.
    /// </summary>
    public bool AutosizeRegisters { get; init; }

    /// <summary>
    /// Get the flag indicating that for 6502 the <c>BRA</c> mnemonic should
    /// be recognized as an alias for the <c>BVC</c> mnemonic.
    /// </summary>
    public bool BranchAlways { get; init; }

    /// <summary>
    /// Get the flag indicating that long (24 bit) addressing should be enabled.
    /// </summary>
    public bool LongAddressing { get; init; }
}

/// <summary>
/// General options the assembler should follow when processing input.
/// </summary>
public readonly struct GeneralOptions
{
    /// <summary>
    /// Get the flag indicating whether symbols and keywords should be
    /// case-sensitive.
    /// </summary>
    public bool CaseSensitive { get; init; }

    /// <summary>
    /// Get the flag indicating that when a <c>.bank</c> directive is processed,
    /// the program counter should reset.
    /// </summary>
	public bool ResetPCOnBank { get; init; }

    /// <summary>
    /// Get a list of defines. Useful for command-line arguments.
    /// </summary>
    public IList<string>? Defines { get; init; }

    /// <summary>
    /// Get the include path for resolving file or source paths.
    /// </summary>
    public string? IncludePath { get; init; }

    /// <summary>
    /// Get a list of sections. Usefor for command-line arguments.
    /// </summary>
    public IList<string>? Sections { get; init; }
}

/// <summary>
/// The output options for the assembler.
/// </summary>
public readonly struct OutputOptions
{
    /// <summary>
    /// Get the output file format.
    /// </summary>
	public string? Format { get; init; }

    /// <summary>
    /// Get the flag indicating that listing should not include original source.
    /// </summary>
	public bool NoSource { get; init; }

    /// <summary>
    /// Get the flag indicating that listing should not include monitor code.
    /// </summary>
	public bool NoAssembly { get; init; }

    /// <summary>
    /// Get the flag indicating that listing should not include disassembly.
    /// </summary>
	public bool NoDisassembly { get; init; }

    /// <summary>
    /// Get the flag if symbol listing should be restricted only to labels.
    /// </summary>
	public bool LabelsOnly { get; init; }

    /// <summary>
    /// Get the flag if symbol listing should be in VICE debugger format.
    /// </summary>
    public bool ViceLabels { get; init; }

    /// <summary>
    /// Get the flag indicating that assembly bytes should be truncated.
    /// </summary>
	public bool TruncateAssembly { get; init; }

    /// <summary>
    /// Get the flag indicating that listing should include all whitespace,
    /// include newline characters.
    /// </summary>
    public bool VerboseList { get; init; }
}

/// <summary>
/// The diagnostic options for the assembler.
/// </summary>
public readonly struct DiagnosticOptions
{
    /// <summary>
    /// Get whether <code>.echo</code> directives should be evaluated each pass.
    /// By default this directive is executed only on first pass.
    /// </summary>
	public bool EchoEachPass { get; init; }

    /// <summary>
    /// Get the flag about disabling highlighting on reporting.
    /// </summary>
	public bool NoHighlighting { get; init; }

    /// <summary>
    /// Warn about the 6502 indirect jump bug
    /// </summary>
    public bool WarnJumpBug { get; init; }
    /// <summary>
    ///  Warn about non-text data operands in text pseudo-ops
    /// </summary>
    public bool WarnTextInNonTextPseudoOp { get; init; }
    /// <summary>
    /// Treat all warnings as errors
    /// </summary>
    public bool WarningsAsErrors { get; init; }
    /// <summary>
    /// Warn of whitespace before labels
    /// </summary>
    public bool WarnWhitespaceBeforeLabels { get; init; }
    /// <summary>
    /// Do not warn about unused sections
    /// </summary>
    public bool DoNotWarnAboutUnusedSections { get; init; }
    /// <summary>
    /// Warn if an identifier's case does not match that
    /// of the defined symbol's
    /// </summary>
    public bool WarnCaseMismatch { get; init; }
    /// <summary>
    /// Warn when registers are being used as identifiers.
    /// </summary>
    public bool WarnRegistersAsIdentifiers { get; init; }
    /// <summary>
    /// Warn when call/returns can be simplified
    /// </summary>
    public bool WarnSimplifyCallReturn { get; init; }
    /// <summary>
    /// Warn about unreferenced symbols
    /// </summary>
    public bool WarnOfUnreferencedSymbols { get; init; }
}

/// <summary>
/// The various assembler options.
/// </summary>
public readonly struct Options
{
    /// <summary>
    /// Get the architecture options.
    /// </summary>
	public ArchitectureOptions ArchitectureOptions { get; init; }

    /// <summary>
    /// Get the general options.
    /// </summary>
	public GeneralOptions GeneralOptions { get; init; }

    /// <summary>
    /// Get the output options.
    /// </summary>
	public OutputOptions OutputOptions { get; init; }

    /// <summary>
    /// Get the diagnostic options.
    /// </summary>
	public DiagnosticOptions DiagnosticOptions { get; init; }
}

