//-----------------------------------------------------------------------------
// Copyright (c) 2017-2024 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

namespace Sixty502DotNet.Shared;

/// <summary>
/// Represents the implementation of the function to convert
/// an <see cref="int"/> to an <see cref="double"/>.
/// </summary>
public sealed class FloatFunction : BuiltInFunctionObject
{
    /// <summary>
    /// Construct a new instance of a <see cref="FloatFunction"/> class.
    /// </summary>
    public FloatFunction()
        : base("float", 1)
    { }

    protected override ValueBase OnInvoke(SyntaxParser.ExpressionCallContext callSite, ArrayValue? parameters)
    {
        return new NumericValue(parameters![0].AsDouble(), parameters[0].ValueType == ValueType.Number);
    }
}

