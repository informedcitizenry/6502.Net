//-----------------------------------------------------------------------------
// Copyright (c) 2017-2024 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

namespace Sixty502DotNet.Shared;

/// <summary>
/// An object representing <see cref="ValueBase"/> as a numeric value.
/// </summary>
public sealed class NumericValue : ValueBase
{
    private double _value;
    private int _intValue;

    /// <summary>
    /// Construct a new instance of the <see cref="NumericValue"/>.
    /// </summary>
    public NumericValue()
        : this(double.NaN, true)
    {

    }

    /// <summary>
    /// Construct a new instance of the <see cref="NumericValue"/> from a double.
    /// </summary>
    /// <param name="num">The double floating point value.</param>
    /// <param name="isFloat">Indicates the number is a float.</param>
    public NumericValue(double num, bool isFloat = false)
    {
        _value = num;
        _intValue = (int)num;
        Prototype = Environment.PrimitiveType;
        ValueType = isFloat ? ValueType.Number : ValueType.Integer;
    }

    protected override int OnCompareTo(ValueBase other)
    {
        return Comparer<double>.Default.Compare(_value, other.AsDouble());
    }

    public override bool TypeCompatible(ValueBase other)
    {
        return base.TypeCompatible(other) || other is CharValue;
    }

    public override int Size() => _value.Size();

    public override string TypeName() => "Number";

    public override double AsDouble() => _value;

    public override int AsInt() => _intValue;

    public override int GetHashCode() => _value.GetHashCode();

    protected override void OnSetAs(ValueBase other)
    {
        _value = ((NumericValue)other)._value;
        _intValue = (int)_value;
        bool isintegral = ValueType == ValueType.Integer && other.ValueType == ValueType.Integer;
        ValueType = isintegral ? ValueType.Integer : ValueType.Number;
    }

    public override string ToString()
    {
        return IsBinary ? $"${_intValue.AsPositive():x}" : _value.ToString();
    }

    public override ValueBase Positive()
    {
        return this;
    }

    public override ValueBase Negative()
    {
        return new NumericValue(-_value, ValueType == ValueType.Number);
    }

    public override ValueBase Complement()
    {
        if (ValueType != ValueType.Integer)
        {
            return new NumericValue(Math.Floor(Math.Abs(_value)), ValueType == ValueType.Number);
        }
        return new NumericValue(~_intValue, false);
    }

    public override ValueBase LSB()
    {
        return new NumericValue(_intValue & 0xff, false);
    }

    public override ValueBase MSB()
    {
        return new NumericValue((_intValue & 0xffff) / 256, false);
    }

    public override ValueBase Word()
    {
        return new NumericValue(_intValue & 0xffff, false);
    }

    public override ValueBase Bank()
    {
        return new NumericValue((_intValue & 0xff0000) / 0x10000, false);
    }

    public override ValueBase HigherWord()
    {
        return new NumericValue((_intValue & 0xffff00) / 0x100, false);
    }

    public override ValueBase PowerOf(ValueBase rhs)
    {
        return new NumericValue(Math.Pow(_value, rhs.AsDouble()), ValueType == ValueType.Number || rhs.ValueType == ValueType.Number);
    }

    public override ValueBase MultiplyBy(ValueBase rhs)
    {
        return new NumericValue(_value * rhs.AsDouble(), ValueType == ValueType.Number || rhs.ValueType == ValueType.Number);
    }

    public override ValueBase Mod(ValueBase rhs)
    {
        if (rhs.AsInt() == 0)
        {
            throw new DivideByZeroException("Attempted to divide by zero");
        }
        unchecked
        {
            int longVal = _intValue;
            longVal %= rhs.AsInt();
            return new NumericValue(longVal, false);
        }
    }

    public override ValueBase DivideBy(ValueBase rhs)
    {
        if (rhs.AsDouble() == 0)
        {
            throw new DivideByZeroException("Attempted to divide by zero");
        }
        return new NumericValue(_value / rhs.AsDouble(), ValueType == ValueType.Number || rhs.ValueType == ValueType.Number);
    }

    public override ValueBase AddWith(ValueBase rhs)
    {
        return new NumericValue(_value + rhs.AsDouble(), ValueType == ValueType.Number || rhs.ValueType == ValueType.Number);
    }

    public override ValueBase Subtract(ValueBase rhs)
    {
        return new NumericValue(_value - rhs.AsDouble(), ValueType == ValueType.Number || rhs.ValueType == ValueType.Number);
    }

    public override ValueBase LeftShift(ValueBase rhs)
    {
        return new NumericValue(_value * Math.Pow(2, rhs.AsDouble()), ValueType == ValueType.Number || rhs.ValueType == ValueType.Number);
    }

    public override ValueBase RightShift(ValueBase rhs)
    {
        return new NumericValue(_intValue >> rhs.AsInt(), false);
    }

    public override ValueBase UnsignedRightShift(ValueBase rhs)
    {
        return new NumericValue((long)Math.Abs(_value) >> rhs.AsInt(), false);
    }

    public override ValueBase GreaterThan(ValueBase rhs)
    {
        return new BoolValue(_value > rhs.AsDouble());
    }

    public override ValueBase GTE(ValueBase rhs)
    {
        return new BoolValue(_value >= rhs.AsDouble());
    }

    public override ValueBase LTE(ValueBase rhs)
    {
        return new BoolValue(_value <= rhs.AsDouble());
    }

    public override ValueBase LessThan(ValueBase rhs)
    {
        return new BoolValue(_value < rhs.AsDouble());
    }

    protected override bool OnEqualTo(ValueBase other)
    {
        return _value.AlmostEquals(other.AsDouble());
    }

    public override ValueBase BitwiseAnd(ValueBase rhs)
    {
        return new NumericValue((long)_value & (long)rhs.AsDouble(), false);
    }

    public override ValueBase BitwiseXor(ValueBase rhs)
    {
        return new NumericValue((long)_value ^ (long)rhs.AsDouble(), false);
    }

    public override ValueBase BitwiseOr(ValueBase rhs)
    {
        return new NumericValue((long)_value | (long)rhs.AsDouble(), false);
    }

    public override ValueBase Increment()
    {
        return new NumericValue(_value + 1, ValueType == ValueType.Number);
    }

    public override ValueBase Decrement()
    {
        return new NumericValue(_value - 1, ValueType == ValueType.Number);
    }

    public override byte[] ToBytes()
    {
        return BitConverter.GetBytes((long)_value).Trimmed();
    }

    public override byte[] ToEndianBytes(bool little)
    {
        byte[] bytes = ToBytes();
        if (little != BitConverter.IsLittleEndian)
        {
            return bytes.Reverse().ToArray();
        }
        return bytes;
    }

    public override ValueBase AsCopy()
    {
        return new NumericValue(_value, ValueType == ValueType.Number)
        {
            IsBinary = IsBinary
        };
    }

    public override object? Data()
    {
        if (ValueType == ValueType.Integer)
        {
            if (_value > int.MaxValue)
            {
                return (uint)_value;
            }
            return _intValue;
        }
        return _value;
    }

    /// <summary>
    /// Gets or sets whether the value represents a binary number.
    /// </summary>
    public bool IsBinary { get; set; }
}

