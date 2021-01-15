//-----------------------------------------------------------------------------
// Copyright (c) 2017-2021 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Core6502DotNet.m65xx
{
    /// <summary>
    /// A class that encapsulates assembly output into a .T64 formatted tape file.
    /// </summary>
    public class T64FormatProvider : IBinaryFormatProvider
    {
        const string Header = "C64S tape image file\r\n";

        byte[] GetNameBytes(string file, int size)
        {
            if (file.Length > size)
                file = file.Substring(0, size);
            else if (file.Length < size)
                file = file.PadRight(size);
            return Encoding.ASCII.GetBytes(file);
        }

        public IEnumerable<byte> GetFormat(FormatInfo info)
        {
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);
            var endAddress = info.StartAddress + info.ObjectBytes.Count();
            byte startL = (byte)(info.StartAddress & 0xFF);
            byte startH = (byte)(info.StartAddress / 256);
            byte endL = (byte)(endAddress & 0xFF);
            byte endH = (byte)(endAddress / 256);

            var file = info.FileName.ToUpper();
            if (file.Length > 4 && file.EndsWith(".T64"))
                file = file[0..^4];
            writer.Write(Encoding.ASCII.GetBytes(Header));  // 00-1F
            writer.Write(new byte[32 - Header.Length]); 
            writer.Write((ushort)0x0101);                   // 20-21
            writer.Write((byte)0x1e);                       // 22
            writer.Write((byte)0x00);                       // 23     
            writer.Write((byte)0x01);                       // 24
            writer.Write((byte)0x00);                       // 25
            writer.Write(new byte[2]);                      // 26-27
            writer.Write(GetNameBytes(file, 24));           // 28-3F
            writer.Write((byte)0x01);                       // 40
            writer.Write((byte)0x82);                       // 41
            writer.Write(startL);                           // 42
            writer.Write(startH);                           // 43
            writer.Write(endL);                             // 44
            writer.Write(endH);                             // 45
            writer.Write(new byte[2]);                      // 46-47
            writer.Write(0x0400);                           // 48-4B
            writer.Write(new byte[4]);                      // 4C-4F
            writer.Write(GetNameBytes(file, 16));           // 50-5F
            writer.Write(new byte[0x3A0]);                  // 60-3FF
            writer.Write(info.ObjectBytes.ToArray());       // 400-...
            return ms.ToArray();
        }
    }
}
