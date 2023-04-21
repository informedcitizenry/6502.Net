//-----------------------------------------------------------------------------
// Copyright (c) 2017-2023 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using Antlr4.Runtime;

namespace Sixty502DotNet.Shared;

/// <summary>
/// A <see cref="SymbolBase"/> that implements the <see cref="IScope"/>
/// interface. This is a named scope.
/// </summary>
public class ScopedSymbol : SymbolBase, IScope
{
    private readonly Dictionary<string, SymbolBase> _members;

    /// <summary>
    /// Construct a new instance of the <see cref="ScopedSymbol"/> class from
    /// another <see cref="ScopedSymbol"/>.
    /// </summary>
    /// <param name="other">The other <see cref="ScopedSymbol"/> object.</param>
    /// <param name="isBuiltin">The flag whether this symbol is built-in.</param>
    public ScopedSymbol(ScopedSymbol other, bool isBuiltin = false)
        : base(other.Name, other.EnclosingScope, isBuiltin)
    {
        IsCaseSensitive = other.IsCaseSensitive;
        AnonymousLabels = new(other.AnonymousLabels);
        _members = new(other._members, IsCaseSensitive.ToStringComparer());
    }

    /// <summary>
    /// Construct a new instance of the <see cref="ScopedSymbol"/> class.
    /// </summary>
    /// <param name="name">The symbol's name, which is also the scope name.
    /// </param>
    /// <param name="enclosingScope">The symbol's enclosing <see cref="IScope"/>.
    /// </param>
    /// <param name="builtIn">The flag whether the symbol is built-in.</param>
    public ScopedSymbol(string name, IScope? enclosingScope, bool isBuiltin = false)
        : base(name, enclosingScope, isBuiltin)
    {
        IsCaseSensitive = enclosingScope?.IsCaseSensitive == true;
        AnonymousLabels = new(this);
        _members = new(IsCaseSensitive.ToStringComparer());
    }

    /// <summary>
    /// Construct a new instance of the <see cref="ScopedSymbol"/> from a
    /// <see cref="IToken"/>.
    /// </summary>
    /// <param name="token">The symbol's defining <see cref="IToken"/>.</param>
    /// <param name="enclosingScope">The symbol's enclosing <see cref="IScope"/>.</param>
    /// <param name="builtIn">The flag whether the symbol is built-in.</param>
    public ScopedSymbol(IToken token, IScope? enclosingScope, bool isBuiltin = false)
        : base(token, enclosingScope, isBuiltin)
    {
        IsCaseSensitive = enclosingScope?.IsCaseSensitive == true;
        AnonymousLabels = new(this);
        _members = new(IsCaseSensitive.ToStringComparer());
    }

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
        if (!_members.TryGetValue(name, out SymbolBase? symbol))
        {
            return EnclosingScope?.Lookup(name);
        }
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

    public SymbolBase? ResolveMember(string name)
    {
        _ = _members.TryGetValue(name, out SymbolBase? member);
        return member;
    }

    public void Remove(string name) => _members.Remove(name);

    public IReadOnlyCollection<SymbolBase> GetUnreferencedSymbols()
        => SymbolManager.GetUnreferencedSymbols(_members);

    public bool IsCaseSensitive { get; init; }

    public bool IsProcScope { get; set; }

    public Label? LocalLabel { get; set; }

    public AnonymousLabelCollection AnonymousLabels { get; init; }
}

