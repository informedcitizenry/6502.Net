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
    /// An implementation of the <see cref="FunctionDefinitionBase"/> class that
    /// defines simple mathematical functions that take a fixed number of
    /// numeric arguments and returns a double.
    /// </summary>
    public class MathFunction : FunctionDefinitionBase
    {
        private readonly Func<double[], double> _invocation;

        private MathFunction(string name, IList<FunctionArg> args, Func<double[], double> definition)
            : base(name, args)
            => (_invocation, IsReferenced) = (definition, true);

        /// <summary>
        /// Construct and return a <see cref="MathFunction"/> that accepts a
        /// single argument.
        /// </summary>
        /// <param name="definition">The function definition.</param>
        /// <returns>A <see cref="MathFunction"/> that accepts a single
        /// argument.</returns>
        public static MathFunction OneArg(string name, Func<double[], double> definition)
        {
            var singleArg = new List<FunctionArg> { new FunctionArg("", TypeCode.Double) };
            return new MathFunction(name, singleArg, definition);
        }

        /// <summary>
        /// Construct and return a <see cref="MathFunction"/> that accepts a
        /// two arguments.
        /// </summary>
        /// <param name="definition">The function definition.</param>
        /// <returns>A <see cref="MathFunction"/> that accepts two
        /// arguments.</returns>
        public static MathFunction TwoArg(string name, Func<double[], double> definition)
        {
            var arg = new FunctionArg("", TypeCode.Double);
            return new MathFunction(name, new List<FunctionArg> { arg, arg }, definition);
        }

        protected override Value? OnInvoke(ArrayValue args)
        {
            var argAsDouble = args.ToArray<double>();
            if (argAsDouble == null)
            {
                return null;
            }
            return new Value(_invocation(argAsDouble));
        }
    }
}
