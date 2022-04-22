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
    /// Represents the implementation of the function to return the output size
    /// of an object in bytes.
    /// </summary>
    public class SizeofFunction : SymbolBase, IFunction
    {
        /// <summary>
        /// Construct a new instance of the <see cref="SizeofFunction"/> class.
        /// </summary>
        public SizeofFunction()
            : base("sizeof") { }

        public TypeCode ReturnType => TypeCode.Int32;

        public IList<FunctionArg> Args
            => new List<FunctionArg>() { new FunctionArg("type", TypeCode.Object) };

        private int ArraySize(ArrayValue val)
        {
            int size = 0;
            foreach (var e in val)
            {
                if (e is ArrayValue a)
                {
                    size += ArraySize(a);
                }
                else if (e.IsString)
                {
                    size += e.ElementCount;
                }
                else
                {
                    size += e.DotNetType switch
                    {
                        TypeCode.Boolean => 1,
                        TypeCode.Char => 1,
                        _ => e.ToDouble().Size()
                    };
                }
            }
            return size;
        }

        public Value? Invoke(ArrayValue args)
        {
            if (args.Count == 1)
            {
                return new Value(ArraySize(args));
            }
            return new Value();
        }
    }
}
