//-----------------------------------------------------------------------------
// Copyright (c) 2017-2022 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;

namespace Sixty502DotNet
{
    /// <summary>
    /// An enumeration of the outcome of the most recently visited statement or
    /// block of statements.
    /// </summary>
    [Flags]
    public enum Status : int
    {
        /// <summary>
        /// The statement or block is evaluating and the subsequent statement
        /// should be visited.
        /// </summary>
        Evaluating = 0b00000,

        /// <summary>
        /// The statement or block is signalling all subsequent processing on
        /// statements within the block should cease.
        /// </summary>
        Complete = 0b00001,

        /// <summary>
        /// A statement in the block encountered a <c>.continue</c> directive
        /// and the next statement to be visited is the first in the block.
        /// </summary>
        Continue = 0b00010,

        /// <summary>
        /// A statement in the block encountered a <c>.break</c> directive
        /// and all subsequent processing on statements within the block
        /// should cease and control should return to the next statement in the
        /// parent block.
        /// </summary>
        Break = 0b00100,

        /// <summary>
        /// A statement in the block encountered a <c>.return</c> directive, all
        /// subsequent processing on statements within the block should cease,
        /// control should return to the statement in another block that called
        /// the function block.
        /// </summary>
        Return = 0b01000,

        /// <summary>
        /// A statement in the block encountered a <c>.goto</c> directive and
        /// the next statement to be visited should be the label operand in
        /// the <c>.goto</c> directive.
        /// </summary>
        Goto = 0b10000
    }

    /// <summary>
    /// A struct representing the state of visitation on the most recent
    /// block, or statement in a block.
    /// </summary>
    public struct BlockState
    {
        /// <summary>
        /// Construct a new <see cref="BlockState"/>.
        /// </summary>
        /// <param name="ret">The return value, if any.</param>
        /// <param name="stat">The status value.</param>
        /// <param name="gotoDest">The <c>.goto</c> destination statement,
        /// if any.</param>
        public BlockState(Value? ret = null,
                          Status stat = Status.Evaluating,
                          Sixty502DotNetParser.StatContext? gotoDest = null)
            => (returnValue, status, gotoDestination) = (ret, stat, gotoDest);

        /// <summary>
        /// Determins if the current block state is one of continuing the
        /// current block, either as evaluating, which tells the assembler to
        /// visit the next statement in the block, or as continuing, which
        /// tells the assembler to loop back to the first statement in the
        /// block.
        /// </summary>
        /// <returns><c>true</c> if the state is continuing, <c>false</c>
        /// otherwise.</returns>
        public bool IsContinuing()
            => status == Status.Evaluating || status == Status.Continue;

        /// <summary>
        /// Get the default Evaluating <see cref="BlockState"/>.
        /// </summary>
        /// <returns>The <see cref="BlockState"/> that is in a state of
        /// <see cref="Status.Evaluating"/>.</returns>
        public static BlockState Evaluating => new();

        /// <summary>
        /// The status value of the block state.
        /// </summary>
        public Status status;

        /// <summary>
        /// The return value, if any, of the block state if the status is
        /// <see cref="Status.Return"/>.
        /// </summary>
        public Value? returnValue;

        /// <summary>
        /// The destination statement of the label if the status is
        /// <see cref="Status.Goto"/>.
        /// </summary>
        public Sixty502DotNetParser.StatContext? gotoDestination;
    }
}
