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
using Sixty502DotNet.Shared.Error;

namespace Sixty502DotNet.Shared.Arch.Formats;

public sealed class Z80FormatProvider(OutputFormat format) : IOutputFormatProvider
{
    private static byte[] ConvertToBytes(int value)
    {
        var lsb = (byte)(value & 0xFF);
        var msb = (byte)(value / 256);
        return [lsb, msb];
    }

    public IReadOnlyCollection<byte> GetFormat
    (
        string fileName, 
        int startAddress, 
        ReadOnlySpan<byte> objectBytes
    )
    {
        var progstart = (ushort)startAddress;
        var progend = (ushort)(startAddress + objectBytes.Length);
        var size = objectBytes.Length;
        var name = fileName.ToUpper();

        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        switch (format)
        {
            case OutputFormat.Zx:
            {
                name = name.Length > 10 ? name[..10] : name.PadLeft(10);

                var buffer = new List<byte>
                {
                    // header
                    0x00,
                    // file type - code
                    0x03
                };
                // file name
                buffer.AddRange(Encoding.ASCII.GetBytes(name));
                // file size =
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
                break;
            }
            case OutputFormat.AmsDos:
            case OutputFormat.AmsTap:
            {
                var buffer = new List<byte>();
                if (format == OutputFormat.AmsDos)
                {
                    name = name.Length > 8 ? name[..8] : name.PadRight(8);

                    name = $"{name}$$$";

                    // user number 0
                    buffer.Add(0);

                }
                else
                {
                    name = name.Length > 16 
                        ? name[..16] 
                        : name.PadRight(16, '\0');
                }

                // amsdos 0 - 11 '0' + name + ext, amstap 0 - 15
                buffer.AddRange(Encoding.ASCII.GetBytes(name));

                if (format == OutputFormat.AmsDos)
                {
                    // 12 - 15 all zeros
                    buffer.AddRange(new byte[3]);
                    
                    // 16 block 
                    buffer.Add(0);

                    // 17 last block 
                    buffer.Add(0);
                }
                else
                {
                    // 16 block number
                    buffer.Add(1);
                    // 17 last block
                    buffer.Add(2);
                }
                
                // 18 binary type
                buffer.Add(2);

                // 19 size
                buffer.AddRange(ConvertToBytes(size));

                // 21 start address
                buffer.AddRange(ConvertToBytes(progstart));

                // 23 first block
                buffer.Add(0xff);

                // 24 logical size
                buffer.AddRange(ConvertToBytes(size));

                // 26 logical start
                buffer.AddRange(ConvertToBytes(progstart));
                
                if (format == OutputFormat.AmsDos)
                {
                    // amsdos 28 - 63 unallocated
                    buffer.AddRange(new byte[36]);

                    // 64 - 67 file size (24-bit number)
                    buffer.AddRange(ConvertToBytes(size));
                    buffer.Add(0);

                    byte checksum = 0;
                    buffer.ForEach(b =>
                    {
                        checksum = (byte)(checksum + b);
                    });
                    // 68 
                    buffer.Add(checksum);

                    // bytes 69 - 127 undefined
                    buffer.AddRange(new byte[60]);
                }
                writer.Write(buffer.ToArray());
                break;
            }
            case OutputFormat.Msx:
                // 0 ID byte
                writer.Write(0xfe);

                // 1 - 2 start address
                writer.Write(ConvertToBytes(progstart));

                // 3 - 4 end address
                writer.Write(ConvertToBytes(progend));

                // 5 - 6 start address
                writer.Write(ConvertToBytes(progstart));
                break;
            case OutputFormat.None:
                // do nothing
                break;
            default:
                throw new OutputFormatException(format);
        }
        writer.Write(objectBytes.ToArray());
        return ms.ToArray();
    }

    public FormatDescriptor Describe(ReadOnlySpan<byte> codeBytes)
    {
        int offset;
        int startAddress;
        int size;
        switch (format)
        {
            case OutputFormat.Zx:
                if (codeBytes.Length < 27) return new FormatDescriptor();
                offset = 26;
                startAddress = codeBytes[22] + codeBytes[23] * 256;
                size = codeBytes[20] + codeBytes[21] * 256;
                break;
            case OutputFormat.AmsDos:
            case OutputFormat.AmsTap:
                offset = format == OutputFormat.AmsDos ? 128 : 28;
                if (codeBytes.Length <= offset) return new FormatDescriptor();
                startAddress = codeBytes[26] + codeBytes[27] * 256;
                size = codeBytes[19] + codeBytes[20] * 256;
                break;
            case OutputFormat.Msx:
                
                if (codeBytes.Length < 8) return new FormatDescriptor();
                offset = 7;
                startAddress = codeBytes[1] + codeBytes[2] * 256;
                size =  (codeBytes[3] + codeBytes[4] * 256) - startAddress;
                break;
            default:
                offset = startAddress = size = 0;
                break;
                
        }
        return new  FormatDescriptor(startAddress, size, codeBytes[offset..].ToArray());
    }
}