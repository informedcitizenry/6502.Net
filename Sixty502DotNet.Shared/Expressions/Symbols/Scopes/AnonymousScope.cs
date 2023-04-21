//-----------------------------------------------------------------------------
// Copyright (c) 2017-2023 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

namespace Sixty502DotNet.Shared;

/// <summary>
/// A class that implements an anonymous scope.
/// </summary>
public sealed class AnonymousScope : ScopedSymbol
{
    /// <summary>
    /// Construct a new instance of the <see cref="AnonymousScope"/> class.
    /// </summary>
    /// <param name="index">The index of the start token where the scope begins.
    /// This is used to track the scope's identification.</param>
    /// <param name="parent">The parent scope.</param>
    public AnonymousScope(int index, IScope parent)
        : base($"::{index}", parent)
    {
        EnclosingScope = parent;
    }
}

