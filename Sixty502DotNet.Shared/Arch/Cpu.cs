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

using Sixty502DotNet.Shared.Compile;
using System.Collections.Frozen;

namespace Sixty502DotNet.Shared.Arch;

public enum Cpu
{
    C64Dtv2,
    M6502,
    M6502I,
    M65816,
    M65C02,
    M65Ce02,
    M65,
    R65C02,
    HuC6280,
    M6800,
    M6809,
    I8080,
    I86,
    Gb80,
    Z80,
}

public static class CpuLookup
{
    public static bool IsInFamilyWith(this Cpu cpu, Cpu other)
    {
        return cpu switch
        {
            Cpu.I86 => other == Cpu.I86,
            Cpu.M6800 or Cpu.M6809 => other switch
            {
                Cpu.M6800 or Cpu.M6809 => true,
                _ => false
            },
            Cpu.I8080 or Cpu.Gb80 or Cpu.Z80 => other switch
            {
                Cpu.I8080 or Cpu.Gb80 or Cpu.Z80 => true,
                _ => false
            },
            _ => other switch
            {
                Cpu.M65 or 
                Cpu.M65816 or 
                Cpu.R65C02 or 
                Cpu.HuC6280 or 
                Cpu.M65C02 or 
                Cpu.C64Dtv2 or 
                Cpu.M65Ce02 or
                Cpu.M6502I or 
                Cpu.M6502 => true,
                _ => false
            }
        };
    }

    private static readonly FrozenDictionary<Cpu, string> s_cpuNamesReverseLookup
        = new Dictionary<Cpu, string>
    {
        { Cpu.C64Dtv2, "c64dtv2" },
        { Cpu.M6502, "6502" },
        { Cpu.M6502I, "6502i" },
        { Cpu.M65816, "65816" },
        { Cpu.M65C02, "65C02" },
        { Cpu.M65Ce02, "65CE02" },
        { Cpu.R65C02, "R65C02" },
        { Cpu.HuC6280, "HuC6280" },
        { Cpu.M6800, "m6800" },
        { Cpu.M6809, "m6809" },
        { Cpu.I8080, "i8080" },
        { Cpu.I86, "i86" },
        { Cpu.Gb80, "gb80" },
        { Cpu.M65, "m65" },
        { Cpu.Z80, "z80" },
    }.ToFrozenDictionary();
    
    private static readonly FrozenDictionary<string, Cpu> s_cpuNames 
        = new Dictionary<string, Cpu>
    {
        { "45GS02", Cpu.M65Ce02 },
        { "c64dtv2", Cpu.C64Dtv2 },
        { "6502", Cpu.M6502 },
        { "6502i", Cpu.M6502I },
        { "65816", Cpu.M65816 },
        { "65C02", Cpu.M65C02 },
        { "65CE02", Cpu.M65Ce02 },
        { "R65C02", Cpu.R65C02 },
        { "HuC6280", Cpu.HuC6280 },
        { "m6800", Cpu.M6800 },
        { "m6809", Cpu.M6809 },
        { "i8080", Cpu.I8080 },
        { "i8086", Cpu.I86 },
        { "i86", Cpu.I86 },
        { "gb80", Cpu.Gb80 },
        { "m65", Cpu.M65 },
        { "W65C02", Cpu.R65C02 },
        { "z80", Cpu.Z80 }
    }.ToFrozenDictionary();

    public static ByteOrder ByteOrder(this Cpu cpu)
    {
        return cpu switch
        {
            Cpu.M6800 or Cpu.M6809 => Compile.ByteOrder.BigEndian,
            _ => Compile.ByteOrder.LittleEndian
        };
    }
    
    public static Cpu? ByName(string? cpuName)
    {
        if (!string.IsNullOrEmpty(cpuName) &&
            s_cpuNames.TryGetValue(cpuName, out var cpu))
            return cpu;
        return null;
    }

    public static string ReverseLookup(Cpu cpu)
        => s_cpuNamesReverseLookup[cpu];
}