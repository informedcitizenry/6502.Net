//-----------------------------------------------------------------------------
// Copyright (c) 2017-2024 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System.Collections.ObjectModel;
using System.Text;

namespace Sixty502DotNet.Shared;

/// <summary>
/// A class that implements an output format provider that refactors 
/// generated code as 6502.Net-compatible source of <c>.byte</c>
/// statements.
/// </summary>
public sealed class ByteSourceFormatProvider : IOutputFormatProvider
{
    const int BytesPerLine = 8;

    public ReadOnlyCollection<byte> GetFormat(OutputFormatInfo info)
    {
        var output = info.ObjectBytes.ToList();
        if (output.Count == 0)
            return new List<byte>().AsReadOnly();

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
        return new ReadOnlyCollection<byte>(Encoding.UTF8.GetBytes(byteSourceBuilder.ToString()));
    }

    public string FormatName => "byteSource";
}

