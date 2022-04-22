//-----------------------------------------------------------------------------
// Copyright (c) 2017-2022 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

namespace Sixty502DotNet
{
    /// <summary>
    /// A <see cref="Value"/> object that represents an array of
    /// <see cref="Value"/>s. An array in 6502.Net takes the form of:
    /// <code>[ val1, val2, val3 ]</code>, where all values share the same type.
    /// </summary>
    public class ArrayValue : Value, IList<Value>
    {
        /// <summary>
        /// Construct a new instance of the <see cref="ArrayValue"/> class as
        /// a copy of another <see cref="ArrayValue"/>.
        /// </summary>
        /// <param name="other">The other <see cref="ArrayValue"/> to
        /// copy.</param>
        public ArrayValue([NotNull] ArrayValue other)
        {
            List = other.List;
            Data = other.Data;
            Type = other.Type;
            ElementsDefined = other.ElementsDefined;
            ElementsIntegral = other.ElementsIntegral;
            ElementsNumeric = other.ElementsNumeric;
            ElementsSameType = other.ElementsSameType;
            ElementType = other.ElementType;
        }

        /// <summary>
        /// Construct a new instance of the <see cref="ArrayValue"/> class.
        /// </summary>
        /// <param name="list">A list of <see cref="Value"/> elements.</param>
        public ArrayValue(IList<Value> list)
        {
            List = list.ToList();
            Data = list;
            Type = List.GetType();
            ElementsIntegral = list.All(v => v.IsIntegral);
            ElementsNumeric = list.All(v => v.IsNumeric);
            ElementsDefined = list.All(v => v.IsDefined);
            SetTypInfo(list[0]);
        }

        /// <summary>
        /// Construct a new instance of the <see cref="ArrayValue"/> class.
        /// </summary>
        public ArrayValue()
        {
            List = new List<Value>();
            Data = List;
            Type = List.GetType();
            ElementsDefined = false;
        }

        /// <summary>
        /// Convert the <see cref="Value"/> elements in the
        /// <see cref="ArrayValue"/> to an array of elements of the type in the
        /// type parameter.
        /// </summary>
        /// <typeparam name="T">The type of the array to convert the
        /// elements.</typeparam>
        /// <returns>An array of objects of the type specified in the type
        /// parameter if successful, otherwise <c>null</c>.</returns>
        public T[]? ToArray<T>() where T : struct
        {
            if (ElementsSameType)
            {
                var tType = Type.GetTypeCode(typeof(T));
                var elType = List[0].DotNetType;
                if (tType == elType || (ElementsNumeric && tType == TypeCode.Double))
                {
                    var arr = new T[ElementCount];
                    for (var i = 0; i < ElementCount; i++)
                    {
                        arr[i] = List[i].ToValue<T>();
                    }
                    return arr;
                }
            }
            return null;
        }

        /// <summary>
        /// Convert the <see cref="Value"/> elements in the
        /// <see cref="ArrayValue"/> to an array of <see cref="object"/>s
        /// representing the underlying values.
        /// </summary>
        /// <returns>An array of objects.</returns>
        public object[] ToArray()
        {
            var arr = new object[ElementCount];
            for (var i = 0; i < ElementCount; i++)
            {
                arr[i] = List[i].ToObject<object>() ?? double.NaN;
            }
            return arr;
        }

        public override bool TryGetElement(Value index, out Value value)
        {
            var i = GetIndexValue(index);
            if (i >= 0)
            {
                value = List[i];
                return true;
            }
            value = new ArrayValue();
            return false;
        }

        public override bool TryGetElements(Value start, Value end, out Value value)
        {
            (int startIndex, int endIndex) = GetRangeValues(start, end);
            if (startIndex >= 0 && endIndex >= 0)
            {
                var len = endIndex - startIndex + 1;
                value = new ArrayValue(List.Skip(startIndex).Take(len).ToList());
                return true;
            }
            value = new ArrayValue();
            return false;
        }

        public override bool SetAs(Value other)
        {
            if (other is ArrayValue array)
            {
                if (ElementType == TypeCode.Empty || !ElementsDefined)
                {
                    List.AddRange(array.List);
                    Data = List;
                    Type = array.Type;
                    if (List.Count > 0)
                    {
                        SetTypInfo(List[0]);
                    }
                    return true;
                }
                if (ElementType == array.ElementType ||
                    (ElementsNumeric && array.ElementsNumeric))
                {
                    List.Clear();
                    List.AddRange(array.List);
                    Data = List;
                    return true;
                }
            }
            return false;
        }

        private bool CanBeTrue(bool flag) => List.Count == 1 || flag;

        private void SetTypInfo(Value item)
        {
            if (item.DotNetType != TypeCode.Object)
            {
                ElementsIntegral = CanBeTrue(ElementsIntegral) && item.IsIntegral && List[0].IsIntegral;
                ElementsNumeric = CanBeTrue(ElementsNumeric) && item.IsNumeric && List[0].IsNumeric;
                ElementsSameType = ElementsNumeric || List.GroupBy(v => v.DotNetType).Count() == 1;
                ElementType = ElementsSameType ?
                                ElementsNumeric ?
                                  ElementsIntegral ? TypeCode.Int32
                                  : TypeCode.Double
                                  : item.DotNetType
                                  : TypeCode.Object;
            }
            else
            {
                ElementType = TypeCode.Object;
                if (List.All(v => v is ArrayValue))
                {
                    List<ArrayValue> arrayVals = List.OfType<ArrayValue>().ToList();
                    ElementsIntegral = arrayVals.All(av => av.ElementsIntegral);
                    ElementsNumeric = arrayVals.All(av => av.ElementsNumeric);
                    ElementsSameType = arrayVals.All(av => av.ElementsSameType) &&
                                        (ElementsNumeric || arrayVals.GroupBy(av => av.ElementType).Count() == 1);
                }
                else
                {
                    ElementsIntegral = ElementsNumeric = ElementsSameType = false;
                }
            }
        }

        public override bool IsDefined => ElementType != TypeCode.Empty;

        public override int ElementCount => List.Count;

        public void Add(Value item)
        {
            if (Count == 0)
            {
                ElementsDefined = item.IsDefined;
            }
            else
            {
                ElementsDefined &= item.IsDefined;
            }
            List.Add(item);
            SetTypInfo(item);
        }

        public void Clear()
        {
            ElementsDefined = false;
            List.Clear();
            ElementsNumeric = ElementsIntegral = ElementsSameType = false;
            ElementType = TypeCode.Empty;
        }

        public override bool Equals(Value? other)
        {
            if (other is ArrayValue array)
            {
                return List.SequenceEqual(array);
            }
            return false;
        }


        /// <summary>
        /// Search the array for the first index of the element that matches
        /// the search conditions in the predicate, if any.
        /// </summary>
        /// <param name="match">The match predicate.</param>
        /// <returns>The index of the first element in the array whose value
        /// matches the search conditions in the predicate, or <c>-1</c> if
        /// no match is found.</returns>
        public int FindIndex(Predicate<Value> match)
            => List.FindIndex(match);

        /// <summary>
        /// Get if any element in the array has an undefined value.
        /// </summary>
        public bool ContainsUndefinedElement
            => List.Any(v => !v.IsDefined);

        /// <summary>
        /// Get the array type.
        /// </summary>
        public TypeCode ElementType { get; protected set; }

        /// <summary>
        /// Get if the array's elements are the same type.
        /// </summary>
        public bool ElementsSameType { get; protected set; }

        /// <summary>
        /// Get if the array's elements are all numeric.
        /// </summary>
        public bool ElementsNumeric { get; protected set; }

        /// <summary>
        /// Get if the array's elements are all integers.
        /// </summary>
        public bool ElementsIntegral { get; protected set; }

        /// <summary>
        /// Get if all array elements are defined.
        /// </summary>
        public bool ElementsDefined { get; protected set; }

        /// <summary>
        /// Get if each array element has a unique value.
        /// </summary>
        public bool ElementsDistinct
            => List.Distinct().Count() == List.Count;

        /// <summary>
        /// Get the index of the first type that does not match the type of the
        /// initial array element, if any exists, otherwise <c>-1</c>.
        /// </summary>
        public int FirstNonMatchingType
            => List.FindIndex(v => v.DotNetType != List[0].DotNetType);

        public int Count => List.Count;

        bool ICollection<Value>.IsReadOnly => false;

        public Value this[int index]
        {
            get => List[index];
            set => List[index].SetAs(value);
        }

        public override string ToString()
        {
            var arrayStr = new List<string>();
            foreach (var el in List)
                arrayStr.Add(el.ToString());
            return $"[{string.Join(',', arrayStr)}]";
        }

        /// <summary>
        /// Determine if any array element whose value matches the argument's
        /// is in the array.
        /// </summary>
        /// <param name="item">The item whose value to search.</param>
        /// <returns><c>true</c> if the array contains the item value,
        /// <c>false</c> otherwise.</returns>
        public bool Contains(Value item) => List.Contains(item);

        public IEnumerator<Value> GetEnumerator() => List.GetEnumerator();

        void ICollection<Value>.CopyTo(Value[] array, int arrayIndex)
            => List.CopyTo(array, arrayIndex);

        bool ICollection<Value>.Remove(Value item)
            => List.Remove(item);

        int IList<Value>.IndexOf(Value item)
            => List.IndexOf(item);

        void IList<Value>.Insert(int index, Value item)
            => List.Insert(index, item);

        void IList<Value>.RemoveAt(int index)
            => List.RemoveAt(index);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Get the underlying <see cref="Value"/> list.
        /// </summary>
        protected List<Value> List { get; init; }
    }
}