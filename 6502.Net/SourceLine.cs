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
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;

namespace Asm6502.Net
{
    /// <summary>
    /// SourceLine class encapsulates a single line of assembly source.
    /// </summary>
    public class SourceLine : IEquatable<SourceLine>, ICloneable
    {
        #region Exception

        /// <summary>
        /// An exception class to handle strings not properly closed in quotation marks
        /// </summary>
        public class QuoteNotEnclosedException : Exception
        {
            public override string Message
            {
                get
                {
                    return Resources.ErrorStrings.QuoteStringNotEnclosed;
                }
            }
        }

        #endregion

        #region Members

        private bool doNotAssemble_;

        private bool definition_;

        private bool comment_;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a new SourceLine object.
        /// </summary>
        /// <param name="filename">The original source filename.</param>
        /// <param name="linenumber">The original source line number.</param>
        /// <param name="source">The unprocessed source string.</param>
        public SourceLine(string filename, int linenumber, string source)
        {
            Assembly = new List<byte>();
            Filename = filename;
            LineNumber = linenumber;
            Scope = 
            Label = 
            Instruction = 
            Operand = 
            Disassembly = string.Empty;
            SourceString = source;
        }

        /// <summary>
        /// Constructs a new SourceLine object.
        /// </summary>
        public SourceLine() :
            this(string.Empty, 0, string.Empty)
        {
            
        }

        #endregion

        #region Methods

        /// <summary>
        /// Take the token and determine whether it is a label, instruction, or operand 
        /// component of the source.
        /// </summary>
        /// <param name="checkReserved">A callback to test if the token is a reserved word,
        /// and thus is an instruction.</param>
        /// <param name="checkSymbol">A callback to test if the token is a symbol, 
        /// and thus is a label.</param>
        /// <param name="token">The token to parse.</param>
        private void SetLineToken(Func<string, bool> checkReserved, Func<string, bool> checkSymbol, string token)
        {
            var trimmed = token.Trim();
            if (string.IsNullOrEmpty(Instruction) &&
                (checkReserved(trimmed) || trimmed.EndsWith("=")))
            {
                if (trimmed.EndsWith("="))
                {
                    if (string.IsNullOrEmpty(Label))
                        Label = trimmed.Trim('=');
                    trimmed = "=";
                }
                Instruction = trimmed;
            }
            else if (string.IsNullOrEmpty(Label) && string.IsNullOrEmpty(Instruction) &&
            (checkSymbol(trimmed) || Regex.IsMatch(trimmed, @"[-\+\*]")))
                Label = trimmed;
            else
                Operand += token;
        }

        /// <summary>
        /// Parse the SourceLine's SourceString property into its component line,
        /// instruction and operand.
        /// </summary>
        /// <param name="checkReserved">A callback to determine which part of the source
        /// is the instruction.</param>
        /// <param name="checkSymbol">A callback to determinw which part of the source
        /// is a label.</param>
        /// <exception cref="SourceLine.QuoteNotEnclosedException">SourceLine.QuoteNotEnclosedException</exception>
        public void Parse(Func<string, bool> checkReserved, Func<string, bool> checkSymbol)
        {
            if (string.IsNullOrWhiteSpace(SourceString)) return;
            bool double_enclosed = false;
            bool single_enclosed = false;
            StringBuilder sb = new StringBuilder();
            string unprocessedTrim = SourceString.Trim();
            for (int n = 0; n < unprocessedTrim.Length; n++)
            {
                char c = unprocessedTrim[n];
                if (!single_enclosed && !double_enclosed && c == ';')
                    break;
                if (c == '"' && !single_enclosed)
                    double_enclosed = !double_enclosed;
                else if (c == '\'' && !double_enclosed)
                    single_enclosed = !single_enclosed;

                sb.Append(c);
                
                if ((char.IsWhiteSpace(c) || c == '=') && !double_enclosed && !single_enclosed)
                {
                    if (string.IsNullOrEmpty(Label) ||
                        string.IsNullOrEmpty(Instruction) ||
                        string.IsNullOrEmpty(Operand))
                    {
                        SetLineToken(checkReserved, checkSymbol, sb.ToString());
                        sb.Clear();
                    }
                }
            }
            if (single_enclosed || double_enclosed)
            {
                throw new QuoteNotEnclosedException();
            }
            SetLineToken(checkReserved, checkSymbol, sb.ToString());
            Label = Label.TrimEnd(':');
            Operand = Operand.Trim();
        }

        /// <summary>
        /// Does a comma-separated-value analysis on the SourceLine's operand
        /// and returns the individual value as a List&lt;string&gt;
        /// </summary>
        /// <returns>Returns a List&lt;string&gt; collection of the values.</returns>
        public List<string> CommaSeparateOperand()
        {
            List<string> csv = new List<string>();

            if (string.IsNullOrEmpty(Operand))
                return csv;

            bool double_enclosed = false;
            bool single_enclosed = false;
            bool paren_enclosed = false;

            StringBuilder sb = new StringBuilder();
            
            for (int i = 0; i < Operand.Length; i++)
            {
                char c = Operand[i];
                if (double_enclosed)
                {
                    sb.Append(c);
                    if (c == '"')
                    {
                        double_enclosed = false;
                        if (i == Operand.Length - 1)
                            csv.Add(sb.ToString().Trim());
                    }
                }
                else if (single_enclosed)
                {
                    sb.Append(c);
                    if (c == '\'')
                    {
                        single_enclosed = false;
                        if (i == Operand.Length - 1)
                            csv.Add(sb.ToString().Trim());
                    }
                }
                else if (paren_enclosed)
                {
                    if (c == '"' && !single_enclosed)
                    {
                        double_enclosed = true;
                    }
                    else if (c == '\'' && !double_enclosed)
                    {
                        single_enclosed = true;
                    }

                    sb.Append(c);
                    if (c == ')' && !double_enclosed && !single_enclosed)
                    {
                        paren_enclosed = false;
                        if (i == Operand.Length - 1)
                            csv.Add(sb.ToString().Trim());
                    }
                }
                else
                {
                    if (c == '"')
                    {
                        double_enclosed = true;
                    }
                    else if (c == '\'')
                    {
                        single_enclosed = true;
                    }
                    else if (c == '(')
                    {
                        paren_enclosed = true;
                    }
                    else if (c == ',')
                    {
                        csv.Add(sb.ToString().Trim());
                        sb.Clear();
                        continue;
                    }
                    sb.Append(c);
                    if (i == Operand.Length - 1)
                        csv.Add(sb.ToString().Trim());
                }
            }
            if (Operand.Last().Equals(','))
                csv.Add(string.Empty);
            return csv;
        }

        /// <summary>
        /// Gets the scope of the SourceLine.
        /// </summary>
        /// <param name="excludeNormal">Exclude "Normal" scopes.</param>
        /// <returns></returns>
        public string GetScope(bool excludeNormal)
        {
            if (!excludeNormal)
                return Scope;
            var scope_components = Scope.Split('.').ToList();
            if (scope_components.Count > 0 && scope_components.Last().EndsWith("@"))
                scope_components.RemoveAt(scope_components.Count - 1);
            return string.Join(".", scope_components);
        }

        /// <summary>
        /// A unique identifier combination of the source's filename and line number.
        /// </summary>
        /// <returns>The identifier string.</returns>
        public string SourceInfo()
        {
            string file = Filename;
            if (file.Length > 14)
                file = Filename.Substring(0, 14) + "...";
            return string.Format("{0, -17}({1})", file, LineNumber);
        }

        #endregion

        #region Override Methods

        public override string ToString()
        {
            string source = string.Empty;
            if (string.IsNullOrEmpty(SourceString) == false)
                source = SourceString.Trim();
            if (source.Length > 10)
                source = source.Substring(0, 10);
            return string.Format("Assemble? {0}: ${1:X}:{2} {3} {4} {5} ({6})",
                                                      (!DoNotAssemble).ToString(),
                                                      PC,
                                                      Label,
                                                      Instruction,
                                                      Operand,
                                                      Disassembly,
                                                      source);
        }

        public override int GetHashCode()
        {
            return LineNumber.GetHashCode() + Filename.GetHashCode() + SourceString.GetHashCode();
        }

        #endregion

        #region IEquatable

        public bool Equals(SourceLine other)
        {
            return (other.LineNumber == this.LineNumber &&
                    other.Filename == this.Filename &&
                    other.SourceString == this.SourceString);
        }

        #endregion

        #region ICloneable

        public object Clone()
        {
            SourceLine clone = new SourceLine
            {
                LineNumber = this.LineNumber,
                Filename = this.Filename,
                Label = this.Label,
                Operand = this.Operand,
                Instruction = this.Instruction,
                SourceString = this.SourceString,
                Scope = this.Scope,
                PC = this.PC
            };
            return clone;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the SourceLine's unique id number.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the SourceLine's Line number in the original source file.
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// Gets or sets the Program Counter of the assembly at the SourceLine.
        /// </summary>
        public int PC { get; set; }

        /// <summary>
        /// Gets or sets the SourceLine's original source filename.
        /// </summary>
        public string Filename { get; set; }

        /// <summary>
        /// Gets or sets the SourceLine scope.
        /// </summary>
        public string Scope { get; set; }

        /// <summary>
        /// Gets or sets the individual assembled bytes.
        /// </summary>
        public List<byte> Assembly { get; set; }

        /// <summary>
        /// Gets or sets the original (unparsed) source string.
        /// </summary>
        public string SourceString { get; set; }

        /// <summary>
        /// Gets or sets the disassembled representation of the source.
        /// </summary>
        public string Disassembly { get; set; }

        /// <summary>
        /// Gets or sets the flag determining whether the SourceLine 
        /// instruction makes it a definition, not a regular directive.
        /// Setting this flag also sets the flag to determine whether
        /// the SourceLine is to be assembled. Note that if the
        /// Comment flag is already set then setting this flag will
        /// have no effect.
        /// </summary>
        public bool IsDefinition
        {
            get { return definition_; }
            set
            {
                if (!comment_)
                {
                    definition_ = value;
                    if (definition_)
                        doNotAssemble_ = definition_;
                }
            }
        }

        /// <summary>
        /// Gets or sets the flag determining whether the SourceLine 
        /// is actually part of a comment block. Setting this flag 
        /// also sets the flag to determine whether the SourceLine 
        /// is to be assembled. 
        /// </summary>
        public bool IsComment
        {
            get { return comment_; }
            set
            {
                comment_ = value;
                if (comment_)
                    doNotAssemble_ = comment_;
            }
        }

        public bool DoNotAssemble
        {
            get { return doNotAssemble_; }
            set
            {
                if (!IsDefinition && !IsComment)
                    doNotAssemble_ = value;
            }
        }

        /// <summary>
        /// The SourceLine's label/symbol. This can be determined using the Parse method.
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// The SourceLine's instruction. This can be determined using the Parse method.
        /// </summary>
        public string Instruction { get; set; }

        /// <summary>
        /// The SourceLine's operand. This can be determined using the Parse method.
        /// </summary>
        public string Operand { get; set; }

        #endregion
    }
}
