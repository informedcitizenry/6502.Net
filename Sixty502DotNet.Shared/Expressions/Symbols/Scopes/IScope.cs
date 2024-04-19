//-----------------------------------------------------------------------------
// Copyright (c) 2017-2024 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using Antlr4.Runtime;

namespace Sixty502DotNet.Shared;

/// <summary>
/// Defines an interface for a symbol scope.
/// </summary>
public interface IScope
{
    /// <summary>
    /// Attempt to look up a symbol by name in this scope. If this scope
    /// is unable to resolve the symbol name, its enclosing scope, if
    /// defined may in turn attempt to resolve it.
    /// </summary>
    /// <param name="name">The name of the symbol to look up</param>
    /// <returns>The <see cref="SymbolBase"/> object with the name, otherwise
    /// <c>null</c> if no such symbol name exists in this scope.</returns>
    SymbolBase? Lookup(string name);

    /// <summary>
    /// Get the scope's name.
    /// </summary>
    void Remove(string name);

    /// <summary>
    /// Define a <see cref="SymbolBase"/> in this scope.
    /// </summary>
    /// <param name="name">The symbol name in its scope.</param>
    /// <param name="symbol">The <see cref="SymbolBase"/> object.</param>
    /// <exception cref="SymbolRedefinitionError"></exception>
    void Define(string name, SymbolBase symbol);

    /// <summary>
    /// Define a <see cref="SymbolBase"/> in this scope from a token.
    /// </summary>
    /// <param name="token">The identifier token.</param>
    /// <param name="symbol">The <see cref="SymbolBase"/> object.</param>
    /// <exception cref="SymbolRedefinitionError"></exception>
    void Define(IToken token, SymbolBase symbol);

    /// <summary>
    /// Perform actions when the scope is deactivating.
    /// </summary>
    void Leave();

    /// <summary>
    /// Get a collection of all unreferenced symbols in this scope.
    /// </summary>
    /// <returns>A read-only collection of all symbols in this scope
    /// that are never referenced elsewhere.</returns>
    IReadOnlyList<SymbolBase> GetSymbols();

    /// <summary>
    /// Get or set the local label that all cheap local labels will be defined
    /// under.
    /// </summary>
    Label? LocalLabel { get; set; }

    /// <summary>
    /// Get or set the scope's enclosing scope.
    /// </summary>
    IScope? EnclosingScope { get; set; }

    /// <summary>
    /// Get a collection of all unreferenced symbols in this scope.
    /// </summary>
    /// <returns>A read-only collection of all symbols in this scope that are
    /// never referenced elsewhere.</returns>
    IReadOnlyCollection<SymbolBase> GetUnreferencedSymbols();

    /// <summary>
    /// Get the flag indicating this scope's symbols should be resolved
    /// with case-sensitivity.
    /// </summary>
    bool IsCaseSensitive { get; }

    /// <summary>
    /// Get the <see cref="AnonymousLabelCollection"/> for this scope.
    /// </summary>
    AnonymousLabelCollection AnonymousLabels { get; }
}

