// Copyright (c) 2026 informedcitizenry
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System.Text;

namespace Sixty502DotNet.Shared.Arch.Formats;

public sealed class C64CartFormatProvider : IOutputFormatProvider
{
    private const string Signature = "C64 CARTRIDGE   ";
    private const string Chip = "CHIP";

    private static IEnumerable<byte> GetBigEndian(uint value)
    {
        var bytes = BitConverter.GetBytes(value);
        return BitConverter.IsLittleEndian ? bytes.Reverse() : bytes;
    }

    private static IEnumerable<byte> GetBigEndian(ushort value)
    {
        var bytes = BitConverter.GetBytes(value);
        if (BitConverter.IsLittleEndian)
            return bytes.Reverse();
        return bytes;
    }

    public IReadOnlyCollection<byte> GetFormat
    (
        string fileName, 
        int startAddress, 
        ReadOnlySpan<byte> objectBytes
    )
    {
        var name = fileName.ToUpper();
        if (name.EndsWith(".CRT") && name.Length > 4)
            name = name[0..^4];
        name = name[..(name.Length > 32 ? 32 : name.Length)];
        var padding = 32 - name.Length;

        var cartBytes = new List<byte>();
        cartBytes.AddRange(Encoding.ASCII.GetBytes(Signature));   // 0000-000F - Signature
        cartBytes.AddRange(GetBigEndian((uint)0x40));               // 0010-0013 - Header length (BE)
        cartBytes.AddRange(GetBigEndian(0x0100));                   // 0014-0015 - Version (BE)
        cartBytes.AddRange(new byte[2]);                            // 0016-0017 - Normal cart
        cartBytes.Add(0);                                                    // 0018      - EXROM inactive
        cartBytes.Add(1);                                                    // 0019      - GAME active             
        cartBytes.AddRange(new byte[6]);                            // 001A-001F - Reserved
        cartBytes.AddRange(Encoding.ASCII.GetBytes(name));          // 0020-003F - Cart name (padded)
        cartBytes.AddRange(new byte[padding]);
        cartBytes.AddRange(Encoding.ASCII.GetBytes(Chip));        // 0040-0043 - CHIP packet sig.   
        cartBytes.AddRange(GetBigEndian((uint)0x2010));             // 0044-0047 - Total packets (BE)
        cartBytes.AddRange(new byte[2]);                            // 0048-0049 - ROM Chip type
        cartBytes.AddRange(new byte[2]);                            // 004A-004B - Bank #
        cartBytes.AddRange(GetBigEndian((ushort)startAddress));     // 004C-004D - Load Addr. (BE)
        cartBytes.AddRange(GetBigEndian(0x2000));                   // 004E-004F - Image size (BE)
        cartBytes.AddRange(objectBytes);                                     // 0050-xxx code - Object data

        return cartBytes.AsReadOnly();
    }

    public FormatDescriptor Describe(ReadOnlySpan<byte> codeBytes)
    {
        return new FormatDescriptor(0x8000, codeBytes.Length - 0x50, codeBytes.ToArray());
    }
}