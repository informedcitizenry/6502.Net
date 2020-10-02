//-----------------------------------------------------------------------------
// Copyright (c) 2017-2020 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using Core6502DotNet.m65xx;
using Core6502DotNet.m680x;
using Core6502DotNet.z80;
using System;

namespace Core6502DotNet
{
    static class Core6502DotNet
    {
        static void Main(string[] args)
        {
            try
            {
                var controller = new AssemblyController(args, SetCpu, SelectFormatProvider);
                controller.Assemble();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
            }
        }

        static AssemblerBase SetCpu(string cpu, AssemblyServices services)
        {
            return cpu switch
            {
                "m6800" => new M6809Asm(services),
                "m6809" => new M6809Asm(services),
                "z80"   => new Z80Asm(services),
                _       => new Asm6502(services)
            };
        }

        static IBinaryFormatProvider SelectFormatProvider(string cpu, string format)
        {
            if (format.Equals("srec") || format.Equals("srecmos"))
                return new SRecordFormatProvider();

            if (format.Equals("hex"))
                return new HexFormatProvider();

            if (format.Equals("bytesource"))
                return new ByteSourceFormatProvider();
            
            if (cpu.Equals("z80"))
                return new Z80FormatProvider();

            if (cpu.StartsWith('m'))
                return new MotorolaFormatProvider();

            return format switch
            {
                "cart" => new C64CartFormatProvider(),
                "d64"  => new D64FormatProvider(),
                "t64"  => new T64FormatProvider(),
                _      => new M6502FormatProvider(),
            };
        }
    }
}
