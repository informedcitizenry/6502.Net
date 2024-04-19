//-----------------------------------------------------------------------------
// Copyright (c) 2017-2024 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using Antlr4.Runtime;

namespace Sixty502DotNet.Shared;

public class InvalidCharLiteralError : Error
{
    /// <summary>
    /// Construct a new instance of a <see cref="InvalidCharLiteralError"/> class. 
    /// </summary>
    /// <param name="parserRuleContext">The <see cref="ParserRuleContext"/>
    /// that is the source of the error.</param>
    public InvalidCharLiteralError(ParserRuleContext parserRuleContext)
        : base(parserRuleContext, "Invalid character literal") {}

    /// <summary>
    /// Construct a new instance of a <see cref="InvalidCharLiteralError"/> class.
    /// </summary>
    /// <param name="offendingSymbol">The <see cref="IToken"/> that is the
    /// source of the error.</param>
    public InvalidCharLiteralError(IToken? offendingSymbol)
        : base(offendingSymbol, "Invalid character literal") {}
}