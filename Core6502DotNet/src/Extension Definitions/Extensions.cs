//-----------------------------------------------------------------------------
// Copyright (c) 2017-2021 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Core6502DotNet
{
    public static class String_Extension
    {
        /// <summary>
        /// Returns an elliptical representation of the string if it 
        /// exceeds the specified length.
        /// </summary>
        /// <param name="s">This string.</param>
        /// <param name="length">The string length before it the ellipsis.</param>
        /// <returns>The modified string.</returns>
        public static string Elliptical(this string s, int length)
        {
            if (s.Length > length)
                return s.Substring(0, length - 3) + "...";
            return s;
        }

        /// <summary>
        /// Tests whether the string is enclosed in double quotes.
        /// </summary>
        /// <param name="s">The string to evaluate.</param>
        /// <returns><c>true</c> if string is fully enclosed in double quotes, otherwise <c>false</c>.</returns>
        public static bool EnclosedInDoubleQuotes(this string s)
        {
            if (s.Length < 3 || s[0] != '"' || s[^1] != '"')
                return false;
            var penult = s.Length - 2;
            if (penult > 0 && s[^2] == '\\')
            {
                var count = 1;
                for (var i = penult; i > 1 && s[^i] == '\\'; i--)
                    count++;
                return count % 2 == 0;
            }
            return true;
        }

        /// <summary>
        /// Trims one instance of the specified character at the start of the string.
        /// </summary>
        /// <returns>The modified string.</returns>
        /// <param name="str">String.</param>
        /// <param name="c">The character to trim.</param>
        public static string TrimStartOnce(this string str, char c)
        {
            if (string.IsNullOrEmpty(str)) return string.Empty;
            if (str.First().Equals(c))
                return str.Length > 1 ? str.Substring(1) : string.Empty;
            return str;
        }

        /// <summary>
        /// Trims one instance of the specified character at the end of the string.
        /// </summary>
        /// <returns>The modified string.</returns>
        /// <param name="str">String.</param>
        /// <param name="c">The character to trim.</param>
        public static string TrimEndOnce(this string str, char c)
        {
            if (string.IsNullOrEmpty(str)) return string.Empty;
            if (str.Last().Equals(c))
                return str.Length > 1 ? str[0..^1] : string.Empty;
            return str;
        }

        /// <summary>
        /// Trims one instance of the specified character at the start and the end of the string.
        /// </summary>
        /// <returns>The modified string.</returns>
        /// <param name="str">String.</param>
        /// <param name="c">The character to trim.</param>
        public static string TrimOnce(this string str, char c) => str.TrimStartOnce(c).TrimEndOnce(c);
    }

    public static class Char_Extension
    {
        /// <summary>
        /// Indicates whether the specified Unicode character could be an operand or 
        /// mathematical operator per usage in an expression.
        /// </summary>
        /// <param name="c">The Unicode character.</param>
        /// <returns><c>true</c>, if the character is of this type, <c>false</c> otherwise.</returns>
        public static bool IsSpecialOperator(this char c) => c == '*' || c == '+' || c == '-';

        /// <summary>
        /// Indicates whether the specified Unicode character is a group operator
        /// </summary>
        /// <param name="c">The Unicode character.</param>
        /// <returns><c>true</c>, if the character is a group operator, <c>false</c> otherwise.</returns>
        public static bool IsOpenOperator(this char c) => char.GetUnicodeCategory(c) == UnicodeCategory.OpenPunctuation;

        /// <summary>
        /// Indicates whether the specified Unicode character is a group enclosing operator
        /// </summary>
        /// <param name="c">The Unicode character.</param>
        /// <returns><c>true</c>, if the character is a group enclosing operator, <c>false</c> otherwise.</returns>
        public static bool IsClosedOperator(this char c) => char.GetUnicodeCategory(c) == UnicodeCategory.ClosePunctuation;

        /// <summary>
        /// Indicates whether the specified Unicode character is a unary operator
        /// </summary>
        /// <param name="c">The Unicode character.</param>
        /// <returns><c>true</c>, if the character is a unary operator, <c>false</c> otherwise.</returns>
        public static bool IsUnaryOperator(this char c) => c == '!' || c == '$' || c == '%' || c == '&' || c == '+' || 
                                                           c == '-' || c == '<' || c == '>' || c == '^' || c == '`' || c == '~';

        /// <summary>
        /// Indicates whether the specified Unicode character is a hex operator
        /// </summary>
        /// <param name="c">The Unicode character.</param>
        /// <returns><c>true</c>, if the character is a hex, <c>false</c> otherwise.</returns>
        public static bool IsHex(this char c) => Uri.IsHexDigit(c);

        /// <summary>
        /// Indicates whether the specified Unicode character is a radix operator.
        /// </summary>
        /// <param name="c">The Unicode character.</param>
        /// <returns><c>true</c>, if the character is a radix operator, <c>false</c> otherwise.</returns>
        public static bool IsRadixOperator(this char c) => c == '$' || c == '%';

        /// <summary>
        /// Indicates whether the specified Unicode character is a 6502.Net operator.
        /// </summary>
        /// <param name="c">The unicode character.</param>
        /// <returns><c>true</c>, if the character is an operator, <c>false</c> otherwise.</returns>
        public static bool IsOperator(this char c) => c == '!' || c == '$' || c == '%' || c == '&' ||
                  c == '(' || c == ')' || c == '*' || c == '+' || c == ',' || c == '-' || c == '/' || 
                  c == ':' || c == '<' || c == '=' || c == '>' || c == '[' || c == ']' || c == '^' || 
                  c == '`' || c == '{' || c == '|' || c == '}' || c == '~';
    }

    public static class Int32_Extension
    {
        /// <summary>
        /// The minimum size required in bytes to store this value.
        /// </summary>
        /// <param name="value">The value to store.</param>
        /// <returns>The size in bytes.</returns>
        public static int Size(this int value)
        {
            if (value > UInt24.MaxValue || value < Int24.MinValue) return 4;
            if (value > ushort.MaxValue || value < short.MinValue) return 3;
            if (value > byte.MaxValue   || value < sbyte.MinValue) return 2;
            return 1;
        }

        /// <summary>
        /// The minimum size required in bytes to store this value.
        /// </summary>
        /// <param name="value">The value to store.</param>
        /// <returns>The size in bytes.</returns>
        public static int Size(this uint value)
        {
            if (value > UInt24.MaxValue) return 4;
            if (value > ushort.MaxValue) return 3;
            if (value > byte.MaxValue)   return 2;
            return 1;
        }
    }

    public static class Int64_Extension
    {
        /// <summary>
        /// The minimum size required in bytes to store this value.
        /// </summary>
        /// <param name="value">The value to store.</param>
        /// <returns>The size in bytes.</returns>
        public static int Size(this long value)
        {
            if (value < 0)
                value = (~value) << 1;

            if ((value & 0xFFFFFF00) == 0) return 1;
            if ((value & 0xFFFF0000) == 0) return 2;
            if ((value & 0xFF000000) == 0) return 3;
            return 4;
        }
    }

    public static class Double_Extension
    {
        /// <summary>
        /// The minimum size required in bytes to store this value.
        /// </summary>
        /// <param name="value">The value to store.</param>
        /// <returns>The size in bytes.</returns>
        public static int Size(this double value)
        {
            if (value > UInt24.MaxValue || value < Int24.MinValue) return 4;
            if (value > ushort.MaxValue || value < short.MinValue) return 3;
            if (value > byte.MaxValue   || value < sbyte.MinValue) return 2;
            return 1;
        }

        /// <summary>
        /// Returns a value indicating whether this double is almost equal with great
        /// precision to a specified <see cref="double"/>. 
        /// </summary>
        /// <returns><c>true</c>, if the two values are almost equal, 
        /// <c>false</c> otherwise.</returns>
        /// <param name="d1">This double.</param>
        /// <param name="obj">A double-precision floating point object.</param>
        public static bool AlmostEquals(this double d1, double obj) =>
            Math.Abs(d1 - obj) <= Math.Max(Math.Abs(d1), Math.Abs(obj)) * 1E-15;

        /// <summary>
        /// Returns a value indicating whether this double is an integer.
        /// </summary>
        /// <param name="d1">This double.</param>
        /// <returns><c>true</c>, if the double is (almost) equal to its integral equivalent,
        /// <c>false</c> otherwise. </returns>
        public static bool IsInteger(this double d1) =>
            d1.AlmostEquals(Math.Truncate(d1));
    }

    public static class IEnumerableT_Extension
    {
        /// <summary>
        /// Get the iterator for the <see cref="IEnumerable{T}"/> collection.
        /// </summary>
        /// <typeparam name="T">The type.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <returns>A <see cref="RandomAccessIterator{T}"/> iterator for the collection.</returns>
        public static RandomAccessIterator<T> GetIterator<T>(this IEnumerable<T> collection)
            => new RandomAccessIterator<T>(collection);


        /// <summary>
        /// Get a hex string representation of the sequence of bytes in the collection.
        /// </summary>
        /// <param name="byteCollection">The byte collection.</param>
        /// <param name="pc">The initial Program Counter.</param>
        /// <returns>A string representation of the byte collection.</returns>
        public static string ToString(this IEnumerable<byte> byteCollection, int pc)
            => ToString(byteCollection, pc, '>', true);

        /// <summary>
        /// Get a hex string representation of the sequence of bytes in the collection.
        /// </summary>
        /// <param name="byteCollection">The byte collection.</param>
        /// <param name="pc">The initial Program Counter.</param>
        /// <param name="startChar">The starting character.</param>
        /// <returns>A string representation of the byte collection.</returns>
        public static string ToString(this IEnumerable<byte> byteCollection, int pc, char startChar)
            => ToString(byteCollection, pc, startChar, true);

        /// <summary>
        /// Get a hex string representation of the sequence of bytes in the collection.
        /// </summary>
        /// <param name="byteCollection">The byte collection.</param>
        /// <param name="pc">The initial Program Counter.</param>
        /// <param name="startChar">The starting character.</param>
        /// <param name="appendStartPc">If true, the string will append the initial Program 
        /// Counter passed in the <paramref name="pc"/> parameter.</param>
        /// <returns>A string representation of the byte collection.</returns>
        public static string ToString(this IEnumerable<byte> byteCollection, int pc, char startChar, bool appendStartPc)
        {
            var sb = new StringBuilder();
            if (appendStartPc)
                sb.Append($"{startChar}{pc:x4}    ");
            var byteList = byteCollection.ToList();
            var rows = byteList.Count / 8;
            if (byteList.Count % 8 != 0)
                rows++;
            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    int offset = y * 8 + x;
                    if (offset >= byteList.Count)
                        break;
                    sb.Append($" {byteList[offset]:x2}");
                }
                if (y < rows - 1)
                {
                    pc += 8;
                    sb.AppendLine();
                    sb.Append($"{startChar}{pc:x4}    ");
                }
            }
            return sb.ToString();
        }
    }

    public static class NullableT_Extension
    {
        /// <summary>
        /// Determines if the <see cref="bool"/>? is not null and its value is true.
        /// </summary>
        /// <param name="value">The bool.</param>
        /// <returns><c>true</c> if the <see cref="bool"/>? is not null and 
        /// its value is true, otherwise <c>false</c>.</returns>
        public static bool IsTrue(this bool? value)
                => value.HasValue && value.Value == true;

        /// <summary>
        /// Determines if the <see cref="int"/>? is not null and its value is greater
        /// than the compared integer value.
        /// </summary>
        /// <param name="d">This value.</param>
        /// <param name="value">The compared value.</param>
        /// <returns><c>true</c> if the <see cref="int"/>? is not null and
        /// its value is greater than the compared value, <c>false</c> otherwise.</returns>
        public static bool GreaterThan(this int? d, int value)
            => d.HasValue && d.Value > value;

        /// <summary>
        /// Determines if the <see cref="int"/>? is not null and its value is greater
        /// than or equal to the compared integer value.
        /// </summary>
        /// <param name="d">This value.</param>
        /// <param name="value">The compared value.</param>
        /// <returns><c>true</c> if the <see cref="int"/>? is not null and
        /// its value is greater or equal to the compared value, <c>false</c> otherwise.</returns>
        public static bool GreaterThanOrEqual(this int? d, int value)
            => d.HasValue && d.Value >= value;

        /// <summary>
        /// Determines if the <see cref="int"/>? is not null and its value is less
        /// than or equal to the compared integer value.
        /// </summary>
        /// <param name="d">This value.</param>
        /// <param name="value">The compared value.</param>
        /// <returns><c>true</c> if the <see cref="int"/>? is not null and
        /// its value is less than or equal to the compared value, <c>false</c> otherwise.</returns>
        public static bool LessThanOrEqual(this int? d, int value)
            => d.HasValue && d.Value <= value;

        /// <summary>
        /// Determines if the <see cref="int"/>? is not null and its value is less
        /// than the compared integer value.
        /// </summary>
        /// <param name="d">This value.</param>
        /// <param name="value">The compared value.</param>
        /// <returns><c>true</c> if the <see cref="int"/>? is not null and
        /// its value is less than the compared value, <c>false</c> otherwise.</returns>
        public static bool LessThan(this int? d, int value)
            => d.HasValue && d.Value < value;

        /// <summary>
        /// Determines if the <see cref="double"/>? is not null and its value is greater
        /// than the compared double value.
        /// </summary>
        /// <param name="d">This value.</param>
        /// <param name="value">The compared value.</param>
        /// <returns><c>true</c> if the <see cref="double"/>? is not null and
        /// its value is greater than the compared value, <c>false</c> otherwise.</returns>
        public static bool GreaterThan(this double? d, double value)
            => d.HasValue && d.Value > value;

        /// <summary>
        /// Determines if the <see cref="double"/>? is not null and its value is greater
        /// than or equal to the compared double value.
        /// </summary>
        /// <param name="d">This value.</param>
        /// <param name="value">The compared value.</param>
        /// <returns><c>true</c> if the <see cref="double"/>? is not null and
        /// its value is greater than or equal to the compared value, <c>false</c> otherwise.</returns>
        public static bool GreaterThanOrEqual(this double? d, double value)
            => d.HasValue && d.Value >= value;

        /// <summary>
        /// Determines if the <see cref="double"/>? is not null and its value is less
        /// than the compared double value.
        /// </summary>
        /// <param name="d">This value.</param>
        /// <param name="value">The compared value.</param>
        /// <returns><c>true</c> if the <see cref="double"/>? is not null and
        /// its value is less than the compared value, <c>false</c> otherwise.</returns>
        public static bool LessThanOrEqual(this double? d, double value)
            => d.HasValue && d.Value <= value;

        /// <summary>
        /// Determines if the <see cref="double"/>? is not null and its value is less
        /// than the compared double value.
        /// </summary>
        /// <param name="d">This value.</param>
        /// <param name="value">The compared value.</param>
        /// <returns><c>true</c> if the <see cref="int"/>? is not null and
        /// its value is greater than the compared value, <c>false</c> otherwise.</returns>
        public static bool LessThan(this double? d, double value)
            => d.HasValue && d.Value < value;

        /// <summary>
        /// Determines if the <see cref="long"/>? is not null and its value is less
        /// than or equal to the compared long value.
        /// </summary>
        /// <param name="d">This value.</param>
        /// <param name="value">The compared value.</param>
        /// <returns><c>true</c> if the <see cref="long"/>? is not null and
        /// its value is less than or equal to the compared value, <c>false</c> otherwise.</returns>
        public static bool LessThanOrEqual(this long? d, long value)
            => d.HasValue && d.Value <= value;
    }
}
