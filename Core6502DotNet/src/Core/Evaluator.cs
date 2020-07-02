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
        public IllegalQuantityException(Token token)
            : base(token.Position, $"Illegal quantity in expression \"{token.Name}\".")
        {

        }
    }

    /// <summary>
    /// A class that can evaluate strings as mathematical expressions.
    /// </summary>
    public static class Evaluator
    {
        #region Members

        static readonly HashSet<string> _restrictedWords = new HashSet<string>
        {
            "true", "false", "MATH_PI", "MATH_E",
            "UINT32_MAX", "UINT32_MIN", "INT32_MAX", "INT32_MIN",
            "UINT24_MAX", "UINT24_MIN", "INT24_MAX", "INT24_MIN",
            "UINT16_MAX", "UINT16_MIN", "INT16_MAX", "INT16_MIN",
            "UINT8_MAX", "UINT8_MIN", "INT8_MAX", "INT8_MIN"
        };

        static readonly Dictionary<string, ConversionDef> _radixOperators = new Dictionary<string, ConversionDef>
        {
            { "$", new ConversionDef(parm => Convert.ToInt64(parm, 16)) },
            { "%", new ConversionDef(parm => Convert.ToInt64(parm,  2)) }
        };
        static readonly Dictionary<Token, OperationDef> _operators = new Dictionary<Token, OperationDef>
        {
            {
                new Token{ Name = "||", Type = TokenType.Operator, OperatorType = OperatorType.Binary },
                new OperationDef(parms => ((int)parms[1]!=0?1:0) | ((int)parms[0]!=0? 1 : 0),  0)
            },
            {
                new Token{ Name = "&&", Type = TokenType.Operator, OperatorType = OperatorType.Binary },
                new OperationDef(parms => ((int)parms[1]!=0?1:0) & ((int)parms[0]!=0? 1 : 0),  1)
            },
            {
                new Token{ Name = "|",  Type = TokenType.Operator, OperatorType = OperatorType.Binary },
                new OperationDef(parms => (long)parms[1]                | (long)parms[0],      2)
            },
            {
                new Token{ Name = "^",  Type = TokenType.Operator, OperatorType = OperatorType.Binary },
                new OperationDef(parms => (long)parms[1]                ^ (long)parms[0],      3)
            },
            {
                new Token{ Name = "&",  Type = TokenType.Operator, OperatorType = OperatorType.Binary },
                new OperationDef(parms => (long)parms[1]                & (long)parms[0],      4)
            },
            {
                new Token{ Name = "!=", Type = TokenType.Operator, OperatorType = OperatorType.Binary },
                new OperationDef(parms => !parms[1].AlmostEquals(parms[0])          ? 1 : 0,   5)
            },
            {
                new Token{ Name = "==", Type = TokenType.Operator, OperatorType = OperatorType.Binary },
                new OperationDef(parms => parms[1].AlmostEquals(parms[0])           ? 1 : 0,   5)
            },
            {
                new Token{ Name = "<",  Type = TokenType.Operator, OperatorType = OperatorType.Binary },
                new OperationDef(parms => parms[1]                      <  parms[0] ? 1 : 0,   6)
            },
            {
                new Token{ Name = "<=", Type = TokenType.Operator, OperatorType = OperatorType.Binary },
                new OperationDef(parms => parms[1]                      <= parms[0] ? 1 : 0,   6)
            },
            {
                new Token{ Name = ">=", Type = TokenType.Operator, OperatorType = OperatorType.Binary },
                new OperationDef(parms => parms[1]                      >= parms[0] ? 1 : 0,   6)
            },
            {
                new Token{ Name = ">",  Type = TokenType.Operator, OperatorType = OperatorType.Binary },
                new OperationDef(parms => parms[1]                      >  parms[0] ? 1 : 0,   6)
            },
            {
                new Token{ Name = "<<", Type = TokenType.Operator, OperatorType = OperatorType.Binary },
                new OperationDef(parms => (int)parms[1]                 << (int)parms[0],      7)
            },
            {
                new Token{ Name = ">>", Type = TokenType.Operator, OperatorType = OperatorType.Binary },
                new OperationDef(parms => (int)parms[1]                 >> (int)parms[0],      7)
            },
            {
                new Token{ Name = "-",  Type = TokenType.Operator, OperatorType = OperatorType.Binary },
                new OperationDef(parms => parms[1]                      -  parms[0],           8)
            },
            {
                new Token{ Name = "+",  Type = TokenType.Operator, OperatorType = OperatorType.Binary },
                new OperationDef(parms => parms[1]                      +  parms[0],           8)
            },
            {
                new Token{ Name = "/",  Type = TokenType.Operator, OperatorType = OperatorType.Binary },
                new OperationDef(delegate(List<double> parms)
                {
                    if (parms[0] == 0) throw new DivideByZeroException();
                    return parms[1] / parms[0];
                },                                                                             9)
            },
            {
                new Token{ Name = "*",  Type = TokenType.Operator, OperatorType = OperatorType.Binary },
                new OperationDef(parms => parms[1]                      *  parms[0],           9)
            },
            {
                new Token{ Name = "%",  Type = TokenType.Operator, OperatorType = OperatorType.Binary },
                new OperationDef(parms => (long)parms[1]                %  (long)parms[0],     9)
            },
            {
                new Token{ Name = "^^", Type = TokenType.Operator, OperatorType = OperatorType.Binary },
                new OperationDef(parms => Math.Pow(parms[1], parms[0]),                       10)
            },
            {
                new Token{ Name = "~",  Type = TokenType.Operator, OperatorType = OperatorType.Unary  },
                new OperationDef(parms => ~((long)parms[0]),                                  11)
            },
            {
                new Token{ Name = "!",  Type = TokenType.Operator, OperatorType = OperatorType.Unary  },
                new OperationDef(parms => (long)parms[0] == 0 ? 1 : 0,                        11)
            },
            {
                new Token{ Name = "&",  Type = TokenType.Operator, OperatorType = OperatorType.Unary  },
                new OperationDef(parms => (long)parms[0]  & 65535,                            11)
            },
            {
                new Token{ Name = "^",  Type = TokenType.Operator, OperatorType = OperatorType.Unary  },
                new OperationDef(parms => ((long)parms[0] & UInt24.MaxValue) / 0x10000,       11)
            },
            {
                new Token{ Name = "-",  Type = TokenType.Operator, OperatorType = OperatorType.Unary  },
                new OperationDef(parms => -parms[0],                                          11)
            },
            {
                new Token{ Name = "+",  Type = TokenType.Operator, OperatorType = OperatorType.Unary  },
                new OperationDef(parms => +parms[0],                                          11)
            },
            {
                new Token{ Name = ">",  Type = TokenType.Operator, OperatorType = OperatorType.Unary  },
                new OperationDef(parms => ((long)parms[0] & 65535) / 0x100,                   11)
            },
            {
                new Token{ Name = "<",  Type = TokenType.Operator, OperatorType = OperatorType.Unary  },
                new OperationDef(parms => (long)parms[0]  & 255,                              11)
            },
            {
                new Token{ Name = "$",  Type = TokenType.Operator, OperatorType = OperatorType.Unary  },
                new OperationDef(parms => parms[0],                                           11)
            },
            {
                new Token{ Name = "%",  Type = TokenType.Operator, OperatorType = OperatorType.Unary  },
                new OperationDef(parms => parms[0],                                           11)
            }
        };
        static readonly Random _rng;
        static readonly Dictionary<string, OperationDef> _functions;
        static readonly List<IFunctionEvaluator> _functionEvaluators;

        #endregion

        #region Constructors

        static Evaluator()
        {
            _rng = new Random();

            _functions = new Dictionary<string, OperationDef>
            {
                { "abs",    new OperationDef(parms => Math.Abs(parms[0]),                                       1) },
                { "acos",   new OperationDef(parms => Math.Acos(parms[0]),                                      1) },
                { "atan",   new OperationDef(parms => Math.Atan(parms[0]),                                      1) },
                { "cbrt",   new OperationDef(parms => Math.Pow(parms[0], 1.0 / 3.0),                            1) },
                { "ceil",   new OperationDef(parms => Math.Ceiling(parms[0]),                                   1) },
                { "cos",    new OperationDef(parms => Math.Cos(parms[0]),                                       1) },
                { "cosh",   new OperationDef(parms => Math.Cosh(parms[0]),                                      1) },
                { "deg",    new OperationDef(parms => parms[0] * 180 / Math.PI,                                 1) },
                { "exp",    new OperationDef(parms => Math.Exp(parms[0]),                                       1) },
                { "floor",  new OperationDef(parms => Math.Floor(parms[0]),                                     1) },
                { "frac",   new OperationDef(parms => Math.Abs(parms[0] - Math.Abs(Math.Round(parms[0], 0))),   1) },
                { "hypot",  new OperationDef(parms => Math.Sqrt(Math.Pow(parms[0], 2) + Math.Pow(parms[1], 2)), 2) },
                { "ln",     new OperationDef(parms => Math.Log(parms[0]),                                       1) },
                { "log10",  new OperationDef(parms => Math.Log10(parms[0]),                                     1) },
                { "pow",    new OperationDef(parms => Math.Pow(parms[0], parms[1]),                             2) },
                { "rad",    new OperationDef(parms => parms[0] * Math.PI / 180,                                 1) },
                { "random", new OperationDef(parms => _rng.Next((int)parms[0], (int)parms[1]),                  2) },
                { "round",  new OperationDef(parms => Math.Round(parms[0]),                                     1) },
                { "sgn",    new OperationDef(parms => Math.Sign(parms[0]),                                      1) },
                { "sin",    new OperationDef(parms => Math.Sin(parms[0]),                                       1) },
                { "sinh",   new OperationDef(parms => Math.Sinh(parms[0]),                                      1) },
                { "sqrt",   new OperationDef(parms => Math.Sqrt(parms[0]),                                      1) },
                { "tan",    new OperationDef(parms => Math.Tan(parms[0]),                                       1) },
                { "tanh",   new OperationDef(parms => Math.Tanh(parms[0]),                                      1) }
            };

            _functionEvaluators = new List<IFunctionEvaluator>();

            Assembler.SymbolManager.DefineConstant("true", 1);
            Assembler.SymbolManager.DefineConstant("false", 0);
            Assembler.SymbolManager.DefineConstant("MATH_PI", Math.PI);
            Assembler.SymbolManager.DefineConstant("MATH_E", Math.E);
            Assembler.SymbolManager.DefineConstant("UINT32_MAX", uint.MaxValue);
            Assembler.SymbolManager.DefineConstant("INT32_MAX", int.MaxValue);
            Assembler.SymbolManager.DefineConstant("UINT32_MIN", uint.MinValue);
            Assembler.SymbolManager.DefineConstant("INT32_MIN", int.MinValue);
            Assembler.SymbolManager.DefineConstant("UINT24_MAX", UInt24.MaxValue);
            Assembler.SymbolManager.DefineConstant("INT24_MAX", Int24.MaxValue);
            Assembler.SymbolManager.DefineConstant("UINT24_MIN", UInt24.MinValue);
            Assembler.SymbolManager.DefineConstant("INT24_MIN", Int24.MinValue);
            Assembler.SymbolManager.DefineConstant("UINT16_MAX", ushort.MaxValue);
            Assembler.SymbolManager.DefineConstant("INT16_MAX", short.MaxValue);
            Assembler.SymbolManager.DefineConstant("UINT16_MIN", ushort.MinValue);
            Assembler.SymbolManager.DefineConstant("INT16_MIN", short.MinValue);
            Assembler.SymbolManager.DefineConstant("UINT8_MAX", byte.MaxValue);
            Assembler.SymbolManager.DefineConstant("INT8_MAX", sbyte.MaxValue);
            Assembler.SymbolManager.DefineConstant("UINT8_MIN", byte.MinValue);
            Assembler.SymbolManager.DefineConstant("INT8_MIN", sbyte.MinValue);

            Assembler.SymbolManager.AddValidSymbolNameCriterion(s => !_restrictedWords.Contains(s) && !_functions.ContainsKey(s));

            AddFunctionEvaluator(Assembler.SymbolManager);
        }

        #endregion

        #region Methods

        static double EvaluateAtomic(Token token)
        {
            if (token.Type != TokenType.Operand)
                throw new ExpressionException(token.Position, $"Invalid operand \"{token.Name}\" encountered.");

            // need to parse the operand token
            // is it a string representation of a numerical value?
            if (!double.TryParse(token.Name, out var converted))
            {
                // is it a string literal?
                if (token.Name.EnclosedInQuotes())
                {    // get the integral equivalent from the code points in the string
                    converted = Assembler.Encoding.GetEncodedValue(token.Name.TrimOnce(token.Name[0]));
                }
                else if (token.Name.Equals("*"))
                {    // get the program counter
                    converted = Assembler.Output.LogicalPC;
                }
                else if (token.Name[0].IsSpecialOperator())
                {    // get the special character value
                    converted = Assembler.SymbolManager.GetLineReference(token);
                }
                else if (token.Name[0] == '0' && token.Name.Length > 2)
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
                            throw new ExpressionException(token.Position, $"\"{token}\" is not a valid numeric constant.");
                    }
                    catch
                    {
                        throw new ExpressionException(token.Position, $"\"{token}\" is not a valid numeric constant.");
                    }
                }
                else
                {
                    if (char.IsDigit(token.Name[0]))
                        throw new ExpressionException(token.Position, $"\"{token}\" is not a valid real literal.");

                    // not a string number or string/char literal, must be a symbol/label
                    converted = Assembler.SymbolManager.GetNumericValue(token);
                }
                if (double.IsNaN(converted))
                    throw new ExpressionException(token.Position, $"\"{token}\" is not a expression.");
            }
            else if (token.Name[0] == '0' && token.Name.Length > 1 && token.Name[1] != 'e' && token.Name[1] != 'E')
            {
                // all leading zeros treated as octal
                try
                {
                    converted = Convert.ToInt64(token.Name, 8);
                }
                catch
                {
                    throw new ExpressionException(token.Position, $"\"{token}\" is not a valid numeric constant.");
                }
            }
            return converted;
        }

        static Stack<double> EvaluateInternal(IEnumerable<Token> tokens)
        {
            var result = new Stack<double>();
            var operands = new Stack<string>();
            var operators = new Stack<Token>();
            OperatorType lastType = OperatorType.None;
            var lastToken = string.Empty;
            var nonBase10 = string.Empty;
            RandomAccessIterator<Token> iterator = tokens.GetIterator();
            Token token;
            while ((token = iterator.GetNext()) != null)
            {
                if (token.Type == TokenType.Operand)
                {
                    if (lastType == OperatorType.Unary && _radixOperators.ContainsKey(lastToken))
                    {
                        nonBase10 = token.Name;
                    }
                    else if (iterator.PeekNext() != null && iterator.PeekNext().Name == "[")
                    {
                        var value = Assembler.SymbolManager.GetNumericVectorElementValue(token, iterator.GetNext());
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
                    lastToken = token.Name; // track for non-base 10 operands that may succeed this operation
                    if ((token.OperatorType == OperatorType.Open && token.Name.Equals("(")) ||
                         token.OperatorType == OperatorType.Separator)
                    {
                        Stack<double> subResults = EvaluateInternal(token.Children);
                        foreach (var sr in subResults)
                            result.Push(sr);
                    }
                    else if (token.OperatorType == OperatorType.Function && !_functions.ContainsKey(token.Name))
                    {
                        IFunctionEvaluator fe = _functionEvaluators.FirstOrDefault(fe => fe.EvaluatesFunction(token));
                        if (fe == null)
                            throw new ExpressionException(token.Position, $"Unknown function \"{token.Name}\".");
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
                            if (!_operators.ContainsKey(token) && !_functions.ContainsKey(token.Name))
                                throw new ExpressionException(token.Position, $"Unknown operator \"{token.Name}\".");

                            Token top = operators.Peek();
                            var opOrder = token.OperatorType == OperatorType.Function ? int.MaxValue : _operators[token].Item2;

                            while ((top.OperatorType == OperatorType.Function || _operators[top].Item2 >= opOrder) && operators.Count > 0)
                            {
                                operators.Pop();
                                DoOperation(top);
                                if (operators.Count > 0)
                                    top = operators.Peek();
                            }
                        }
                        if (token.OperatorType != OperatorType.Function && !_operators.ContainsKey(token))
                            throw new ExpressionException(token.Position, $"Invalid expression \"{token.Name}\".");
                        operators.Push(token);
                    }
                }
                else
                {
                    throw new ExpressionException(token.Position, $"Invalid expression \"{token.Name}\".");
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
                        result.Push(_radixOperators[op.Name](nonBase10));
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
                        operation = _functions[op.Name];
                        parmCount = operation.Item2;
                    }
                    else
                    {
                        if (!_operators.ContainsKey(op))
                            throw new ExpressionException(op.Position, $"Invalid expression \"{op.Name}\".");
                        operation = _operators[op];
                        if (op.OperatorType == OperatorType.Binary)
                            parmCount++;
                    }
                    while (parmCount-- >= parms.Count)
                    {
                        if (result.Count == 0)
                        {
                            var opType = op.OperatorType == OperatorType.Function ? "function" : "operator";
                            throw new ExpressionException(op.Position, $"Missing operand argument for {opType} \"{op.Name}\".");
                        }
                        parms.Add(result.Pop());
                    }
                    result.Push(operation.Item1(parms));
                }
            }
        }

        static double DoEvaluation(IEnumerable<Token> tokens, double minValue, double maxValue, bool isMath)
        {
            Stack<double> result;
            try
            {
                result = EvaluateInternal(tokens);
            }
            catch (SymbolException symEx)
            {
                if (Assembler.CurrentPass > 0)
                    throw symEx;

                if (isMath)
                    return 0xFFFF;

                return 0;
            }

            if (result.Count != 1)
                throw new ExpressionException(tokens.Last().LastChild.Position,
                    $"Unexpected expression found: \"{tokens.Last().LastChild}\".");

            var r = result.Pop();

            if (isMath && (r < minValue || r > maxValue))
                throw new IllegalQuantityException(tokens.First());

            else if (!isMath && !(r == 0 || r == 1))
                throw new ExpressionException(tokens.First().Position, "Invalid conditional expression.");

            return r;
        }

        /// <summary>
        /// Evaluates a parsed token as a mathematical expression.
        /// </summary>
        public static double Evaluate(Token token) => Evaluate(token, int.MinValue, uint.MaxValue);

        /// <summary>
        /// Evaluates a parsed tokens as a mathematical expression.
        /// </summary>
        public static double Evaluate(Token token, long minValue, long maxValue)
        {
            if (!token.HasChildren)
            {
                var atomic = EvaluateAtomic(token);
                if (atomic < minValue || atomic > maxValue)
                    throw new IllegalQuantityException(token);
                return EvaluateAtomic(token);
            }
            return Evaluate(token.Children, minValue, maxValue);
        }

        /// <summary>
        /// Evaluates a series of parsed tokens as a mathematical expression.
        /// </summary>
        public static double Evaluate(IEnumerable<Token> tokens) => Evaluate(tokens, int.MinValue, uint.MaxValue);

        /// <summary>
        /// Evaluates a series of parsed tokens as a mathematical expression.
        /// </summary>
        /// <param name="tokens">The expression as a parsed collection of <see cref="Token"/>s.</param>
        /// <param name="minValue">The minimum value required for the result.</param>
        /// <param name="maxValue">The maximum value required for the result.</param>
        /// <exception cref="ExpressionException"></exception>
        public static double Evaluate(IEnumerable<Token> tokens, long minValue, long maxValue)
            => DoEvaluation(tokens, minValue, maxValue, true);

        /// <summary>
        /// Converts an operand to a numerical value.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <exception cref="ExpressionException"></exception>
        public static double Evaluate(string expression)
            => EvaluateAtomic(new Token(expression) { Type = TokenType.Operand });

        /// <summary>
        /// Evaluates a series of parsed tokens as a conditional (boolean) evaluation.
        /// </summary>
        /// <param name="tokens">The expression as a parsed collection of <see cref="Token"/>s.</param>
        /// <exception cref="ExpressionException"></exception>
        public static bool EvaluateCondition(IEnumerable<Token> tokens)
            => DoEvaluation(tokens, -1E-15, 1.000000000000001, false).AlmostEquals(1);


        public static bool EvaluateCondition(Token token)
        {
            if (token.HasChildren)
                return EvaluateCondition(token.Children);
            return EvaluateAtomic(token).AlmostEquals(1);
        }

        /// <summary>
        /// Add a custom function evaluator to the evaluator.
        /// </summary>
        /// <param name="evaluator">The <see cref="IFunctionEvaluator"/> responsible for
        /// implementing the custom function definition.</param>
        public static void AddFunctionEvaluator(IFunctionEvaluator evaluator)
            => _functionEvaluators.Add(evaluator);

        #endregion
    }
}