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

using System.Collections;
using System.Text;

namespace Sixty502DotNet.Shared.Eval.Scope;

public sealed class Dictionary 
    : IResolver, IEnumerable<KeyValuePair<Value, Value>>
{
    private readonly Dictionary<Value, Value> _values;
    
    public Dictionary() => _values = new Dictionary<Value, Value>();
    
    public Dictionary(Dictionary other) 
        => _values = other._values.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    
    public Dictionary(IEnumerable<KeyValuePair<Value, Value>> other)
        => _values = other.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    
    public bool TryAdd(Value key, Value value) => _values.TryAdd(key, value);
    
    public void Add(string key, Value  value) 
        => _values.Add(new Value(key), value);
    
    public bool IsCompatibleType(Dictionary other)
    {
        var keys = Keys.ToArray();
        var otherKeys = other.Keys.ToArray();
        var vals = _values.Values.ToArray();
        var otherVals = other._values.Values.ToArray();
        return ((keys.Length == 0 && otherKeys.Length == 0) ||
               keys[0].IsCompatibleType(otherKeys[0])) &&
                ((vals.Length == 0 && otherVals.Length == 0) ||
                 vals[0].IsCompatibleType(otherVals[0]));
    }
    
    public bool ContainsKey(Value key) => _values.ContainsKey(key);

    public bool ContainsKey(string key) 
        => _values.ContainsKey(new Value(key));
    
    public IEnumerable<Value> Keys => _values.Keys;

    public IEnumerable<Value> Values => _values.Values;
    
    public bool TryGetValue(Value key, out Value? value) 
        => _values.TryGetValue(key, out value);
    
    public bool TryGetValue(string key, out Value? value)
        => _values.TryGetValue(new Value(key), out value);

    public bool GetBoolValueFromPath(string path)
    {
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var dictionary = FindDictionaryFromPath(segments);
        if (dictionary?.TryGetValue(segments.Last(), out var value) == true)
        {
            return value?.AsBoolean() ?? false;
        }
        return false;
    }

    public string? GetStringValueFromPath(string path)
    {
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var dictionary = FindDictionaryFromPath(segments);
        return dictionary?.TryGetValue(segments.Last(), out var value) == true 
            ? value?.AsString() : null;
    }

    public long GetIntValueFromPath(string path)
    {
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var dictionary = FindDictionaryFromPath(segments);
        if (dictionary?.TryGetValue(segments.Last(), out var value) == true)
        {
            return value?.AsInt() ?? 0;
        }
        return 0;
    }

    public IList<string> GetListFromPath(string path)
    {
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var dictionary = FindDictionaryFromPath(segments);
        if (dictionary?.TryGetValue(segments.Last(), out var value) == true &&
            value?.AsArray() is {} arrayValue)
        {
            return arrayValue.Select(t => t.AsString()).ToList();
        }
        return [];
    }
    
    private Dictionary? FindDictionaryFromPath(string[] segments)
    {
        var dictionary = this;
        for (var i = 0; i < segments.Length - 1; i++)
        {
            if (!dictionary.TryGetValue(segments[i], out var subValue) ||
                subValue?.AsDictionary() is not { } dictionaryValue)
            {
                return null;
            }
            dictionary = dictionaryValue;
        }
        return dictionary;
    }
    
    public int Count => _values.Count;
    
    public Value this[Value key]
    {
        get => _values[key];
        set => _values[key] = value;
    }

    public Value this[string key] 
        => _values[new Value(key)];

    public Value? Lookup(string key)
    {
        _ = _values.TryGetValue(new Value(key), out var value);
        return value;
    }

    public Value? LookupLocally(string key) => Lookup(key);

    public string Report(string parent, bool labelsAddressesOnly, bool viceLabels)
    {
        var sb = new StringBuilder();
        foreach (var kvp in _values)
        {
            if (!string.IsNullOrEmpty(parent))
            {
                sb.Append(parent);
                sb.Append('.');
            }
            if (kvp.Value.AsResolver() is {} resolver)
            {
                sb.Append(resolver
                    .Report($"{parent}.{kvp.Key}",  labelsAddressesOnly, viceLabels));
            }
        }
        return sb.ToString();
    }
    
    public IEnumerator<KeyValuePair<Value, Value>> GetEnumerator() 
        => _values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}