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
using System.IO;
using System.Linq;
using System.Text;

namespace DotNetAsm
{
    /// <summary>
    /// Defines an interface for an assembly controller.
    /// </summary>
    public interface IAssemblyController
    {
        /// <summary>
        /// Add a line assembler to the DotNetAsm.IAssemblyController's list of assemblers.
        /// </summary>
        /// <param name="lineAssembler">The DotNetAsm.ILineAssembler</param>
        void AddAssembler(ILineAssembler lineAssembler);

        /// <summary>
        /// Add a user-defined symbol to the DotNetAsm.IAssemblyController's reserved words.
        /// </summary>
        /// <param name="symbol"></param>
        void AddSymbol(string symbol);


        /// <summary>
        /// Performs assembly operations based on the command line arguments passed,
        /// including output to an object file and assembly listing.
        /// </summary>
        void Assemble();

        /// <summary>
        /// Gets the command-line arguments passed by the end-user and parses into 
        /// strongly-typed options.
        /// </summary>
        AsmCommandLineOptions Options { get; }

        /// <summary>
        /// Gets or sets an output action to write custom-architecture header data to the output.
        /// </summary>
        Action<IAssemblyController,BinaryWriter> HeaderOutputAction { get; set; }

        /// <summary>
        /// Gets or sets an output action to writer custom-architecture header data to the output.
        /// </summary>
        Action<IAssemblyController, BinaryWriter> FooterOutputAction { get; set; }

        /// <summary>
        /// Get the value of the scoped label in the controller's symbol table (if exists).
        /// </summary>
        /// <param name="label">The label to lookup the value.</param>
        /// <param name="line">The SourceLine where the label is being referenced.</param>
        /// <returns>The label value as a string, otherwise an empty string.</returns>
        string GetScopedLabelValue(string label, SourceLine line);

        /// <summary>
        /// Checks if a given token is actually an instruction or directive, either
        /// for the DotNetAsm.AssemblyController or any line assemblers.
        /// </summary>
        /// <param name="token">The token to check</param>
        /// <returns>True, if the token is an instruction or directive</returns>
        bool IsInstruction(string token);

        /// <summary>
        /// Determines if the variable name is defined.
        /// </summary>
        /// <param name="variable">The variable to check</param>
        /// <returns>True, if the variable is defined</returns>
        bool IsVariable(string variable);

        /// <summary>
        /// Sets a variable to the given value.
        /// </summary>
        /// <param name="variable">The valid variable name</param>
        /// <param name="value">The value to provide</param>
        void SetVariable(string variable, long value);

        /// <summary>
        /// Gets the value of the variable, if it has been defined. To check if a variable
        /// has been defined before calling this method, first call the IsVariable method.
        /// </summary>
        /// <param name="variable">The variable name to get the value for</param>
        /// <returns>The variable's value</returns>
        long GetVariable(string variable);

        /// <summary>
        /// Gets or sets the disassembler. 
        /// </summary>
        ILineDisassembler Disassembler { get; set; }

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

        /// <summary>
        /// Gets the custom DotNetAsm.AsmEncoding for encoding text strings
        /// </summary>
        AsmEncoding Encoding { get; }

        /// <summary>
        /// Gets or sets the banner text used at start of compilation.
        /// </summary>
        string BannerText { get; set; }

        /// <summary>
        /// Gets or sets the verbose banner text at start of compilation
        /// </summary>
        string VerboseBannerText { get; set; }
    }
}
