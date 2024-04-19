//-----------------------------------------------------------------------------
// Copyright (c) 2017-2024 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using Antlr4.Runtime;

namespace Sixty502DotNet.Shared;

/// <summary>
/// A symbol that resolves to a constant value that can only be defined once in
/// a pass.
/// </summary>
public sealed class Constant : SymbolBase, IValueResolver
{
    /// <summary>
    /// Create a new instance of a <see cref="Constant"/> symbol.
    /// </summary>
    /// <param name="name">The constant symbol's name.</param>
    /// <param name="enclosingScope">The symbol's enclosing scope.</param>
    /// <param name="isBuiltIn">The constant is a built-in symbol.</param>
    public Constant(string name, IScope? enclosingScope, bool isBuiltIn = false)
        : this(name, new UndefinedValue(), enclosingScope, isBuiltIn)
    {

    }


    /// <summary>
    /// Create a new instance of a <see cref="Constant"/> symbol, initializing
    /// it to a value.
    /// </summary>
    /// <param name="name">The constant symbol's name.</param>
    /// <param name="value">The constant symbol's value.</param>
    /// <param name="enclosingScope">The symbol's enclosing scope.</param>
    /// <param name="isBuiltIn">The constant is a built-in symbol.</param>
    public Constant(string name, ValueBase value, IScope? enclosingScope, bool isBuiltIn = false)
        : base(name, enclosingScope, isBuiltIn)
    {
        Value = value;
        IsReferenced = isBuiltIn;
    }

    /// <summary>
    /// Create a new instance of a <see cref="Constant"/> symbol from a token.
    /// </summary>
    /// <param name="token">The identifier token.</param>
    /// <param name="enclosingScope">The symbol's enclosing scope.</param>
    /// <param name="isBuiltIn">The constant is a built-in symbol.</param>
    public Constant(IToken token, IScope? enclosingScope, bool isBuiltIn = false)
        : this(token, new UndefinedValue(), enclosingScope, isBuiltIn)
    {

    }

    /// <summary>
    /// Create a new instance of a <see cref="Constant"/> symbol from a token,
    /// initializing it to a value.
    /// </summary>
    /// <param name="token">The identifier token.</param>
    /// <param name="value">The constant symbol's value.</param>
    /// <param name="enclosingScope">The symbol's enclosing scope.</param>
    public Constant(IToken token, ValueBase value, IScope? enclosingScope)
        : this(token, value, enclosingScope, false)
    {
    }

    /// <summary>
    /// Create a new instance of a <see cref="Constant"/> symbol from a token,
    /// initializing it to a value.
    /// </summary>
    /// <param name="token">The identifier token.</param>
    /// <param name="value">The constant symbol's value.</param>
    /// <param name="enclosingScope">The symbol's enclosing scope.</param>
    /// <param name="isBuiltIn">The constant is a built-in symbol.</param>
    public Constant(IToken token, ValueBase value, IScope? enclosingScope, bool isBuiltIn = false)
        : base(token, enclosingScope, isBuiltIn)
    {
        Value = value;
    }

    public override string ToString() => $"{base.ToString()} = {Value}";

    public bool IsConstant => true;

    public ValueBase Value { get; set; }
}

