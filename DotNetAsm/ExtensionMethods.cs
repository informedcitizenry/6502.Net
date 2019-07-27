//-----------------------------------------------------------------------------
// Copyright (c) 2017-2019 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

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

            var i = sb.Length - 1;
            for (; i >= 0; i--)
            {
                if (!char.IsWhiteSpace(sb[i]))
                    break;
            }

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
        public enum EnclosureType
        {
            Quote = 0,
            SingleQuote,
            Parenthesis,
            BracketParenthesis
        }

        private static readonly string OPEN_PARENS = "([";
        private static readonly string CLOSE_PARENS = ")]";

        /// <summary>
        /// String to split by into substrings by length.
        /// </summary>
        /// <param name="str">The string to split.</param>
        /// <param name="maxLength">The maximum length per sub-string. "Carry-over" 
        /// substrings after split will be their own string.</param>
        /// <returns>An <see cref="T:System.Collections.Generic.IEnumerable&lt;string&gt;"/> class.</returns>
        public static IEnumerable<string> SplitByLength(this string str, int maxLength)
        {
            var index = 0;
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
        /// <param name="result">The resulting quoted string if the entire string is enclosed in quotes.</param>
        /// <returns><c>True</c> if string is fully enclosed in quotes, otherwise <c>false</c>.</returns>
        public static bool EnclosedInQuotes(this string str, out string result)
        {
            result = str.GetNextQuotedString(0, true);
            return !string.IsNullOrEmpty(result) && str.Substring(1, str.Length - 2).Equals(result);
        }

        /// <summary>
        /// Tests whether the string is enclosed in double quotes.
        /// </summary>
        /// <param name="str">The string to evaluate.</param>
        /// <returns><c>True</c> if string is fully enclosed in quotes, otherwise <c>false</c>.</returns>
        public static bool EnclosedInQuotes(this string str)
        {
            var lastStrIx = str.Length - 1;
            if (lastStrIx >= 1 && (str[0] == '\'' || str[0] == '"') && str[0] == str[lastStrIx])
            {
                var closure = str[0];
                for (var i = 1; i <= lastStrIx; i++)
                {
                    var c = str[i];
                    if (c == closure)
                        return i == lastStrIx;
                    else if (c == '\\')
                        i++;
                }
            }
            return false;
        }

        /// <summary>
        /// Retrieve a substring within the string enclosed in the specified enclosure type, such as
        /// brackets, or double-quotes
        /// </summary>
        /// <param name="str">This string.</param>
        /// <param name="type">The <see cref="{DotNetAsm.StringExtensions.EnclosureType}"/>.</param>
        /// <param name="allowNested">Allow the enclosure to contain nested enclosures.</param>
        /// <param name="includeClosure">Include the closure in the resulting substring.</param>
        /// <param name="allowEscape">Allow the substring to escape the enclosure, so it will not be
        /// evaluated as an enclosure.</param>
        /// <param name="doNotUnescape">Do not unescape the escaped enclosure in the final substring.</param>
        /// <returns>The substring containing the enclosure, or an empty string if no enclosure is
        /// found in the string.</returns>
        /// <exception cref="{System.Exception"}/>
        public static string GetEnclosure(this string str, EnclosureType type, bool allowNested, bool includeClosure, bool allowEscape, bool doNotUnescape = true)
        {
            var closureIx = -1;
            var nested = 0;
            string open = string.Empty, close = string.Empty, errorString = ErrorStrings.QuoteStringNotEnclosed;
            switch (type)
            {
                case EnclosureType.Quote:
                    open = "\"'";
                    close = "\"'";
                    break;
                case EnclosureType.SingleQuote:
                    open = "'";
                    close = "'";
                    break;
                case EnclosureType.Parenthesis:
                    open = OPEN_PARENS;
                    close = CLOSE_PARENS;
                    errorString = ErrorStrings.None;
                    break;
                case EnclosureType.BracketParenthesis:
                    open = "[";
                    close = "]";
                    errorString = ErrorStrings.None;
                    break;
            }
            var enclosureBuilder = new StringBuilder();
            for (var i = 0; i < str.Length; i++)
            {
                var c = str[i];
                if (closureIx > -1)
                {
                    if (allowEscape && c == '\\')
                    {
                        var escLen = 2;
                        if (str[i + 1] == 'u')
                            escLen = 6;
                        else if (str[i + 1] == 'x')
                            escLen = 4;
                        if (i >= str.Length - escLen)
                            throw new Exception(errorString);

                        if (doNotUnescape)
                            enclosureBuilder.Append(str.Substring(i, escLen));
                        else
                            enclosureBuilder.Append(Regex.Unescape(str.Substring(i, escLen)));
                        i += escLen - 1;
                    }
                    else if (type != EnclosureType.Quote && type != EnclosureType.SingleQuote && (c == '\'' || c == '"'))
                    {
                        var quoted = str.Substring(i).GetEnclosure(type: EnclosureType.Quote,
                                                      allowNested: false,
                                                      includeClosure: true,
                                                      allowEscape: true,
                                                      doNotUnescape: true);
                        enclosureBuilder.Append(quoted);
                        i += quoted.Length - 1;
                    }
                    else if (c == open[closureIx] && allowNested)
                    {
                        nested++;
                        enclosureBuilder.Append(c);
                    }
                    else
                    {
                        if (c == close[closureIx])
                        {
                            if (includeClosure)
                                enclosureBuilder.Append(c);
                            if (allowNested)
                                nested--;
                            if (nested == 0)
                                return enclosureBuilder.ToString();
                        }
                        else
                        {
                            enclosureBuilder.Append(c);
                        }
                    }
                }
                else
                {
                    closureIx = open.IndexOf(c);
                    if (closureIx > -1)
                    {
                        if (includeClosure)
                            enclosureBuilder.Append(c);
                        if (allowNested)
                            nested++;
                    }
                }
            }
            if (closureIx > -1 || nested > 0)
                throw new Exception(errorString);
            return string.Empty;
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
        public static string TrimOnce(this string str, char c) => str.TrimStartOnce(c).TrimEndOnce(c);

        /// <summary>
        /// Gets the next parenthetical group in the string.
        /// </summary>
        /// <param name="str">The string to evaluate.</param>
        /// <returns>The first instance of a parenthetical group, or the whole string.</returns>
        /// <exception cref="T:System.FormatException"></exception>
        public static string GetNextParenEnclosure(this string str) =>
            str.GetEnclosure(type: EnclosureType.Parenthesis, allowNested: true, includeClosure: true, allowEscape: false);

        /// <summary>
        /// Gets the next parenthetical group in the string.
        /// </summary>
        /// <returns>The first instance of a parenthetical group, or the whole string.</returns>
        /// <param name="str">The string to evaluate.</param>
        /// <param name="atIndex">The index at which to search the string.</param>
        /// <exception cref="T:System.FormatException"></exception>
        public static string GetNextParenEnclosure(this string str, int atIndex) =>
            str.Substring(atIndex).GetNextParenEnclosure();

        /// <summary>
        /// Gets the next double- or single-quoted string within the string.
        /// </summary>
        /// <returns>The next quoted string, or empty if no quoted string present.</returns>
        /// <param name="str">String.</param>
        /// <exception cref="T:System.Exception"></exception>
        public static string GetNextQuotedString(this string str) => str.GetNextQuotedString(0);

        /// <summary>
        /// Gets the next double- or single-quoted string within the string.
        /// </summary>
        /// <returns>The next quoted string, or empty if no quoted string present.</returns>
        /// <param name="str">String.</param>
        /// <param name="atIndex">The index at which to search the string.</param>
        /// <exception cref="T:System.Exception"></exception>
        public static string GetNextQuotedString(this string str, int atIndex)
            => str.GetNextQuotedString(atIndex, false);

        /// <summary>
        /// Gets the next double- or single-quoted string within the string.
        /// </summary>
        /// <returns>The next quoted string, or empty if no quoted string present.</returns>
        /// <param name="str">String.</param>
        /// <param name="doNotUnescape">If true, do not unescape the string.</param>
        /// <exception cref="T:System.Exception"></exception>
        public static string GetNextQuotedString(this string str, bool doNotUnescape)
            => str.GetNextQuotedString(0, doNotUnescape);

        /// <summary>
        /// Gets the next double- or single-quoted string within the string.
        /// </summary>
        /// <returns>The next quoted string, or empty if no quoted string present.</returns>
        /// <param name="str">String.</param>
        /// <param name="atIndex">The index at which to search the string.</param>
        /// <param name="doNotUnescape">If true, do not unescape the string.</param>
        /// <exception cref="T:System.Exception"></exception>
        public static string GetNextQuotedString(this string str, int atIndex, bool doNotUnescape)
        {
            return str.Substring(atIndex).GetEnclosure(type: EnclosureType.Quote,
                                                       allowNested: false,
                                                       includeClosure: false,
                                                       allowEscape: true,
                                                       doNotUnescape: doNotUnescape);
        }

        /// <summary>
        /// Does a comma-separated-value analysis on the <see cref="T:DotNetAsm.SourceLine"/>'s operand
        /// and returns the individual value as a <see cref="T:System.Collections.Generic.List&lt;string&gt;"/>.
        /// </summary>
        /// <param name="str">The string to evaluate.</param>
        /// <returns>A <see cref="{System.Collections.Generic.List&lt;string&gt;}"/> of the values.</returns>
        /// <exception cref="T:System.Exception"></exception>
        public static List<string> CommaSeparate(this string str)
        {
            var csv = new List<string>();

            if (string.IsNullOrEmpty(str))
                return csv;

            if (!str.Contains(","))
                return new List<string> { str };

            var sb = new StringBuilder();

            for (var i = 0; i < str.Length; i++)
            {
                var c = str[i];
                if (c == '\'' || c == '"')
                {
                    var quoted = str.GetNextQuotedString(atIndex: i, doNotUnescape: true);
                    var quotedEndIx = quoted.Length + 2;
                    sb.Append(str.Substring(i, quotedEndIx));
                    i += quotedEndIx - 1;
                }
                else if (c == '(' || c == '[')
                {
                    var parenClosure = str.GetNextParenEnclosure(atIndex: i);
                    sb.Append(parenClosure);
                    i += parenClosure.Length - 1;
                }
                else if (c == ',')
                {
                    csv.Add(sb.ToString().Trim());
                    sb.Clear();
                }
                else
                {
                    sb.Append(c);
                }
            }
            if (sb.Length > 0)
                csv.Add(sb.ToString().Trim());
            if (str.Last().Equals(','))
                csv.Add(string.Empty);
            return csv;
        }
    }

    public static class Char_Extension
    {
        /// <summary>
        /// Indicates whether the specified Unicode character is a mathematical
        /// operator.
        /// </summary>
        /// <returns><c>true</c>, if the character is an operator, <c>false</c> otherwise.</returns>
        /// <param name="c">The Unicode character.</param>
        public static bool IsOperator(this char c) => (char.IsSymbol(c) && !c.IsRadixOperator()) || c == '/' || c == '*' || c == '-' || c == '&' || c == '%' || c == '!';

        /// <summary>
        /// Indicates whether the specified Unicode character is a radix operator.
        /// </summary>
        /// <returns><c>true</c>, if the character is a radix operator, <c>false</c> otherwise.</returns>
        /// <param name="c">The Unicode character.</param>
        public static bool IsRadixOperator(this char c) => c == '$' || c == '%';
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
        /// Returns a value indicating whether this double is almost equal with great
        /// precision to a specified <see cref="double"/>. 
        /// </summary>
        /// <returns><c>true</c>, if the two values are almost equal, 
        /// <c>false</c> otherwise.</returns>
        /// <param name="d1">This double.</param>
        /// <param name="obj">A double-precision floating point object.</param>
        public static bool AlmostEquals(this double d1, double obj) =>
            Math.Abs(d1 - obj) <= Math.Max(Math.Abs(d1), Math.Abs(obj)) * 1E-15;
    }
}
