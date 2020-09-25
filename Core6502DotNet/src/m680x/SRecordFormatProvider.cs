//-----------------------------------------------------------------------------
// Copyright (c) 2017-2020 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Core6502DotNet.m680x
{
    /// <summary>
    /// A class that outputs binary as a Motorola S-Record or S-Record type text file.
    /// </summary>
    public class SRecordFormatProvider : Core6502Base, IBinaryFormatProvider
    {
        const int RecordSize = 0x20;
        const int MosRecordSize = 21;

        static int GetPcCheckSum(int pc) => (pc & 255) + (pc & 65535) / 256;

        static int GetSRecordchecksum(int value) => (~(value & 255)) & 255;

        /// <summary>
        /// Creates a new instance of the S-Record type format provider.
        /// </summary>
        /// <param name="services">The shared <see cref="AssemblyServices"/> object.</param>
        public SRecordFormatProvider(AssemblyServices services)
            : base(services)
        {
        }

        public IEnumerable<byte> GetFormat(IEnumerable<byte> objectBytes)
        {
            var sRecBuilder = new StringBuilder();

            var pc = Services.Output.ProgramStart;
            var fileBytes = objectBytes.ToArray();
            var bytesLeft = fileBytes.Length;
            var lineCount = 0;

            string recHeader;
            int recordBytes;
            var fmt = Services.OutputFormat;
            if (fmt.Equals("srec", Services.StringComparison))
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

            while (pc < Services.Output.ProgramEnd)
            {
                var lineBytes = bytesLeft < recordBytes ? bytesLeft % recordBytes : recordBytes;
                var lineSize = recHeader[0] == 'S' ? lineBytes + 3 : lineBytes;
                sRecBuilder.Append(recHeader);
                sRecBuilder.Append($"{lineSize:X2}");
                sRecBuilder.Append($"{pc:X4}");
                var checkSum = lineSize + GetPcCheckSum(pc);

                for (int i = 0; i < lineBytes; i++)
                {
                    var offset = (pc - Services.Output.ProgramStart) + i;
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
            return Encoding.ASCII.GetBytes(sRecBuilder.ToString());
        }
    }
}