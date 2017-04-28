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

using org.mariuszgromada.math.mxparser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Asm6502.Net
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

        private Expression evalImpl_;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs an instance of the ExpressionEvaluator class.
        /// </summary>
        /// <param name="ignoreCase">Ignore the case of symbols, such as variables and function calls.</param>
        public ExpressionEvaluator(bool ignoreCase)
        {
            IgnoreCase = ignoreCase;
            SymbolLookups = new Dictionary<string, Func<string, object, string>>();

            evalImpl_ = new Expression();

            evalImpl_.addFunctions(new Function("cbrt(x) = x^0.333333333333333333"),
                                   new Function("hypot(x,y) = sqrt(x^2 + y^2)"),
                                   new Function("random(x,y) = rUnid(x,y)"),
                                   new Function("pow(x,y) = x^y"),
                                   new Function("frac(x) = abs(x) - abs(round(x,0))"));
        }

        /// <summary>
        /// Constructs an instance of the ExpressionEvaluator class.
        /// </summary>
        public ExpressionEvaluator()
            : this(false)
        {

        }

        #endregion

        #region Static Methods

        /// <summary>
        /// Capture and return the first parenthetical group in the string. 
        /// </summary>
        /// <param name="value">The string expression.</param>
        /// <param name="fromLeft">Evaluate from left.</param>
        /// <returns>Returns the first instance of a parenthetical group.</returns>
        public static string FirstParenGroup(string value, bool fromLeft)
        {
            int parens = 0;
            string parengroup = string.Empty;
            string evaluated;
            char open = '(', close = ')';
            if (fromLeft == false)
            {
                evaluated = new string(value.Reverse().ToArray());
                open = ')'; close = '(';
            }
            else
            {
                evaluated = value;
            }
            bool quote_enclosed = false;
            foreach (var c in evaluated)
            {
                if (parens >= 1)
                    parengroup += c.ToString();

                if (c == '"')
                {
                    quote_enclosed = !quote_enclosed;
                }
                if (quote_enclosed)
                    continue;

                if (c == open)
                {
                    if (parens == 0)
                        parengroup += c.ToString();
                    parens++;
                }
                else if (c == close)
                {
                    parens--;
                    if (parens == 0)
                    {
                        if (fromLeft == false)
                            parengroup = new string(parengroup.Reverse().ToArray());
                        return parengroup;
                    }
                    if (parens < 0)
                        throw new FormatException();
                }
            }
            if (parens > 0)
                throw new FormatException();
            return value;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Evaluate the text string as a mathematical expression.
        /// </summary>
        /// <param name="expression">The string representation of the mathematical expression.</param>
        /// <returns>The result of the expression evaluation.</returns>
        /// <exception cref="ExpressionEvaluator.ExpressionException">ExpressionEvaluator.ExpressionException</exception>
        public long Eval(string expression)
        {
            try
            {
                string pre_eval = PreEvaluate(expression);

                evalImpl_.setExpressionString(pre_eval);
                
                var eval = evalImpl_.calculate();
                if (eval == Double.NaN)
                    throw new ExpressionException(expression);

                return Convert.ToInt64(Math.Floor(eval));
            }
            catch
            {
                throw new ExpressionException(expression);
            }
        }

        /// <summary>
        /// Determines whether the expression can be evaluated from the current
        /// symbol lookup state of the calling client.
        /// </summary>
        /// <param name="expression">The expression to test whether it can be evaluated.</param>
        /// <returns>True, if the expression can be evaluated, otherwise false.</returns>
        public bool CanEvaluate(string expression)
        {
            bool can = true;
            foreach(var lookup in SymbolLookups)
            {
                var matches = Regex.Matches(expression, lookup.Key);
                foreach (Match m in matches)
                {
                    if (lookup.Value != null)
                    {
                        string result = lookup.Value(m.Value, SymbolLookupObject);
                        if (result == EVAL_FAIL)
                            can = false;
                    }
                }
            }
            return can;
        }

        /// <summary>
        /// Evaluates the text string as a conditional (boolean) evaluation.
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

            string paren_pattern = @"(?<![a-zA-Z0-9])!?\(.+\)";
            string and_or_pattern = @"\|\||&&";
            string condition_pattern = @"([^<=>!]+)(<=|<|>=|>|==|!=)([^<=>!]+)";

            try
            {
                bool not = false;

                while (Regex.IsMatch(condition, paren_pattern))
                {
                    var match = Regex.Match(condition, paren_pattern).Value;
                    if (match.StartsWith("!"))
                    {
                        not = true;
                        match = match.TrimStart('!');
                        condition = condition.TrimStart('!');
                    }
                    string first_paren = FirstParenGroup(match, true);
                    bool first_paren_result = EvalCondition(first_paren.Substring(1, first_paren.Length - 2));
                    if (not) first_paren_result = !first_paren_result;
                    condition = condition.Replace(first_paren, first_paren_result.ToString());
                }

                var subconditions = Regex.Split(condition, and_or_pattern);
                foreach (var subcondition in subconditions)
                {
                    var Match = Regex.Match(subcondition, condition_pattern);
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
                            case "<":  result = (comp1 <  comp2); break;
                            case "<=": result = (comp1 <= comp2); break;
                            case ">":  result = (comp1 >  comp2); break;
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
                string firstparen_r = FirstParenGroup(rval, true);
                if (firstparen_r != rval)
                {
                    post = rval.Substring(firstparen_r.Length) + post;
                    rval = firstparen_r;
                }
            }
            return new Tuple<string, string>(rval, post);
        }

        /// <summary>
        /// Convert unary expressions MSB/LSB/Bankbyte to corresponding binary expressions,
        /// since the internal expression implementer does not handle unary operators. 
        /// </summary>
        /// <param name="match">The Regex match of the unary operation.</param>
        /// <returns>Returns corresponding the binary operation.</returns>
        private string ConvertUnary(Match match)
        {
            // <value = value % 256
            // >value = (value/256) % 256
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

            return string.Format("({0}#256){1}", value, capture.Item2);
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
            var mod_pattern = @"(?<=[a-zA-Z0-9_\)]|\s)%(?=[a-zA-Z0-9_\(]|\s)";
            var unary_pattern = @"(?<![a-zA-Z0-9_\)<>])(<|>|\^)(\(.+\)|[a-z0-9_\.]+)";
            var char_pattern = @"(?<![a-zA-Z0-9_\)])'(.)'(?![a-zA-Z0-9_\(])";
            var func_pattern = @"([A-Z][A-Z0-9]*)\((.*?)\)";
            var altbin_pattern = @"%([\.#]+)";

            // convert hex e.g. $FFD2
            expression = Regex.Replace(expression, @"\$([a-f0-9]+)",
                m => Convert.ToInt64(m.Groups[1].Value, 16).ToString(),
                IgnoreCase ? RegexOptions.IgnoreCase : RegexOptions.None);

            // if allowed also convert alt bin, e.g. %..##.#..
            if (AllowAlternateBinString)
            {
                // allow alternate binary string, e.g. %.###.##.##... in lieu of %0111011011000
                expression = Regex.Replace(expression, altbin_pattern, delegate(Match m)
                {
                    string bin = m.Value.Replace(".", "0");
                    return bin.Replace("#", "1");
                });
            }
            // Certain operators are reserved. 
            if (Regex.IsMatch(expression, @"[\[@#\]]"))
                throw new ExpressionException(expression);

            // convert bin e.g. %0110101
            expression = Regex.Replace(expression, @"%([01]+)", m => Convert.ToInt32(m.Groups[1].Value, 2).ToString(), RegexOptions.IgnoreCase);

            // convert char constant to numeric
            expression = Regex.Replace(expression, char_pattern, delegate(Match m)
            {
                byte b = Convert.ToByte(m.Groups[1].Value[0]);
                string convert = b.ToString();
                if (CharEncoding != null)
                    convert = CharEncoding(b).ToString();
                return convert;
            },
            IgnoreCase ? RegexOptions.IgnoreCase : RegexOptions.None);

            // convert client-defined symbols into values
            foreach (var l in SymbolLookups)
            {
                string sym_pattern = @"(\W+|^)(" + l.Key + @")(\W+|$)";

                // strip white-spaces between operators and symbols first
                expression = Regex.Replace(expression, @"\s+("+ l.Key + @")\s+", m => 
                    {
                        return m.Groups[1].Value;
                    });

                expression = Regex.Replace(expression, sym_pattern, delegate(Match m)
                {
                    if (l.Value != null &&
                        !string.IsNullOrEmpty(m.Value) &&
                        !m.Groups[3].Value.StartsWith("(")) // exclude function calls
                    {
                        return m.Groups[1].Value +
                                l.Value(m.Groups[2].Value, SymbolLookupObject) +
                                m.Groups[3].Value;
                    }
                    return m.Value;
                }, IgnoreCase ? RegexOptions.IgnoreCase : RegexOptions.None);
            }

            // convert LSB/MSB/bankbyte to (x % 256), x/256, or x/65536 respectively
            expression = Regex.Replace(expression, unary_pattern, ConvertUnary,
                IgnoreCase ? RegexOptions.IgnoreCase : RegexOptions.None);

            expression = Regex.Replace(expression, mod_pattern, m => "#");

            // massage bitwise operators
            expression = Regex.Replace(expression, "~", "@~");
            expression = Regex.Replace(expression, @"\^", "@^");
            expression = Regex.Replace(expression, "&", "@&");
            expression = Regex.Replace(expression, @"\|", "@|");
            expression = Regex.Replace(expression, "<<", "@<<");
            expression = Regex.Replace(expression, ">>", "@>>");

            // convert pow ** operator to single-char ^ operator
            expression = Regex.Replace(expression, @"\*\*", "^");

            // convert functions (but not their arguments) to lowercase if we
            // are ignoring case
            if (IgnoreCase)
                expression = Regex.Replace(expression, func_pattern, m => m.Groups[1].Value.ToLower() + "(" + m.Groups[2].Value + ")", RegexOptions.IgnoreCase);

            return expression;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the symbol lookup helpers for the evaluator to translate symbols
        /// in expressions. The key for each dictionary entry is a regular expression (regex)
        /// that provides lookup information. The value is a callback function to provide the 
        /// lookup.
        /// </summary>
        public Dictionary<string, Func<string, object, string>> SymbolLookups { get; private set; }

        /// <summary>
        /// Ignore case of symbols, such as variables and function calls.
        /// </summary>
        public bool IgnoreCase { get; set; }

        /// <summary>
        /// Gets or sets the client-usable object in the symbol lookup callback.
        /// </summary>
        public object SymbolLookupObject { get; set; }

        /// <summary>
        /// Allow alternate binary string format ('.' for '0' and '#' for '1'). 
        /// Useful for representing pixel data.
        /// </summary>
        public bool AllowAlternateBinString { get; set; }

        /// <summary>
        /// Gets or sets the char-encoding transform, e.g. ASCII to Commodore PETSCII.
        /// </summary>
        public Func<byte, byte> CharEncoding { get; set; }

        #endregion
    }
}
