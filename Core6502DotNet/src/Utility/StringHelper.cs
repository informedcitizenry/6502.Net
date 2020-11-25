//-----------------------------------------------------------------------------
// Copyright (c) 2017-2020 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Core6502DotNet
{
    /// <summary>
    /// A helper class for string evaluations and manipulation.
    /// </summary>
    public static class StringHelper
    {
        /// <summary>
        /// Determines whether the tokenized expression is a string literal.
        /// </summary>
        /// <param name="iterator">The iterator to the tokenized expression.</param>
        /// <returns></returns>
        public static bool IsStringLiteral(RandomAccessIterator<Token> iterator)
            => iterator.Current != null && iterator.Current.IsDoubleQuote() && Token.IsEnd(iterator.PeekNext());

        /// <summary>
        /// Determines whether the tokenized expression is a string.
        /// </summary>
        /// <param name="iterator">The iterator to the tokenized expression.</param>
        /// <param name="services">The shared assembly services.</param>
        /// <returns></returns>
        public static bool ExpressionIsAString(RandomAccessIterator<Token> iterator, AssemblyServices services)
        {
            var token = iterator.Current;
            if (token.IsDoubleQuote())
                return Token.IsEnd(iterator.PeekNext());
            var ix = iterator.Index;
            var result = false;
            if (token.Type == TokenType.Function && token.Name.Equals("format", services.StringComparison))
            {
                iterator.MoveNext();
                var open = 1;
                while (open > 0)
                {
                    token = iterator.GetNext();
                    if (token.Name.Equals("("))
                        open++;
                    else if (token.Name.Equals(")"))
                        open--;
                }
                var last = iterator.PeekNext();
                result = Token.IsEnd(last);
            }
            else if (token.Type == TokenType.Operand && 
                    (char.IsLetter(token.Name[0]) || token.Name[0] == '_') &&
                    !services.Evaluator.IsReserved(token.Name))
            {
                var sym = services.SymbolManager.GetSymbol(token, false);
                if (sym != null)
                {
                    if (iterator.MoveNext() && iterator.Current.Name.Equals("["))
                    {
                        var subscript = (int)services.Evaluator.Evaluate(iterator);
                        result = Token.IsEnd(iterator.Current) &&
                                 subscript >= 0 && subscript < sym.StringVector.Count;
                    }
                    else
                    {
                        result = Token.IsEnd(iterator.Current) &&
                                 sym.StorageType == StorageType.Scalar &&
                                 sym.DataType == DataType.String;
                    }
                }
            }
            iterator.SetIndex(ix);
            return result;
        }

        /// <summary>
        /// Gets a string from the tokenized expression.
        /// </summary>
        /// <param name="iterator">The iterator to the tokenized expression.</param>
        /// <param name="services">The shared assembly services.</param>
        /// <returns></returns>
        public static string GetString(RandomAccessIterator<Token> iterator, AssemblyServices services)
        {
            if (iterator.Current == null && !iterator.MoveNext())
                return string.Empty;
            var token = iterator.Current;
            if (Token.IsEnd(iterator.PeekNext()) && token.IsDoubleQuote())
            {
                iterator.MoveNext();
                return Regex.Unescape(token.Name.ToString()).TrimOnce('"');
            }
            else if (token.Type == TokenType.Function && token.Name.Equals("format", services.StringComparison))
            {
                var str = GetFormatted(iterator, services);
                if (!string.IsNullOrEmpty(str) && Token.IsEnd(iterator.Current))
                    return str;
            }
            else if (token.Type == TokenType.Operand && 
                    (char.IsLetter(token.Name[0]) || token.Name[0] == '_') &&
                    !services.Evaluator.IsReserved(token.Name))
            {
                var sym = services.SymbolManager.GetSymbol(token, services.CurrentPass > 0);
                if (sym == null)
                    return string.Empty;
                if (sym.DataType == DataType.String)
                {
                    if ((!iterator.MoveNext() || Token.IsEnd(iterator.Current)) && sym.StorageType == StorageType.Scalar)
                    {
                        return sym.StringValue.ToString();
                    }
                    else if (sym.StorageType == StorageType.Vector && iterator.Current.Name.Equals("["))
                    {
                        var current = iterator.Current;
                        var subscript = (int)services.Evaluator.Evaluate(iterator);
                        if (Token.IsEnd(iterator.Current))
                        {
                            if (subscript >= 0 && subscript < sym.StringVector.Count)
                                return sym.StringVector[subscript].ToString();
                            throw new SyntaxException(current, "Index out of range.");
                        }
                    }
                }
            }
            throw new SyntaxException(token, "Type mismatch.");
        }

        /// <summary>
        /// Gets the formatted string from the tokenized expression.
        /// </summary>
        /// <param name="iterator">The iterator to the tokenized expression.</param>
        /// <param name="services">The shared assembly services.</param>
        /// <returns></returns>
        public static string GetFormatted(RandomAccessIterator<Token> iterator, AssemblyServices services)
        {
            iterator.MoveNext();
            var format = iterator.GetNext();
            if (Token.IsEnd(format))
                return null;
            string fmt;
            if (!format.IsDoubleQuote())
            {
                if (format.Type != TokenType.Function && !format.Name.Equals("format", services.StringComparison))
                    return null;
                fmt = GetFormatted(iterator, services);
            }
            else
            {
                fmt = Regex.Unescape(format.Name.TrimOnce('"').ToString());
            }
            var parms = new List<object>();
            if (iterator.MoveNext())
            {
                Token token;
                while (!Token.IsEnd(token = iterator.GetNext()))
                {
                    if (ExpressionIsAString(iterator, services))
                    {
                        if (token.IsDoubleQuote())
                            parms.Add(token.Name.ToString().TrimOnce('"'));
                        else
                            parms.Add(GetFormatted(iterator, services));
                    }
                    else
                    {
                        parms.Add((int)services.Evaluator.Evaluate(iterator, false));
                    }
                }
            }
            if (parms.Count == 0)
                return fmt;
            return string.Format(fmt, parms.ToArray());
        }
    }
}