//-----------------------------------------------------------------------------
// Copyright (c) 2017-2020 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Core6502DotNet
{
    /// <summary>
    /// Represents one line of source code in the original
    /// assembly source.
    /// </summary>
    public class SourceLine
    {
        #region Constructors

        /// <summary>
        /// Constructs a new instance of the source line class.
        /// </summary>
        /// <param name="fileName">The source file name.</param>
        /// <param name="lineNumber">The source line number.</param>
        public SourceLine(string fileName, int lineNumber)
        {
            Label = Instruction = Operand = null;

            LineNumber = lineNumber;
            Filename = fileName;

            IsParsed = false;

            UnparsedSource =
            ParsedSource = string.Empty;

            Assembly = new List<byte>();
        }

        public SourceLine(string fileName, int lineNumber, string originalSource, Token parent, bool ignoreErrors)
            : this(fileName, lineNumber)
        {
            UnparsedSource = originalSource;
            ParsedSource = Assembler.Options.CaseSensitive ? originalSource : originalSource.ToLower();
            ParseOne(parent, ignoreErrors);
        }

        SourceLine(string originalSource, Token parentSep, bool ignoreErrors)
        {
            UnparsedSource = originalSource.Substring(parentSep.Position);
            ParsedSource = string.Join(' ', parentSep.Children);
            ParseOne(parentSep, ignoreErrors);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Perform a deep-copy of the current source line.
        /// </summary>
        /// <returns>A clone of the current source line.</returns>
        public SourceLine Clone()
        {
            var copy = new SourceLine(Filename, LineNumber)
            {
                ParsedSource = ParsedSource,
                UnparsedSource = UnparsedSource,
            };
            if (Label != null)
                copy.Label = Label.Clone();
            if (Instruction != null)
                copy.Instruction = Instruction.Clone();
            if (Operand != null)
                copy.Operand = Operand.Clone();
            return copy;
        }

        void ParseOne(Token parentSep, bool ignoreErrors)
        {
            IsParsed = true;
            Token firstChild = parentSep.Children[0];
            // if there is an operand under first child treat it as a label
            if (firstChild.HasChildren)
            {
                Label = firstChild.Children[0];

                if (firstChild.Children.Count > 1)
                {
                    if (firstChild.Children[1].OperatorType == OperatorType.Binary &&
                         firstChild.Children[1].Name.Equals("="))
                    {
                        Instruction = firstChild.Children[1];
                        Instruction.Type = TokenType.Instruction;
                        Operand = new Token
                        {
                            Type = TokenType.Operator,
                            OperatorType = OperatorType.Separator,
                            Children = firstChild.Children.Skip(2).ToList()
                        };
                        if (parentSep.Children.Count > 1)
                            Operand.Children.AddRange(parentSep.Children.Skip(1));
                        return;
                    }
                    Assembler.Log.LogEntry(Filename, LineNumber, firstChild.Children[1].Position,
                               $"Unknown instruction \"{firstChild.Children[1].Name}\" found.", true);
                }
            }
            if (parentSep.Children.Count > 1)
            {
                // second child should either be an instruction or nothing
                if (parentSep.Children.Count(t => t.Type == TokenType.Instruction) != 1)
                {
                    if (!ignoreErrors)
                    {
                        Token secondInstruction = parentSep.Children.Skip(2).First(t => t.Type == TokenType.Instruction);
                        throw new ExpressionException(secondInstruction.Position,
                         $"Additional instruction \"{secondInstruction.Name}\" found in statement. Use the \":\" operator for compound statements.");
                    }
                }
                Instruction = parentSep.Children[1];

                if (parentSep.Children.Count > 2)
                    Operand = parentSep.Children[2];
            }
        }

        /// <summary>
        /// Reset the <see cref="SourceLine"/>'s internal state.
        /// </summary>
        public void Reset()
        {
            ParsedSource = string.Empty;
            Label = Instruction = Operand = null;
        }

        public override string ToString()
        {
            if (!IsParsed)
                return ParsedSource;
            var sb = new StringBuilder();
            if (Label != null)
                sb.Append($"[Label]: {LabelName} ");
            if (Instruction != null)
                sb.Append($"[Instruction]: {InstructionName} ");
            if (Operand != null)
            {
                sb.Append("[Operand]: ");
                AppendChildren(Operand.Children);
                void AppendChildren(IEnumerable<Token> children)
                {
                    foreach (Token child in children)
                    {
                        if (!string.IsNullOrEmpty(child.Name))
                            sb.Append(child.Name);
                        if (child.HasChildren)
                            AppendChildren(child.Children);
                        if (child.OperatorType == OperatorType.Open)
                            sb.Append(Evaluator.Groups[child.Name]);
                    }
                }
            }
            return sb.ToString();
        }

        #endregion

        #region Properties

        /// <summary>
        /// The <see cref="SourceLine"/>'s label/symbol. This can be determined using the Parse method.
        /// </summary>
        public Token Label { get; set; }

        /// <summary>
        /// Gets the name of the <see cref="SourceLine"/>'s label, if defined.
        /// </summary>
        public string LabelName => Label == null ? string.Empty : Label.Name;

        // <summary>
        /// The <see cref="SourceLine"/>'s instruction. This can be determined using the Parse method.
        /// </summary>
        public Token Instruction { get; set; }

        /// <summary>
        /// Gets the name of the <see cref="SourceLine"/>'s instruction, if defined.
        /// </summary>
        public string InstructionName => Instruction == null ? string.Empty : Instruction.Name;

        /// <summary>
        /// The collection of tokens that make up the <see cref="SourceLine"/>'s operand. This can be determined using the Parse method.
        /// </summary>
        public Token Operand { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="SourceLine"/>'s Line number in the original source file.
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="SourceLine"/>'s original source filename.
        /// </summary>
        public string Filename { get; set; }

        /// <summary>
        /// Gets or sets the individual assembled bytes.
        /// </summary>
        public List<byte> Assembly { get; set; }

        /// <summary>
        /// Gets or sets the parsed source of the original source string.
        /// </summary>
        public string ParsedSource { get; set; }

        /// <summary>
        /// Gets or sets the original (unparsed) source string.
        /// </summary>
        public string UnparsedSource { get; set; }

        /// <summary>
        /// Gets a flag indicating whether this <see cref="SourceLine"/> has been parsed.
        /// </summary>
        /// <value><c>true</c> if the line has been parsed; otherwise, <c>false</c>.</value>
        public bool IsParsed { get; set; }

        /// <summary>
        /// Gets a flag determining if the operand has any children tokens.
        /// </summary>
        public bool OperandHasToken => Operand != null &&
                                       Operand.HasChildren &&
                                       Operand.Children.Any(c => !string.IsNullOrEmpty(c.Name) || c.HasChildren);

        /// <summary>
        /// Gets the operand's expression string.
        /// </summary>
        public string OperandExpression => Operand != null ? Operand.ToString() : string.Empty;

        #endregion
    }
}
