//-----------------------------------------------------------------------------
// Copyright (c) 2017-2019 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

namespace DotNetAsm
{
    /// <summary>
    /// A class that represents information about an instruction, including its 
    /// size, CPU and opcode.
    /// </summary>
    public class Instruction
    {
        /// <summary>
        /// The instruction size (including operands).
        /// </summary>
        public int Size { get; set; }

        /// <summary>
        /// The opcode of the instruction.
        /// </summary>
        public int Opcode { get; set; }

        /// <summary>
        /// Gets or sets the CPU of this instruction.
        /// </summary>
        /// <value>The cpu.</value>
        public string CPU { get; set; }
    }

    /// <summary>
    /// Represents an operand format, including captured expressions
    /// </summary>
    public class OperandFormat
    {
        /// <summary>
        /// The format string of the operand
        /// </summary>
        public string FormatString;

        /// <summary>
        /// The first captured evaluated expression.
        /// </summary>
        public long Eval1 { get; set; }

        /// <summary>
        /// The second captured evaluated expression.
        /// </summary>
        public long Eval2 { get; set; }
    }
}