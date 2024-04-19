//-----------------------------------------------------------------------------
// Copyright (c) 2017-2024 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System.Text;

namespace Sixty502DotNet.Shared;

/// <summary>
/// A class that maintains a reference to all scopes and symbols defined
/// during parsing
/// and assembly.
/// </summary>
public sealed class SymbolManager
{
    private readonly List<Variable> _declaredVariables;

    /// <summary>
    /// Construct a new instance of the <see cref="SymbolManager"/> class.
    /// </summary>
    /// <param name="caseSensitive">Determine if symbol and scope
    /// names are considered case-sensitive.</param>
    public SymbolManager(bool caseSensitive)
        : this(new GlobalScope(caseSensitive))
    {

    }

    /// <summary>
    /// Construct a new instance of the <see cref="SymbolManager"/> class.
    /// </summary>
    /// <param name="globalScope">The
    /// <see cref="Sixty502DotNet.GlobalScope"/> as the initial scope.
    /// </param>
    public SymbolManager(GlobalScope globalScope)
    {
        Scope = GlobalScope = globalScope;
        _declaredVariables = new List<Variable>();
        ImportedScopes = new List<IScope>();
        CallStack = new();
    }

    /// <summary>
    /// Lookup a symbol by name up to the given scope.
    /// </summary>
    /// <param name="name">The name to search.</param>
    /// <returns>The symbol if found, otherwise <c>null</c><./returns>
    public SymbolBase? LookupToScope(string name)
    {
        if (name[0] == '_')
        {
            SymbolBase? localSym = ActiveScope.LocalLabel?.ResolveMember(name);
            if (localSym != null)
            {
                return localSym;
            }
        }
        if (ActiveScope is ScopedSymbol symbolScope)
        {
            return symbolScope.ResolveMember(name);
        }
        return ActiveScope.Lookup(name);
    }

    /// <summary>
    /// Lookup a symbol by name.
    /// </summary>
    /// <param name="name">The symbol name.</param>
    /// <returns>A <see cref="SymbolBase"/> if the symbol is defined within
    /// the current scope, otherwise a <c>null</c> value.</returns>
    public SymbolBase? Lookup(string name)
    {
        if (name[0] == '_')
        {
            SymbolBase? localSym = ActiveScope.LocalLabel?.ResolveMember(name);
            if (localSym != null)
            {
                return localSym;
            }
        }
        return ActiveScope.Lookup(name);
    }

    /// <summary>
    /// Resets the <see cref="SymbolManager"/>, including setting
    /// the current scope to the global socpe.
    /// </summary>
    public void Reset()
    {
        ClearVariables();
        ImportedScopes.Clear();
        Scope.Leave();
        Scope = GlobalScope;
    }

    /// <summary>
    /// Push the scope onto the scope stack and define all subsequent
    /// symbols within the scope.
    /// </summary>
    /// <param name="scope">The <see cref="IScope"/>.</param>
    public void PushScope(IScope scope)
    {
        /*if (scope is Label)
        {
            Scope.LocalLabel = null;
        }*/
        scope.EnclosingScope = Scope;
        Scope = scope;
    }

    /// <summary>
    /// Pop the scope from the scope stack and set the enclosing
    /// scope as the current scope.
    /// </summary>
    /// <returns>The <see cref="IScope"/> popped off the
    /// scope stack.</returns>
    public IScope PopScope()
    {
        var current = Scope;
        Scope = current.EnclosingScope ?? GlobalScope;
        current.LocalLabel = null;
        current.Leave();
        return current;
    }


    /// <summary>
    /// Remove all variables from their scope.
    /// </summary>
    public void ClearVariables()
    {
        foreach (var sym in _declaredVariables)
        {
            sym.EnclosingScope?.Remove(sym.Name);
        }
        _declaredVariables.Clear();
    }

    /// <summary>
    /// Define a symbol in the active scope.
    /// </summary>
    /// <param name="symbol">The symbol to define.</param>
    public void Define(SymbolBase symbol)
    {
        IScope scope = ActiveScope;
        bool isCheapLocal = symbol.Name[0] == '_';
        if (isCheapLocal && scope.LocalLabel != null)
        {
            scope = scope.LocalLabel;
            symbol.EnclosingScope = scope;
        }
        else if (scope is IValueResolver resolver && resolver.Value.Prototype?.Lookup(symbol.Name) != null)
        {
            scope = resolver.Value.Prototype;
        }
        scope.Define(symbol.Name, symbol);
        if (symbol is Label localLabel && !localLabel.DefinesScope && !isCheapLocal)
        {
            scope.LocalLabel = localLabel;
        }
        if (symbol is Variable var && !InFunctionScope)
        {
            _declaredVariables.Add(var);
        }
    }

    private static string GetSymbolListings(IScope scope, bool labelsOnly, bool viceLabels)
    {
        StringBuilder symBuilder = new();
        var scopeSyms = scope.GetSymbols();
        for (int i = 0; i < scopeSyms.Count; i++)
        {
            if (scopeSyms[i].IsBuiltIn || scopeSyms[i] is Variable || scopeSyms[i] is AnonymousScope)
            {
                continue;
            }
            if (scopeSyms[i] is IValueResolver resolver)
            {
                if (viceLabels)
                {
                    if (resolver.Value.IsNumeric)
                    {
                        symBuilder.AppendLine($"al {resolver.Value.AsInt():x} {scopeSyms[i].GetViceName()}");
                    }
                }
                else if (resolver is Label l && l.Value.IsDefined)
                {
                    symBuilder.AppendLine($"{l.FullName,-50}= {l.Value.AsInt():x4} ({l.Value.AsInt()})");
                }
                else if (resolver.Value is not FunctionObject && !labelsOnly)
                {
                    symBuilder.AppendLine($"{scopeSyms[i].FullName,-50}= {resolver.Value}");
                }
            }
            if (scopeSyms[i] is ScopedSymbol scopedSymbol)
            {
                symBuilder.Append(GetSymbolListings(scopedSymbol, labelsOnly, viceLabels));
            }
        }
        return symBuilder.ToString();
    }

    /// <summary>
    /// Gets a listing of all defined value-type symbols, excluding built-in
    /// symbols in VICE format.
    /// </summary>
    /// <returns>The label listing in VICE format.</returns>
    public string GetViceSymbolListing()
    {
        return GetSymbolListings(GlobalScope, false, true);
    }

    /// <summary>
    /// Gets a listing of all defined value-type symbols, excluding built-in
    /// symbols.
    /// </summary>
    /// <param name="labelsOnly">Only get labels.</param>
    /// <returns>The symbol listing.</returns>
    public string GetSymbolListing(bool labelsOnly)
    {
        return GetSymbolListings(GlobalScope, labelsOnly, false);
    }

    /// <summary>
    /// Determines whether the specified scope is in the current active scope
    /// stack.
    /// </summary>
    /// <param name="scope">The scope to test.</param>
    /// <returns><c>true</c> if the scope is part of the active scope stack,
    /// <c>false</c> othwerise.</returns>
    public bool ScopeIsActive(IScope? scope)
    {
        IScope? active = ActiveScope;
        while (active != null)
        {
            if (ReferenceEquals(active, scope))
            {
                return true;
            }
            active = active.EnclosingScope;
        }
        return false;
    }

    /// <summary>
    /// Get a collection of all unreferenced symbols in the
    /// <see cref="GlobalScope"/> and all its subscopes.
    /// </summary>
    /// <returns>The list of unreferenced symbols.</returns>
    public IReadOnlyCollection<SymbolBase> GetUnreferencedSymbols()
        => GlobalScope.GetUnreferencedSymbols(); 

    /// <summary>
    /// Get a collection of all unreferenced symbols in a dictionary.
    /// </summary>
    /// <param name="symbols">The dictionary of unreferenced symbols.</param>
    /// <returns>The list of unreferenced symbols, excluding <see cref="AnonymousScope"/>
    /// types.</returns>
    public static IReadOnlyCollection<SymbolBase> GetUnreferencedSymbols(IDictionary<string, SymbolBase> symbols)
    {
        List<SymbolBase> unreferenced = new();
        foreach (KeyValuePair<string, SymbolBase> sym in symbols)
        {
            if (sym.Value.IsBuiltIn) continue;
            bool isAnonymous = sym.Value is AnonymousScope;
            if (!sym.Value.IsReferenced && !isAnonymous)
            {
                unreferenced.Add(sym.Value);
            }
            else if (sym.Value is IScope scope)
            {
                unreferenced.AddRange(scope.GetUnreferencedSymbols());
            }
        }
        return unreferenced.AsReadOnly();
    }

    /// <summary>
    /// Get the root <see cref="GlobalScope"/>.
    /// </summary>
    public GlobalScope GlobalScope { get; init; }

    /// <summary>
    /// Get or set the current scope.
    /// </summary>
    public IScope Scope { get; set; }

    /// <summary>
    /// Get the scopes imported for symbol resolution.
    /// </summary>
    public List<IScope> ImportedScopes { get; init; }

    /// <summary>
    /// Get the stack of current active function scopes.
    /// </summary>
    public Stack<IScope> CallStack { get; init; }

    /// <summary>
    /// Get the current active scope.
    /// </summary>
    public IScope ActiveScope
    {
        get
        {
            if (CallStack.TryPeek(out IScope? fcnScope))
            {
                return fcnScope;
            }
            return Scope;
        }
    }

    public bool InFunctionScope => CallStack.Count > 0;
}