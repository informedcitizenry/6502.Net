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
    static class Core6502DotNet
    {
        static void Main()
        {
            try
            {
                var controller = new AssemblyController();
                AssemblerBase cpuAssembler;
                Assembler.FormatSelector = Select8BitFormat;

                if (Assembler.Options.CPU.Equals("z80"))
                    cpuAssembler = new Z80Asm();
                else
                    cpuAssembler = new Asm6502();
                
                controller.AddAssembler(cpuAssembler);
                controller.Assemble();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
            }
        }

        static IBinaryFormatProvider Select8BitFormat(string format)
        {
            if (format.Equals("srec", Assembler.StringComparison) || 
                format.Equals("srecmos", Assembler.StringComparison))
                return new SRecordFormatProvider();
            
            if (Assembler.Options.CPU.Equals("z80"))
                return new Z80FormatProvider();

            return format.ToLower() switch
            {
                "d64" => new D64FormatProvider(),
                _ => new M6502FormatProvider(),
            };
        }
    }
}
