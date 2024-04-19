//-----------------------------------------------------------------------------
// Copyright (c) 2017-2024 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

namespace Sixty502DotNet.Shared;

/// <summary>
/// Represents an implementation of a function that returns the base address
/// of a section.
/// </summary>
public sealed class SectionFunction : BuiltInFunctionObject
{
    private readonly CodeOutput _output;

    /// <summary>
    /// Construct a new instance of a <see cref="SectionFunction"/>.
    /// </summary>
    /// <param name="services">The shared <see cref="AssemblyServices"/> for
    /// assembler runtime.</param>
	public SectionFunction(AssemblyServices services)
        : base("section", 1)
    {
        _output = services.State.Output;
    }

    protected override ValueBase OnInvoke(SyntaxParser.ExpressionCallContext callSite, ArrayValue? parameters)
    {
        string sectionName = parameters![0].AsString();
        try
        {
            return new NumericValue(_output.GetSectionStart(sectionName));
        }
        catch (Exception ex)
        {
            throw new Error(callSite, ex.Message);
        }
    }
}

