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
    public class ConcatFunction : FunctionDefinitionBase
    {
        public ConcatFunction()
            : base("concat", new List<FunctionArg>
            {
                new FunctionArg("arr1", variantType: true),
                new FunctionArg("arr2", variantType: true)
            })
        {
        }

        private static Value? ConcatStrings(ArrayValue strVals)
        {
            if (strVals[0] is Value v1 && v1.IsString &&
                strVals[1] is Value v2 && v2.IsString)
            {
                return new Value($"\"{string.Concat(v1.ToString(true), v2.ToString(true))}\"");
            }
            throw new Error(Errors.TypeMismatchError);
        }

        protected override Value? OnInvoke(ArrayValue args)
        {
            if (args[0] is ArrayValue arr1 && args[1] is ArrayValue arr2 && arr1.TypeCompatible(arr2))
            {
                var concatlist = arr1.ToList();
                concatlist.AddRange(arr2.ToList());
                return new ArrayValue(concatlist);
            } 
            return ConcatStrings(args);
        }
    }
}

