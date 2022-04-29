//-----------------------------------------------------------------------------
// Copyright (c) 2017-2022 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Sixty502DotNet
{
    /// <summary>
    /// A <see cref="ArrayValue"/> object that defines a dictionary value.
    /// A dictionary in 6502.Net takes the form of:
    /// <code>{
    ///    key: value
    /// }</code>, where each key is an <see cref="Value"/> that must be of the
    /// same primitive type and must be unique, and each value must be of the
    /// same type.
    /// </summary>
    public class DictionaryValue : ArrayValue, IDictionary<Value, Value>
    {
        private Dictionary<Value, Value> _dictionary;

        /// <summary>
        /// Construct a new instance of the <see cref="DictionaryValue"/> class.
        /// </summary>
        /// <param name="keys">A list of values that represent the
        /// dictionary keys. The order of the keys in the list must match
        /// that of the list of values.</param>
        /// <param name="values">A list of values that represent the
        /// dictionary values. The order of the values in the list must match
        /// that of the list of keys.</param>
        public DictionaryValue(IList<Value> keys, IList<Value> values)
            : base(values)
        {
            if (keys.Count > 0 && values.Count == keys.Count && CanBeKey(keys[0])
                 && ElementsSameType)
            {
                _dictionary = keys.Zip(values, (k, v) => new { k, v })
              .ToDictionary(x => x.k, x => x.v);
            }
            else
            {
                List.Clear();
                _dictionary = new Dictionary<Value, Value>();
            }
        }

        /// <summary>
        /// Construct a new instance of the <see cref="DictionaryValue"/> class
        /// as a copy of another <see cref="DictionaryValue"/>.
        /// </summary>
        /// <param name="other">The <see cref="DictionaryValue"/> to
        /// copy.</param>
        public DictionaryValue(DictionaryValue other)
            : base(other.List)
        {
            if (other.IsValid)
            {
                _dictionary = other._dictionary;
            }
            else
            {
                _dictionary = new Dictionary<Value, Value>();
                List.Clear();
            }
        }

        public override bool TryGetElement(Value key, out Value value)
        {
            if (key.DotNetType == KeyType && _dictionary.TryGetValue(key, out value!))
            {
                return true;
            }
            value = Undefined;
            return false;
        }

        public override bool TryGetElements(Value start, Value end, out Value value)
        {
            value = Undefined;
            return false;
        }

        public override bool SetAs(Value other)
        {
            if (other is DictionaryValue dict && base.SetAs(other))
            {
                _dictionary = dict._dictionary;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Determines if the <see cref="Value"/> is a value that can be a
        /// key. The key must be a primitive type.
        /// </summary>
        /// <param name="value">The key value.</param>
        /// <returns><c>true</c> if the value can be a key in the dictionary,
        /// <c>false</c> otherwise.</returns>
        public static bool CanBeKey(Value value)
            => value.IsPrimitiveType;

        public void Add(Value key, Value value) => _dictionary.Add(key, value);

        public bool ContainsKey(Value key) => _dictionary.ContainsKey(key);

        public bool Remove(Value key) => _dictionary.Remove(key);

        public bool TryGetValue(Value key, [MaybeNullWhen(false)] out Value value)
            => _dictionary.TryGetValue(key, out value);

        public override string ToString()
        {
            var kvps = new List<string>();
            foreach (var kvp in _dictionary)
                kvps.Add($"{kvp.Key}: {kvp.Value}");
            return $"{{{string.Join(',', kvps)}}}";
        }

        public override bool Equals(Value? other)
        {
            if (other is DictionaryValue dict)
            {
                return _dictionary.Count == dict._dictionary.Count &&
                    !_dictionary.Except(dict._dictionary).Any();
            }
            return false;
        }

        void ICollection<KeyValuePair<Value, Value>>.Add(KeyValuePair<Value, Value> item)
            => throw new NotImplementedException();

        bool ICollection<KeyValuePair<Value, Value>>.Contains(KeyValuePair<Value, Value> item)
            => throw new NotImplementedException();

        void ICollection<KeyValuePair<Value, Value>>.CopyTo(KeyValuePair<Value, Value>[] array, int arrayIndex)
            => throw new NotImplementedException();

        bool ICollection<KeyValuePair<Value, Value>>.Remove(KeyValuePair<Value, Value> item)
            => throw new NotImplementedException();

        IEnumerator<KeyValuePair<Value, Value>> IEnumerable<KeyValuePair<Value, Value>>.GetEnumerator()
            => _dictionary.GetEnumerator();

        /// <summary>
        /// Get the key's .Net type.
        /// </summary>
        public TypeCode KeyType
            => base.IsDefined ? _dictionary.Keys.First().DotNetType
                              : TypeCode.Empty;

        /// <summary>
        /// Get if the dictionary is valid. To be valid, all elements in the
        /// key list must be valid key types and the key values must be unique.
        /// </summary>
        public bool IsValid => ElementsSameType &&
            Keys.All(k => CanBeKey(k)) &&
            Keys.Distinct().Count() == Keys.Count &&
            (Keys.All(k => k.IsNumeric) || (Keys.GroupBy(k => k.DotNetType).Count() == 1));

        /// <summary>
        /// Get the <see cref="DictionaryValue"/>'s entries as a collection of
        /// <see cref="KeyValuePair"/>s.
        /// </summary>
        public IEnumerable<KeyValuePair<Value, Value>> Entries
            => _dictionary.Select(kvp => kvp);

        /// <summary>
        /// Get the <see cref="DictionaryValue"/>'s keys as a
        /// <see cref="Dictionary{TKey, TValue}.KeyCollection"/>.
        /// </summary>
        public Dictionary<Value, Value>.KeyCollection Keys => _dictionary.Keys;

        /// <summary>
        /// Get the <see cref="DictionaryValue"/>'s values as a
        /// <see cref="Dictionary{TKey, TValue}.ValueCollection"/>.
        /// </summary>
        public Dictionary<Value, Value>.ValueCollection Values => _dictionary.Values;

        bool ICollection<KeyValuePair<Value, Value>>.IsReadOnly => false;

        ICollection<Value> IDictionary<Value, Value>.Keys => _dictionary.Keys;

        ICollection<Value> IDictionary<Value, Value>.Values => _dictionary.Values;

        public Value this[Value key]
        {
            get => _dictionary[key];
            set => _dictionary[key] = value;
        }
    }
}
