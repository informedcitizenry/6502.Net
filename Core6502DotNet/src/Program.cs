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

        static IBinaryFormatProvider SelectFormatProvider(string format, AssemblyServices services)
        {
            if (format.Equals("srec", services.StringComparison) || 
                format.Equals("srecmos", services.StringComparison))
                return new SRecordFormatProvider(services);

            if (format.Equals("hex", services.StringComparison))
                return new HexFormatProvider(services);

            if (format.Equals("bytesource", services.StringComparison))
                return new ByteSourceFormatProvider(services);
            
            if (services.CPU.Equals("z80"))
                return new Z80FormatProvider(services);

            if (services.CPU.StartsWith('m'))
                return new MotorolaFormatProvider(services);

            return format.ToLower() switch
            {
                "cart" => new C64CartFormatProvider(services),
                "d64"  => new D64FormatProvider(services),
                "t64"  => new T64FormatProvider(services),
                _      => new M6502FormatProvider(services),
            };
        }
    }
}
