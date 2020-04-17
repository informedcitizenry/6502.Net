//-----------------------------------------------------------------------------
// Copyright (c) 2017-2020 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Core6502DotNet
{
    /// <summary>
    /// A class that outputs binary as a Motorola S-Record or S-Record type text file.
    /// </summary>
    public class SRecordFormatProvider : IBinaryFormatProvider
    {
        const int RecordSize = 0x20;
        const int MosRecordSize = 21;

        static int GetPcCheckSum(int pc) => (pc % 256) + ((pc % 65536) / 256);

        static int GetSRecordchecksum(int value) => (~(value % 256)) & 0xFF;

        public IEnumerable<byte> GetFormat()
        {
            var sRecBuilder = new StringBuilder();

            var pc = Assembler.Output.ProgramStart;
            var fileBytes = Assembler.Output.GetCompilation().ToArray();
            var bytesLeft = fileBytes.Length;
            var lineCount = 0;

            string recHeader;
            int recordBytes;
            if (Assembler.Options.Format.Equals("srec"))
            {
                recHeader = "S1";
                recordBytes = RecordSize;
            }
            else
            {
                recHeader = ";";
                recordBytes = MosRecordSize;
            }

            while (pc < Assembler.Output.ProgramEnd)
            {
                var lineBytes = bytesLeft < recordBytes ? bytesLeft % recordBytes : recordBytes;
                var lineSize = recHeader[0] == 'S' ? lineBytes + 3 : lineBytes;
                sRecBuilder.Append(recHeader);
                sRecBuilder.Append($"{lineSize:X2}");
                sRecBuilder.Append($"{pc:X4}");
                var checkSum = lineSize + GetPcCheckSum(pc);

                for (int i = 0; i < lineBytes; i++)
                {
                    checkSum += fileBytes[i];
                    sRecBuilder.Append($"{fileBytes[i]:X2}");
                }
                if (recHeader[0] == 'S')
                {
                    checkSum = GetSRecordchecksum(checkSum);
                    sRecBuilder.Append($"{checkSum:X2}");
                }
                else
                {
                    checkSum = checkSum % 65536;
                    sRecBuilder.Append($"{checkSum:X4}");
                }
                pc += lineBytes;
                bytesLeft -= lineBytes;
                sRecBuilder.Append('\n');
                lineCount++;
            }
            if (recHeader[0] == 'S')
            {
                var checkSum = GetSRecordchecksum(3 + GetPcCheckSum(Assembler.Output.ProgramStart));
                sRecBuilder.Append($"S903{Assembler.Output.ProgramStart:X4}{checkSum:X2}\n");
            }
            else
            {
                sRecBuilder.Append($";00{lineCount:X4}{lineCount:X4}\n");
            }
            return Encoding.ASCII.GetBytes(sRecBuilder.ToString());
        }
    }
}