//-----------------------------------------------------------------------------
// Copyright (c) 2017 informedcitizenry <informedcitizenry@gmail.com>
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to 
// deal in the Software without restriction, including without limitation the 
// rights to use, copy, modify, merge, publish, distribute, sublicense, and/or 
// sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in 
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS 
// IN THE SOFTWARE.
//-----------------------------------------------------------------------------

using Mathos.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace DotNetAsm
{
    /// <summary>
    /// Math expression evaluator class. Takes string input and parses and evaluates.
    /// </summary>
    public class ExpressionEvaluator : IEvaluator
    {
        #region Exception

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
            public ExpressionException(string expression)
            {
                ExpressionString = expression;
            }

            /// <summary>
            /// Overrides the Exception message.
            /// </summary>
            public override string Message
            {
                get
                {
                    return "Unknown or invalid expression: " + ExpressionString;
                }
            }
        }

        #endregion

        #region Constants

        /// <summary>
        /// Represents the string indicating the string expression can be evaluated.
        /// </summary>
        public const string EVAL_OKAY = "?OKAY";

        /// <summary>
        /// Represents the string indicating the expression cannot be evaluated.
        /// </summary>
        public const string EVAL_FAIL = "?FAIL";

        #endregion

        #region Members

        private MathParser _evalImpl;

        private System.Random _rng;

        private Dictionary<string, string> _expCache;

        // instantiate compiled regexes for most common replacements (performance advantage?)
        private Regex _hexRegex;
        private Regex _binaryRegex;
        private Regex _unaryRegex;
        private Dictionary<string, Tuple<Func<string, string>, Regex>> _symbolLookups;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs an instance of the ExpressionEvaluator class.
        /// </summary>
        /// <param name="ignoreCase">Ignore the case of symbols, such as variables and function calls.</param>
        public ExpressionEvaluator(bool ignoreCase)
        {
            IgnoreCase = ignoreCase;
            _symbolLookups = new Dictionary<string, Tuple<Func<string, string>, Regex>>();
            _expCache = new Dictionary<string, string>();
            _rng = new Random();

            _hexRegex = new Regex(@"\$([a-fA-F0-9]+)", RegexOptions.Compiled);
            _binaryRegex = new Regex(@"%([01]+)", RegexOptions.Compiled);
            _unaryRegex = new Regex(@"(?<![a-zA-Z0-9_.)<>])(<|>|\^)(\(.+\)|[a-zA-Z0-9_.]+)", RegexOptions.Compiled);

            _evalImpl = new Mathos.Parser.MathParser(true, true, false);

            _evalImpl.OperatorList.Add("{");
            _evalImpl.OperatorList.Add("}");
            _evalImpl.OperatorList.Add(";");
            _evalImpl.OperatorList.Add("&");
            _evalImpl.OperatorList.Add("|");
            _evalImpl.OperatorList.Add("~");

            // repurpose the caret operator to bitwise XOR
            _evalImpl.OperatorAction.Remove("^");

            _evalImpl.OperatorAction.Add("{", (x, y) => (decimal)((int)x << (int)y));
            _evalImpl.OperatorAction.Add("}", (x, y) => (decimal)((int)x >> (int)y));
            _evalImpl.OperatorAction.Add(";", (x, y) => (decimal)Math.Pow((double)x, (double)y));
            _evalImpl.OperatorAction.Add("^", (x, y) => (decimal)((int)x ^ (int)y));
            _evalImpl.OperatorAction.Add("&", (x, y) => (decimal)((int)x & (int)y));
            _evalImpl.OperatorAction.Add("|", (x, y) => (decimal)((int)x | (int)y));

            _evalImpl.LocalFunctions.Add("cbrt", x => (decimal)Math.Pow((double)x[0], 0.333333333333333333));
            _evalImpl.LocalFunctions.Add("hypot", x => (decimal)Math.Sqrt(Math.Pow((double)x[0], 2) + Math.Pow((double)x[1], 2)));
            _evalImpl.LocalFunctions.Add("random", x => (decimal)_rng.Next((int)x[0], (int)x[1]));
            _evalImpl.LocalFunctions.Add("frac", x => Math.Abs(x[0] - Math.Abs(Math.Round(x[0], 0))));
            _evalImpl.LocalFunctions.Add("acos", x => (decimal)Math.Acos((double)x[0]));
            _evalImpl.LocalFunctions.Add("atan", x => (decimal)Math.Atan((double)x[0]));
            _evalImpl.LocalFunctions.Add("ceil", x => (decimal)Math.Ceiling((double)x[0]));
            _evalImpl.LocalFunctions.Add("deg", x => (decimal)(x[0] * 180 / (decimal)Math.PI));
            _evalImpl.LocalFunctions.Add("rad", x => (decimal)(x[0] * (decimal)Math.PI / 180));
            _evalImpl.LocalFunctions.Add("ln", x => (decimal)Math.Log((double)x[0]));
            _evalImpl.LocalFunctions.Add("sgn", x => (decimal)Math.Sign((double)x[0]));
        }

        /// <summary>
        /// Constructs an instance of the ExpressionEvaluator class.
        /// </summary>
        public ExpressionEvaluator()
            : this(false)
        {

        }

        #endregion

        #region Methods

        /// <summary>
        /// Evaluates a text string as a mathematical expression.
        /// </summary>
        /// <param name="expression">The string representation of the mathematical expression.</param>
        /// <returns>The result of the expression evaluation.</returns>
        /// <exception cref="ExpressionEvaluator.ExpressionException">ExpressionEvaluator.ExpressionException</exception>
        /// <exception cref="System.DivideByZeroException">System.DivideByZeroException</exception>
        public long Eval(string expression)
        {
            try
            {
                string pre_eval = PreEvaluate(expression);

                if (string.IsNullOrWhiteSpace(expression))
                    throw new ExpressionException(expression);

                return (long)_evalImpl.Parse(pre_eval);
            }
            catch (DivideByZeroException ex)
            {
                throw ex;
            }
            catch
            {
                throw new ExpressionException(expression);
            }
        }

        /// <summary>
        /// Evaluates a text string as a mathematical expression.
        /// </summary>
        /// <param name="expression">The string representation of the mathematical expression.</param>
        /// <param name="minval">The minimum value of expression. If the evaluated value is
        /// lower, a System.OverflowException will occur.</param>
        /// <param name="maxval">The maximum value of the expression. If the evaluated value 
        /// is higher, a System.OverflowException will occur.</param>
        /// <returns>The result of the expression evaluation.</returns>
        /// <exception cref="ExpressionEvaluator.ExpressionException">ExpressionEvaluator.ExpressionException</exception>
        /// <exception cref="System.DivideByZeroException">System.DivideByZeroException</exception>
        /// <exception cref="System.OverflowException">System.OverflowException</exception>
        public long Eval(string expression, long minval, long maxval)
        {
            long result = Eval(expression);
            if (result < minval || result > maxval)
                throw new OverflowException(expression);
            return result;
        }

        /// <summary>
        /// Evaluates a text string as a conditional (boolean) evaluation.
        /// </summary>
        /// <param name="condition">The string representation of the conditional expression.</param>
        /// <returns>Returns true, if the expression is true, false otherwise.</returns>
        /// <exception cref="ExpressionEvaluator.ExpressionException">ExpressionEvaluator.ExpressionException</exception>
        public bool EvalCondition(string condition)
        {
            // 1. look at all paren closures and slice them into subexpressions
            // 2. slice each || and && further into subexpressions
            // 3. evaluate each compare 
            // 4. convert trues and falses to 1s and 0s and convert logical operators to bitwise
            // 5. evaluate as a mathematical (bitwise) expression and return result == 1
            // ex: ((a<b||c>d)&&(b<d||f>d))||e<f
            // 1. (a<b||c>d),(b<d||f>d), and e<f are evaluated further
            // 2. the results of (a<b||c>d) and (b<d||f>d) are then "&&ed" against each other 
            // 3. that result is further "||ed" against result of e<f, etc.
            // 4. False||True becomes 0|1, which results to a final result of true

            string and_or_pattern = @"\|\||&&";
            string cond_op_pattern = @"([^<=>!\(]+)(<=|<|>=|>|==|!=)([^\)<=>!]+)";
            string paren_pattern = @"\([^<=>!\(]+\)";

            try
            {
                // replace all subexpressions in parantheses, e.g. 4 > (2+1)
                while (Regex.IsMatch(condition, paren_pattern))
                {
                    condition = Regex.Replace(condition, paren_pattern, m =>
                    {
                        long val = Eval(m.Value);
                        return val.ToString();
                    });
                }

                var subconditions = Regex.Split(condition, and_or_pattern);
                foreach (var subcondition in subconditions)
                {
                    var Match = Regex.Match(subcondition, cond_op_pattern);
                    if (string.IsNullOrEmpty(Match.Value) == false)
                    {
                        if (string.IsNullOrWhiteSpace(Match.Groups[1].Value) ||
                            string.IsNullOrWhiteSpace(Match.Groups[3].Value))
                            throw new ExpressionException(subcondition);

                        var comp1 = Eval(Match.Groups[1].Value);
                        var comp2 = Eval(Match.Groups[3].Value);
                        bool result = false;
                        switch (Match.Groups[2].Value)
                        {
                            case "<": result = (comp1 < comp2); break;
                            case "<=": result = (comp1 <= comp2); break;
                            case ">": result = (comp1 > comp2); break;
                            case ">=": result = (comp1 >= comp2); break;
                            case "==": result = (comp1 == comp2); break;
                            case "!=": result = (comp1 != comp2); break;
                        }
                        condition = condition.Replace(subcondition, result.ToString());
                    }
                }

                condition = condition.Replace(true.ToString(), "1");
                condition = condition.Replace(false.ToString(), "0");
                condition = condition.Replace("||", "|");
                condition = condition.Replace("&&", "&");
                var condition_val = Eval(condition);
                return condition_val == 1;
            }
            catch
            {
                throw new ExpressionException(condition);
            }
        }

        /// <summary>
        /// Get the RValue of an expression post-first parenthetical group.
        /// </summary>
        /// <param name="rval">The string to evaluate.</param>
        /// <param name="post">The "after" value.</param>
        /// <returns>The RValue of the epxression.</returns>
        private Tuple<string, string> GetRVal(string rval, string post)
        {
            if (rval.StartsWith("("))
            {
                string firstparen_r = rval.FirstParenEnclosure();
                if (firstparen_r != rval)
                {
                    post = rval.Substring(firstparen_r.Length) + post;
                    rval = firstparen_r;
                }
            }
            return new Tuple<string, string>(rval, post);
        }

        /// <summary>
        /// Convert the unary complementary expression to the value, since the internal
        /// expression implementer does not handle unary operators.
        /// </summary>
        /// <param name="match">The System.Text.RegularExpressions.Match of the complementary operation.</param>
        /// <returns>Returns the evaluated operation.</returns>
        private string ConvertCompl(Match match)
        {
            var post = string.Empty;
            var first_paren = match.Groups[1].Value.FirstParenEnclosure();
            if (first_paren != match.Groups[1].Value)
                post = match.Groups[1].Value.Substring(first_paren.Length);
            var compl = Eval(first_paren);
            string composite = (~compl).ToString() + PreEvaluate(post);

            composite = Regex.Replace(composite, @"\+\s*-", "-");
            composite = Regex.Replace(composite, @"-\s*-", "+");

            return composite;
        }

        /// <summary>
        /// Convert unary expressions MSB/LSB/Bankbyte to corresponding binary expressions,
        /// since the internal expression implementer does not handle unary operators. 
        /// </summary>
        /// <param name="match">The System.Text.RegularExpressions.Match of the unary operation.</param>
        /// <returns>Returns corresponding the binary operation.</returns>
        private string ConvertUnary(Match match)
        {
            // <value =  value        % 256
            // >value = (value/256)   % 256
            // ^value = (value/65536) % 256
            string divisor = 256.ToString();
            Tuple<string, string> capture = GetRVal(match.Groups[2].Value, match.Groups[3].Value);
            string value = capture.Item1;
            if (match.Groups[1].Value != "<")
            {
                if (match.Groups[1].Value == "^")
                    divisor = 65536.ToString();       // ^ operator means the bank byte of a 24-bit value

                value = string.Format("({0}/{1})", value, divisor);
            }

            return string.Format("({0}%256){1}", value, PreEvaluate(capture.Item2));
        }

        /// <summary>
        /// Pre-process the expression string for final readiness to the underlying
        /// implementation engine. 
        /// </summary>
        /// <param name="expression">The math expression string.</param>
        /// <param name="evalSubExpressions">Evaluate sub-expressions as well.</param>
        /// <param name="allowRestrictedOperators">Allow restricted operators (false unless recursively called)</param>
        /// <returns>Returns the "sanitized"/normally expression ready for final evaluation.</returns>
        private string PreEvaluate(string expression)
        {
            var char_pattern = @"(?<![a-zA-Z0-9_)])'(.)'(?![a-zA-Z0-9_(])";
            var func_pattern = @"([a-zA-Z][a-zA-Z0-9]*)\((.*?)\)";
            var altbin_pattern = @"%([\.#]+)";
            
            if (!_expCache.ContainsKey(expression))
            {
                string key = expression;

                if (_hexRegex.IsMatch(expression))
                {
                    // convert hex e.g. $FFD2
                    expression = _hexRegex.Replace(expression,
                        m => Convert.ToInt64(m.Groups[1].Value, 16).ToString());

                }

                // if allowed also convert alt bin, e.g. %..##.#..
                if (AllowAlternateBinString) //&& Regex.IsMatch(expression, altbin_pattern))
                {
                    // allow alternate binary string, e.g. %.###.##.##... in lieu of %0111011011000
                    expression = Regex.Replace(expression, altbin_pattern, delegate(Match m)
                    {
                        string bin = m.Value.Replace(".", "0");
                        return bin.Replace("#", "1");
                    });
                }

                // convert bin e.g. %0110101
                if (_binaryRegex.IsMatch(expression))
                    expression = _binaryRegex.Replace(expression,
                        m => Convert.ToInt32(m.Groups[1].Value, 2).ToString());

                // convert unary bitwise complement
                if (Regex.IsMatch(expression, @"(?<![a-zA-Z0-9_)<>])~(\(.+\)|[a-zA-Z0-9_.]+)"))
                    expression = Regex.Replace(expression, @"(?<![a-zA-Z0-9_)<>])~(\(.+\)|[a-zA-Z0-9_.]+)", ConvertCompl);

                // convert log10(x) to log(x,10)
                if (Regex.IsMatch(expression, @"log10(\(.+\))", IgnoreCase ? RegexOptions.IgnoreCase : RegexOptions.None)) 
                {
                    expression = Regex.Replace(expression, @"log10(\(.+\))", m =>
                    {
                        string post = string.Empty;
                        var first_paren = m.Groups[1].Value.FirstParenEnclosure();
                        if (first_paren != m.Groups[1].Value)
                        {
                            post = PreEvaluate(m.Groups[1].Value.Substring(first_paren.Length));
                        }
                        first_paren = first_paren.TrimEnd(')') + ",10)";
                        return string.Format("log{0}{1}", first_paren, post);

                    }, IgnoreCase ? RegexOptions.IgnoreCase : RegexOptions.None);
                }

                // convert LSB/MSB/bankbyte to (x % 256), x/256, or x/65536 respectively
                if (_unaryRegex.IsMatch(expression))
                    expression = _unaryRegex.Replace(expression, ConvertUnary);

                // convert functions (but not their arguments) to lowercase if we
                // are ignoring case
                if (IgnoreCase && Regex.IsMatch(expression, func_pattern))
                    expression = Regex.Replace(expression, func_pattern, m =>
                        m.Groups[1].Value.ToLower() + "(" + m.Groups[2].Value + ")");

                _expCache.Add(key, expression);
            }
            else
            {
                expression = _expCache[expression];
            }

            // convert char constant to numeric (we can't cache this because 
            // the char encoding can change between calls thus invalidating
            // previous result
            if (Regex.IsMatch(expression, char_pattern))
            {
                expression = Regex.Replace(expression, char_pattern, delegate(Match m)
                {
                    byte b = Convert.ToByte(m.Groups[1].Value[0]);
                    string convert = b.ToString();
                    return convert;
                });
            }

            expression = ReplaceSymbols(expression);

            // Certain operators are reserved and must be used in a legal way. 
            if (Regex.IsMatch(expression, @"[\[{};\]]") || Regex.IsMatch(expression, @"^%|%$"))
                throw new ExpressionException(expression);

            // massage bit shift and pow operators
            return Regex.Replace(
                    Regex.Replace(
                        Regex.Replace(expression,
                        "<<", "{"),
                    ">>", "}"),
                   @"\*\*", ";");
        }

        private string ReplaceSymbols(string expression)
        {
            // convert client-defined symbols into values
            foreach(var lookup in _symbolLookups)
            {

                var f = lookup.Value.Item1;
                Regex r = lookup.Value.Item2;

                // strip white-spaces between operators and symbols first
                expression = Regex.Replace(expression, @"\s*(" + lookup.Key + @")\s*", m =>
                {
                    return m.Groups[1].Value;
                });

                expression = r.Replace(expression, m =>
                {
                    string match = m.Value;
                    if (f != null && !string.IsNullOrEmpty(match))
                        return f(match);
                    return match;
                });
                if (expression.Contains(EVAL_FAIL))
                    return string.Empty;
            }
            return expression;
        }

        /// <summary>
        /// Defines a symbol lookup for the evaluator to translate symbols (such as 
        /// variables) in expressions.
        /// </summary>
        /// <param name="regex">A regex pattern for the symbol</param>
        /// <param name="lookupfunc">The lookup function to define the symbol</param>
        /// <exception cref="System.ArgumentNullException">System.ArgumentNullException</exception>
        public void DefineSymbolLookup(string regex, Func<string, string> lookupfunc)
        {
            Tuple<Func<string, string>, Regex> val =
                new Tuple<Func<string, string>, Regex>(lookupfunc,
                    new Regex(regex, RegexOptions.Compiled | (IgnoreCase ? RegexOptions.IgnoreCase : RegexOptions.None)));
            if (_symbolLookups.ContainsKey(regex))
                _symbolLookups[regex] = val;
            else
                _symbolLookups.Add(regex, val);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Ignore case of symbols, such as variables and function calls.
        /// </summary>
        public bool IgnoreCase { get; set; }

        /// <summary>
        /// Allow alternate binary string format ('.' for '0' and '#' for '1'). 
        /// Useful for representing pixel data.
        /// </summary>
        public bool AllowAlternateBinString { get; set; }

        #endregion
    }
}
