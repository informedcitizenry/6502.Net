//-----------------------------------------------------------------------------
// Copyright (c) 2017-2022 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

namespace Sixty502DotNet
{
    /// <summary>
    /// Represents the implementation of a function that returns the union of
    /// two separate array objects.
    /// </summary>
    public class UnionFunction : FunctionDefinitionBase
    {
        /// <summary>
        /// Construct a new instance of the <see cref="UnionFunction"/> class.
        /// </summary>
        public UnionFunction()
            : base("union", new List<FunctionArg>
            {
                new FunctionArg("arr1", TypeCode.Object),
                new FunctionArg("arr2", TypeCode.Object)
            })
        {
        }

        protected override Value? OnInvoke(ArrayValue args)
        {
            if (args[0] is ArrayValue arr1 && args[1] is ArrayValue arr2 && arr1.TypeCompatible(arr2))
            {
                var unionlist = arr1.ToHashSet();
                unionlist.UnionWith(arr2.ToHashSet());
                return new ArrayValue(unionlist.ToList());
            }
            throw new NotImplementedException();
        }
    }
}

