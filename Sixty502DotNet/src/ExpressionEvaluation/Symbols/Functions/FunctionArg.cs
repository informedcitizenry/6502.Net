//-----------------------------------------------------------------------------
// Copyright (c) 2017-2022 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;

namespace Sixty502DotNet
{
    public struct FunctionArg
    {
        public FunctionArg(string name)
            => (Name, Type, DefaultValue) = (name, TypeCode.Double, Value.Undefined);

        public FunctionArg(string name, TypeCode type)
            => (Name, Type, DefaultValue) = (name, type, Value.Undefined);

        public FunctionArg(string name, Value defaultValue)
            => (Name, Type, DefaultValue) = (name, TypeCode.Double, defaultValue);

        public string Name { get; init; }

        public TypeCode Type { get; init; }

        public Value DefaultValue { get; init; }
    }
}
