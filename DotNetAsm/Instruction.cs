//-----------------------------------------------------------------------------
// Copyright (c) 2017-2019 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System.Collections.Generic;

namespace DotNetAsm
{
    /// <summary>
    /// A class that represents information about an instruction, including its 
    /// size, CPU and opcode.
    /// </summary>
    public class Instruction
    {
        /// <summary>
        /// Gets or sets the instruction size (including operands).
        /// </summary>
        public int Size { get; set; }

        /// <summary>
        /// Gets or sets the opcode of the instruction.
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
        #region Constructors

        /// <summary>
        /// Constructs a new OperandFormat instance
        /// </summary>
        public OperandFormat()
        {
            FormatString = string.Empty;
            Evaluations = new List<long>();
            EvaluationSizes = new List<int>();
        }

        #endregion

        #region Properties

        /// <summary>
        /// The format string of the operand
        /// </summary>
        public string FormatString;

        /// <summary>
        /// The captured evaluations
        /// </summary>
        public List<long> Evaluations { get; set; }

        /// <summary>
        /// The captured evaluation sizes
        /// </summary>
        public List<int> EvaluationSizes { get; set; }

        #endregion
    }
}