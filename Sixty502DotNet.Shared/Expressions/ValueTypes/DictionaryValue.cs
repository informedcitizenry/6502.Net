//-----------------------------------------------------------------------------
// Copyright (c) 2017-2024 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;

namespace Sixty502DotNet.Shared;

/// <summary>
/// A <see cref="ArrayValue"/> object that defines a dictionary value.
/// A dictionary in 6502.Net takes the form of:
/// <code>{
///    key: value
/// }</code>, where each key is an <see cref="Value"/> that must be of the
/// same primitive type and must be unique, and each value must be of the
/// same type.
/// </summary>
public sealed partial class DictionaryValue : ValueBase, IDictionary<ValueBase, ValueBase>
{
    /// <summary>
    /// The status of an attempt to set a key/value pair in a
    /// <see cref="DictionaryValue"/>.
    /// </summary>
    public enum AddStatus
    {
        /// <summary>
        /// The key/value pair was added successfully.
        /// </summary>
        Success,
        /// <summary>
        /// The key/value pair could not be added due to a duplicate key.
        /// </summary>
        DuplicateKey,
        /// <summary>
        /// The key/value pair could not be added because the key type does not
        /// match existing key types.
        /// </summary>
        KeyTypeMismatch,
        /// <summary>
        /// The key/value pair could not be added because the key type is not
        /// a valid type for a key.
        /// </summary>
        KeyTypeInvalid,
        /// <summary>
        /// The key/value pair could not be added because the value type does
        /// not match existing value types.
        /// </summary>
        ValueTypeMismatch
    };

    private Dictionary<ValueBase, ValueBase> _dictionary;
    private ValueBase? _firstKey;
    private ValueBase? _firstVal;
    private bool _keyIsStringType;

    /// <summary>
    /// Construct a new instance of the <see cref="DictionaryValue"/>.
    /// </summary>
    public DictionaryValue()
    {
        _keyIsStringType = false;
        _dictionary = new Dictionary<ValueBase, ValueBase>();
        _firstKey = null;
        _firstVal = null;
        Prototype = new(Environment.DictionaryType);
        ValueType = ValueType.Dictionary;
    }

    /// <summary>
    /// Construct a new instance of the <see cref="DictionaryValue"/> class.
    /// </summary>
    /// <param name="keys">A list of values that represent the
    /// dictionary keys. The order of the keys in the list must match
    /// that of the list of values.</param>
    /// <param name="values">A list of values that represent the
    /// dictionary values. The order of the values in the list must match
    /// that of the list of keys.</param>
    public DictionaryValue(IList<ValueBase> keys, IList<ValueBase> values)
    {
        _dictionary = new Dictionary<ValueBase, ValueBase>();
        Prototype = new(Environment.DictionaryType);
        ValueType = ValueType.Dictionary;
        for (int i = 0; i < keys.Count; i++)
        {
            Add(keys[i], values[i]);
        }
    }

    /// <summary>
    /// Constrcut a new instance of a <see cref="DictionaryValue"/> from an
    /// <see cref="IDictionary"/>.
    /// </summary>
    /// <param name="dict">The .Net <see cref="IDictionary"/>.</param>
    public DictionaryValue(IDictionary<ValueBase, ValueBase> dict)
    {
        _dictionary = new();
        Prototype = new(Environment.DictionaryType);
        ValueType = ValueType.Dictionary;
        foreach (KeyValuePair<ValueBase, ValueBase> kvp in dict)
        {
            Add(kvp.Key, kvp.Value);
        }
    }

    public override bool TypeCompatible(ValueBase other)
    {
        if (other is DictionaryValue otherDict &&
            otherDict._firstKey != null && _firstKey != null &&
            otherDict._firstVal != null && _firstVal != null)
        {
            return otherDict._firstKey.TypeCompatible(_firstKey) &&
                   otherDict._firstVal.TypeCompatible(_firstVal);
        }
        return false;
    }

    /// <summary>
    /// Get the value of the given integer key if the key type is numeric.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns>The value for the given key.</returns>
    /// <exception cref="TypeMismatchError"></exception>
    /// <exception cref="KeyNotFoundException"></exception>
    public override ValueBase this[int key]
    {
        get
        {
            NumericValue k = new(key);
            if (_firstKey != null)
            {
                if (!_firstKey.TypeCompatible(k))
                {
                    throw new TypeMismatchError();
                }
                if (_dictionary.TryGetValue(k, out ValueBase? val))
                {
                    return val;
                }
            }
            throw new KeyNotFoundException($"Key {k} not present in dictionary");
        }
        set
        {
            NumericValue k = new(key);
            if (_firstKey?.TypeCompatible(k) != true)
            {
                if (_firstKey != null)
                {
                    throw new TypeMismatchError();
                }
                _firstKey = k;
            }
            _dictionary[k] = value;
        }
    }

    public override ValueBase UpdateMember(ValueBase atIndex, ValueBase value)
    {
        if (atIndex.IsDefined && value.IsDefined && _firstKey?.TypeCompatible(atIndex) == true)
        {
            _dictionary[atIndex] = value;
            return value;
        }
        return new UndefinedValue();
    }

    /// <summary>
    /// Get the value of the given key.
    /// </summary>
    /// <param name="key">The key to look up.</param>
    /// <returns>The value for the given key.</returns>
    public override ValueBase this[ValueBase key]
    {
        get => _dictionary[key];
        set
        {
            if (_dictionary.TryGetValue(key, out ValueBase? existing))
            {
                existing.SetAs(value);
            }
            else
            {
                _dictionary[key] = value;
            }
        }
    }

    public override bool ContainsKey(ValueBase value)
    {
        if (_dictionary.Count > 0)
        {
            if (!_dictionary.Keys.ElementAt(0).TypeCompatible(value))
            {
                throw new TypeMismatchError(Expression?.Start);
            }
            return _dictionary.ContainsKey(value);
        }
        return false;
    }

    protected override int OnCompareTo(ValueBase other)
    {
        return Comparer<int>.Default.Compare(_dictionary.Count, other.Count);
    }

    public override string ToString()
    {
        StringBuilder sb = new("{");
        foreach (KeyValuePair<ValueBase, ValueBase> kvp in _dictionary)
        {
            if (sb.Length > 1)
            {
                sb.Append(", ");
            }
            string keyString = kvp.Key.ToString()!;
            if (kvp.Key is StringValue && Ident().IsMatch(kvp.Key.AsString()))
            {
                keyString = $"{keyString.TrimOnce('"')}";
            }
            sb.Append($"{keyString}: {kvp.Value}");
        }
        sb.Append('}');
        return sb.ToString();
    }

    /// <summary>
    /// Get the dictionary's keys.
    /// </summary>
    public ICollection<ValueBase> Keys => _dictionary.Keys;

    /// <summary>
    /// Get the dictionary's values.
    /// </summary>
    public ICollection<ValueBase> Values => _dictionary.Values;

    public override int Count => _dictionary.Count;

    public bool IsReadOnly => false;

    /// <summary>
    /// Add a key/value pair to the dictionary.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <exception cref="TypeMismatchError"></exception>
    public void Add(ValueBase key, ValueBase value)
    {
        if (_firstKey == null || _firstVal == null)
        {
            _firstKey = key;
            _firstVal = value;
        }
        if (!_firstKey.TypeCompatible(key) || !_firstVal.TypeCompatible(value))
        {
            throw new TypeMismatchError();
        }
        _dictionary.Add(key, value);
    }

    /// <summary>
    /// Try to add the key/value pair to the dictionary.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <param name="addStatus">The <see cref="AddStatus"/>.</param>
    /// <returns><c>true</c> if the key/value pair was addedd successfully,
    /// <c>false</c> otherwise.</returns>
    public bool TryAdd(ValueBase key, ValueBase value, out AddStatus addStatus)
    {
        addStatus = AddStatus.Success;
        if (_firstKey == null || _firstVal == null)
        {
            if (key is not BoolValue &&
                key is not CharValue &&
                key is not NumericValue &&
                key is not StringValue)
            {
                addStatus = AddStatus.KeyTypeInvalid;
                return false;
            }
            _keyIsStringType = key is StringValue || key is CharValue;
            _firstKey = key;
            _firstVal = value;
            _dictionary.Add(key, value);
            if (_keyIsStringType)
            {

                Prototype!.Define(key.AsString(), new Variable(key.AsString(), value, Prototype));
            }
            return true;
        }
        if (!_firstKey.TypeCompatible(key))
        {
            addStatus = AddStatus.KeyTypeMismatch;
            return false;
        }
        if (!_firstVal.TypeCompatible(value))
        {
            addStatus = AddStatus.ValueTypeMismatch;
            return false;
        }
        if (!_dictionary.TryAdd(key, value))
        {
            addStatus = AddStatus.DuplicateKey;
            return false;
        }
        if (_keyIsStringType)
        {
            Prototype!.Define(key.AsString(), new Variable(key.AsString(), value, Prototype));
        }
        return true;
    }

    /// <summary>
    /// Add a key/value pair to the <see cref="DictionaryValue"/>.
    /// </summary>
    /// <param name="item">The key/value pair.</param>
    public void Add(KeyValuePair<ValueBase, ValueBase> item)
    {
        Add(item.Key, item.Value);
    }

    /// <summary>
    /// Clear the dictionary of member elements.
    /// </summary>
    public void Clear()
    {
        _dictionary.Clear();
    }

    /// <summary>
    /// Determine whether the dictionary contains a key/value pair.
    /// </summary>
    /// <param name="item">The key/value pair to lookup.</param>
    /// <returns><c>true</c> if both the key and value are present in the
    /// dictionary, <c>false</c> otherwise.</returns>
    public bool Contains(KeyValuePair<ValueBase, ValueBase> item)
    {
        return _dictionary.Contains(item);
    }

    public void CopyTo(KeyValuePair<ValueBase, ValueBase>[] array, int arrayIndex)
    {
        throw new NotImplementedException();
    }

    public IEnumerator<KeyValuePair<ValueBase, ValueBase>> GetEnumerator()
    {
        return _dictionary.GetEnumerator();
    }

    /// <summary>
    /// Remove the value of the given key from the dictionary.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns><c>true</c> if the value was able to removed for the given
    /// key, <c>false</c> otherwise.</returns>
    public bool Remove(ValueBase key)
    {
        return _dictionary.Remove(key);
    }

    /// <summary>
    /// Remove the key/value pair from the dictionary. 
    /// </summary>
    /// <param name="item">The key/value pair.</param>
    /// <returns><c>true</c> if the value was able to removed for the given
    /// key, <c>false</c> otherwise.</returns>
    /// <exception cref="NotImplementedException"></exception>
    public bool Remove(KeyValuePair<ValueBase, ValueBase> item)
    {
        throw new NotImplementedException();
    }

    public bool TryGetValue(ValueBase key, [MaybeNullWhen(false)] out ValueBase value)
    {
        return _dictionary.TryGetValue(key, out value);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public override object? Data()
    {
        return null;
    }

    public override int Size()
    {
        int size = 0;
        foreach (KeyValuePair<ValueBase, ValueBase> kvp in _dictionary)
        {
            size += kvp.Value.Size();
        }
        return size;
    }

    public override ValueBase AsCopy()
    {
        return new DictionaryValue(this);
    }

    public override string TypeName()
    {
        if (Count > 0)
        {
            KeyValuePair<ValueBase, ValueBase> kvp = _dictionary.First();
            return $"Dictionary<{kvp.Key.TypeName()},{kvp.Value.TypeName()}>";
        }
        return "Dictionary<,>";
    }

    public override ValueBase AddWith(ValueBase rhs)
    {
        DictionaryValue other = (DictionaryValue)rhs;
        return new DictionaryValue(_dictionary.Concat(other._dictionary).ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
    }

    public override IDictionary<ValueBase, ValueBase> ToDictionary()
    {
        return _dictionary;
    }

    protected override void OnSetAs(ValueBase other)
    {
        _dictionary = ((DictionaryValue)other)._dictionary;
    }

    protected override bool OnEqualTo(ValueBase other)
    {
        DictionaryValue otherDict = (DictionaryValue)other;
        if (Count != otherDict.Count)
        {
            return false;
        }
        foreach (KeyValuePair<ValueBase, ValueBase> kvp in _dictionary)
        {
            if (!otherDict._dictionary.TryGetValue(kvp.Key, out ValueBase? otherVal) ||
                !kvp.Value.Equals(otherVal))
            {
                return false;
            }
        }
        return true;
    }
    [GeneratedRegex(@"(_|\p{L})\w*", RegexOptions.Compiled)]
    private static partial Regex Ident();
}

