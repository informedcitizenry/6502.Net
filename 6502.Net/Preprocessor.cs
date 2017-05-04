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

namespace Asm6502.Net
{
    /// <summary>
    /// A preprocessor class for the 6502.Net assembler.
    /// </summary>
    public class Preprocessor : AssemblerBase
    {
        #region Constants

        /// <summary>
        /// The default token indicating a scope block opening. This field is constant.
        /// </summary>
        private const string OPEN_SCOPE = ".block";

        /// <summary>
        /// The default token indicating a scope block closure. This field is constant.
        /// </summary>
        private const string CLOSE_SCOPE = ".endblock";

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a Preprocessor object.
        /// </summary>
        /// <param name="controller">The assembly controller.</param>
        /// <param name="checkReserved">A function to check for reserved keywords such as 
        /// mnemonics or pseudo-ops.</param>
        /// <param name="checkSymbol">A function to check for symbols such as labels.</param>
        public Preprocessor(IAssemblyController controller,
                            Func<string, bool> checkReserved, 
                            Func<string, bool> checkSymbol)
            : base(controller)
        {
            FileRegistry = new HashSet<string>();
            ReservedFunc = checkReserved;
            SymbolNameFunc = checkSymbol;
            Macros = new Dictionary<string, Macro>();
            Scope = new Stack<string>();
            
            Reserved.DefineType("Directives", new string[]
                {
                    ".binclude", ".include",
                    OPEN_SCOPE, CLOSE_SCOPE,
                    ".comment",  ".endcomment",
                    ".macro",    ".endmacro",
                    ".segment",  ".endsegment" 
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
        private List<SourceLine> ProcessBlocks(IEnumerable<SourceLine> listing,
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
                    Controller.Log.LogEntry(listing.Last(), Resources.ErrorStrings.MissingClosure);
                else
                    Controller.Log.LogEntry(listing.Last(), Resources.ErrorStrings.ClosureDoesNotCloseBlock, closeBlock);
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
                            Controller.Log.LogEntry(line, Resources.ErrorStrings.QuoteStringNotEnclosed);
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
                    Controller.Log.LogEntry(line, Resources.ErrorStrings.QuoteStringNotEnclosed);
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
            sourcelines = ProcessBlocks(sourcelines, sourcelines.ToList(), ".comment", ".endcomment", ProcessCommentBlocks, true);

            // we can't do this check until all commenting has been processed
            CheckQuotes(sourcelines);

            sourcelines = ProcessBlocks(sourcelines, sourcelines.ToList(), ".segment", ".endsegment", DefineSegments, false);
            sourcelines = ProcessBlocks(sourcelines, sourcelines.ToList(), ".macro", ".endmacro", DefineMacro, false);

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
                        Controller.Log.LogEntry(line, Resources.ErrorStrings.FilenameNotSpecified);
                        continue;
                    }
                    if (FileRegistry.Add(filename.Trim('"')) == false)
                    {
                        throw new Exception("File '" + line.Operand + "' previously included. Possible circular reference?");
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
                            Controller.Log.LogEntry(line, Resources.ErrorStrings.LabelNotValid, label);
                            continue;
                        }
                        if (line.Operand.EnclosedInQuotes() == false)
                        {
                            Controller.Log.LogEntry(line, Resources.ErrorStrings.None);
                            continue;
                        }
                        if (FileRegistry.Add(line.Operand.Trim('"')) == false)
                        {
                            throw new Exception("File " + line.Operand + " previously included. Possible circular reference?");
                        }

                        openblock.Label = label;
                    }
                    else if (line.Operand.EnclosedInQuotes() == false)
                    {
                        Controller.Log.LogEntry(line, Resources.ErrorStrings.FilenameNotSpecified);
                        continue;
                    }

                    openblock.Instruction = ".block";
                    processedLines.Add(openblock);
                    var inclistings = ConvertToSource(line.Operand.Trim('"'));

                    processedLines.AddRange(inclistings);
                    processedLines.Add(new SourceLine());
                    processedLines.Last().Instruction = ".endblock";
                }
                else
                {
                    processedLines.Add(line);
                }
            }
            return processedLines;
        }

        /// <summary>
        /// Define macros from the source list.
        /// </summary>
        /// <param name="source">The SourceLines.</param>
        private void DefineMacro(IEnumerable<SourceLine> source)
        {
            var def = source.First();
            if (def.IsComment == false)
            {
                if (def.Label.StartsWith("_") || SymbolNameFunc(def.Label) == false ||
                ReservedFunc(def.Label) || Reserved.IsReserved(def.Label))
                {
                    Controller.Log.LogEntry(def, Resources.ErrorStrings.LabelNotValid, def.Label);
                    return;
                }
                if (Macros.ContainsKey("." + def.Label))
                {
                    Controller.Log.LogEntry(def, Resources.ErrorStrings.MacroRedefinition, def.Label);
                    return;
                }
                try
                {
                    Macros.Add("." + def.Label, Macro.Create(source, 
                                                             Controller.Options.StringComparison, 
                                                             OPEN_SCOPE, 
                                                             CLOSE_SCOPE, 
                                                             false));
                }
                catch (Macro.MacroException ex)
                {
                    Controller.Log.LogEntry(ex.Line, ex.Message);
                    return;
                }
                def.IsDefinition = true;
            }
            def = source.Last();
            def.IsDefinition = true;
        }

        /// <summary>
        /// Define segments from the SourceLines.
        /// </summary>
        /// <param name="source">The source listing.</param>
        private void DefineSegments(IEnumerable<SourceLine> source)
        {
            var nocomments = source.Where(l => !l.IsComment);
            Stack<string> root = new Stack<string>();
            foreach (var line in nocomments)
            {
                if (line.Instruction.Equals(".segment", Controller.Options.StringComparison))
                {
                    if (line.Operand.StartsWith("_") || SymbolNameFunc(line.Operand) == false)
                    {
                        Controller.Log.LogEntry(line, Resources.ErrorStrings.TooFewArguments, line.Operand);
                        return;
                    }
                    if (string.IsNullOrEmpty(line.Label) == false)
                    {
                        Controller.Log.LogEntry(line, Resources.ErrorStrings.None);
                        return;
                    }
                    if (Macros.ContainsKey(line.Operand))
                    {
                        Controller.Log.LogEntry(line, "Re-definition of macro or segment '" + line.Operand + "'");
                        return;
                    }
                    line.IsDefinition = true;
                    string segmentname = "." + line.Operand;
                    Macros.Add(segmentname, new Macro());
                    root.Push(segmentname);
                    Macros[root.Peek()].IsSegment = true;
                }
                else if (line.Instruction.Equals(".endsegment", Controller.Options.StringComparison))
                {
                    if (line.Operand.Equals(root.Peek().TrimStart('.')))
                    {
                        line.IsDefinition = true;
                        root.Pop();
                    }
                    else
                    {
                        Controller.Log.LogEntry(line, "Segment closure does not close current segment");
                    }

                }
                else if (root.Peek().Equals(line.Instruction, Controller.Options.StringComparison))
                {
                    Controller.Log.LogEntry(line, Resources.ErrorStrings.RecursiveMacro);
                    continue;
                }
                else
                {
                    Macros[root.Peek()].Source.Add(line);
                }
            }
        }

        /// <summary>
        /// Checks that all blocks are closed properly.
        /// </summary>
        /// <param name="lines">The source lines.</param>
        private void CheckBlocks(IEnumerable<SourceLine> lines)
        {
            this.Scope.Clear();
            Stack<string> condscope = new Stack<string>();
            foreach (var line in lines)
            {
                var b = line.Instruction.ToLower();
                switch (b)
                {
                    case ".block":
                        Scope.Push(".endblock");
                        break;
                    case ".endblock":
                        if (Scope.Count == 0 || Scope.Peek().ToLower() != b)
                        {
                            Controller.Log.LogEntry(line, Resources.ErrorStrings.ClosureDoesNotCloseBlock, line.Instruction);
                        }
                        Scope.Pop();
                        break;
                    default:
                        break;
                }
            }
            if (condscope.Count != 0 || Scope.Count != 0)
                Controller.Log.LogEntry(lines.Last(), "End of file reached without closure closing block '" + Scope.Peek() + "'");
        }
        
        /// <summary>
        /// Define scopes and labels from the source listing.
        /// </summary>
        /// <param name="controller">The assembly controller.</param>
        /// <param name="lines">The SourceLine listing.</param>
        public void DefineScopedSymbols(List<SourceLine> lines)
        {
            var assembled = lines.Where(l => !l.DoNotAssemble);
            CheckBlocks(assembled);

            int anon = 0;
            foreach(var line in assembled)
            {
                if (Controller.TerminateAssembly(line))
                    break;

                if (string.IsNullOrEmpty(line.Label) && string.IsNullOrEmpty(line.Instruction))
                {
                    line.Scope = string.Join(".", Scope.Reverse());
                    continue;
                }

                if (line.Instruction.Equals(".block", Controller.Options.StringComparison))
                {
                    if (string.IsNullOrEmpty(line.Operand) == false)
                        Controller.Log.LogEntry(line, Resources.ErrorStrings.DirectiveTakesNoArguments, line.Instruction);

                    if (Scope.Count > 0 && !string.IsNullOrEmpty(line.Label) && Scope.Peek().EndsWith("@"))
                        Scope.Pop();

                    string label = line.Label;
                    if (string.IsNullOrEmpty(label) == false)
                    {
                        if (label.StartsWith("_") || SymbolNameFunc(label) == false)
                        {
                            Controller.Log.LogEntry(line, Resources.ErrorStrings.None);
                            continue;
                        }
                        Scope.Push(label);
                    }
                    else
                    {
                        var anonscope = "!anon" + anon.ToString();
                        Scope.Push(anonscope);
                        anon++;
                    }
                    //line.IsDefinition = true;
                }
                else if (line.Instruction.Equals(".endblock", Controller.Options.StringComparison))
                {
                    if (string.IsNullOrEmpty(line.Operand) == false)
                        Controller.Log.LogEntry(line, Resources.ErrorStrings.DirectiveTakesNoArguments, line.Instruction);

                    //line.IsDefinition = true;

                    if (string.IsNullOrEmpty(line.Label))
                    {
                        while (Scope.Peek().EndsWith("@"))
                            Scope.Pop();

                        if (Scope.Count == 0)
                        {
                            Controller.Log.LogEntry(line, Resources.ErrorStrings.ClosureDoesNotCloseBlock, line.Instruction);
                            continue;
                        }
                        Scope.Pop();
                    }
                    else
                    {
                        if (SymbolNameFunc(line.Label))
                        {
                            Scope.Push(line.Label);

                        }
                        else if (line.Label != "+" && line.Label != "-")
                        {
                            Controller.Log.LogEntry(line, Resources.ErrorStrings.LabelNotValid, line.Label);
                            continue;
                        }
                    }
                }
                else
                {
                    if (!line.Label.StartsWith("_") && SymbolNameFunc(line.Label))
                    {
                        if (Scope.Count > 0 && Scope.Peek().EndsWith("@"))
                            Scope.Pop();
                        Scope.Push(line.Label + "@");
                    }
                }

                line.Scope = string.Join(".", Scope.Reverse());//.Replace("@", string.Empty);

                if (line.Instruction.Equals(".endblock", Controller.Options.StringComparison) &&
                    string.IsNullOrEmpty(line.Label) == false)
                {
                    while (Scope.Peek().EndsWith("@"))
                        Scope.Pop();

                    if (Scope.Count == 0)
                    {
                        Controller.Log.LogEntry(line, Resources.ErrorStrings.ClosureDoesNotCloseBlock, line.Instruction);
                        continue;
                    }
                    Scope.Pop();
                }

                if (SymbolNameFunc(line.Label))
                {
                    string scoped = line.Scope.Replace("@", "");
                    if (line.Label.StartsWith("_"))
                        scoped += "." + line.Label;
                    if (/*Controller.Labels.ContainsKey(scoped)*/Controller.Labels.ContainsKey(scoped))
                    {
                        Controller.Log.LogEntry(line, Resources.ErrorStrings.LabelRedefinition, line.Label);
                        continue;
                    }
                    else
                    {
                        Controller.Labels.Add(scoped, "0");
                    }
                }
            }

            if (Scope.Count > 0 && Scope.Peek().EndsWith("@") == false)
                throw new Exception("End of file reached without block closure");
        }

        /// <summary>
        /// Expand all macro invocations in the source listing.
        /// </summary>
        /// <param name="listing">The SourceLine listing.</param>
        /// <returns>The modified source with macros expanded.</returns>
        public List<SourceLine> ExpandMacros(IEnumerable<SourceLine> listing)
        {
            List<SourceLine> processed = new List<SourceLine>();
            foreach (var line in listing)
            {
                var instruction = line.Instruction;
                var matches = Macros.Keys.Where(k => k.Equals(instruction, Controller.Options.StringComparison));

                processed.Add(line);
                if (matches.Count() == 0 || line.DoNotAssemble)
                {
                    continue;
                }
                line.IsDefinition = true;
                var macname = matches.First();
                var macro = Macros[macname];

                if (macro.IsSegment == true)
                {
                    processed.AddRange(macro.Source);
                    processed = ExpandMacros(processed);
                }
                else
                {
                    var expanded = macro.Expand(line);
                    processed.AddRange(expanded);
                }
            }
            return processed;
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
                                    return ReservedFunc(token) || Reserved.IsReserved(token) ||
                                        Regex.IsMatch(token, @"\.[a-zA-Z][a-zA-Z0-9]*") ||
                                        token == "=";
                                },
                                delegate(string token)
                                {
                                    return SymbolNameFunc(token);
                                });

                            // check to see if there are instructions like *= something
                            if (!string.IsNullOrEmpty(line.Label) && string.IsNullOrEmpty(line.Instruction) 
                                &&!string.IsNullOrWhiteSpace(line.Operand))
                            {
                                var m = Regex.Match(line.Label, @"(\s*)([a-zA-Z0-9_]+|\+|-|\*)(=)");
                                if (!string.IsNullOrEmpty(m.Value))
                                {
                                    line.Label = m.Groups[2].Value;
                                    line.Instruction = m.Groups[3].Value;
                                }
                            }
                            else if (string.IsNullOrEmpty(line.Label) && string.IsNullOrEmpty(line.Instruction) 
                                &&!string.IsNullOrWhiteSpace(line.Operand))
                            {
                                /// check to see if there are instructions like *=something
                                var m = Regex.Match(line.Operand, @"(\s*)([a-zA-Z0-9_]+|\+|-|\*)(=)(.+)");
                                if (!string.IsNullOrEmpty(m.Value))
                                {
                                    line.Label = m.Groups[2].Value;
                                    line.Instruction = m.Groups[3].Value;
                                    line.Operand = m.Groups[4].Value.Trim();
                                }
                            }

                            sourcelines.Add(line);
                        }
                        catch (SourceLine.QuoteNotEnclosedException)
                        {
                            Controller.Log.LogEntry(file, currentline, Resources.ErrorStrings.QuoteStringNotEnclosed);
                        }
                        currentline++;
                    }
                    sourcelines = Preprocess(sourcelines).ToList();
                }
                return sourcelines;
            }
            else
            {
                throw new FileNotFoundException("Unable to open source file", file);
            }
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

        /// <summary>
        /// Gets or sets the helper function that will determine if a given token
        /// being processed is a reserved word, such as an instruction or 
        /// assembler directive.
        /// </summary>
        private Func<string, bool> ReservedFunc { get; set; }

        #endregion      
    }
}
