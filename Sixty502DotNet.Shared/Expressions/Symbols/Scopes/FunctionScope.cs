//-----------------------------------------------------------------------------
// Copyright (c) 2017-2023 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

namespace Sixty502DotNet.Shared;

/// <summary>
/// A representation of a scope for all symbols in a function call.
/// </summary>
public sealed class FunctionScope : ScopedSymbol
{
    /// <summary>
    /// Create a new instance of a <see cref="FunctionScope"/>.
    /// </summary>
    /// <param name="name">The scope name.</param>
    /// <param name="parent">The parent <see cref="IScope"/>.</param>
    public FunctionScope(string name, IScope parent)
        : base(name, parent)
    {

    }

    public override string ToString() => $"{base.ToString()}()";
}
