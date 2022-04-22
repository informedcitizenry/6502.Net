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
    /// Represents the implementation of the function to convert
    /// an <see cref="int"/> to an <see cref="double"/>.
    /// </summary>
    public class FloatFunction : FunctionDefinitionBase
    {
        /// <summary>
        /// Construct a new instance of a <see cref="FloatFunction"/> class.
        /// </summary>
        public FloatFunction()
            : base(name: "float", new List<FunctionArg> { new FunctionArg("int", TypeCode.Int32) })
        { }

        protected override Value OnInvoke(ArrayValue args)
            => new(args[0].DotNetType == TypeCode.Char ? args[0].ToString(false)[0] * 1.0 : args[0].ToDouble());
    }
}
