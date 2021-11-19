//-----------------------------------------------------------------------------
// Copyright (c) 2017-2021 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
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
    /// An enumeration of possible value types returned in an successful evaluation.
    /// </summary>
    public enum ValueType
    {
        Unknown = 0,
        Boolean,
        Binary,
        Double,
        Integer
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

        static readonly IReadOnlyDictionary<StringView, ConversionDef> s_radixOperators = new Dictionary<StringView, ConversionDef>
        {
            { "$", new ConversionDef(parm => Convert.ToInt64(parm.ToString().Replace("_", string.Empty), 16)) },
            { "%", new ConversionDef(parm => Convert.ToInt64(GetBinaryString(parm.ToString()),  2)) }
        };

        static readonly IReadOnlyDictionary<Token, OperationDef> s_operators = new Dictionary<Token, OperationDef>
        {
            { new Token(",",  TokenType.Separator), new OperationDef(parms => parms[0],                                             0) },
            { new Token("?",  TokenType.Ternary),   new OperationDef(parms => parms[2] == 1 ? parms[0] : parms[1],                  1) },
            { new Token(":",  TokenType.Ternary),   new OperationDef(parms => parms[0],                                             1) },
            { new Token(">",  TokenType.Unary),     new OperationDef(parms => ((long)parms[0] & 65535) / 0x100,                     2) },
            { new Token("<",  TokenType.Unary),     new OperationDef(parms => (long)parms[0]  & 255,                                2) },
            { new Token("&",  TokenType.Unary),     new OperationDef(parms => (long)parms[0]  & 65535,                              2) },
            { new Token("^",  TokenType.Unary),     new OperationDef(parms => ((long)parms[0] & UInt24.MaxValue) / 0x10000,         2) },
            { new Token("||", TokenType.Binary),    new OperationDef(parms => ((long)parms[1]!=0?1:0) | ((long)parms[0]!=0?1:0),    3) },
            { new Token("&&", TokenType.Binary),    new OperationDef(parms => ((long)parms[1]!=0?1:0) & ((long)parms[0]!=0?1:0),    4) },
            { new Token("|",  TokenType.Binary),    new OperationDef(parms => (long)parms[1] | (long)parms[0],                      5) },
            { new Token("^",  TokenType.Binary),    new OperationDef(parms => (long)parms[1] ^ (long)parms[0],                      6) },
            { new Token("&",  TokenType.Binary),    new OperationDef(parms => (long)parms[1] & (long)parms[0],                      7) },
            { new Token("<=>",TokenType.Binary),    new OperationDef(parms => Comparer<double>.Default.Compare(parms[1], parms[0]), 8) },
            { new Token("!=", TokenType.Binary),    new OperationDef(parms => !parms[1].AlmostEquals(parms[0])?1:0,                 9) },
            { new Token("==", TokenType.Binary),    new OperationDef(parms => parms[1].AlmostEquals(parms[0])?1:0,                  9) },
            { new Token("<",  TokenType.Binary),    new OperationDef(parms => parms[1]  < parms[0] ? 1 : 0,                        10) },
            { new Token("<=", TokenType.Binary),    new OperationDef(parms => parms[1] <= parms[0] ? 1 : 0,                        10) },
            { new Token(">=", TokenType.Binary),    new OperationDef(parms => parms[1] >= parms[0] ? 1 : 0,                        10) },
            { new Token(">",  TokenType.Binary),    new OperationDef(parms => parms[1]  > parms[0] ? 1 : 0,                        10) },
            { new Token("<<", TokenType.Binary),    new OperationDef(parms => (int)parms[1] << (int)parms[0],                      11) },
            { new Token(">>", TokenType.Binary),    new OperationDef(parms => (int)parms[1] >> (int)parms[0],                      11) },
            { new Token("-",  TokenType.Binary),    new OperationDef(parms => parms[1] - parms[0],                                 12) },
            { new Token("+",  TokenType.Binary),    new OperationDef(parms => parms[1] + parms[0],                                 12) },
            { new Token("/",  TokenType.Binary),    new OperationDef(parms => parms[0]==0?throw new DivideByZeroException():parms[1]/parms[0],13) },
            { new Token("*",  TokenType.Binary),    new OperationDef(parms => parms[1] * parms[0],                                 13) },
            { new Token("%",  TokenType.Binary),    new OperationDef(parms => (long)parms[1] % (long)parms[0],                     13) },
            { new Token("^^", TokenType.Binary),    new OperationDef(parms => Math.Pow(parms[1], parms[0]),                        14) },
            { new Token("~",  TokenType.Unary),     new OperationDef(parms => ~((long)parms[0]),                                   15) },
            { new Token("!",  TokenType.Unary),     new OperationDef(parms => parms[0].AlmostEquals(0) ? 1 : 0,                    15) },
            { new Token("-",  TokenType.Unary),     new OperationDef(parms => -parms[0],                                           15) },
            { new Token("+",  TokenType.Unary),     new OperationDef(parms => +parms[0],                                           15) }
        };
        static readonly CultureInfo s_info;
        const NumberStyles AsmNumberStyle = NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowThousands | NumberStyles.AllowExponent;
        static readonly Random s_rng = new Random();
        readonly IReadOnlyDictionary<StringView, OperationDef> _functions;
        readonly List<IFunctionEvaluator> _functionEvaluators;
        readonly Dictionary<StringView, double> _constants;

        #endregion

        #region Constructors

        static Evaluator()
        {
            s_info = CultureInfo.CreateSpecificCulture("en-US");
            var nfi = s_info.NumberFormat;
            nfi.NumberGroupSeparator = "_";
        }

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
                { "sizeof", new OperationDef(parms => parms[0].Size(),                                          1) },
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
        public bool ExpressionIsCondition(RandomAccessIterator<Token> tokens)
            => ExpressionIsCondition(tokens, true);

        /// <summary>
        /// Determines whether the collection of parsed tokens comprise a conditional
        /// expression that evaluates to <c>true</c> or <c>false</c>.
        /// </summary>
        /// <param name="tokens">The collection of parsed tokens representing the expression.</param>
        /// <param name="restoreIterator">Restore the iterator to its position before testing condition.</param>
        /// <returns><c>true</c> if the expression is conditional, <c>false</c> otherwise.</returns>
        public bool ExpressionIsCondition(RandomAccessIterator<Token> tokens, bool restoreIterator) 
        {
            var firstIndex = tokens.Index;
            bool booleanNeeded = true;
            if (tokens.Current?.Type == TokenType.Open)
            {
                tokens.MoveNext();
                booleanNeeded = !ExpressionIsCondition(tokens, false);
            }
            Token token = tokens.GetNext();
            if ((!booleanNeeded && (Token.IsTerminal(token) || token.Type == TokenType.Ternary)) || 
                (!Token.IsTerminal(token) && token.ValueType == ValueType.Boolean && (Token.IsTerminal(tokens.PeekNext()) || tokens.PeekNext().Type == TokenType.Ternary)))
            {
                if (restoreIterator)
                    tokens.SetIndex(firstIndex);
                return true;
            }
            bool comparerFound = false, comparerNeeded = false;
            while (!Token.IsTerminal(token) && token.Type != TokenType.Ternary)
            {
                if (token.Type == TokenType.Operand && booleanNeeded && !comparerNeeded)
                {
                    if (token.ValueType == ValueType.Boolean)
                    {
                        booleanNeeded = false;
                    }
                    else if (token.Name[0] == '_' || char.IsLetter(token.Name[0]))
                    {
                        var operands = new List<Token> { token };
                        if (tokens.PeekNext() != null && tokens.PeekNext().Name.Equals("["))
                        {
                            var ix = tokens.Index;
                            tokens.MoveNext();
                            operands.AddRange(Token.GetGroup(tokens));
                            tokens.SetIndex(ix + operands.Count - 1);
                        }
                        var eval = Evaluate(operands.GetIterator());
                        if (eval == 0 || eval == 1)
                            booleanNeeded = false;
                        else
                            comparerNeeded = true;
                    }
                    else
                    {
                        comparerNeeded = true;
                    }
                }
                else if ((token.Type.HasFlag(TokenType.Unary) && token.Name.Equals("!")) ||
                         token.Name.Equals("("))
                {
                    var isSubCondition = ExpressionIsCondition(tokens, false);
                    if (booleanNeeded && !comparerNeeded && isSubCondition)
                        booleanNeeded = false;
                    else
                        comparerNeeded = true;
                }
                else if (token.Type == TokenType.Radix || token.Type == TokenType.Unary || token.Type == TokenType.Function)
                {
                    comparerNeeded = true;
                    if (token.Type == TokenType.Radix || token.Type == TokenType.Function)
                    {
                        tokens.MoveNext();
                        if (token.Type == TokenType.Function)
                            _ = Token.GetGroup(tokens);
                    }
                }
                else if (token.Type == TokenType.Binary)
                {
                    if (s_conditionals.Contains(token.Name))
                        comparerFound = true;
                    else
                        comparerNeeded = true;
                }
                token = tokens.GetNext();
            }
            if (restoreIterator)
                tokens.SetIndex(firstIndex);
            return (!booleanNeeded && !comparerNeeded) || comparerFound;
        }

        Stack<double> EvaluateInternal(RandomAccessIterator<Token> iterator, bool fromGroup = false)
        {
            var results = new Stack<double>();
            var operators = new Stack<Token>();
            var lastType = TokenType.None;
            StringView lastToken = "";
            var index = iterator.Index; // in case we need to backtrack
            Token token;
            var end = TokenType.Terminal;
            while ((token = iterator.GetNext()) != null && !(token.Type == TokenType.Closed || (!fromGroup && end.HasFlag(token.Type))))
            {
                if (token.Type == TokenType.Operand || token.Type == TokenType.Radix)
                {
                    if (token.Type == TokenType.Operand && token.ValueType == ValueType.Unknown)
                    {
                        results.Push(UnknownValueEvaluator(iterator));
                        continue;
                    }
                    if (token.Type == TokenType.Radix)
                    {
                        if (Token.IsTerminal(iterator.PeekNext()) || iterator.PeekNext().ValueType == ValueType.Unknown)
                            throw new ExpressionException(token, "Expected value.");
                        token = iterator.GetNext();
                    }
                    results.Push((double)token.Value);
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
                        if (token.Type != TokenType.Function && !s_operators.ContainsKey(token))
                            throw new SyntaxException(token, "Unexpected expression.");
                        if (!TokenType.Evaluation.HasFlag(token.Type))
                            return new Stack<double>();
                        if (token.Type == TokenType.Ternary && token.Name.Equals("?"))
                        {
                            if (iterator.PeekNext() == null)
                                throw new SyntaxException(token, "Invalid ternary expression.");
                            var iterCopy = new RandomAccessIterator<Token>(iterator, index);
                            if (!ExpressionIsCondition(iterCopy))
                                throw new SyntaxException(token, "Invalid ternary expression.");
                            while (operators.Count > 0)
                                DoOperation(operators.Pop(), results);
                            var subExpressions = EvaluateInternal(iterator, false);
                            foreach (var subEx in subExpressions)
                                results.Push(subEx);
                            iterator.Rewind(iterator.Index - 1);
                            DoOperation(token, results);
                        }
                        else if (operators.Count > 0)
                        {
                            var top = operators.Peek();
                            if (top.Name.Equals("!") && !token.Name.Equals("||") && !token.Name.Equals("&&"))
                                throw new SyntaxException(token, "Unexpected expression.");
                            var opOrder = token.Type == TokenType.Function ? int.MaxValue : s_operators[token].Item2;
                            while ((top.Type == TokenType.Function || s_operators[top].Item2 >= opOrder) && operators.Count > 0)
                            {
                                operators.Pop();
                                DoOperation(top, results);
                                if (operators.Count > 0)
                                    top = operators.Peek();
                            }
                        }
                        if (token.Type == TokenType.Separator)
                            index = iterator.Index;
                        else if (token.Type != TokenType.Ternary)
                            operators.Push(token);
                    }
                }
            }   
            while (operators.Count > 0)
                DoOperation(operators.Pop(), results);

            return results;
        }

        void DoOperation(Token op, Stack<double> output)
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
                else if (op.Type == TokenType.Ternary)
                    parmCount += 2;
            }
            while (--parmCount >= 0)
            {
                if (output.Count == 0)
                {
                    var opType = op.Type == TokenType.Function ? "function" : "operator";
                    throw new SyntaxException(op, $"Missing arguments for {opType} \"{op}\".");
                }
                parms.Add(output.Pop());
            }
            if ((op.Name.Equals("||") || op.Name.Equals("&&") || op.Name.Equals("!")) && parms.Any(p => p != 1 && p != 0))
                throw new ExpressionException(op, "Invalid conditional expression.");
            output.Push(operation.Item1(parms));
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
                if (!ExpressionIsCondition(new RandomAccessIterator<Token>(tokens, ix)))
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
        /// Get the value from the given <see cref="StringView"/> or pair of <see cref="StringView"/> values.
        /// </summary>
        /// <param name="radix">A radix expression preceding the name.</param>
        /// <param name="name">The name.</param>
        /// <param name="comparer">A comparer.</param>
        /// <returns>A tuple containing the value type (if known) and the boxed value.</returns>
        public static (ValueType valueType, double value) GetValue(StringView radix, StringView name, StringViewComparer comparer)
        {
            if (radix != null)
                return (ValueType.Binary, s_radixOperators[radix](name));
            if (!double.TryParse(name.ToString(), AsmNumberStyle, s_info, out var converted))
            {
                if (name[0] == '0' && name.Length > 2)
                {
                    // try to convert non-base 10 literals in 0x/0b/0o notation.
                    try
                    {
                        var numBase = name[1] switch
                        {
                            'b' or 'B' => 2,
                            'o' or 'O' => 8,
                            'x' or 'X' => 16,
                            _          => throw new ArgumentException("Not a valid numeric constant.")
                        };
                        return (ValueType.Binary, Convert.ToInt64(name.Substring(2).Replace("_", ""), numBase));
                    }
                    catch
                    {
                        throw new ArgumentException("Not a valid numeric constant.");
                    }
                }
                if (name.Equals("false", comparer) || name.Equals("true", comparer))
                    return (ValueType.Boolean, name.Equals("true", comparer) ? 1 : 0);
                return (ValueType.Unknown, double.NaN);
            }
            if (name[0] == '0' &&
                name.Length > 1 &&
                (name[1] >= '0' && name[1] <= '7') ||
                (name.Length > 3 && name[1] == '_' && name[2] >= '0' && name[2] <= '7'))
            {
                // all leading zeros treated as octal
                try
                {
                    return (ValueType.Boolean, Convert.ToInt64(name.ToString().Replace("_", string.Empty), 8));
                }
                catch
                {
                    throw new ArgumentException("Not a valid numeric constant.");
                }
            }
            if (converted.IsInteger())
                return (ValueType.Integer, converted);
            return (ValueType.Double, converted);
        }

        /// <summary>
        /// Evaluates a parsed tokens as a mathematical expression.
        /// </summary>
        /// <param name="token">The parsed token representing the expression.</param>
        /// <param name="minValue">The minimum value allowed in the evaluation.</param>
        /// <param name="maxValue">The maximum value allowed in the evaluation.</param>
        /// <param name="allowSymbolEvaluation">Allow symbol evaluation to occur if 
        /// the token is not successfully parsed.</param>
        /// <returns>The result of the evaluation.</returns>
        /// <exception cref="DivideByZeroException"></exception>
        /// <exception cref="ExpressionException"></exception>
        /// <exception cref="IllegalQuantityException>"></exception>
        /// <exception cref="SyntaxException"></exception>
        public double Evaluate(Token token, double minValue, double maxValue)
        {
            double converted;
            if (token.ValueType == ValueType.Unknown)
            {
                var tokens = new List<Token> { token };
                var it = tokens.GetIterator();
                it.MoveNext();
                converted = UnknownValueEvaluator(it);
            }
            else
            {
                converted = (double)token.Value;
            }
            if (converted < minValue || converted > maxValue)
                throw new IllegalQuantityException(token, converted);
            return converted;
        }

        /// <summary>
        /// Evaluates a collection of parsed tokens as a conditional expression.
        /// </summary>
        /// <param name="tokens">An iterator to the collection of parsed tokens representing the expression.</param>
        /// <returns><c>true</c> if the condition evaluated to true, <c>false</c> otherwise.</returns>
        /// <param name="advanceIterator">Advances the iterator before beginning evaluation.</param>
        /// <returns><c>true</c> if the condition evaluated to true, <c>false</c> otherwise.</returns>
        /// <exception cref="DivideByZeroException"></exception>
        /// <exception cref="ExpressionException"></exception>
        /// <exception cref="IllegalQuantityException>"></exception>
        /// <exception cref="SyntaxException"></exception>
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
                             .Replace('.', '0')
                             .Replace("_", string.Empty);
            return binary.Replace("_", string.Empty);
        }

        #endregion

        #region Properties

        /// <summary>
        /// An evaluator of symbols the evaluator does not recognize
        /// </summary>
        public Func<RandomAccessIterator<Token>, double> UnknownValueEvaluator { get; set; }

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