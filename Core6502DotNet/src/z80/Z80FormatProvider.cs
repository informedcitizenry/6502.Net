//-----------------------------------------------------------------------------
// Copyright (c) 2017-2021 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;

namespace Core6502DotNet.z80
{
    /// <summary>
    /// A class to handle disk/tape formats for several popular Z80-based systems.
    /// </summary>
    public class Z80FormatProvider : IBinaryFormatProvider
    {
        byte[] ConvertToBytes(int value)
        {
            var lsb = (byte)(value & 0xFF);
            var msb = (byte)(value / 256);
            return new byte[] { lsb, msb };
        }
 
        public IEnumerable<byte> GetFormat(FormatInfo info)
        {
            var fmt = info.FormatName;
            var progstart = (ushort)info.StartAddress;
            var progend = (ushort)(info.StartAddress + info.ObjectBytes.Count());
            var size = info.ObjectBytes.Count();
            var name = info.FileName.ToUpper();

            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);
            if (fmt.Equals("zx"))
            {
                if (name.Length > 10)
                    name = name.Substring(0, 10);
                else
                    name = name.PadLeft(10);

                var buffer = new List<byte>
                    {
                        // header
                        0x00,
                        // file type - code
                        0x03
                    };
                // file name
                buffer.AddRange(Encoding.ASCII.GetBytes(name));
                // file size
                buffer.AddRange(ConvertToBytes(size));
                // program start
                buffer.AddRange(ConvertToBytes(progstart));
                // unused
                buffer.AddRange(ConvertToBytes(0x8000));

                // calculate checksum
                byte checksum = 0x00;
                buffer.ForEach(b => { checksum ^= b; });

                // add checksum
                buffer.Add(checksum);

                // write the buffer
                writer.Write(buffer.ToArray());
            }
            else if (fmt.Equals("amsdos") || fmt.Equals("amstap"))
            {
                var buffer = new List<byte>();
                if (fmt.Equals("amsdos"))
                {
                    if (name.Length > 8)
                        name = name.Substring(0, 8);
                    else
                        name = name.PadRight(8);

                    name = string.Format("{0}$$$", name);

                    // user number 0
                    buffer.Add(0);

                }
                else
                {
                    if (name.Length > 16)
                        name = name.Substring(0, 16);
                    else
                        name = name.PadRight(16, '\0');
                }

                // name
                buffer.AddRange(Encoding.ASCII.GetBytes(name));

                if (fmt.Equals("amsdos"))
                {
                    // block
                    buffer.Add(0);

                    // last block
                    buffer.Add(0);
                }
                else
                {
                    buffer.Add(1);
                    buffer.Add(2);
                }

                // binary type
                buffer.Add(2);

                // size
                buffer.AddRange(ConvertToBytes(size));

                // start address
                buffer.AddRange(ConvertToBytes(progstart));

                // first block
                buffer.Add(0xff);

                // logical size
                buffer.AddRange(ConvertToBytes(size));

                // logical start
                buffer.AddRange(ConvertToBytes(progstart));

                // unallocated
                buffer.AddRange(new byte[36]);

                if (fmt.Equals("amsdos"))
                {
                    // file size (24-bit number)
                    buffer.AddRange(ConvertToBytes(size));
                    buffer.Add(0);

                    byte checksum = 0;
                    buffer.ForEach(b =>
                    {
                        checksum = (byte)(checksum + b);
                    });
                    buffer.Add(checksum);

                    // bytes 69 - 127 undefined
                    buffer.AddRange(new byte[60]);
                }
                writer.Write(buffer.ToArray());
            }
            else if (fmt.Equals("msx"))
            {
                // ID byte
                writer.Write(0xfe);

                // start address
                writer.Write(ConvertToBytes(progstart));

                // end address
                writer.Write(ConvertToBytes(progend));

                // start address
                writer.Write(ConvertToBytes(progstart));
            }
            else if (string.IsNullOrEmpty(fmt))
            {
                // do nothing
            }
            else
            {
                throw new Exception($"Format \"{fmt}\" not supported with targetted CPU.");
            }
            writer.Write(info.ObjectBytes.ToArray());
            return ms.ToArray();
        }
    }
}