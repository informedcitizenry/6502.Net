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
    /// Represents the implementation of the function to return the length of
    /// a collection or string.
    /// </summary>
    public class LenFunction : SymbolBase, IFunction
    {
        /// <summary>
        /// Constructs a new instance of a <see cref="LenFunction"/>.
        /// </summary>
        public LenFunction()
            : base("len") { }

        public TypeCode ReturnType => TypeCode.Int32;

        public IList<FunctionArg> Args
            => new List<FunctionArg> { new FunctionArg("object", TypeCode.Object) };

        public Value Invoke(ArrayValue args)
        {
            if (args.Count == 1 && args[0].IsString || args[0] is ArrayValue)
            {
                return new Value(args[0].ElementCount);
            }
            if (args.Count == 0)
            {
                throw new Error("Missing parameter.");
            }
            throw new Error("Too many parameters.");
        }
    }
}
