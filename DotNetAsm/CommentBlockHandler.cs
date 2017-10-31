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

namespace DotNetAsm
{
    public class CommentBlockHandler : AssemblerBase, IBlockHandler
    {
        #region Members

        private bool _inCommentBlock;
        private List<SourceLine> _processedLines;

        #endregion

        #region Constructors

        public CommentBlockHandler(IAssemblyController controller)
            :base(controller)
        {
            Reserved.DefineType("CommentDirectives", new string[]
            {
                ".comment", ".endcomment"
            });

            _inCommentBlock = false;
            _processedLines = new List<SourceLine>();
        }

        #endregion

        public IEnumerable<SourceLine> GetProcessedLines()
        {
            throw new NotImplementedException();
        }

        public bool IsProcessing()
        {
            return _inCommentBlock;
        }

        public void Process(SourceLine line)
        {
            throw new NotImplementedException();
        }

        public bool Processes(string token)
        {
            throw new NotImplementedException();
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }
    }
}
