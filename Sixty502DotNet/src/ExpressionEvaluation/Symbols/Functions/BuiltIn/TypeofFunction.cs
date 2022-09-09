//-----------------------------------------------------------------------------
// Copyright (c) 2017-2022 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

namespace Sixty502DotNet
{
    /// <summary>
    /// Represents an implementation of the function that returns the type of an
    /// expression or value.
    /// </summary>
    public class TypeofFunction : FunctionDefinitionBase
    {
        /// <summary>
        /// Construct a new instance of the <see cref="TypeofFunction"/> class.
        /// </summary>
        public TypeofFunction()
            : base("typeof", new List<FunctionArg> { new FunctionArg("", TypeCode.Object) })
        {
        }

        private string GetType(Value value)
        {
            if (!value.IsDefined)
            {
                return "?";
            }
            if (value is DictionaryValue dict)
            {
                var key = dict.Keys.FirstOrDefault();
                if (key != null)
                {
                    var keysDouble = dict.KeyType == TypeCode.Double;
                    var valsDouble = dict.ElementsNumeric && !dict.ElementsIntegral;
                    var keyType = key.IsIntegral && keysDouble ? "Double" : GetType(key);
                    var val = dict[key];
                    var valType = val.IsIntegral && valsDouble ? "Double" : GetType(val);
                    return $"Dictionary<{keyType},{valType}>";
                }
                return "Dictionary";
            }
            if (value is ArrayValue array)
            {
                if (array.ElementCount > 0)
                {
                    var elem0 = array[0];
                    if (elem0.IsIntegral && array.ElementsNumeric && !array.ElementsIntegral)
                    {
                        return "Array<Double>";
                    }
                    return $"Array<{GetType(elem0)}>";
                }
                return "Array";
            }
            if (value is FunctionValue fcn)
            {
                return "Function";
            }
            return value.DotNetType switch
            {
                TypeCode.Boolean    => "Boolean",
                TypeCode.Char       => "Char",
                TypeCode.Double     => "Double",
                TypeCode.Int32      => "Integer",
                TypeCode.Int64      => "Long",
                TypeCode.String     => "String",
                TypeCode.UInt32     => "Unsigned",
                _                   => "Object"
            };
        }

        protected override Value OnInvoke(ArrayValue args)
            => new($"\"{GetType(args[0])}\"");
    }
}

