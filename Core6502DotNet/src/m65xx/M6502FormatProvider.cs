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

namespace Core6502DotNet.m65xx
{
    /// <summary>
    /// A class that handles disk/tape file formats for several popular 65xx-based
    /// architectures.
    /// </summary>
    public class M6502FormatProvider : Core6502Base, IBinaryFormatProvider
    {
        /// <summary>
        /// Creates a new instance of the 65xx-based format provider.
        /// </summary>
        /// <param name="services">The shared <see cref="AssemblyServices"/> object.</param>
        public M6502FormatProvider(AssemblyServices services)
            :base(services)
        {
        }

        public IEnumerable<byte> GetFormat(IEnumerable<byte> objectBytes)
        {
            var fmt = Services.OutputFormat;
            byte startL = (byte)(Services.Output.ProgramStart & 0xFF);
            byte startH = (byte)(Services.Output.ProgramStart / 256);
            byte endL = (byte)(Services.Output.ProgramEnd & 0xFF);
            byte endH = (byte)(Services.Output.ProgramEnd / 256);
            byte sizeL = (byte)(objectBytes.Count() & 0xFF);
            byte sizeH = (byte)(objectBytes.Count() / 256);

            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);
            if (string.IsNullOrEmpty(fmt) || fmt.Equals("cbm"))
            {
                writer.Write(startL);
                writer.Write(startH);
            }
            else if (fmt.Equals("atari-xex"))
            {
                writer.Write(new byte[] { 0xff, 0xff }); // FF FF
                writer.Write(startL); writer.Write(startH);
                writer.Write(endL); writer.Write(endH);
            }
            else if (fmt.Equals("apple2"))
            {
                writer.Write(startL); writer.Write(startH);
                writer.Write(sizeL); writer.Write(sizeH);
            }
            else if (!fmt.Equals("flat"))
            {
                throw new ArgumentException($"Format \"{fmt}\" not supported with targeted CPU.");
            }
            writer.Write(objectBytes.ToArray());
            return ms.ToArray();
        }
    }
}