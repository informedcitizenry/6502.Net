//-----------------------------------------------------------------------------
// Copyright (c) 2017-2024 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;

namespace Sixty502DotNet.Shared;

/// <summary>
/// Implements a binary reader, which takes a path and
/// returns a byte array.
/// </summary>
public class FileSystemBinaryReader : IBinaryFileReader
{
    private readonly string? _includePath;

    /// <summary>
    /// Construct a new instance of a <see cref="FileSystemBinaryReader"/> class.
    /// </summary>
    /// <param name="includePath">The include path.</param>
    public FileSystemBinaryReader(string? includePath)
    {
        _includePath = includePath;
    }


    public byte[] ReadAllBytes(string path)
    {
        if (!string.IsNullOrEmpty(_includePath))
        {
            path = Path.Combine(_includePath, path);
        }
        return File.ReadAllBytes(path);
    }
}

