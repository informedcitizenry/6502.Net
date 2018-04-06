//-----------------------------------------------------------------------------
// Copyright (c) 2017, 2018 informedcitizenry <informedcitizenry@gmail.com>
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
using System.Text.RegularExpressions;

namespace DotNetAsm
{
    /// <summary>
    /// Encapsulates a single line of assembly source.
    /// </summary>
    public class SourceLine : IEquatable<SourceLine>, ICloneable
    {
        #region Members

        bool _doNotAssemble;

        bool _comment;

        #region Static Members

        static Regex _regThree;
        static Regex _regThreeAlt;
        static Regex _regTwo;
        static Regex _regOne;

        #endregion

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs an instance of a <see cref="T:DotNetAsm.SourceLine"/> object.
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
        /// Constructs an instance of a <see cref="T:DotNetAsm.SourceLine"/> object.
        /// </summary>
        public SourceLine() :
            this(string.Empty, 0, string.Empty)
        {
            
        }

        #region Static Constructors

        /// <summary>
        /// Initializes the <see cref="T:DotNetAsm.SourceLine"/> class.
        /// </summary>
        static SourceLine()
        {
            _regThree    = new Regex(@"^([^\s]+)\s+(([^\s]+)\s+(.+))$", RegexOptions.Compiled);
            _regThreeAlt = new Regex(@"^([^\s]+)\s*(=)\s*(.+)$",        RegexOptions.Compiled);
            _regTwo      = new Regex(@"^([^\s]+)\s+(.+)$",              RegexOptions.Compiled);
            _regOne      = new Regex(@"^([^\s]+)$",                     RegexOptions.Compiled);
        }

        #endregion

        #endregion

        #region Methods

        /// <summary>
        /// Parse the <see cref="DotNetAsm.SourceLine"/>'s SourceString property into its component line,
        /// instruction and operand.
        /// </summary>
        /// <param name="checkInstruction">A callback to determine which part of the source
        /// is the instruction.</param>
        /// <exception cref="System.Exception"></exception>
        public void Parse(Func<string, bool> checkInstruction)
        {
            int length = 0;
            for (; length < SourceString.Length; length++)
            {
                char c = SourceString[length];
                if (c == '"' || c == '\'')
                    length += SourceString.GetNextQuotedString(atIndex: length).Length - 1;
                else if (c == ';')
                    break;
            }
            var processed = SourceString.Substring(0, length).Trim();
            var m = _regThree.Match(processed);
            if (string.IsNullOrEmpty(m.Value) == false)
            {
                if (checkInstruction(m.Groups[1].Value))
                {
                    Instruction = m.Groups[1].Value;
                    Operand = m.Groups[2].Value;
                }
                else
                {
                    Label = m.Groups[1].Value;
                    Instruction = m.Groups[3].Value;
                    Operand = m.Groups[4].Value;
                }
            }
            else
            {
                m = _regThreeAlt.Match(processed);
                if (string.IsNullOrEmpty(m.Value) == false)
                {
                    Label = m.Groups[1].Value;
                    Instruction = m.Groups[2].Value;
                    Operand = m.Groups[3].Value;
                }
                else
                {
                    m = _regTwo.Match(processed);
                    if (string.IsNullOrEmpty(m.Value) == false)
                    {
                        if (checkInstruction(m.Groups[2].Value))
                        {
                            Label = m.Groups[1].Value;
                            Instruction = m.Groups[2].Value;
                        }
                        else
                        {
                            Instruction = m.Groups[1].Value;
                            Operand = m.Groups[2].Value;
                        }
                    }
                    else
                    {
                        m = _regOne.Match(processed);
                        if (string.IsNullOrEmpty(m.Value) == false)
                        {
                            if (checkInstruction(m.Groups[1].Value))
                                Instruction = m.Groups[1].Value;
                            else
                                Label = m.Groups[1].Value;
                        }
                    }
                }
            }
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
            if (DoNotAssemble)
                return string.Format("Do Not Assemble {0}", SourceString);
            return string.Format("Line {0} ${1:X4} L:{2} I:{3} O:{4}",
                                                        LineNumber
                                                      , PC
                                                      , Label
                                                      , Instruction
                                                      , Operand);
        }

        public override int GetHashCode() => LineNumber.GetHashCode() + 
                                             Filename.GetHashCode() + 
                                             SourceString.GetHashCode();

        #endregion

        #region IEquatable

        public bool Equals(SourceLine other) => 
                   (other.LineNumber == this.LineNumber &&
                    other.Filename == this.Filename &&
                    other.SourceString == this.SourceString);

        #endregion

        #region ICloneable

        public object Clone() => new SourceLine
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

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the <see cref="T:DotNetAsm.SourceLine"/>'s unique id number.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="T:DotNetAsm.SourceLine"/>'s Line number in the original source file.
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// Gets or sets the Program Counter of the assembly at the SourceLine.
        /// </summary>
        public long PC { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="T:DotNetAsm.SourceLine"/>'s original source filename.
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
        /// Gets or sets the flag determining whether the <see cref="T:DotNetAsm.SourceLine"/> 
        /// is actually part of a comment block. Setting this flag 
        /// also sets the flag to determine whether the SourceLine 
        /// is to be assembled. 
        /// </summary>
        public bool IsComment
        {
            get { return _comment; }
            set
            {
                _comment = value;
                if (_comment)
                    _doNotAssemble = _comment;
            }
        }

        public bool DoNotAssemble
        {
            get { return _doNotAssemble; }
            set
            {
                if (!IsComment)
                    _doNotAssemble = value;
            }
        }

        /// <summary>
        /// The <see cref="T:DotNetAsm.SourceLine"/>'s label/symbol. This can be determined using the Parse method.
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// The <see cref="T:DotNetAsm.SourceLine"/>'s instruction. This can be determined using the Parse method.
        /// </summary>
        public string Instruction { get; set; }

        /// <summary>
        /// The <see cref="T:DotNetAsm.SourceLine"/>'s operand. This can be determined using the Parse method.
        /// </summary>
        public string Operand { get; set; }

        #endregion
    }
}
