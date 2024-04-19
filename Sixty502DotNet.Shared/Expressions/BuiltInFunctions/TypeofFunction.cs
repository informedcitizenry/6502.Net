//-----------------------------------------------------------------------------
// Copyright (c) 2017-2024 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System.Text;

namespace Sixty502DotNet.Shared;

/// <summary>
/// Represents an implementation of the function that returns the type of an
/// expression or value.
/// </summary>
public sealed class TypeofFunction : BuiltInFunctionObject
{
    private readonly AssemblyServices _services;

    /// <summary>
    /// Construct a new instance of the <see cref="TypeofFunction"/>.
    /// </summary>
	public TypeofFunction(AssemblyServices services)
        : base("typeof", 1)
    {
        _services = services;
    }

    protected override ValueBase OnInvoke(SyntaxParser.ExpressionCallContext callSite, ArrayValue? parameters)
    {
        return new StringValue($"\"{parameters![0].TypeName()}\"", _services.Encoding, _services.Encoding.EncodingName);
    }
}

