//-----------------------------------------------------------------------------
// Copyright (c) 2017-2023 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using Antlr4.Runtime;

namespace Sixty502DotNet.Shared;

/// <summary>
/// Represents an error caused by a type mismatch between two expressions.
/// </summary>
public sealed class TypeMismatchError : Error
{
    /// <summary>
    /// Construct a new instance of a <see cref="TypeMismatchError"/>.
    /// </summary>
    public TypeMismatchError()
        : this((IToken?)null)
    {

    }

    /// <summary>
    /// Construct a new instance of a <see cref="TypeMismatchError"/> from
    /// a token.
    /// </summary>
    /// <param name="token">The token that raised the error.</param>
    public TypeMismatchError(IToken? token)
        : base(token, "Type mismatch")
    {

    }

    /// <summary>
    /// Construct a new instance of a <see cref="TypeMismatchError"/> from
    /// a parsed expression.
    /// </summary>
    /// <param name="context">The parsed context that raised the error.</param>
    public TypeMismatchError(ParserRuleContext context)
        : base(context, "Type mismatch")
    {

    }
}
