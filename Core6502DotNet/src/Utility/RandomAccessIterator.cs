//-----------------------------------------------------------------------------
// Copyright (c) 2017-2021 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Core6502DotNet
{
    /// <summary>
    /// A generic class that implements a random-access iterator for an enumerable collection.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class RandomAccessIterator<T> : IEnumerator<T>, IEnumerable<T>
    {
        #region Members

        readonly T[] _list;
        readonly int _length;
        readonly int _firstIndex;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a new instance of a <see cref="RandomAccessIterator{T}"/> class.
        /// </summary>
        /// <param name="collection">The source collection for the iterator.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public RandomAccessIterator(IEnumerable<T> collection)
           : this(collection, -1) { }

        /// <summary>
        /// Constructs a new instance of a <see cref="RandomAccessIterator{T}"/> class.
        /// </summary>
        /// <param name="collection">The source collection for the iterator.</param>
        /// <param name="firstIndex">The first index of the iterator.</param>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="ArgumentOutOfRangeException"/>
        public RandomAccessIterator(IEnumerable<T> collection, int firstIndex)
        {
            if (collection == null)
                throw new ArgumentNullException();
            _list = collection.ToArray();
            _length = _list.Length;
            if (_length > 0 && (firstIndex < -1 || firstIndex >= _length))
                throw new ArgumentOutOfRangeException();
            _firstIndex = firstIndex;
            Index = firstIndex;
        }

        /// <summary>
        /// Constructs a new instance of a <see cref="RandomAccessIterator{T}"/> class.
        /// </summary>
        /// <param name="iterator">An iterator from which to copy.</param>
        /// <param name="reset">Reset the copied indicator.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public RandomAccessIterator(RandomAccessIterator<T> iterator, bool reset)
        {
            if (iterator == null)
                throw new ArgumentNullException();
            _firstIndex = iterator._firstIndex;
            _list = iterator._list;
            _length = iterator._length;
            Index = reset ? _firstIndex : iterator.Index;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Fasts the iterator forward by the specified amount.
        /// </summary>
        /// <param name="amount">The amount to fast forward the iterator.</param>
        /// <exception cref="ArgumentException"/>
        /// <exception cref="ArgumentOutOfRangeException"/>
        public void FastForward(int amount)
        {
            if (amount == 0)
                throw new ArgumentException();

            if (amount < Index || amount >= _length)
                throw new ArgumentOutOfRangeException();
            Index = amount - 1;
        }

        /// <summary>
        /// Get the next object in the iterator, or the default value if iteration has completed.
        /// </summary>
        /// <returns>The next object.</returns>
        public T GetNext()
        {
            if (++Index < _length)
                return _list[Index];
            return default;
        }

        public bool MoveNext()
        {
            if (Index == _length || ++Index == _length)
            {
                Index = _firstIndex;
                return false;
            }
            return true;
        }

        /// <summary>
        /// Looks at the next element in the collection without advancing the iterator.
        /// </summary>
        /// <returns>The next element, or the default value if the iteration is completing.</returns>
        public T PeekNext()
        {
            if (Index > _firstIndex && Index < _length - 1)
                return _list[Index + 1];
            return default;
        }

        /// <summary>
        /// Looks at the next element in the collection excluding the predicate 
        /// without advancing the iterator.
        /// </summary>
        /// <param name="predicate">The predicate to filter the iteration.</param>
        /// <returns>The next element, or the default value if the iteration is completing.</returns>
        public T PeekNextSkipping(Predicate<T> predicate)
        {
            var i = Index + 1;
            while (i < _length && predicate(_list[i])) { i++; }
            return i < _length ? _list[i] : default;
        }

        public void Reset() => Index = _firstIndex;

        /// <summary>
        /// Rewinds the iterator back to the specified index.
        /// </summary>
        /// <param name="index">The index to move the iterator back.</param>
        /// <exception cref="ArgumentOutOfRangeException"/>
        public void Rewind(int index)
        {
            if (index < _firstIndex || index >= Index)
                throw new ArgumentOutOfRangeException();
            Index = index;
        }

        /// <summary>
        /// Searches for the specified object and returns the zero-based index of the first within the entire iterator.
        /// </summary>
        /// <param name="item">The object to locate.</param>
        /// <returns>The zero-based index of the first occurrence of item within the iterator from its current iteration point, 
        /// if found; otherwise, -1.</returns>
        public int IndexOf(T item) => Index >= 0 ? _list.ToList().IndexOf(item, Index) : -1;

        /// <summary>
        /// Sets the iterator to the specified index.
        /// </summary>
        /// <param name="index">The index of the iterator.</param>
        /// <exception cref="ArgumentOutOfRangeException"/>
        public void SetIndex(int index)
        {
            if (index < Index)
                Rewind(index);
            else if (index >= _length)
                throw new ArgumentOutOfRangeException();

            Index = index;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion

        #region Properties

        /// <summary>
        /// Gets the current index of the iterator.
        /// </summary>
        public int Index { get; private set; }

        public T Current
        {
            get
            {
                if (Index <= _firstIndex || Index >= _length)
                    return default;
                return _list[Index];
            }
        }

        object IEnumerator.Current => Current;

        public void Dispose() { }

        public IEnumerator<T> GetEnumerator() => this;

        #endregion
    }
}