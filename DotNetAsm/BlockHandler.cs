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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNetAsm
{
    public abstract class BlockHandler<Thandler> : AssemblerBase
    {
        protected class Entry<Tentry>
        {
            public Entry(SourceLine line, Block<Tentry> block)
            {
                Line = line.Clone() as SourceLine;
                LinkedBlock = block;
            }

            public SourceLine Line { get; set; }
            public Block<Tentry> LinkedBlock { get; set; }
        }

        protected class Block<Tblock>
        {
            public List<Entry<Tblock>> Entries { get; set; }
            public Block<Tblock> BackLink { get; set; }
            public Tblock Key { get; set; }
        }

        private List<SourceLine> _source;
        private Block<Thandler> _currBlock;

        public BlockHandler(IAssemblyController controller, List<SourceLine> source) :
            base(controller)
        {
            _currBlock =
            RootBlock = new Block<Thandler>();
        }

        public bool ProcessesLine(SourceLine line)
        {
            return Reserved.IsReserved(line.Instruction) || Level > 0;
        }

        public void Process(SourceLine line)
        {
            if (!ProcessesLine(line))
                return;

            if (IsOpen(line.Instruction))
            {
                Level++;

                Block<Thandler> block = new Block<Thandler>();
                block.BackLink = _currBlock;
                Entry<Thandler> entry = new Entry<Thandler>(null, block);
                _currBlock.Entries.Add(entry);
                _currBlock = block;

                DoProcess(line, block);
            }
            else if (IsClosure(line.Instruction))
            {
                if (Level == 0)
                {
                    Controller.Log.LogEntry(line, ErrorStrings.ClosureDoesNotCloseBlock, line.Instruction);
                    return;
                }
                Level--;
                _currBlock = _currBlock.BackLink;
                if (Level == 0)
                {
                    DoProcessBlock(RootBlock, _source);
                }
            }
            else if (Reserved.IsReserved(line.Instruction))
            {
                DoProcess(line, _currBlock);
            }
            else
            {
                Entry<Thandler> entry = new Entry<Thandler>(line, null);
                _currBlock.Entries.Add(entry);
            }
        }

        public void Reset()
        {
            RootBlock.Entries.Clear();
            RootBlock.Key = default(Thandler);
            _currBlock = RootBlock;
            Level = 0;

            DoReset();
        }

        protected void ProcessEntries(Block<Thandler> block, List<SourceLine> source)
        {
            foreach (var entry in block.Entries)
            {
                if (entry != null)
                    DoProcessBlock(entry.LinkedBlock, source);
                else
                    source.Add(entry.Line.Clone() as SourceLine);
            }
        }

        protected override bool IsReserved(string token)
        {
            return Reserved.IsReserved(token);
        }

        protected abstract bool IsOpen(string token);

        protected abstract bool IsClosure(string token);

        protected abstract void DoReset();

        protected abstract void DoProcess(SourceLine line, Block<Thandler> block);

        protected abstract void DoProcessBlock(Block<Thandler> block, List<SourceLine> source);

        protected int Level { get; private set; }

        protected Block<Thandler> RootBlock { get; private set; }
    }
}
