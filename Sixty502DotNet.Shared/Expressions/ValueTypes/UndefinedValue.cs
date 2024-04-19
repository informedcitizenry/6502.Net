//-----------------------------------------------------------------------------
// Copyright (c) 2017-2024 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

namespace Sixty502DotNet.Shared;

/// <summary>
/// Represents an undefined value.
/// </summary>
public sealed class UndefinedValue : ValueBase
{
    public UndefinedValue() => IsDefined = false;

    public override bool AsBool() => false;

    public override double AsDouble() => double.NaN;

    public override string AsString() => string.Empty;

    public override char AsChar() => char.MinValue;

    protected override void OnSetAs(ValueBase other)
    {
        throw new InvalidCastException("Cannot set an undefined value.");
    }

    protected override int OnCompareTo(ValueBase other)
    {
        return 0;
    }

    public override int Size() => 0;

    public override string TypeName() => "?";

    public override string ToString()
    {
        return "undefined";
    }

    protected override bool OnEqualTo(ValueBase other)
    {
        return other?.IsDefined != true;
    }

    public override object? Data() => null;
}

