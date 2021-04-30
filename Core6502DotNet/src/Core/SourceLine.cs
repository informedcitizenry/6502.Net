//-----------------------------------------------------------------------------
// Copyright (c) 2017-2021 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System.Collections.Generic;

namespace Core6502DotNet
{

    /// <summary>
    /// Represents one line of source code in the original
    /// assembly source.
    /// </summary>
    public class SourceLine
    {
        #region Members

        readonly List<string> _sources;

        #endregion

        #region Constructor

        /// <summary>
        /// Constructs a new instance of the source line class.
        /// </summary>
        /// <param name="fileName">The source file name.</param>
        /// <param name="lineNumber">The source line number.</param>
        /// <param name="sources">The line sources.</param>
        /// <param name="tokens">The collection of parsed tokens comprising the label, instruction, and any operands.</param>
        /// <param name="indexInSource">The index in the original source the line began.</param>
        public SourceLine(string fileName, int lineNumber, List<string> sources, List<Token> tokens, int indexInSource)
        {
            Filename = fileName;
            LineNumber = lineNumber;
            _sources = sources;
            IndexInSources = indexInSource;
            Operands = new List<Token>();
            foreach (var token in tokens)
            {
                token.Line = this;
                if (Label == null && token.Type == TokenType.Label)
                    Label = token;
                else if (Instruction == null && token.Type == TokenType.Instruction)
                    Instruction = token;
                else
                    Operands.Add(token);
            }
        }

        #endregion

        #region Methods

        public override string ToString() => $"{Filename} ({LineNumber}): {Source}";

        #endregion

        #region Properties

        /// <summary>
        ///  Gets the <see cref="SourceLine"/>'s original source filename.
        /// </summary>
        public string Filename { get; }

        /// <summary>
        /// Gets the <see cref="SourceLine"/>'s Line number in the original source file.
        /// </summary>
        public int LineNumber { get; }

        /// <summary>
        /// Gets the first line of the <see cref="SourceLine"/>'s original source.
        /// </summary>
        public string Source => _sources.Count > 0 ? _sources[0] : string.Empty;

        /// <summary>
        /// Gets the line's full sources.
        /// </summary>
        public string FullSource
        {
            get
            {
                if (_sources.Count > 0)
                {
                    if (_sources.Count > 1)
                        return string.Join('\n', _sources);
                    return _sources[0];
                }
                return string.Empty;
            }
        }

        /// <summary>
        /// Gets the <see cref="SourceLine"/>'s label/symbol. 
        /// </summary>
        public Token Label { get; }

        /// <summary>
        /// Gets the position in sources where the line's own source began.
        /// </summary>
        public int IndexInSources { get; }

        /// <summary>
        /// Gets the <see cref="SourceLine"/>'s instruction. 
        /// </summary>
        public Token Instruction { get; }

        /// <summary>
        /// Gets the collection of tokens that make up the <see cref="SourceLine"/>'s operand. 
        /// </summary>
        public List<Token> Operands { get; }

        #endregion
    }
}