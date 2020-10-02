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
    /// A class that represents assembly output as source of '.byte' pseudo-ops.
    /// </summary>
    public class ByteSourceFormatProvider : IBinaryFormatProvider
    {
        const int BytesPerLine = 8;

        public IEnumerable<byte> GetFormat(FormatInfo info)
        {
            var output = info.ObjectBytes.ToList();
            if (output.Count == 0)
                return new List<byte>();

            var byteSourceBuilder = new StringBuilder($"\t\t\t* = ${info.StartAddress:x4}\n");
            if (output.Count == 1)
            {
                byteSourceBuilder.AppendLine($"\n\t\t\t.byte ${output[0]:x2}");
            }
            else
            {
                for (var i = 0; i < output.Count; i++)
                {
                    var byt = output[i];
                    if (i % BytesPerLine == 0 && i < output.Count - 1)
                        byteSourceBuilder.Append($"\n\t\t\t.byte ${byt:x2}");
                    else
                        byteSourceBuilder.Append($", ${byt:x2}");

                }
            }
            byteSourceBuilder.AppendLine();
            return Encoding.UTF8.GetBytes(byteSourceBuilder.ToString());
        }
    }
}
