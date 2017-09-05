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

namespace DotNetAsm
{
    /// <summary>
    /// Defines an interface for a line assembler.
    /// </summary>
    public interface ILineAssembler
    {
        /// <summary>
        /// Assemble the line of source into output bytes of the
        /// target architecture.
        /// </summary>
        /// <param name="evaluator">An expression evaluator.</param>
        void AssembleLine(SourceLine line);

        /// <summary>
        /// Gets the size of the instruction in the source line.
        /// </summary>
        /// <param name="line">The source line to query.</param>
        /// <returns>Returns the size in bytes of the instruction or directive.</returns>
        int GetInstructionSize(SourceLine line);

        /// <summary>
        /// Indicates whether this line assembler will assemble the 
        /// given instruction or directive.
        /// </summary>
        /// <param name="instruction">The instruction.</param>
        /// <returns>True, if the line assembler can assemble the source, 
        /// otherwise false.</returns>
        bool AssemblesInstruction(string instruction);
    }
}
