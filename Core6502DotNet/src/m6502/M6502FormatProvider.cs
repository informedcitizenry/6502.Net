//-----------------------------------------------------------------------------
// Copyright (c) 2017-2020 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace Core6502DotNet.m6502
{
    /// <summary>
    /// A class that handles disk/tape file formats for several popular 65xx-based
    /// architectures.
    /// </summary>
    public class M6502FormatProvider : IBinaryFormatProvider
    {
        public IEnumerable<byte> GetFormat()
        {
            var fmt = Assembler.Options.Format;
            var progstart = (ushort)Assembler.Output.ProgramStart;
            var progend = (ushort)Assembler.Output.ProgramCounter;
            var progsize = Assembler.Output.GetCompilation().Count;

            using (var ms = new MemoryStream())
            {
                using (var writer = new BinaryWriter(ms))
                {
                    if (string.IsNullOrEmpty(fmt) || fmt.Equals("cbm"))
                    {
                        writer.Write(progstart);
                    }
                    else if (fmt.Equals("atari-xex"))
                    {
                        writer.Write(new byte[] { 0xff, 0xff }); // FF FF
                        writer.Write(progstart);
                        writer.Write(progend);
                    }
                    else if (fmt.Equals("apple2"))
                    {
                        writer.Write(progstart);
                        writer.Write(progsize);
                    }
                    else if (!fmt.Equals("flat"))
                    {
                        throw new ArgumentException($"Format \"{fmt}\" not supported with targeted CPU.");
                    }
                    writer.Write(Assembler.Output.GetCompilation().ToArray());
                    return ms.ToArray();
                }
            }
        }
    }
}