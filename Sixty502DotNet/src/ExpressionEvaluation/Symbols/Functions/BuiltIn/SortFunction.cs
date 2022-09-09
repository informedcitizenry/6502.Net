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
    public class SortFunction : FunctionDefinitionBase
    {
        public SortFunction()
            : base("sort", new List<FunctionArg> { new FunctionArg("array", variantType: true)} )
        {

        }

        protected override Value? OnInvoke(ArrayValue args)
        {
            if (args[0].IsString)
            {
                string argStr = args[0].ToString(true);
                return new Value($"\"{string.Concat(argStr.OrderBy(c => c))}\"");
            }
            if (args[0] is ArrayValue arr)
            {
                if (arr.ElementsNumeric)
                {
                    var numSorted = arr.OrderBy(v => v.ToDouble());
                    return new ArrayValue(numSorted.ToList());
                }
                if (arr.ElementType == TypeCode.String)
                {
                    var strSorted = arr.Select(v => v.ToString(true)).ToArray();
                    Array.Sort(strSorted, StringComparer.Ordinal);
                    var sortedArr = new ArrayValue();
                    foreach (var s in strSorted)
                        sortedArr.Add(new Value($"\"{s}\""));
                    return sortedArr;
                }
                if (arr.ElementType == TypeCode.Boolean)
                {
                    var boolSorted = arr.OrderBy(v => v.ToBool());
                    return new ArrayValue(boolSorted.ToList());
                }
            }
            throw new Error(Errors.TypeMismatchError);
        }
    }
}

