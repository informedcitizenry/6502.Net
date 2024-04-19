//-----------------------------------------------------------------------------
// Copyright (c) 2017-2024 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;

namespace Sixty502DotNet.Shared;

/// <summary>
/// A struct holding basic information about an output binary format.
/// </summary>
public struct FormatInfo
{
    /// <summary>
    /// Construct a new instance of a <see cref="FormatInfo"/> struct.
    /// </summary>
    /// <param name="startAddressOffset">The format's start address offset in
    /// the binary.</param>
    /// <param name="objectCodeOffset">The format's object code offset in the
    /// binary.</param>
    public FormatInfo(int startAddressOffset, int objectCodeOffset)
    {
        StartAddressOffset = startAddressOffset;
        ObjectCodeOffset = objectCodeOffset;
    }

    /// <summary>
    /// Get the format's start address offset in the binary.
    /// </summary>
    public int StartAddressOffset { get; }

    /// <summary>
    /// Get the format's object code offset in the binary.
    /// </summary>
    public int ObjectCodeOffset { get; }
}

