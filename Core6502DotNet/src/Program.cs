//-----------------------------------------------------------------------------
// Copyright (c) 2017-2020 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using Core6502DotNet.m6502;
using Core6502DotNet.z80;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Core6502DotNet
{
    class Core6502DotNet
    {
        static void Main(string[] args)
        {
            try
            {
                var controller = new AssemblyController(args);
                AssemblerBase cpuAssembler;
                if (Assembler.Options.CPU.Equals("z80"))
                {
                    Assembler.BinaryFormatProvider = new Z80FormatProvider();
                    cpuAssembler = new Z80Asm();
                }
                else
                {
                    if (Assembler.Options.Format.Equals("d64"))
                        Assembler.BinaryFormatProvider = new D64FormatProvider();
                    else
                        Assembler.BinaryFormatProvider = new M6502FormatProvider();
                    cpuAssembler = new Asm6502();
                }
                controller.AddAssembler(cpuAssembler);
                controller.Assemble();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
            }
        }
    }
}
