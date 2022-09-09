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
            => (Name, Type, VariantType, DefaultValue) = (name, TypeCode.Double, false, Value.Undefined);

        public FunctionArg(string name, TypeCode type)
            => (Name, Type, VariantType, DefaultValue) = (name, type, false, Value.Undefined);

        public FunctionArg(string name, Value defaultValue)
            => (Name, Type, VariantType, DefaultValue) = (name, defaultValue.DotNetType, false, defaultValue);

        public FunctionArg(string name, bool variantType)
            => (Name, Type, VariantType, DefaultValue) =
            (name, variantType ? TypeCode.Object : TypeCode.Double, variantType, Value.Undefined);

        public string Name { get; init; }

        public bool VariantType { get; init; }

        public TypeCode Type { get; init; }

        public Value DefaultValue { get; init; }
    }
}
