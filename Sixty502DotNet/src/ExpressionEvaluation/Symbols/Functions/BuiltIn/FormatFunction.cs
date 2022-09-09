//-----------------------------------------------------------------------------
// Copyright (c) 2017-2022 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
namespace Sixty502DotNet
{
    /// <summary>
    /// Implements a string formatter function.
    /// </summary>
    public class FormatFunction : FunctionDefinitionBase
    {
        private static readonly List<FunctionArg> s_formatArgs = new()
        {
            new FunctionArg("format", TypeCode.String),
            new FunctionArg("parms", TypeCode.Object)
        };

        /// <summary>
        /// Construct a new instance of the <see cref="FormatFunction"/> class.
        /// </summary>
        public FormatFunction()
            : base("format", s_formatArgs, true) { }

        protected override Value? OnInvoke(ArrayValue args)
        {
            if (args.Count == 1)
            {
                return new Value(args[0].ToString());
            }
            var format = args[0].ToString(true);
            var parmObjs = new object[args.Count - 1];
            for (var i = 1; i < args.Count; i++)
            {
                parmObjs[i - 1] = args[i].ToObject<object>() ?? "";
            }
            return new Value($"\"{string.Format(format, parmObjs)}\"");
        }
    }
}
