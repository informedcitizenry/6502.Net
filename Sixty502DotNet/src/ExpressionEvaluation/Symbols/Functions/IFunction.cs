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
    public interface IFunction
    {
        /// <summary>
        /// Invoke the function, returning a value.
        /// </summary>
        /// <param name="args">The arguments in the function call as an
        /// <see cref="ArrayValue"/> object.</param>
        /// <returns>The return value as an <see cref="Value"/> if the function
        /// returns a value, otherwise <code>null</code>.</returns>
        Value? Invoke(ArrayValue args);

        /// <summary>
        /// Gets the return type.
        /// </summary>
        TypeCode ReturnType { get; }

        /// <summary>
        /// Gets the list of expected arguments along with their default values,
        /// if any.
        /// </summary>
        IList<FunctionArg> Args { get; }
    }
}
