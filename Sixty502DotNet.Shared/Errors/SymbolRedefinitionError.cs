//-----------------------------------------------------------------------------
// Copyright (c) 2017-2024 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using Antlr4.Runtime;

namespace Sixty502DotNet.Shared;

/// <summary>
/// Represents an error caused when a new symbol shares a name with one
/// that already exists in the current scope.
/// </summary>
public sealed class SymbolRedefinitionError : Error
{
    /// <summary>
    /// Construct a new instance of a <see cref="SymbolRedefinitionError"/> from
    /// a token.
    /// </summary>
    /// <param name="token">The token responsible for the error.</param>
    /// <param name="original">The original token (of the existing symbol
    /// if known).</param>
    public SymbolRedefinitionError(IToken token, IToken? original)
        : base(token, $"Redefinition of symbol '{token.Text}'")
    {
        Original = original;
    }

    /// <summary>
    /// Get the original <see cref="IToken"/> that defined the existing symbol.
    /// </summary>
    public IToken? Original { get; }
}

