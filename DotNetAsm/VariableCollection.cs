//-----------------------------------------------------------------------------
// Copyright (c) 2017 informedcitizenry <informedcitizenry@gmail.com>
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to 
// deal in the Software without restriction, including without limitation the 
// rights to use, copy, modify, merge, publish, distribute, sublicense, and/or 
// sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in 
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS 
// IN THE SOFTWARE.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace DotNetAsm
{
    /// <summary>
    /// A <see cref="T:DotNetAsm.SymbolCollectionBase"/> implemented as a variable collection.
    /// </summary>
    public sealed class VariableCollection : SymbolCollectionBase
    {
        #region Members

        Regex _regExpression;
        IEvaluator _evaluator;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="T:DotNetAsm.VariableCollection"/> class.
        /// </summary>
        /// <param name="comparer">A <see cref="T:System.StringComparper"/>.</param>
        /// <param name="evaluator">A <see cref="T:DotNetAsm.IEvaluator"/> to evaluate RValue.</param>
        public VariableCollection(StringComparer comparer, IEvaluator evaluator)
            : base(comparer)
        {
            RegexOptions option = RegexOptions.Compiled;
            option |= comparer == StringComparer.CurrentCultureIgnoreCase ? RegexOptions.IgnoreCase : RegexOptions.None;
            _evaluator = evaluator;
            _regExpression = new Regex("^(_*" + Patterns.SymbolUnicode + @")\s*=\s*(.+)$", option);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Parses the assignment expression as a <see cref="T:System.Collections.Generic.KeyValuePair&lt;string, long&gt;"/> 
        /// </summary>
        /// <returns>The variable and its assigned value</returns>
        /// <param name="expression">The assignment expression.</param>
        /// <param name="inScope">The scope of the variable</param>
        KeyValuePair<string, long> ParseExpression(string expression, string inScope)
        {
            Match m = _regExpression.Match(expression);
            if (string.IsNullOrEmpty(m.Value) == false)
                return new KeyValuePair<string, long>(inScope + m.Groups[1].Value,
                                                      _evaluator.Eval(m.Groups[2].Value, int.MinValue, uint.MaxValue));
            return new KeyValuePair<string, long>(string.Empty, long.MinValue);
        }

        /// <summary>
        /// Gets the variable from a variable assignment expression.
        /// </summary>
        /// <returns>The variable from expression.</returns>
        /// <param name="expression">The assignment expression.</param>
        /// <param name="inScope">The current scope the expression is in.</param>
        public string GetVariableFromExpression(string expression, string inScope)
        {
            return ParseExpression(expression, inScope).Key;
        }

        /// <summary>
        /// Gets the assignment (RValue) from the expression.
        /// </summary>
        /// <returns>The assignment from expression.</returns>
        /// <param name="expression">Expression.</param>
        public string GetAssignmentFromExpression(string expression)
        {
            Match m = _regExpression.Match(expression);
            if (string.IsNullOrEmpty(m.Value) == false)
                return m.Groups[2].Value;
            return string.Empty;
        }

        /// <summary>
        /// Sets the variable according to the assignment expression &lt;var&gt; = &lt;operand&gt;.
        /// </summary>
        /// <returns>The variable and its assignment as a 
        /// <see cref="T:System.Collections.Generic.KeyValuePair&lt;string, long&gt;"/>.</returns>
        /// <param name="expression">The assignment expression.</param>
        /// <param name="inScope">The current scope the expression is in.</param>
        /// <exception cref="T:DotNetAsm.ExpressionException">DotNetAsm.ExpressionException</exception>
        /// <exception cref="T:DotNetAsm.SymbolCollectionException">DotNetAsm.SymbolCollectionException</exception>
        public KeyValuePair<string, long> SetVariable(string expression, string inScope)
        {
            KeyValuePair<string, long> result = ParseExpression(expression, inScope);
            if (string.IsNullOrEmpty(result.Key))
                throw new ExpressionException(expression);
            if (IsSymbolValid(result.Key, true) == false)
                throw new SymbolCollectionException(result.Key, SymbolCollectionException.ExceptionReason.SymbolNotValid);
            SetSymbol(result.Key, result.Value, false);
            return result;
        }

        #endregion
    }
}
