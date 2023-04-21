//-----------------------------------------------------------------------------
// Copyright (c) 2017-2023 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

namespace Sixty502DotNet.Shared;

/// <summary>
/// Information related to a format to assist the <see cref="IOutputFormatProvider"/>
/// in formatting assembly.
/// </summary>
public sealed class OutputFormatInfo
{
    /// <summary>
    /// Creates a new instance of a <see cref="OutputFormatInfo"/> class.
    /// </summary>
    /// <param name="fileName">The output file name.</param>
    /// <param name="startAddress">The output start address.</param>
    /// <param name="objectBytes">The output object bytes.</param>
    public OutputFormatInfo(string fileName, int startAddress, IEnumerable<byte> objectBytes)
    {
        FileName = fileName;
        StartAddress = startAddress;
        ObjectBytes = objectBytes;
    }

    /// <summary>
    /// The output's file name.
    /// </summary>
    public string FileName { get; }

    /// <summary>
    /// The output start address.
    /// </summary>
    public int StartAddress { get; }

    /// <summary>
    /// The output object bytes.
    /// </summary>
    public IEnumerable<byte> ObjectBytes { get; }
}
