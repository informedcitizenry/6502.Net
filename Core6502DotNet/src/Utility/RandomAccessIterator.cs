//-----------------------------------------------------------------------------
// Copyright (c) 2017-2020 informedcitizenry <informedcitizenry@gmail.com>
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
           : this(collection, 0) { }

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
            if (_length > 0 && (firstIndex < 0 || firstIndex >= _length))
                throw new ArgumentOutOfRangeException();
            _firstIndex = firstIndex;
            Index = firstIndex - 1;
        }

        /// <summary>
        /// Constructs a new instance of a <see cref="RandomAccessIterator{T}"/> class.
        /// </summary>
        /// <param name="iterator">An iterator from which to copy.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public RandomAccessIterator(RandomAccessIterator<T> iterator)
        {
            if (iterator == null)
                throw new ArgumentNullException();
            _firstIndex = iterator._firstIndex;
            Index = iterator.Index;
            _list = iterator._list;
            _length = iterator._length;
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

        /// <summary>
        /// Moves the iterator to the first element in the collection not matching the
        /// predicate.
        /// </summary>
        /// <param name="predicate">A condition for which to skip any elements 
        /// in the collection while retrieving the next element.</param>
        /// <returns>The first element in the collection 
        /// after the predicate evaluates to false.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public T FirstNotMatching(Predicate<T> predicate)
        {
            if (predicate == null)
                throw new ArgumentNullException();
            while (MoveNext() && predicate(Current)) { }
            return Current;
        }

        /// <summary>
        /// Bypasses the number of elements in a sequence then returns the remaining elements.
        /// </summary>
        /// <param name="count">The number of elements to skip before returning the remaining
        /// elements.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> that contains the elements that occur
        /// after the specified index in the input sequence.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public IEnumerable<T> Skip(int count) => _list.Skip(count);

        /// <summary>
        /// The first element in the collection matching the conditions in the predicate. This operation
        /// will advance the iterator forward until the match is found, or the iteration is complete.
        /// </summary>
        /// <param name="predicate">A condition for which to find the next matching element in the
        /// collection.</param>
        /// <returns>The first element matching the predicate.</returns>
        public T FirstOrDefault(Predicate<T> predicate)
        {
            while (MoveNext() && !predicate(Current)) { }
            return Current;
        }

        public bool MoveNext() => ++Index < _length;

        /// <summary>
        /// Looks at the next element in the collection without advancing the iterator.
        /// </summary>
        /// <returns>The next element, or the default value if the iteration is completing.</returns>
        public T PeekNext()
        {
            if (Index < _length - 1)
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
            while (i < _length && predicate.Invoke(_list[i])) { i++; }
            return i < _length ? _list[i] : default;
        }

        public void Reset() => Index = _firstIndex - 1;

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
                if (Index < _firstIndex || Index >= _length)
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