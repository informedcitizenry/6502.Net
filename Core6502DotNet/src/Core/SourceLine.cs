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
        /// <param name="indexInSource">The index in the original source the line began.</param>
        /// <param name="originalSource">The original (unprocessed) source.</param>
        /// <param name="head">The head token used to parse down to label, instruction, and operand tokens.</param>
        /// <param name="services">The shared <see cref="AssemblyServices"/> object.</param>
        /// <param name="ignoreParsingError">Ignore any parsing errors.</param>
        public SourceLine(string fileName, 
                          int lineNumber, 
                          int indexInSource,
                          string originalSource, 
                          Token head, 
                          AssemblyServices services,
                          bool ignoreParsingError)
        {
            LineNumber = lineNumber;
            Filename = fileName;
            IndexInSource = indexInSource;

            UnparsedSource = originalSource;
            
            if (head == null)
            {
                Label = Instruction = Operand = null;
                IsParsed = false;
                ParsedSource = string.Empty;
            }
            else
            {
                IsParsed = true;
                ParsedSource = services.Options.CaseSensitive ? originalSource : originalSource.ToLower();
                var firstChild = head.Children[0];
                // if there is an operand under first child treat it as a label
                if (firstChild.Children.Count > 0)
                {
                    Label = firstChild.Children[0];

                    if (firstChild.Children.Count > 1)
                    {
                        if (firstChild.Children[1].OperatorType == OperatorType.Binary &&
                             firstChild.Children[1].Name.Equals("="))
                        {
                            Instruction = new Token("=", 
                                                    "=", 
                                                    TokenType.Instruction, 
                                                    OperatorType.None, 
                                                    firstChild.Children[1].Position);
                            Operand = new Token(string.Empty, string.Empty,
                                                TokenType.Operator,
                                                OperatorType.Separator,
                                                firstChild.Children[1].Position + 1,
                                                firstChild.Children.Skip(2));
                            if (head.Children.Count > 1)
                                Operand.Children.AddRange(head.Children.Skip(1));
                            return;
                        }
                        if (!ignoreParsingError)
                            services.Log.LogEntry(Filename, LineNumber, firstChild.Children[1].Position,
                                       $"Unknown instruction \"{firstChild.Children[1].Name}\" found.");
                    }
                }
                if (head.Children.Count > 1)
                {
                    Instruction = head.Children[1];

                    if (head.Children.Count > 2)
                    {
                        if (head.Children.Count > 3)
                            services.Log.LogEntry(Filename, LineNumber, head.Children[3].Position,
                                    $"Unexpected expression \"{head.Children[3].Name}\". found");
                        else
                            Operand = head.Children[2];
                    }
                }
            }            
        }

        SourceLine(string fileName, 
                   int lineNumber, 
                   int indexInSource,
                   Token label, 
                   Token instruction, 
                   Token operand, 
                   bool isParsed, 
                   string unparsedSource,
                   string parsedSource)
        {
            Filename = fileName;
            LineNumber = lineNumber;
            IndexInSource = indexInSource;
            Label = label;
            Instruction = instruction;
            Operand = operand;
            IsParsed = isParsed;
            UnparsedSource = unparsedSource;
            ParsedSource = parsedSource;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Make a copy of the current source line, but with the specified line number.
        /// </summary>
        /// <param name="lineNumber">The line nummber of the copied source line.</param>
        /// <returns>The copied source line with the specified line number.</returns>
        public SourceLine WithLineNumber(int lineNumber) =>
                            new SourceLine(Filename,
                                            lineNumber,
                                            IndexInSource,
                                            Label,
                                            Instruction,
                                            Operand,
                                            IsParsed,
                                            UnparsedSource,
                                            ParsedSource);

        public SourceLine NoLabel() =>
            new SourceLine(Filename,
                            LineNumber,
                            IndexInSource,
                            null,
                            Instruction,
                            Operand,
                            IsParsed,
                            UnparsedSource,
                            ParsedSource);

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
                        if (child.Children.Count > 0)
                            AppendChildren(child.Children);
                        if (child.OperatorType == OperatorType.Open)
                            sb.Append(LexerParser.Groups[child.Name]);
                    }
                }
            }
            return sb.ToString();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the <see cref="SourceLine"/>'s label/symbol. 
        /// </summary>
        public Token Label { get; }

        /// <summary>
        /// Gets the name of the <see cref="SourceLine"/>'s label, if defined.
        /// </summary>
        public string LabelName => Label == null ? string.Empty : Label.Name;

        /// <summary>
        /// Gets the <see cref="SourceLine"/>'s instruction. 
        /// </summary>
        public Token Instruction { get; }

        /// <summary>
        /// Gets the name of the <see cref="SourceLine"/>'s instruction, if defined.
        /// </summary>
        public string InstructionName => Instruction == null ? string.Empty : Instruction.Name;

        /// <summary>
        /// The collection of tokens that make up the <see cref="SourceLine"/>'s operand. This can be determined using the Parse method.
        /// </summary>
        public Token Operand { get; }

        /// <summary>
        /// Gets or sets the <see cref="SourceLine"/>'s Line number in the original source file.
        /// </summary>
        public int LineNumber { get; }

        /// <summary>
        /// Gets the position in source the line's own source began.
        /// </summary>
        public int IndexInSource { get; }

        /// <summary>
        /// Gets or sets the <see cref="SourceLine"/>'s original source filename.
        /// </summary>
        public string Filename { get; }

        /// <summary>
        /// Gets or sets the parsed source of the original source string.
        /// </summary>
        public string ParsedSource { get; }

        /// <summary>
        /// Gets or sets the original (unparsed) source string.
        /// </summary>
        public string UnparsedSource { get; }

        /// <summary>
        /// Gets a flag indicating whether this <see cref="SourceLine"/> has been parsed.
        /// </summary>
        public bool IsParsed { get; }

        /// <summary>
        /// Gets a flag determining if the operand has any children tokens.
        /// </summary>
        public bool OperandHasToken
            => Operand != null && !string.IsNullOrEmpty(Operand.ToString());

        /// <summary>
        /// Gets the operand's expression string.
        /// </summary>
        public string OperandExpression => Operand == null ? string.Empty : Operand.ToString().Trim();

        #endregion
    }
}