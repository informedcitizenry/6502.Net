//-----------------------------------------------------------------------------
// Copyright (c) 2017-2022 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using Antlr4.Runtime;

namespace Sixty502DotNet
{
    public class CustomParseError : RecognitionException
    {
        public CustomParseError()
            : base(null, null)
        {

        }

        public CustomParseError(string message, IRecognizer recognizer, IIntStream input, ParserRuleContext ctx)
            : base(message, recognizer, input, ctx)
        {
        }
    }
}
