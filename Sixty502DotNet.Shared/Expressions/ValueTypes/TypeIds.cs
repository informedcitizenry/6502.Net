//-----------------------------------------------------------------------------
// Copyright (c) 2017-2023 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

namespace Sixty502DotNet.Shared;

/// <summary>
/// Represent the identifiers of the assembler's built-in types.
/// </summary>
public static class TypeIds
{
    /// <summary>
    /// The internal type name of an array, used to resolve member functions.
    /// </summary>
    public const string Array = "@Array.prototype@";

    /// <summary>
    /// The internal type name of a dictionary, used to resolve member functions.
    /// </summary>
    public const string Dictionary = "@Dictionary.prototype@";

    /// <summary>
    /// The internal type name of a callable object, used to resolve member functions.
    /// </summary>
    public const string Function = "@Function.prototype@";

    /// <summary>
    /// The internal type name of primitives, used to resolve member functions.
    /// </summary>
    public const string Primitive = "@Primitive.prototype@";

    /// <summary>
    /// The internal type name of a string, used to resolve member functions.
    /// </summary>
    public const string String = "@String.prototype@";

    /// <summary>
    /// The internal type name of a tuple, used to resolve member functions.
    /// </summary>
    public const string Tuple = "@Tuple.prototype@";
}

