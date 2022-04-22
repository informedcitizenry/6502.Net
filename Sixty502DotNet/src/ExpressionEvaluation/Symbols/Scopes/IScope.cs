//-----------------------------------------------------------------------------
// Copyright (c) 2017-2022 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System.Collections.Generic;

namespace Sixty502DotNet
{
    /// <summary>
    /// Defines an interface for a symbol scope.
    /// </summary>
    public interface IScope
    {
        /// <summary>
        /// Define a <see cref="SymbolBase"/> in this scope.
        /// </summary>
        /// <param name="name">The symbol name in its scope.</param>
        /// <param name="symbol">The <see cref="SymbolBase"/> object.</param>
        /// <exception cref="Error"></exception>
        void Define(string name, SymbolBase symbol);

        /// <summary>
        /// Attempt to resolve a symbol by name in this scope. If this scope
        /// is unable to resolve the symbol name, its enclosing scope, if
        /// defined may in turn attempt to resolve it.
        /// </summary>
        /// <param name="name">The name of the symbol to resolve</param>
        /// <returns>The <see cref="SymbolBase"/> object with the name, or
        /// <c>null</c> if no such symbol name exists in this scope.</returns>
        SymbolBase? Resolve(string name);

        /// <summary>
        /// Remove a symbol from this scope.
        /// </summary>
        /// <param name="name">The symbol name.</param>
        void Remove(string name);

        /// <summary>
        /// Attempt to resolve a <see cref="AnonymousLabel"/> in this scope.
        /// </summary>
        /// <param name="atIndex">The index at which to resolve the anonymous
        /// label. Note this index is the one at which the reference
        /// is defined and refers to the label token's index
        /// in the parsed source.</param>
        /// <returns>The <see cref="AnonymousLabel"/> at the index, or
        /// <c>null</c> if no such label exists at the index.
        /// </returns>
        AnonymousLabel? ResolveAnonymousLabel(int atIndex);

        /// <summary>
        /// Attempt to resolve a <see cref="AnonymousLabel"/> in this scope.
        /// </summary>
        /// <param name="name">The name of the line reference, which is a series
        /// of one or more <c>+</c> or <c>-</c> characters.</param>
        /// <param name="fromIndex">The index from which to search for the
        /// anonymous label, either backward or forward. Note this index
        /// is the index of a token in the parsed source.</param>
        /// <returns>The <see cref="AnonymousLabel"/>, or
        /// <c>null</c> if no such label exists.
        /// </returns>
        AnonymousLabel? ResolveAnonymousLabel(string name, int fromIndex);

        /// <summary>
        /// Get the scope's name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Get or set the scope's enclosing scope.
        /// </summary>
        IScope? EnclosingScope { get; set; }

        /// <summary>
        /// Get a collection of all unreferenced symbols in this scope.
        /// </summary>
        /// <returns>An <see cref="IReadOnlyCollection{Symbol}"/> of all
        /// symbols in this scope that are never referenced elsewhere.</returns>
        IReadOnlyCollection<SymbolBase> GetUnreferencedSymbols();

        /// <summary>
        /// Get the flag indicating this scope is in a function call scope.
        /// </summary>
        bool InFunctionScope { get; }

        /// <summary>
        /// Get the flag indicating this scope's symbols should be resolved
        /// with case-sensitivity.
        /// </summary>
        bool IsCaseSensitive { get; }
    }
}

