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

using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;

namespace Sixty502DotNet.Shared.Arch.Formats;

public class Hex86FormatProvider : IOutputFormatProvider
{
    public IReadOnlyCollection<byte> GetFormat
    (
        string fileName, 
        int startAddress, 
        ReadOnlySpan<byte> objectBytes
    )
    {
        var sb = new StringBuilder(objectBytes.Length * 2);
        var pc = startAddress;
        var end = startAddress + objectBytes.Length;
        var bytesLeft = objectBytes.Length;
        while (pc < end)
        {
            var lineBytes = bytesLeft < 0x10 ? bytesLeft % objectBytes.Length : 0x10;
            var checkSum = lineBytes + pc / 256 + pc % 256;
            sb.Append($":{lineBytes:X2}{pc:X4}00");
            for (var i = 0; i < lineBytes; i++)
            {
                var offset =  pc - startAddress + i;
                checkSum += objectBytes[offset];
                sb.Append($"{objectBytes[offset]:X2}");
            }
            checkSum = ~(checkSum & 255) & 255;
            sb.AppendLine($"{checkSum:X2}");
            pc += lineBytes;
            bytesLeft -= lineBytes;
        }
        sb.Append(":00000001FF");
        return new ReadOnlyCollection<byte>(Encoding.ASCII.GetBytes(sb.ToString()));
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
        var i = 0;
        var startAddress = -1;
        for (; i < listingLines.Length; i++)
        {
            var line = listingLines[i].Trim();
            if (!TextRecord.TryParse(line, ':', out var record) || 
                record == null ||
                record.Type > 1 ||
                (record.Type != 0 && record.DataSize > 0))
            {
                return new FormatDescriptor();
            }
            var checksum = record.DataSize + record.Address / 256 + record.Address % 256;
            var lineData = record.Data;
            if (startAddress < 0) startAddress = record.Address;
            if (string.IsNullOrEmpty(lineData))
            {
                break;
            }
            for (var b = 0; b < lineData.Length - 2; b += 2)
            {
                if (!int.TryParse
                    (
                        lineData.AsSpan(b, 2), 
                        NumberStyles.HexNumber, 
                        CultureInfo.InvariantCulture,
                        out var data
                    ))
                {
                    return new FormatDescriptor();
                }
                checksum += data;
                actualCodeBytes.Add((byte)data);
            }
            checksum = ~(checksum & 255) & 255;
            if (checksum != record.Checksum)
            {
                return new FormatDescriptor();
            }
        }
        return new FormatDescriptor(startAddress, actualCodeBytes.Count, actualCodeBytes.ToArray());
    }
}