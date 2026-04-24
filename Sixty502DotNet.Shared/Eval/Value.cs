// Copyright (c) 2026 informedcitizenry
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using Sixty502DotNet.Shared.Eval.Function;
using Sixty502DotNet.Shared.Eval.Scope;
using Sixty502DotNet.Shared.Eval.String;
using System.Globalization;
using System.Text;

// ReSharper disable NullableWarningSuppressionIsUsed

namespace Sixty502DotNet.Shared.Eval;

public sealed class Value : IEquatable<Value>, IComparable<Value>
{
    private readonly bool _boolValue;

    private readonly long _intValue;
    
    private readonly Int128 _int128Value;

    private readonly Dictionary? _dictionaryValues;

    private readonly double _floatValue;
    
    private readonly char _charValue;
    
    private readonly AsmString? _stringValue;

    private readonly List<Value>? _arrayValues;

    private readonly IFunction? _functionValue;
    
    private readonly IAddress? _addressValue;

    private readonly IResolver? _resolverValue;

    public Value() => TypeTag = TypeTag.Undefined;

    public Value(AsmString stringValue)
    {
        TypeTag =  TypeTag.String;
        _stringValue = stringValue;
    }
    
    public Value(bool boolValue)
    {
        _boolValue = boolValue;
        TypeTag = TypeTag.Boolean;
    }

    public Value(Value other)
    {
        TypeTag = other.TypeTag;
        switch (TypeTag)
        {
            case TypeTag.Address:
                _addressValue = other._addressValue; break;
            case TypeTag.Array:
            case TypeTag.Tuple:
                _arrayValues = other._arrayValues; break;
            case TypeTag.String:
                _stringValue = other._stringValue; break;
            case TypeTag.Boolean: _boolValue = other._boolValue; break;
            case TypeTag.Char:
                _charValue = other._charValue; 
                break;
            case TypeTag.Dictionary:
                _dictionaryValues = other._dictionaryValues; break;
            case TypeTag.Float: _floatValue = other._floatValue; break;
            case TypeTag.Function: _functionValue = other._functionValue; break;
            case TypeTag.Int: _intValue = other._intValue; break;
            case TypeTag.Int128: _int128Value = other._int128Value; break;
            case TypeTag.Resolver: 
                _resolverValue = other._resolverValue;
                if (_resolverValue is IAddress &&
                    other._resolverValue is IAddress addressable)
                {
                    _addressValue = addressable;
                }
                break;
            case TypeTag.Undefined:
            default:
                break;
        }
    }
    
    public Value(IFunction functionValue)
    {
        _functionValue = functionValue;
        TypeTag = TypeTag.Function;
    }

    public Value(long intValue)
    {
        _intValue = intValue;
        TypeTag = TypeTag.Int;
    }

    public Value(Int128 int128)
    {
        if (int128 >= long.MinValue && int128 <= long.MaxValue)
        {
            _intValue = (long)int128;
            TypeTag =  TypeTag.Int128;
            return;
        }
        _intValue = long.MinValue;
        _int128Value =  int128;
        TypeTag = TypeTag.Int128;

    }
    
    public Value(IAddress address)
    {
        _addressValue = address;
        if (address is IResolver resolver)
        {
            _resolverValue = resolver;
        }
        TypeTag = TypeTag.Address;
    }
    
    public Value(IResolver resolver)
    {
        _resolverValue = resolver;
        if (resolver is IAddress addressable)
        {
            _addressValue = addressable;
        }
        TypeTag = TypeTag.Resolver;
    }

    public Value(double floatValue)
    {
        _floatValue = floatValue;
        TypeTag = TypeTag.Float;
    }
    
    public Value(char charValue)
    {
        _charValue = charValue;
        TypeTag = TypeTag.Char;
    }

    public Value(string stringSegmentValue, TextEncodingType textEncodingType)
    {
        TypeTag = TypeTag.String;
        _stringValue = new AsmString(textEncodingType, stringSegmentValue);
    }
    
    public Value(string stringValue)
    {
        _stringValue = new AsmString(TextEncodingType.Default, stringValue);
        TypeTag =  TypeTag.String;
    }

    public Value(List<Value> arrayValues, TypeTag typeTag)
    {
        _arrayValues = arrayValues.ToList();
        TypeTag = typeTag;
    }

    public Value(Dictionary dictionaryValues)
    {
        _dictionaryValues = new Dictionary(dictionaryValues);
        _resolverValue = _dictionaryValues;
        TypeTag = TypeTag.Dictionary;
    }

    public TypeTag TypeTag { get; }

    public bool IsRValue()
        => !(AsFunction() is Method or Method.Instance || 
             AsResolver() is Namespace || 
             AsResolver() is Enumeration
        {
            Assignable: false
        });

    public override int GetHashCode()
    {
        switch (TypeTag)
        {
            case TypeTag.Array:
            case TypeTag.Tuple:
            {
                var hash = new HashCode();
                for (var i = 0; i < _arrayValues!.Count; i++)
                {
                    hash.Add(_arrayValues[i].GetHashCode());
                }
                return hash.ToHashCode();
            }
            case TypeTag.String:
                return _stringValue!.GetHashCode();
            case TypeTag.Boolean:
                return _boolValue.GetHashCode();
            case TypeTag.Char:
                return _charValue.GetHashCode();
            case TypeTag.Float:
                return _floatValue.GetHashCode();
            case TypeTag.Int:
                return _intValue.GetHashCode();
            case TypeTag.Int128:
                return _int128Value.GetHashCode();
            case TypeTag.Address:
                return AsInt().GetHashCode();
            case TypeTag.Dictionary:
            {
                var hash = new HashCode();
                foreach (var pair in _dictionaryValues!)
                {
                    hash.Add(pair.Key.GetHashCode());
                    hash.Add(pair.Value.GetHashCode());
                }
                return hash.ToHashCode();
            }
            case TypeTag.Function:
                return _functionValue?.GetHashCode() ?? 0;
            case TypeTag.Undefined:
                return 0.GetHashCode();
        }
        return 0;
    }

    public bool CanBeKey()
    {
        return TypeTag switch
        {
            TypeTag.String or
            TypeTag.Char or 
            TypeTag.Address or 
            TypeTag.Boolean or 
            TypeTag.Float or 
            TypeTag.Int or 
            TypeTag.Int128 => true,
            _ => false
        };
    }

    public int CompareTo(Value? other)
    {
        if (other == null) return 1;
        if (Equals(other)) return 0;
        if (AsAsmString() is { } str && other.AsAsmString() is { } otherStr)
        {
            return str.CompareTo(otherStr);
        }
        if (IsNumber() && other.IsNumber())
        {
            if (TypeTag == TypeTag.Float || other.TypeTag == TypeTag.Float)
            {
                return Comparer<double>.Default.Compare(AsDouble(), other.AsDouble());
            }
            if (TypeTag == TypeTag.Int128 || other.TypeTag == TypeTag.Int128)
            {
                return Comparer<Int128>.Default.Compare(AsInt128(), other.AsInt128());
            }
            return Comparer<long>.Default.Compare(AsInt(), other.AsInt());
        }
        if (AsArray() is { } arr && other.AsArray() is { } otherArr 
                                 && TypeTag == TypeTag.Array && other.TypeTag == TypeTag.Array)
        {
            if (arr.Count < otherArr.Count) return -1;
            if (arr.Count > otherArr.Count) return 1;
            int sum = 0;
            for (var i = 0; i < arr.Count; i++)
            {
                sum += arr[i].CompareTo(otherArr[i]);
            }
            return sum;
        }
        return -1;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj is Value other)
        {
            return Equals(other);
        }
        return false;
    }

    public bool IsCompatibleType(Value other)
    {
        if (AsDictionary() is { } dict && other.AsDictionary() is { } otherDict)
        {
            return dict.IsCompatibleType(otherDict);
        }
        if (AsArray() is { } arr && other.AsArray() is { } otherArr && TypeTag == other.TypeTag)
        {
            return TypeTag switch
            {
                TypeTag.Tuple when arr.Count != otherArr.Count => false,
                TypeTag.Tuple => !arr.Where((t, i) => !t.IsCompatibleType(otherArr[i])).Any(),
                _ => (arr.Count == 0 && otherArr.Count == 0) || arr[0].IsCompatibleType(otherArr[0])
            };
        }
        return (IsCharOrString() && other.IsCharOrString()) ||
                (IsNumber() && !IsCharOrString() && other.IsNumber() && !other.IsCharOrString()) ||
                (IsRValue() && other.IsRValue() && TypeTag == other.TypeTag);
    }

    public bool IsIdenticalTo(Value other)
    {
        if (other.TypeTag != TypeTag) return false;
        return TypeTag switch
        {
            TypeTag.Array or 
            TypeTag.Tuple => ReferenceEquals(_arrayValues, other._arrayValues),
            TypeTag.Dictionary => ReferenceEquals(_dictionaryValues, other._dictionaryValues),
            TypeTag.Function => ReferenceEquals(_functionValue, other._functionValue),
            TypeTag.String => ReferenceEquals(_stringValue, other._stringValue),
            TypeTag.Resolver when _resolverValue is not IAddress && other._resolverValue is not IAddress 
                => ReferenceEquals(_resolverValue, other._resolverValue),
            _ => Equals(other)
        };
    }

    public string TypeDisplayName()
    {
        switch (TypeTag)
        {
            case TypeTag.Address:
            case TypeTag.Boolean: 
            case TypeTag.Char:
            case TypeTag.Enumerable:
                return TypeTag.ToString();
            case TypeTag.Float:
            case TypeTag.Int: 
            case TypeTag.Int128: return "Number";
            case TypeTag.Function: return "Function";
            case TypeTag.String: return "String";
        }
        var sb = new StringBuilder();
        if (AsArray() is { } arr)
        {
            sb.Append($"{TypeTag}<");
            if (arr.Count > 0)
            {
                sb.Append(arr[0].TypeDisplayName());
                if (TypeTag == TypeTag.Tuple)
                {
                    for (var i = 1; i < arr.Count; i++)
                    {
                        sb.Append($",{arr[i].TypeDisplayName()}");
                    }
                }
            }
            else
            {
                sb.Append('?');
            }
            sb.Append('>');
        }
        else if (AsDictionary() is { } dict)
        {
            sb.Append("Dictionary<");
            if (dict.Count > 0)
            {
                var firstKvp = dict.First();
                sb.Append($"{firstKvp.Key.TypeDisplayName()},{firstKvp.Value.TypeDisplayName()}");
            }
            else
            {
                sb.Append("?,?");
            }
            sb.Append('>');
        }
        else if (AsResolver() is {} resolver)
        {
            sb.Append(resolver.GetType().Name);
        }
        else
        {
            sb.Append('?');
        }
        return sb.ToString();
    }

    public bool Equals(Value? other)
    {
        if (other is null) return false;
        if (IsCharOrString() && other.IsCharOrString())
        {
            return TypeTag == TypeTag.String 
                ? _stringValue!.Equals(other._stringValue!) 
                : _charValue == other._charValue;
        }
        if (IsNumber() && other.IsNumber())
        {
            if (TypeTag != TypeTag.Int128) return AsDouble().FloatEq(other.AsDouble());
            if (other.TypeTag == TypeTag.Int128) return _int128Value == other._int128Value;
            return false;
        }
        if (other.TypeTag != TypeTag) return false;
        switch (TypeTag)
        {
            case TypeTag.Array:
            case TypeTag.Tuple:
                return _arrayValues!.SequenceEqual(other._arrayValues!);
            case TypeTag.Boolean:
                return _boolValue == other._boolValue;
            case TypeTag.Dictionary:
            {
                if (_dictionaryValues!.Count != other._dictionaryValues!.Count) return false;
                foreach (var kvp in _dictionaryValues)
                {
                    if (!other._dictionaryValues.TryGetValue(kvp.Key, out var value)) return false;
                    if (value?.Equals(kvp.Value) == false) return false;
                }
                return true;
            }
            case TypeTag.Function:
                return ReferenceEquals(_functionValue, other._functionValue);
            case TypeTag.Undefined:
            default:
                return false;
        }
    }

    public bool UpdateIndex(long i, Value value, TextEncodingCollection encodings)
    {
        if (i >= Length ||
            TypeTag != TypeTag.Array ||
            _arrayValues?[(int)i].IsCompatibleType(value) == false)
        {
            return false;
        }
        var existing = _arrayValues![(int)i];
        if (existing.TypeTag == TypeTag.String && value.TypeTag != TypeTag.String)
        {
            _arrayValues![(int)i] = new Value(value.AsString());
        }
        else if (existing.IsNumber() && value.IsCharOrString())
        {
            _arrayValues![(int)i] = new Value(value.AsInt(encodings));
        }
        else
        {
            _arrayValues![(int)i] = value;
        }
        return true;
    }

    public int Length
    {
        get
        {
            return TypeTag switch
            {
                TypeTag.Array or 
                TypeTag.Tuple => _arrayValues!.Count,
                TypeTag.String => _stringValue!.Length,
                TypeTag.Dictionary => _dictionaryValues!.Count,
                _ => 0
            };
        }
    }
    
    public bool IsCharOrString() 
        => TypeTag is TypeTag.String or TypeTag.Char;

    public bool IsNumber()
        => TypeTag switch
        {
            TypeTag.Char or TypeTag.Int or TypeTag.Float or TypeTag.Address or TypeTag.Int128 => true,
            TypeTag.String => _stringValue!.Length <= 4,
            _ => false
        };
    
    public bool IsInt()
        => TypeTag is TypeTag.Char or TypeTag.Int or TypeTag.Address or TypeTag.Int128;
    
    public bool IsDefined => TypeTag != TypeTag.Undefined;
    
    public bool AsBoolean() => _boolValue;

    public double AsDouble(TextEncodingCollection? encoding = null) 
        => TypeTag == TypeTag.Float ? _floatValue : AsInt(encoding);

    public Int128 AsInt128(TextEncodingCollection? encoding = null) 
        => TypeTag == TypeTag.Int128 ? _int128Value : AsInt(encoding);

    public char AsChar()
    {
        switch (TypeTag)
        {
            case TypeTag.Char: return _charValue;
            default:
            {
                try
                {
                    return char.ConvertFromUtf32((int)AsInt())[0];
                }
                catch 
                {
                    return char.MinValue;
                }
            }
        }
    }
    
    public long AsInt(TextEncodingCollection? encoding = null)
    {
        switch (TypeTag)
        {
            case TypeTag.String:
                return encoding?.GetEncodedValue(_stringValue!)
                       ?? _stringValue!.ToInt(encoding?.CurrentTextEncoding ?? Encoding.UTF8) 
                       ?? long.MinValue;
            case TypeTag.Char:
                return encoding?.GetEncodedValue(_charValue) ?? _charValue;
            case TypeTag.Int: return _intValue;
            case TypeTag.Float 
                when double.IsFinite(_floatValue) && 
                     _floatValue is >= long.MinValue and <= long.MaxValue: 
                return (long)_floatValue;
            case TypeTag.Address: return _addressValue!.Address;
            default:
                return long.MinValue;
        }
    }

    public IAddress? AsAddress() => _addressValue;
    
    public IResolver?  AsResolver() => _resolverValue;
    
    public IList<Value>? AsArray() => _arrayValues;
    
    public Dictionary? AsDictionary() => _dictionaryValues;
    
    public IFunction? AsFunction() => _functionValue;

    public AsmString? AsAsmString() => _stringValue;
    
    public string AsString(Encoding? encoding = null)
    {
        switch (TypeTag)
        {
            case TypeTag.String: 
                return _stringValue!.ToString(encoding ?? Encoding.UTF8).Replace("\n", "\\n").Replace("\r", "\\n");
            case TypeTag.Char: return _charValue.ToString();
            case TypeTag.Float: return _floatValue.ToString(CultureInfo.CurrentCulture);
            case TypeTag.Int: return _intValue.ToString(CultureInfo.CurrentCulture);
            case TypeTag.Int128: return _int128Value.ToString(CultureInfo.CurrentCulture);
            case TypeTag.Address:
                return _addressValue!.Address.ToString();
            case TypeTag.Resolver when _resolverValue is IAddress addressable:
                return addressable.Address.ToString();
            case TypeTag.Boolean: return _boolValue ? "true" : "false";
            case TypeTag.Array:
            case TypeTag.Tuple:
            {
                var s = new StringBuilder();
                s.Append(TypeTag == TypeTag.Array ? "[" : "(");
                for (var i = 0; i < _arrayValues!.Count; i++)
                {
                    if (i > 0)
                    {
                        s.Append(", ");
                    }
                    s.Append(_arrayValues[i]);
                }
                s.Append(TypeTag == TypeTag.Array ? "]" : ")");
                return s.ToString();
            }
            case TypeTag.Dictionary:
            {
                var ss = new StringBuilder();
                ss.Append('{');
                var isFirst = true;
                foreach (var kvp in _dictionaryValues!)
                {
                    if (!isFirst)
                    {
                        ss.Append(", ");
                    }
                    ss.Append($"{kvp.Key}: {kvp.Value}");
                    isFirst = false;
                }
                ss.Append('}');
                return ss.ToString();
            }
            case TypeTag.Function:
                return "function()";
            case TypeTag.Undefined: 
            default: return string.Empty;
        }
    }

    public object? ToObject()
    {
        return TypeTag switch
        {
            TypeTag.Address => _addressValue,
            TypeTag.Array or 
            TypeTag.Tuple => _arrayValues,
            TypeTag.Boolean => _boolValue,
            TypeTag.Char => _charValue,
            TypeTag.Dictionary => _dictionaryValues,
            TypeTag.Float => _floatValue,
            TypeTag.Function => _functionValue,
            TypeTag.Int => _intValue,
            TypeTag.Int128 => _int128Value,
            TypeTag.Resolver => _resolverValue,
            TypeTag.String => _stringValue,
            _ => null
        };
    }

    public override string ToString()
    {
        return TypeTag switch
        {
            TypeTag.String => $"\"{AsString()}\"",
            TypeTag.Char => $"'{AsString()}'",
            TypeTag.Undefined => "undefined",
            _ => AsString()
        };
    }
    
    private string PrettyPrint(int indent)
    {
        switch (TypeTag)
        {
            case TypeTag.Address:
            case TypeTag.Boolean: 
            case TypeTag.Int:
            case TypeTag.Int128:
            case TypeTag.Float:
            case TypeTag.String:
                return ToString();
            case TypeTag.Char:
                return $"\"{AsString()}\"";
            case TypeTag.Undefined: return "null";
        }
        StringBuilder sb = new();
        sb.Append(TypeTag == TypeTag.Array ? "[" : "{");
        if (_arrayValues != null)
        {
            for (var i = 0; i < _arrayValues.Count; i++)
            {
                if (i > 0) sb.Append(',');
                if (indent > 0)
                {
                    sb.Append($"\n{new string(' ', indent)}");
                }
                sb.Append(_arrayValues[i].PrettyPrint(indent + 4));
            }
        }
        else
        {
            var dict = _dictionaryValues ?? new Dictionary();
            var c = 0;
            foreach (KeyValuePair<Value, Value> kvp in dict)
            {
                if (c++ > 0)
                {
                    sb.Append(',');
                    
                }
                if (indent > 0)
                {
                    sb.Append($"\n{new string(' ', indent)}");
                }
                sb.Append($"{kvp.Key.ToString()}: ");
                sb.Append(kvp.Value.PrettyPrint(indent + 4));
            }
        }
        if (indent > 0)
        {
            sb.Append('\n');
            indent -= 4;
            if (indent > 0)
            {
                sb.Append(new string(' ', indent));
            }
        }
        return sb.Append(TypeTag == TypeTag.Array ? ']' : '}').ToString();
    }
    
    public string PrettyPrint() => PrettyPrint(4);

    public string JsonPath { get; set; } = string.Empty;

    public Value? Parent { get; set; }
}

