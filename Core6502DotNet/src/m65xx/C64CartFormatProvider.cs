//-----------------------------------------------------------------------------
// Copyright (c) 2017-2021 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Core6502DotNet.m65xx
{
    public class C64CartFormatProvider : IBinaryFormatProvider
    {
        const string Signature = "C64 CARTRIDGE   ";
        const string CHIP      = "CHIP";

        static IEnumerable<byte> GetBigEndian(uint value)
        {
            var bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
                return bytes.Reverse();
            return bytes;
        }

        static IEnumerable<byte> GetBigEndian(ushort value)
        {
            var bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
                return bytes.Reverse();
            return bytes;
        }

        public IEnumerable<byte> GetFormat(FormatInfo info)
        {
            var name = info.FileName.ToUpper();
            if (name.EndsWith(".CRT") && name.Length > 4)
                name = name[0..^4];
            name = name.Substring(0, name.Length > 32 ? 32 : name.Length);
            var padding = 32 - name.Length;

            var cartBytes = new List<byte>();
            cartBytes.AddRange(Encoding.ASCII.GetBytes(Signature));   // 0000-000F - Signature
            cartBytes.AddRange(GetBigEndian((uint)0x40));             // 0010-0013 - Header length (BE)
            cartBytes.AddRange(GetBigEndian(0x0100));                 // 0014-0015 - Version (BE)
            cartBytes.AddRange(new byte[2]);                          // 0016-0017 - Normal cart
            cartBytes.Add(0);                                         // 0018      - EXROM inactive
            cartBytes.Add(1);                                         // 0019      - GAME active             
            cartBytes.AddRange(new byte[6]);                          // 001A-001F - Reserved
            cartBytes.AddRange(Encoding.ASCII.GetBytes(name));        // 0020-003F - Cart name (padded)
            cartBytes.AddRange(new byte[padding]);                                  
            cartBytes.AddRange(Encoding.ASCII.GetBytes(CHIP));        // 0040-0043 - CHIP packet sig.   
            cartBytes.AddRange(GetBigEndian((uint)0x2010));           // 0044-0047 - Total packets (BE)
            cartBytes.AddRange(new byte[2]);                          // 0048-0049 - ROM Chip type
            cartBytes.AddRange(new byte[2]);                          // 004A-004B - Bank #
            cartBytes.AddRange(GetBigEndian((ushort)info.StartAddress));// 004C-004D - Load Addr. (BE)
            cartBytes.AddRange(GetBigEndian(0x2000));                 // 004E-004F - Image size (BE)
            cartBytes.AddRange(info.ObjectBytes);                     // 0050-xxx code - Object data

            return cartBytes;
        }
    }
}
