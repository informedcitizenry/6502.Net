//-----------------------------------------------------------------------------
// Copyright (c) 2017-2019 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace DotNetAsm
{
    /// <summary>
    /// Encapsulates a single line of assembly source.
    /// </summary>
    public sealed class SourceLine : ICloneable, IEquatable<SourceLine>
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
        /// Reset the SourceLine's label, instruction and operand.
        /// </summary>
        public void Reset() => Label = Instruction = Operand = string.Empty;

        #region Override Methods

        public override string ToString()
        {
            if (DoNotAssemble)
                return string.Format("Do Not Assemble {0}", SourceString);

            if (IsParsed)
                return string.Format("Line {0} ${1:X4} [ID={2}] L:{3} I:{4} O:{5}",
                                                            LineNumber
                                                          , PC
                                                          , Id
                                                          , Label.Substring(0, 30)
                                                          , Instruction
                                                          , Operand.Substring(0, 30));
            return string.Format("Line {0} ${1:X4} [ID={2}] {3}",
                                                        LineNumber
                                                      , PC
                                                      , Id
                                                      , SourceString);
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

        /// <summary>
        /// Gets a flag indicating whether this <see cref="T:DotNetAsm.SourceLine"/> has been parsed.
        /// </summary>
        /// <value><c>true</c> if the line has been parsed; otherwise, <c>false</c>.</value>
        public bool IsParsed
        {
            get => !string.IsNullOrEmpty(Label) || !string.IsNullOrEmpty(Instruction);
        }

        #endregion
    }
}
