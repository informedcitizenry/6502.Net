using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
namespace Sixty502DotNet
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
                return string.Concat(s.AsSpan(0, length - 3), "...");
            return s;
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
            if (str[0] == c)
                return str.Length > 1 ? str[1..] : string.Empty;
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
            if (str[^1] == c)
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
            if (value > byte.MaxValue || value < sbyte.MinValue) return 2;
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
            if (value > byte.MaxValue) return 2;
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
            if (value > byte.MaxValue || value < sbyte.MinValue) return 2;
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

    public static class ArrayT_Extension
    {
        /// <summary>
        /// Initialize the array with a default value.
        /// </summary>
        /// <typeparam name="T">The array type.</typeparam>
        /// <param name="arr">This array.</param>
        /// <param name="value">The default value to initialize the array to.</param>
        public static void InitializeTo<T>(this T[] arr, T value)
        {
            for (var i = 0; i < arr.Length; ++i)
            {
                arr[i] = value;
            }
        }
    }

    public static class IEnumerableT_Extension
    {
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

    public static class Sixty502DotNetParserStatContext_Extension
    {
        /// <summary>
        /// Get the source line string from the parsed statement context.
        /// </summary>
        /// <param name="stat">The parsed statement context.</param>
        /// <param name="wholeSource">Return the entire source encopassing the
        /// statement source, including extraneous newline characters.</param>
        /// <returns>The original line of source for the pasrsed statement.
        /// </returns>
        public static string GetSourceLine(this Sixty502DotNetParser.StatContext stat, bool wholeSource)
            => stat.GetSourceLine(null, wholeSource);

        /// <summary>
        /// Get the source line string from the parsed statement context.
        /// </summary>
        /// <param name="stat">The parsed statement context.</param>
        /// <param name="fromToken">Get the source from the token rather than
        /// the beginning of the source.</param>
        /// <param name="wholeSource">Return the entire source encopassing the
        /// statement source, including extraneous newline characters.</param>
        /// <returns>The original line of source for the pasrsed statement.
        /// </returns>
        public static string GetSourceLine(this Sixty502DotNetParser.StatContext stat, Token? fromToken, bool wholeSource)
        {
            var src = stat.Start.InputStream;
            var i = -1;
            int start;
            if (fromToken == null)
            {
                fromToken = stat.Start as Token;
                src.Seek(fromToken!.StartIndex);
                while (src.LA(i) != TokenConstants.EOF && src.LA(i) != '\n' && src.LA(i) != '\r' && src.LA(i) != ':')
                {
                    i--;
                }
                start = fromToken.StartIndex + i + 1;
            }
            else
            {
                start = fromToken.StartIndex;
            }
            src.Seek(stat.Stop.StopIndex);
            i = 1;
            while (src.LA(i) != TokenConstants.EOF && src.LA(i) != '\n' && src.LA(i) != '\r' && src.LA(i) != ':')
            {
                i++;
            }
            if (wholeSource)
            {
                while (src.LA(i) == '\n' || src.LA(i) == '\r')
                    i++;
                i--;
            }
            var stop = stat.Stop.StopIndex + i - 1;
            if (src.LA(i) != TokenConstants.EOF)
            {
                stop--;
            }
            if (start >= stop)
            {
                // this can happen sometimes if the stop index is from a different source file
                // from the start (for example in a macro exansion, so we just set the stop
                // back to the stop index
                stop = stat.Start.StopIndex;
            }
            var interval = new Interval(start, stop);
            if (wholeSource)
            {
                return src.GetText(interval);
            }
            return src.GetText(interval).Split(new char[] { '\r', '\n' })[0];
        }
    }
}
