//-----------------------------------------------------------------------------
// Copyright (c) 2017-2022 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using System;
using System.Collections.Generic;

namespace Sixty502DotNet
{
    /// <summary>
    /// A token factor to assist the lexer in creating tokens.
    /// </summary>
    public class TokenFactory : ITokenFactory
    {
        /// <summary>
        /// Construct a new instance of the <see cref="TokenFactory"/> class.
        /// </summary>
        public TokenFactory()
        {
            Filenames = new();
            Included = new();
            IncludeLineNumbers = new();
            MacroInvokeLines = new();
        }

        /// <summary>
        /// Construct a new instance of the <see cref="TokenFactory"/> class.
        /// </summary>
        /// <param name="fileName">The default filename of the tokens created.
        /// </param>
        public TokenFactory(string fileName)
            : this()
        {
            Filenames.Push(fileName);
        }

        /// <summary>
        /// Create a <see cref="Token"/>.
        /// </summary>
        /// <param name="source">The token's <see cref="ITokenSource"/> and
        /// <see cref="ICharStream"/> source.</param>
        /// <param name="type">The token type.</param>
        /// <param name="_">Discarded parameter.</param>
        /// <param name="channel">The channel to broadcast by the token's
        /// <see cref="ITokenStream"/>.</param>
        /// <param name="start">The start index in the
        /// <see cref="ICharStream"/>.</param>
        /// <param name="stop">The stop index in the
        /// <see cref="ICharStream"/>.</param>
        /// <param name="line">The line number in source of the token.</param>
        /// <param name="charPositionInLine">The character position in the
        /// line in source of the token.</param>
        /// <returns>A token object.</returns>
        public Token Create(Tuple<ITokenSource, ICharStream> source, int type, string _, int channel, int start, int stop, int line, int charPositionInLine)
        {
            var token = new Token(source, type, channel, start, stop)
            {
                Line = line,
                Column = charPositionInLine
            };
            ICharStream input = source.Item2;
            if (start > -1 && stop > -1)
            {
                token.Text = input.GetText(new Interval(start, stop));
            }
            else
            {
                token.Text = string.Empty;
            }
            token.Filename = Filenames.Peek();
            if (Included.Count > 0 && Included.Peek() && Filenames.Count > 0 && IncludeLineNumbers.Count > 0)
            {
                var currentFile = Filenames.Pop();
                token.IncludedFrom = new Tuple<string, int>(Filenames.Peek(), IncludeLineNumbers.Peek());
                Filenames.Push(currentFile);
            }
            if (MacroInvokeLines.Count > 0)
            {
                token.MacroInvoke = MacroInvokeLines.Peek();
            }
            return token;
        }

        IToken ITokenFactory.Create(Tuple<ITokenSource, ICharStream> source, int type, string text, int channel, int start, int stop, int line, int charPositionInLine)
            => Create(source, type, text, channel, start, stop, line, charPositionInLine);

        /// <summary>
        /// Create a <see cref="Token"/>.
        /// </summary>
        /// <param name="type">The token type.</param>
        /// <param name="text">The token's text.</param>
        /// <returns>A token object.</returns>
        public static Token Create(int type, string text)
            => new(type, text);

        [return: NotNull]
        IToken ITokenFactory.Create(int type, string text)
            => Create(type, text);

        /// <summary>
        /// Get or set the included stack.
        /// </summary>
        public Stack<bool> Included { get; set; }

        /// <summary>
        /// Get or set the stack of filenames of the source files being
        /// processed.
        /// </summary>
        public Stack<string> Filenames { get; init; }

        /// <summary>
        /// Get or set the stack of included source files line numbers.
        /// </summary>
        public Stack<int> IncludeLineNumbers { get; init; }

        /// <summary>
        /// Get or set the stack of macro invocation source and line numbers.
        /// </summary>
        public Stack<Tuple<string, int>> MacroInvokeLines { get; init; }
    }
}
