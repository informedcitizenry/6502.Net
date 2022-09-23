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
    /// Represents a function definition as a value type.
    /// </summary>
    public class FunctionValue : Value, IFunction
    {
        private readonly IFunction _fcnDefinition;

        /// <summary>
        /// Construct a new instance of a <see cref="FunctionValue"/> class.
        /// </summary>
        /// <param name="arrowCtx">The parsed arrow function context.</param>
        /// <param name="services">The shared <see cref="AssemblyServices"/> object.</param>
        public FunctionValue(Sixty502DotNetParser.ArrowFuncContext arrowCtx, AssemblyServices services)
            : this(UserFunctionDefinition.Declare(arrowCtx, services.Symbols), services)
        {
            ((UserFunctionDefinition)_fcnDefinition).Visitor = Visitor;
        }

        /// <summary>
        /// Construct a new instance of a <see cref="FunctionValue"/> class.
        /// </summary>
        /// <param name="function">The <see cref="IFunction"/> symbol to copy.</param>
        /// <param name="services">The shared <see cref="AssemblyServices"/> object.</param>
        public FunctionValue(IFunction function, AssemblyServices services)
        {
            Visitor = new BlockVisitor(services);
            _fcnDefinition = function;
            Type = typeof(FunctionValue); // just to get the DotNetType
        }

        /// <summary>
        /// Invoke the <see cref="FunctionValue"/> as a function.
        /// </summary>
        /// <param name="parms">The function parameters.</param>
        /// <returns></returns>
        public Value? Invoke(ArrayValue parms)
        {
            return _fcnDefinition.Invoke(parms);
        }

        public override string ToString() => "() =>";

        /// <summary>
        /// The <see cref="FunctionValue"/>'s <see cref="BlockVisitor"/> object.
        /// </summary>
        public BlockVisitor Visitor { get; init; }

        public TypeCode ReturnType => _fcnDefinition.ReturnType;

        public IList<FunctionArg> Args => _fcnDefinition.Args;
    }
}