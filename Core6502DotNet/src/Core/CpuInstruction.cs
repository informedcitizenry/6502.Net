//-----------------------------------------------------------------------------
// Copyright (c) 2017-2021 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

namespace Core6502DotNet
{
    /// <summary>
    /// A struct that represents information about an instruction, including its 
    /// size, CPU and opcode.
    /// </summary>
    public readonly struct CpuInstruction
    {
        #region Constructors

        /// <summary>
        /// Creates a new instance of a CPU instruction.
        /// </summary>
        /// <param name="cpu">The CPU's name.</param>
        /// <param name="opcode">The instruction's opcode.</param>
        public CpuInstruction(string cpu, uint opcode)
        {
            CPU = cpu;
            Opcode = opcode;
            Size = 1;
        }

        /// <summary>
        /// Creates a new instance of a CPU instruction.
        /// </summary>
        /// <param name="cpu">The CPU's name.</param>
        /// <param name="opcode">The instruction's opcode.</param>
        /// <param name="size">The total size of the instruction, including operand data.</param>
        public CpuInstruction(string cpu, uint opcode, int size)
        {
            CPU = cpu;
            Opcode = opcode;
            Size = size;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the instruction size (including operands).
        /// </summary>
        public int Size { get; }

        /// <summary>
        /// Gets or sets the opcode of the instruction.
        /// </summary>
        public uint Opcode { get; }

        /// <summary>
        /// Gets or sets the CPU of this instruction.
        /// </summary>
        /// <value>The cpu.</value>
        public string CPU { get; }

        #endregion
    }
}
