//-----------------------------------------------------------------------------
// Copyright (c) 2017-2020 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using Core6502DotNet.m6502;
using Core6502DotNet.z80;
using System;

namespace Core6502DotNet
{
    internal class Core6502DotNet
    {
        static void Main(string[] args)
        {
            Assembler.Initialize(args);

            AssemblerBase cpuAssembler;
            if (Assembler.Options.CPU.ToLower().Equals("z80"))
            {
                Assembler.BinaryFormatProvider = new Z80FormatProvider();
                cpuAssembler = new Z80Asm();
            }
            else
            {
                if (Assembler.Options.Architecture.ToLower().Equals("d64"))
                    Assembler.BinaryFormatProvider = new D64FormatProvider();
                else
                    Assembler.BinaryFormatProvider = new M6502FormatProvider();
                cpuAssembler = new Asm6502();
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
