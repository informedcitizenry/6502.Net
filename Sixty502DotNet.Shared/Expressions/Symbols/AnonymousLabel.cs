//-----------------------------------------------------------------------------
// Copyright (c) 2017-2023 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

namespace Sixty502DotNet.Shared;

/// <summary>
/// Represents an anonymous label.
/// </summary>
public sealed class AnonymousLabel : IValueResolver
{
    /// <summary>
    /// Construct a new instance of an <see cref="AnonymousLabel"/>.
    /// </summary>
    /// <param name="name">The label name.</param>
    public AnonymousLabel(char name)
    {
        Name = name;
        Value = new UndefinedValue();
    }

    /// <summary>
    /// Get the anonymous label's name (<c>+</c> or <c>-</c>)
    /// </summary>
    public char Name { get; }

    public bool IsConstant => true;

    public ValueBase Value { get; set; }
}

