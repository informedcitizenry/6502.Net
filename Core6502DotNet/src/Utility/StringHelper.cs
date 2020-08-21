//-----------------------------------------------------------------------------
// Copyright (c) 2017-2020 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Text;

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
        /// <returns>Returns the format string from the parsed token.</returns>
        /// <exception cref="SyntaxException"></exception>
        public static string GetStringFormat(Token formatToken)
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
                fmtString = GetStringFormat(firstChild.Children[1]);
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
                    throw new SyntaxException(tokenChildren[1].Position, "Expected argument list in format call.");
                objects = new List<object>();
                IEnumerable<Token> args = tokenChildren.Skip(1);
                foreach (Token arg in args)
                {
                    if (arg.Children.Count == 0)
                        throw new SyntaxException(arg.Position, "Value not specified.");
                    var argChild = arg.Children[0];

                    if (argChild.OperatorType == OperatorType.Function && argChild.Name.Equals("format"))
                    {
                        objects.Add(GetStringFormat(arg.Children[1]));
                    }
                    else if (argChild.Name.EnclosedInDoubleQuotes())
                    {
                        objects.Add(argChild.Name.TrimOnce('"'));
                    }
                    else
                    {
                        if (Assembler.SymbolManager.SymbolExists(argChild.Name))
                        {
                            var symbol = Assembler.SymbolManager.GetStringValue(argChild);
                            if (!string.IsNullOrEmpty(symbol))
                                objects.Add(symbol);
                            else
                                objects.Add((long)Assembler.SymbolManager.GetNumericValue(argChild));
                        }
                        else
                        {
                            objects.Add((long)Evaluator.Evaluate(arg));
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
        public static bool ExpressionIsAString(Token expression)
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
                var symbol = Assembler.SymbolManager.GetStringVectorElementValue(expression.Children[0],
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
        /// <returns>The string literal, if it is a string.</returns>
        public static string GetString(Token expression)
        {
            if (expression.Children.Count == 1)
            {
                if (expression.Children[0].Name.EnclosedInDoubleQuotes())
                    return expression.Children[0].Name.TrimOnce('"');
                if (expression.Children[0].Type == TokenType.Operand)
                    return Assembler.SymbolManager.GetStringValue(expression.Children[0].Name);
            }
            if (expression.Children.Count == 2)
            {
                if (expression.Children[0].OperatorType == OperatorType.Function &&
                   expression.Children[0].Name.Equals("format"))
                    return GetStringFormat(expression.Children[1]);
                if (expression.Children[0].Type == TokenType.Operand &&
                    expression.Children[1].Name.Equals("["))
                {
                    var stringVal = Assembler.SymbolManager.GetStringVectorElementValue(expression.Children[0], expression.Children[1]);
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

        /// <summary>
        /// Gets the fully disassembly in the <see cref="SourceLine"/>'s
        /// assembly collection.
        /// </summary>
        /// <param name="line">The <see cref="SourceLine"/>.</param>
        /// <param name="startPc">The initial Program Counter.</param>
        /// <returns>The full representation of the disassembly of the byte collection in
        /// the <see cref="SourceLine"/>.</returns>
        public static string GetByteDisassembly(SourceLine line, int startPc)
        {
            var sb = new StringBuilder();
            var assembly = Assembler.Output.GetBytesFrom(startPc);
            if (!Assembler.Options.NoAssembly)
            {
                sb.Append(assembly.Take(8).ToString(startPc).PadRight(43, ' '));
                if (!Assembler.Options.NoSource)
                    sb.Append(line.UnparsedSource);
                if (assembly.Count > 8)
                {
                    sb.AppendLine();
                    sb.Append(assembly.Skip(8).ToString(startPc + 8));
                }
            }
            else
            {
                sb.Append($">{startPc:x4}");
                if (!Assembler.Options.NoSource)
                    sb.Append($"{line.UnparsedSource,43}");
            }
            return sb.ToString();
        }
    }
}