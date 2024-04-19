//-----------------------------------------------------------------------------
// Copyright (c) 2017-2024 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using Antlr4.Runtime;

namespace Sixty502DotNet.Shared;

/// <summary>
/// Represets an invalid operation on an expression or between expressions.
/// </summary>
public sealed class InvalidOperationError : Error
{
    /// <summary>
    /// Construct a new instance of a <see cref="InvalidOperationError"/>.
    /// </summary>
    public InvalidOperationError()
        : this(null)
    {
    }

    /// <summary>
    /// Construct a new instance of a <see cref="InvalidOperationError"/>
    /// from a token.
    /// </summary>
    /// <param name="token">The token causing the error.</param>
    public InvalidOperationError(IToken? token)
        : base(token, "Invalid operation")
    {

    }
}

