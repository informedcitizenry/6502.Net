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

namespace DotNetAsm
{
    /// <summary>
    /// An interface for a block handler to process defined blocks in assembly source.
    /// </summary>
    public interface IBlockHandler
    {
        /// <summary>
        /// Determines if the block handler processes the token (instruction).
        /// </summary>
        /// <param name="token">The instruction to check if the block handler processes</param>
        /// <returns>True, if the block handler processes, otherwise false</returns>
        bool Processes(string token);

        /// <summary>
        /// Process the DotNetAsm.SourceLine if it is processing or the instruction is a
        /// block instruction.
        /// </summary>
        /// <param name="line">The DotNetAsm.SourceLine to process</param>
        void Process(SourceLine line);

        /// <summary>
        /// Reset the block handler.
        /// </summary>
        void Reset();

        /// <summary>
        /// Check if the block handler is currently processing a block.
        /// </summary>
        /// <returns></returns>
        bool IsProcessing();

        /// <summary>
        /// Get the processed lines. This is typically called after the IsProcessing()
        /// method changes from true to false.
        /// </summary>
        /// <returns>A System.Collections.Generic.IEnumerable&lt;DotNetAsm.SourceLine&gt; 
        /// of the processed block</returns>
        IEnumerable<SourceLine> GetProcessedLines();
    }
}
