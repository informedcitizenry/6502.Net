//-----------------------------------------------------------------------------
// Copyright (c) 2017 informedcitizenry <informedcitizenry@gmail.com>
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to 
// deal in the Software without restriction, including without limitation the 
// rights to use, copy, modify, merge, publish, distribute, sublicense, and/or 
// sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in 
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS 
// IN THE SOFTWARE.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DotNetAsm
{
    public static class StringBuilderExtensions
    {
        /// <summary>
        /// Removes all trailing occurrences of white spaces from the
        /// current System.Text.StringBuilder object.
        /// </summary>
        /// <param name="sb">This StringBuilder.</param>
        /// <returns>The trimmed System.Text.StringBuilder.</returns>
        public static StringBuilder TrimEnd(this StringBuilder sb)
        {
            if (sb == null || sb.Length == 0) return sb;

            int i = sb.Length - 1;
            for (; i >= 0; i--)
                if (!char.IsWhiteSpace(sb[i]))
                    break;

            if (i < sb.Length - 1)
                sb.Length = i + 1;

            return sb;
        }

        /// <summary>
        /// Removes all leading occurrences of white spaces from the
        /// current System.Text.StringBuilder object.
        /// </summary>
        /// <param name="sb">This StringBuilder.</param>
        /// <returns>The trimmed System.Text.StringBuilder.</returns>
        public static StringBuilder TrimStart(this StringBuilder sb)
        {
            if (sb == null || sb.Length == 0) return sb;

            while (sb.Length > 0 && char.IsWhiteSpace(sb[0]))
                sb.Remove(0, 1);

            return sb;
        }

        /// <summary>
        /// Removes all leading and trailing occurrences of white spaces from the
        /// current System.Text.StringBuilder object.
        /// </summary>
        /// <param name="sb">This StringBuilder.</param>
        /// <returns>The trimmed System.Text.StringBuilder.</returns>
        public static StringBuilder Trim(this StringBuilder sb)
        {
            return sb.TrimStart().TrimEnd();
        }
    }

    public static class StringExtensions
    {
        /// <summary>
        /// String to split by into substrings by length.
        /// </summary>
        /// <param name="str">The string to split.</param>
        /// <param name="maxLength">The maximum length per sub-string. "Carry-over" 
        /// substrings after split will be their own string.</param>
        /// <returns>An IEnumerable&lt;string&gt; class.</returns>
        public static IEnumerable<string> SplitByLength(this string str, int maxLength)
        {
            int index = 0;
            while (index + maxLength < str.Length)
            {
                yield return str.Substring(index, maxLength);
                index += maxLength;
            }

            yield return str.Substring(index);
        }

        /// <summary>
        /// Tests whether the string is enclosed in double quotes.
        /// </summary>
        /// <param name="str">The string to evaluate.</param>
        /// <returns>True if string is fully enclosed in quotes, false otherwise.</returns>
        public static bool EnclosedInQuotes(this string str)
        {
            if (string.IsNullOrEmpty(str) || string.IsNullOrWhiteSpace(str))
                return false;
            bool enclosed = false;
            if (str.StartsWith("\"") && str.EndsWith("\""))
            {
                bool escaped = false;
                string substr = str.Substring(1);
                for (int i = 0; i < substr.Length; i++)
                {
                    if (substr[i] == '"' && !escaped)
                        enclosed = !enclosed;
                    else if (i < substr.Length - 1 && substr[i] == '\\' && substr[i + 1] == '"')
                    {
                        i++;
                        escaped = true;
                        continue;
                    }
                    escaped = false;
                }
            }
            return enclosed;
        }

        /// <summary>
        /// Capture and return the first parenthetical group in the string. 
        /// </summary>
        /// <param name="str">The string to evaluate</param>
        /// <returns>Returns the first instance of a parenthetical group</returns>
        /// <exception cref="System.FormatException">System.FormatException</exception>
        public static string FirstParenEnclosure(this string str)
        {
            int parens = 0;
            string parengroup = string.Empty;
            char open = '(', close = ')';
            bool quote_enclosed = false;
            foreach (var c in str)
            {
                if (parens >= 1)
                    parengroup += c.ToString();

                if (c == '"')
                {
                    quote_enclosed = !quote_enclosed;
                }
                if (quote_enclosed)
                    continue;

                if (c == open)
                {
                    if (parens == 0)
                        parengroup += c.ToString();
                    parens++;
                }
                else if (c == close)
                {
                    parens--;
                    if (parens == 0)
                    {
                        return parengroup;
                    }
                    if (parens < 0)
                        throw new FormatException();
                }
            }
            if (parens > 0)
                throw new FormatException();
            return str;
        }
    }

    public static class Int64_Extension
    {
        /// <summary>
        /// The minimum size required in bytes to store this value.
        /// </summary>
        /// <param name="value">The value to store.</param>
        /// <returns>The size in bytes.</returns>
        public static int Size(this Int64 value)
        {
            int size = 1;
            Int64 absval = Math.Abs(value) / 256;

            while (absval > 0)
            {
                size++;
                absval /= 256;
            }
            if ((size == 1 && value < sbyte.MinValue) ||
                (size == 2 && value < short.MinValue) ||
                (size == 3 && value < Int24.MinValue) ||
                (size == 4 && value < int.MinValue))
                size++;
            return size;
        }

        /// <summary>
        /// Gets the bitwise AND mask for the value.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>Returns the AND value as an unsigned 32-bit integer.</returns>
        public static uint AndMask(this Int64 value)
        {
            int size = value.Size() - 1;
            uint and = 0xFF;
            while (size > 0)
            {
                and *= 256;
                and += 0xFF;
                size--;
            }
            return and;
        }
    }

    public static class List_t_Extension
    {
        /// <summary>
        /// Returns all indexes of the given item in the list.
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <param name="list">The list in which to search the item.</param>
        /// <param name="item">The item to find.</param>
        /// <returns>A List&lt;int&gt; of all indexes the value is found in the list.</returns>
        public static List<int> AllIndexesOf<T>(this List<T> list, T item)
        {
            if (list == null || list.Count == 0)
                throw new ArgumentException("The list cannot be null or empty.");
            List<int> indexes = new List<int>();
            for (int index = 0; ; index++)
            {
                index = list.IndexOf(item, index);
                if (index == -1)
                    return indexes;
                indexes.Add(index);
            }
        }

        /// <summary>
        /// Returns all indexes of the given item in the list.
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <param name="list">The list in which to search the item.</param>
        /// <param name="expression">Filters the list of values based on a predicate.</param>
        /// <returns>A List&lt;int&gt; of all indexes the value is found in the list.</returns>
        public static List<int> AllIndexesOf<T>(this List<T> list, System.Func<T, bool> expression)
        {
            var any = list.Where(expression);
            List<int> indexes = new List<int>();
            foreach (var a in any)
            {
                indexes.AddRange(AllIndexesOf(list, a));
            }
            return indexes;
        }
    }
}
