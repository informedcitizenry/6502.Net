//-----------------------------------------------------------------------------
// Copyright (c) 2017-2024 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

namespace Sixty502DotNet.Shared;

/// <summary>
/// Encapsulates a specific CPU architecture, which includes its encoder, instruction set for parsing and
/// its endianness.
/// </summary>
public readonly struct Architecture
{
    private Architecture(IDictionary<string, int> instructionSet, CpuEncoderBase encoder, bool isLittleEndian)
    {
        InstructionSet = instructionSet;
        Encoder = encoder;
        IsLittleEndian = isLittleEndian;
    }

    /// <summary>
    /// Construct a new <see cref="Architecture"/> from the specified cpuid.
    /// </summary>
    /// <param name="cpuid">The cpuid of the architecture.</param>
    /// <param name="services">The shared <see cref="AssemblyServices"/>
    /// for assembly runtime.</param>
    /// <returns></returns>
    public static Architecture FromCpuid(string cpuid, AssemblyServices services)
    {
        CpuEncoderBase encoder = cpuid switch
        {
            "m6800" or
            "m6809"             => new M680xInstructionEncoder(cpuid, services),
            "i8080" or
            "gb80" or
            "z80"               => new Z80InstructionEncoder(cpuid, services),
            _                   => new M65xxInstructionEncoder(cpuid, services)
        };
        return new Architecture(InstructionSets.GetByCpuid(cpuid),
                                encoder,
                                !cpuid.StartsWith("m680"));
    }

    /// <summary>
    /// Gets the default <see cref="Architecture"/> object, which is the
    /// 6502.
    /// </summary>
    /// <param name="services">The shared <see cref="AssemblyServices"/> for
    /// assembly runtime.</param>
    /// <returns></returns>
    public static Architecture Default(AssemblyServices services)
    {
        return new Architecture(InstructionSets.DefaultInstructionSet,
                                new M65xxInstructionEncoder(services),
                                true);
    }

    /// <summary>
    /// Get the architecture's instruction set characters and corresponding
    /// <see cref="SyntaxLexer"/> values. Useful for parsing source code.
    /// </summary>
    public IDictionary<string, int> InstructionSet { get; init; }

    /// <summary>
    /// Get the architecture's <see cref="CpuEncoderBase"/> for encoding
    /// source code into binary.
    /// </summary>
    public CpuEncoderBase Encoder { get; init; }

    /// <summary>
    /// Get the flag indicating whether the byte order of the target architecture
    /// is little endian.
    /// </summary>
    public bool IsLittleEndian { get; init; }
}

