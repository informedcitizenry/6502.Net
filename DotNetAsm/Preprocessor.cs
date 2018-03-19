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
using System.IO;
using System.Linq;

namespace DotNetAsm
{
    /// <summary>
    /// A preprocessor class for the 6502.Net assembler.
    /// </summary>
    public class Preprocessor : AssemblerBase
    {
        #region Members

        Func<string, bool> _symbolNameFunc;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a Preprocessor object.
        /// </summary>
        /// <param name="controller">The assembly controller.</param>
        /// <param name="checkSymbol">A function to check for symbols such as labels or variables.</param>
        public Preprocessor(IAssemblyController controller,
                            Func<string, bool> checkSymbol)
            : base(controller)
        {
            FileRegistry = new HashSet<string>();
            _symbolNameFunc = checkSymbol;
            Reserved.DefineType("Directives", ".binclude", ".include", ".comment",  ".endcomment");
        }

        #endregion

        #region Methods

        /// <summary>
        /// Check if all quotes are properly closed.
        /// </summary>
        /// <param name="sourcelines">The list of <see cref="T:DotNetAsm.SourceLine"/>s.</param>
        void CheckQuotes(IEnumerable<SourceLine> sourcelines)
        {
            var nocomments = sourcelines.Where(l => !l.IsComment);
            foreach (var line in nocomments)
            {
                bool double_enclosed = false;
                for (int i = 0; i < line.Operand.Length; i++)
                {
                    var c = line.Operand[i];
                    if (!double_enclosed && c == '\'')
                    {
                        i += 2;
                        if (i >= line.Operand.Length || line.Operand[i] != '\'')
                        {
                            Controller.Log.LogEntry(line, ErrorStrings.QuoteStringNotEnclosed);
                            break;
                        }
                    }
                    else if (c == '"')
                    {
                        double_enclosed = !double_enclosed;
                    }
                }
                if (double_enclosed)
                {
                    Controller.Log.LogEntry(line, ErrorStrings.QuoteStringNotEnclosed);
                    break;
                }
            }
        }

        /// <summary>
        /// Preprocess all comment blocks, macro and segment definitions.
        /// </summary>
        /// <param name="sourcelines">The <see cref="T:System.Collections.Generic.IEnumerable&lt;DotNetAsm.SourceLine&gt;"/>collection</param>
        /// <returns></returns>
        public IEnumerable<SourceLine> Preprocess(IEnumerable<SourceLine> sourcelines)
        {
            // we can't do this check until all commenting has been processed
            ProcessCommentBlocks(sourcelines);
            CheckQuotes(sourcelines);
            return ProcessIncludes(sourcelines.Where(l => !l.IsComment));
        }

        /// <summary>
        /// Process Includes as source.
        /// </summary>
        /// <param name="listing">The source listing containing potential ".include" directives.</param>
        /// <returns>Returns the new source with the included sources expanded.</returns>
        IEnumerable<SourceLine> ProcessIncludes(IEnumerable<SourceLine> listing)
        {
            var includedLines = new List<SourceLine>();
            foreach (var line in listing)
            {
                if (line.Instruction.Equals(".include", Controller.Options.StringComparison))
                {
                    string filename = line.Operand;
                    if (filename.EnclosedInQuotes() == false)
                    {
                        Controller.Log.LogEntry(line, ErrorStrings.FilenameNotSpecified);
                        continue;
                    }
                    var inclistings = ConvertToSource(filename.TrimOnce('"'));
                    includedLines.AddRange(inclistings);
                }
                else if (line.Instruction.Equals(".binclude", Controller.Options.StringComparison))
                {
                    var args = line.Operand.CommaSeparate();
                    var openblock = new SourceLine();
                    if (args.Count > 1)
                    {
                        if (string.IsNullOrEmpty(line.Label) == false && _symbolNameFunc(line.Label) == false)
                        {
                            Controller.Log.LogEntry(line, ErrorStrings.LabelNotValid, line.Label);
                            continue;
                        }
                        if (line.Operand.EnclosedInQuotes() == false)
                        {
                            Controller.Log.LogEntry(line, ErrorStrings.None);
                            continue;
                        }
                        if (FileRegistry.Add(line.Operand.TrimOnce('"')) == false)
                        {
                            throw new Exception(string.Format(ErrorStrings.FilePreviouslyIncluded, line.Operand));
                        }
                        openblock.Label = line.Label;
                    }
                    else if (line.Operand.EnclosedInQuotes() == false)
                    {
                        Controller.Log.LogEntry(line, ErrorStrings.FilenameNotSpecified);
                        continue;
                    }
                    openblock.Instruction = ConstStrings.OPEN_SCOPE;
                    includedLines.Add(openblock);
                    var inclistings = ConvertToSource(line.Operand.TrimOnce('"'));

                    includedLines.AddRange(inclistings);
                    includedLines.Add(new SourceLine());
                    includedLines.Last().Instruction = ConstStrings.CLOSE_SCOPE;
                }
                else
                {
                    includedLines.Add(line);
                }
            }
            return includedLines;
        }

        /// <summary>
        /// Marks all SourceLines within comment blocks as comments.
        /// </summary>
        /// <param name="source">The source listing.</param>
        void ProcessCommentBlocks(IEnumerable<SourceLine> source)
        {
            bool incomments = false;
            foreach(var line in source)
            {
                if (!incomments)
                    incomments = line.Instruction.Equals(".comment", Controller.Options.StringComparison);
                
                line.IsComment = incomments;
                if (line.Instruction.Equals(".endcomment", Controller.Options.StringComparison))
                {
                    if (incomments)
                        incomments = false;
                    else
                        Controller.Log.LogEntry(line, ErrorStrings.ClosureDoesNotCloseBlock, line.Instruction);
                }   
            }
            if (incomments)
                throw new Exception(ErrorStrings.MissingClosure);
        }

        /// <summary>
        /// Converts a file to a SourceLine list.
        /// </summary>
        /// <param name="file">The filename.</param>
        /// <returns>A <see cref="T:System.Collections.Generic.IEnumerable&lt;DotNetAsm.SourceLine&gt;"/> d.</returns>
        public IEnumerable<SourceLine> ConvertToSource(string file)
        {
            if (FileRegistry.Add(file) == false)
                throw new Exception(string.Format(ErrorStrings.FilePreviouslyIncluded, file));

            if (File.Exists(file))
            {
                Console.WriteLine("Processing input file " + file + "...");
                int currentline = 1;
                var sourcelines = new List<SourceLine>();
                using (StreamReader reader = new StreamReader(File.Open(file, FileMode.Open)))
                {
                    while (reader.EndOfStream == false)
                    {
                        var unprocessedline = reader.ReadLine();
                        try
                        {
                            var line = new SourceLine(file, currentline, unprocessedline);
                            line.Parse(
                                delegate(string token)
                                {
                                    return Controller.IsInstruction(token) || Reserved.IsReserved(token) ||
                                        (token.StartsWith(".") && Macro.IsValidMacroName(token.Substring(1))) ||
                                        token == "=";
                                });
                            sourcelines.Add(line);
                        }
                        catch (SourceLine.QuoteNotEnclosedException)
                        {
                            Controller.Log.LogEntry(file, currentline, ErrorStrings.QuoteStringNotEnclosed);
                        }
                        currentline++;
                    }
                    sourcelines = Preprocess(sourcelines).ToList();
                }
                return sourcelines;
            }
            throw new FileNotFoundException(string.Format("Unable to open source file \"{0}\"", file));
        }

        public override bool IsReserved(string token) => Reserved.IsReserved(token);

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the file registry of input files.
        /// </summary>
        public HashSet<string> FileRegistry { get; set; }

        #endregion
    }
}