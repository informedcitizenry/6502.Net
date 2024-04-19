//-----------------------------------------------------------------------------
// Copyright (c) 2017-2024 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using Antlr4.Runtime;

namespace Sixty502DotNet.Shared;

/// <summary>
/// A concrete implementation of a <see cref="IScope"/> that respresents a
/// global unnamed scope.
/// </summary>
public sealed class GlobalScope : IScope
{
    private readonly Dictionary<string, SymbolBase> _members;

    /// <summary>
    /// Construct a new instance of a <see cref="GlobalScope"/> object.
    /// </summary>
    /// <param name="caseSensitive">Specify if all symbols in the scope are
    /// case-sensitive. This value will cascade down to all child scopes.</param>
    public GlobalScope(bool caseSensitive)
    {
        IsCaseSensitive = caseSensitive;
        EnclosingScope = null;
        _members = new(caseSensitive.ToStringComparer());
        AnonymousLabels = new(this);
    }

    public IScope? EnclosingScope { get; set; }

    public void Define(string name, SymbolBase symbol)
    {
        if (name.Length == 1 && name[0] == '_')
        {
            return;
        }
        if (!_members.TryAdd(name, symbol))
        {
            throw new Exception($"Redefinition of symbol '{name}'");
        }
    }

    public void Define(IToken token, SymbolBase symbol)
    {
        if (token.Text.Length == 1 && token.Text[0] == '_')
        {
            return;
        }
        if (!_members.TryAdd(token.Text, symbol))
        {
            throw new SymbolRedefinitionError(token, symbol.Token);
        }
    }

    public SymbolBase? Lookup(string name)
    {
        _ = _members.TryGetValue(name, out SymbolBase? symbol);
        return symbol;
    }

    public void Leave()
    {
        LocalLabel = null;
    }

    public IReadOnlyList<SymbolBase> GetSymbols()
    {
        return _members.Values.ToList().AsReadOnly();
    }

    public void Remove(string name) => _members.Remove(name);

    public IReadOnlyCollection<SymbolBase> GetUnreferencedSymbols()
        => SymbolManager.GetUnreferencedSymbols(_members);

    public bool IsCaseSensitive { get; }

    public Label? LocalLabel { get; set; }

    public AnonymousLabelCollection AnonymousLabels { get; }

}
