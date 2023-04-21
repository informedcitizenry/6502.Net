//-----------------------------------------------------------------------------
// Copyright (c) 2017-2023 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using Antlr4.Runtime;

namespace Sixty502DotNet.Shared;

/// <summary>
/// Implements a token factory that is custom to the 6502.Net parsing
/// process, capturing the inclusion and macro info.
/// </summary>
public class TokenFactory : CommonTokenFactory
{
    /// <summary>
    /// Construct a new instance of a <see cref="TokenFactory"/> class.
    /// </summary>
    public TokenFactory()
    {
        Inclusions = new();
        MacroName = null;
        MacroCharStream = null;
        MacroLine = 0;
    }

    /// <summary>
    /// Create and return a new token.
    /// </summary>
    /// <param name="source">The token source.</param>
    /// <param name="type">The token type.</param>
    /// <param name="text">The token text.</param>
    /// <param name="channel">The token channel.</param>
    /// <param name="start">The token start index.</param>
    /// <param name="stop">The token stop index.</param>
    /// <param name="line">The token source line.</param>
    /// <param name="charPositionInLine">The token character position
    /// in its source.</param>
    /// <returns>The created token.</returns>
    public override CommonToken Create(Tuple<ITokenSource, ICharStream> source, int type, string text, int channel, int start, int stop, int line, int charPositionInLine)
    {
        return new Token(source, type, channel, start, stop)
        {
            Text = text,
            Line = line,
            Column = charPositionInLine,
            Inclusions = Inclusions,
            MacroName = MacroName,
            MacroLine = MacroLine,
            MacroCharStream = MacroCharStream,
            SubstitutionStartIndex = -1,
            SubstitutionStopIndex = -1
        };
    }

    /// <summary>
    /// Get the inclusions stack.
    /// </summary>
    public Stack<(string, int)> Inclusions { get; }

    /// <summary>
    /// Get or set the macro whose definition the token resides.
    /// </summary>
    public string? MacroName { get; set; }

    /// <summary>
    /// Get or set the line number in the macro invocation.
    /// </summary>
    public int MacroLine { get; set; }

    /// <summary>
    /// Get or set the <see cref="ICharStream"/> of the macro associated
    /// to the token.
    /// </summary>
    public ICharStream? MacroCharStream { get; set; }
}

