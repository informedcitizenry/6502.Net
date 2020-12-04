//-----------------------------------------------------------------------------
// Copyright (c) 2017-2020 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

using ConversionDef = System.Func<Core6502DotNet.StringView, double>;
using OperationDef  = System.Tuple<System.Func<System.Collections.Generic.List<double>, double>, int>;

namespace Core6502DotNet
{
    /// <summary>
    /// Represents errors in evaluation of expressions.
    /// </summary>
    public class IllegalQuantityException : ExpressionException
    {
        /// <summary>
        /// Constructs a new instance of the evaluator exception.
        /// </summary>
        /// <param name="token">The token that is the cause of the exception.</param>
        /// <param name="quantity">The evaluated quantity that raised the error.</param>
        public IllegalQuantityException(Token token, double quantity)
            : base(token.Position, $"Illegal quantity in expression \"{token.Name}\" ({quantity}).")
            => Quantity = quantity;

        /// <summary>
        /// Gets the quantity that raised the error.
        /// </summary>
        public double Quantity { get; }
    }

    /// <summary>
    /// A class that can evaluate strings as mathematical expressions.
    /// </summary>
    public class Evaluator
    {
        #region Constants

        /// <summary>
        /// Represents the smallest possible value of a CBM/MBF floating
        /// point value.
        /// </summary>
        public const double CbmFloatMinValue = -2.93783588E+39;

        /// <summary>
        /// Represents the largest possible value of a CBM/MBF floating
        /// point value.
        /// </summary>
        public const double CbmFloatMaxValue = 1.70141183E+38;

        #endregion

        #region Members

        static readonly HashSet<StringView> s_conditionals = new HashSet<StringView>
        {
            "||", "&&", "<", "<=", ">", ">=", "==", "!="
        };

        static readonly HashSet<StringView> s_comparers = new HashSet<StringView>
        {
            "<", "<=", "!=", "==", ">=", ">"
        };

        static readonly IReadOnlyDictionary<StringView, ConversionDef> s_radixOperators = new Dictionary<StringView, ConversionDef>
        {
            { "$", new ConversionDef(parm => Convert.ToInt64(parm.ToString().Replace("_", string.Empty), 16)) },
            { "%", new ConversionDef(parm => Convert.ToInt64(GetBinaryString(parm.ToString().Replace("_", string.Empty)),  2)) }
        };

        static readonly IReadOnlyDictionary<Token, OperationDef> s_operators = new Dictionary<Token, OperationDef>
        {
            { new Token(",",  TokenType.Separator), new OperationDef(parms => parms[0],                                             0) },
            { new Token(">",  TokenType.Unary),     new OperationDef(parms => ((long)parms[0] & 65535) / 0x100,                     1) },
            { new Token("<",  TokenType.Unary),     new OperationDef(parms => (long)parms[0]  & 255,                                1) },
            { new Token("&",  TokenType.Unary),     new OperationDef(parms => (long)parms[0]  & 65535,                              1) },
            { new Token("^",  TokenType.Unary),     new OperationDef(parms => ((long)parms[0] & UInt24.MaxValue) / 0x10000,         1) },
            { new Token("||", TokenType.Binary),    new OperationDef(parms => ((long)parms[1]!=0?1:0) | ((long)parms[0]!=0?1:0),    2) },
            { new Token("&&", TokenType.Binary),    new OperationDef(parms => ((long)parms[1]!=0?1:0) & ((long)parms[0]!=0?1:0),    3) },
            { new Token("|",  TokenType.Binary),    new OperationDef(parms => (long)parms[1] | (long)parms[0],                      4) },
            { new Token("^",  TokenType.Binary),    new OperationDef(parms => (long)parms[1] ^ (long)parms[0],                      5) },
            { new Token("&",  TokenType.Binary),    new OperationDef(parms => (long)parms[1] & (long)parms[0],                      6) },
            { new Token("!=", TokenType.Binary),    new OperationDef(parms => !parms[1].AlmostEquals(parms[0])?1:0,                 7) },
            { new Token("==", TokenType.Binary),    new OperationDef(parms =>  parms[1].AlmostEquals(parms[0])?1:0,                 7) },
            { new Token("<",  TokenType.Binary),    new OperationDef(parms =>  parms[1]  < parms[0] ? 1 : 0,                        8) },
            { new Token("<=", TokenType.Binary),    new OperationDef(parms =>  parms[1] <= parms[0] ? 1 : 0,                        8) },
            { new Token(">=", TokenType.Binary),    new OperationDef(parms =>  parms[1] >= parms[0] ? 1 : 0,                        8) },
            { new Token(">",  TokenType.Binary),    new OperationDef(parms =>  parms[1]  > parms[0] ? 1 : 0,                        8) },
            { new Token("<<", TokenType.Binary),    new OperationDef(parms =>  (int)parms[1] << (int)parms[0],                      9) },
            { new Token(">>", TokenType.Binary),    new OperationDef(parms =>  (int)parms[1] >> (int)parms[0],                      9) },
            { new Token("-",  TokenType.Binary),    new OperationDef(parms =>  parms[1] - parms[0],                                10) },
            { new Token("+",  TokenType.Binary),    new OperationDef(parms =>  parms[1] + parms[0],                                10) },
            { new Token("/",  TokenType.Binary),
                new OperationDef(delegate(List<double> parms)
                {
                    if (parms[0].AlmostEquals(0)) throw new DivideByZeroException(); return parms[1] / parms[0];
                },                                                                                                                 11) },
            { new Token("*",  TokenType.Binary),    new OperationDef(parms => parms[1] * parms[0],                                 11) },
            { new Token("%",  TokenType.Binary),    new OperationDef(parms => (long)parms[1] % (long)parms[0],                     11) },
            { new Token("^^", TokenType.Binary),    new OperationDef(parms => Math.Pow(parms[1], parms[0]),                        12) },
            { new Token("~",  TokenType.Unary),     new OperationDef(parms => ~((long)parms[0]),                                   13) },
            { new Token("!",  TokenType.Unary),     new OperationDef(parms =>  parms[0].AlmostEquals(0) ? 1 : 0,                   13) },
            { new Token("-",  TokenType.Unary),     new OperationDef(parms => -parms[0],                                           13) },
            { new Token("+",  TokenType.Unary),     new OperationDef(parms => +parms[0],                                           13) },
            { new Token("$",  TokenType.Radix),     new OperationDef(parms =>  parms[0],                                           13) },
            { new Token("%",  TokenType.Radix),     new OperationDef(parms =>  parms[0],                                           13) }
        };

        static readonly Random s_rng = new Random();
        readonly IReadOnlyDictionary<StringView, OperationDef> _functions;
        readonly List<IFunctionEvaluator> _functionEvaluators;
        readonly Dictionary<StringView, double> _constants;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new instance of the expression evaluator.
        /// </summary>
        public Evaluator()
            : this(false)
        {

        }

        /// <summary>
        /// Creates a new instance of the expression evaluator.
        /// </summary>
        /// <param name="caseSensitive">Sets whether constants should be treated as
        /// case sensitive or not.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public Evaluator(bool caseSensitive)
        {
            var comp = caseSensitive ? StringViewComparer.Ordinal : StringViewComparer.IgnoreCase;
            _functions = new Dictionary<StringView, OperationDef>(comp)
            {
                { "abs",    new OperationDef(parms => Math.Abs(parms[0]),                                       1) },
                { "acos",   new OperationDef(parms => Math.Acos(parms[0]),                                      1) },
                { "atan",   new OperationDef(parms => Math.Atan(parms[0]),                                      1) },
                { "byte",   new OperationDef(parms => (int)parms[0] & 0xFF,                                     1) },
                { "cbrt",   new OperationDef(parms => Math.Pow(parms[0], 1.0 / 3.0),                            1) },
                { "ceil",   new OperationDef(parms => Math.Ceiling(parms[0]),                                   1) },
                { "cos",    new OperationDef(parms => Math.Cos(parms[0]),                                       1) },
                { "cosh",   new OperationDef(parms => Math.Cosh(parms[0]),                                      1) },
                { "deg",    new OperationDef(parms => parms[0] * 180 / Math.PI,                                 1) },
                { "dword",  new OperationDef(parms => (long)parms[0] & 0xFFFFFFFF,                              1) },
                { "exp",    new OperationDef(parms => Math.Exp(parms[0]),                                       1) },
                { "floor",  new OperationDef(parms => Math.Floor(parms[0]),                                     1) },
                { "frac",   new OperationDef(parms => Math.Abs(parms[0] - Math.Abs(Math.Round(parms[0], 0))),   1) },
                { "hypot",  new OperationDef(parms => Math.Sqrt(Math.Pow(parms[0], 2) + Math.Pow(parms[1], 2)), 2) },
                { "ln",     new OperationDef(parms => Math.Log(parms[0]),                                       1) },
                { "log",    new OperationDef(parms => Math.Log(parms[0], parms[1]),                             2) },
                { "log10",  new OperationDef(parms => Math.Log10(parms[0]),                                     1) },
                { "long",   new OperationDef(parms => (int)parms[0] & 0xFFFFFF,                                 1) },
                { "pow",    new OperationDef(parms => Math.Pow(parms[0], parms[1]),                             2) },
                { "rad",    new OperationDef(parms => parms[0] * Math.PI / 180,                                 1) },
                { "random", new OperationDef(parms => s_rng.Next((int)parms[0], (int)parms[1]),                 2) },
                { "round",  new OperationDef(parms => Math.Round(parms[0]),                                     1) },
                { "sgn",    new OperationDef(parms => Math.Sign(parms[0]),                                      1) },
                { "sin",    new OperationDef(parms => Math.Sin(parms[0]),                                       1) },
                { "sinh",   new OperationDef(parms => Math.Sinh(parms[0]),                                      1) },
                { "sqrt",   new OperationDef(parms => Math.Sqrt(parms[0]),                                      1) },
                { "tan",    new OperationDef(parms => Math.Tan(parms[0]),                                       1) },
                { "tanh",   new OperationDef(parms => Math.Tanh(parms[0]),                                      1) },
                { "word",   new OperationDef(parms => (int)parms[0] & 0xFFFF,                                   1) }
            };
            _constants = new Dictionary<StringView, double>(comp)
            {
                { "true",       1               },
                { "false",      0               },
                { "MATH_PI",    Math.PI         },
                { "MATH_E",     Math.E          },
                { "UINT32_MAX", uint.MaxValue   },
                { "INT32_MAX",  int.MaxValue    },
                { "UINT32_MIN", uint.MinValue   },
                { "INT32_MIN",  int.MinValue    },
                { "UINT24_MAX", UInt24.MaxValue },
                { "INT24_MAX",  Int24.MaxValue  },
                { "UINT24_MIN", UInt24.MinValue },
                { "INT24_MIN",  Int24.MinValue  },
                { "UINT16_MAX", ushort.MaxValue },
                { "INT16_MAX",  short.MaxValue  },
                { "UINT16_MIN", ushort.MinValue },
                { "INT16_MIN",  short.MinValue  },
                { "UINT8_MAX",  byte.MaxValue   },
                { "INT8_MAX",   sbyte.MaxValue  },
                { "UINT8_MIN",  byte.MinValue   },
                { "INT8_MIN",   sbyte.MinValue  }
            };
            _functionEvaluators = new List<IFunctionEvaluator>();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Determines whether the collection of parsed tokens comprise a conditional
        /// expression that evaluates to <c>true</c> or <c>false</c>.
        /// </summary>
        /// <param name="tokens">The collection of parsed tokens representing the expression.</param>
        /// <returns><c>true</c> if the expression is conditional, <c>false</c> otherwise.</returns>
        public bool ExpressionIsCondition(IEnumerable<Token> tokens)
        {
            var lastComparerFound = -1;
            var lastLogicalFound = -1;
            var lastBinary = -1;
            var lastWasCompound = false;

            var tokenList = tokens.ToList();
            if (tokenList.Count == 1 && (tokenList[0].Name.Equals("true") || tokenList[0].Name.Equals("false")))
                return true;
            if (tokenList.Count == 2 && tokenList[0].Name.Equals("!") && tokenList[0].Type == TokenType.Unary)
                return ExpressionIsCondition(tokenList.Skip(1));
            for (var i = 0; i < tokenList.Count; i++)
            {
                var token = tokenList[i];
                if (token.Type == TokenType.Binary)
                {
                    if (s_conditionals.Contains(token.Name))
                    {
                        lastWasCompound = false;
                        if (s_comparers.Contains(token.Name))
                            lastComparerFound = i;
                        else
                            lastLogicalFound = i;
                    }
                    else if (lastWasCompound)
                    {
                        return false;
                    }
                    lastBinary = i;
                }
                else if (token.Type == TokenType.Open)
                {
                    var opens = 1;
                    var afterExpression = i;
                    var isFunction = i > 0 && tokenList[i - 1].Type == TokenType.Function;
                    while (opens != 0)
                    {
                        if (tokenList[++afterExpression].Type == TokenType.Open)
                            opens++;
                        else if (tokenList[afterExpression].Type == TokenType.Closed)
                            opens--;
                    }
                    if (!isFunction && ExpressionIsCondition(tokenList.GetRange(i + 1, afterExpression - i - 1)))
                    {
                        if (i > 0 &&
                            tokenList[i - 1].Type == TokenType.Unary &&
                            tokenList[i - 1].Name[0] != '!')
                            return false;
                        if (lastBinary > lastLogicalFound)
                            return false;
                        lastWasCompound = true;
                        lastComparerFound = i;
                    }
                    i = afterExpression;
                }
                else if (token.Type == TokenType.Closed)
                    break;
            }
            return lastComparerFound > -1 && lastComparerFound > lastLogicalFound;
        }

        Stack<double> EvaluateInternal(RandomAccessIterator<Token> iterator, bool fromGroup = false)
        {
            var results = new Stack<double>();
            var operators = new Stack<Token>();
            var lastType = TokenType.None;
            StringView lastToken = "";
            StringView nonBase10 = "";
            Token token;
            var end = TokenType.End;
            while ((token = iterator.GetNext()) != null && !(token.Type == TokenType.Closed || (!fromGroup && end.HasFlag(token.Type))))
            {
                if (token.Type == TokenType.Operand)
                {
                    if (lastType == TokenType.Radix)
                    {
                        if (!nonBase10.Equals(""))
                            throw new ExpressionException(token, "Unexpected token.");
                        nonBase10 = token.Name;
                    }
                    else if (iterator.PeekNext() != null && iterator.PeekNext().Name.Equals("["))
                    {
                        var value = SymbolEvaluator(iterator);
                        if (double.IsNegativeInfinity(value))
                            throw new ExpressionException(token, "Index is out of range.");
                        results.Push(value);
                    }
                    else
                    {
                        results.Push(Evaluate(token));
                    }
                }
                else
                {
                    lastType = token.Type;
                    lastToken = token.Name;
                    if (token.Type == TokenType.Open)
                    {
                        var subResults = EvaluateInternal(iterator, true);
                        if (subResults.Count < 1)
                            return subResults;
                        foreach (var sr in subResults)
                            results.Push(sr);
                    }
                    else if (token.Type == TokenType.Function && !_functions.ContainsKey(lastToken))
                    {
                        var fe = _functionEvaluators.FirstOrDefault(fe => fe.EvaluatesFunction(token));
                        if (fe == null)
                            throw new SyntaxException(token, "Unknown function.");
                        results.Push(fe.EvaluateFunction(iterator));
                    }
                    else if (token.Type == TokenType.Unary || token.Type == TokenType.Radix)
                    {
                        operators.Push(token);
                    }
                    else
                    {
                        if (!TokenType.Evaluation.HasFlag(token.Type))
                            return new Stack<double>();
                        if (operators.Count > 0)
                        {
                            var top = operators.Peek();
                            var opOrder = token.Type == TokenType.Function ? int.MaxValue : s_operators[token].Item2;
                            while ((top.Type == TokenType.Function || s_operators[top].Item2 >= opOrder) && operators.Count > 0)
                            {
                                operators.Pop();
                                DoOperation(top, ref nonBase10, results);
                                if (operators.Count > 0)
                                    top = operators.Peek();
                            }
                        }
                        if (token.Type != TokenType.Separator)
                            operators.Push(token);
                    }
                }
            }
            while (operators.Count > 0)
                DoOperation(operators.Pop(), ref nonBase10, results);

            return results;
        }

        private void DoOperation(Token op, ref StringView nonBase10, Stack<double> output)
        {
            if (nonBase10.Length > 0)
            {
                try
                {
                    output.Push(s_radixOperators[op.Name](nonBase10));
                    nonBase10 = "";
                }
                catch
                {
                    throw new SyntaxException(op, $"\"{op.Name}{nonBase10}\" is not a valid expression.");
                }
            }
            else
            {
                OperationDef operation;
                var parms = new List<double>();
                var parmCount = 1;
                if (op.Type == TokenType.Function)
                {
                    operation = _functions[op.Name];
                    parmCount = operation.Item2;
                }
                else
                {
                    operation = s_operators[op];
                    if (op.Type == TokenType.Binary)
                        parmCount++;
                }
                while (parmCount-- >= parms.Count)
                {
                    if (output.Count == 0)
                    {
                        var opType = op.Type == TokenType.Function ? "function" : "operator";
                        throw new SyntaxException(op, $"Missing operand argument for {opType} \"{op}\".");
                    }
                    parms.Add(output.Pop());
                }
                output.Push(operation.Item1(parms));
            }
        }

        double DoEvaluation(RandomAccessIterator<Token> tokens, bool advanceIterator, double minValue, double maxValue, bool isMath)
        {
            var first = tokens.Current;
            if (first == null)
            {
                first = tokens.GetNext();
                tokens.Reset();
            }
            if (!advanceIterator)
                tokens.Rewind(tokens.Index - 1);
            var ix = tokens.Index;
            var result = EvaluateInternal(tokens);
            if (result.Count != 1)
                throw new SyntaxException(first, "Invalid expression.");
            var r = result.Pop();
            if (r != 0 && !double.IsNormal(r))
            {
                if (isMath)
                    return 0xffff;
                else
                    return 0;
            }
            if (isMath && (r < minValue || r > maxValue))
                throw new IllegalQuantityException(first, r);

            if (!isMath)
            {
                var ienumtokens = new RandomAccessIterator<Token>(tokens, true)
                                        .Skip(ix + 1)
                                        .Take(tokens.Index - ix - 1);
                if (!ExpressionIsCondition(ienumtokens))
                    throw new SyntaxException(first, "Invalid conditional expression.");
            }
            return r;
        }

        public double Evaluate(RandomAccessIterator<Token> tokens, bool advanceIterator)
            => DoEvaluation(tokens, advanceIterator, int.MinValue, uint.MaxValue, true);

        /// <summary>
        /// Evaluates a collection of parsed tokens as a mathematical expression.
        /// </summary>
        /// <param name="tokens">An iterator to the collection of parsed tokens representing the expression.</param>
        /// <returns>The result of the evaluation.</returns>
        /// <exception cref="DivideByZeroException"></exception>
        /// <exception cref="ExpressionException"></exception>
        /// <exception cref="IllegalQuantityException>"></exception>
        /// <exception cref="SyntaxException"></exception>
        public double Evaluate(RandomAccessIterator<Token> tokens)
            => DoEvaluation(tokens, true, int.MinValue, uint.MaxValue, true);

        /// <summary>
        /// Evaluates a collection of parsed tokens as a mathematical expression.
        /// </summary>
        /// <param name="tokens">An iterator to the collection of parsed tokens representing the expression.</param>
        /// <param name="minValue">The minimum value allowed in the evaluation.</param>
        /// <param name="maxValue">The maximum value allowed in the evaluation.</param>
        /// <returns>The result of the evaluation.</returns>
        /// <exception cref="DivideByZeroException"></exception>
        /// <exception cref="ExpressionException"></exception>
        /// <exception cref="IllegalQuantityException>"></exception>
        /// <exception cref="SyntaxException"></exception>
        public double Evaluate(RandomAccessIterator<Token> tokens, double minValue, double maxValue)
            => DoEvaluation(tokens, true, minValue, maxValue, true);

        /// <summary>
        /// Evaluates a collection of parsed tokens as a mathematical expression.
        /// </summary>
        /// <param name="tokens">An iterator to the collection of parsed tokens representing the expression.</param>
        /// <param name="doNotAdvance">Will not advance the iterator before beginning evaluation.</param>
        /// <param name="minValue">The minimum value allowed in the evaluation.</param>
        /// <param name="maxValue">The maximum value allowed in the evaluation.</param>
        /// <returns>The result of the evaluation.</returns>
        /// <exception cref="DivideByZeroException"></exception>
        /// <exception cref="ExpressionException"></exception>
        /// <exception cref="IllegalQuantityException>"></exception>
        /// <exception cref="SyntaxException"></exception>
        public double Evaluate(RandomAccessIterator<Token> tokens, bool doNotAdvance, double minValue, double maxValue)
            => DoEvaluation(tokens, doNotAdvance, minValue, maxValue, true);

        /// <summary>
        /// Evaluates a parsed token as a mathematical expression.
        /// </summary>
        /// <param name="token">The parsed token representing the expression.</param>
        /// <returns>The result of the evaluation.</returns>
        /// <exception cref="DivideByZeroException"></exception>
        /// <exception cref="ExpressionException"></exception>
        /// <exception cref="IllegalQuantityException>"></exception>
        /// <exception cref="SyntaxException"></exception>
        public double Evaluate(Token token)
            => Evaluate(token, int.MinValue, uint.MaxValue);

        /// <summary>
        /// Evaluates a parsed tokens as a mathematical expression.
        /// </summary>
        /// <param name="token">The parsed token representing the expression.</param>
        /// <param name="minValue">The minimum value allowed in the evaluation.</param>
        /// <param name="maxValue">The maximum value allowed in the evaluation.</param>
        /// <returns>The result of the evaluation.</returns>
        /// <exception cref="DivideByZeroException"></exception>
        /// <exception cref="ExpressionException"></exception>
        /// <exception cref="IllegalQuantityException>"></exception>
        /// <exception cref="SyntaxException"></exception>
        public double Evaluate(Token token, double minValue, double maxValue)
        {
            if (!double.TryParse(token.Name.ToString().Replace("_", string.Empty), out var converted))
            {
                if (token.Name[0] == '0' && token.Name.Length > 2)
                {
                    // try to convert non-base 10 literals in 0x/0b/0o notation.
                    try
                    {
                        if (token.Name[1] == 'b' || token.Name[1] == 'B')
                            converted = Convert.ToInt64(GetBinaryString(token.Name.Substring(2).Replace("_", string.Empty)), 2);
                        else if (token.Name[1] == 'o' || token.Name[1] == 'O')
                            converted = Convert.ToInt64(token.Name.Substring(2).Replace("_", string.Empty), 8);
                        else if (token.Name[1] == 'x' || token.Name[1] == 'X')
                            converted = Convert.ToInt64(token.Name.Substring(2).Replace("_", string.Empty), 16);
                        else
                            throw new ExpressionException(token, "Not a valid numeric constant.");
                    }
                    catch
                    {
                        throw new ExpressionException(token, "Not a valid numeric constant.");
                    }
                }
                else if (!_constants.TryGetValue(token.Name, out converted))
                {
                    var tokens = new List<Token> { token };
                    var it = tokens.GetIterator();
                    it.MoveNext();
                    converted = SymbolEvaluator(it);
                }
            }
            else if (token.Name[0] == '0'  &&
                     token.Name.Length > 1 &&
                     token.Name[1] >= '0'  &&
                     token.Name[1] <= '7')
            {
                // all leading zeros treated as octal
                try
                {
                    converted = Convert.ToInt64(token.ToString(), 8);
                }
                catch
                {
                    throw new ExpressionException(token, "Not a valid numeric constant.");
                }
            }
            if (converted < minValue || converted > maxValue)
                throw new ExpressionException(token, "Illegal quantity.");
            return converted;
        }

        public bool EvaluateCondition(RandomAccessIterator<Token> tokens, bool advanceIterator)
            => DoEvaluation(tokens, advanceIterator, 0, 1, false) == 1;

        /// <summary>
        /// Evaluates a collection of parsed tokens as a conditional expression.
        /// </summary>
        /// <param name="tokens">An iterator to the collection of parsed tokens representing the expression.</param>
        /// <returns><c>true</c> if the condition evaluated to true, <c>false</c> otherwise.</returns>
        /// <exception cref="DivideByZeroException"></exception>
        /// <exception cref="ExpressionException"></exception>
        /// <exception cref="IllegalQuantityException>"></exception>
        /// <exception cref="SyntaxException"></exception>
        public bool EvaluateCondition(RandomAccessIterator<Token> tokens)
            => DoEvaluation(tokens, true, 0, 1, false) == 1;

        /// <summary>
        /// Evaluates a parsed token as a conditional expression.
        /// </summary>
        /// <param name="token">The parsed token representing the expression.</param>
        /// <returns><c>true</c> if the condition evaluated to true, <c>false</c> otherwise.</returns>
        /// <exception cref="DivideByZeroException"></exception>
        /// <exception cref="ExpressionException"></exception>
        /// <exception cref="IllegalQuantityException>"></exception>
        /// <exception cref="SyntaxException"></exception>
        public bool EvaluateCondition(Token token)
        {
            var eval = Evaluate(token);
            if (eval != 1 && eval != 0)
                throw new ExpressionException(token, "Expression is not a condition.");
            return eval == 1;
        }

        /// <summary>
        /// Determines if the symbol is a reserved word for the Evaluator.
        /// </summary>
        /// <param name="symbol">The symbol name.</param>
        /// <returns><c>true</c> if the specified token is in the collection of reserved words,
        /// regardless of type, otherwise <c>false</c>.</returns>
        public bool IsReserved(StringView symbol)
           => _constants.ContainsKey(symbol) ||
              _functions.ContainsKey(symbol) ||
              _functionEvaluators.Any(fe => fe.IsFunctionName(symbol));

        /// <summary>
        /// Invoke a function call, discarding the return value.
        /// </summary>
        /// <param name="function">The parsed token representing the function.</param>
        /// <param name="functionCall">The parsed token collectiong representing the function call.</param>
        /// <exception cref="SyntaxException"></exception>
        public void Invoke(Token function, RandomAccessIterator<Token> functionCall)
        {
            if (_functions.ContainsKey(function.Name))
                return;
            var fe = _functionEvaluators.FirstOrDefault(fe => fe.EvaluatesFunction(function));
            if (fe == null)
                throw new SyntaxException(function, $"Unknown function \"{function}\".");
            fe.InvokeFunction(functionCall);
        }

        /// <summary>
        /// Get the binary string as a series of <c>1</c>s and <c>0</c>s, whether the input string is itself such a
        /// string, or is a series of <c>#</c>s and <c>.</c>s.
        /// </summary>
        /// <param name="binary">The binary or alt-binary string.</param>
        /// <returns>A binary string suitable for conversion.</returns>
        public static string GetBinaryString(string binary)
        {
            if (binary[0] == '#' || binary[0] == '.')
                return binary.Replace('#', '1')
                             .Replace('.', '0');
            return binary;
        }

        #endregion

        #region Properties

        /// <summary>
        /// An evaluator of symbols the evaluator does not recognize
        /// </summary>
        public Func<RandomAccessIterator<Token>, double> SymbolEvaluator { get; set; }

        /// <summary>
        /// Add a custom function evaluator to the evaluator.
        /// </summary>
        /// <param name="evaluator">The <see cref="IFunctionEvaluator"/> responsible for
        /// implementing the custom function definition.</param>
        public void AddFunctionEvaluator(IFunctionEvaluator evaluator)
            => _functionEvaluators.Add(evaluator);

        #endregion
    }
}