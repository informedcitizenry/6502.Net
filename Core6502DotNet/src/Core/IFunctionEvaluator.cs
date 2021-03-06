﻿//-----------------------------------------------------------------------------
// Copyright (c) 2017-2021 informedcitizenry <informedcitizenry@gmail.com>
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
        /// <returns><c>true</c> if the evaluator does evaluate the function, otherwise <c>false</c>.</returns>
        bool EvaluatesFunction(Token function);

        /// <summary>
        /// Invoke the function with the given parameters, and return the value returned
        /// from the function.
        /// </summary>
        /// <param name="tokens">The collection of tokens that comprise the function call, including parameters.</param>
        /// <returns>The value as a <see cref="double"/>.</returns>
        double EvaluateFunction(RandomAccessIterator<Token> tokens);

        /// <summary>
        /// Invoke the function with the given parameters.
        /// </summary>
        /// <param name="tokens">The collection of tokens that comprise the function call, including parameters.</param>
        void InvokeFunction(RandomAccessIterator<Token> tokens);

        /// <summary>
        /// Determines if the given symbol is a named function the evaluator evaluates.
        /// </summary>
        /// <param name="symbol">The symbol name.</param>
        /// <returns><c>true</c> if the symbol is a function name, <c>false</c> otherwise.</returns>
        bool IsFunctionName(StringView symbol);
    }
}
