//-----------------------------------------------------------------------------
// Copyright (c) 2017-2023 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

namespace Sixty502DotNet.Shared;

/// <summary>
/// A class to provide services for defining an enum in 6502.Net source.
/// </summary>
public static class Enum
{
    /// <summary>
    /// Define an enum object.
    /// </summary>
    /// <param name="context">The parsed declaration.</param>
    /// <param name="symbols">The <see cref="SymbolManager"/>.</param>
    /// <returns>A <see cref="ScopedSymbol"/> that represents the declared
    /// enum.</returns>
	public static ScopedSymbol Define(SyntaxParser.StatEnumDeclContext context, SymbolManager symbols)
    {
        ScopedSymbol enumSym = new(context.Identifier().Symbol, symbols.ActiveScope)
        {
            IsReferenced = false
        };
        symbols.Define(enumSym);
        int startVal = 0;
        SyntaxParser.EnumDefContext[] enumDefs = context.enumDef();
        for (int i = 0; i < enumDefs.Length; i++)
        {
            SyntaxParser.EnumDefContext enumDef = enumDefs[i];
            int defVal;
            if (enumDef.primaryExpr() != null)
            {
                int minval = i == 0 ? int.MinValue : startVal;
                defVal = Evaluator.EvalNumberLiteralType(enumDef.primaryExpr(), "Invalid enumeration", minval, int.MaxValue);
                startVal = defVal + 1;
            }
            else
            {
                defVal = startVal++;
            }
            Constant defSym = new(enumDef.Identifier().Symbol, new NumericValue(defVal), enumSym);
            enumSym.Define(defSym.Name, defSym);
        }
        return enumSym;
    }
}
