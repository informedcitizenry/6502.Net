//-----------------------------------------------------------------------------
// Copyright (c) 2017-2020 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System.Collections.Generic;
using System.Text;

namespace Core6502DotNet
{
    /// <summary>
    /// A class that represents assembly output as source of '.byte' pseudo-ops.
    /// </summary>
    public class ByteSourceFormatProvider : Core6502Base, IBinaryFormatProvider
    {
        const int BytesPerLine = 8;

        /// <summary>
        /// Constructs a new instance of the format provider.
        /// </summary>
        /// <param name="services">The shared <see cref="AssemblyServices"/> object.</param>
        public ByteSourceFormatProvider(AssemblyServices services)
            :base(services)
        {
        }

        public IEnumerable<byte> GetFormat()
        {
            var output = Services.Output.GetCompilation();
            if (output.Count == 0)
                return new List<byte>();

            var byteSourceBuilder = new StringBuilder($"\t\t\t* = ${Services.Output.ProgramStart:x4}\n");
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
