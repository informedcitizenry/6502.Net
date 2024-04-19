//-----------------------------------------------------------------------------
// Copyright (c) 2017-2024 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using System.Text;

namespace Sixty502DotNet.Shared;

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

    public static int AsPositive(this int value)
    {
        return value.Size() switch
        {
            4 => value & 0x7fffffff,
            3 => value & 0xffffff,
            2 => value & 0xffff,
            _ => value & 0xff
        };
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

    /// <summary>
    /// Gets whether the integer represents a statement terminating character,
    /// such as a newline, colon or EOF.
    /// </summary>
    /// <param name="c">This integer.</param>
    /// <returns><c>true</c> if the integer value represents a terminating character,
    /// <c>false</c> otherwise.</returns>
    public static bool EndsStatement(this int c) => c == -1 || c == '\n' || c == ':' || c == '\r' || c == '\0';

    internal static bool IsOneOf(this int value, params int[] ints)
    {
        return ints.Contains(value);
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
        !double.IsNaN(d1) && d1.AlmostEquals(Math.Truncate(d1));
}

public static class Bool_Extension
{
    /// <summary>
    /// Converts the boolean value to a <see cref="StringComparer"/> value.
    /// </summary>
    /// <param name="value"></param>
    /// <returns>A <see cref="StringComparer.Ordinal"/> if <c>true</c>,
    /// otherwise <see cref="StringComparer.OrdinalIgnoreCase"/>.</returns>
    public static StringComparer ToStringComparer(this bool value)
    {
        return value ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
    }

    /// <summary>
    /// Converts the boolean value to a <see cref="StringComparison"/> value.
    /// </summary>
    /// <param name="value"></param>
    /// <returns>A <see cref="StringComparison.Ordinal"/> if <c>true</c>,
    /// otherwise <see cref="StringComparison.OrdinalIgnoreCase"/>.</returns>
    public static StringComparison ToStringComparison(this bool value)
    {
        return value ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
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
    /// Determines if the <see cref="double"/>? is not null and its value is less
    /// than the compared double value.
    /// </summary>
    /// <param name="d">This value.</param>
    /// <param name="value">The compared value.</param>
    /// <returns><c>true</c> if the <see cref="double"/>? is not null and
    /// its value is less than the compared value, <c>false</c> otherwise.</returns>
    public static bool LessThanOrEqual(this double? d, double value)
        => d.HasValue && d.Value <= value;
}

public static class Byte_Array_Extension
{
    public static byte[] Trimmed(this byte[] bytes)
    {
        int leading = bytes[^1];
        int beforeLeading;
        if (leading == 0 || leading == 255)
        {
            if (BitConverter.IsLittleEndian)
            {
                beforeLeading = bytes.ToList().FindLastIndex(b => b != leading);
            }
            else
            {
                beforeLeading = bytes.ToList().FindIndex(b => b != leading);
            }
        }
        else
        {
            beforeLeading = bytes.Length - 1;
        }
        if (beforeLeading < 0) beforeLeading++;
        return bytes.Take(beforeLeading + 1).ToArray();
    }
}

public static class IEnumerableT_Extension
{
    /// <summary>
    /// Represent the byte collection as a hex string.
    /// </summary>
    /// <param name="bytes">This byte collection.</param>
    /// <param name="count">The number of elements in the collection to
    /// represent in the hex string.</param>
    /// <returns>A hexadecimal string representation of the byte collection.
    /// </returns>
    public static string ToHexString(this IEnumerable<byte> bytes, int count)
    {
        StringBuilder sb = new();
        ReadOnlySpan<byte> byeList = bytes.ToArray().AsSpan();
        foreach (byte b in byeList)
        {
            sb.Append($"{b:x2}");
            if (--count == 0)
            {
                break;
            }
            sb.Append(' ');
        }
        return sb.ToString().Trim();
    }
}

public static class IParseTree_Extensions
{
    /// <summary>
    /// Gets whether this parsed expression is constant in value.
    /// </summary>
    /// <param name="expression">This expression.</param>
    /// <returns><c>true</c> if the expression is constant, <c>false</c>
    /// otherwise.</returns>
    public static bool IsConstant(this SyntaxParser.ExprContext expression)
    {
        return expression.value.IsDefined;
    }

    /// <summary>
    /// Get the member name part of the identifier.
    /// </summary>
    /// <param name="identifier">The parsed identifier.</param>
    /// <returns>The member name.</returns>
    public static string NamePart(this SyntaxParser.IdentifierPartContext identifier)
    {
        if (identifier.ident() != null)
        {
            return identifier.ident().GetText();
        }
        return identifier.Start.Text.TrimStartOnce('.').Trim();
    }

    /// <summary>
    /// Get the entire source line for a parsed statement.
    /// </summary>
    /// <param name="stat">The <see cref="SyntaxParser.StatContext"/>.</param>
    /// <param name="wholeSource">Indicate whether the statement's entire source,
    /// including line breaks, should be included.</param>
    /// <returns>The statement's source line.</returns>
    public static string GetSourceLine(this SyntaxParser.StatContext stat, bool wholeSource)
    {
        return stat.GetAllText(true, wholeSource);
    }

    public static int GetAllTextLength(this ParserRuleContext tree)
    {
        int a = tree.Start.StartIndex;
        int b = tree.Stop.StopIndex + 1;
        return b - a;
    }

    /// <summary>
    /// Get all text, including trivial characters (such as inline whitespace
    /// and comments) in the parse tree, as a way to represent the original 
    /// unparsed source text.</summary>
    /// <param name="tree">This <see cref="IParseTree"/>.</param>
    /// <param name="includeEndTrivia">Include all leading and trailing trivial
    /// characters.</param>
    /// <param name="includeNewlines">Include newlines that may be captured inside
    /// the parsed tree.</param>
    /// <returns>A string representation of the parse tree.</returns>
    public static string GetAllText(this IParseTree tree, bool includeEndTrivia, bool includeNewlines)
    {
        IList<IToken> treeTokens = tree.GetTreeTokens();
        if (treeTokens.Count == 0)
        {
            return string.Empty;
        }
        StringBuilder sb = new();
        ICharStream charStream = treeTokens[0].InputStream;
        for (int i = 0; i < treeTokens.Count && treeTokens[i].Type != TokenConstants.EOF; i++)
        {
            Token t = (Token)treeTokens[i];
            int start = -1;
            int startIndex = treeTokens[i].StartIndex, stopIndex = treeTokens[i].StopIndex;

            if (t.SubstitutionStartIndex > -1 || i != 0 && !char.IsWhiteSpace(sb[^1]) || (includeEndTrivia && sb.Length == 0))
            {
                if (t.SubstitutionStartIndex > -1)
                {
                    startIndex = t.SubstitutionStartIndex;
                    if (i == 0)
                    {
                        charStream = t.MacroCharStream!;
                    }
                }
                else if (charStream != treeTokens[i].InputStream)
                {
                    charStream = treeTokens[i].InputStream;
                }
                charStream.Seek(startIndex);
                int subTr = charStream.LA(start);
                if (subTr.IsOneOf('\n', '\r') && includeNewlines && i > 0 && i < treeTokens.Count - 1)
                {
                    subTr = charStream.LA(--start);
                }
                while (!subTr.EndsStatement() && char.IsWhiteSpace((char)subTr))
                {
                    start--;
                    subTr = charStream.LA(start);
                }
                if (i != 0 && subTr.EndsStatement() && (!includeNewlines || i == treeTokens.Count - 1))
                {
                    break;
                }
                start++;
                if (start < 0)
                {
                    Interval preTrivia = new(startIndex + start, startIndex - 1);
                    sb.Append(charStream.GetText(preTrivia));
                }
            }
            if (charStream != treeTokens[i].InputStream)
            {
                charStream = treeTokens[i].InputStream;
            }
            int lineBreakIndex = treeTokens[i].Text.IndexOf('\r');
            if (lineBreakIndex < 0) lineBreakIndex = treeTokens[i].Text.IndexOf('\n');
            if ((treeTokens[i].Type == SyntaxParser.NL || lineBreakIndex >= 0) &&
                (!includeNewlines || i == treeTokens.Count - 1))
            {
                if (includeNewlines)
                {
                    lineBreakIndex = treeTokens[i].Text.LastIndexOf('\r');
                    if (lineBreakIndex < 0) lineBreakIndex = treeTokens[i].Text.LastIndexOf('\n');
                }
                if (lineBreakIndex > 0)
                {
                    sb.Append(treeTokens[i].Text.AsSpan(0, lineBreakIndex));
                }
                break;
            }
            sb.Append(treeTokens[i].Text);
            if (includeEndTrivia && i == treeTokens.Count - 2)
            {
                if (t.SubstitutionStopIndex > -1 && t.MacroCharStream != null)
                {
                    stopIndex = t.SubstitutionStopIndex;
                    charStream = t.MacroCharStream;
                }
                charStream.Seek(stopIndex + 1);
                int stop = 1;
                int e = charStream.LA(stop);
                if (e >= 0)
                {
                    while (!e.EndsStatement())
                    {
                        e = charStream.LA(stop++);
                    }
                    stop--;
                    if (stop > 0)
                    {
                        Interval tokenPlusTrivia = new(stopIndex + 1, stopIndex + (stop - 1));
                        sb.Append(charStream.GetText(tokenPlusTrivia));
                    }
                }
            }
        }
        return sb.ToString();
    }

    /// <summary>
    /// Get the source info (source name, line and position) of the token.
    /// </summary>
    /// <param name="token">This token.</param>
    /// <returns>The source info of the token.</returns>
    public static string SourceInfo(this IToken token)
    {
        return $"{token.TokenSource.SourceName}({token.Line}:{token.Column}):";
    }

    /// <summary>
    /// Get all the parse tree's tokens.
    /// </summary>
    /// <param name="tree">This parse tree.</param>
    /// <returns>A list of the parse tree's tokens.</returns>
    public static IList<IToken> GetTreeTokens(this IParseTree tree)
    {
        List<IToken> treeTokens = new();
        for (int i = 0; i < tree.ChildCount; i++)
        {
            IParseTree child = tree.GetChild(i);
            if (child is ITerminalNode terminalNode)
            {
                treeTokens.Add(terminalNode.Symbol);
            }
            else
            {
                treeTokens.AddRange(child.GetTreeTokens());
            }
        }
        return treeTokens;
    }
}