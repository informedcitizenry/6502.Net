//-----------------------------------------------------------------------------
// Copyright (c) 2017, 2018 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using FunctionDef = System.Tuple<System.Func<System.Collections.Generic.List<double>, double>, int>;

namespace DotNetAsm
{
    /// <summary>
    /// Exception class for evaluation expressions.
    /// </summary>
    public class ExpressionException : Exception
    {
        /// <summary>
        /// Gets or sets the expression string that raises the exception.
        /// </summary>
        public string ExpressionString { get; set; }

        /// <summary>
        /// Constructs an instance of the ExpressionException class.
        /// </summary>
        /// <param name="expression">The expression that raises the exception.</param>
        public ExpressionException(string expression) : base()
        {
            ExpressionString = expression;
        }

        /// <summary>
        /// Overrides the Exception message.
        /// </summary>
        public override string Message
            => "Unknown or invalid expression: " + ExpressionString;
    }

    /// <summary>
    /// Math expression evaluator class. Takes string input and parses and evaluates 
    /// to a <see cref="T:System.Int64"/>.
    /// </summary>
    public class Evaluator : IEvaluator
    {
        #region Constants

        /// <summary>
        /// Represents the string indicating the expression cannot be evaluated.
        /// </summary>
        public const string EVAL_FAIL = "?FAIL";

        #endregion

        #region Members

        Random _rng;
        Dictionary<string, double> _cache;
        readonly Dictionary<string, Tuple<Regex, Func<string, string>>> _symbolLookups;
        Dictionary<string, FunctionDef> _functions, _operators;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs an instance of the <see cref="T:DotNetAsm.Evaluator"/> class, used to evaluate 
        /// strings as mathematical expressions.
        /// </summary>
        public Evaluator()
        {
            _symbolLookups = new Dictionary<string, Tuple<Regex, Func<string, string>>>();
            _cache = new Dictionary<string, double>();
            _rng = new Random();

            _functions = new Dictionary<string, FunctionDef>
            {
                { "abs",    new FunctionDef(parms => Math.Abs(parms[0]),             1) },
                { "acos",   new FunctionDef(parms => Math.Acos(parms[0]),            1) },
                { "atan",   new FunctionDef(parms => Math.Atan(parms[0]),            1) },
                { "cbrt",   new FunctionDef(parms => Math.Pow(parms[0], 1.0 / 3.0),  1) },
                { "ceil",   new FunctionDef(parms => Math.Ceiling(parms[0]),         1) },
                { "cos",    new FunctionDef(parms => Math.Cos(parms[0]),             1) },
                { "cosh",   new FunctionDef(parms => Math.Cosh(parms[0]),            1) },
                { "deg",    new FunctionDef(parms => (parms[0] * 180 / Math.PI),     1) },
                { "exp",    new FunctionDef(parms => Math.Exp(parms[0]),             1) },
                { "floor",  new FunctionDef(parms => Math.Floor(parms[0]),           1) },
                { "frac",   new FunctionDef(parms => Math.Abs(parms[0] - Math.Abs(Math.Round(parms[0], 0))), 1) },
                { "hypot",  new FunctionDef(parms => Math.Sqrt(Math.Pow(parms[1], 2) + Math.Pow(parms[0], 2)), 2) },
                { "ln",     new FunctionDef(parms => Math.Log(parms[0]),             1) },
                { "log10",  new FunctionDef(parms => Math.Log10(parms[0]),           1) },
                { "pow",    new FunctionDef(parms => Math.Pow(parms[1], parms[0]),   2) },
                { "rad",    new FunctionDef(parms => (parms[0] * Math.PI / 180),     1) },
                { "random", new FunctionDef(parms => _rng.Next((int)parms[1], (int)parms[0]), 2) },
                { "sgn",    new FunctionDef(parms => Math.Sign(parms[0]),            1) },
                { "sin",    new FunctionDef(parms => Math.Sin(parms[0]),             1) },
                { "sinh",   new FunctionDef(parms => Math.Sinh(parms[0]),            1) },
                { "sqrt",   new FunctionDef(parms => Math.Sqrt(parms[0]),            1) },
                { "tan",    new FunctionDef(parms => Math.Tan(parms[0]),             1) },
                { "tanh",   new FunctionDef(parms => Math.Tanh(parms[0]),            1) },
                { "round",
                    new FunctionDef(delegate (List<double> parms)
                     {
                         if (parms.Count == 2)
                             return Math.Round(parms[1], (int)parms[0]);
                         return Math.Round(parms[0]);
                     }, 2)
                }
            };

            _operators = new Dictionary<string, FunctionDef>
            {

                { "||",     new FunctionDef(parms => (int)parms[1]  | (int)parms[0],            0) },
                { "&&",     new FunctionDef(parms => (int)parms[1]  & (int)parms[0],            1) },
                { "|",      new FunctionDef(parms => (long)parms[1] | (long)parms[0],           2) },
                { "^",      new FunctionDef(parms => (long)parms[1] ^ (long)parms[0],           3) },
                { "&",      new FunctionDef(parms => (long)parms[1] & (long)parms[0],           4) },
                { "!=",     new FunctionDef(parms => (long)parms[1] != (long)parms[0] ? 1 : 0,  5) },
                { "==",     new FunctionDef(parms => (long)parms[1] == (long)parms[0] ? 1 : 0,  5) },
                { "<",      new FunctionDef(parms => (long)parms[1] < (long)parms[0]  ? 1 : 0,  6) },
                { "<=",     new FunctionDef(parms => (long)parms[1] <= (long)parms[0] ? 1 : 0,  6) },
                { ">=",     new FunctionDef(parms => (long)parms[1] >= (long)parms[0] ? 1 : 0,  6) },
                { ">",      new FunctionDef(parms => (long)parms[1] > (long)parms[0]  ? 1 : 0,  6) },
                { "=",      new FunctionDef(parms => Double.NaN,                                6) },
                { "<<",     new FunctionDef(parms => (int)parms[1]  << (int)parms[0],           7) },
                { ">>",     new FunctionDef(parms => (int)parms[1]  >> (int)parms[0],           7) },
                { "-",      new FunctionDef(parms => parms[1]       - parms[0],                 8) },
                { "+",      new FunctionDef(parms => parms[1]       + parms[0],                 8) },
                { "/",      new FunctionDef(parms => parms[1]       / parms[0],                 9) },
                { "*",      new FunctionDef(parms => parms[1]       * parms[0],                 9) },
                { "%",      new FunctionDef(parms => (long)parms[1] % (long)parms[0],           9) },
                { "!",      new FunctionDef(parms => Double.NaN,                                10) },
                { "~",      new FunctionDef(parms => Double.NaN,                                10) },
                { "\x11-",  new FunctionDef(parms => -parms[0],                                 11) },
                { "\x11+",  new FunctionDef(parms => parms[0],                                  11) },
                { "\x11~",  new FunctionDef(parms => ~((long)parms[0]),                         12) },
                { "\x11!",  new FunctionDef(parms => (long)parms[0] == 0 ? 1 : 0,               12) },
                { "**",     new FunctionDef(parms => Math.Pow(parms[1], parms[0]),              13) },
                { "\x11>",  new FunctionDef(parms => (long)(parms[0] / 0x100) % 256,            14) },
                { "\x11<",  new FunctionDef(parms => (long)parms[0]  % 256,                     14) },
                { "\x11&",  new FunctionDef(parms => (long)parms[0]  % 65536,                   14) },
                { "\x11^",  new FunctionDef(parms => (long)(parms[0] / 0x10000) % 256,          14) }
            };
        }

        #endregion

        #region Methods

        bool AddToken(StringBuilder token, List<string> toList)
        {
            if (token.Length > 0)
            {
                toList.Add(token.ToString());
                token.Clear();
                return true;
            }
            return false;
        }

        // Take a string expression and tokenize it.
        List<string> Tokenize(string expression)
        {
            StringBuilder operandString = new StringBuilder(),
                          operatorString = new StringBuilder();

            List<string> tokens = new List<string>();
            for (int i = 0; i < expression.Length; i++)
            {
                var c = expression[i];
                if ((c.Equals('%') && operandString.Length == 0 && (i == 0 || (!_operators.ContainsKey(expression[i - 1].ToString()) && expression[i - 1] != ')' ))) ||
                     c.Equals('$') || char.IsLetterOrDigit(c) || c.Equals('.') || c.Equals('#'))
                {
                    AddToken(token: operatorString, toList: tokens);
                    operandString.Append(c);
                    continue;
                }
                else if (_operators.ContainsKey(c.ToString()) || c.Equals('(') || c.Equals(')') || c.Equals(','))
                {
                    AddToken(token: operandString, toList: tokens);
                    if (c.Equals('(') || c.Equals(')'))
                    {
                        AddToken(token: operatorString, toList: tokens);
                        tokens.Add(c.ToString());
                    }
                    else
                    {
                        // Is the compound operator valid? If not, tokenize the existing operator string.
                        if (!_operators.ContainsKey(string.Concat(operatorString.ToString(), c)))
                            AddToken(token: operatorString, toList: tokens);
                        operatorString.Append(c);
                    }
                    continue;
                }
                if (!AddToken(token: operandString, toList: tokens))
                    AddToken(token: operatorString, toList: tokens);
            }
            if (!AddToken(token: operandString, toList: tokens))
                AddToken(token: operatorString, toList: tokens);

            return tokens;
        }

        // Tokenize expression and then re-order according to RPN notation.
        List<string> ToRpn(string expression)
        {
            var tokens = Tokenize(expression);
            var output = new List<string>();
            var operators = new Stack<string>();
            var lastToken = string.Empty;
            var functionstack = new Stack<Tuple<int, int>>();
            var parens = 0;

            foreach (var t in tokens)
            {
                if (_functions.ContainsKey(t))
                {
                    // if the token is a function name push onto the stack
                    lastToken = t;
                    operators.Push(t);

                    // keep track of param count if necessary
                    functionstack.Push(new Tuple<int, int>(_functions[t].Item2, parens + 1));
                }
                else if (_operators.ContainsKey(t))
                {
                    // check for unary
                    string op;
                    if (string.IsNullOrEmpty(lastToken) || 
                        _functions.ContainsKey(lastToken) ||
                        _operators.ContainsKey(lastToken))
                        op = string.Concat("\x11", t);
                    else
                        op = t;

                    // or else if the token is an operator, send higher order
                    // operators or functions at the top of the stack to the output
                    while (operators.Count > 0)
                    {
                        var topofstack = operators.Peek();
                        if (_functions.ContainsKey(topofstack))
                        {
                            output.Add(operators.Pop());
                        }
                        else if (topofstack != "(")
                        {
                            var toporder = _operators[topofstack].Item2;
                            var tokenorder = _operators[op].Item2;
                            if (toporder >= tokenorder)
                                output.Add(operators.Pop());
                            else
                                break;
                        }
                        else
                        {
                            // don't pop off open parens (just yet)
                            break;
                        }
                    }
                    // finally push the operator onto the stack
                    operators.Push(op);
                    lastToken = op;
                }
                else if (t.Equals("("))
                {
                    // else open paren push onto the stack
                    if (!string.IsNullOrEmpty(lastToken) && !_operators.ContainsKey(lastToken) && !_functions.ContainsKey(lastToken))
                        throw new ExpressionException(expression);
                    parens++;
                    operators.Push(t);

                    // new clause (unaries are acceptable)
                    lastToken = string.Empty;
                }
                else if (t.Equals(")") || t.Equals(","))
                {
                    if (string.IsNullOrEmpty(lastToken) || _operators.ContainsKey(lastToken))
                        throw new ExpressionException(expression);

                    // closed paren or param closure (comma) move all operators into output
                    // until an open paren is reached
                    while (operators.Peek() != "(")
                        output.Add(operators.Pop());

                    // only pop the open paren off the stack if it is a closed paren
                    if (t.Equals(")"))
                    {
                        operators.Pop();
                        if (functionstack.Count > 0 && functionstack.Peek().Item2 == parens)
                            functionstack.Pop();
                        parens--;
                    }
                    else
                    {
                        // track params against function definition
                        var fcntop = functionstack.Pop();
                        var parmnum = fcntop.Item1 - 1;
                        if (parmnum < 1)
                            throw new ExpressionException(expression);
                        functionstack.Push(new Tuple<int, int>(parmnum, fcntop.Item2));
                    }
                    // A comma, like open paren, marks new clause. Closed paren does not.
                    lastToken = t.Equals(")") ? t : string.Empty;
                }
                else
                {
                    // else this is not a function or an operator nor a paren, so send the token
                    // onto the output.

                    // non-operator tokens cannot be preceded by functions nor by other non-operator tokens 
                    if (!string.IsNullOrEmpty(lastToken) && !_operators.ContainsKey(lastToken))
                        throw new ExpressionException(expression);

                    lastToken = t;
                    output.Add(t);
                }
            }
            // pop all remaining operators off of stack and send to the output.
            while (operators.Count > 0)
                output.Add(operators.Pop());
            return output;
        }

        double Calculate(IEnumerable<string> output)
        {
            var result = new Stack<double>();

            foreach (var s in output)
            {
                if (s.Equals(")"))
                    continue;

                if (double.TryParse(s, out double num))
                {
                    result.Push(num);
                }
                else if ((s.StartsWith("%", StringComparison.Ordinal) && s.Length > 1) ||
                         s.StartsWith("$", StringComparison.Ordinal))
                {
                    var hexbin = s.Substring(1);
                    int radix;
                    if (s.First().Equals('%'))
                    {
                        radix = 2;
                        hexbin = Regex.Replace(hexbin, @"^([#.]+)$",
                                               m => m.Groups[1].Value.Replace("#", "1").Replace(".", "0"));
                    }
                    else
                    {
                        radix = 16;
                    }
                    result.Push(Convert.ToInt64(hexbin, radix));
                }
                else
                {
                    FunctionDef operation;
                    List<double> parms = new List<double> { result.Pop() };

                    if (_functions.ContainsKey(s))
                    {
                        operation = _functions[s];
                        var parmcount = _functions[s].Item2 - 1;
                        while (parmcount-- > 0)
                            parms.Add(result.Pop());
                    }
                    else
                    {
                        operation = _operators[s];
                        if (!s.StartsWith("\x11", StringComparison.Ordinal))
                            parms.Add(result.Pop());
                    }
                    result.Push(operation.Item1(parms));
                }
            }
            return result.Pop();
        }

        bool ContainsSymbols(string expression) =>
                _symbolLookups.Values.Any(l => l.Item1.IsMatch(expression));

        // convert client-defined symbols into values
        string EvalDefinedSymbols(string expression)
        {
            foreach (var lookup in _symbolLookups)
            {
                Regex r = lookup.Value.Item1;
                var f = lookup.Value.Item2;

                expression = r.Replace(expression, m =>
                {
                    string match = m.Value;
                    if (f != null && !string.IsNullOrEmpty(match))
                        return f(match);
                    return match;
                });
                if (expression.Contains(EVAL_FAIL))
                    throw new ExpressionException(expression);
            }
            return expression;
        }

        // Evaluate internally the expression to a double.
        double EvalInternal(string expression)
        {
            if (string.IsNullOrEmpty(expression))
                throw new ExpressionException(expression);

            // if we have already cached the expression, just return the cached evaluation.
            if (_cache.ContainsKey(expression))
                return _cache[expression];

            // symbols are assumed to be volatile, so we cannot cache them.
            if (!ContainsSymbols(expression))
                _cache.Add(expression, Double.NaN);

            var output = ToRpn(EvalDefinedSymbols(expression));
            if (output.Count == 0)
                throw new ExpressionException(expression);
            
            try
            {
                var result = Calculate(output);

                if (double.IsInfinity(result))
                    throw new DivideByZeroException(expression);

                if (double.IsNaN(result))
                    throw new ExpressionException(expression);

                // update the cached value
                if (_cache.ContainsKey(expression))
                    _cache[expression] = result;
                return result;
            }
            catch (Exception)
            {
                throw new ExpressionException(expression);
            }
        }

        /// <summary>
        /// Evaluates a text string as a mathematical expression.
        /// </summary>
        /// <param name="expression">The string representation of the mathematical expression.</param>
        /// <returns>The result of the expression evaluation as a <see cref="T:System.Int64"/> value.</returns>
        /// <exception cref="T:DotNetAsm.ExpressionException">DotNetAsm.ExpressionException</exception>
        /// <exception cref="T:System.DivideByZeroException">System.DivideByZeroException</exception>
        public long Eval(string expression) => Eval(expression, Int32.MinValue, UInt32.MaxValue);

        /// <summary>
        /// Evaluates a text string as a mathematical expression.
        /// </summary>
        /// <param name="expression">The string representation of the mathematical expression.</param>
        /// <param name="minval">The minimum value of expression. If the evaluated value is
        /// lower, a System.OverflowException will occur.</param>
        /// <param name="maxval">The maximum value of the expression. If the evaluated value 
        /// is higher, a System.OverflowException will occur.</param>
        /// <returns>The result of the expression evaluation as a <see cref="T:System.Int64"/> value.</returns>
        /// <exception cref="T:DotNetAsm.ExpressionException">DotNetAsm.ExpressionException</exception>
        /// <exception cref="T:System.DivideByZeroException">System.DivideByZeroException</exception>
        /// <exception cref="T:System.OverflowException">System.OverflowException</exception>
        public long Eval(string expression, long minval, long maxval)
        {
            var result = EvalInternal(expression);
            if (result < minval || result > maxval)
                throw new OverflowException(expression);
            return (long)result;
        }

        /// <summary>
        /// Evaluates a text string as a conditional (boolean) evaluation.
        /// </summary>
        /// <param name="condition">The string representation of the conditional expression.</param>
        /// <returns><c>True</c>, if the expression is true, otherwise <c>false</c>.</returns>
        /// <exception cref="T:DotNetAsm.ExpressionException">DotNetAsm.ExpressionException</exception>
        public bool EvalCondition(string condition) => Eval(condition) == 1;

        /// <summary>
        /// Defines a symbol lookup for the evaluator to translate symbols (such as 
        /// variables) in expressions.
        /// </summary>
        /// <param name="pattern">A regex pattern for the symbol</param>
        /// <param name="lookupfunc">The lookup function to define the symbol</param>
        /// <exception cref="T:System.ArgumentNullException">System.ArgumentNullException</exception>
        public void DefineSymbolLookup(string pattern, Func<string, string> lookupfunc)
        {
            var value = new Tuple<Regex, Func<string, string>>(new Regex(pattern, RegexOptions.Compiled), lookupfunc);
            if (_symbolLookups.ContainsKey(pattern))
                _symbolLookups[pattern] = value;
            else
                _symbolLookups.Add(pattern, value);
        }

        #endregion
    }
}
