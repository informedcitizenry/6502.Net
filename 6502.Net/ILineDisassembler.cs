using System;
using System.Collections.Generic;

namespace Asm6502.Net
{
    /// <summary>
    /// Represents an interface for a line disassembler.
    /// </summary>
    public interface ILineDisassembler
    {
        /// <summary>
        /// Disassemble a line of 6502-source.
        /// </summary>
        /// <param name="line">The SourceLine</param>
        /// <returns>A string representation of the source.</returns>
        string DisassembleLine(SourceLine line);

        /// <summary>
        /// Gets a flag indicating if printing is on.
        /// </summary>
        bool PrintingOn { get; }

        /// <summary>
        /// Gets or sets the set of directives to skip if 
        /// verbose option is not set.
        /// </summary>
        HashSet<string> SkipOnVerbose { get; set; }
    }
}
