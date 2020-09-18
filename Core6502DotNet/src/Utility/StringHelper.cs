//-----------------------------------------------------------------------------
// Copyright (c) 2017-2020 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;

namespace Core6502DotNet
{
    /// <summary>
    /// A helper class for string evaluations and manipulation.
    /// </summary>
    public static class StringHelper
    {
        /// <summary>
        /// Gets the string format from the parsed token.
        /// </summary>
        /// <param name="formatToken">The tokenfrom which to evaluate the string expression.</param>
        /// <param name="symbolManager">The symbol manager to reference symbols.</param>
        /// <param name="evaluator">The evaluator to help evaluate expressions.</param>
        /// <returns>Returns the format string from the parsed token.</returns>
        /// <exception cref="SyntaxException"></exception>
        public static string GetStringFormat(Token formatToken, SymbolManager symbolManager, Evaluator evaluator)
        {
            if (formatToken.Children.Count == 0)
                throw new SyntaxException(formatToken.Position, "String format argument not supplied.");

            string fmtString;
            Token firstChild = formatToken.Children[0];
            var tokenChildren = formatToken.Children;
            if (firstChild.Children.Count > 0 &&
                firstChild.Children[0].OperatorType == OperatorType.Function &&
                firstChild.Children[0].Name.Equals("format"))
            {
                fmtString = GetStringFormat(firstChild.Children[1], symbolManager, evaluator);
            }
            else
            {
                fmtString = firstChild.ToString();
                if (!fmtString.EnclosedInDoubleQuotes())
                    throw new ExpressionException(formatToken.Children[0].Position, "String format argument must be a string.");
                fmtString = fmtString.TrimOnce('"');
            }

            List<object> objects = null;
            if (tokenChildren.Count > 1)
            {
                if (tokenChildren[1].OperatorType != OperatorType.Separator)
                    throw new ExpressionException(tokenChildren[1].Position, "Expected argument list in format call.");
                objects = new List<object>();
                IEnumerable<Token> args = tokenChildren.Skip(1);
                foreach (Token arg in args)
                {
                    if (arg.Children.Count == 0)
                        throw new SyntaxException(arg.Position, "Value not specified.");
                    var argChild = arg.Children[0];

                    if (argChild.OperatorType == OperatorType.Function && argChild.Name.Equals("format"))
                    {
                        objects.Add(GetStringFormat(arg.Children[1], symbolManager, evaluator));
                    }
                    else if (argChild.Name.EnclosedInDoubleQuotes())
                    {
                        objects.Add(argChild.Name.TrimOnce('"'));
                    }
                    else
                    {
                        if (symbolManager.SymbolExists(argChild.Name))
                        {
                            var symbol = symbolManager.GetStringValue(argChild);
                            if (!string.IsNullOrEmpty(symbol))
                                objects.Add(symbol);
                            else
                                objects.Add((long)symbolManager.GetNumericValue(argChild));
                        }
                        else
                        {
                            objects.Add((long)evaluator.Evaluate(arg));
                        }
                    }
                }
            }
            return string.Format(fmtString, objects.ToArray());
        }

        /// <summary>
        /// Determines whether the tokenized expression is in fact a complete string.
        /// </summary>
        /// <param name="expression">The tokenized expression.</param>
        /// <returns><c>true</c> if the expression is a string, otherwise <c>false</c>.</c></returns>
        public static bool ExpressionIsAString(Token expression, SymbolManager symbolManager)
        {
            if (expression.Children.Count > 2)
                return false;
            var first = expression.Children[0];
            if ((first.OperatorType == OperatorType.Function && first.Name.Equals("format")) ||
                    (expression.Children.Count == 1 && first.Name.EnclosedInDoubleQuotes()))
                return true;
            if (expression.Children.Count == 2 &&
                first.Type == TokenType.Operand &&
                expression.Children[1].Name.Equals("["))
            {
                var symbol = symbolManager.GetStringVectorElementValue(expression.Children[0],
                                                                       expression.Children[1]);
                return !string.IsNullOrEmpty(symbol);

            }
            return false;
        }

        /// <summary>
        /// Converts the tokenized expression to its string literal 
        /// component.
        /// </summary>
        /// <param name="expression">The expression as a parsed <see cref="Token"/>.</param>
        /// <param name="symbolManager">The symbol manager to reference symbols.</param>
        /// <param name="evaluator">The evaluator to help evaluate expressions.</param>
        /// <returns>The string literal, if it is a string.</returns>
        public static string GetString(Token expression, SymbolManager symbolManager, Evaluator evaluator)
        {
            if (expression.Children.Count == 1)
            {
                if (expression.Children[0].Name.EnclosedInDoubleQuotes())
                    return expression.Children[0].Name.TrimOnce('"');
                if (expression.Children[0].Type == TokenType.Operand)
                    return symbolManager.GetStringValue(expression.Children[0].Name);
            }
            if (expression.Children.Count == 2)
            {
                if (expression.Children[0].OperatorType == OperatorType.Function &&
                   expression.Children[0].Name.Equals("format"))
                    return GetStringFormat(expression.Children[1], symbolManager, evaluator);
                if (expression.Children[0].Type == TokenType.Operand &&
                    expression.Children[1].Name.Equals("["))
                {
                    var stringVal = symbolManager.GetStringVectorElementValue(expression.Children[0], expression.Children[1]);
                    if (stringVal == string.Empty)
                        throw new SyntaxException(expression.Children[0].Position, "Type mismatch");
                    if (stringVal == null)
                        throw new SyntaxException(expression.Children[1].Position,
                            "Index is out of range.");
                    return stringVal;
                }
            }
            return string.Empty;
        }
    }
}