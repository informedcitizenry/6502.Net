//-----------------------------------------------------------------------------
// Copyright (c) 2017-2019 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace DotNetAsm
{
    /// <summary>
    /// A concrete implementation of <see cref="DotNetAsm.ISymbolManager" />.
    /// </summary>
    public sealed class SymbolManager : ISymbolManager
    {
        #region Members

        Dictionary<int, SourceLine> _anonPlusLines, _anonMinusLines, _orderedMinusLines;

        #region Static Members

        static readonly Dictionary<string, double> _constants = new Dictionary<string, double>(StringComparer.Ordinal)
        {
            { "MATH_PI", Math.PI },
            { "MATH_E", Math.E }
        };


        #endregion

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="T:DotNetAsm.SymbolManager"/> class.
        /// </summary>
        public SymbolManager()
        {
            Variables = new VariableCollection(Assembler.Options.StringComparar, Assembler.Evaluator);

            Labels = new LabelCollection(Assembler.Options.StringComparar);

            Labels.AddCrossCheck(Variables);
            Variables.AddCrossCheck(Labels);

            Labels.SetSymbol("MATH_PI", 3, true);
            Labels.SetSymbol("MATH_E", 2, true);

            _anonPlusLines = new Dictionary<int, SourceLine>();
            _anonMinusLines = new Dictionary<int, SourceLine>();
        }

        #endregion

        #region Methods

        string GetNamedSymbolValue(string symbol, SourceLine line, string scope)
        {
            if (symbol.First() == '_')
                symbol = string.Concat(scope, symbol);
            if (Variables.IsScopedSymbol(symbol, scope))
                return Variables.GetScopedSymbolValue(symbol, line.Scope).ToString();

            var value = Labels.GetScopedSymbolValue(symbol, line.Scope);
            if (value.Equals(long.MinValue))
                throw new SymbolNotDefinedException(symbol);
            return value.ToString();

        }

        string ConvertAnonymous(string symbol, SourceLine line, bool errorOnNotFound)
        {
            var trimmed = symbol.Trim(new char[] { '(', ')' });
            var addr = GetFirstAnonymousLabelFrom(line, trimmed);//GetAnonymousAddress(_currentLine, trimmed);
            if (addr < 0 && errorOnNotFound)
            {
                Assembler.Log.LogEntry(line, ErrorStrings.CannotResolveAnonymousLabel);
                return "0";
            }
            return addr.ToString();
        }

        public void AddAnonymousLine(SourceLine line)
        {
            if (line.Label.Equals("+"))
            {
                _anonPlusLines.Add(line.Id, line);
            }
            else if (line.Label.Equals("-"))
            {
                _anonMinusLines.Add(line.Id, line);
                // ordered dictionary is invalid now
                _orderedMinusLines = null;
            }
        }

        long GetFirstAnonymousLabelFrom(SourceLine fromLine, string direction)
        {
            int id = fromLine.Id;

            int count = direction.Length;
            bool forward = direction[0] == '+';
            SourceLine found = null;
            while (count > 0)
            {
                KeyValuePair<int, SourceLine> searched;
                if (forward)
                {
                    searched = _anonPlusLines.FirstOrDefault(l => l.Key > id);
                }
                else
                {
                    if (_orderedMinusLines == null)
                        _orderedMinusLines = _anonMinusLines.OrderByDescending(l => l.Key).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                    searched = _orderedMinusLines.FirstOrDefault(l => l.Key < id);
                }
                found = searched.Value;

                if (found == null)
                    break;

                if (string.IsNullOrEmpty(found.Scope) || found.Scope.Equals(fromLine.Scope, Assembler.Options.StringComparison) ||
                    (fromLine.Scope.Length > found.Scope.Length &&
                     found.Scope.Equals(fromLine.Scope.Substring(0, found.Scope.Length), Assembler.Options.StringComparison)))
                    count--;

                id = found.Id;
            }
            if (found != null)
                return found.PC;
            return -1;
        }

        public List<ExpressionElement> TranslateExpressionSymbols(SourceLine line, string expression, string scope, bool errorOnNotFound)
        {
            char lastTokenChar = char.MinValue;
            StringBuilder translated = new StringBuilder(), symbolBuilder = new StringBuilder();
            for (int i = 0; i < expression.Length; i++)
            {
                char c = expression[i];
                if (lastTokenChar.IsRadixOperator() && !char.IsLetterOrDigit(c))
                    throw new ExpressionException(expression);
                
                if (c == '\'' || c == '"')
                {
                    var literal = expression.GetNextQuotedString(i, true);
                    i += literal.Length + 1;
                    if (literal.Contains("\\"))
                        literal = Regex.Unescape(literal);
                    var bytes = Assembler.Encoding.GetBytes(literal);
                    if (bytes.Length > sizeof(int))
                        throw new OverflowException(literal);
                    if (bytes.Length < sizeof(int))
                        Array.Resize(ref bytes, sizeof(int));
                    var encodedValue = BitConverter.ToInt32(bytes, 0);
                    translated.Append(encodedValue);
                    lastTokenChar = '0'; // can be any operand
                }
                else if ((c == '*' || c == '-' || c == '+') &&
                         (lastTokenChar.IsOperator() || lastTokenChar == '(' || lastTokenChar == char.MinValue))
                {
                    bool isSpecial = false;
                    if (c == '*' && (lastTokenChar == '(' || i == 0 || expression[i - 1] != '*'))
                    {
                        isSpecial = true;
                        translated.Append(Assembler.Output.LogicalPC.ToString());
                    }
                    else if (lastTokenChar == '(' ||
                             lastTokenChar == char.MinValue || (lastTokenChar.IsOperator()) && char.IsWhiteSpace(expression[i - 1]))
                    {
                        int j = i, k;
                        for (; j < expression.Length && expression[j] == c; j++)
                            symbolBuilder.Append(c);
                        for (k = j; k < expression.Length && char.IsWhiteSpace(expression[k]); k++) { }
                        if (j >= expression.Length ||
                            (lastTokenChar == '(' && expression[k] == ')') ||
                            ((lastTokenChar == char.MinValue || lastTokenChar.IsOperator()) &&
                            char.IsWhiteSpace(expression[k - 1]) && expression[k].IsOperator()))
                        {
                            isSpecial = true;
                            translated.Append(ConvertAnonymous(symbolBuilder.ToString(), line, errorOnNotFound));
                            i = j - 1;
                        }
                        symbolBuilder.Clear();
                    }
                    if (isSpecial)
                    {
                        lastTokenChar = translated[translated.Length - 1];
                    }
                    else
                    {
                        lastTokenChar = c;
                        translated.Append(c);
                    }
                }
                else
                {
                    if (!char.IsWhiteSpace(c))
                        lastTokenChar = c;
                    translated.Append(c);
                }
            }
            var elements = Assembler.Evaluator.ParseElements(translated.ToString()).ToList();

            for (int i = 0; i < elements.Count; i++)
            {
                if (elements[i].type == ExpressionElement.Type.Operand && (elements[i].word[0] == '_' || char.IsLetter(elements[i].word[0])))
                {
                    var symbol = elements[i].word;
                    if (_constants.ContainsKey(symbol))
                        symbol = _constants[symbol].ToString();
                    else
                        symbol = GetNamedSymbolValue(symbol, line, scope);
                    if (symbol[0] == '-')
                    {
                        elements.Insert(i, new ExpressionElement
                        {
                            word = "-",
                            type = ExpressionElement.Type.Operator,
                            subType = ExpressionElement.Subtype.Unary
                        });
                        i++;
                        symbol = symbol.Substring(1);
                    }
                    elements[i] = new ExpressionElement
                    {
                        word = symbol,
                        type = ExpressionElement.Type.Operand,
                        subType = ExpressionElement.Subtype.None
                    };
                }
            }
            return elements;
        }

        public bool IsSymbol(string symbol) =>
            Labels.IsSymbol(symbol) || Variables.IsSymbol(symbol);

        #endregion

        #region Properties

        public VariableCollection Variables { get; private set; }

        public LabelCollection Labels { get; private set; }

        #endregion
    }
}
