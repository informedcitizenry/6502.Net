//-----------------------------------------------------------------------------
// Copyright (c) 2017-2020 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using ConversionDef = System.Func<string, double>;
using OperationDef = System.Tuple<System.Func<System.Collections.Generic.List<double>, double>, int>;

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

        static readonly HashSet<string> s_conditionals = new HashSet<string>
        {
            "||", "&&", "<", "<=", ">", ">=", "==", "!="
        };

        static readonly HashSet<string> s_comparers = new HashSet<string>
        {
            "<", "<=", "!=", "==", ">=", ">"
        };

        static readonly IReadOnlyDictionary<string, ConversionDef> s_radixOperators = new Dictionary<string, ConversionDef>
        {
            { "$", new ConversionDef(parm => Convert.ToInt64(parm, 16)) },
            { "%", new ConversionDef(parm => Convert.ToInt64(parm,  2)) }
        };
        static readonly IReadOnlyDictionary<Token, OperationDef> s_operators = new Dictionary<Token, OperationDef>
        {
            {
                new Token("||", TokenType.Operator, OperatorType.Binary),
                new OperationDef(parms => (!parms[1].AlmostEquals(0)?1:0) | (!parms[0].AlmostEquals(0)?1:0),  0)
            },
            {
                new Token("&&", TokenType.Operator, OperatorType.Binary),
                new OperationDef(parms => (!parms[1].AlmostEquals(0)?1:0) & (!parms[0].AlmostEquals(0)?1:0),  1)
            },
            {
                new Token("|", TokenType.Operator, OperatorType.Binary),
                new OperationDef(parms => (long)parms[1]                | (long)parms[0],      2)
            },
            {
                new Token("^", TokenType.Operator, OperatorType.Binary),
                new OperationDef(parms => (long)parms[1]                ^ (long)parms[0],      3)
            },
            {
                new Token("&", TokenType.Operator, OperatorType.Binary),
                new OperationDef(parms => (long)parms[1]                & (long)parms[0],      4)
            },
            {
                new Token("!=", TokenType.Operator, OperatorType.Binary),
                new OperationDef(parms => !parms[1].AlmostEquals(parms[0])          ? 1 : 0,   5)
            },
            {
                new Token("==", TokenType.Operator, OperatorType.Binary),
                new OperationDef(parms => parms[1].AlmostEquals(parms[0])           ? 1 : 0,   5)
            },
            {
                new Token("<", TokenType.Operator, OperatorType.Binary),
                new OperationDef(parms => parms[1]                      <  parms[0] ? 1 : 0,   6)
            },
            {
                new Token("<=", TokenType.Operator, OperatorType.Binary),
                new OperationDef(parms => parms[1]                      <= parms[0] ? 1 : 0,   6)
            },
            {
                new Token(">=", TokenType.Operator, OperatorType.Binary),
                new OperationDef(parms => parms[1]                      >= parms[0] ? 1 : 0,   6)
            },
            {
                new Token(">", TokenType.Operator, OperatorType.Binary),
                new OperationDef(parms => parms[1]                      >  parms[0] ? 1 : 0,   6)
            },
            {
                new Token("<<", TokenType.Operator, OperatorType.Binary),
                new OperationDef(parms => (int)parms[1]                 << (int)parms[0],      7)
            },
            {
                new Token(">>", TokenType.Operator, OperatorType.Binary),
                new OperationDef(parms => (int)parms[1]                 >> (int)parms[0],      7)
            },
            {
                new Token("-", TokenType.Operator, OperatorType.Binary),
                new OperationDef(parms => parms[1]                      -  parms[0],           8)
            },
            {
                new Token("+", TokenType.Operator, OperatorType.Binary),
                new OperationDef(parms => parms[1]                      +  parms[0],           8)
            },
            {
                new Token("/", TokenType.Operator, OperatorType.Binary),
                new OperationDef(delegate(List<double> parms)
                {
                    if (parms[0].AlmostEquals(0)) throw new DivideByZeroException();
                    return parms[1] / parms[0];
                },                                                                             9)
            },
            {
                new Token("*", TokenType.Operator, OperatorType.Binary),
                new OperationDef(parms => parms[1]                      *  parms[0],           9)
            },
            {
                new Token("%", TokenType.Operator, OperatorType.Binary),
                new OperationDef(parms => (long)parms[1]                %  (long)parms[0],     9)
            },
            {
                new Token("^^", TokenType.Operator, OperatorType.Binary),
                new OperationDef(parms => Math.Pow(parms[1], parms[0]),                       10)
            },
            {
                new Token("~", TokenType.Operator, OperatorType.Unary),
                new OperationDef(parms => ~((long)parms[0]),                                  11)
            },
            {
                new Token("!", TokenType.Operator, OperatorType.Unary),
                new OperationDef(parms => parms[0].AlmostEquals(0) ? 1 : 0,                   11)
            },
            {
                new Token("&", TokenType.Operator, OperatorType.Unary),
                new OperationDef(parms => (long)parms[0]  & 65535,                            11)
            },
            {
                new Token("^", TokenType.Operator, OperatorType.Unary),
                new OperationDef(parms => ((long)parms[0] & UInt24.MaxValue) / 0x10000,       11)
            },
            {
                new Token("-", TokenType.Operator, OperatorType.Unary),
                new OperationDef(parms => -parms[0],                                          11)
            },
            {
                new Token("+", TokenType.Operator, OperatorType.Unary),
                new OperationDef(parms => +parms[0],                                          11)
            },
            {
                new Token(">", TokenType.Operator, OperatorType.Unary),
                new OperationDef(parms => ((long)parms[0] & 65535) / 0x100,                   11)
            },
            {
                new Token("<", TokenType.Operator, OperatorType.Unary),
                new OperationDef(parms => (long)parms[0]  & 255,                              11)
            },
            {
                new Token("$", TokenType.Operator, OperatorType.Unary),
                new OperationDef(parms => parms[0],                                           11)
            },
            {
                new Token("%", TokenType.Operator, OperatorType.Unary),
                new OperationDef(parms => parms[0],                                           11)
            }
        };
        static readonly Random s_rng = new Random();

        static readonly IReadOnlyDictionary<string, OperationDef> s_functions = new Dictionary<string, OperationDef>
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

        readonly List<IFunctionEvaluator> _functionEvaluators;
        readonly Func<Token, Token, double> _symbolEvaluator;
        readonly Dictionary<string, double> _constants;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new instance of the expression evaluator.
        /// </summary>
        /// <param name="caseSensitive">Sets whether constants should be treated as
        /// case sensitive or not.</param>
        /// <param name="symbolEvaluator">An evaluator of symbols the evaluator does
        /// not recognize.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public Evaluator(bool caseSensitive, Func<Token, Token, double> symbolEvaluator) 
        {
            _functionEvaluators = new List<IFunctionEvaluator>();
            var comparer = caseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
            _symbolEvaluator = symbolEvaluator ?? throw new ArgumentNullException();
            _constants = new Dictionary<string, double>(comparer)
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
        }

        #endregion

        #region Methods

        /// <summary>
        /// Determines if the tokenized expression is constant.
        /// </summary>
        /// <param name="token">The token expression.</param>
        /// <returns><c>true</c> if the expression is a constant, otherwise <c>false</c>.</returns>
        public bool ExpressionIsConstant(Token token)
        {
            var tokenAsString = token.ToString();
            if (string.IsNullOrEmpty(tokenAsString))
                return false;
            if (tokenAsString[0] == ',')
                if (tokenAsString.Length < 2)
                    return false;
                else
                    tokenAsString = tokenAsString.Substring(1);
            var name = tokenAsString.Replace("%", "0b")
                                    .Replace("$", "0x")
                                    .Trim()
                                    .TrimStart('(')
                                    .TrimEnd(')');
            try { _ = EvaluateAtomic(new Token(name, TokenType.Operand)); }
            catch { return false; }
            return true;
        }

        double EvaluateAtomic(Token token)
        {
            if (token.Type != TokenType.Operand)
                throw new SyntaxException(token.Position, 
                    $"Invalid operand \"{token.Name}\" encountered.");

            // is the operand a string representation of a numerical value?
            if (!double.TryParse(token.Name, out var converted))
            {
                if (token.Name[0] == '0' && token.Name.Length > 2)
                {
                    // try to convert non-base 10 literals in 0x/0b/0o notation.
                    try
                    {
                        if (token.Name[1] == 'b' || token.Name[1] == 'B')
                            converted = Convert.ToInt64(token.Name.Substring(2), 2);
                        else if (token.Name[1] == 'o' || token.Name[1] == 'O')
                            converted = Convert.ToInt64(token.Name.Substring(2), 8);
                        else if (token.Name[1] == 'x' || token.Name[1] == 'X')
                            converted = Convert.ToInt64(token.Name.Substring(2), 16);
                        else
                            throw new ExpressionException(token.Position,
                                $"\"{token.Name}\" is not a valid numeric constant.");
                    }
                    catch
                    {
                        throw new ExpressionException(token.Position,
                            $"\"{token.Name}\" is not a valid numeric constant.");
                    }
                }
                else if (!_constants.TryGetValue(token.Name, out converted))
                {
                    converted = _symbolEvaluator(token, null);
                }
            }
            else if (token.Name[0] == '0' &&
                     token.Name.Length > 1 &&
                     token.Name[1] != '.' &&
                     token.Name[1] != 'e' &&
                     token.Name[1] != 'E')
            {
                // all leading zeros treated as octal
                try
                {
                    converted = Convert.ToInt64(token.Name, 8);
                }
                catch
                {
                    throw new ExpressionException(token.Position,
                        $"\"{token.Name}\" is not a valid numeric constant.");
                }
            }
            return converted;
        }

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
            if (tokenList.Count == 2 && tokenList[0].Name.Equals("!") && tokenList[0].OperatorType == OperatorType.Unary)
                return ExpressionIsCondition(tokenList.Skip(1));
            for (var i = 0; i < tokenList.Count; i++)
            {
                var token = tokenList[i];
                if (token.OperatorType == OperatorType.Binary)
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
                else if (token.Children.Count > 0)
                {
                    if (ExpressionIsCondition(token.Children))
                    {
                        if (i > 0 && 
                            tokenList[i - 1].OperatorType == OperatorType.Unary && 
                            tokenList[i - 1].Name[0] != '!')
                                return false;
                        if (lastBinary > lastLogicalFound)
                            return false;
                        lastWasCompound = true;
                        lastComparerFound = i;
                    }
                }
            }
            return lastComparerFound > -1 && lastComparerFound > lastLogicalFound;
        }

        Stack<double> EvaluateInternal(IEnumerable<Token> tokens)
        {
            var result = new Stack<double>();
            var operands = new Stack<string>();
            var operators = new Stack<Token>();
            var lastType = OperatorType.None;
            var lastToken = string.Empty;
            var nonBase10 = string.Empty;
            var iterator = tokens.GetIterator();
            Token token;
            while ((token = iterator.GetNext()) != null)
            {
                if (token.Type == TokenType.Operand)
                {
                    if (lastType == OperatorType.Unary && s_radixOperators.ContainsKey(lastToken))
                    {
                        if (!string.IsNullOrEmpty(nonBase10))
                            throw new SyntaxException(iterator.Current.Position, $"Unexpected token {token.Name}.");
                        nonBase10 = token.Name;
                    }
                    else if (iterator.PeekNext() != null && iterator.PeekNext().Name == "[")
                    {
                        var value = _symbolEvaluator(token, iterator.GetNext());
                        if (double.IsNegativeInfinity(value))
                            throw new ExpressionException(iterator.Current, "Index is out of range.");
                        result.Push(value);
                    }
                    else
                    {
                        result.Push(EvaluateAtomic(token));
                    }
                }
                else if (token.Type == TokenType.Operator)
                {
                    lastType = token.OperatorType;
                    lastToken = token.Name;
                    if (token.Children.Count > 0)
                    {
                        var subResults = EvaluateInternal(token.Children);
                        foreach (var sr in subResults)
                            result.Push(sr);
                        if (lastToken.IsByteExtractor())
                            operators.Push(token);
                    }
                    else if (token.OperatorType == OperatorType.Function && !s_functions.ContainsKey(lastToken))
                    {
                        var fe = _functionEvaluators.FirstOrDefault(fe => fe.EvaluatesFunction(token));
                        if (fe == null)
                            throw new SyntaxException(token.Position, $"Unknown function \"{lastToken}\".");
                        result.Push(fe.EvaluateFunction(token, iterator.GetNext()));
                    }
                    else if (token.OperatorType == OperatorType.Unary)
                    {
                        operators.Push(token);
                    }
                    else
                    {
                        if (operators.Count > 0)
                        {
                            if (!s_operators.ContainsKey(token) && !s_functions.ContainsKey(lastToken))
                                throw new SyntaxException(token.Position, $"Unknown operator.");

                            Token top = operators.Peek();
                            var opOrder = token.OperatorType == OperatorType.Function ? int.MaxValue : s_operators[token].Item2;

                            while ((top.OperatorType == OperatorType.Function || s_operators[top].Item2 >= opOrder) && operators.Count > 0)
                            {
                                operators.Pop();
                                DoOperation(top);
                                if (operators.Count > 0)
                                    top = operators.Peek();
                            }
                        }
                        if (token.OperatorType != OperatorType.Function && !s_operators.ContainsKey(token))
                            throw new SyntaxException(token.Position, "Invalid expression.");
                        operators.Push(token);
                    }
                }
                else
                {
                    throw new SyntaxException(token.Position, $"Invalid expression.");
                }
            }
            while (operators.Count > 0)
                DoOperation(operators.Pop());
            return result;

            void DoOperation(Token op)
            {
                if (!string.IsNullOrEmpty(nonBase10))
                {
                    try
                    {
                        result.Push(s_radixOperators[op.Name](nonBase10));
                        nonBase10 = string.Empty;
                    }
                    catch
                    {
                        throw new ExpressionException(op.Position, $"\"{op.Name}{nonBase10}\" is not a valid expression.");
                    }
                }
                else
                {
                    OperationDef operation;
                    var parms = new List<double>();
                    var parmCount = 1;
                    if (op.OperatorType == OperatorType.Function)
                    {
                        operation = s_functions[op.Name];
                        parmCount = operation.Item2;
                    }
                    else
                    {
                        if (!s_operators.ContainsKey(op))
                            throw new SyntaxException(op.Position, $"Invalid expression \"{op.Name}\".");
                        operation = s_operators[op];
                        if (op.OperatorType == OperatorType.Binary)
                            parmCount++;
                    }
                    while (parmCount-- >= parms.Count)
                    {
                        if (result.Count == 0)
                        {
                            var opType = op.OperatorType == OperatorType.Function ? "function" : "operator";
                            throw new SyntaxException(op.Position, $"Missing operand argument for {opType} \"{op.Name}\".");
                        }
                        parms.Add(result.Pop());
                    }
                    result.Push(operation.Item1(parms));
                }
            }
        }

        double DoEvaluation(IEnumerable<Token> tokens, double minValue, double maxValue, bool isMath)
        {
            var result = EvaluateInternal(tokens);
            if (result.Count != 1)
                throw new SyntaxException(tokens.Last().LastChild.Position,
                    $"Unexpected expression found: \"{tokens.Last().LastChild}\".");

            var r = result.Pop();
            if (r != 0 && !double.IsNormal(r))
            {
                if (isMath)
                    return 0xffff;
                else
                    return 0;
            }
            if (isMath && (r < minValue || r > maxValue))
                throw new IllegalQuantityException(tokens.First(), r);
            else if (!isMath && !ExpressionIsCondition(tokens))
                throw new ExpressionException(tokens.First().Position, "Invalid conditional expression.");
            return r;
        }

        /// <summary>
        /// Invoke a function call, discarding the return value.
        /// </summary>
        /// <param name="function">The parsed token representing the function call.</param>
        /// <exception cref="SyntaxException"></exception>
        public void Invoke(Token function, Token parms)
        {
            if (s_functions.ContainsKey(function.Name))
                return;
            var fe = _functionEvaluators.FirstOrDefault(fe => fe.EvaluatesFunction(function));
            if (fe == null)
                throw new SyntaxException(function.Position, $"Unknown function \"{function}\".");
            fe.InvokeFunction(function, parms);
        }

        /// <summary>
        /// Evaluates a parsed token as a mathematical expression.
        /// </summary>
        /// <param name="token">The parsed token representing the expression.</param>
        /// <returns>The result of the evaluation.</returns>
        /// <exception cref="DivideByZeroException"></exception>
        /// <exception cref="ExpressionException"></exception>
        /// <exception cref="IllegalQuantityException>"></exception>
        /// <exception cref="SyntaxException"></exception>
        public double Evaluate(Token token) => Evaluate(token, int.MinValue, uint.MaxValue);

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
        public double Evaluate(Token token, long minValue, long maxValue)
        {
            if (token.Children.Count == 0)
            {
                var atomic = EvaluateAtomic(token);
                if (atomic < minValue || atomic > maxValue)
                    throw new IllegalQuantityException(token, atomic);
                return atomic;
            }
            return Evaluate(token.Children, minValue, maxValue);
        }

        /// <summary>
        /// Evaluates a collection of parsed tokens as a mathematical expression.
        /// </summary>
        /// <param name="tokens">The collection of parsed tokens representing the expression.</param>
        /// <param name="minValue">The minimum value allowed in the evaluation.</param>
        /// <param name="maxValue">The maximum value allowed in the evaluation.</param>
        /// <returns>The result of the evaluation.</returns>
        /// <exception cref="DivideByZeroException"></exception>
        /// <exception cref="ExpressionException"></exception>
        /// <exception cref="IllegalQuantityException>"></exception>
        /// <exception cref="SyntaxException"></exception>
        public double Evaluate(IEnumerable<Token> tokens) => Evaluate(tokens, int.MinValue, uint.MaxValue);

        /// <summary>
        /// Evaluates a collection of parsed tokens as a mathematical expression.
        /// </summary>
        /// <param name="tokens">The collection of parsed tokens representing the expression.</param>
        /// <param name="minValue">The minimum value allowed in the evaluation.</param>
        /// <param name="maxValue">The maximum value allowed in the evaluation.</param>
        /// <returns>The result of the evaluation.</returns>
        /// <exception cref="DivideByZeroException"></exception>
        /// <exception cref="ExpressionException"></exception>
        /// <exception cref="IllegalQuantityException>"></exception>
        /// <exception cref="SyntaxException"></exception>
        public double Evaluate(IEnumerable<Token> tokens, long minValue, long maxValue)
            => DoEvaluation(tokens, minValue, maxValue, true);

        /// <summary>
        /// Evaluates a collection of parsed tokens as a mathematical expression.
        /// </summary>
        /// <param name="tokens">The collection of parsed tokens representing the expression.</param>
        /// <param name="minValue">The minimum value allowed in the evaluation.</param>
        /// <param name="maxValue">The maximum value allowed in the evaluation.</param>
        /// <returns>The result of the evaluation.</returns>
        /// <exception cref="DivideByZeroException"></exception>
        /// <exception cref="ExpressionException"></exception>
        /// <exception cref="IllegalQuantityException>"></exception>
        /// <exception cref="SyntaxException"></exception>
        public double Evaluate(IEnumerable<Token> tokens, double minValue, double maxValue)
            => DoEvaluation(tokens, minValue, maxValue, true);

        /// <summary>
        /// Evaluates a string as a mathematical expression.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <returns>The result of the evaluation.</returns>
        /// <exception cref="DivideByZeroException"></exception>
        /// <exception cref="ExpressionException"></exception>
        /// <exception cref="IllegalQuantityException>"></exception>
        /// <exception cref="SyntaxException"></exception>
        public double Evaluate(string expression)
            => EvaluateAtomic(new Token(expression, TokenType.Operand));

        /// <summary>
        /// Evaluates a collection of parsed tokens as a conditional expression.
        /// </summary>
        /// <param name="tokens">The collection of parsed tokens representing the expression.</param>
        /// <returns><c>true</c> if the condition evaluated to true, <c>false</c> otherwise.</returns>
        /// <exception cref="DivideByZeroException"></exception>
        /// <exception cref="ExpressionException"></exception>
        /// <exception cref="IllegalQuantityException>"></exception>
        /// <exception cref="SyntaxException"></exception>
        public bool EvaluateCondition(IEnumerable<Token> tokens)
            => DoEvaluation(tokens, 0, 1, false) == 1;

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
            if (token.Children.Count == 0)
                return EvaluateAtomic(token).AlmostEquals(1);
            return EvaluateCondition(token.Children);
            
        }

        /// <summary>
        /// Determines if the symbol is a reserved word for the Evaluator.
        /// </summary>
        /// <param name="symbol">The symbol name.</param>
        /// <returns><c>true</c> if the specified token is in the collection of reserved words,
        /// regardless of type, otherwise <c>false</c>.</returns>
        public bool IsReserved(string symbol)
            => _constants.ContainsKey(symbol)  ||
               s_functions.ContainsKey(symbol) ||
               _functionEvaluators.Any(fe => fe.IsFunctionName(symbol));

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