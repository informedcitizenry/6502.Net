//-----------------------------------------------------------------------------
// Copyright (c) 2017-2024 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

namespace Sixty502DotNet.Shared;

/// <summary>
/// A class that saves the context for a specific instruction's code generation,
/// including the architecture CPU at the moment of code generation, the actual
/// generated code, its offset from the progrma start, and the parsed
/// <see cref="SyntaxParser.InstructionCpuContext"/> for the instruction. This
/// permits further analysis of the generated code.
/// </summary>
public sealed class CodeAnalysisContext
{
    /// <summary>
    /// Create a new instance of the <see cref="CodeAnalysisContext"/> object.
    /// </summary>
    /// <param name="cpuid">The current CPU.</param>
    /// <param name="objectCode">The object code generated.</param>
    /// <param name="offset">The offset from program start.</param>
    /// <param name="context">The parsed instruction context.</param>
    public CodeAnalysisContext(string cpuid, IReadOnlyCollection<byte> objectCode, int offset, SyntaxParser.InstructionCpuContext context)
    {
        Cpuid = cpuid;
        ObjectCode = objectCode;
        Offset = offset;
        Context = context;
    }

    /// <summary>
    /// Get or set the report based on context analysis.
    /// </summary>
    public string? Report { get; set; }

    /// <summary>
    /// Get the context's CPU.
    /// </summary>
    public string Cpuid { get; init; }

    /// <summary>
    /// Get the object code generated associated with the context.
    /// </summary>
    public IReadOnlyCollection<byte> ObjectCode { get; init; }

    /// <summary>
    /// Get the context offset from program start.
    /// </summary>
    public int Offset { get; init; }

    /// <summary>
    /// Get the parsed instruction.
    /// </summary>
    public SyntaxParser.InstructionCpuContext Context { get; init; }
}

