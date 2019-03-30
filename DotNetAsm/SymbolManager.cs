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

        IAssemblyController _controller;

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
        /// <param name="controller">The <see cref="DotNetAsm.IAssemblyController"/> for
        /// this symbol manager.</param>
        public SymbolManager(IAssemblyController controller)
        {
            _controller = controller;

            Variables = new VariableCollection(controller.Options.StringComparar, controller.Evaluator);

            Labels = new LabelCollection(controller.Options.StringComparar);

            Labels.AddCrossCheck(Variables);
            Variables.AddCrossCheck(Labels);

            Labels.SetSymbol("MATH_PI", 3, true);
            Labels.SetSymbol("MATH_E", 2, true);

            _anonPlusLines = new Dictionary<int, SourceLine>();
            _anonMinusLines = new Dictionary<int, SourceLine>();
        }

        #endregion

        #region Methods

        string GetNamedSymbolValue(string symbol, SourceLine line, string scope, bool error)
        {
            if (symbol.First() == '_')
                symbol = string.Concat(scope, symbol);
            if (Variables.IsScopedSymbol(symbol, scope))
                return Variables.GetScopedSymbolValue(symbol, line.Scope).ToString();

            var value = Labels.GetScopedSymbolValue(symbol, line.Scope);
            if (value.Equals(long.MinValue))
            {
                if (error)
                    throw new SymbolNotDefinedException(symbol);
                return "0";
            }
            return value.ToString();

        }

        string ConvertAnonymous(string symbol, SourceLine line, bool errorOnNotFound)
        {
            var trimmed = symbol.Trim(new char[] { '(', ')' });
            var addr = GetFirstAnonymousLabelFrom(line, trimmed);//GetAnonymousAddress(_currentLine, trimmed);
            if (addr < 0 && errorOnNotFound)
            {
                _controller.Log.LogEntry(line, ErrorStrings.CannotResolveAnonymousLabel);
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
                
                if (string.IsNullOrEmpty(found.Scope) || found.Scope.Equals(fromLine.Scope, _controller.Options.StringComparison) || 
                    (fromLine.Scope.Length > found.Scope.Length && 
                     found.Scope.Equals(fromLine.Scope.Substring(0, found.Scope.Length), _controller.Options.StringComparison)))
                    count--;

                id = found.Id;
            }
            if (found != null)
                return found.PC;
            return -1;
        }

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
        public List<ExpressionElement> TranslateExpressionSymbols(SourceLine line, string expression, string scope, bool errorOnNotFound)
        {
            char lastTokenChar = char.MinValue;
            StringBuilder translated = new StringBuilder(), symbolBuilder = new StringBuilder();
            for (int i = 0; i < expression.Length; i++)
            {
                char c = expression[i];
                if (c == '\'' || c == '"')
                {
                    var literal = expression.GetNextQuotedString(i);
                    var unescaped = literal.TrimOnce('\'');
                    if (unescaped.Contains("\\"))
                    {
                        unescaped = Regex.Unescape(unescaped);
                    }
                    var charval = _controller.Encoding.GetEncodedValue(unescaped.Substring(0, 1)).ToString();
                    translated.Append(charval);
                    i += literal.Length - 1;
                    lastTokenChar = charval.Last();
                }
                else if ((c == '*' || c == '-' || c == '+') &&
                         (lastTokenChar.IsOperator() || lastTokenChar == '(' || lastTokenChar == char.MinValue))
                {
                    bool isSpecial = false;
                    if (c == '*' && (lastTokenChar == '(' || i == 0 || expression[i - 1] != '*'))
                    {
                        isSpecial = true;
                        translated.Append(_controller.Output.LogicalPC.ToString());
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
            var elements = _controller.Evaluator.ParseElements(translated.ToString()).ToList();

            for(int i = 0; i < elements.Count; i++)
            {
                if (elements[i].type == ExpressionElement.Type.Operand && (elements[i].word[0] == '_' || char.IsLetter(elements[i].word[0])))
                {
                    var symbol = elements[i].word;
                    if (_constants.ContainsKey(symbol))
                        symbol = _constants[symbol].ToString();
                    else
                        symbol = GetNamedSymbolValue(symbol, line, scope, errorOnNotFound);
                    if (symbol[0] == '-')
                    {
                        elements.Insert(i, new ExpressionElement
                        {
                            word = "-",
                            type = ExpressionElement.Type.Operator,
                            subtype = ExpressionElement.Subtype.Unary
                        });
                        i++;
                        symbol = symbol.Substring(1);
                    }
                    elements[i] = new ExpressionElement
                    {
                        word = symbol,
                        type = ExpressionElement.Type.Operand,
                        subtype = ExpressionElement.Subtype.None
                    };
                }
            }
            return elements;
        }

        #endregion

        #region Properties

        public VariableCollection Variables { get; private set; }

        public LabelCollection Labels { get; private set; }

        #endregion
    }
}
