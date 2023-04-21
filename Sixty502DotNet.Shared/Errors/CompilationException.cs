//-----------------------------------------------------------------------------
// Copyright (c) 2017-2023 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

namespace Sixty502DotNet.Shared;

/// <summary>
/// An error in compilation.
/// </summary>
public sealed class CompilationException : Exception
{
    /// <summary>
    /// Create a new instance of the compilation exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    public CompilationException(string message)
        : base(message) { }
}

