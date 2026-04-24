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

public sealed class T64FormatProvider : IOutputFormatProvider
{
    public IReadOnlyCollection<byte> GetFormat
    (
        string fileName, 
        int startAddress, 
        ReadOnlySpan<byte> objectBytes
    )
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        var endAddress = startAddress + objectBytes.Length;
        byte startL = (byte)(startAddress & 0xFF);
        byte startH = (byte)(startAddress / 256);
        byte endL = (byte)(endAddress & 0xFF);
        byte endH = (byte)(endAddress / 256);

        var file = fileName.ToUpper();
        if (file.Length > 4 && file.EndsWith(".T64"))
            file = file[..^4];
        writer.Write(Encoding.ASCII.GetBytes(Header));  // 00-1F
        writer.Write(new byte[32 - Header.Length]);
        writer.Write((ushort)0x0101);                           // 20-21
        writer.Write((byte)0x1e);                               // 22
        writer.Write((byte)0x00);                               // 23     
        writer.Write((byte)0x01);                               // 24
        writer.Write((byte)0x00);                               // 25
        writer.Write(new byte[2]);                        // 26-27
        writer.Write(GetNameBytes(file, 24));         // 28-3F
        writer.Write((byte)0x01);                               // 40
        writer.Write((byte)0x82);                               // 41
        writer.Write(startL);                                   // 42
        writer.Write(startH);                                   // 43
        writer.Write(endL);                                     // 44
        writer.Write(endH);                                     // 45
        writer.Write(new byte[2]);                        // 46-47
        writer.Write(0x0400);                                   // 48-4B
        writer.Write(new byte[4]);                        // 4C-4F
        writer.Write(GetNameBytes(file, 16));         // 50-5F
        writer.Write(new byte[0x3A0]);                    // 60-3FF
        writer.Write(objectBytes.ToArray());              // 400-...
        return ms.ToArray();
    }

    public FormatDescriptor Describe(ReadOnlySpan<byte> codeBytes)
    {
        return new  FormatDescriptor(0, 0, []);
    }

    private const string Header = "C64S tape image file\r\n";

    private static byte[] GetNameBytes(string file, int size)
    {
        if (file.Length > size)
            file = file[..size];
        else if (file.Length < size)
            file = file.PadRight(size);
        return Encoding.ASCII.GetBytes(file);
    }
}