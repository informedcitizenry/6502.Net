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
    /// Represents the implementation of a function that take a code point
    /// argument and returns an <see cref="IValue"/> with a <see cref="string"/>
    /// value.
    /// </summary>
    public class CharFunction : FunctionDefinitionBase
    {
        private readonly AsmEncoding _encoding;

        /// <summary>
        /// Construct a new instance of the <see cref="CharFunction"/> class.
        /// </summary>
        /// <param name="encoding">The encoding that converts the function
        /// argument to a string.</param>
        public CharFunction(AsmEncoding encoding)
            : base("char", new List<FunctionArg> { new FunctionArg("", TypeCode.Int32) })
            => _encoding = encoding;

        protected override Value OnInvoke(ArrayValue args)
        {
            if (args[0].ToInt() >= 0 && args[0].ToInt() <= 0x10FFFF)
            {
                var bytes = BitConverter.GetBytes(args[0].ToInt());
                var chars = _encoding.GetChars(bytes);
                return new Value($"\"{new string(chars)}\"");
            }
            throw new Error(Errors.IllegalQuantity);
        }
    }
}
