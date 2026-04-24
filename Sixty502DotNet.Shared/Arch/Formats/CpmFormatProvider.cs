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

public class CpmFormatProvider : IOutputFormatProvider
{
    public IReadOnlyCollection<byte> GetFormat(string fileName, int startAddress, ReadOnlySpan<byte> objectBytes)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        var paragraphs = (objectBytes.Length + 15) / 16 + 0x10;
        var rem = (paragraphs - 1) * 16 - objectBytes.Length;

        var maxSize = objectBytes.Length < 0x1000 ? 0x1000 : 0xffff;
        // "CODE" group
        writer.Write((byte)0x01); 
        // group length
        writer.Write((byte)(paragraphs & 0xff));
        writer.Write((byte)(paragraphs >> 8 & 0xff));
        // base
        writer.Write((byte)(startAddress & 0xff));
        writer.Write((byte)(startAddress >> 8 & 0xff));
        // min
        writer.Write((byte)(paragraphs & 0xff));
        writer.Write((byte)(paragraphs >> 8 & 0xff));
        // max
        writer.Write((byte)(maxSize & 0xff));
        writer.Write((byte)(maxSize >> 8 & 0xff));
        
        writer.Write(new byte[rem]);
        writer.Write(objectBytes);
        return ms.ToArray();
    }

    public FormatDescriptor Describe(ReadOnlySpan<byte> codeBytes)
    {
        if (codeBytes.Length < 10) return new  FormatDescriptor(0, 0, []);
        var paragraphs = codeBytes[1] + codeBytes[2] * 256;
        var startAddress = codeBytes[3] + codeBytes[4] * 256;
        
        var objectLength = (paragraphs - 0x10) * 16 - 15;
        var rem = (paragraphs - 1) * 16 - objectLength;
        var offset = rem + 9;
        return new  FormatDescriptor(startAddress, codeBytes.Length - offset, codeBytes[offset..].ToArray());
    }
}