//-----------------------------------------------------------------------------
// Copyright (c) 2017, 2018 informedcitizenry <informedcitizenry@gmail.com>
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
        /// current <see cref="T:System.Text.StringBuilder"/> object.
        /// </summary>
        /// <param name="sb">This <see cref="T:System.Text.StringBuilder"/>.</param>
        /// <returns>The trimmed <see cref="T:System.Text.StringBuilder"/>.</returns>
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
        /// current <see cref="T:System.Text.StringBuilder"/> object.
        /// </summary>
        /// <param name="sb">This <see cref="T:System.Text.StringBuilder"/>.</param>
        /// <returns>The trimmed <see cref="T:System.Text.StringBuilder"/>.</returns>
        public static StringBuilder TrimStart(this StringBuilder sb)
        {
            if (sb == null || sb.Length == 0) return sb;

            while (sb.Length > 0 && char.IsWhiteSpace(sb[0]))
                sb.Remove(0, 1);

            return sb;
        }

        /// <summary>
        /// Removes all leading and trailing occurrences of white spaces from the
        /// current <see cref="T:System.Text.StringBuilder"/> object.
        /// </summary>
        /// <param name="sb">This <see cref="T:System.Text.StringBuilder"/>.</param>
        /// <returns>The trimmed <see cref="T:System.Text.StringBuilder"/>.</returns>
        public static StringBuilder Trim(this StringBuilder sb) => sb.TrimStart().TrimEnd();
    }

    public static class StringExtensions
    {
        /// <summary>
        /// String to split by into substrings by length.
        /// </summary>
        /// <param name="str">The string to split.</param>
        /// <param name="maxLength">The maximum length per sub-string. "Carry-over" 
        /// substrings after split will be their own string.</param>
        /// <returns>An <see cref="T:System.Collections.Generic.IEnumerable&lt;string&gt;"/> class.</returns>
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
        /// <returns><c>True</c> if string is fully enclosed in quotes, otherwise <c>false</c>.</returns>
        public static bool EnclosedInQuotes(this string str)
        {
            if (string.IsNullOrEmpty(str) || string.IsNullOrWhiteSpace(str))
                return false;
            bool enclosed = false;
            if (str.StartsWith("\"", StringComparison.CurrentCulture) &&
                str.EndsWith("\"", StringComparison.CurrentCulture))
            {
                bool escaped = false;
                var substr = str.Substring(1);
                for (int i = 0; i < substr.Length; i++)
                {
                    if (substr[i] == '"' && !escaped) {
                        enclosed = !enclosed;
                        if (enclosed == false)
                            break; // catches the ""hello, world"" false positive
                    }
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
                return str.Length > 1 ? str.Substring(0, str.Length - 1) : string.Empty;
            return str;
        }

        /// <summary>
        /// Trims one instance of the specified character at the start and the end of the string.
        /// </summary>
        /// <returns>The modified string.</returns>
        /// <param name="str">String.</param>
        /// <param name="c">The character to trim.</param>
        public static string TrimOnce(this string str, char c)
        {
            return str.TrimStartOnce(c).TrimEndOnce(c);
        }

        /// <summary>
        /// Capture and return the first parenthetical group in the string. 
        /// </summary>
        /// <param name="str">The string to evaluate</param>
        /// <returns>The first instance of a parenthetical group</returns>
        /// <exception cref="T:System.FormatException">System.FormatException</exception>
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

        /// <summary>
        /// Does a comma-separated-value analysis on the <see cref="T:DotNetAsm.SourceLine"/>'s operand
        /// and returns the individual value as a <see cref="T:System.Collections.Generic.List&lt;string&gt;"/>.
        /// </summary>
        /// <param name="str">The string to evaluate</param>
        /// <returns>A <see cref="T:System.Collections.Generic.List&lt;string&gt;"/> of the values.</returns>
        public static List<string> CommaSeparate(this string str)
        {
            var csv = new List<string>();

            if (string.IsNullOrEmpty(str))
                return csv;

            bool double_enclosed = false;
            bool single_enclosed = false;
            bool paren_enclosed = false;

            var sb = new StringBuilder();

            int charExpCount = 0;

            for (int i = 0; i < str.Length; i++)
            {
                char c = str[i];
                if (double_enclosed)
                {
                    sb.Append(c);
                    if (c == '"')
                    {
                        double_enclosed = false;
                        if (i == str.Length - 1)
                            csv.Add(sb.ToString().Trim());
                    }
                }
                else if (single_enclosed)
                {
                    charExpCount++;
                    sb.Append(c);
                    if (c == '\'')
                    {
                        if (charExpCount == 2)
                        {
                            // don't set single quote closed unless we 
                            // have consumed the char in single quote.
                            charExpCount = 0;
                            single_enclosed = false;
                            if (i == str.Length - 1)
                                csv.Add(sb.ToString().Trim());
                        }
                    }
                }
                else if (paren_enclosed)
                {
                    if (c == '"' && !single_enclosed)
                        double_enclosed = true;
                    else
                        single_enclosed |= (c == '\'' && !double_enclosed);

                    sb.Append(c);
                    if (c == ')' && !double_enclosed && !single_enclosed)
                    {
                        paren_enclosed = false;
                        if (i == str.Length - 1)
                            csv.Add(sb.ToString().Trim());
                    }
                }
                else
                {
                    if (c == '"')
                    {
                        double_enclosed = true;
                    }
                    else if (c == '\'')
                    {
                        single_enclosed = true;
                    }
                    else if (c == '(')
                    {
                        paren_enclosed = true;
                    }
                    else if (c == ',')
                    {
                        csv.Add(sb.ToString().Trim());
                        sb.Clear();
                        continue;
                    }

                    sb.Append(c);
                    if (i == str.Length - 1)
                        csv.Add(sb.ToString().Trim());
                }
            }
            if (double_enclosed || single_enclosed)
                throw new Exception(ErrorStrings.QuoteStringNotEnclosed);

            if (str.Last().Equals(','))
                csv.Add(string.Empty);
            return csv;
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
    }
}
