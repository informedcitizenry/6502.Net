//-----------------------------------------------------------------------------
// Copyright (c) 2017-2023 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

namespace Sixty502DotNet.Shared;

/// <summary>
/// An object representing <see cref="ValueBase"/> as a char (8-bit) value.
/// </summary>
public sealed class CharValue : ValueBase
{
    private char _value;

    /// <summary>
    /// Create a new instance of a <see cref="CharValue"/>.
    /// </summary>
    public CharValue()
        : this('\0')
    {

    }

    /// <summary>
    /// Create a new instance of a <see cref="CharValue"/> whose underlying
    /// value is set to the specified character value.
    /// </summary>
    /// <param name="val">The character value.</param>
    public CharValue(char val)
    {
        _value = val;
        Prototype = Environment.PrimitiveType;
        ValueType = ValueType.Char;
    }

    /// <summary>
    /// Create a new instance of a <see cref="CharValue"/> whose underlying
    /// value is set to the specified double value.
    /// </summary>
    /// <param name="num">The double value.</param>
    /// <exception cref="ArgumentException"></exception>
    public CharValue(double num)
    {
        string strVal = char.ConvertFromUtf32((int)num);
        char[] chars = TextEncoding.GetChars(TextEncoding.GetBytes(strVal));
        if (chars.Length > 1)
        {
            throw new ArgumentException();
        }
        _value = chars[0];
        ValueType = ValueType.Char;
        Prototype = Environment.PrimitiveType;
    }

    /// <summary>
    /// Create a new instance of a <see cref="CharValue"/> from a string.
    /// </summary>
    /// <param name="str">The string of the character.</param>
    public CharValue(string str)
        : this(str[1..^1][0])
    {

    }

    public override double AsDouble()
    {
        return AsInt();
    }

    public override int AsInt()
    {
        if (TextEncoding is AsmEncoding enc)
        {
            return enc.GetEncodedValue(_value.ToString());
        }
        return char.ConvertToUtf32(_value.ToString(), 0);
    }

    protected override int OnCompareTo(ValueBase other)
    {
        return Comparer<char>.Default.Compare(_value, other.AsChar());
    }

    public override bool TypeCompatible(ValueBase other)
    {
        return base.TypeCompatible(other) || other is NumericValue;
    }

    protected override bool OnEqualTo(ValueBase other)
    {
        if (other is NumericValue)
        {
            return Convert.ToInt64(_value) == other.AsDouble();
        }
        return _value == other.AsChar();
    }

    public override int Size()
    {
        return TextEncoding.GetByteCount(_value.ToString());
    }

    public override string TypeName() => "Character";

    public override ValueBase AddWith(ValueBase rhs)
    {
        if (rhs is StringValue)
        {
            return new StringValue($"\"{rhs.AddWith(this)}\"")
            {
                TextEncoding = this.TextEncoding
            };
        }
        if (rhs is CharValue || rhs is NumericValue)
        {
            return new NumericValue(rhs.AsDouble() + AsDouble());
        }
        return base.AddWith(rhs);
    }

    public override string ToString()
    {
        return $"'{_value}'";
    }

    public override byte[] ToBytes()
    {
        return TextEncoding.GetBytes(new char[] { _value });
    }

    public override ValueBase AsCopy()
    {
        return new CharValue(ToString())
        {
            TextEncoding = TextEncoding
        };
    }

    public override char AsChar() => _value;

    public override string AsString() => _value.ToString();

    public override int GetHashCode() => _value.GetHashCode();

    protected override void OnSetAs(ValueBase other)
    {
        if (other.IsNumeric)
        {
            string conv = char.ConvertFromUtf32(other.AsInt());
            if (conv.Length > 1)
            {
                throw new InvalidOperationError(Expression?.Start);
            }
            _value = conv[0];
            return;
        }
        _value = ((CharValue)other)._value;
    }

    public override object? ToObject()
    {
        return _value;
    }
}

