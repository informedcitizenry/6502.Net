//-----------------------------------------------------------------------------
// Copyright (c) 2017-2022 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

namespace Sixty502DotNet
{
    /// <summary>
    /// Defines a method to select an instruction set based on a specified CPU
    /// name.
    /// </summary>
    public static class InstructionSetSelector
    {
        /// <summary>
        /// Return the <see cref="InstructionSet"/> of the specified CPU.
        /// </summary>
        /// <param name="services">The shared <see cref="AssemblyServices"/>
        /// object.</param>
        /// <param name="cpuName">The CPU's name.</param>
        /// <returns>The instruction set for the CPU.</returns>
        /// <exception cref="Error"></exception>
        public static InstructionSet Select(AssemblyServices services, string cpuName)
        {
            return cpuName switch
            {
                "45GS02"  or
                "6502"    or
                "6502i"   or
                "65816"   or
                "65C02"   or
                "65CE02"  or
                "c64dtv"  or
                "HuC6280" or
                "m65"     or
                "R65C02"  or
                "W65C02"         => new M65xx(services, cpuName),

                "m6800"   or
                "m6809"          => new M680x(services, cpuName),

                "z80"     or
                "i8080"          => new Z80(services, cpuName),

                _                => throw new Error($"CPU \"{cpuName}\" not valid.")
            };
        }
    }
}
