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
    /// Represents the implementation of a function that filters a given array
    /// or string according to a predicate.
    /// </summary>
    public class FilterFunction : FunctionDefinitionBase
    {
        /// <summary>
        /// Construct a new instance of the <see cref="FilterFunction"/> class.
        /// </summary>
        public FilterFunction()
            : base("filter", new List<FunctionArg>
            {
                new FunctionArg("array", variantType: true),
                new FunctionArg("predicate", TypeCode.Object)
            })
        {

        }

        private static Value? FilterString(Value str, FunctionValue predicate)
        {
            string strVal = str.ToString(true);
            StringBuilder filtered = new();
            foreach(var c in strVal)
            {
                ArrayValue args = new()
                {
                    new Value($"'{c}'")
                };
                Value? match = predicate.Invoke(args);
                if (match == null)
                {
                    throw new Error("Predicate did not return a value.");
                }
                if (match.DotNetType != TypeCode.Boolean)
                {
                    throw new Error(Errors.TypeMismatchError);
                }
                if (match.ToBool())
                {
                    filtered.Append(c);
                }
            }
            return new Value($"\"{filtered}\"");
        }

        protected override Value? OnInvoke(ArrayValue args)
        {
            if (args[1] is FunctionValue predicate)
            {
                if (args[0].IsString)
                {
                    return FilterString(args[0], predicate);
                }
                if (args[0] is not ArrayValue arr)
                {
                    throw new Error(Errors.TypeMismatchError);
                }
                ArrayValue filtered = new();
                foreach(var val in arr)
                {
                    ArrayValue args2 = new()
                    {
                        val
                    };
                    Value? match = predicate.Invoke(args2);
                    if (match == null)
                    {
                        throw new Error("Predicate did not return a value.");
                    }
                    if (match.DotNetType != TypeCode.Boolean)
                    {
                        throw new Error(Errors.TypeMismatchError);
                    }
                    if (match.ToBool())
                    {
                        filtered.Add(val);
                    }
                }
                return filtered;
            }
            throw new Error("Parameter must be a function expression.");
        }
    }
}

