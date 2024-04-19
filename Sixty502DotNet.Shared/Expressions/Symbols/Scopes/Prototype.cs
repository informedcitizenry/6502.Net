//-----------------------------------------------------------------------------
// Copyright (c) 2017-2024 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//----------------------------------------------------------------------------- 

namespace Sixty502DotNet.Shared;

/// <summary>
/// Represents a scope for a built-in type for instances of the type to access
/// its members, such as <c>size</c> and <c>toString</c>.
/// </summary>
public sealed class Prototype : ScopedSymbol
{
    /// <summary>
    /// Construct a new instance of a <see cref="Prototype"/> as a copy of
    /// another <see cref="Prototype"/> object.
    /// </summary>
    /// <param name="other">The other <see cref="Prototype"/> to copy.</param>
    public Prototype(Prototype other)
        : base(other, true)
    {

    }

    /// <summary>
    /// Construct a new instance of a <see cref="Prototype"/>.
    /// </summary>
    /// <param name="name">The type's name.</param>
    /// <param name="scope">The enclosing scope.</param>
    public Prototype(string name, IScope? scope)
        : base(name, scope, true)
    {

    }
}

