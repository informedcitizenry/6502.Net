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
/// An object representing a collection of values, either of homogeneous type
/// as an array, or a heterogeneous type as a tuple.
/// </summary>
public sealed class ArrayValue : ValueBase, IList<ValueBase>
{
    private readonly List<ValueBase> _list;
    private bool _elementsSameType;
    private bool _isTuple;

    /// <summary>
    /// Construct a new instance of an <see cref="ArrayValue"/>.
    /// </summary>
    public ArrayValue()
        : this(new List<ValueBase>())
    {

    }

    /// <summary>
    /// Construct a new instance of an <see cref="ArrayValue"/> from
    /// a list of values.
    /// </summary>
    /// <param name="values">The list of values that are the array's
    /// elements.</param>
    public ArrayValue(IList<ValueBase> values)
    {
        _list = new List<ValueBase>();
        _elementsSameType = true;
        _isTuple = false;
        for (int i = 0; i < values.Count; i++)
        {
            this[i] = values[i];
        }
        Prototype = Environment.ArrayType;
        ValueType = ValueType.Array;
    }

    /// <summary>
    /// Construct a new instance of an <see cref="ArrayValue"/> as a copy of
    /// another array.
    /// </summary>
    /// <param name="other">The other <see cref="ArrayValue"/> whose elements
    /// are copied to this one.</param>
    public ArrayValue(ArrayValue other)
        : this(other._list)
    {
        IsTuple = other.IsTuple;
    }

    /// <summary>
    /// Get the element in the array at the specified index.
    /// </summary>
    /// <param name="index">The index to access.</param>
    /// <returns>The value at the index.</returns>
    public override ValueBase this[int index]
    {
        get => _list[index];
        set
        {
            if (index >= Count)
            {
                _list.Add(value);
            }
            else
            {
                _list[index].SetAs(value);
            }
            _elementsSameType &= _list[0].TypeCompatible(value);
        }
    }

    /// <summary>
    /// Checks whether the array contains the given value as an element.
    /// </summary>
    /// <param name="value">The value to test whether the array contains.</param>
    /// <returns><c>true</c> if the array contains the value, <c>false</c>
    /// otherwise.</returns>
    /// <exception cref="TypeMismatchError"></exception>
    public override bool Contains(ValueBase value)
    {
        if (_list.Count > 0)
        {
            if (!_list[0].TypeCompatible(value))
            {
                throw new TypeMismatchError(Expression?.Start);
            }
            return _list.Contains(value);
        }
        return false;
    }

    /// <summary>
    /// Get the element in the array at the specified index.
    /// </summary>
    /// <param name="index">The index to access.</param>
    /// <returns>The value at the index.</returns>
    public override ValueBase this[ValueBase index]
    {
        get
        {
            int i = (int)index.AsDouble();
            if (i < 0)
            {
                i = _list.Count + i;
            }
            return _list[i];
        }
        set
        {
            int i = (int)index.AsDouble();
            if (i < 0)
            {
                i = _list.Count + i;
            }
            this[i] = value;
        }
    }

    public override int Count => _list.Count;

    public bool IsReadOnly => false;

    /// <summary>
    /// Gets whether the array is a tuple.
    /// </summary>
    public bool IsTuple
    {
        get => _isTuple;
        set
        {
            _isTuple = value;
            Prototype = _isTuple ? Environment.TupleType : Environment.ArrayType;
            ValueType = ValueType.Tuple;
        }
    }

    protected override void OnSetAs(ValueBase other)
    {
        if (other is ArrayValue array)
        {
            _list.Clear();
            _list.AddRange(array._list);
        }
        ContainsUndefinedElements = false;
    }

    public override bool TypeCompatible(ValueBase other)
    {
        if (other is ArrayValue otherArray && Count > 0 && otherArray.Count > 0)
        {
            if (IsTuple && otherArray.IsTuple && Count == otherArray.Count)
            {
                for (int i = 0; i < Count; i++)
                {
                    if (!_list[i].TypeCompatible(otherArray._list[i]))
                    {
                        return false;
                    }
                }
                return true;
            }
            if (!IsTuple && !otherArray.IsTuple)
            {
                return ElementsSameType &&
                    otherArray.ElementsSameType &&
                    _list[0].TypeCompatible(otherArray._list[0]);
            }
        }
        return false;
    }

    public override ValueBase[] Slice(int start, int length)
    {
        ValueBase[] arrayVals = new ValueBase[length];
        for (int i = 0; i < length; i++)
        {
            arrayVals[i] = this[start + i];
        }
        return arrayVals;
    }

    public override ValueBase FromRange(Range range)
    {
        return new ArrayValue(Slice(range.Start.Value, range.End.Value - range.Start.Value).ToList())
        {
            IsTuple = IsTuple
        };
    }

    public override string ToString()
    {
        (string openBrace, string closeBrace) = IsTuple ? ("(", ")") : ("[", "]");
        StringBuilder sb = new(openBrace);
        for (int i = 0; i < _list.Count; i++)
        {
            if (i > 0)
            {
                sb.Append(',');
            }
            sb.Append(_list[i].ToString());
        }
        sb.Append(closeBrace);
        return sb.ToString();
    }

    public void Add(ValueBase item)
    {
        if (_list.Count > 0)
        {
            _elementsSameType &= _list[0].TypeCompatible(item);
        }
        ContainsUndefinedElements |= !item.IsDefined;
        _list.Add(item);
    }

    public override ValueBase AddWith(ValueBase rhs)
    {
        if (rhs is not ArrayValue other || IsTuple || other.IsTuple)
        {
            throw new TypeMismatchError();
        }
        IEnumerable<ValueBase> concat = _list.Concat(other._list);
        return new ArrayValue(concat.ToList());
    }

    public override void Reverse()
    {
        _list.Reverse();
    }

    public override void Sort(ValueComparer comparer)
    {
        _list.Order(comparer);
    }

    public void Clear()
    {
        _list.Clear();
    }

    public void CopyTo(ValueBase[] array, int arrayIndex)
    {
        _list.CopyTo(array, arrayIndex);
    }

    public IEnumerator<ValueBase> GetEnumerator()
    {
        return _list.GetEnumerator();
    }

    public int IndexOf(ValueBase item)
    {
        return _list.IndexOf(item);
    }

    public void Insert(int index, ValueBase item)
    {
        _list.Insert(index, item);
    }

    public bool Remove(ValueBase item)
    {
        return _list.Remove(item);
    }

    public void RemoveAt(int index)
    {
        _list.RemoveAt(index);
    }

    public override void Sort()
    {
        _list.Sort();
    }

    public override object? ToObject()
    {
        return null;
    }

    public override IList<ValueBase> ToList()
    {
        return _list;
    }

    public override byte[] ToBytes()
    {
        List<byte> bytes = new();
        for (int i = 0; i < _list.Count; i++)
        {
            bytes.AddRange(_list[i].ToBytes());
        }
        return bytes.ToArray();
    }

    public override byte[] ToEndianBytes(bool little)
    {
        return ToBytes();
    }

    public override int Size()
    {
        int size = 0;
        for (int i = 0; i < _list.Count; i++)
        {
            size += _list[i].Size();
        }
        return size;
    }

    public override ValueBase AsCopy()
    {
        return new ArrayValue(_list)
        {
            IsTuple = IsTuple
        };
    }

    public override string TypeName()
    {
        if (IsTuple || !ElementsSameType)
        {
            StringBuilder sb = new();
            sb.Append("Tuple<");
            for (int i = 0; i < Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(',');
                }
                sb.Append(_list[i].TypeName());
            }
            sb.Append('>');
            return sb.ToString();
        }
        if (Count == 0)
        {
            return "Array<?>";
        }
        return $"Array<{_list[0].TypeName()}>";
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <summary>
    /// Get whether the array contains any elements that are
    /// undefined.
    /// </summary>
    public bool ContainsUndefinedElements { get; private set; }

    protected override int OnCompareTo(ValueBase other)
    {
        return Comparer<int>.Default.Compare(Count, other.Count);
    }

    protected override bool OnEqualTo(ValueBase other)
    {
        ArrayValue otherArray = (ArrayValue)other;
        if (other.Count != Count)
        {
            return false;
        }
        for (int i = 0; i < otherArray.Count; i++)
        {
            if (!_list[i].Equals(other[i]))
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Get if the elements in this <see cref="ArrayValue"/> are all of the
    /// same type.
    /// </summary>
    public bool ElementsSameType => _elementsSameType;
}

