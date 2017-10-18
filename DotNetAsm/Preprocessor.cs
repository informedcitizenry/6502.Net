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
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace DotNetAsm
{
    /// <summary>
    /// A preprocessor class for the 6502.Net assembler.
    /// </summary>
    public class Preprocessor : AssemblerBase
    {
        #region Constructors

        /// <summary>
        /// Constructs a Preprocessor object.
        /// </summary>
        /// <param name="controller">The assembly controller.</param>
        /// <param name="checkReserved">A function to check for reserved keywords such as 
        /// mnemonics or pseudo-ops.</param>
        /// <param name="checkSymbol">A function to check for symbols such as labels.</param>
        public Preprocessor(IAssemblyController controller,
                            Func<string, bool> checkSymbol)
            : base(controller)
        {
            FileRegistry = new HashSet<string>();
            SymbolNameFunc = checkSymbol;
            Macros = new Dictionary<string, Macro>();
            Scope = new Stack<string>();

            Reserved.DefineType("Directives", new string[]
                {
                    ".binclude", ".include",
                    ".comment",  ".endcomment",
                });
        }

        #endregion

        #region Methods

        /// <summary>
        /// Process the list of source line blocks whose closures are denoted in the
        /// openBlock and closeBlock closures.
        /// </summary>
        /// <param name="listing">The source listing.</param>
        /// <param name="lines">The processed lines.</param>
        /// <param name="openBlock">The open block keyword.</param>
        /// <param name="closeBlock">The block closure keyword.</param>
        /// <param name="processor">The method to process the block within the 
        /// block closures.</param>
        /// <param name="includeInSource">Include the processed block in the final source.</param>
        /// <returns>Returns a SourceLine list of processed blocks.</returns>
        private IEnumerable<SourceLine> ProcessBlocks(IEnumerable<SourceLine> listing,
            List<SourceLine> lines,
            string openBlock,
            string closeBlock,
            ProcessBlock processor,
            bool includeInSource)
        {
            if (processor == null)
            {
                return lines;
            }
            var allopens = lines.AllIndexesOf(l => string.IsNullOrEmpty(l.Instruction) == false && openBlock.Equals(l.Instruction, Controller.Options.StringComparison));
            var allclosed = lines.AllIndexesOf(l => string.IsNullOrEmpty(l.Instruction) == false && closeBlock.Equals(l.Instruction, Controller.Options.StringComparison));

            List<SourceLine> beforeblock = new List<SourceLine>(), afterblock = new List<SourceLine>();
            if (allclosed.Count != allopens.Count)
            {
                if (allclosed.Count < allopens.Count)
                    Controller.Log.LogEntry(listing.Last(), ErrorStrings.MissingClosure);
                else
                    Controller.Log.LogEntry(listing.Last(), ErrorStrings.ClosureDoesNotCloseBlock, closeBlock);
                return lines;
            }
            int block_count = 0;
            int ix_after_closed;
            if (allopens.Count == 0)
                return lines;
            List<SourceLine> block;
            if (allopens.First() > 0)
            {
                beforeblock.AddRange(lines.GetRange(0, allopens.First()));
            }

            if (allopens.Count == 1)
            {
                // the simplest scenario just one block   
                block = lines.GetRange(allopens.First(), allclosed.Last() - allopens.First() + 1);
                block_count = block.Count;
                processor(block);
                ix_after_closed = allclosed.Last() + 1;
            }
            else
            {
                // multiple blocks
                int x = 1, y = 0;
                while (x < allopens.Count)
                {
                    if (allopens[x] < allclosed[y])
                    {
                        // nested block
                        y++;
                        x++;
                    }
                    else
                    {
                        break;
                    }
                }
                ix_after_closed = allclosed[y] + 1;
                int count = ix_after_closed - allopens.First();

                // get block
                block = lines.GetRange(allopens.First(), count);
                block_count = block.Count;
                processor(block);
            }
            if (ix_after_closed < lines.Count)
            {
                afterblock.AddRange(lines.GetRange(ix_after_closed, lines.Count - ix_after_closed));
            }
            var processedLines = new List<SourceLine>();
            processedLines.AddRange(beforeblock);
            if (includeInSource)
            {
                processedLines.AddRange(block);
            }
            processedLines.AddRange(afterblock);
            return ProcessBlocks(listing, processedLines, openBlock, closeBlock, processor, includeInSource);
        }

        /// <summary>
        /// Check if all quotes are properly closed.
        /// </summary>
        /// <param name="sourcelines">The list of sourceLines.</param>
        private void CheckQuotes(IEnumerable<SourceLine> sourcelines)
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
        /// <param name="sourcelines">The SourceLine list</param>
        /// <returns></returns>
        public IEnumerable<SourceLine> Preprocess(IEnumerable<SourceLine> sourcelines)
        {
            // we can't do this check until all commenting has been processed
            CheckQuotes(ProcessBlocks(sourcelines, sourcelines.ToList(), ".comment", ".endcomment", ProcessCommentBlocks, true));
            return ProcessIncludes(sourcelines);
        }

        /// <summary>
        /// Process Includes as source.
        /// </summary>
        /// <param name="listing">The source listing containing potential ".include" directives.</param>
        /// <returns>Returns the new source with the included sources expanded.</returns>
        private IEnumerable<SourceLine> ProcessIncludes(IEnumerable<SourceLine> listing)
        {
            var processedLines = new List<SourceLine>();
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
                    if (FileRegistry.Add(filename.Trim('"')) == false)
                    {
                        throw new Exception(string.Format(ErrorStrings.FilePreviouslyIncluded, line.Operand));
                    }
                    var inclistings = ConvertToSource(filename.Trim('"'));
                    processedLines.AddRange(inclistings);
                }
                else if (line.Instruction.Equals(".binclude", Controller.Options.StringComparison))
                {
                    var args = line.CommaSeparateOperand();
                    SourceLine openblock = new SourceLine();
                    if (args.Count > 1)
                    {
                        string label = line.Label;
                        if (string.IsNullOrEmpty(line.Label) == false && SymbolNameFunc(line.Label) == false)
                        {
                            Controller.Log.LogEntry(line, ErrorStrings.LabelNotValid, label);
                            continue;
                        }
                        if (line.Operand.EnclosedInQuotes() == false)
                        {
                            Controller.Log.LogEntry(line, ErrorStrings.None);
                            continue;
                        }
                        if (FileRegistry.Add(line.Operand.Trim('"')) == false)
                        {
                            throw new Exception(string.Format(ErrorStrings.FilePreviouslyIncluded, line.Operand));
                        }

                        openblock.Label = label;
                    }
                    else if (line.Operand.EnclosedInQuotes() == false)
                    {
                        Controller.Log.LogEntry(line, ErrorStrings.FilenameNotSpecified);
                        continue;
                    }

                    openblock.Instruction = AssemblyController.OPEN_SCOPE;
                    processedLines.Add(openblock);
                    var inclistings = ConvertToSource(line.Operand.Trim('"'));

                    processedLines.AddRange(inclistings);
                    processedLines.Add(new SourceLine());
                    processedLines.Last().Instruction = AssemblyController.CLOSE_SCOPE;
                }
                else
                {
                    processedLines.Add(line);
                }
            }
            return processedLines;
        }

        /// <summary>
        /// Marks all SourceLines within comment blocks as comments.
        /// </summary>
        /// <param name="source">The source listing.</param>
        private void ProcessCommentBlocks(IEnumerable<SourceLine> source)
        {
            foreach (var line in source)
            {

                line.Label = line.Instruction = string.Empty;
                line.Operand = string.Empty;
                line.IsComment = true;
            }
        }

        /// <summary>
        /// Converts a file to a SourceLine list.
        /// </summary>
        /// <param name="file">The filename.</param>
        /// <returns>A SourceLine list.</returns>
        public IEnumerable<SourceLine> ConvertToSource(string file)
        {
            if (File.Exists(file))
            {
                Console.WriteLine("Processing input file " + file + "...");
                int currentline = 1;
                List<SourceLine> sourcelines = new List<SourceLine>();
                using (StreamReader reader = new StreamReader(File.Open(file, FileMode.Open)))
                {
                    while (reader.EndOfStream == false)
                    {
                        string unprocessedline = reader.ReadLine();
                        try
                        {
                            var line = new SourceLine(file, currentline, unprocessedline);
                            line.Parse(
                                delegate(string token)
                                {
                                    return Controller.IsInstruction(token) || Reserved.IsReserved(token) ||
                                        Regex.IsMatch(token, @"^\.[\p{Ll}\p{Lu}\p{Lt}][\p{Ll}\p{Lu}\p{Lt}0-9]*$") ||
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
            else
            {
                throw new FileNotFoundException(string.Format("Unable to open source file \"{0}\"", file));
            }
        }

        protected override bool IsReserved(string token)
        {
            return Reserved.IsReserved(token);
        }

        #endregion

        #region Properties

        /// <summary>
        /// The delegate function that will process the identified block of source.
        /// </summary>
        /// <param name="block">The source block to process.</param>
        delegate void ProcessBlock(IEnumerable<SourceLine> block);

        /// <summary>
        /// Gets or sets the defined segments. 
        /// </summary>
        private Dictionary<string, List<SourceLine>> Segments { get; set; }

        /// <summary>
        /// Gets or sets the defined macros.
        /// </summary>
        private Dictionary<string, Macro> Macros { get; set; }

        /// <summary>
        /// Gets or sets the scope stack.
        /// </summary>
        private Stack<string> Scope { get; set; }

        /// <summary>
        /// Gets or sets the file registry of input files.
        /// </summary>
        public HashSet<string> FileRegistry { get; set; }

        /// <summary>
        /// Gets or sets the helper function that will determine if a given token
        /// being processed is a label/symbol.
        /// </summary>
        private Func<string, bool> SymbolNameFunc { get; set; }

        #endregion
    }
}