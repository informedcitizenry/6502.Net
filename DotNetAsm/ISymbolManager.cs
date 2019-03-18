//-----------------------------------------------------------------------------
// Copyright (c) 2017-2019 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;
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
        /// Translates the symbols in expression strings to values.
        /// </summary>
        /// <returns>The translated expression.</returns>
        /// <param name="line">The <see cref="DotNetAsm.SourceLine"/> the expression
        /// appears in. This is needed for scoping purposes.</param>
        /// <param name="expression">The expression string.</param>
        /// <param name="scope">Scope information about the current expression.</param>
        /// <param name="errorOnAnonymousNotFound">Raise an error if the anonymous symbol could
        /// not be translated.</param>
        IEnumerable<ExpressionElement> TranslateExpressionSymbols(SourceLine line, string expression, string scope, bool errorOnAnonymousNotFound);
    }
}
