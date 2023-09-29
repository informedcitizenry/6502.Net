//-----------------------------------------------------------------------------
// Copyright (c) 2017-2023 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System.Collections.ObjectModel;
using System.Text;

namespace Sixty502DotNet.Shared;

/// <summary>
/// An output format provider responsible for converting output code into
/// either a Motorola S-Record or MOS S-Record text file, a format
/// compatible with EPROM readers.
/// </summary>
public sealed class SRecordFormatProvider : IOutputFormatProvider
{
    const int RecordSize = 0x20;
    const int MosRecordSize = 21;

    static int GetPcCheckSum(int pc) => (pc & 255) + (pc & 65535) / 256;

    static int GetSRecordchecksum(int value) => (~(value & 255)) & 255;

    /// <summary>
    /// Construct a new instance of the <see cref="SRecordFormatProvider"/>
    /// class.
    /// </summary>
    /// <param name="formatName">The format name, either <c>srec</c>
    /// or <c>srec-mos</c>.</param>
    public SRecordFormatProvider(string formatName)
        => FormatName = formatName;

    public ReadOnlyCollection<byte> GetFormat(OutputFormatInfo info)
    {
        var sRecBuilder = new StringBuilder();
        var end = info.StartAddress + info.ObjectBytes.Count();
        var pc = info.StartAddress;
        var fileBytes = info.ObjectBytes.ToArray();
        var bytesLeft = fileBytes.Length;
        var lineCount = 0;

        string recHeader;
        int recordBytes;
        if (FormatName.Equals("srec"))
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

            for (int i = 0; i < lineBytes; i++)
            {
                var offset = pc - info.StartAddress + i;
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
        return new ReadOnlyCollection<byte>(Encoding.ASCII.GetBytes(sRecBuilder.ToString()));
    }

    public string FormatName { get; }
}

