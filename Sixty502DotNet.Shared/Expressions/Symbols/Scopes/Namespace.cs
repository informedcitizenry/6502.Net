//-----------------------------------------------------------------------------
// Copyright (c) 2017-2024 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using Antlr4.Runtime;

namespace Sixty502DotNet.Shared;

/// <summary>
/// A named scope with member symbols that can be re-used throughout
/// assembly.
/// </summary>
public sealed class Namespace : ScopedSymbol
{
    /// <summary>
    /// Construct a new instance of the <see cref="Namespace"/> class from
    /// a token.
    /// </summary>
    /// <param name="token">The identifier token.</param>
    /// <param name="enclosingScope">The enclosing scope.</param>
    public Namespace(IToken token, IScope? enclosingScope)
        : base(token, enclosingScope)
    {
    }

    /// <summary>
    /// Construct a new instance of the <see cref="Namespace"/> class.
    /// </summary>
    /// <param name="name">The namespace name.</param>
    /// <param name="enclosingScope">The enclosing scope.</param>
    public Namespace(string name, IScope? enclosingScope)
        : base(name, enclosingScope)
    {
    }
}

