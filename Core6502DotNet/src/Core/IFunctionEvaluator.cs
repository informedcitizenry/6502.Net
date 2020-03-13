//-----------------------------------------------------------------------------
// Copyright (c) 2017-2020 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

namespace Core6502DotNet
{
    /// <summary>
    /// Defines an interface for a function evaluator.
    /// </summary>
    public interface IFunctionEvaluator
    {
        /// <summary>
        /// Determines whether the <see cref="IFunctionEvaluator"/> can evaluate the function
        /// given in the token.
        /// </summary>
        /// <param name="function">The parsed <see cref="Token"/> containing the function name.</param>
        /// <returns></returns>
        bool EvaluatesFunction(Token function);

        /// <summary>
        /// Evaluate the function.
        /// </summary>
        /// <param name="function">The parsed <see cref="Token"/> containing the function name.</param>
        /// <param name="parameters">The parsed <see cref="Token"/> containing the list of function
        /// parameters.</param>
        /// <returns>The value as a <see cref="double"/>.</returns>
        double EvaluateFunction(Token function, Token parameters);
    }
}
