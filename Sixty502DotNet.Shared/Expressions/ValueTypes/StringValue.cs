//-----------------------------------------------------------------------------
// Copyright (c) 2017-2024 informedcitizenry <informedcitizenry@gmail.com>
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

    private byte[] _bytes;

    private char[] _chars;

    private readonly bool _isMultiline;

    /// <summary>
    /// Construct a new instance of a <see cref="StringValue"/>.
    /// </summary>
    public StringValue()
    {
        _value = string.Empty;
        _charValueEnumerator = new CharValueEnumerator(string.Empty, Encoding.UTF8, null);
        _chars = Array.Empty<char>();
        _bytes = Array.Empty<byte>();
    }

    public StringValue(string str)
        : this(str, Encoding.UTF8, null)
    {

    }

    /// <summary>
    /// Construct a new instance of a <see cref="StringValue"/> from a given
    /// <see cref="string"/>.
    /// </summary>
    /// <param name="str">The string value.</param>
    /// <param name="encoding">The encoding associated to the <see cref="StringValue"/>.</param>
    /// <param name="encodingName">The encoding name of the encoding.</param>
    public StringValue(string str, Encoding encoding, string? encodingName)
    {
        _value = str.Trim('"');
        _charValueEnumerator = new CharValueEnumerator(_value, encoding, encodingName);
        _isMultiline = _value.Contains('\n') || _value.Contains('\r');
        TextEncoding = encoding;
        EncodingName = encodingName;
        _bytes = null!;
        _chars = null!;
        GetBytesAndChars();
        Prototype = Environment.StringType;
        ValueType = ValueType.String;

    }

    /// <summary>
    /// Construct a new instance of a <see cref="StringValue"/> from a
    /// <see cref="char"/> collection.
    /// </summary>
    /// <param name="chars">The characters of the string.</param>
    /// <param name="encoding">The encoding associated to the <see cref="StringValue"/>.</param>
    /// <param name="encodingName">The encoding name of the encoding.</param>
    public StringValue(IEnumerable<char> chars, Encoding encoding, string? encodingName)
        : this(new string(chars.ToArray()), encoding, encodingName)
    {

    }

    /// <summary>
    /// Construct a new instance of a <see cref="StringValue"/> from a collection
    /// of <see cref="CharValue"/>s.
    /// </summary>
    /// <param name="chars">The char values to make up the string.</param>
    /// <param name="encoding">The encoding associated to the <see cref="StringValue"/>.</param>
    /// <param name="encodingName">The encoding name of the encoding.</param>
    public StringValue(IEnumerable<CharValue> chars, Encoding encoding, string? encodingName)
    {
        StringBuilder sb = new();
        foreach (CharValue c in chars)
        {
            sb.Append(c.AsString());
        }
        _value = sb.ToString();
        TextEncoding = encoding;
        EncodingName = encodingName;
        _charValueEnumerator = new CharValueEnumerator(_value, encoding, encodingName);
        _isMultiline = _value.Contains('\n') || _value.Contains('\r');
        _bytes = null!;
        _chars = null!;
        GetBytesAndChars();
        Prototype = Environment.StringType;
        ValueType = ValueType.String;
    }

    /// <summary>
    /// Construct a new instance of a <see cref="StringValue"/> from a given
    /// array of <see cref="char"/>s.
    /// </summary>
    /// <param name="chars">The char array.</param>
    /// <param name="encoding">The encoding associated to the <see cref="StringValue"/>.</param>
    /// <param name="encodingName">The encoding name of the encoding.</param>
    public StringValue(ValueBase[] chars, Encoding encoding, string? encodingName)
    {
        StringBuilder sb = new();
        for (int i = 0; i < chars.Length; i++)
        {
            sb.Append(chars[i].AsChar());
        }
        _value = sb.ToString();
        _isMultiline = _value.Contains('\n') || _value.Contains('\r');
        TextEncoding = encoding;
        EncodingName = encodingName;
        _charValueEnumerator = new CharValueEnumerator(_value, encoding, encodingName);
        _bytes = null!;
        _chars = null!;
        GetBytesAndChars();
        ValueType = ValueType.String;
        Prototype = Environment.StringType;
    }

    private void GetBytesAndChars()
    {
        if (TextEncoding is AsmEncoding enc)
        {
            var currentEncoding = enc.EncodingName;
            enc.SelectEncoding(EncodingName ?? currentEncoding);
            _bytes = TextEncoding.GetBytes(_value);
            _chars = enc.GetChars(_bytes);
            enc.SelectEncoding(currentEncoding);
            return;
        }
        _bytes = TextEncoding.GetBytes(_value);
        _chars = TextEncoding.GetChars(_bytes);
    }

    private byte[] GetBytes()
    {
        if (TextEncoding is AsmEncoding asmEncoding)
        {
            var currentName = asmEncoding.EncodingName;
            asmEncoding.SelectEncoding(EncodingName ?? currentName);
            var bytes = TextEncoding.GetBytes(_value);
            asmEncoding.SelectEncoding(currentName);
            return bytes;
        }
        return TextEncoding.GetBytes(_value);
    }

    protected override int OnCompareTo(ValueBase other)
    {
        if (other is StringValue sv)
        {
            return Comparer<byte[]>.Default.Compare(_bytes, sv._bytes);
        }
        throw new TypeMismatchError(Expression?.Start);
    }

    protected override bool OnEqualTo(ValueBase? other)
    {
        if (other is StringValue sv)
        {
            return EqualityComparer<byte[]>.Default.Equals(_bytes, sv._bytes);
        }
        throw new TypeMismatchError(Expression?.Start);
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
        if (_bytes.Length > 4)
        {
            throw new IllegalQuantityError(Expression?.Start);
        }
        int val = 0;
        for (int i = _bytes.Length - 1; i >= 0; i--)
        {
            val = (val << 8) | _bytes[i];
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

    public override int Size() => _bytes.Length;

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
        _charValueEnumerator = new CharValueEnumerator(_value, TextEncoding, EncodingName);
    }

    public override ValueBase UpdateMember(ValueBase atIndex, ValueBase value)
    {
        this[atIndex] = value;
        return value;
    }

    public override void Sort()
    {
        Array.Sort(_chars);
        _value = new string(_chars);
        _bytes = ToBytes();
    }

    public override ValueBase this[int index]
    {
        get
        {
            return new CharValue(_chars[index], TextEncoding, EncodingName);
        }
        set
        {
            _chars[index] = value.AsChar();
            _value = new string(_chars);
            _bytes = GetBytes();
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
        char[] slice = _chars.Skip(start).Take(length).ToArray();
        ValueBase[] charVals = new ValueBase[length];
        for (int i = 0; i < length; i++)
        {
            charVals[i] = new CharValue(slice[i], TextEncoding, EncodingName);
        }
        return charVals;
    }

    public override ValueBase FromRange(Range range)
    {
        return new StringValue($"\"{_value[range]}\"", TextEncoding, EncodingName);
    }

    public override ValueBase AddWith(ValueBase rhs)
    {
        StringValue sv;
        if (rhs is CharValue)
        {
            sv = new StringValue($"{_value}{rhs.AsChar()}", TextEncoding, EncodingName);
        }
        else
        {
            sv = new StringValue($"{_value}{rhs.AsString()}", TextEncoding, EncodingName);
        }
        return sv;
    }

    public override int Count => _chars.Length;

    public override IList<ValueBase> ToList()
    {
        List<ValueBase> chars = new();
        for (int i = 0; i < _chars.Length; i++)
        {
            chars.Add(new CharValue(_chars[i], TextEncoding, EncodingName));
        }
        return chars;
    }

    public override object? Data() => _value;

    public override ValueBase AsCopy()
    {
        return new StringValue(ToString(), TextEncoding, EncodingName);
    }

    public override byte[] ToBytes() => _bytes;

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

