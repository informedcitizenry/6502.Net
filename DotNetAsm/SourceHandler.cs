//-----------------------------------------------------------------------------
// Copyright (c) 2017-2019 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;

namespace DotNetAsm
{
    /// <summary>
    /// Handles all source file inclusions, and parses comment blocks.
    /// </summary>
    public class SourceHandler : AssemblerBase, IBlockHandler
    {
        #region members

        private readonly List<SourceLine> _includedLines;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="T:DotNetAsm.SourceHandler"/> class.
        /// </summary>
        public SourceHandler()
        {
            Reserved.DefineType("Directives", ".comment", ".endcomment", ".include", ".binclude");
            _includedLines = new List<SourceLine>();
            FileRegistry = new HashSet<string>();
        }

        #endregion

        #region IBlockHandler

        private void ProcessCommentBlocks(List<SourceLine> source)
        {
            const int ENDCOMMENT_SIZE = 11;
            var inComment = false;
            for (var i = 0; i < source.Count; i++)
            {
                SourceLine line = source[i];
                line.IsComment = inComment;
                if (inComment)
                {
                    var endBlockIx = line.SourceString.IndexOf(".endcomment", Assembler.Options.StringComparison);
                    if (endBlockIx > -1)
                    {
                        var afterIx = endBlockIx + ENDCOMMENT_SIZE;
                        line.IsComment = inComment = false;
                        if (line.SourceString.Length > afterIx)
                        {
                            if (char.IsWhiteSpace(line.SourceString[afterIx]))
                            {
                                source.Insert(i + 1, new SourceLine
                                {
                                    Filename = line.Filename,
                                    LineNumber = line.LineNumber,
                                    SourceString = line.SourceString.Substring(afterIx)
                                });
                            }
                            else
                            {
                                Assembler.Log.LogEntry(line, ErrorStrings.None);
                                break;
                            }
                        }
                    }
                }
                else
                {
                    var commBlockIx = line.SourceString.IndexOf(".comment", Assembler.Options.StringComparison);
                    if (commBlockIx > -1)
                    {
                        line.IsComment = inComment = true;
                        if (commBlockIx > 0)
                        {
                            if (char.IsWhiteSpace(line.SourceString[commBlockIx - 1]))
                            {
                                source.Insert(i, new SourceLine
                                {
                                    Filename = line.Filename,
                                    LineNumber = line.LineNumber,
                                    SourceString = line.SourceString.Substring(0, commBlockIx)
                                });
                            }
                            else
                            {
                                Assembler.Log.LogEntry(line, ErrorStrings.None);
                                break;
                            }
                        }
                    }
                }
            }
            if (inComment)
                throw new Exception(ErrorStrings.MissingClosure);
        }

        public bool Processes(string token) => IsReserved(token);

        public void Process(SourceLine line)
        {
            var openblock = new SourceLine();
            if (line.Instruction.Equals(".binclude", Assembler.Options.StringComparison))
            {
                List<string> args = line.Operand.CommaSeparate();

                if (args.Count > 1)
                {
                    if (string.IsNullOrEmpty(line.Label) == false)
                    {
                        Assembler.Log.LogEntry(line, ErrorStrings.LabelNotValid, line.Label);
                        Reset();
                        return;
                    }
                    else
                    {
                        openblock.Label = line.Label;
                    }
                }
                else if (line.Operand.EnclosedInQuotes() == false)
                {
                    Assembler.Log.LogEntry(line, ErrorStrings.FilenameNotSpecified);
                }
                openblock.Instruction = ConstStrings.OPEN_SCOPE;
                _includedLines.Add(openblock);
            }
            if (line.Operand.EnclosedInQuotes() == false)
            {
                Assembler.Log.LogEntry(line, ErrorStrings.None);
            }
            else
            {
                var fileName = line.Operand.Trim('"');
                if (File.Exists(fileName))
                {
                    FileRegistry.Add(fileName);
                    Console.WriteLine("Processing input file " + fileName + "...");
                    var currentline = 1;
                    var sourcelines = new List<SourceLine>();
                    using (var reader = new StreamReader(File.Open(fileName, FileMode.Open)))
                    {
                        while (reader.EndOfStream == false)
                        {
                            var source = reader.ReadLine();
                            _includedLines.Add(new SourceLine(fileName, currentline, source));
                            currentline++;
                        }
                    }
                }
                else
                {
                    throw new FileNotFoundException(string.Format("Unable to open source file '{0}'", fileName));
                }
                if (openblock.Instruction.Equals(ConstStrings.OPEN_SCOPE))
                {
                    var closeBlock = new SourceLine
                    {
                        Instruction = ConstStrings.CLOSE_SCOPE
                    };
                    _includedLines.Add(closeBlock);
                }
            }
            ProcessCommentBlocks(_includedLines);
        }

        public void Reset() => _includedLines.Clear();

        public bool IsProcessing() => false;

        public IEnumerable<SourceLine> GetProcessedLines() => _includedLines;

        #endregion

        #region

        /// <summary>
        /// Gets or sets the file registry of input files.
        /// </summary>
        public HashSet<string> FileRegistry { get; set; }

        #endregion

    }
}
