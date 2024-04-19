//-----------------------------------------------------------------------------
// Copyright (c) 2017-2024 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

namespace Sixty502DotNet.Shared;

/// <summary>
/// Represents the implementation of the function to return the output size
/// of an object in bytes.
/// </summary>
public sealed class SizeofFunction : BuiltInFunctionObject
{
    /// <summary>
    /// Construct a new instance of the <see cref="SizeofFunction"/>.
    /// </summary>
	public SizeofFunction()
        : base("sizeof", 1)
    {
    }

    protected override ValueBase OnInvoke(SyntaxParser.ExpressionCallContext callSite, ArrayValue? parameters)
    {
        return new NumericValue(parameters![0].Size());
    }
}

