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
using System.Linq;

namespace DotNetAsm
{
    public sealed class ForerNexterHandlerer : AssemblerBase, IBlockHandler
    {
        #region Classes 

        /// <summary>
        /// A block of for next loops implemented as a linked list.
        /// </summary>
        class ForNextBlock
        {
            #region Classes

            /// <summary>
            /// An entry in a DotNetAsm.ForNextBlock
            /// </summary>
            public class ForNextEntry
            {
                /// <summary>
                /// The <see cref="T:DotNetAsm.SourceLine"/> in the block.
                /// </summary>
                public SourceLine Line { get; set; }

                /// <summary>
                /// The <see cref="T:DotNetAsm.RepetitionHandler.RepetitionBlock"/> to link to.
                /// </summary>
                public ForNextBlock LinkedBlock { get; set; }

                /// <summary>
                /// Constructs a <see cref="T:DotNetAsm.RepetitionHandler.RepetitionBlock.RepetitionEntry"/>.
                /// </summary>
                /// <param name="line">The <see cref="T:DotNetAsm.SourceLine"/> to add. This value can 
                /// be null.</param>
                /// <param name="block">The <see cref="T:DotNetAsm.RepetitionHandler.RepetitionBlock"/>
                /// to link to. This value can be null.</param>
                public ForNextEntry(SourceLine line, ForNextBlock block)
                {
                    if (line != null)
                        Line = line.Clone() as SourceLine;
                    else
                        Line = null;
                    LinkedBlock = block;
                }
            }

            #endregion

            #region Members

            LinkedList<ForNextEntry> _entries;
            LinkedListNode<ForNextEntry> _currentEntry;

            #endregion

            #region Constructors

            /// <summary>
            /// Constructs a new instance of a <see cref="T:DotNetAsm.ForNextHandler.ForNextBlock"/>.
            /// </summary>
            public ForNextBlock()
            {
                Parent = null;
                _entries = new LinkedList<ForNextEntry>();
                IterExpressions = new List<string>();
                InitExpression =
                Condition = string.Empty;
            }

            #endregion

            #region Methods

            /// <summary>
            /// Add an entry to the block.
            /// </summary>
            /// <param name="line">The <see cref="T:DotNetAsm.SourceLine"/> of the entry</param>
            /// <param name="block">The <see cref="T:DotNetAsm.ForNextHandler.ForNextBlock"/> of the entry</param>
            public void AddEntry(SourceLine line, ForNextBlock block)
            {
                if (block != null)
                    block.Parent = this;
                _entries.AddLast(new ForNextEntry(line, block));
            }

            /// <summary>
            /// Advance the block one entry
            /// </summary>
            /// <returns>The current <see cref="T:DotNetAsm.ForNextHandler.ForNextBlock.ForNextEntry"/>.</returns>
            public ForNextEntry Advance()
            {
                if (_currentEntry == null)
                    _currentEntry = _entries.First;
                else
                    _currentEntry = _currentEntry.Next;
                return _currentEntry.Value;
            }

            /// <summary>
            /// Restarts the block to the first entry.
            /// </summary>
            /// <returns>The current <see cref="T:DotNetAsm.ForNextHandler.ForNextBlock.ForNextEntry"/>.</returns>
            public ForNextEntry Begin()
            {
                _currentEntry = _entries.First;
                return _currentEntry.Value;
            }

            /// <summary>
            /// Determines if the block is at the beginning (first entry).
            /// </summary>
            /// <returns><c>True</c> if the block is at the beginning, otherwise <c>false</c>.</returns>
            public bool IsBeginning() => _currentEntry == null || _currentEntry == _entries.First;

            /// <summary>
            /// Get the next child <see cref="T:DotNetAsm.ForNextHandler.ForNextBlock"/> from the current entry.
            /// </summary>
            /// <returns>The next child <see cref="T:DotNetAsm.ForNextHandler.ForNextBlock"/>.</returns>
            public ForNextBlock NextChild()
            {
                if (_currentEntry == null)
                    _currentEntry = _entries.First;
                while (_currentEntry.Value.LinkedBlock == null)
                    _currentEntry = _currentEntry.Next;
                return _currentEntry.Value.LinkedBlock;
            }

            /// <summary>
            /// Get a collection of all the <see cref="T:DotNetAsm.SourceLine"/>s in the block's entries.
            /// </summary>
            /// <returns>A <see cref="T:System.Collections.Generic.IEnumerable&lt;SourceLine&gt;"/>.</returns>
            public IEnumerable<SourceLine> GetProcessedLines()
            {
                var processed = new List<SourceLine>();
                foreach (var entry in _entries)
                {
                    if (entry.LinkedBlock != null)
                        processed.AddRange(entry.LinkedBlock.GetProcessedLines());
                    else
                        processed.Add(entry.Line.Clone() as SourceLine);
                }
                return processed;
            }

            /// <summary>
            /// Resets the block's entries, including the entries inside child blocks.
            /// </summary>
            public void ResetEntries()
            {
                foreach (var entry in _entries)
                {
                    if (entry.LinkedBlock != null)
                        entry.LinkedBlock.ResetEntries();
                }
                _entries.Clear();
                _currentEntry = null;
            }

            #endregion

            #region Properties

            /// <summary>
            /// Gets the parent block to a <see cref="T:DotNetAsm.ForNextHandler.ForNextBlock"/>
            /// </summary>
            public ForNextBlock Parent { get; set; }

            /// <summary>
            /// Gets or sets the initialization expression of the block.
            /// </summary>
            public string InitExpression { get; set; }

            /// <summary>
            /// Gets the list of iteration expressions evaluated upon each for loop.
            /// </summary>
            public List<string> IterExpressions { get; set; }

            /// <summary>
            /// Gets the block's loop condition.
            /// </summary>
            public string Condition { get; set; }

            /// <summary>
            /// Gets or sets the scope of the entries in the block.
            /// </summary>
            public string Scope { get; set; }

            #endregion
        }

        #endregion

        #region Members

        ForNextBlock _rootBlock;
        ForNextBlock _currBlock;
        ForNextBlock _breakBlock;

        Stack<ForNextBlock> _blocks;

        readonly List<SourceLine> _processedLines;

        int _levels;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="T:DotNetAsm.ForerNexterHandlerer"/> class.
        /// </summary>
        /// <param name="controller">The <see cref="T:DotNetAsm.IAssemblyController"/> for this
        /// handler.</param>
        public ForerNexterHandlerer(IAssemblyController controller)
            : base(controller)
        {
            Reserved.DefineType("Directives",
                                ".for", ".next", ".break", ".continue");
            _currBlock =
            _rootBlock = new ForNextBlock();
            _breakBlock = null;
            _levels = 0;
            _processedLines = new List<SourceLine>();
            _blocks = new Stack<ForNextBlock>();
        }

        #endregion

        #region Methods

        #region IBlockHandler Methods

        public IEnumerable<SourceLine> GetProcessedLines() => _processedLines;

        public bool IsProcessing() => _levels > 0;

        public void Process(SourceLine line)
        {
            switch(line.Instruction.ToLower())
            {
                case ".for":
                    {
                        var csvs = line.Operand.CommaSeparate();
                        if (csvs.Count == 0)
                        {
                            Controller.Log.LogEntry(line, ErrorStrings.TooFewArguments, line.Instruction);
                            return;
                        }
                        var newblock = new ForNextBlock
                        {
                            InitExpression = csvs.First()
                        };
                        if (csvs.Count > 1)
                            newblock.Condition = csvs[1];
                        if (csvs.Count > 2)
                            newblock.IterExpressions.AddRange(csvs.Skip(2));
                        if (_rootBlock == null)
                        {
                            _currBlock = _rootBlock = newblock;
                        } else 
                        {
                            _currBlock.AddEntry(null, newblock);
                            _currBlock = newblock;
                        }
                        break;
                    }
                case ".next":
                    {
                        if (_currBlock != _rootBlock)
                        {
                            _currBlock = _currBlock.Parent;
                        }
                        else
                        {
                            _processedLines.AddRange(processBlocks());

                        }
                        break;
                    }
                default:
                    _currBlock.AddEntry(line, null);
                    break;

            }
        }

        private IEnumerable<SourceLine> processBlocks()
        {
            throw new NotImplementedException();
        }

        public bool Processes(string token)
        {
            return Reserved.IsOneOf("Directives", token);
        }

        public void Reset()
        {
            _blocks.Clear();
            _breakBlock = null;
            _processedLines.Clear();
            _currBlock = _rootBlock = null;;
            //_rootBlock.ResetEntries();
        }

        #endregion

        #endregion
    }
}
