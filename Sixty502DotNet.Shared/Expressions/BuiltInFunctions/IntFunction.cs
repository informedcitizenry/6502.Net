//-----------------------------------------------------------------------------
// Copyright (c) 2017-2023 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

namespace Sixty502DotNet.Shared;

/// <summary>
/// Represents the implementation of the function to convert
/// a <see cref="double"/> to an <see cref="int"/>.
/// </summary>
public sealed class IntFunction : BuiltInFunctionObject
{
    /// <summary>
    /// Construct a new instance of an <see cref="IntFunction"/>.
    /// </summary>
	public IntFunction()
        : base("int", 1)
    {

    }

    protected override ValueBase OnInvoke(SyntaxParser.ExpressionCallContext callSite, ArrayValue? parameters)
    {
        return new NumericValue(parameters![0].AsDouble());
    }
}

