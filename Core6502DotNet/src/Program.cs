//-----------------------------------------------------------------------------
// Copyright (c) 2017-2020 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;

namespace Core6502DotNet
{
    internal class Core6502DotNet
    {
        static void Main(string[] args)
        {
            Assembler.Initialize(args);

            AssemblerBase cpuAssembler;
            if (Assembler.Options.CPU.Equals("z80"))
            {
                Assembler.BinaryFormatProvider = new z80.Z80FormatProvider();
                cpuAssembler = new z80.Z80Asm();
            }
            else
            {
                if (Assembler.Options.Architecture.ToLower().Equals("d64"))
                    Assembler.BinaryFormatProvider = new m6502.D64FormatProvider();
                else
                    Assembler.BinaryFormatProvider = new m6502.M6502FormatProvider();
                cpuAssembler = new m6502.Asm6502();
            }
            var controller = new AssemblyController(cpuAssembler);
            try
            {
                controller.Assemble();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
            }
        }
    }
}
