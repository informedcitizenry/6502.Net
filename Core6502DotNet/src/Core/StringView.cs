//-----------------------------------------------------------------------------
// Copyright (c) 2017-2021 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;

namespace Core6502DotNet
{
    /// <summary>
    /// Represents a sequence of characters at a specific position and
    /// length in a <see cref="string"/> object.
    /// </summary>
    public class StringView : IEquatable<StringView>, 
                              IComparable<StringView>, 
                              IEnumerable<char>, 
                              IEnumerator<char>
    {
        #region Members
        
        readonly int _endIndex;
        int _enumerator;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a new instance of a string view.
        /// </summary>
        public StringView()
            :this(null, 0, 0)
        {

        }

        /// <summary>
        /// Constructs a new instance of a string view.
        /// </summary>
        /// <param name="str">The string object to construct the view
        /// from.</param>
        public StringView(string str)
            : this(str, 0, str.Length)
        {

        }


        /// <summary>
        /// Constructs a new instance of a string view.
        /// </summary>
        /// <param name="str">The string object to construct the view
        /// from.</param>
        /// <param name="position">The starting position of the string view.</param>
        /// <param name="length">The view length.</param>
        public StringView(string str, int position, int length)
        {
            String = str;
            Position = position;
            Length = length;
            _endIndex = Position + Length;
            _enumerator = -1;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Returns a value wether a specified character occurs within the
        /// string at the defined view.
        /// </summary>
        /// <param name="value">The character to seek.</param>
        /// <returns>c>true</c> if the value parameter occurs within this string; 
        /// otherwise, <c>false</c>.</returns>
        public bool Contains(char value)
        {
            for (int i = Position; i < _endIndex; i++)
                if (String[i] == value)
                    return true;
            return false;
        }

        public override int GetHashCode() => GetHashCode(false);

        internal int GetHashCode(bool ignoreCase)
        {
            unchecked
            {
                var hash = 17;
                for (var i = Position; i < _endIndex; i++)
                {
                    hash *= 23;
                    if (ignoreCase)
                        hash += char.ToLower(String[i]).GetHashCode();
                    else
                        hash += String[i].GetHashCode();
                }
                return hash;
            }
        }

        /// <summary>
        /// Returns a copy of this <see cref="StringView"/> converted to
        /// lowercase.
        /// </summary>
        /// <returns>A <see cref="StringView"/> in lower case</returns>
        public string ToLower() => String.Substring(Position, Length).ToLower();

        /// <summary>
        /// Retrieves a substring from this instance. The substring starts at a specified
        ///  character position and has a specified length.
        /// </summary>
        /// <param name="startIndex">The zero-based starting character position of a 
        /// substring in this instance.</param>
        /// <param name="length">The number of characters in the substring.</param>
        /// <returns>A string that is equivalent to the substring of length length that begins at
        /// startIndex in this instance, or System.String.Empty if startIndex is equal to
        /// the length of this instance and length is zero.</returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public string Substring(int startIndex, int length)
            => String.Substring(Position + startIndex, length);

        /// <summary>
        /// Retrieves a substring from this instance. The substring starts at a specified
        ///  character position and has a specified length.
        /// </summary>
        /// <param name="startIndex">The zero-based starting character position of a 
        /// substring in this instance.</param>
        /// <returns>A string that is equivalent to the substring of length length that begins at
        /// startIndex in this instance, or System.String.Empty if startIndex is equal to
        /// the length of this instance and length is zero.</returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public string Substring(int startIndex)
            => Substring(startIndex, Length - startIndex);

        /// <summary>
        /// Gets a new <see cref="StringView"/> within the specified <see cref="Range"/>
        /// of the current <see cref="StringView"/>.
        /// </summary>
        /// <param name="range">The range within the current <see cref="StringView"/>.</param>
        /// <returns>A <see cref="StringView"/> from a range of the current <see cref="StringView"/>.</returns>
        /// <exception cref="IndexOutOfRangeException"></exception>
        public StringView this[Range range]
        {
            get
            {
                var actStart = range.Start.Value;
                if (range.Start.IsFromEnd)
                    actStart = Length - actStart;
                var actEnd = range.End.Value;
                if (range.End.IsFromEnd)
                    actEnd = Length - actEnd;
                var len = actEnd - actStart + 1;
                return new StringView(String, Position + actStart, len);
            }
        }


        /// <summary>
        /// Gets the <see cref="char"/> object at a specified position in the
        /// current <see cref="StringView"/> object.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>The object at position index.</returns>
        /// <exception cref="IndexOutOfRangeException"></exception>
        public char this[Index index]
        {
            get
            {
                var actOffs = index.Value;
                if (index.IsFromEnd)
                    actOffs = Length - actOffs;
                return String[Position + actOffs];
            }
        }

        public override string ToString() =>
            String.Substring(Position, Length);

        public static implicit operator StringView(string str) => new StringView(str);

        internal static int CompareViews(StringView v1, StringView v2, bool caseSensitive)
        {  
            for(int i = v1.Position, j = v2.Position; i < v1._endIndex && j < v2._endIndex; i++, j++)
            {
                var c1 = caseSensitive ? v1.String[i] : char.ToLower(v1.String[i]);
                var c2 = caseSensitive ? v2.String[j] : char.ToLower(v2.String[j]);
                if (c1 < c2)
                    return -1;
                if (c1 > c2)
                    return 1;
            }
            return Comparer<int>.Default.Compare(v1.Length, v2.Length);
        }

        internal static bool ViewsEqual(StringView v1, StringView v2, bool caseSensitive)
        {
            for (int i = v1.Position, j = v2.Position; i < v1._endIndex && j < v2._endIndex; i++, j++)
            {
                var c1 = caseSensitive ? v1.String[i] : char.ToLower(v1.String[i]);
                var c2 = caseSensitive ? v2.String[j] : char.ToLower(v2.String[j]);
                if (c1 != c2)
                    return false;
            }
            return v1.Length == v2.Length;
        }

        public bool Equals(StringView other)
        {
            if (Length == other.Length)
            {
                for(int i = Position, j = other.Position; i < _endIndex; i++, j++)
                {
                    if (String[i] != other.String[j])
                        return false;
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Determines whether the string represented by this 
        /// <see cref="StringView"/> and a specified <see cref="string"/>
        /// object have the same value. A parameter specifies the culture,
        /// case, and sort rules used in the comparison.
        /// </summary>
        /// <param name="value">The <see cref="string"/> to compare to this
        /// instance.</param>
        /// <param name="comparisonType">One of the enumeration values that 
        /// specifies how the strings will be compared.</param>
        /// <returns><c>true</c> if the value of the value parameter is the
        /// same as this <see cref="StringView"/>; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="ArgumentException">comparisonType is not
        /// a <see cref="System.StringComparison"/> value.</exception>
        public bool Equals(string value, StringComparison comparisonType)
        {
            if (value.Length == Length)
                return String.Substring(Position, Length).Equals(value, comparisonType);
            return false;
        }

        /// <summary>
        /// Determines whether the string represented by this 
        /// <see cref="StringView"/> and a specified <see cref="string"/>
        /// object have the same value.
        /// </summary>
        /// <param name="value">The <see cref="string"/> to compare to this
        /// instance.</param>
        /// <returns><c>true</c> if the value of the value parameter is the
        /// same as this <see cref="StringView"/>; otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(string value)
        {
            if (value.Length == Length)
            {
                for (int i = 0, j = Position; i < Length; i++, j++)
                    if (value[i] != String[j])
                        return false;
                return true;
            }
            return false;
        }

        public override bool Equals(object obj)
        {
            if (obj is StringView other)
                return Equals(other);
            return false;
        }

        /// <summary>
        /// Indicates whether this <see cref="StringView"/> object is equal in
        /// value to specified <see cref="StringView"/> object.
        /// </summary>
        /// <param name="other">The other <see cref="StringView"/> object.</param>
        /// <param name="comparer">A <see cref="StringViewComparer"/> to compare
        /// the two objects.</param>
        /// <returns><c>true</c> if both objects are equal in value; 
        /// otherwise <c>false</c>.</returns>
        public bool Equals(StringView other, StringViewComparer comparer) 
            => comparer.Equals(this, other);

        /// <summary>
        /// Trim the <see cref="StringView"/> object of the first occurence
        /// of the specific character if it is the leading or trailing
        /// one to occur.
        /// </summary>
        /// <param name="c">The character to trim from the view.</param>
        /// <returns>A copy of the <see cref="StringView"/> trimmed of the 
        /// leading or trailing specified character.</returns>
        public StringView TrimOnce(char c)
        {
            if (Length > 0)
            {
                int trimStart = 0, trimEnd = 0;
                if (String[Position] == c)
                    trimStart++;
                if (String[Position + Length - 1] == c)
                    trimEnd++;
                return new StringView(String, Position + trimStart, Length - trimEnd - trimStart);
            }
            return this;
        }

        public int CompareTo(StringView value)
            => CompareViews(this, value, true);

        /// <summary>
        /// Compares this instance with a specified <see cref="System.Object"/> and indicates 
        /// whether this instance precedes, follows, or appears in the same position in the 
        /// sort order as the specified <see cref="System.Object"/>.
        /// </summary>
        /// <param name="value">An object that evaluates to <see cref="StringView"/>.</param>
        /// <returns>A 32-bit signed integer that indicates whether this instance precedes, 
        /// follows, or appears in the same position in the sort order as the value parameter.
        /// Value Condition Less than zero This instance precedes value. Zero This instance 
        /// has the same position in the sort order as value. Greater than zero This instance
        /// follows value. -or- value is null.</returns>
        public int CompareTo(StringView value, StringViewComparer comparer)
            => comparer.Compare(this, value);

        public IEnumerator<char> GetEnumerator() => this;

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        
        public bool MoveNext() => ++_enumerator + Position < _endIndex;
        
        public void Reset() { }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            _enumerator = -1;
        }

        #endregion

        #region Properties

        /// <summary>
        /// The <see cref="StringView"/>'s position in its string.
        /// </summary>
        public int Position { get; }

        /// <summary>
        /// The <see cref="StringView"/>'s length in its string.
        /// </summary>
        public int Length { get; }

        /// <summary>
        /// The <see cref="string"/> object for the view.
        /// </summary>
        public string String { get; }

        public char Current => 
            _enumerator >= 0 && _enumerator + Position < _endIndex ? String[Position + _enumerator] : '\0';

        object IEnumerator.Current => Current;

        #endregion
    }

    /// <summary>
    /// Represents a <see cref="StringView"/> comparison operation that usees specific
    /// case comparison rules.
    /// </summary>
    public abstract class StringViewComparer : IEqualityComparer<StringView>, IComparer<StringView>
    {
        #region Subclasses

        class IgnoreCaseComparer : StringViewComparer
        {
            public override int Compare(StringView x, StringView y)
                => StringView.CompareViews(x, y, false);

            public override bool Equals(StringView x, StringView y)
                => StringView.ViewsEqual(x, y, false);

            public override int GetHashCode(StringView obj)
                => obj.GetHashCode(true);

            public static IgnoreCaseComparer Instance = new IgnoreCaseComparer();
        }

        class CaseSensitiveComparer : StringViewComparer
        {
            public override int Compare(StringView x, StringView y)
                => StringView.CompareViews(x, y, true);

            public override bool Equals(StringView x, StringView y)
                => StringView.ViewsEqual(x, y, true);

            public override int GetHashCode(StringView obj)
                => obj.GetHashCode();

            public static CaseSensitiveComparer Instance = new CaseSensitiveComparer();
        }

        #endregion

        #region Methods

        public abstract int Compare(StringView x, StringView y);

        public abstract bool Equals(StringView x, StringView y);

        public abstract int GetHashCode(StringView obj);

        #endregion

        #region Properties

        /// <summary>
        /// Gets a <see cref="StringViewComparer"/> object that performs a case-
        /// insensitive ordinal string comparison.
        /// </summary>
        public static StringViewComparer IgnoreCase => IgnoreCaseComparer.Instance;

        /// <summary>
        /// Gets a <see cref="StringViewComparer"/> object that performs a case-sensitive
        /// ordinal string comparison.
        /// </summary>
        public static StringViewComparer Ordinal => CaseSensitiveComparer.Instance;

        #endregion
    }

    public static class StringView_Extensions
    {
        /// <summary>
        /// Determines whether the string represented by this 
        /// <see cref="string"/> and a specified <see cref="StringView"/>
        /// object have the same value. A parameter specifies the culture,
        /// case, and sort rules used in the comparison.
        /// </summary>
        /// <param name="str">This string.</param>
        /// <param name="value">The <see cref="StringView"/> to compare to this
        /// instance.</param>
        /// <param name="comparisonType">One of the enumeration values that 
        /// specifies how the strings will be compared.</param>
        /// <returns><c>true</c> if the value of the value parameter is the
        /// same as this <see cref="StringView"/>; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="ArgumentException">comparisonType is not
        /// a <see cref="System.StringComparison"/> value.</exception>
        public static bool Equals(this string str, StringView value, StringComparison comparisonType)
            => value.Equals(str, comparisonType);
    }
}
