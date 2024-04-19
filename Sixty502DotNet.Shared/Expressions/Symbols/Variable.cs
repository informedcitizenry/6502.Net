//-----------------------------------------------------------------------------
// Copyright (c) 2017-2024 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using Antlr4.Runtime;

namespace Sixty502DotNet.Shared;

/// <summary>
/// A named <see cref="SymbolBase"/> object and value resolver whose value can
/// be changed as often as necessary.
/// </summary>
public sealed class Variable : SymbolBase, IValueResolver
{
    /// <summary>
    /// Create a new instance of the <see cref="Variable"/> class from a token,
    /// initializing it to a value.
    /// </summary>
    /// <param name="token">The identifier token.</param>
    /// <param name="value">The variable's initial value.</param>
    /// <param name="enclosingScope">The variable's enclosing scope.</param>
    public Variable(IToken token, ValueBase value, IScope? enclosingScope)
        : base(token, enclosingScope)
    {
        Value = value;
    }

    /// <summary>
    /// Create a new instance of the <see cref="Variable"/> class from a token.
    /// </summary>
    /// <param name="token">The identifier token.</param>
    /// <param name="enclosingScope">The variable's enclosing scope.</param>
    public Variable(IToken token, IScope? enclosingScope)
        : base(token, enclosingScope)
    {
        Value = new UndefinedValue();
    }

    /// <summary>
    /// Create a new instance of the <see cref="Variable"/> class.
    /// </summary>
    /// <param name="name">The variable name.</param>
    /// <param name="enclosingScope">The variable's enclosing scope.</param>
    public Variable(string name, IScope? enclosingScope)
        : base(name, enclosingScope)
    {
        Value = new UndefinedValue();
    }

    /// <summary>
    /// Create a new instance of the <see cref="Variable"/> class, initializing
    /// it to a value.
    /// </summary>
    /// <param name="name">The variable name.</param>
    /// <param name="value">Tne variable's initial value.</param>
    /// <param name="enclosingScope">The variable's enclosing scope.</param>
    public Variable(string name, ValueBase value, IScope? enclosingScope)
        : base(name, enclosingScope)
    {
        Value = value;
    }

    public override string ToString() => $"{base.ToString()}:{Value.ValueType} = {Value}";

    public bool IsConstant => false;

    public ValueBase Value { get; set; }
}

