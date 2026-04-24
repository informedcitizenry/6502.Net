// Copyright (c) 2026 informedcitizenry
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using Sixty502DotNet.Shared.Arch;
using Sixty502DotNet.Shared.Arch.Formats;
using Sixty502DotNet.Shared.Encode;
using Sixty502DotNet.Shared.Error;
using System.Text;

namespace Sixty502DotNet.Shared;

public static class Disassembler
{
    public static string Disassemble(byte[] codeBytes, Cpu cpu, DisassemblyOptions options)
    {
        var programCounter = GetImagesAndProgramCounter(codeBytes, cpu, options, out var image);
        if (programCounter == -1 || image.Length == 0)
        {
            return string.Empty;
        }
        var length = image.Length;
        var sb = new StringBuilder($"                                 * = {programCounter:x4}");
        sb.AppendLine("\n"); // two lines 

        for (var i = 0; i < length;  )
        {
            var currBytes =  image[i..];
            if (options.ListMonitorCode)
            {
                sb.Append($".{programCounter,-7:x4}");
            }
            else
            {
                sb.Append($".{programCounter,-23:x4}");
            }
            var changedProgramCounter = programCounter;
            var disassembly = cpu switch
            {
                Cpu.M6800 or
                Cpu.M6809 => MotorolaEncoder.Decode(currBytes, cpu, ref changedProgramCounter),
                Cpu.Gb80 or
                Cpu.I8080 or
                Cpu.Z80 => ZilogIntelEncoder.Decode(currBytes, cpu, ref changedProgramCounter),
                Cpu.I86 => I86Encoder.Decode(currBytes, ref changedProgramCounter),
                _ => M65xxEncoder.Decode(currBytes, cpu, options.M16, options.X16, ref changedProgramCounter)
            };
            var disassemblyCount = changedProgramCounter - programCounter;
            if (options.ListMonitorCode)
            {
                for (var b = 0; b < disassemblyCount && b < currBytes.Length; b++)
                {
                    sb.Append($" {currBytes[b]:x2}");
                }
                for (var y = 23 - disassemblyCount * 3; y >= 0; y--)
                {
                    sb.Append(' ');
                }
            }
            if (options.ListDisassembly)
            {
                sb.Append(disassembly);
            }
            sb.AppendLine();
            i += disassemblyCount;
            programCounter = changedProgramCounter;
        }
        return sb.ToString();
    }

    private static int GetImagesAndProgramCounter
    (
        byte[] codeBytes, 
        Cpu cpu,
        DisassemblyOptions options, 
        out byte[] image
    )
    {
        int offset = options.Offset, 
            startAddress = options.StartAddress, 
            endAddress = options.EndAddress;
        if (!string.IsNullOrEmpty(options.Format) ||
            (startAddress == -1 && endAddress == -1))
        {
            OutputFormat fmt;
            if (string.IsNullOrEmpty(options.Format))
            {
                fmt = cpu switch
                {
                    Cpu.C64Dtv2 or 
                    Cpu.HuC6280 or 
                    Cpu.M65 or 
                    Cpu.M65C02 or 
                    Cpu.M65Ce02 or 
                    Cpu.M6502 or 
                    Cpu.M6502I or 
                    Cpu.M65816 or 
                    Cpu.R65C02 => OutputFormat.Cbm,
                   _ => OutputFormat.Flat 
                };
            }
            else
            {
                var format = FormatLookup.ByName(options.Format);
                if (format == null)
                {
                    throw new DecodeException($"Invalid format `{format}` specified.");
                }
                fmt = format.Value;
            }
            IOutputFormatProvider provider = fmt switch
            {
                OutputFormat.AmsDos or
                OutputFormat.AmsTap or
                OutputFormat.Msx or
                OutputFormat.Zx => new Z80FormatProvider(fmt),
                OutputFormat.ByteSource => new ByteSourceFormatProvider(),
                OutputFormat.D64 => new D64FormatProvider(),
                OutputFormat.Cart => new C64CartFormatProvider(),
                OutputFormat.Cpm => new CpmFormatProvider(),
                OutputFormat.Flat => new FlatFormatProvider(),
                OutputFormat.Mz => new MzFormatProvider(),
                OutputFormat.Hex => new HexFormatProvider(),
                OutputFormat.Hex86 => new Hex86FormatProvider(),
                OutputFormat.SRecMos or OutputFormat.SRecord => new SRecordFormatProvider(fmt),
                _ => new M65xxFormatProvider(fmt)
            };
            var descriptor = provider.Describe(codeBytes);
            if (fmt is OutputFormat.ByteSource or
                OutputFormat.Hex or
                OutputFormat.Hex86 or
                OutputFormat.SRecord or
                OutputFormat.SRecMos)
            {
                startAddress = descriptor.StartAddress;
                image = descriptor.Object.Skip(offset).ToArray();
            }
            if (descriptor.Length > 0)
            {
                if (descriptor.Length - offset < 0)
                {
                    image = [];
                    return -1;
                }
                startAddress = descriptor.StartAddress;
                image = descriptor.Object.Skip(offset).Take(descriptor.Length - offset).ToArray();
            }
            else
            {
                if (descriptor.FoundProblem)
                {
                    throw new DecodeException("The specified format cannot support the input file.");
                }
                image = [];
            }
        }
        else
        {
            if (startAddress == -1)
            {
                startAddress = 0;
            }
            if (endAddress == -1)
            {
                endAddress = startAddress + codeBytes.Length;
            }
            var length = endAddress - startAddress;
            if (length < 0 || length + offset > codeBytes.Length)
            {
                image = [];
                return -1;
            }
            image = codeBytes.Skip(offset).Take(length).ToArray();
        }
        return startAddress;
    }
}