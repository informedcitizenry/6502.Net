//-----------------------------------------------------------------------------
// Copyright (c) 2017-2020 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;

namespace Core6502DotNet
{
    /// <summary>
    /// Represents expression errors. 
    /// </summary>
    public class ExpressionException : Exception
    {
        #region Constructors

        /// <summary>
        /// Constructs a new expression exception.
        /// </summary>
        /// <param name="token">The token that caused the exception.</param>
        /// <param name="message">The exception message.</param>
        public ExpressionException(Token token, string message)
            : this(token.Position, message)
        {
            Token = token;
        }

        /// <summary>
        /// Constructs a new expression exception.
        /// </summary>
        /// <param name="position">The token that caused the exception.</param>
        /// <param name="message">The exception message.</param>
        public ExpressionException(int position, string message)
            : base(message) => Position = position;

        #endregion

        #region Properties

        /// <summary>
        /// The source token that caused the exception.
        /// </summary>
        public Token Token { get; set; }

        /// <summary>
        /// The position in the source line that caused the exception.
        /// </summary>
        public int Position { get; set; }

        #endregion
    }
}
