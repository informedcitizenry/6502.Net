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
using System.Text;

namespace Asm6502.Net
{
    /// <summary>
    /// Represents an interface for a line disassembler.
    /// </summary>
    public interface ILineDisassembler
    {
        /// <summary>
        /// Disassemble a line of 6502 source.
        /// </summary>
        /// <param name="line">The SourceLine.</param>
        /// <returns>A string representation of the source.</returns>
        string DisassembleLine(SourceLine line);

        /// <summary>
        /// Disassemble a line of 6502 source to a supplied 
        /// System.Text.StringBuilder.
        /// </summary>
        /// <param name="line">The SourceLine to disassemble.</param>
        /// <param name="sb">A System.Text.StringBuilder to output disassembly.</param>
        void DisassembleLine(SourceLine line, StringBuilder sb);

        /// <summary>
        /// Gets a flag indicating if printing is on.
        /// </summary>
        bool PrintingOn { get; }
    }
}
