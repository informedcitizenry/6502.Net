//-----------------------------------------------------------------------------
// Copyright (c) 2017-2024 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;

namespace Sixty502DotNet.Shared;

/// <summary>
/// Represents a callable object. This class must be inherited.
/// </summary>
public abstract class FunctionObject : ValueBase
{
    /// <summary>
    /// Construct a new instance of the <see cref="FunctionObject"/>.
    /// </summary>
    protected FunctionObject()
    {
        Name = string.Empty;
        ValueType = ValueType.Callable;
        Prototype = Environment.FunctionType;
    }

    public override bool TypeCompatible(ValueBase other)
    {
        return other is FunctionObject;
    }

    /// <summary>
    /// Gets the function arity.
    /// </summary>
    public abstract int Arity { get; init; }

    public override object? Data()
    {
        return this;
    }

    public override string TypeName() => ToString();

    public override string ToString()
    {
        if (Arity < 0)
        {
            return "Callable(var_arg)";
        }
        return $"Callable(argc: {Arity})";
    }

    protected override int OnCompareTo(ValueBase other)
    {
        throw new TypeMismatchError();
    }

    protected override bool OnEqualTo(ValueBase other)
    {
        if (other is FunctionObject fo)
        {
        
            return ReferenceEquals(this, fo);
        }
        return false;
    }

    protected override void OnSetAs(ValueBase other)
    {
        throw new TypeMismatchError();
    }

    /// <summary>
    /// Get or set the callable object's name.
    /// </summary>
    public string Name { get; set; }
}

