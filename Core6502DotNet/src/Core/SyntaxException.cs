//-----------------------------------------------------------------------------
// Copyright (c) 2017-2020 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

namespace Core6502DotNet
{
    /// <summary>
    /// Represents a syntax error.
    /// </summary>
    public class SyntaxException : ExpressionException
    {
        public SyntaxException(Token token, string message)
            : base(token, message) { }

        public SyntaxException(int position, string message)
            : base(position, message) { }
    }
}
