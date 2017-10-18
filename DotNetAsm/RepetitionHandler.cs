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
using System.Collections.ObjectModel;

namespace DotNetAsm
{
    /// <summary>
    /// Handles repetitions in assembly source.
    /// </summary>
    public class RepetitionHandler : AssemblerBase, IBlockHandler
    {
        #region Private Classes

        /// <summary>
        /// A block of repetitions implemented as a linked list.
        /// </summary>
        private class RepetitionBlock
        {
            /// <summary>
            /// An entry in a DotNetAsm.RepetitionBlock.
            /// </summary>
            public class RepetitionEntry
            {
                /// <summary>
                /// The DotNetAsm.SourceLine in the block.
                /// </summary>
                public SourceLine Line { get; set; }

                /// <summary>
                /// The DotNetAsm.RepetitionHandler.RepetitionBlock to link to.
                /// </summary>
                public RepetitionBlock LinkedBlock { get; set; }

                /// <summary>
                /// Constructs a DotNetAsm.RepetitionHandler.RepetitionBlock.RepetitionEntry.
                /// </summary>
                /// <param name="line">The DotNetAsm.SourceLine to add. This value can 
                /// be null.</param>
                /// <param name="block">The DotNetAsm.RepetitionHandler.RepetitionBlock
                /// to link to. This value can be null.</param>
                public RepetitionEntry(SourceLine line, RepetitionBlock block)
                {
                    if (line != null)
                        Line = line.Clone() as SourceLine;
                    else
                        Line = null;
                    LinkedBlock = block;
                }
            }

            /// <summary>
            /// Constructs a DotNetAsm.RepetitionHandler.RepetitionBlock.
            /// </summary>
            public RepetitionBlock()
            {
                Entries = new List<RepetitionEntry>();
                BackLink = null;
                RepeatAmounts = 0;
            }

            /// <summary>
            /// The System.List&lt;DotNetAsm.RepetitionHandler.RepetitionBlock.RepetitionEntry&lt;
            /// </summary>
            public List<RepetitionEntry> Entries { get; set; }

            /// <summary>
            /// A back link to a DotNetAsm.RepetitioBHandler.RepetitionBlock
            /// </summary>
            public RepetitionBlock BackLink { get; set; }

            /// <summary>
            /// The amount of times the block should be repeated in final assembly
            /// </summary>
            public int RepeatAmounts { get; set; }
        }

        #endregion

        #region Members

        private RepetitionBlock _rootBlock;
        private RepetitionBlock _currBlock;

        private List<SourceLine> _processedLines;

        private int _levels;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a DotNetAsm.RepetitionHandler.
        /// </summary>
        /// <param name="controller">The DotNetAsm.IAssemblyController for this
        /// handler.</param>
        public RepetitionHandler(IAssemblyController controller) :
            base(controller)
        {
            _currBlock =
            _rootBlock = new RepetitionBlock();
            _levels = 0;
            _processedLines = new List<SourceLine>();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Processes the DotNetAsm.SourceLine for repetitions, or within a repetition block.
        /// </summary>
        /// <param name="line">The DotNetAsm.SourceLine to process</param>
        public void Process(SourceLine line)
        {
            if (line.Instruction.Equals(".repeat", Controller.Options.StringComparison))
            {
                if (string.IsNullOrEmpty(line.Operand))
                {
                    Controller.Log.LogEntry(line, ErrorStrings.TooFewArguments, line.Instruction);
                    return;
                }
                else if (string.IsNullOrEmpty(line.Label) == false)
                {
                    Controller.Log.LogEntry(line, ErrorStrings.None);
                    return;
                }

                if (_levels > 0)
                {
                    RepetitionBlock block = new RepetitionBlock();
                    block.BackLink = _currBlock;
                    RepetitionBlock.RepetitionEntry entry =
                        new RepetitionBlock.RepetitionEntry(null, block);
                    _currBlock.Entries.Add(entry);
                    _currBlock = block;
                }
                _currBlock.RepeatAmounts = (int)Controller.Evaluator.Eval(line.Operand);
                _levels++;

            }
            else if (line.Instruction.Equals(".endrepeat", Controller.Options.StringComparison))
            {
                if (_levels == 0)
                {
                    Controller.Log.LogEntry(line, ErrorStrings.ClosureDoesNotCloseBlock, line.Instruction);
                    return;
                }
                else if (string.IsNullOrEmpty(line.Operand) == false)
                {
                    Controller.Log.LogEntry(line, ErrorStrings.TooManyArguments, line.Instruction);
                    return;
                }
                else if (string.IsNullOrEmpty(line.Label) == false)
                {
                    Controller.Log.LogEntry(line, ErrorStrings.None);
                    return;
                }

                _levels--;
                _currBlock = _currBlock.BackLink;
                if (_levels == 0)
                {
                    ProcessLines(_rootBlock, _rootBlock.RepeatAmounts);
                }
            }
            else
            {
                RepetitionBlock.RepetitionEntry entry =
                    new RepetitionBlock.RepetitionEntry(line, null);
                _currBlock.Entries.Add(entry);
            }
        }

        /// <summary>
        /// Perform final processing on the DotNetAsm.RepetitionHandler.ProcessedLines.
        /// </summary>
        /// <param name="block">The DotNetAsm.RepetitionHandler.RepetitionBlock to process</param>
        /// <param name="repeat">The number of times the block needs repeating</param>
        private void ProcessLines(RepetitionBlock block, int repeat)
        {
            for (int i = 0; i < repeat; i++)
            {
                foreach (var entry in block.Entries)
                {
                    if (entry.LinkedBlock != null)
                    {
                        ProcessLines(entry.LinkedBlock, entry.LinkedBlock.RepeatAmounts);
                    }
                    else
                    {
                        _processedLines.Add(entry.Line.Clone() as SourceLine);
                    }
                }
            }
        }

        /// <summary>
        /// Reset the DotNetAsm.RepetitionHandler. All processed lines will be cleared.
        /// </summary>
        public void Reset()
        {
            _processedLines.Clear();
            _rootBlock.Entries.Clear();
            _rootBlock.RepeatAmounts = 0;
            _currBlock = _rootBlock;
        }

        /// <summary>
        /// Determines whether the DotNetAsm.RepetitionHandler processes the given token.
        /// </summary>
        /// <param name="token">The token to determine if is an instruction that
        /// the handler processes.</param>
        /// <returns>True, if the DotNetAsm.RepetitionHandler processes this token</returns>
        public bool Processes(string token)
        {
            return IsReserved(token);
        }

        /// <summary>
        /// Determines if the token is a reserved word.
        /// </summary>
        /// <param name="token">The token to determine</param>
        /// <returns>True, if the token is reserved for the DotNetAsm.RepetitionHandler</returns>
        protected override bool IsReserved(string token)
        {
            return token.Equals(".repeat", Controller.Options.StringComparison) ||
                   token.Equals(".endrepeat", Controller.Options.StringComparison);
        }

        /// <summary>
        /// Gets the flag that determines if the DotNetAsm.RepetitionHandler is currently in
        /// processing mode.
        /// </summary>
        public bool IsProcessing()
        {
            return _levels > 0;  
        }

        /// <summary>
        /// Gets the read-only processed blocks of repeated lines.
        /// </summary>
        public IEnumerable<SourceLine> GetProcessedLines()
        {
            return _processedLines;
        }

        #endregion
    }
}