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
/// generated code as a long hexadecimal string.
/// </summary>
public sealed class HexFormatProvider : IOutputFormatProvider
{
    public ReadOnlyCollection<byte> GetFormat(OutputFormatInfo info)
    {
        var hex = BitConverter.ToString(info.ObjectBytes.ToArray()).Replace("-", string.Empty);
        return new ReadOnlyCollection<byte>(Encoding.ASCII.GetBytes(hex));
    }

    public string FormatName => "hex";
}

