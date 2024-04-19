//-----------------------------------------------------------------------------
// Copyright (c) 2017-2024 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System.Runtime.InteropServices;

namespace Sixty502DotNet.Shared;

/// <summary>
/// A union of a <see cref="double"/> and a <see cref="long"/> holding the
/// double's binary representation in memory.
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = 8)]
public readonly struct Ieee754Converter
{
    /// <summary>
    /// Get the binary field.
    /// </summary>
    [FieldOffset(0)]
    public readonly ulong Binary;

    /// <summary>
    /// Get the float field.
    /// </summary>
    [FieldOffset(0)]
    public readonly double Double;

    /// <summary>
    /// Construct a new <see cref="Ieee754Converter"/> struct from the
    /// double floating point value.
    /// </summary>
    /// <param name="floatVal">The floating point value.</param>
    public Ieee754Converter(double floatVal)
    {
        Binary = 0;
        Double = floatVal;
    }

    /// <summary>
    /// Construct a new <see cref="Ieee754Converter"/> struct from the
    /// binary value.
    /// </summary>
    /// <param name="ulongVal">The binary value.</param>
    public Ieee754Converter(ulong ulongVal)
    {
        Double = 0;
        Binary = ulongVal;
    }
}

