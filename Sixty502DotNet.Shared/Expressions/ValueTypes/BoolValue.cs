//-----------------------------------------------------------------------------
// Copyright (c) 2017-2024 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

namespace Sixty502DotNet.Shared;

/// <summary>
/// An object representing <see cref="ValueBase"/> as a boolean value. 
/// </summary>
public sealed class BoolValue : ValueBase
{
    private bool _value;

    /// <summary>
    /// Construct a new instance of the <see cref="BoolValue"/> class.
    /// </summary>
    public BoolValue()
    {
        _value = false;
        Prototype = Environment.PrimitiveType;
        ValueType = ValueType.Boolean;
    }

    /// <summary>
    /// Construct a new instance of the <see cref="BoolValue"/> from a boolean
    /// value.
    /// </summary>
    /// <param name="val">The boolean value.</param>
    public BoolValue(bool val)
    {
        _value = val;
        Prototype = Environment.PrimitiveType;
        ValueType = ValueType.Boolean;
    }

    protected override int OnCompareTo(ValueBase other)
    {
        return Comparer<bool>.Default.Compare(_value, other.AsBool());
    }

    public override bool AsBool() => _value;

    public override int Size() => 1;

    public override string TypeName() => "Boolean";

    public override int GetHashCode() => _value.GetHashCode();

    public override string ToString()
    {
        return _value.ToString().ToLower();
    }

    public override bool Equals(object? obj)
    {
        if (obj is ValueBase v)
        {
            return Equals(v);
        }
        return false;
    }

    protected override void OnSetAs(ValueBase other)
    {
        _value = ((BoolValue)other)._value;
    }

    public override ValueBase Not()
    {
        return new BoolValue()
        {
            _value = !_value
        };
    }

    public override ValueBase And(ValueBase rhs)
    {
        return new BoolValue(_value && rhs.AsBool());
    }

    public override ValueBase Or(ValueBase rhs)
    {
        return new BoolValue(_value || rhs.AsBool());
    }

    public override object? Data()
    {
        return _value;
    }

    public override ValueBase AsCopy()
    {
        return new BoolValue(_value);
    }

    public override byte[] ToBytes()
    {
        return new byte[1] { (byte)(_value ? 1 : 0) };
    }

    protected override bool OnEqualTo(ValueBase other)
    {
        return other.AsBool() == _value;
    }
}

