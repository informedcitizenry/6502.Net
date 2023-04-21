//-----------------------------------------------------------------------------
// Copyright (c) 2017-2023 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

namespace Sixty502DotNet.Shared;

/// <summary>
/// An error for a Program Counter rollover.
/// </summary>
public sealed class ProgramOverflowException : Exception
{
    /// <summary>
    /// Creates a new instance of a program overflow error.
    /// </summary>
    /// <param name="message">The custom overflow message.</param>
    public ProgramOverflowException(string message)
        : base(message)
    {
    }
}

