//-----------------------------------------------------------------------------
// Copyright (c) 2017-2019 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System.Collections.Generic;

namespace DotNetAsm
{
    /// <summary>
    /// Represents an interface for a symbol manager.
    /// </summary>
    public interface ISymbolManager
    {
        /// <summary>
        /// Gets the <see cref="DotNetAsm.VariableCollection"/> of the manager.
        /// </summary>
        /// <value>The variables.</value>
        VariableCollection Variables { get; }

        /// <summary>
        /// Gets the labels of the manager as a 
        /// <see cref="DotNetAsm.LabelCollection"/>.
        /// </summary>
        /// <value>The labels.</value>
        LabelCollection Labels { get; }

        /// <summary>
        /// Adds <see cref="DotNetAsm.SourceLine"/> for anonymous label tracking.
        /// </summary>
        /// <param name="line">The source line having an anonymous label.</param>
        void AddAnonymousLine(SourceLine line);

        /// <summary>
        /// Translates all special symbols in the expression into a 
        /// <see cref="System.Collections.Generic.List{DotNetAsm.ExpressionElement}"/>
        /// for use by the evualator.
        /// </summary>
        /// <returns>The expression symbols.</returns>
        /// <param name="line">The current source line.</param>
        /// <param name="expression">The expression to evaluate.</param>
        /// <param name="scope">The current scope.</param>
        /// <param name="errorOnNotFound">If set to <c>true</c> raise an error 
        /// if a symbol encountered in the expression was not found.</param>
        List<ExpressionElement> TranslateExpressionSymbols(SourceLine line, string expression, string scope, bool errorOnNotFound);

        /// <summary>
        /// Returns a flag indicating whether the symbol is defined in any of the 
        /// symbol manager's collections.
        /// </summary>
        /// <param name="symbol"></param>
        /// <returns>True, if the symbol is a defined label or variable, otherwise false.</returns>
        bool IsSymbol(string symbol);
    }
}
