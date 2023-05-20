//-----------------------------------------------------------------------------
// Copyright (c) 2017-2023 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System.Collections;
using System.Text;

namespace Sixty502DotNet.Shared;

/// <summary>
/// An object representing <see cref="ValueBase"/> as a string value.
/// </summary>
public sealed class StringValue : ValueBase, IEnumerable<CharValue>
{
    private string _value;
    private CharValueEnumerator _charValueEnumerator;
    private readonly bool _isMultiline;

    /// <summary>
    /// Construct a new instance of a <see cref="StringValue"/>.
    /// </summary>
    public StringValue()
        : this(string.Empty)
    {

    }

    /// <summary>
    /// Construct a new instance of a <see cref="StringValue"/> from a given
    /// <see cref="string"/>.
    /// </summary>
    /// <param name="str">The string value.</param>
    public StringValue(string str)
    {
        _value = str.Trim('"');
        _charValueEnumerator = new CharValueEnumerator(_value);
        _isMultiline = _value.Contains('\n') || _value.Contains('\r');
        Prototype = Environment.StringType;
        ValueType = ValueType.String;
    }

    /// <summary>
    /// Construct a new instance of a <see cref="StringValue"/> from a
    /// <see cref="char"/> collection.
    /// </summary>
    /// <param name="chars">The characters of the string.</param>
    public StringValue(IEnumerable<char> chars)
        : this(new string(chars.ToArray()))
    {

    }

    /// <summary>
    /// Construct a new instance of a <see cref="StringValue"/> from a collection
    /// of <see cref="CharValue"/>s.
    /// </summary>
    /// <param name="chars">The char values to make up the string.</param>
    public StringValue(IEnumerable<CharValue> chars)
    {
        StringBuilder sb = new();
        foreach (CharValue c in chars)
        {
            sb.Append(c.AsString());
        }
        _value = sb.ToString();
        _charValueEnumerator = new CharValueEnumerator(_value);
        _isMultiline = _value.Contains('\n') || _value.Contains('\r');
        Prototype = Environment.StringType;
        ValueType = ValueType.String;
    }

    /// <summary>
    /// Construct a new instance of a <see cref="StringValue"/> from a given
    /// array of <see cref="char"/>s.
    /// </summary>
    /// <param name="chars">The char array.</param>
    public StringValue(ValueBase[] chars)
    {
        StringBuilder sb = new();
        for (int i = 0; i < chars.Length; i++)
        {
            char c = chars[i].AsChar();
            _isMultiline |= c == '\n' || c == '\r';
            sb.Append(c);
        }
        _value = sb.ToString();
        _charValueEnumerator = new CharValueEnumerator(_value);
        ValueType = ValueType.String;
        Prototype = Environment.StringType;
    }

    protected override int OnCompareTo(ValueBase other)
    {
        return Comparer<string>.Default.Compare(_value, other.AsString());
    }

    protected override bool OnEqualTo(ValueBase? other)
    {
        return other?.AsString().Equals(_value) == true;
    }

    public override string AsString()
    {
        return _value;
    }

    public override double AsDouble()
    {
        return AsInt();
    }

    public override int AsInt()
    {
        if (TextEncoding is AsmEncoding enc)
        {
            return enc.GetEncodedValue(_value);
        }
        byte[] textBytes = TextEncoding.GetBytes(_value);
        int val = 0;
        for (int i = textBytes.Length - 1; i >= 0; i--)
        {
            val = (val << 8) | textBytes[i];
        }
        return val;
    }

    public override bool Contains(ValueBase value)
    {
        if (value.ValueType == ValueType.String)
        {
            return _value.Contains(value.AsString());
        }
        return _value.Contains(value.AsChar());
    }

    public override string ToString()
    {
        return _isMultiline ? $"\"\"\"{_value.Replace("\n", "\\n").Replace("\r", "\\n")}\"\"\""
            : $"\"{_value}\"";
    }

    public override int Size()
    {
        return TextEncoding.GetByteCount(_value);
    }

    public override string TypeName() => "String";

    public override int GetHashCode() => _value.GetHashCode();

    protected override void OnSetAs(ValueBase other)
    {
        if (other.IsNumeric || other.ValueType == ValueType.Char)
        {
            _value = char.ConvertFromUtf32((int)other.AsDouble());
        }
        else
        {
            _value = ((StringValue)other)._value;
        }
        _charValueEnumerator = new CharValueEnumerator(_value);
    }

    public override void Sort()
    {
        List<char> valueChars = _value.ToCharArray().ToList();
        valueChars.Sort();
        _value = new string(valueChars.ToArray());
    }

    public override ValueBase this[int index]
    {
        get => new CharValue(_value[index]);
        set
        {
            char[] chars = _value.ToCharArray();
            chars[index] = value.AsChar();
            _value = new string(chars);
        }
    }

    public override ValueBase this[ValueBase index]
    {
        get
        {
            int i = (int)index.AsDouble();
            if (i < 0)
            {
                i = _value.Length + i;
            }
            return this[i];
        }
        set
        {
            int i = (int)index.AsDouble();
            if (i < 0)
            {
                i = _value.Length + i;
            }
            this[i] = value;
        }
    }

    public override ValueBase[] Slice(int start, int length)
    {
        ValueBase[] charVals = new ValueBase[length];
        for (int i = 0; i < length; i++)
        {
            charVals[i] = new CharValue($"'{_value[start + i]}'");
        }
        return charVals;
    }

    public override ValueBase FromRange(Range range)
    {
        return new StringValue($"\"{_value[range]}\"");
    }

    public override ValueBase AddWith(ValueBase rhs)
    {
        StringValue sv = new();
        if (rhs is CharValue)
        {
            sv._value = $"{_value}{rhs.AsChar()}";
        }
        else
        {
            sv._value = $"{_value}{rhs.AsString()}";
        }
        return sv;
    }

    public override int Count => _value.Length;

    public override IList<ValueBase> ToList()
    {
        List<ValueBase> chars = new();
        foreach (char c in _value)
        {
            chars.Add(new CharValue($"'{c}'")
            {
                TextEncoding = TextEncoding
            });
        }
        return chars;
    }

    public override object? ToObject()
    {
        return _value;
    }

    public override ValueBase AsCopy()
    {
        return new StringValue(ToString())
        {
            TextEncoding = TextEncoding
        };
    }

    public override byte[] ToBytes()
    {
        return TextEncoding.GetBytes(_value);
    }

    public override byte[] ToEndianBytes(bool little)
    {
        return ToBytes();
    }

    public IEnumerator<CharValue> GetEnumerator()
    {
        return _charValueEnumerator;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

