//-----------------------------------------------------------------------------
// Copyright (c) 2017-2022 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using Antlr4.Runtime;
using System;

namespace Sixty502DotNet
{
    /// <summary>
    /// A parsed token containing information about its type, source filename,
    /// line number, and character position in the line number.
    /// </summary>
    public class Token : CommonToken
    {
        /// <summary>
        /// Construct a new instance of the <see cref="Token"/> class.
        /// </summary>
        /// <param name="source">The token's <see cref="ITokenSource"/> and
        /// <see cref="ICharStream"/> source.</param>
        /// <param name="type">The token type.</param>
        /// <param name="channel">The channel to broadcast by the token's
        /// <see cref="ITokenStream"/>.</param>
        /// <param name="start">The start index in the
        /// <see cref="ICharStream"/>.</param>
        /// <param name="stop">The stop index in the
        /// <see cref="ICharStream"/>.</param>
        public Token(Tuple<ITokenSource, ICharStream> source, int type, int channel, int start, int stop)
            : base(source, type, channel, start, stop)
        {
            Filename = "???";
            IncludedFrom = Tuple.Create("", -1);
            MacroInvoke = Tuple.Create("", -1);
        }

        /// <summary>
        /// Construct a new instance of the <see cref="Token"/> class.
        /// </summary>
        /// <param name="type">The token type.</param>
        /// <param name="text">The token's text.</param>
        public Token(int type, string text)
            : base(type, text)
        {
            Filename = "???";
            IncludedFrom = Tuple.Create("", -1);
            MacroInvoke = Tuple.Create("", -1);
        }

        /// <summary>
        /// Get or set filename of the token's source.
        /// </summary>
        public string Filename { get; set; }

        /// <summary>
        /// Get or set the file and line number the of the included source,
        /// if the token is from a different source than the current one.
        /// Used for diagnostic purposes.
        /// </summary>
        public Tuple<string, int> IncludedFrom { get; set; }

        /// <summary>
        /// Get or set the source filename and line number of the macro
        /// expansion if the token is an expansion of a macro. Used for
        /// diagnostic purposes.
        /// </summary>
        public Tuple<string, int> MacroInvoke { get; set; }
    }
}
