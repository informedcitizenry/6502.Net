//-----------------------------------------------------------------------------
// Copyright (c) 2017-2022 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;

namespace Sixty502DotNet
{
    /// <summary>
    /// Represents the implementation of a function that transforms elements
    /// of an array into a new array by calling a given transform function.
    /// </summary>
    public class MapFunction : FunctionDefinitionBase
    {
        /// <summary>
        /// Construct a new instance of the <see cref="MapFunction"/> class.
        /// </summary>
        public MapFunction()
            : base("map", new List<FunctionArg>
            {
                new FunctionArg("array", TypeCode.Object),
                new FunctionArg("transformFunc", TypeCode.Object)
            })
        {
        }

        protected override Value? OnInvoke(ArrayValue args)
        {
            if (args[1] is FunctionValue transformFunc)
            {
                if (args[0] is not ArrayValue arr)
                {
                    throw new Error(Errors.TypeMismatchError);
                }
                ArrayValue transformedArray = new ArrayValue();
                foreach (var val in arr)
                {
                    ArrayValue args2 = new ArrayValue();
                    args2.Add(val);
                    Value? transformed = transformFunc.Invoke(args2);
                    if (transformed == null)
                    {
                        throw new Error("Transform function did not return a value.");
                    }
                    if (!transformedArray.TypeCompatible(transformed))
                    {
                        throw new Error(Errors.TypeMismatchError);
                    }
                    transformedArray.Add(transformed);
                }
                return transformedArray;
            }
            throw new Error("Parameter must be a function expression.");
        }
    }
}

