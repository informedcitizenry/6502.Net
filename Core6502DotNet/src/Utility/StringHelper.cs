//-----------------------------------------------------------------------------
// Copyright (c) 2017-2021 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
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
            => iterator.Current != null && iterator.Current.IsDoubleQuote() && iterator.Current.Name.Length > 2 && Token.IsTerminal(iterator.PeekNext());

        /// <summary>
        /// Determines whether the tokenized expression is a string.
        /// </summary>
        /// <param name="iterator">The iterator to the tokenized expression.</param>
        /// <param name="services">The shared assembly services.</param>
        /// <returns></returns>
        public static bool ExpressionIsAString(RandomAccessIterator<Token> iterator, AssemblyServices services)
        {
            var token = iterator.Current;
            if (token == null)
                return false;
            if (token.IsDoubleQuote())
                return token.Name.Length > 2 && Token.IsTerminal(iterator.PeekNext());
            var ix = iterator.Index;
            var result = false;
            if (token.Type == TokenType.Function && 
                (token.Name.Equals("format", services.StringComparison) || token.Name.Equals("char", services.StringComparison)))
            {
                iterator.MoveNext();
                var parms = Token.GetGroup(iterator);
                var last = iterator.GetNext();
                result = Token.IsTerminal(last);
                if (token.Name.Equals("char", services.StringComparison))
                    result &= services.Evaluator.Evaluate(parms.GetIterator(), 0, 0x10FFFF).IsInteger();
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
                        result = Token.IsTerminal(iterator.Current) &&
                                 subscript >= 0 && subscript < sym.StringVector.Count;
                    }
                    else
                    {
                        result = Token.IsTerminal(iterator.Current) &&
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
            if (IsStringLiteral(iterator))
            {
                iterator.MoveNext();
                return Regex.Unescape(token.Name.ToString()).TrimOnce('"');
            }
            else if (token.Type == TokenType.Function && token.Name.Equals("format", services.StringComparison))
            {
                var str = GetFormatted(iterator, services);
                if (!string.IsNullOrEmpty(str) && Token.IsTerminal(iterator.Current))
                    return str;
            }
            else if (token.Type == TokenType.Function && token.Name.Equals("char", services.StringComparison))
            {
                var code = (int)services.Evaluator.Evaluate(iterator, 0, 0x10FFFF);
                return char.ConvertFromUtf32(services.Encoding.GetCodePoint(code));
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
                    if (services.Options.WarnCaseMismatch)
                    {
                        var lookupName = token.Name.Length == sym.Name.Length ? token.Name.ToString() :
                            token.Name.ToString().Split('.', StringSplitOptions.RemoveEmptyEntries)[^1];
                        if (!lookupName.Equals(sym.Name, StringComparison.Ordinal))
                            services.Log.LogEntry(token, $"Specified lookup to symbol \"{sym.Name}\" did not match its case.", false);
                    }
                    if ((!iterator.MoveNext() || Token.IsTerminal(iterator.Current)) && sym.StorageType == StorageType.Scalar)
                    {
                        return sym.StringValue.TrimOnce('"').ToString();
                    }
                    else if (sym.StorageType == StorageType.Vector && iterator.Current.Name.Equals("["))
                    {
                        var current = iterator.Current;
                        var subscript = (int)services.Evaluator.Evaluate(iterator);
                        if (Token.IsTerminal(iterator.Current))
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
            if (Token.IsTerminal(format))
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
                while (!Token.IsTerminal(iterator.GetNext()))
                {
                    if (ExpressionIsAString(iterator, services))
                    {
                        parms.Add(GetString(iterator, services));
                    }
                    else if (services.Evaluator.ExpressionIsCondition(iterator))
                    {
                        parms.Add(services.Evaluator.EvaluateCondition(iterator, false));
                    }
                    else
                    {
                        var parmVal = services.Evaluator.Evaluate(iterator, false);
                        if (Regex.IsMatch(fmt, $"\\{{{parms.Count}(,-?\\d+)?:(d|D|x|X)\\d*\\}}"))
                            parms.Add((int)parmVal);
                        else
                            parms.Add(parmVal);
                        if (iterator.Current.Type == TokenType.Closed && !Token.IsTerminal(iterator.PeekNext()))
                            break; // are we part of a larger expression?
                    }
                }
            }
            if (parms.Count == 0)
                return fmt;
            try
            {
                return string.Format(fmt, parms.ToArray());
            }
            catch (FormatException)
            {
                throw new SyntaxException(format, "There was a problem with the format string.");
            }
        }
    }
}