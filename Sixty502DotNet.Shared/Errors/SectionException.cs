//-----------------------------------------------------------------------------
// Copyright (c) 2017-2023 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

namespace Sixty502DotNet.Shared;

/// <summary>
/// An error for an invalid operation with a section.
/// </summary>
public sealed class SectionException : Exception
{
    /// <summary>
    /// Creates an instance of a section error.
    /// </summary>
    /// <param name="">The custom section error message.</param>
    public SectionException(string message)
        : base(message)
    {

    }
}