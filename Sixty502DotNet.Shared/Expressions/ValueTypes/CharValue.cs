//-----------------------------------------------------------------------------
// Copyright (c) 2017-2024 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System.Text;

namespace Sixty502DotNet.Shared;

/// <summary>
/// An object representing <see cref="ValueBase"/> as a char (8-bit) value.
/// </summary>
public sealed class CharValue : ValueBase
{
    private char _value;

    private int _numVal;

    /// <summary>
    /// Create a new instance of a <see cref="CharValue"/>.
    /// </summary>
    public CharValue()
        : this('\0', Encoding.UTF8, null)
    {

    }

    /// <summary>
    /// Create a new instance of a <see cref="CharValue"/> whose underlying
    /// value is set to the specified character value.
    /// </summary>
    /// <param name="val">The character value.</param>
    /// <param name="encoding">The encoding associated to the <see cref="CharValue"/>.</param>
    /// <param name="encodingName">The encoding name of the encoding.</param>
    public CharValue(char val, Encoding encoding, string? encodingName)
    {
        _value = val;
        TextEncoding = encoding;
        EncodingName = encodingName;
        if (TextEncoding is AsmEncoding enc && !char.IsSurrogate(_value))
        {
            var current = enc.EncodingName;
            enc.SelectEncoding(encodingName ?? current);
            _numVal = enc.GetEncodedValue(_value.ToString());
            enc.SelectEncoding(current);
        }
        else
        {
            _numVal = _value;
        }
        Prototype = Environment.PrimitiveType;
        ValueType = ValueType.Char;
    }


    /// <summary>
    /// Create a new instance of a <see cref="CharValue"/> from a string.
    /// </summary>
    /// <param name="str">The string of the character.</param>
    /// <param name="encoding">The encoding associated to the <see cref="CharValue"/>.</param>
    /// <param name="encodingName">The encoding name of the encoding.</param>
    public CharValue(string str, Encoding encoding, string? encodingName)
        : this(str[1..^1][0], encoding, encodingName)
    {

    }

    public override double AsDouble() => _numVal;

    public override int AsInt() => _numVal;

    protected override int OnCompareTo(ValueBase other)
    {
        return Comparer<int>.Default.Compare(_numVal, other.AsInt());
    }

    public override bool TypeCompatible(ValueBase other)
    {
        return base.TypeCompatible(other) || other is NumericValue;
    }

    protected override bool OnEqualTo(ValueBase other)
    {
        if (other is NumericValue)
        {
            return _numVal == other.AsInt();
        }
        return _value == other.AsChar();
    }

    public override int Size() => _numVal.Size();

    public override string TypeName() => "Character";

    public override ValueBase AddWith(ValueBase rhs)
    {
        if (rhs is StringValue)
        {
            return rhs.AddWith(this);
        }
        if (rhs is CharValue || rhs is NumericValue)
        {
            return new NumericValue(rhs.AsDouble() + AsDouble());
        }
        return base.AddWith(rhs);
    }

    public override string ToString() => $"'{_value}'";

    public override byte[] ToBytes()
    {
        return BitConverter.GetBytes(_numVal).Take(2).ToArray();
    }

    public override byte[] ToEndianBytes(bool little)
    {
        var bytes = ToBytes();
        if (little != BitConverter.IsLittleEndian)
        {
            return bytes.Reverse().ToArray();
        }
        return bytes;
    }

    public override ValueBase AsCopy()
    {
        return new CharValue(ToString(), TextEncoding, EncodingName);
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
            if (TextEncoding is AsmEncoding enc)
            {
                var encodingName = enc.EncodingName;
                enc.SelectEncoding(EncodingName ?? encodingName);
                var chars = enc.GetChars(enc.GetBytes(conv));
                if (chars.Length > 1)
                {
                    throw new InvalidOperationError(Expression?.Start);
                }
                _value = chars[0];
                enc.SelectEncoding(encodingName);
            }
            else
            {
                _value = conv[0];
            }
            _numVal = other.AsInt();
            return;
        }
        _value = ((CharValue)other)._value;
    }

    public override object? Data() => _value;
}

