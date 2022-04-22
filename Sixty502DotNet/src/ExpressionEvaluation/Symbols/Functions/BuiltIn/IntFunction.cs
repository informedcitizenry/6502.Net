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
    /// a <see cref="double"/> to an <see cref="int"/>.
    /// </summary>
    public sealed class IntFunction : FunctionDefinitionBase
    {
        /// <summary>
        /// Construct a new instance of the <see cref="IntFunction"/> class.
        /// </summary>
        public IntFunction()
            : base(name: "int", new List<FunctionArg> { new FunctionArg("float", TypeCode.Double) })
        { }

        protected override Value OnInvoke(ArrayValue args)
            => new(args[0].DotNetType == TypeCode.Char ? args[0].ToString(false)[0] : args[0].ToInt());
    }
}
