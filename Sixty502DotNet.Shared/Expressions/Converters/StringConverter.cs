//-----------------------------------------------------------------------------
// Copyright (c) 2017-2024 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Antlr4.Runtime;

namespace Sixty502DotNet.Shared;

/// <summary>
/// A class that converts a parsed well-formed character or string literal into
/// a <see cref="string"/>.
/// </summary>
public static partial class StringConverter
{
    private static readonly Regex s_surrogates = StringRegex();
    private static readonly Regex s_unicodepoint = UnicodeRegex();

    private static string Unescape(string escapedString)
    {
        string unic = s_surrogates.Replace(escapedString, u =>
            char.ToString((char)ushort.Parse(u.Groups[1].Value, NumberStyles.AllowHexSpecifier)) +
            char.ToString((char)ushort.Parse(u.Groups[2].Value, NumberStyles.AllowHexSpecifier)));
        unic = s_unicodepoint.Replace(unic, u =>
            char.ConvertFromUtf32(int.Parse(u.Groups[1].Value, NumberStyles.AllowHexSpecifier)));
        return Regex.Unescape(unic.Replace("\\x0", "\\x"));
    }

    /// <summary>
    /// Convert the parsed Commodore-encoded character literal into a character.
    /// </summary>
    /// <param name="charToken">The character literal representation.</param>
    /// <param name="encoding">The encoding.</param>
    /// <returns>A value that encapsulates the character.</returns>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="InvalidCharLiteralError"><exception>
    public static ValueBase ConvertCbmPetsciiChar(IToken charToken, Encoding? encoding)
    {
        string encodingName = charToken.Type == SyntaxParser.CbmScreenCharLiteral ?
                                                "\"cbmscreen\"" :
                                                "\"petscii\"";
        AsmEncoding? asmEncoding = encoding as AsmEncoding;
        if (asmEncoding == null)
        {
            asmEncoding = new AsmEncoding();
            EncodingMappings.MapEncodings(asmEncoding);
        }
        StringValue charStringVal = new(Unescape($"\"{charToken.Text[2..^1]}\""), asmEncoding!, encodingName);
        var charStringList = charStringVal.ToList();
        if (charStringList.Count != 1)
        {
            throw new InvalidCharLiteralError(charToken);
        }
        return charStringList[0];
    }

    /// <summary>
    /// Convert the parsed character literal into a character.
    /// </summary>
    /// <param name="charString">The character literal representation.</param>
    /// <param name="encoding">The encoding.</param>
    /// <returns>A value that encapsulates the character.</returns>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="InvalidCharLiteralError"><exception>
    public static ValueBase ConvertChar(IToken charToken, Encoding encoding, string? encodingName)
    {
        StringValue charStringVal = new(Unescape($"\"{charToken.Text[1..^1]}\""), encoding, encodingName);
        var charStringList = charStringVal.ToList();
        if (charStringList.Count != 1)
        {
            throw new InvalidCharLiteralError(charToken);
        }
        return new CharValue(charStringList[0].AsChar(), encoding, encodingName);
    }

    /// <summary>
    /// Convert the parsed string literal into a string.
    /// </summary>
    /// <param name="stringString">The string literal representation.</param>
    /// <param name="encoding">The encoding.</param>
    /// <param name="encodingName">The (optional) encoding name.</param>
    /// <returns>A value that encapsulates the string.</returns>
    public static ValueBase ConvertString(string stringString, Encoding encoding, string? encodingName)
    {
        if (stringString[0] == '"' && stringString[1] == '"' && stringString[2] == '"')
        {
            return new StringValue(stringString, encoding, encodingName);
        }
        return new StringValue(Unescape(stringString), encoding, encodingName);
    }

    [GeneratedRegex("\\\\u([dD][8-9a-bA-B][0-9a-fA-F]{2})\\\\u([dD][c-fC-F][0-9a-fA-F]{2})", RegexOptions.Compiled)]
    private static partial Regex StringRegex();
    [GeneratedRegex("\\\\[uU]([0-9a-fA-F]{4,8})", RegexOptions.Compiled)]
    private static partial Regex UnicodeRegex();
}

