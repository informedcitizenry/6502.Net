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


namespace Asm6502.Net
{
    /// <summary>
    /// Defines an interface for an assembly controller.
    /// </summary>
    public interface IAssemblyController
    {
        /// <summary>
        /// Performs assembly operations based on the command line arguments passed,
        /// including output to an object file and assembly listing.
        /// </summary>
        /// <param name="args">The command line arguments to direct the assembler.</param>
        void Assemble(string[] args);

        /// <summary>
        /// Gets the command-line arguments passed by the end-user and parses into a strongly-typed
        /// set of options.
        /// </summary>
        AsmCommandLineOptions Options { get; }

        /// <summary>
        /// Get the value of the label in the controller's symbol table (if exists).
        /// </summary>
        /// <param name="label">The label to lookup the value.</param>
        /// <param name="line">The SourceLine where the label is being referenced.</param>
        /// <returns>The label value as a string, otherwise an empty string.</returns>
        string GetScopedLabelValue(string label, SourceLine line);

        /// <summary>
        /// Indicates if the instruction in the given source line 
        /// terminates all further assembly.
        /// </summary>
        /// <param name="line">The SourceLine to evaluate.</param>
        /// <returns>True if assembly should end, false otherwise.</returns>
        bool TerminateAssembly(SourceLine line);

        /// <summary>
        /// The Compilation object to handle binary output.
        /// </summary>
        Compilation Output { get; }

        /// <summary>
        /// The controller's error log to track errors and warnings.
        /// </summary>
        ErrorLog Log { get; }

        /// <summary>
        /// Gets the labels for the controller.
        /// </summary>
        IDictionary<string, string> Labels { get; }

        /// <summary>
        /// Gets expression evaluator for the controller.
        /// </summary>
        IEvaluator Evaluator { get; }
    }
}
