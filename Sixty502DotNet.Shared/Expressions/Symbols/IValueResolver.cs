//-----------------------------------------------------------------------------
// Copyright (c) 2017-2024 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;

namespace Sixty502DotNet.Shared;

/// <summary>
/// Represents an interface to a named symbol that can resolve to a value.
/// </summary>
public interface IValueResolver
{
    /// <summary>
    /// Get or set the resolver's bound value.
    /// </summary>
    ValueBase Value { get; set; }

    /// <summary>
    /// Get whether the resolver's value is constant.
    /// </summary>
    bool IsConstant { get; }
}

