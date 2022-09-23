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
    /// Represents the implementation of a function that reduces array elements
    /// to a single value according to the algorithm of a given function.
    /// </summary>
    public class ReduceFunction : FunctionDefinitionBase
    {
        /// <summary>
        /// Construct a new instance of the <see cref="ReduceFunction"/> class.
        /// </summary>
        public ReduceFunction()
            : base("reduce", new List<FunctionArg>
            {
                new FunctionArg("array", TypeCode.Object),
                new FunctionArg("reducer", TypeCode.Object)
            })
        {
        }

        protected override Value? OnInvoke(ArrayValue args)
        {
            if (args[1] is FunctionValue reducer)
            {
                if (args[0] is ArrayValue array)
                {
                    Value reduced = array[0];
                    for(int i = 1; i < array.Count; i++)
                    {
                        ArrayValue reduceParams = new()
                        {
                            reduced,
                            array[i]
                        };
                        Value? reduction = reducer.Invoke(reduceParams);
                        if (reduction?.IsDefined != true)
                        {
                            throw new Error("Function did not return a value.");
                        }
                        if (!reduced.SetAs(reduction!))
                        {
                            throw new Error(Errors.TypeMismatchError);
                        }
                    }
                    return reduced;
                }
                throw new Error(Errors.TypeMismatchError);
            }
            throw new Error("Parameter must be a function expression.");
        }
    }
}

