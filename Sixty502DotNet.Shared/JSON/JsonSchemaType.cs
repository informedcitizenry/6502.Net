//-----------------------------------------------------------------------------
// Copyright (c) 2017-2024 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

namespace Sixty502DotNet.Shared;

[Flags]
public enum JsonSchemaType : int
{
    None = 0,
    Boolean,
    Integer = Boolean << 1,
    Float = Boolean << 2,
    Number = Integer | Float,
    String = Boolean << 3,
    Null = Boolean << 4,
    Array = Boolean << 5,
    Object = Boolean << 6
}

