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
    /// Represents the implementation of the function that accepts the name of
    /// a defined section parameter and returns the section's starting address.
    /// </summary>
    public class SectionFunction : FunctionDefinitionBase
    {
        private readonly CodeOutput _codeOutput;

        /// <summary>
        /// Construct a new instance of the <see cref="SectionFunction"/>
        /// object.
        /// </summary>
        /// <param name="output">The code output object containing section
        /// definitions.</param>
        public SectionFunction(CodeOutput output)
            : base("section", new List<FunctionArg> { new FunctionArg("", TypeCode.String) })
            => _codeOutput = output;

        protected override Value OnInvoke(ArrayValue args)
        {
            var start = _codeOutput.GetSectionStart(args[0].ToString(true));
            return new Value(start);
        }
    }
}
