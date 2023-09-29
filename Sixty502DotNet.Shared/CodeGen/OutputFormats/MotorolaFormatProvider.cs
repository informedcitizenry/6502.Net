//-----------------------------------------------------------------------------
// Copyright (c) 2017-2023 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System.Collections.ObjectModel;

namespace Sixty502DotNet.Shared;

/// <summary>
/// A class that implements the default format provider for m680x CPUs.
/// </summary>
public sealed class MotorolaFormatProvider : IOutputFormatProvider
{
    public ReadOnlyCollection<byte> GetFormat(OutputFormatInfo info)
    {
        if (!string.IsNullOrEmpty(FormatName) &&
                !FormatName.Equals("flat"))
            throw new FormatException($"Unrecognized file format: \"{FormatName}\".");
        return info.ObjectBytes.ToList().AsReadOnly();
    }

    public string FormatName => "motorola";
}

