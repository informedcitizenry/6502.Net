//-----------------------------------------------------------------------------
// Copyright (c) 2017-2023 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using Antlr4.Runtime;

namespace Sixty502DotNet.Shared;

/// <summary>
/// The custom 6502.Net token.
/// </summary>
public class Token : CommonToken
{
    /// <summary>
    /// Construct a new token from an existing <see cref="IToken"/>.
    /// </summary>
    /// <param name="other">The ANTLR token.</param>
    public Token(IToken other)
        : base(other)
    {
        Inclusions = new();
        MacroName = null;
        MacroLine = 0;
        OriginalTokenIndex = TokenIndex;
    }

    /// <summary>
    /// Construct a new token from an existing <see cref="Token"/>.
    /// </summary>
    /// <param name="other">The 6502.Net token.</param>
    public Token(Token other)
        : base(other)
    {
        Inclusions = new();
        MacroName = null;
        MacroLine = 0;
        SubstitutionStartIndex = other.SubstitutionStartIndex;
        OriginalTokenIndex = TokenIndex;
    }

    /// <summary>
    /// Construct a new instance of a token.
    /// </summary>
    /// <param name="source">The token source.</param>
    /// <param name="type">The token type.</param>
    /// <param name="channel">The channel of the token.</param>
    /// <param name="start">The token's start index.</param>
    /// <param name="stop">The token's stop index.</param>
    public Token(Tuple<ITokenSource, ICharStream> source, int type, int channel, int start, int stop)
        : base(source, type, channel, start, stop)
    {
        Inclusions = new();
        MacroName = null;
        MacroLine = 0;
        OriginalTokenIndex = TokenIndex;
    }

    /// <summary>
    /// Construct a new instance of a token with a specified type.
    /// </summary>
    /// <param name="type">The token type.</param>
    public Token(int type)
        : base(type)
    {
        Inclusions = new();
        MacroName = null;
        MacroLine = 0;
        MacroCharStream = null;
        SubstitutionStartIndex =
        SubstitutionStopIndex = -1;
        OriginalTokenIndex = TokenIndex;
    }

    /// <summary>
    /// Construct a new instance of a token with a specified type and text.
    /// </summary>
    /// <param name="type">The token type.</param>
    /// <param name="text">The token text.</param>
    public Token(int type, string text)
        : base(type, text)
    {
        Inclusions = new();
        MacroName = null;
        MacroLine = 0;
        MacroCharStream = null;
        SubstitutionStartIndex =
        SubstitutionStopIndex = -1;
        OriginalTokenIndex = TokenIndex;
    }

    /// <summary>
    /// Get the listed inclusion stack as a reversed list.
    /// </summary>
    public IList<(string, int)> ListedInclusions
    {
        get
        {
            return Inclusions.Reverse().ToList();
        }
    }

    /// <summary>
    /// Get the inclusions tack.
    /// </summary>
    public Stack<(string, int)> Inclusions { get; init; }

    /// <summary>
    /// Get or set the token's macro invocation name.
    /// </summary>
    public string? MacroName { get; set; }

    /// <summary>
    /// Get or set the token's macro invocation line.
    /// </summary>
    public int MacroLine { get; set; }

    /// <summary>
    /// Get or set the original token index.
    /// </summary>
    public int OriginalTokenIndex { get; set; }

    /// <summary>
    /// Get or set the macro substitution start index.
    /// </summary>
    public int SubstitutionStartIndex { get; set; }

    /// <summary>
    /// Get or set the macro substitution stop index.
    /// </summary>
    public int SubstitutionStopIndex { get; set; }

    /// <summary>
    /// Get or set the macro <see cref="ICharStream"/>.
    /// </summary>
    public ICharStream? MacroCharStream { get; set; }
}

public static class Token_Extension
{
    /// <summary>
    /// Construct a new copy of this <see cref="IToken"/> with a new type
    /// and text.
    /// </summary>
    /// <param name="token">This token.</param>
    /// <param name="type">The new token type.</param>
    /// <param name="text">The new token text.</param>
    /// <returns></returns>
    public static IToken ToNew(this IToken token, int type, string text)
    {
        Tuple<ITokenSource, ICharStream> source = new
        (
            token.TokenSource, token.InputStream
        );
        if (token is Token asmToken)
        {
            return new Token(source, type, asmToken.Channel, asmToken.StartIndex, asmToken.StartIndex + text.Length)
            {
                Column = token.Column,
                Inclusions = asmToken.Inclusions,
                MacroLine = asmToken.MacroLine,
                MacroName = asmToken.MacroName,
                Text = text
            };
        }
        return new Token(source, type, token.Channel, token.StartIndex, token.StartIndex + text.Length)
        {
            Column = token.Column,
            Text = text
        };
    }
}
