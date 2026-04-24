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

using System.Globalization;
using System.Text;

namespace Sixty502DotNet.Shared.Arch.Formats;

public sealed class HexFormatProvider : IOutputFormatProvider
{
    public IReadOnlyCollection<byte> GetFormat
    (
        string fileName, 
        int startAddress, 
        ReadOnlySpan<byte> objectBytes
    )
    {
        var hex = Convert.ToHexString(objectBytes.ToArray());
        return Encoding.ASCII.GetBytes(hex);
    }

    public FormatDescriptor Describe(ReadOnlySpan<byte> codeBytes)
    {
        var hexString = Encoding.ASCII.GetString(codeBytes);
        if (hexString.Length % 2 != 0)
        {
            return new FormatDescriptor();
        }
        var hexBytes = new List<byte>();
        for (var i = 0; i < hexString.Length; i += 2)
        {
            if (!int.TryParse(hexString.Substring(i, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture,
                    out var data))
            {
                return new FormatDescriptor();
            }
            hexBytes.Add((byte)data);
        }
        return new FormatDescriptor(0, hexBytes.Count, hexBytes.ToArray());
    }
}