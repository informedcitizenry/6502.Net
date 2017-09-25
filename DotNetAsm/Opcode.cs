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
        #region Methods

        /// <summary>
        /// Lookup an opcode from a supplied System.Collections.Generic.IEnumerable&lt;Opcode&gt;
        /// by its disassembly format.
        /// </summary>
        /// <param name="format">The disassembly format to use to lookup the opcode</param>
        /// <param name="opcodes">The System.Collections.Generic.IEnumerable&lt;Opcode&gt; to look in</param>
        /// <returns>The DotNetAsm.Opcode matching the format</returns>
        public static Opcode LookupOpcode(string format, IEnumerable<Opcode> opcodes)
        {
            Opcode opc = null;

            Opcode[] opcList = opcodes.ToArray();

            for (int i = 0; i < 0x100; i++)
            {
                if (opcList[i] == null)
                    continue;
                if (opcList[i].Extension != null)
                {
                    Opcode result = LookupOpcode(format, opcList[i].Extension.ToArray());
                    if (result != null)
                    {
                        opc = result;
                        opc.Index = i | (result.Index << 8);
                        break;
                    }
                }
                else if (opcList[i].DisasmFormat.Equals(format))
                {
                    opc = opcList[i];
                    opc.Index = i;
                    break;
                }
            }
            return opc;
        }

        /// <summary>
        /// Lookup the index of an opcode in a supplied System.Collections.Generic.IEnumerable&lt;Opcode&gt;
        /// from its disassembly format.
        /// </summary>
        /// <param name="format">The disassembly format to use to lookup the opcode</param>
        /// <param name="opcodes">The System.Collections.Generic.IEnumerable&lt;Opcode&gt; to look in</param>
        /// <returns>The index of the DotNetAsm.Opcode matching the format</returns>
        public static int LookupOpcodeIndex(string format, IEnumerable<Opcode> opcodes)
        {
            Opcode[] opcList = opcodes.ToArray();
            int index = -1;

            for (int i = 0; i < 0x100; i++)
            {
                if (opcList[i] == null)
                    continue;
                if (opcList[i].Extension != null)
                {
                    Opcode result = LookupOpcode(format, opcList[i].Extension.ToArray());
                    if (result != null)
                    {
                        index = i | (result.Index << 8);
                        break;
                    }
                }
                else if (opcList[i].DisasmFormat.Equals(format))
                {
                    index = i;
                    break;
                }
            }
            return index;
        }

        /// <summary>
        /// Lookup an opcode from a supplied System.Collections.Generic.IEnumerable&lt;string&gt;
        /// by its disassembly format.
        /// </summary>
        /// <param name="format">The disassembly format to use to lookup the opcode</param>
        /// <param name="opcodes">The System.Collections.Generic.IEnumerable&lt;string&gt; to look in</param>
        /// <returns>The index of the DotNetAsm.Opcode matching the format</returns>
        public static int LookupOpcodeIndex(string format, IEnumerable<string> opcodes)
        {
            string[] opcList = opcodes.ToArray();
            for (int i = 0; i < opcList.Length; i++ )
            {
                if (format.Equals(opcList[i]))
                    return i;
            }
            return -1;
        }

        #endregion

        #region Properties

        /// <summary>
        /// The System.Collections.Generic.IEnumerable&lt;Opcode&gt; that extends from
        /// this opcode.
        /// </summary>
        public IEnumerable<Opcode> Extension { get; set; }

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


        #endregion
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

        private Regex _regex;
        private string _format;
        private string _exp1Format;
        private string _exp2Format;
        private int _reg1Group, _exp1Group;
        private int _reg2Group, _exp2Group;
        private bool _treatParenEnclosureAsExpr;
        private IEvaluator _evaluator;

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
        /// <param name="caseSensitive">Indicates the evaluation is case-sensitive</param>
        /// <param name="treatParenAsExpr">If the first subexpression is enclosed in 
        /// paranetheses, enclose the subexpression's position in the final format
        /// inside paranetheses as well</param>
        /// <param name="evaluator">If not null, a DotNetAsm.IEvaluator to evaluate the second 
        /// subexpression as part of the final format</param>
        public FormatBuilder(string regex, string format, string exp1format, string exp2format, int reg1, int reg2, int exp1, int exp2, bool caseSensitive, bool treatParenAsExpr, IEvaluator evaluator)
        {
            RegexOptions options = caseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;
            _regex = new Regex(regex, options | RegexOptions.Compiled);
            _format = format;
            _exp1Format = exp1format;
            _exp2Format = exp2format;
            _reg1Group = reg1; _exp1Group = exp1;
            _reg2Group = reg2; _exp2Group = exp2;
            _treatParenEnclosureAsExpr = treatParenAsExpr;
            _evaluator = evaluator;
        }

        /// <summary>
        /// Constructs an instance of a z80DotNet.z80Asm.FormatBuilder class.
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
        /// <param name="caseSensitive">Indicates the evaluation is case-sensitive</param>
        /// <param name="treatParenAsExpr">If the first subexpression is enclosed in 
        /// paranetheses, enclose the subexpression's position in the final format
        /// inside paranetheses as well</param>
        public FormatBuilder(string regex, string format, string exp1format, string exp2format, int reg1, int reg2, int exp1, int exp2, bool caseSensitive, bool treatParenAsExpr)
            : this(regex, format, exp1format, exp2format, reg1, reg2, exp1, exp2, caseSensitive, treatParenAsExpr, null)
        {

        }

        /// <summary>
        /// Constructs an instance of a z80DotNet.z80Asm.FormatBuilder class.
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
        /// <param name="caseSensitive">Indicates the evaluation is case-sensitive</param>
        /// <param name="evaluator">If not null, a DotNetAsm.IEvaluator to evaluate the second 
        /// subexpression as part of the final format</param>
        public FormatBuilder(string regex, string format, string exp1format, string exp2format, int reg1, int reg2, int exp1, int exp2, bool caseSensitive, IEvaluator evaluator)
            : this(regex, format, exp1format, exp2format, reg1, reg2, exp1, exp2, caseSensitive, false, evaluator)
        {

        }

        /// <summary>
        /// Constructs an instance of a z80DotNet.z80Asm.FormatBuilder class.
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
        /// <param name="caseSensitive">Indicates the evaluation is case-sensitive</param>
        public FormatBuilder(string regex, string format, string exp1format, string exp2format, int reg1, int reg2, int exp1, int exp2, bool caseSensitive)
            : this(regex, format, exp1format, exp2format, reg1, reg2, exp1, exp2, caseSensitive, false, null)
        {

        }

        #endregion

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
                fmt = new OperandFormat();
                fmt.Expression1 = m.Groups[_exp1Group].Value;
                fmt.Expression2 = m.Groups[_exp2Group].Value;
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

        public override string ToString()
        {
            return _regex.ToString();
        }
    }

}
