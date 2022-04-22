//-----------------------------------------------------------------------------
// Copyright (c) 2017-2022 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

namespace Sixty502DotNet
{
    /// <summary>
    /// A representation of an opcode, including its actual code value,
    /// overall instruction size, and CPU.
    /// </summary>
    public struct Opcode
    {
        /// <summary>
        /// Construct an <see cref="Opcode"/>.
        /// </summary>
        /// <param name="cpu">The opcode's CPU.</param>
        /// <param name="code">The code value.</param>
        /// <param name="size">The instruction size.</param>
        public Opcode(string cpu, int code, int size = 1)
            => (this.cpu, this.code, this.size) = (cpu, code, size);

        /// <summary>
        /// The opcode's code value.
        /// </summary>
        public int code;

        /// <summary>
        /// The opcode instruction's overall size in bytes.
        /// </summary>
        public int size;

        /// <summary>
        /// The opcode's CPU.
        /// </summary>
        public string cpu;
    }
}
