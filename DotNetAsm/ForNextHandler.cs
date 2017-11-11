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

using System.Collections.Generic;
using System.Linq;

namespace DotNetAsm
{
    public class ForNextHandler : AssemblerBase, IBlockHandler
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
                /// The DotNetAsm.SourceLine in the block.
                /// </summary>
                public SourceLine Line { get; set; }

                /// <summary>
                /// The DotNetAsm.RepetitionHandler.RepetitionBlock to link to.
                /// </summary>
                public ForNextBlock LinkedBlock { get; set; }

                /// <summary>
                /// Constructs a DotNetAsm.RepetitionHandler.RepetitionBlock.RepetitionEntry.
                /// </summary>
                /// <param name="line">The DotNetAsm.SourceLine to add. This value can 
                /// be null.</param>
                /// <param name="block">The DotNetAsm.RepetitionHandler.RepetitionBlock
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
            /// Constructs a new instance of a ForNextBlock
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
            /// <param name="line">The DotNetAsm.SourceLine of the entry</param>
            /// <param name="block">The DotNetAsm.ForNextHandler.ForNextBlock of the entry</param>
            public void AddEntry(SourceLine line, ForNextBlock block)
            {
                if (block != null)
                    block.Parent = this;
                _entries.AddLast(new ForNextEntry(line, block));
            }

            /// <summary>
            /// Advance the block one entry
            /// </summary>
            /// <returns>Returns the current DotNetAsm.ForNextHandler.ForNextBlock.ForNextEntry</returns>
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
            /// <returns>Returns the current DotNetAsm.ForNextHandler.ForNextBlock.ForNextEntry</returns>
            public ForNextEntry Begin()
            {
                _currentEntry = _entries.First;
                return _currentEntry.Value;
            }

            /// <summary>
            /// Determines if the block is at the beginning (first entry).
            /// </summary>
            /// <returns>True, if the block is at the beginning</returns>
            public bool IsBeginning()
            {
                return _currentEntry == null || _currentEntry == _entries.First;
            }

            /// <summary>
            /// Get the next child DotNetAsm.ForNextHandler.ForNextBlock from the current entry.
            /// </summary>
            /// <returns>The next child DotNetAsm.ForNextHandler.ForNextBlock</returns>
            public ForNextBlock NextChild()
            {
                if (_currentEntry == null)
                    _currentEntry = _entries.First;
                while (_currentEntry.Value.LinkedBlock == null)
                    _currentEntry = _currentEntry.Next;
                return _currentEntry.Value.LinkedBlock;
            }

            /// <summary>
            /// Get a collection of all the DotNetAsm.SourceLines in the block's entries.
            /// </summary>
            /// <returns>A System.Collections.Generic.IEnumerable&lt;SourceLine&gt;</returns>
            public IEnumerable<SourceLine> GetProcessedLines()
            {
                List<SourceLine> processed = new List<SourceLine>();
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
                foreach(var entry in _entries)
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
            /// Gets the parent block to a DotNetAsm.ForNextHandler.ForNextBlock
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
            /// The condition to meet to stop the block.
            /// </summary>
            public string Condition { get; set; }

            /// <summary>
            /// Gets or sets the scope of the entries in the block.
            /// </summary>
            public string Scope { get; set; }

            #endregion
        }

        #endregion

        ForNextBlock _rootBlock;
        ForNextBlock _currBlock;
        ForNextBlock _breakBlock;

        readonly List<SourceLine> _processedLines;

        int _levels;

        /// <summary>
        /// Constructs a DotNetAsm.ForNextHandler.
        /// </summary>
        /// <param name="controller">The DotNetAsm.IAssemblyController for this
        /// handler.</param>
        public ForNextHandler(IAssemblyController controller)
            : base(controller)
        {
            Reserved.DefineType("Directives", 
                ".for", ".next", ".break",
                "@@ for @@", "@@ next @@", "@@ break @@"
            );
            _currBlock =
            _rootBlock = new ForNextBlock();
            _breakBlock = null;
            _levels = 0;
            _processedLines = new List<SourceLine>();
        }

        /// <summary>
        /// Perform a full reset on the block.
        /// </summary>
        void FullReset()
        {
            _breakBlock = null;
            _processedLines.Clear();
            _currBlock = _rootBlock;
            _rootBlock.ResetEntries();
        }

        /// <summary>
        /// Determines whether the DotNetAsm.RepetitionHandler processes the given token.
        /// </summary>
        /// <param name="token">The token to determine if is an instruction that
        /// the handler processes.</param>
        /// <returns>True, if the DotNetAsm.ForNextHandler processes this token</returns>
        public bool Processes(string token)
        {
            return Reserved.IsOneOf("Directives", token);
        }

        /// <summary>
        /// Processes the DotNetAsm.SourceLine for repetitions, or within a ForNextBlock.
        /// </summary>
        /// <param name="line">The DotNetAsm.SourceLine to process</param>
        public void Process(SourceLine line)
        {
            string instruction = line.Instruction.ToLower();

            if (instruction.Equals(".for"))
            {
                if (string.IsNullOrEmpty(line.Operand))
                {
                    Controller.Log.LogEntry(line, ErrorStrings.TooFewArguments, line.Instruction);
                    return;
                }
                else if (string.IsNullOrEmpty(line.Label) == false)
                {
                    // capture the label
                    _processedLines.Add(new SourceLine()
                    {
                        Label = line.Label,
                        SourceString = line.Label,
                        LineNumber = line.LineNumber,
                        Filename = line.Filename
                    });
                }      
                // .for <init_expression>, <condition>, <iteration_expression>
                var csvs = line.CommaSeparateOperand();
                if (csvs.Count < 2)
                {
                    Controller.Log.LogEntry(line, ErrorStrings.TooFewArguments, line.Instruction);
                    return;
                }

                if (_levels > 0)
                {
                    ForNextBlock block = new ForNextBlock();
                    _currBlock.AddEntry(null, block);
                    _currBlock = block;
                }
                else
                {
                    _currBlock = _rootBlock;

                }
                _levels++;
                _currBlock.Scope = line.Scope;
                _currBlock.InitExpression = csvs.First();
                _currBlock.Condition = csvs[1];

                if (_currBlock == _rootBlock)
                {
                    if (!string.IsNullOrEmpty(_currBlock.InitExpression))
                    {
                        var iteratorvar = Controller.Variables.SetVariable(_currBlock.InitExpression, _currBlock.Scope);
                        if (string.IsNullOrEmpty(iteratorvar.Key))
                        {
                            Controller.Log.LogEntry(line, ErrorStrings.BadExpression, csvs.First());
                            return;
                        }
                        _processedLines.Add(new SourceLine()
                        {
                            SourceString = ConstStrings.SHADOW_SOURCE,
                            Instruction = ConstStrings.VAR_DIRECTIVE,
                            Operand = string.Format("{0}={1}", iteratorvar.Key, iteratorvar.Value)
                        });
                    }
                }
                _currBlock.AddEntry(new SourceLine()
                {
                    SourceString = ConstStrings.SHADOW_SOURCE,
                    Instruction = "@@ for @@"
                }, null);

                if (csvs.Count >= 3)
                {
                    _currBlock.IterExpressions.Clear();
                    _currBlock.IterExpressions.AddRange(csvs.GetRange(2, csvs.Count - 2));
                }   
     
            }
            else if (instruction.Equals(".next"))
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
                SourceLine loopLine = new SourceLine()
                {
                    SourceString = ConstStrings.SHADOW_SOURCE,
                    Instruction = "@@ next @@"
                };
                _currBlock.AddEntry(loopLine, null);
                _currBlock.Begin();
                _levels--;
                _currBlock = _currBlock.Parent;

                if (_levels == 0)
                {
                    _currBlock = _rootBlock;
                    _processedLines.AddRange(_rootBlock.GetProcessedLines());
                }
            }
            else if (instruction.Equals(".break"))
            {
                if (_levels == 0)
                {
                    Controller.Log.LogEntry(line, "Illegal use of .break");
                    return;
                }
                string procinst = "@@ break @@";
                SourceLine shadow = new SourceLine()
                {
                    SourceString = ConstStrings.SHADOW_SOURCE,
                    Instruction = procinst
                };
                _currBlock.AddEntry(shadow, null);
            }
            else if (instruction.Equals("@@ for @@"))
            {
                if (_currBlock.IsBeginning())
                {
                    if (_currBlock == _rootBlock)
                        _currBlock.Begin();
                    _currBlock.Advance();
                }
                else
                {
                    var child = _currBlock.NextChild();
                    _currBlock.Advance();
                    _currBlock = child;
                    _currBlock.Begin();
                    _currBlock.Advance();

                    if (_breakBlock == null && !string.IsNullOrEmpty(_currBlock.InitExpression))
                    {
                        var initval = Controller.Variables.SetVariable(_currBlock.InitExpression, _currBlock.Scope);
                        _processedLines.Add(new SourceLine()
                        {
                            SourceString = ConstStrings.SHADOW_SOURCE,
                            Instruction = ConstStrings.VAR_DIRECTIVE,
                            Operand = string.Format("{0}={1}", initval.Key, initval.Value)
                        });
                    }
                }
            }
            else if (instruction.Equals("@@ next @@"))
            {
                if (_breakBlock != null)
                {
                    if (_currBlock == _breakBlock)
                        _breakBlock = null;
      
                    _currBlock = _currBlock.Parent;
                    if (_currBlock == null)
                    {
                        FullReset();
                    }
                    return;
                }

                // update each var in the expressions during runtime as well as
                // in output source (i.e., emit .let n = ... epxressions)
                foreach(var iterexp in _currBlock.IterExpressions)
                {
                    var itervar = Controller.Variables.SetVariable(iterexp, _currBlock.Scope);
                    _processedLines.Add(new SourceLine()
                    {
                        SourceString = ConstStrings.SHADOW_SOURCE,
                        Instruction = ConstStrings.VAR_DIRECTIVE,
                        Operand = string.Format("{0}={1}", itervar.Key, itervar.Value)
                    });
                }

                _currBlock.Begin();

                if (Controller.Evaluator.EvalCondition(_currBlock.Condition))
                {
                    _processedLines.AddRange(_currBlock.GetProcessedLines());
                }
                else
                {
                    _breakBlock = null;
                    _currBlock = _currBlock.Parent;
                    if (_currBlock == null)
                        FullReset();
                }
            }
            else if (instruction.Equals("@@ break @@"))
            {
                if (_breakBlock == null)
                    _breakBlock = _currBlock;
            }
            else if (_breakBlock == null)
            {
                _currBlock.AddEntry(line, null);
            }
        }

        /// <summary>
        /// Performs a reset operation on the ForNextBlock.
        /// </summary>
        public void Reset()
        {
            _processedLines.Clear(); 
        }

        /// <summary>
        /// Gets the flag that determines if the DotNetAsm.ForNextHandler is currently in
        /// processing mode.
        /// </summary>
        /// <returns>True, if the block handler is still processing input lines</returns>
        public bool IsProcessing()
        {
            return _levels > 0 || _breakBlock != null;
        }

        /// <summary>
        /// Gets the read-only processed blocks of repeated lines.
        /// </summary>
        public IEnumerable<SourceLine> GetProcessedLines()
        {
            return _processedLines;
        }
    }
}
