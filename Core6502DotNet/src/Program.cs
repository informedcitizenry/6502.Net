//-----------------------------------------------------------------------------
// Copyright (c) 2017-2021 informedcitizenry <informedcitizenry@gmail.com>
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
    static class Program
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

        static CpuAssembler SetCpu(string cpu, AssemblyServices services)
        {
            if (services.Options.BranchAlways &&
                !string.IsNullOrEmpty(cpu) && 
                !cpu.StartsWith("6502"))
                services.Log.LogEntrySimple("Option '--enable-branch-always' ignored for non-6502 CPU.", false);
            return cpu switch
            {
                "m6800" => new M6809Asm(services),
                "m6809" => new M6809Asm(services),
                "i8080" => new Z80Asm(services),
                "z80"   => new Z80Asm(services),
                _       => new Asm6502(services)
            };
        }

        static IBinaryFormatProvider SelectFormatProvider(string cpu, string format)
        {
            return format switch
            {
                "flat"          => null,
                "srec"          => new SRecordFormatProvider(),
                "srecmos"       => new SRecordFormatProvider(),
                "hex"           => new HexFormatProvider(),
                "bytesource"    => new ByteSourceFormatProvider(),
                _               => cpu switch
                {
                    "i8080"     => null,
                    "z80"       => new Z80FormatProvider(),
                    "m68"       => new MotorolaFormatProvider(),
                    _           => format switch
                    {
                        "cart"  => new C64CartFormatProvider(),
                        "d64"   => new D64FormatProvider(),
                        "t64"   => new T64FormatProvider(),
                        _       => new M6502FormatProvider()
                    }
                }
            };
        }
    }
}
