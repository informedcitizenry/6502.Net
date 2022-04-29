//-----------------------------------------------------------------------------
// Copyright (c) 2017-2022 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Sixty502DotNet
{
    /// <summary>
    /// Represents an implementation of the function that reads a byte of
    /// generated code at the specified offset.
    /// </summary>
    public class PeekFunction : FunctionDefinitionBase
    {
        private readonly CodeOutput _output;

        /// <summary>
        /// Construct a new instance of the <see cref="PeekFunction"/> class.
        /// </summary>
        /// <param name="output">The code output object.</param>
        public PeekFunction(CodeOutput output)
            : base("peek", new List<FunctionArg> { new FunctionArg("", TypeCode.Int32) })
            => _output = output;

        protected override Value OnInvoke(ArrayValue args)
        {
            if (args[0].IsDefined)
            {
                if (args[0].ToInt() >= short.MinValue && args[0].ToInt() <= ushort.MaxValue)
                {
                    return new Value(_output.Peek(args[0].ToInt() & 0xFFFF));
                }
                throw new Error(Errors.IllegalQuantity);
            }
            return Value.Undefined;
        }
    }
}
