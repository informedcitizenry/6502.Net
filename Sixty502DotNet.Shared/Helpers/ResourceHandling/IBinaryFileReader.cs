//-----------------------------------------------------------------------------
// Copyright (c) 2017-2024 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;

namespace Sixty502DotNet.Shared;

/// <summary>
/// Provides an interface for a class that returns a byte array from a
/// specified path.
/// </summary>
public interface IBinaryFileReader
{
    /// <summary>
    /// Read all bytes from the specified path.
    /// </summary>
    /// <param name="path">The path, which can be a URL or a filepath.</param>
    /// <returns>A <see cref="byte"/> array of the contents the path
    /// refers to.</returns>
    byte[] ReadAllBytes(string path);
}

