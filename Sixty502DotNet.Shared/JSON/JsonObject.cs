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
/// Represents a parsed JSON object, which is a key-value pair of named properties
/// and their values.
/// </summary>
public sealed class JsonObject : ValueBase, IEnumerable<KeyValuePair<string, ValueBase?>>
{
    private readonly Dictionary<string, ValueBase?> _dict;

    /// <summary>
    /// Construct a new instance of a <see cref="JsonObject"/> class.
    /// </summary>
    public JsonObject()
    {
        ValueType = ValueType.Dictionary;
        _dict = new();
    }

    /// <summary>
    /// Construct a new instance of a <see cref="JsonObject"/> class with a
    /// specified key-value member pair.
    /// </summary>
    /// <param name="property">The property name.</param>
    /// <param name="value">The property value.</param>
    public JsonObject(string property, ValueBase? value)
    {
        ValueType = ValueType.Dictionary;
        _dict = new()
        {
            [property] = value
        };
    }

    /// <summary>
    /// Get the object's properties.
    /// </summary>
    /// <returns>A collection of the object's properties.</returns>
    public IEnumerable<string> Properties()
    {
        return _dict.Keys;
    }

    /// <summary>
    /// Get the value by the specified property.
    /// </summary>
    /// <param name="property">The property name.</param>
    /// <returns>The value of the property.</returns>
    public ValueBase? this[string property]
    {
        get
        {
            _ = _dict.TryGetValue(property, out ValueBase? value);
            return value;
        }
    }

    /// <summary>
    /// Add a property to the object.
    /// </summary>
    /// <param name="property">The property name.</param>
    /// <param name="value">The property value.</param>
    public void Add(string property, ValueBase? value)
    {
        _dict.Add(property, value);
    }

    /// <summary>
    /// Get the property value, if it exists.
    /// </summary>
    /// <param name="property">The property name.</param>
    /// <returns>The property value if it exists, otherwise <c>null</c>.
    /// </returns>
    public ValueBase? GetValue(string property)
    {
        if (_dict.TryGetValue(property, out ValueBase? val))
        {
            return val;
        }
        return null;
    }

    /// <summary>
    /// Try to get the property value.
    /// </summary>
    /// <param name="property">The property name.</param>
    /// <param name="value">The <see cref="ValueBase"/> to store the
    /// value, if it exists.</param>
    /// <returns><c>true</c> if the object has the property, <c>false</c>
    /// otherwise.</returns>
    public bool TryGetValue(string property, out ValueBase? value)
    {
        return _dict.TryGetValue(property, out value);
    }

    /// <summary>
    /// Get if the object contains a key (property).
    /// </summary>
    /// <param name="property">The property name.</param>
    /// <returns><c>true</c> if the object contains the property,
    /// <c>false</c> otherwise.</returns>
    public bool ContainsKey(string property)
    {
        return _dict.ContainsKey(property);
    }

    public override object? ToObject()
    {
        throw new NotImplementedException();
    }

    public override string TypeName()
    {
        return "[Object]";
    }

    internal string ToString(int indent, int startIndent)
    {
        StringBuilder sb = new();
        sb.Append('{');
        if (indent > 0) sb.Append($"\n{new string(' ', indent)}");

        int c = 0;
        foreach (KeyValuePair<string, ValueBase?> kvp in _dict)
        {
            if (c++ > 0)
            {
                sb.Append(',');
                if (indent > 0)
                {
                    sb.Append($"\n{new string(' ', indent)}");
                }
            }
            sb.Append($"\"{kvp.Key}\": ");
            if (kvp.Value != null)
            {
                if (kvp.Value is JsonObject obj && indent > 0)
                {
                    sb.Append(obj.ToString(indent * 2, startIndent));
                }
                else if (kvp.Value is JsonArray arr && indent > 0)
                {
                    sb.Append(arr.ToString(indent * 2, startIndent));
                }
                else sb.Append(kvp.Value.ToString());
            }
            else
            {
                sb.Append("null");
            }
        }
        if (indent > 0)
        {
            sb.Append('\n');
            if (indent / 2 < startIndent)
            {
                indent = 0;
            }
            else
            {
                indent /= 2;
            }
            if (indent > 0)
            {
                sb.Append(new string(' ', indent));
            }
        }
        return sb.Append('}').ToString();
    }

    /// <summary>
    /// Get a formatted JSON representation of the <see cref="JsonObject"/>.
    /// </summary>
    /// <param name="indent">The formatting indentation amount.</param>
    /// <returns>A formatted JSON string.</returns>
    public string ToString(int indent)
    {
        return ToString(indent, indent);
    }

    /// <summary>
    /// Get the JSON representation of the <see cref="JsonObject"/>.
    /// </summary>
    /// <returns>A JSON string.</returns>
    public override string ToString()
    {
        return ToString(0, 0);
    }

    protected override int OnCompareTo(ValueBase other)
    {
        return -1;
    }

    protected override bool OnEqualTo(ValueBase other)
    {
        return false;
    }

    protected override void OnSetAs(ValueBase other)
    {

    }

    public IEnumerator<KeyValuePair<string, ValueBase?>> GetEnumerator()
    {
        return _dict.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public override int Count => _dict.Count;
}

