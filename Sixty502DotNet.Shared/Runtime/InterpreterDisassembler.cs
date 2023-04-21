//-----------------------------------------------------------------------------
// Copyright (c) 2017-2023 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using Antlr4.Runtime;
using System;
using System.Text;

namespace Sixty502DotNet.Shared;

public sealed partial class Interpreter
{
    private static readonly Dictionary<string, FormatInfo> _formats = new()
    {
        { "apple2", new FormatInfo(0, 4) },
        { "atari-xex", new FormatInfo(2, 6) },
        { "cbm", new FormatInfo(0, 2) },
        { "flat", new FormatInfo(-1, 0) }
    };

    /// <summary>
    /// Disassemble a buffer of binary data.
    /// </summary>
    /// <param name="objectCode">The object code as a list of byte buffers.
    /// </param>
    /// <param name="programCounter">The initial program counter.</param>
    /// <param name="offset">The offset within the byte buffers to begin
    /// disassembly.</param>
    /// <returns>The disassembled bytes as a string.</returns>
    /// <exception cref="Error"></exception>
    public string Disassemble(List<byte[]> objectCode, int? programCounter, int? offset)
    {
        string cpuid = _options.ArchitectureOptions.Cpuid ?? "6502";

        Architecture architecture = Architecture.FromCpuid(cpuid, Services);
        _encoder = architecture.Encoder;
        _encoder.SetCpu(cpuid);

        string format = _options.OutputOptions.Format ?? "cbm";
        if (!_formats.TryGetValue(format, out FormatInfo info))
        {
            throw new Error($"Format '{format}' not accepted for disassembler");
        }
        int startPc = 0;
        if (programCounter.HasValue)
        {
            if (info.StartAddressOffset >= 0)
            {
                AddWarning((IToken?)null, "Program counter option ignored due to format specified");    
            }
            if (programCounter.Value < 0 || programCounter.Value > ushort.MaxValue)
            {
                throw new Error("Disssembler start program counter requires unsigned 16-bit integral value");
            }
            startPc = programCounter.Value;
        }
        int trueOffset = info.ObjectCodeOffset;
        if (offset.HasValue)
        {
            if (offset.Value < 0 || offset.Value > ushort.MaxValue)
            {
                throw new Error("Disssembler offset requires unsigned 16-bit integral value");
            }
            trueOffset = offset.Value;
        }
        StringBuilder sb = new();
        for (int i = 0; i < objectCode.Count; i++)
        {
            if (info.StartAddressOffset >= 0 && objectCode[i].Length > info.StartAddressOffset + 1)
            {
                startPc = objectCode[i][0] + objectCode[i][1] * 256;
            }
            if (trueOffset >= objectCode[i].Length)
            {
                continue;
            }
            sb.AppendLine(_encoder.DecodeBuffer(objectCode[i], trueOffset, startPc, _options.OutputOptions.NoAssembly));
        }
        return sb.ToString();
    }
}

