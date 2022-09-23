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
    /// Represents a function definition, which implements the interface
    /// <see cref="IFunction"/> and inherits the <see cref="SymbolBase"/> class.
    /// This class must be inherited.
    /// </summary>
    public abstract class FunctionDefinitionBase : SymbolBase, IFunction
    {
        /// <summary>
        /// Construct a new instance of the <see cref="FunctionDefinitionBase"/>
        /// class.
        /// </summary>
        /// <param name="name">The function name.</param>
        /// <param name="args">The list of expected arguments and their
        /// default values, if any.</param>
        /// <param name="variableArgs">Set whether the function should expect
        /// a variable number of arguments.</param>
        protected FunctionDefinitionBase(string name, IList<FunctionArg> args, bool variableArgs)
            : this(name, args) => VariableArgs = variableArgs;

        /// <summary>
        /// Construct a new instance of the <see cref="FunctionDefinitionBase"/>
        /// class.
        /// </summary>
        /// <param name="name">The function name.</param>
        /// <param name="args">The list of expected arguments and their
        protected FunctionDefinitionBase(string name, IList<FunctionArg> args)
            : base(name)
        {
            Args = args;
            FirstDefaultArg = -1;
            for (int i = 0; i < args.Count; i++)
            {
                if (FirstDefaultArg == -1 && args[i].DefaultValue.IsDefined)
                {
                    FirstDefaultArg = i;
                }
                else if (FirstDefaultArg >= 0 && !args[i].DefaultValue.IsDefined)
                {
                    throw new Error("Argument missing default value");
                }
            }
        }

        public virtual TypeCode ReturnType => TypeCode.Double;

        public IList<FunctionArg> Args { get; init; }

        /// <summary>
        /// The implementation of the function invocation.
        /// </summary>
        /// <param name="args">The arguments passed to the function
        /// as an <see cref="ArrayValue"/> object.</param>
        /// <returns>The return value of the function call as an
        /// <see cref="Value"/> if the function returns a value, otherwise
        /// <code>null</code>.</returns>
        protected abstract Value? OnInvoke(ArrayValue args);

        public Value? Invoke(ArrayValue args)
        {
            if (args.Count != Args.Count)
            {
                if (args.Count > Args.Count && !VariableArgs)
                {
                    throw new Error("Too many arguments.");
                }
                if (args.Count < Args.Count && (args.Count < FirstDefaultArg || FirstDefaultArg == -1))
                {
                    throw new Error("Too few arguments.");
                }
            }
            for (var i = 0; i < args.Count && !VariableArgs; i++)
            {
                if (args[i].IsDefined &&
                    args[i].DotNetType != Args[i].Type &&
                    !Args[i].VariantType &&
                    !(args[i].IsNumeric && Args[i].Type == TypeCode.Double))
                {
                    throw new Error(Errors.TypeMismatchError);
                }
            }
            return OnInvoke(args);
        }

        public override string ToString()
            => $"{Name}()";

        /// <summary>
        /// Gets the flag indicating the function accepts a variable number
        /// of arguments.
        /// </summary>
        protected bool VariableArgs { get; init; }

        /// <summary>
        /// Gets the index of the first argument in the list that has a default
        /// value.
        /// </summary>
        public int FirstDefaultArg { get; init; }
    }
}
