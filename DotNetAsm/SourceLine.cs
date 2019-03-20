//-----------------------------------------------------------------------------
// Copyright (c) 2017-2019 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;

namespace DotNetAsm
{
    /// <summary>
    /// Encapsulates a single line of assembly source.
    /// </summary>
    public sealed class SourceLine : IEquatable<SourceLine>, ICloneable
    {
        #region Members

        bool _doNotAssemble;

        bool _comment;

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

        #endregion

        #region Methods

        /// <summary>
        /// Parse the <see cref="DotNetAsm.SourceLine"/>'s SourceString property 
        /// into its component line, instruction and operand.
        /// </summary>
        /// <param name="isInstruction">A callback to determine which part of the source
        /// is the instruction.</param>
        /// <returns>A <see cref="System.Collections.Generic.IEnumerable&lt;DotNetAsm.SourceLine&gt;"/>.
        /// This collection will be empty if the line's SourceString does not contain a
        /// compound instruction.</returns>
        public IEnumerable<SourceLine> Parse(Func<string, bool> isInstruction)
        {
            return Parse(isInstruction, true);
        }

        /// <summary>
        /// Parse the <see cref="DotNetAsm.SourceLine"/>'s SourceString property 
        /// into its component line, instruction and operand.
        /// </summary>
        /// <param name="isInstruction">A callback to determine which part of the source
        /// is the instruction.</param>
        /// <param name="allowLabel">Allow a label to be defined</param>
        /// <returns>A <see cref="System.Collections.Generic.IEnumerable&lt;DotNetAsm.SourceLine&gt;"/>.
        /// This collection will be empty if the line's SourceString does not contain a
        /// compound instruction.</returns>
        public IEnumerable<SourceLine> Parse(Func<string, bool> isInstruction, bool allowLabel)
        {
            var tokenBuilder = new StringBuilder();
            var len = SourceString.Length;
            List<SourceLine> compounds = new List<SourceLine> { this };
            Label = Instruction = Operand = string.Empty;
            int instructionIndex = 0;
            int i;
            for (i = 0; i < len; i++)
            {
                var c = SourceString[i];
                if (char.IsWhiteSpace(c) || c == ';' || c == ':' || i == len - 1)
                {
                    // stop at a white space or the last character in the string

                    // if token not yet being built skip whitspace
                    if (char.IsWhiteSpace(c) && tokenBuilder.Length == 0)
                    {
                        continue;
                    }

                    if (!char.IsWhiteSpace(c) && c != ';' && c != ':')
                        tokenBuilder.Append(c);

                    if (string.IsNullOrEmpty(Instruction))
                    {
                        var token = tokenBuilder.ToString();
                        if (string.IsNullOrEmpty(Label) && allowLabel)
                        {
                            if (isInstruction(token))
                            {
                                instructionIndex = i - token.Length;
                                Instruction = token;
                            }
                            else
                            {
                                Label = token;
                            }
                        }
                        else
                        {
                            instructionIndex = i - token.Length;
                            Instruction = token;
                        }
                        tokenBuilder.Clear();
                    }
                    else if (char.IsWhiteSpace(c) && i < len - 1)
                    {
                        // operand can include white spaces, so capture...
                        tokenBuilder.Append(c);
                    }
                    if (c == ';' || c == ':')
                    {
                        if (c == ':' && i < len - 1)
                        {
                            var compoundLine = new SourceLine
                            {
                                Filename = this.Filename,
                                LineNumber = this.LineNumber
                            };
                            var newSource = this.SourceString.Substring(i + 1);
                            compoundLine.SourceString = newSource.PadLeft(instructionIndex + newSource.Length);

                            SourceString = SourceString.Substring(0, i);
                            // and parsed compound (and any others)
                            compounds.AddRange(compoundLine.Parse(isInstruction, false));
                        }
                        break;
                    }
                }
                else if (c == '"' || c == '\'')
                {
                    // process quotes separately
                    var quoted = SourceString.GetNextQuotedString(atIndex: i);
                    tokenBuilder.Append(quoted);
                    i += quoted.Length - 1;
                }
                else if (c == '=' && string.IsNullOrEmpty(Instruction))
                {
                    // constructions such as label=value must be picked up 
                    // so the instruction is the assignment operator
                    if (string.IsNullOrEmpty(Label))
                        Label = tokenBuilder.ToString();
                    Instruction = "=";
                    instructionIndex = i - 1;
                    tokenBuilder.Clear();
                }
                else
                {
                    tokenBuilder.Append(c);
                }
            }
            Operand = tokenBuilder.ToString().TrimEnd();
            return compounds;
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

        public override int GetHashCode() => LineNumber.GetHashCode() | 
                                             Filename.GetHashCode() |
                                             SourceString.GetHashCode();

        public override bool Equals(object obj)
                => obj != null && 
                   (ReferenceEquals(this, obj) ||
                    (obj is SourceLine && this.Equals((SourceLine)obj)));

        #endregion

        #region IEquatable

        public bool Equals(SourceLine other) => 
                    other.LineNumber == LineNumber &&
                    other.Filename.Equals(Filename) &&
                    other.SourceString.Equals(SourceString);

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
