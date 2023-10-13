//-----------------------------------------------------------------------------
// Copyright (c) 2017-2023 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;
using Antlr4.Runtime;

namespace Sixty502DotNet.Shared;

public class IllegalQuantityError : Error
{
    /// <summary>
    /// Construct a new instance of an <see cref="IllegalQuantityError"/> class.
    /// </summary>
    /// <param name="parserRuleContext">The <see cref="ParserRuleContext"/>
    /// that is the source of the error.</param>
    public IllegalQuantityError(ParserRuleContext parserRuleContext)
		: base(parserRuleContext, "Illegal quantity")
	{
	}

    /// <summary>
    /// Construct a new instance of an <see cref="Error"/> class.
    /// </summary>
    /// <param name="offendingSymbol">The <see cref="IToken"/> that is the
    /// source of the error.</param>
    public IllegalQuantityError(IToken? offendingSymbol)
        : base(offendingSymbol, "Illegal quantity")
    {
    }
}

