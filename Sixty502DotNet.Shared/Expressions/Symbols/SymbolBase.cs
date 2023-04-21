//-----------------------------------------------------------------------------
// Copyright (c) 2017-2023 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using Antlr4.Runtime;

namespace Sixty502DotNet.Shared;

/// <summary>
/// A class representing a named symbol defined within a scope. This class
/// must be inherited.
/// </summary>
public class SymbolBase
{
    private bool _isReferenced;

    /// <summary>
    /// Construct a new instance of a <see cref="SymbolBase"/> from a token.
    /// </summary>
    /// <param name="token">The symbol's defining <see cref="IToken"/>.</param>
    /// <param name="enclosingScope">The symbol's enclosing <see cref="IScope"/>.</param>
    /// <param name="builtIn">The flag whether the symbol is built-in.</param>
    public SymbolBase(IToken token, IScope? enclosingScope, bool builtIn = false)
        : this(token.Text, enclosingScope, builtIn)
    {
        _isReferenced = false;
        Token = token;
    }

    /// <summary>
    /// Construct a new instance of a <see cref="SymbolBase"/>.
    /// </summary>
    /// <param name="name">The symbol's name, unique in its scope.</param>
    /// <param name="enclosingScope">The symbol's enclosing <see cref="IScope"/>.</param>
    /// <param name="builtIn">The flag whether the symbol is built-in.</param>
    public SymbolBase(string name, IScope? enclosingScope, bool builtIn = false)
    {
        Name = name;
        _isReferenced = false;
        EnclosingScope = enclosingScope;
        if (enclosingScope is ScopedSymbol scoped)
        {
            InProcScope = scoped.InProcScope;
        }
        else
        {
            InProcScope = false;
        }
        IsBuiltIn = builtIn;
    }

    public override string ToString() => FullName;

    /// <summary>
    /// Get the symbol's VICE name.
    /// </summary>
    /// <returns>The symbol name conforming to VICE label format.</returns>
    public string GetViceName()
    {
        ScopedSymbol? parent = EnclosingScope as ScopedSymbol;
        List<string> ancestors = new()
        {
            Name
        };
        while (parent != null)
        {
            ancestors.Insert(0, parent.Name);
            parent = parent.EnclosingScope as ScopedSymbol;
        }
        return $".{string.Join(':', ancestors)}";
    }

    /// <summary>
    /// Get the symbol's fully qualified domain name, including its ancestor
    /// scopes.
    /// </summary>
    public string FullName
    {
        get
        {
            ScopedSymbol? scope = EnclosingScope as ScopedSymbol;
            List<string> scopes = new();
            while (scope != null)
            {
                scopes.Insert(0, scope.Name);
                scope = scope.EnclosingScope as ScopedSymbol;
            }
            if (scopes.Count > 0)
            {
                scopes.Add(Name);
                return string.Join('.', scopes);
            }
            return Name;
        }
    }

    /// <summary>
    /// Get the symbol's name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Get the symbol's defining token, if it was defined by a token.
    /// </summary>
    public IToken? Token { get; }

    /// <summary>
    /// Get whether the symbol is built-in.
    /// </summary>
    public bool IsBuiltIn { get; }

    /// <summary>
    /// Get or set whether the symbol is referenced from elsewhere in source.
    /// </summary>
    public bool IsReferenced
    {
        get => _isReferenced;
        set
        {
            _isReferenced = value;
            if (value && EnclosingScope is ScopedSymbol ss)
            {
                ss.IsReferenced = value;
            }
        }
    }

    /// <summary>
    /// Get whether the symbol exists inside a <c>.proc</c> scope.
    /// </summary>
    public bool InProcScope { get; set; }

    /// <summary>
    /// Get or set the symbol's enclosing <see cref="IScope"/>.
    /// </summary>
    public IScope? EnclosingScope { get; set; }
}

