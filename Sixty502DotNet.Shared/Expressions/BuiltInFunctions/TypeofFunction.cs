//-----------------------------------------------------------------------------
// Copyright (c) 2017-2023 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

namespace Sixty502DotNet.Shared;

/// <summary>
/// Represents an implementation of the function that returns the type of an
/// expression or value.
/// </summary>
public sealed class TypeofFunction : BuiltInFunctionObject
{
    /// <summary>
    /// Construct a new instance of the <see cref="TypeofFunction"/>.
    /// </summary>
	public TypeofFunction()
        : base("typeof", 1)
    {
    }

    protected override ValueBase OnInvoke(SyntaxParser.ExpressionCallContext callSite, ArrayValue? parameters)
    {
        return new StringValue($"\"{parameters![0].TypeName()}\"");
    }
}

