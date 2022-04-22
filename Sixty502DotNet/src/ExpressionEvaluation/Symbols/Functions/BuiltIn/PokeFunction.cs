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
    /// Represents an implementation of the function to directly update the
    /// generated code at the specified offset.
    /// </summary>
    public class PokeFunction : FunctionDefinitionBase
    {
        private readonly CodeOutput _output;

        /// <summary>
        /// Constructs a new instance of the <see cref="PokeFunction"/> class.
        /// </summary>
        /// <param name="output">The code output object.</param>
        public PokeFunction(CodeOutput output)
            : base("poke", new List<FunctionArg>
            {
                new FunctionArg("address", TypeCode.Int32),
                new FunctionArg("value", TypeCode.Int32)
            }) => _output = output;

        protected override Value OnInvoke(ArrayValue args)
        {
            if (args[0].IsDefined && args[1].IsDefined)
            {
                if (args[0].ToInt() < short.MinValue || args[0].ToInt() > ushort.MaxValue ||
                args[1].ToInt() < sbyte.MinValue || args[1].ToInt() > byte.MaxValue)
                {
                    throw new Error(Errors.IllegalQuantity);
                }
                _output.Poke(args[0].ToInt() & 0xFFFF, Convert.ToByte(args[1].ToInt() & 0xFF));
            }
            return Value.Undefined();
        }
    }
}
