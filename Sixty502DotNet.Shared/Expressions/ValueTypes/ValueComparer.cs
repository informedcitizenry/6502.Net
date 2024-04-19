//-----------------------------------------------------------------------------
// Copyright (c) 2017-2024 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

namespace Sixty502DotNet.Shared;

/// <summary>
/// Provides a function record for a custom user compare for sorting.
/// </summary>
public sealed class ValueComparerFunction
{
    /// <summary>
    /// Construct a new instance of a <see cref="ValueComparerFunction"/>.
    /// </summary>
    /// <param name="callSite">The parsed callsite.</param>
    /// <param name="evaluator">The <see cref="Shared.Evaluator"/> that will evaluate
    /// the custom compare function.</param>
    /// <param name="compareFunc">The compare function object.</param>
    public ValueComparerFunction(SyntaxParser.ExpressionCallContext callSite,
                                 Evaluator evaluator,
                                 FunctionObject compareFunc)
    {
        CallSite = callSite;
        Evaluator = evaluator;
        CompareFunc = compareFunc;
    }

    /// <summary>
    /// Get the compare function's call site.
    /// </summary>
    public SyntaxParser.ExpressionCallContext CallSite { get; init; }

    /// <summary>
    /// Get the <see cref="Shared.Evaluator"/> that will perform the compare.
    /// </summary>
    public Evaluator Evaluator { get; init; }

    /// <summary>
    /// The custom compare function.
    /// </summary>
    public FunctionObject CompareFunc { get; init; }
}

/// <summary>
/// Provides comparison between <see cref="ValueBase"/>s.
/// </summary>
public sealed class ValueComparer : IComparer<ValueBase>
{
    /// <summary>
    /// Construct a new function of the <see cref="ValueComparer"/> class.
    /// </summary>
	public ValueComparer() => Function = null;

    /// <summary>
    /// Compare two values.
    /// </summary>
    /// <param name="x">The first value to compare.</param>
    /// <param name="y">The second value to compare.</param>
    /// <returns><c>-1</c> if the first value is less than the second,
    /// <c>0</c> if the two values are equal, or <c>1</c> if the first value
    /// is greater than the second.</returns>
    /// <exception cref="Error"></exception>
    public int Compare(ValueBase? x, ValueBase? y)
    {
        if (Function != null && x != null && y != null)
        {
            ArrayValue compareParams = new()
            {
                x, y
            };
            ValueBase? result = Function.Evaluator.Invoke(Function.CallSite, Function.CompareFunc, compareParams)
                ?? throw new Error(Function.CallSite.exprList().expr()[1], "Compare function must return a value");
            if (!result.IsDefined)
            {
                return 0;
            }
            return result.AsInt();
        }
        return Comparer<ValueBase>.Default.Compare(x, y);
    }

    /// <summary>
    /// Get or set the <see cref="ValueComparerFunction"/> that will perform
    /// custom comparison between two values.
    /// </summary>
    public ValueComparerFunction? Function { get; set; }
}

