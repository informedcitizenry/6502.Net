//-----------------------------------------------------------------------------
// Copyright (c) 2017-2023 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

namespace Sixty502DotNet.Shared;

/// <summary>
/// Represents an implementation of the function that returns a range of
/// elements from a collection. 
/// </summary>
public sealed class RangeFunction : BuiltInFunctionObject
{
    /// <summary>
    /// Construct a new instance of the <see cref="RangeFunction"/>.
    /// </summary>
    public RangeFunction()
        : base("range", -1)
    {
    }

    protected override ValueBase OnInvoke(SyntaxParser.ExpressionCallContext callSite, ArrayValue parameters)
    {
        int start = 0, step = 1;
        int stop = parameters[0].AsInt();
        if (parameters.Count > 1)
        {
            start = parameters[0].AsInt();
            stop = parameters[1].AsInt();
            if (parameters.Count > 2)
            {
                if (parameters.Count > 3)
                {
                    throw new Error(callSite.exprList(), "Too many parameters for function");
                }
                step = parameters[2].AsInt();
            }
        }
        if (step == 0 || (Math.Sign(step) != Math.Sign(stop) && Math.Sign(step) == Math.Sign(start)))
        {
            throw new Error(callSite.exprList(), "One or more parameters is invalid");
        }
        ArrayValue array = new();
        for (int i = start; (start > stop && i > stop) || (start < stop && i < stop); i += step)
        {
            array.Add(new NumericValue(i)
            {
                Expression = callSite
            });
        }
        return array;
    }
}

