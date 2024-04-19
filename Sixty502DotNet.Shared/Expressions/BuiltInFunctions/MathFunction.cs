//-----------------------------------------------------------------------------
// Copyright (c) 2017-2024 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

namespace Sixty502DotNet.Shared;

/// <summary>
/// An implementation of the <see cref="BuiltInFunctionObject"/> class that
/// defines simple mathematical functions that take a fixed number of
/// numeric arguments and returns a double.
/// </summary>
public sealed class MathFunction : BuiltInFunctionObject
{
    private readonly Func<double[], double> _func;

    /// <summary>
    /// Construct a new instance of the built-int math function, with the
    /// implementation a function that take a double array and returns a double.
    /// </summary>
    /// <param name="name">The math function name.</param>
    /// <param name="func">The function implementation.</param>
    /// <param name="arity">The function arity.</param>
    public MathFunction(string name, Func<double[], double> func, int arity)
        : base(name, arity)
    {
        _func = func;
    }

    protected override ValueBase OnInvoke(SyntaxParser.ExpressionCallContext callSite, ArrayValue? parameters)
    {
        double[] p = new double[Arity];
        if (parameters != null)
        {
            for (int i = 0; i < p.Length; i++)
            {
                p[i] = parameters[i].AsDouble();
            }
        }
        return new NumericValue(_func(p));
    }
}

