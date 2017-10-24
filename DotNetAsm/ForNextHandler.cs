using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNetAsm
{
    public class ForNextHandler : AssemblerBase, IBlockHandler
    {
        #region Classes 

        /// <summary>
        /// A block of for next loops implemented as a linked list.
        /// </summary>
        private class ForNextBlock
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

            private LinkedList<ForNextEntry> _entries;
            private LinkedListNode<ForNextEntry> _currentEntry;

            #endregion

            #region Constructors

            /// <summary>
            /// Constructs a new instance of a ForNextBlock
            /// </summary>
            public ForNextBlock()
            {
                Parent = null;
                _entries = new LinkedList<ForNextEntry>();
                Variable =
                Condition = string.Empty;
                Init = Step = 0;
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
            public ForNextBlock Parent { get; private set; }

            /// <summary>
            /// Gets or sets the iteration variable of the block.
            /// </summary>
            public string Variable { get; set; }

            /// <summary>
            /// Gets or sets the initial value of the iteration variable at the start of the loop.
            /// </summary>
            public long Init { get; set; }

            /// <summary>
            /// The step (increment amount) of the iteration variable of the block.
            /// </summary>
            public long Step { get; set; }

            /// <summary>
            /// The condition to meet to stop the block.
            /// </summary>
            public string Condition { get; set; }

            #endregion
        }

        #endregion

        private ForNextBlock _rootBlock;
        private ForNextBlock _currBlock;
        private ForNextBlock _breakBlock;

        private List<SourceLine> _processedLines;

        private int _levels;

        /// <summary>
        /// Constructs a DotNetAsm.ForNextHandler.
        /// </summary>
        /// <param name="controller">The DotNetAsm.IAssemblyController for this
        /// handler.</param>
        public ForNextHandler(IAssemblyController controller)
            : base(controller)
        {
            Reserved.DefineType("Directives", new string[] 
            { 
                ".for", ".next", ".break",
                "@@ for @@", "@@ next @@", "@@ break @@"
            });
            _currBlock =
            _rootBlock = new ForNextBlock();
            _breakBlock = null;
            _levels = 0;
            _processedLines = new List<SourceLine>();
        }

        /// <summary>
        /// Perform a full reset on the block.
        /// </summary>
        private void FullReset()
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
                    Controller.Log.LogEntry(line, ErrorStrings.None);
                    return;
                }      
                // .for <var> = <init>, <condition>, <step>
                var csvs = line.CommaSeparateOperand();
                if (csvs.Count > 3)
                {
                    Controller.Log.LogEntry(line, ErrorStrings.TooManyArguments, line.Instruction);
                    return;
                }
                else if (csvs.Count < 2)
                {
                    Controller.Log.LogEntry(line, ErrorStrings.TooFewArguments, line.Instruction);
                    return;
                }
                var init = csvs.First().Split(new char[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                if (init.Length != 2)
                {
                    Controller.Log.LogEntry(line, ErrorStrings.None);
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

                _currBlock.Variable = init.First().Trim();
                _currBlock.Init = Controller.Evaluator.Eval(init.Last(), int.MinValue, uint.MaxValue);
                _currBlock.Condition = csvs[1];
                Controller.SetVariable(_currBlock.Variable, _currBlock.Init);

                if (_currBlock == _rootBlock)
                {
                    _processedLines.Add(new SourceLine()
                    {
                        SourceString = SourceLine.SHADOW_SOURCE,
                        Label = _currBlock.Variable,
                        Instruction = ".var",
                        Operand = _currBlock.Init.ToString()
                    });
                }
                _currBlock.AddEntry(new SourceLine()
                {
                    SourceString = SourceLine.SHADOW_SOURCE,
                    Instruction = "@@ for @@"
                }, null);

                if (csvs.Count == 3)
                    _currBlock.Step = (int)Controller.Evaluator.Eval(csvs.Last(), int.MinValue, uint.MaxValue);
                else
                    _currBlock.Step = 1;
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
                    SourceString = SourceLine.SHADOW_SOURCE,
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
                    SourceString = SourceLine.SHADOW_SOURCE,
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

                    if (_breakBlock == null)
                    {
                        Controller.SetVariable(_currBlock.Variable, _currBlock.Init);

                        _processedLines.Add(new SourceLine()
                        {
                            SourceString = SourceLine.SHADOW_SOURCE,
                            Label = _currBlock.Variable,
                            Instruction = ".var",
                            Operand = _currBlock.Init.ToString()
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

                var iteration_value = Controller.GetVariable(_currBlock.Variable);
                Controller.SetVariable(_currBlock.Variable, iteration_value + _currBlock.Step);
                _currBlock.Begin();

                if (Controller.Evaluator.EvalCondition(_currBlock.Condition))
                {
                    _processedLines.Add(new SourceLine()
                    {
                        SourceString = SourceLine.SHADOW_SOURCE,
                        Label = _currBlock.Variable,
                        Instruction = ".var",
                        Operand = (iteration_value + _currBlock.Step).ToString()
                    });
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
