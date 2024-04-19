//-----------------------------------------------------------------------------
// Copyright (c) 2017-2024 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using Antlr4.Runtime;

namespace Sixty502DotNet.Shared;

/// <summary>
/// A class representing a label, which is a named reference to program code.
/// A label is a scoped symbol that resolves to an address.
/// </summary>
public sealed class Label : ScopedSymbol, IValueResolver
{
    /// <summary>
    /// Construct a new instance of a <see cref="Label"/> from a token.
    /// </summary>
    /// <param name="token">The identifier token.</param>
    /// <param name="enclosingScope">The label's enclosing scope.</param>
    public Label(IToken token, IScope? enclosingScope)
        : base(token, enclosingScope)
    {
        IsProcScope = false;
        DefinesScope = false;
        Value = new UndefinedValue();
        Bank = 0;
    }

    public override string ToString()
    {
        if (Value.IsDefined)
        {
            return $"{base.ToString()} = ${Value.AsInt():x4} ({Value.AsInt()})";
        }
        return $"{base.ToString()} = $0000 (0)";
    }

    /// <summary>
    /// Get or set the current bank address the label is defined under.
    /// </summary>
    public int Bank { get; set; }

    /// <summary>
    /// Get or set whether the label itself is a scope for other labels and
    /// symbols.
    /// </summary>
    public bool DefinesScope { get; set; }

    public bool IsConstant => true;

    public ValueBase Value { get; set; }
}

