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
using System.Text.RegularExpressions;

namespace Sixty502DotNet.Shared.Arch.Formats;

public sealed partial class ByteSourceFormatProvider : IOutputFormatProvider
{
    private const int BytesPerLine = 8;

    public IReadOnlyCollection<byte> GetFormat(string fileName, int startAddress, ReadOnlySpan<byte> objectBytes)
    {
        var output = objectBytes.ToArray();
        if (output.Length == 0)
            return new List<byte>().AsReadOnly();

        var byteSourceBuilder = new StringBuilder($"\t\t\t* = ${startAddress:x4}\n");
        if (output.Length == 1)
        {
            byteSourceBuilder.AppendLine($"\n\t\t\t.byte ${output[0]:x2}");
        }
        else
        {
            for (var i = 0; i < output.Length; i++)
            {
                var byt = output[i];
                if (i % BytesPerLine == 0 && i < output.Length - 1)
                    byteSourceBuilder.Append($"\n\t\t\t.byte ${byt:x2}");
                else
                    byteSourceBuilder.Append($", ${byt:x2}");

            }
        }
        byteSourceBuilder.AppendLine();
        return Encoding.UTF8.GetBytes(byteSourceBuilder.ToString());
    }

    public FormatDescriptor Describe(ReadOnlySpan<byte> codeBytes)
    {
        var actualCodeBytes = new List<byte>(codeBytes.Length / 3);
        var listingLines = Encoding.ASCII.GetString(codeBytes)
            .Split(['\n', '\r'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (listingLines.Length == 0)
        {
            return new FormatDescriptor();
        }
        var startLine = listingLines[0];
        int startAddress;
        try
        {
            var match = PcAssignRegex().Match(startLine);
            var groups = match.Groups;
            startAddress = Convert.ToInt32(groups[1].Value, 16);
        }
        catch
        {
            return new FormatDescriptor();
        }

        var l = 1;
        for (; l < listingLines.Length; l++)
        {
            var line = listingLines[l];
            if (!line.StartsWith(".byte ", StringComparison.OrdinalIgnoreCase))
            {
                break;
            }
            var hexes = line[6..].Split(',', StringSplitOptions.TrimEntries);
            for (var i = 0; i < hexes.Length; )
            {
                var hex = hexes[i];
                if (hex is not ['$', _, _])
                {
                    break;
                }
                var hexByteString = hex[1..];
                try
                {
                    var hexByte = (byte)Convert.ToInt32(hexByteString, 16);
                    actualCodeBytes.Add(hexByte);
                }
                catch
                {
                    break;
                }
            }
        }
        return l < listingLines.Length 
            ? new FormatDescriptor() 
            : new FormatDescriptor(startAddress, actualCodeBytes.Count, actualCodeBytes.ToArray());
    }

    [GeneratedRegex(@"^\*\s*=\s*\$([0-9A-Fa-f]+)$")]
    private static partial Regex PcAssignRegex();
}