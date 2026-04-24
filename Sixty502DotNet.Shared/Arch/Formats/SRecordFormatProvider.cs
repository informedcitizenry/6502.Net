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

public sealed class SRecordFormatProvider(OutputFormat format) : IOutputFormatProvider
{
    public IReadOnlyCollection<byte> GetFormat
    (
        string fileName, 
        int startAddress, 
        ReadOnlySpan<byte> objectBytes
    )
    {
        var sRecBuilder = new StringBuilder();
        var end = startAddress + objectBytes.Length;
        var pc = startAddress;
        var fileBytes = objectBytes;
        var bytesLeft = objectBytes.Length;
        var lineCount = 0;
        string recHeader;
        int recordBytes;
        if (format == OutputFormat.SRecord)
        {
            recHeader = "S1";
            recordBytes = RecordSize;
            sRecBuilder.Append("S0030000FC\n"); // header
        }
        else
        {
            recHeader = ";";
            recordBytes = MosRecordSize;
        }
        while (pc < end)
        {
            var lineBytes = bytesLeft < recordBytes ? bytesLeft % recordBytes : recordBytes;
            var lineSize = recHeader[0] == 'S' ? lineBytes + 3 : lineBytes;
            sRecBuilder.Append(recHeader);
            sRecBuilder.Append($"{lineSize:X2}");
            sRecBuilder.Append($"{pc:X4}");
            var checkSum = lineSize + GetPcCheckSum(pc);

            for (var i = 0; i < lineBytes; i++)
            {
                var offset = pc - startAddress + i;
                checkSum += fileBytes[offset];
                sRecBuilder.Append($"{fileBytes[offset]:X2}");
            }
            if (recHeader[0] == 'S')
            {
                checkSum = GetSRecordchecksum(checkSum);
                sRecBuilder.Append($"{checkSum:X2}");
            }
            else
            {
                checkSum &= 65535;
                sRecBuilder.Append($"{checkSum:X4}");
            }
            pc += lineBytes;
            bytesLeft -= lineBytes;
            sRecBuilder.Append('\n');
            lineCount++;
        }
        if (recHeader[0] == 'S')
            sRecBuilder.Append("S9030000FC");
        else
            sRecBuilder.Append($";00{lineCount:X4}{lineCount:X4}");
        return new List<byte>(Encoding.ASCII.GetBytes(sRecBuilder.ToString())).AsReadOnly();
    }

    public FormatDescriptor Describe(ReadOnlySpan<byte> codeBytes)
    {
        var recordString = Encoding.ASCII.GetString(codeBytes);
        var expectedHeader = format == OutputFormat.SRecMos ? ';' : 'S';
        var recordBytes = format == OutputFormat.SRecord ? RecordSize : MosRecordSize;
        var lines = recordString.Split(['\n', '\r'],
            StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var startAddress = -1;
        var actualCodeBytes = new List<byte>();
        var index = format == OutputFormat.SRecord ? 1 : 0;
        if (format == OutputFormat.SRecord && lines.Length < 2 || !lines[0].Equals("S0030000FC"))
        {
            return new FormatDescriptor();
        }
        for (; index < lines.Length; index++)
        {
            var line = lines[index];
            if (!TextRecord.TryParse(line, expectedHeader, out var record) || 
                record == null ||
                (record.Type != 1 && record.Type != 9))
            {
                return new FormatDescriptor();
            }
            var lineBytes = record.DataSize < recordBytes ? record.DataSize % recordBytes : recordBytes;
            var lineSize = expectedHeader == 'S' ? lineBytes + 3 : lineBytes;
            var checkSum = lineSize + GetPcCheckSum(record.Address);
            if (startAddress == -1) startAddress = record.Address;
            if (string.IsNullOrEmpty(record.Data))
            {
                break;
            }
            for (var b = 0; b < record.Data.Length; b += 2)
            {
                if (!int.TryParse
                    (
                        record.Data.AsSpan(b, 2),
                        NumberStyles.HexNumber,
                        CultureInfo.InvariantCulture,
                        out var data
                    ))
                {
                    return new FormatDescriptor();
                }
                checkSum += data;
                actualCodeBytes.Add((byte)data);
            }
            checkSum = expectedHeader == 'S'
                ? GetSRecordchecksum(checkSum)
                : checkSum & 65535;
            if (checkSum != record.Checksum)
            {
                return new FormatDescriptor();
            }
        }
        return new FormatDescriptor(startAddress, actualCodeBytes.Count, actualCodeBytes.ToArray());
    }

    private const int RecordSize = 0x20;
    private const int MosRecordSize = 21;

    private static int GetPcCheckSum(int pc) => (pc & 255) + (pc & 65535) / 256;

    private static int GetSRecordchecksum(int value) => ~(value & 255) & 255;
}