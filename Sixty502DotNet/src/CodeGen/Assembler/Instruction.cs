//-----------------------------------------------------------------------------
// Copyright (c) 2017-2022 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

namespace Sixty502DotNet
{
    /// <summary>
    /// An abstraction of a CPU instruction, associating a mnemonic to an
    /// addressing mode.
    /// </summary>
    public record Instruction
    {
        /// <summary>
        /// Constructs a new <see cref="Instruction"/> record.
        /// </summary>
        /// <param name="mnemonic">The mnemonic type.</param>
        /// <param name="mode">The addressing mode.</param>
        public Instruction(int mnemonic, int mode)
            => (this.mnemonic, this.mode) = (mnemonic, mode);

        public int mnemonic;
        public int mode;
    }
}
