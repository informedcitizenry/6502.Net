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

namespace Sixty502DotNet.Shared.Arch.Formats;

public class MzFormatProvider : IOutputFormatProvider
{
    public IReadOnlyCollection<byte> GetFormat(string fileName, int startAddress, ReadOnlySpan<byte> objectBytes)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        var blocks = 1 + objectBytes.Length / 512;
        var lastBlockBytes = objectBytes.Length % 512;
        
        // 00h-01h
        writer.Write("MZ");
        // 02h-03h
        writer.Write((ushort)(lastBlockBytes & 0xffff));
        // 04h-05h
        writer.Write((ushort)(blocks & 0xffff));
        // 06h-07h
        writer.Write((ushort)0x0001); // relocation entries
        // 08h-09h
        writer.Write((ushort)0x0020); // header paragraphs
        // 0ah-0bh
        writer.Write((ushort)0x0000); // min paragraphs
        // 0ch-0dh
        writer.Write((ushort)0xffff); // max paragraphs
        // 0eh-0fh
        writer.Write((ushort)0x0000); // ss
        // 10h-11h
        writer.Write((ushort)0x0000); // sp
        // 12h-13h
        writer.Write((ushort)0x0000); // checksum
        // 14h-15h
        writer.Write((ushort)0x0000); // ip
        // 16h-17h
        writer.Write((ushort)0x0000); // cs
        // 18h-19h
        writer.Write((ushort)0x003e); // first relocation addr
        // 1ah-1bh
        writer.Write((ushort)0x0000); // overlay 
        // 1ch-3dh
        writer.Write(new byte[0x3e - 0x1c]); // simple header, no extra stuff
        // 3eh-3fh
        writer.Write((ushort)0x0100); // relocation pointer
        // 40h-1ffh
        writer.Write(new byte[512 - 0x40]); // for DOS?
        writer.Write(objectBytes);
        writer.Write((ushort)0x0000); // ??
        return ms.ToArray();
    }

    public FormatDescriptor Describe(ReadOnlySpan<byte> codeBytes)
    {
        if (codeBytes.Length < 513) return new FormatDescriptor();
        var relocationAddress = codeBytes[0x18];
        var startAddress = codeBytes[relocationAddress] + codeBytes[relocationAddress + 1] * 256;
        var length = codeBytes.Length - 512;
        return new  FormatDescriptor(startAddress, length, codeBytes[512..].ToArray());
    }
}