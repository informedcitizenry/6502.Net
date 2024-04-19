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
/// Represents a parsed JSON array, which inherits from the
/// <see cref="ValueBase"/>, and represents a list of <see cref="ValueBase"/>
/// entries.
/// </summary>
public sealed class JsonArray : ValueBase, IEnumerable<ValueBase?>
{
    private readonly List<ValueBase?> _list;

    /// <summary>
    /// Construct a new instance of a <see cref="JsonArray"/>.
    /// </summary>
    public JsonArray()
    {
        ValueType = ValueType.Array;
        _list = new();
    }

    /// <summary>
    /// Construct a new instance of a <see cref="JsonArray"/> as a copy of
    /// an other <see cref="JsonArray"/>.
    /// </summary>
    /// <param name="other">The other <see cref="JsonArray"/>.</param>
    public JsonArray(JsonArray other)
    {
        ValueType = ValueType.Array;
        _list = new List<ValueBase?>(other._list);
    }

    /// <summary>
    /// Add an element to the array.
    /// </summary>
    /// <param name="item">The element to add to the array.</param>
    public void Add(ValueBase? item)
    {
        _list.Add(item);
    }

    /// <summary>
    /// Clear the array.
    /// </summary>
    public void Clear()
    {
        _list.Clear();
    }

    public override bool Contains(ValueBase? item)
    {
        if (item == null)
        {
            return _list.Any(l => l == null);
        }
        return _list.Contains(item);
    }

    /// <summary>
    /// Copy the <see cref="JsonArray"/> elements to an array.
    /// </summary>
    /// <param name="array">The array.</param>
    /// <param name="arrayIndex">The index to copy.</param>
    public void CopyTo(ValueBase?[] array, int arrayIndex)
    {
        _list.CopyTo(array, arrayIndex);
    }

    /// <summary>
    /// Get the index of the element if it exists.
    /// </summary>
    /// <param name="item">The item in the <see cref="JsonArray"/> to
    /// search.</param>
    /// <returns>The index within the array of the value, otherwise
    /// <c>-1</c>.</returns>
    public int IndexOf(ValueBase? item)
    {
        return _list.IndexOf(item);
    }

    /// <summary>
    /// Insert an element into the <see cref="JsonArray"/> at a specified
    /// index.
    /// </summary>
    /// <param name="index">The index to insert the item.</param>
    /// <param name="item">The item to insert.</param>
    public void Insert(int index, ValueBase? item)
    {
        _list.Insert(index, item);
    }

    /// <summary>
    /// Attempt to remove an item from the <see cref="JsonArray"/>.
    /// </summary>
    /// <param name="item">The item to remove.</param>
    /// <returns><c>true</c> if the element can be removed, <c>false</c>
    /// otherwise.</returns>
    public bool Remove(ValueBase? item)
    {
        return _list.Remove(item);
    }

    /// <summary>
    /// Remove the element at a specified index.
    /// </summary>
    /// <param name="index">The index of the item to remove.</param>
    public void RemoveAt(int index)
    {
        _list.RemoveAt(index);
    }

    /// <summary>
    /// Get the item at this index.
    /// </summary>
    /// <param name="index">The index.</param>
    /// <returns>The value if it exists, otherwise an
    /// <see cref="UndefinedValue"/>.</returns>
    public override ValueBase this[int index]
    {
        get => _list[index] ?? new UndefinedValue();
        set
        {
            if (index >= Count)
            {
                _list.Add(value);
            }
            else
            {
                _list[index] = value;
            }
        }
    }

    public override IList<ValueBase> ToList()
    {
        return _list.Where(l => l != null).OfType<ValueBase>().ToList() ?? new List<ValueBase>();
    }

    public override object? Data()
    {
        return _list;
    }

    public override string TypeName()
    {
        return "[Object]";
    }

    internal string ToString(int indent, int startIndent)
    {
        StringBuilder sb = new();
        sb.Append('[');
        if (indent > 0) sb.Append($"\n{new string(' ', indent)}");
        for (int i = 0; i < _list.Count; i++)
        {
            if (i > 0)
            {
                sb.Append(',');
                if (indent > 0)
                {
                    sb.Append($"\n{new string(' ', indent)}");
                }
            }
            if (_list[i] is JsonArray arr && indent > 0)
            {
                sb.Append(arr.ToString(indent * 2, startIndent));
            }
            else if (_list[i] is JsonObject obj && indent > 0)
            {
                sb.Append(obj.ToString(indent * 2, startIndent));
            }
            else sb.Append(_list[i]?.ToString() ?? "null");
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
        return sb.Append(']').ToString();
    }

    /// <summary>
    /// Get a formatted JSON representation of the <see cref="JsonArray"/>.
    /// </summary>
    /// <param name="indent">The formatting indentation amount.</param>
    /// <returns>A formatted JSON string.</returns>
    public string ToString(int indent)
    {
        return ToString(indent, indent);
    }

    /// <summary>
    /// Get the JSON representation of the <see cref="JsonArray"/>.
    /// </summary>
    /// <returns>A JSON string.</returns>
    public override string ToString()
    {
        return ToString(0, 0);
    }

    public override int Count => _list.Count;

    protected override int OnCompareTo(ValueBase other)
    {
        return Comparer<int>.Default.Compare(Count, other.Count);
    }

    protected override bool OnEqualTo(ValueBase other)
    {
        return false;
    }

    protected override void OnSetAs(ValueBase other)
    {

    }

    public IEnumerator<ValueBase?> GetEnumerator()
    {
        return _list.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

