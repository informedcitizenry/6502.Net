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

using Sixty502DotNet.Shared.Error;

namespace Sixty502DotNet.Shared.Arch.Formats;

// ReSharper disable once InconsistentNaming
public sealed class M65xxFormatProvider(OutputFormat format) : IOutputFormatProvider
{
    public IReadOnlyCollection<byte> GetFormat
    (
        string fileName, 
        int startAddress, 
        ReadOnlySpan<byte> objectBytes
    )
    {
        var size = objectBytes.Length;
        var end = startAddress + size;
        var startL = (byte)(startAddress & 0xFF);
        var startH = (byte)(startAddress / 256);
        var endL = (byte)(end & 0xFF);
        var endH = (byte)(end / 256);
        var sizeL = (byte)(size & 0xFF);
        var sizeH = (byte)(size / 256);
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        switch (format)
        {
            case OutputFormat.Flat:
                break;
            case OutputFormat.None:
            case OutputFormat.Cbm:
                writer.Write(startL);
                writer.Write(startH);
                break;
            case OutputFormat.Apple2:
                writer.Write(startL); writer.Write(startH);
                writer.Write(sizeL); writer.Write(sizeH);
                break;
            case OutputFormat.Xex:
                writer.Write(new byte[] { 0xff, 0xff }); // FF FF
                writer.Write(startL); writer.Write(startH);
                writer.Write(endL); writer.Write(endH);
                break;
            default:
                throw new OutputFormatException();
        }
        writer.Write(objectBytes.ToArray());
        return ms.ToArray();
    }

    public FormatDescriptor Describe(ReadOnlySpan<byte> codeBytes)
    {
        int offset;
        int startAddress;
        int size;
        var image = codeBytes.ToArray();
        switch (format)
        {
            case OutputFormat.Flat:
                offset = 0;
                startAddress = 0;
                size = codeBytes.Length;
                break;
            case OutputFormat.None:
            case OutputFormat.Cbm:
                if (codeBytes.Length < 3) return new  FormatDescriptor(0, 0, []);
                offset = 2;
                startAddress = codeBytes[0] + codeBytes[1] * 256;
                size = codeBytes.Length - 2;
                break;
            case  OutputFormat.Apple2:
                if (codeBytes.Length < 5) return new  FormatDescriptor(0, 0, []);
                offset = 4;
                startAddress = codeBytes[0] * 256 + codeBytes[1] * 256;
                size = codeBytes[2] + codeBytes[3] * 256;
                break;
            case OutputFormat.Xex:
                if (codeBytes.Length < 7) return new  FormatDescriptor(0, 0, []);
                offset = 6;
                startAddress = codeBytes[1] * 256 + codeBytes[2] * 256;
                size = (codeBytes[3] + codeBytes[4] * 256) - startAddress;
                break;
            default:
                return new FormatDescriptor();
                
        }
        return new FormatDescriptor(startAddress, size, image.Skip(offset).Take(size).ToArray());
    }
}