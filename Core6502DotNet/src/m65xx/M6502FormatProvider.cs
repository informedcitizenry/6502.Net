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
    public class M6502FormatProvider : IBinaryFormatProvider
    {
        public IEnumerable<byte> GetFormat(FormatInfo info)
        {
            var fmt = info.FormatName;
            var size = info.ObjectBytes.Count();
            var end = info.StartAddress + size;
            byte startL = (byte)(info.StartAddress & 0xFF);
            var startH = (byte)(info.StartAddress / 256);
            byte endL = (byte)(end & 0xFF);
            byte endH = (byte)(end / 256);
            byte sizeL = (byte)(size & 0xFF);
            byte sizeH = (byte)(size / 256);

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
            writer.Write(info.ObjectBytes.ToArray());
            return ms.ToArray();
        }
    }
}