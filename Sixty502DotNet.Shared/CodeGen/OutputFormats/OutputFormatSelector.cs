//-----------------------------------------------------------------------------
// Copyright (c) 2017-2023 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

namespace Sixty502DotNet.Shared;

/// <summary>
/// Defines a method to select an output format provider based on the
/// format's name and CPU type.
/// </summary>
public static class OutputFormatSelector
{
    /// <summary>
    /// Select the <see cref="IOutputFormatProvider"/> instance for the
    /// given format name and CPU name.
    /// </summary>
    /// <param name="format">The output format name.</param>
    /// <param name="cpu">The CPU name.</param>
    /// <returns>The output formatter.</returns>
    public static IOutputFormatProvider? Select(string format, string cpu)
    {
        return format switch
        {
            "flat"              => null,
            "srec" or "srecmos" => new SRecordFormatProvider(format),
            "hex"               => new HexFormatProvider(),
            "bytesource"        => new ByteSourceFormatProvider(),
            _                   => cpu switch
            {
                "i8080"         => null,
                "z80"           => new Z80FormatProvider(format),
                "m6800" or
                "m6809"         => new MotorolaFormatProvider(),
                _               => format switch
                {
                    "cart"      => new C64CartFormatProvider(),
                    "d64"       => new D64FormatProvider(),
                    "t64"       => new T64FormatProvider(format),
                    _           => new M65xxFormatProvider(format)
                }
            }
        };
    }

    /// <summary>
    /// Gets the default <see cref="IOutputFormatProvider"/>, which is the
    /// <c>cbm</c> output format provider.
    /// </summary>
    public static IOutputFormatProvider DefaultProvider
        => new M65xxFormatProvider("cbm");
}

