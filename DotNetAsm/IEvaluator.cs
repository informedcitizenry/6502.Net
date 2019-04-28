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
    /// Defines an interface for an expression evaluator that can evaluate mathematical 
    /// expressions from strings.
    /// </summary>
    public interface IEvaluator
    {
        /// <summary>
        /// Evaluates a text string as a mathematical expression.
        /// </summary>
        /// <param name="expression">The string representation of the mathematical expression.</param>
        /// <returns>The result of the expression evaluation.</returns>
        long Eval(string expression);

        /// <summary>
        /// Evaluates a text string as a mathematical expression.
        /// </summary>
        /// <param name="expression">The string representation of the mathematical expression.</param>
        /// <param name="minval">The minimum value of expression. If the evaluated value is
        /// lower, then an exception will occur.</param>
        /// <param name="maxval">The maximum value of the expression. If the evaluated value 
        /// is higher, then an exception will occur.</param>
        /// <returns>The result of the expression evaluation.</returns>
        long Eval(string expression, long minval, long maxval);

        /// <summary>
        /// Evaluates a text string as a conditional (boolean) evaluation.
        /// </summary>
        /// <param name="expression">The string representation of the conditional expression.</param>
        /// <returns><c>True</c> if the expression is true, otherwise <c>false</c>.</returns>
        bool EvalCondition(string expression);

        /// <summary>
        /// Defines a parser for the evaluator. Typically used to translate symbols (such as 
        /// variables) in expressions.
        /// </summary>
        /// <param name="parsingFunc">The parsing function to return the expression elements..</param>
        void DefineParser(Func<string, List<ExpressionElement>> parsingFunc);

        /// <summary>
        /// Extracts the individual expression elements, or tokens, from a string
        /// representation of a mathematical expression before they are sent to
        /// the calculation unit for final processing.
        /// </summary>
        /// <returns> A <see cref="System.Collections.Generic.List{DotNetAsm.ExpressionElement}"/>
        /// </returns>
        /// <param name="expression">The mathematical expression.</param>
        List<ExpressionElement> ParseElements(string expression);
    }
}
