//-----------------------------------------------------------------------------
// Copyright (c) 2017-2022 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

namespace Sixty502DotNet
{
    /// <summary>
    /// A record of the runtime state of various assembly attributes, such as
    /// the current pass, statement, and starting program counter value.
    /// </summary>
    public record AssemblyState
    {

        /// <summary>
        /// Gets or sets the flag that determines whether disassembly should print.
        /// </summary>
        public bool PrintOff { get; set; }

        /// <summary>
        /// Gets or sets the number of passes attempted. Setting this
        /// property resets the PassNeeded property.
        /// </summary>
        public int CurrentPass { get; set; }

        /// <summary>
        /// Gets or sets the flag that determines if another pass is needed. 
        /// This field is reset when the Passes property changes.
        /// </summary>
        public bool PassNeeded { get; set; }

        /// <summary>
        /// Get or set the current parsed statement.
        /// </summary>
        public Sixty502DotNetParser.StatContext? CurrentStatement { get; set; }

        /// <summary>
        /// Get or set the initial long logical program counter at the start
        /// of the current statement assembly.
        /// </summary>
        public int LongLogicalPCOnAssemble { get; set; }

        /// <summary>
        /// Get or set the initial logical program counter at the start of the
        /// current statement assembly.
        /// </summary>
        public int LogicalPCOnAssemble { get; set; }
    }
}
