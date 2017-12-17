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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace DotNetAsm
{
    /// <summary>
    /// A class that represents information about an opcode, including its diasssembly format,
    /// size, and its index (or code).
    /// </summary>
    public class Opcode
    {
        /// <summary>
        /// The Disassembly string format
        /// </summary>
        public string DisasmFormat { get; set; }

        /// <summary>
        /// The opcode size
        /// </summary>
        public int Size { get; set; }

        /// <summary>
        /// The index of the opcode
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Gets or sets the CPU of this opcode.
        /// </summary>
        /// <value>The cpu.</value>
        public string CPU { get; set; }
    }

    /// <summary>
    /// Represents an operand format, including captured expressions
    /// </summary>
    public class OperandFormat
    {
        /// <summary>
        /// The format string of the operand
        /// </summary>
        public string FormatString;

        /// <summary>
        /// The first captured expression
        /// </summary>
        public string Expression1;

        /// <summary>
        /// The second captured expression
        /// </summary>
        public string Expression2;
    }

    public class FormatBuilder
    {
        #region Members

        readonly Regex _regex;
        string _format;
        string _exp1Format;
        string _exp2Format;
        int _reg1Group, _exp1Group;
        int _reg2Group, _exp2Group;
        bool _treatParenEnclosureAsExpr;
        IEvaluator _evaluator;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs an instance of a DotNetAsm.FormatBuilder class.
        /// </summary>
        /// <param name="regex">A valid System.Text.RegularExpressions.Regex pattern</param>
        /// <param name="format">The final format of the operand as a valid .Net 
        /// System.String format</param>
        /// <param name="exp1format">The format of the first subexpression as a valid .Net
        /// System.String format</param>
        /// <param name="exp2format">The format of the second subexpression as a valid .Net
        /// System.String format</param>
        /// <param name="reg1">The index of the first register's matching group in the
        /// regex pattern</param>
        /// <param name="reg2">The index of the second register's matching group in the
        /// regex pattern</param>
        /// <param name="exp1">The index of the first subexpression's matching group in
        /// the regex pattern</param>
        /// <param name="exp2">The index of the second subexpression's matching group in
        /// the regex pattern</param>
        /// <param name="regexOptions">Any System.Text.RegularExpressions.RegexOptions</param>
        /// <param name="treatParenAsExpr">If the first subexpression is enclosed in 
        /// paranetheses, enclose the subexpression's position in the final format
        /// inside paranetheses as well</param>
        /// <param name="evaluator">If not null, a DotNetAsm.IEvaluator to evaluate the second 
        /// subexpression as part of the final format</param>
        public FormatBuilder(string regex, string format, string exp1format, string exp2format, int reg1, int reg2, int exp1, int exp2, RegexOptions regexOptions, bool treatParenAsExpr, IEvaluator evaluator)
        {
            _regex = new Regex(regex, regexOptions | RegexOptions.Compiled);
            _format = format;
            _exp1Format = exp1format;
            _exp2Format = exp2format;
            _reg1Group = reg1; _exp1Group = exp1;
            _reg2Group = reg2; _exp2Group = exp2;
            _treatParenEnclosureAsExpr = treatParenAsExpr;
            _evaluator = evaluator;
        }

        /// <summary>
        /// Constructs an instance of a DotNetAsm.FormatBuilder class.
        /// </summary>
        /// <param name="regex">A valid System.Text.RegularExpressions.Regex pattern</param>
        /// <param name="format">The final format of the operand as a valid .Net 
        /// System.String format</param>
        /// <param name="exp1format">The format of the first subexpression as a valid .Net
        /// System.String format</param>
        /// <param name="exp2format">The format of the second subexpression as a valid .Net
        /// System.String format</param>
        /// <param name="reg1">The index of the first register's matching group in the
        /// regex pattern</param>
        /// <param name="reg2">The index of the second register's matching group in the
        /// regex pattern</param>
        /// <param name="exp1">The index of the first subexpression's matching group in
        /// the regex pattern</param>
        /// <param name="exp2">The index of the second subexpression's matching group in
        /// the regex pattern</param>
        /// <param name="regexOptions">Any System.Text.RegularExpressions.RegexOptions</param>
        /// <param name="treatParenAsExpr">If the first subexpression is enclosed in 
        /// paranetheses, enclose the subexpression's position in the final format
        /// inside paranetheses as well</param>
        public FormatBuilder(string regex, string format, string exp1format, string exp2format, int reg1, int reg2, int exp1, int exp2, RegexOptions regexOptions, bool treatParenAsExpr)
            : this(regex, format, exp1format, exp2format, reg1, reg2, exp1, exp2, regexOptions, treatParenAsExpr, null)
        {

        }

        /// <summary>
        /// Constructs an instance of a DotNetAsm.FormatBuilder class.
        /// </summary>
        /// <param name="regex">A valid System.Text.RegularExpressions.Regex pattern</param>
        /// <param name="format">The final format of the operand as a valid .Net 
        /// System.String format</param>
        /// <param name="exp1format">The format of the first subexpression as a valid .Net
        /// System.String format</param>
        /// <param name="exp2format">The format of the second subexpression as a valid .Net
        /// System.String format</param>
        /// <param name="reg1">The index of the first register's matching group in the
        /// regex pattern</param>
        /// <param name="reg2">The index of the second register's matching group in the
        /// regex pattern</param>
        /// <param name="exp1">The index of the first subexpression's matching group in
        /// the regex pattern</param>
        /// <param name="exp2">The index of the second subexpression's matching group in
        /// the regex pattern</param>
        /// <param name="regexOptions">Any System.Text.RegularExpressions.RegexOptions</param>
        /// <param name="evaluator">If not null, a DotNetAsm.IEvaluator to evaluate the second 
        /// subexpression as part of the final format</param>
        public FormatBuilder(string regex, string format, string exp1format, string exp2format, int reg1, int reg2, int exp1, int exp2, RegexOptions regexOptions, IEvaluator evaluator)
            : this(regex, format, exp1format, exp2format, reg1, reg2, exp1, exp2, regexOptions, false, evaluator)
        {

        }

        /// <summary>
        /// Constructs an instance of a DotNetAsm.FormatBuilder class.
        /// </summary>
        /// <param name="regex">A valid System.Text.RegularExpressions.Regex pattern</param>
        /// <param name="format">The final format of the operand as a valid .Net 
        /// System.String format</param>
        /// <param name="exp1format">The format of the first subexpression as a valid .Net
        /// System.String format</param>
        /// <param name="exp2format">The format of the second subexpression as a valid .Net
        /// System.String format</param>
        /// <param name="reg1">The index of the first register's matching group in the
        /// regex pattern</param>
        /// <param name="reg2">The index of the second register's matching group in the
        /// regex pattern</param>
        /// <param name="exp1">The index of the first subexpression's matching group in
        /// the regex pattern</param>
        /// <param name="exp2">The index of the second subexpression's matching group in
        /// the regex pattern</param>
        /// <param name="regexOptions">Any System.Text.RegularExpressions.RegexOptions</param>
        public FormatBuilder(string regex, string format, string exp1format, string exp2format, int reg1, int reg2, int exp1, int exp2, RegexOptions regexOptions)
            : this(regex, format, exp1format, exp2format, reg1, reg2, exp1, exp2, regexOptions, false, null)
        {

        }

        /// <summary>
        /// Constructs an instance of a DotNetAsm.FormatBuilder class.
        /// </summary>
        /// <param name="regex">A valid System.Text.RegularExpressions.Regex pattern</param>
        /// <param name="format">The final format of the operand as a valid .Net 
        /// System.String format</param>
        /// <param name="exp1format">The format of the first subexpression as a valid .Net
        /// System.String format</param>
        /// <param name="exp2format">The format of the second subexpression as a valid .Net
        /// System.String format</param>
        /// <param name="reg1">The index of the first register's matching group in the
        /// regex pattern</param>
        /// <param name="reg2">The index of the second register's matching group in the
        /// regex pattern</param>
        /// <param name="exp1">The index of the first subexpression's matching group in
        /// the regex pattern</param>
        /// <param name="exp2">The index of the second subexpression's matching group in
        /// the regex pattern</param>
        public FormatBuilder(string regex, string format, string exp1format, string exp2format, int reg1, int reg2, int exp1, int exp2)
            : this(regex, format, exp1format, exp2format, reg1, reg2, exp1, exp2, RegexOptions.None, false, null)
        {

        }

        #endregion

        #region Methods

        /// <summary>
        /// Evaluates an operand expression and returns a DotNetAsm.OperandFormat
        /// with captured subexpressions.
        /// </summary>
        /// <param name="expression">The operand expression to evaluate</param>
        /// <returns>A DotNetAsm.OperandFormat object</returns>
        public OperandFormat GetFormat(string expression)
        {
            OperandFormat fmt = null;
            if (_regex.IsMatch(expression))
            {
                var m = _regex.Match(expression);
                fmt = new OperandFormat
                {
                    Expression1 = m.Groups[_exp1Group].Value,
                    Expression2 = m.Groups[_exp2Group].Value
                };
                string exp1Format = _exp1Format;
                string exp2Format = _exp2Format;
                if (_evaluator != null)
                {
                    exp2Format = _evaluator.Eval(fmt.Expression2).ToString();
                    fmt.Expression2 = string.Empty; // we need to empty this because this is a format element, not expression!
                }
                if (_treatParenEnclosureAsExpr && fmt.Expression1.StartsWith("(") && fmt.Expression1.EndsWith(")"))
                {
                    if (fmt.Expression1.Equals(fmt.Expression1.FirstParenEnclosure()))
                        exp1Format = "(" + _exp1Format + ")";
                }
                fmt.FormatString = string.Format(_format,
                                    m.Groups[_reg1Group].Value.Replace(" ", ""),
                                    m.Groups[_reg2Group].Value.Replace(" ", ""),
                                    exp1Format,
                                    exp2Format);

            }
            return fmt;
        }

        public override string ToString() => _regex.ToString();

        #endregion
    }
}
