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
    /// Defines a scoping symbol that contains member <see cref="SymbolBase"/>
    /// objects. This class must be inherited.
    /// </summary>
    public abstract class NamedMemberSymbol : SymbolBase, IScope
    {
        private readonly AnonymousLabelCollection _anonymousLabels;

        /// <summary>
        /// Constructs a new instance of the <see cref="NamedMemberSymbol"/>
        /// class.
        /// </summary>
        /// <param name="name">The symbol's name.</param>
        /// <param name="enclosingScope">The symbol's enclosing scope.</param>
        public NamedMemberSymbol(string name, IScope? enclosingScope)
            : base(name)
        {
            InFunctionScope = enclosingScope?.InFunctionScope == true;
            IsCaseSensitive = enclosingScope?.IsCaseSensitive == true;
            EnclosingScope = enclosingScope;
            _anonymousLabels = new AnonymousLabelCollection(this);
            var comparer = IsCaseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
            Members = new Dictionary<string, SymbolBase>(comparer);
            IsReferenced = false;
        }

        /// <summary>
        /// Attempt to resolve a member <see cref="SymbolBase"/> by its name. The
        /// symbol attempting to be resolved will only be one within this
        /// symbol's scope.
        /// </summary>
        /// <param name="name">The symbol name.</param>
        /// <returns>The symbol resolved by the name if successfully resolved,
        /// otherwise <c>null</c>.</returns>
        public virtual SymbolBase? ResolveMember(string name)
        {
            if (Members.TryGetValue(name, out var sym))
            {
                return sym;
            }
            return null;
        }

        public virtual void Define(string name, SymbolBase symbol)
        {
            if (symbol is AnonymousLabel lineRef)
            {
                _anonymousLabels.Define(lineRef);
            }
            else
            {
                try
                {
                    Members.Add(name, symbol);
                }
                catch
                {
                    throw new Error(string.Format(Errors.SymbolExistsError, name));
                }
            }
            symbol.Scope = this;
        }

        public virtual SymbolBase? Resolve(string name)
        {
            if (Members.TryGetValue(name, out SymbolBase? sym))
            {
                return sym;
            }
            return EnclosingScope?.Resolve(name);
        }

        public AnonymousLabel? ResolveAnonymousLabel(int atIndex)
            => _anonymousLabels.Resolve(atIndex);


        public AnonymousLabel? ResolveAnonymousLabel(string name, int fromIndex)
            => _anonymousLabels.Resolve(name, fromIndex);

        public IReadOnlyCollection<SymbolBase> GetUnreferencedSymbols()
        {
            var syms = new List<SymbolBase>();
            foreach (var sym in Members)
            {
                if (!sym.Value.IsReferenced)
                    syms.Add(sym.Value);
                if (sym.Value is IScope scope)
                    syms.AddRange(scope.GetUnreferencedSymbols());
            }
            syms.AddRange(_anonymousLabels.GetUnreferencedSymbols());
            return syms.AsReadOnly();
        }

        public void Remove(string name) => Members.Remove(name);

        public bool IsCaseSensitive { get; init; }

        public IScope? EnclosingScope { get; set; }

        public virtual bool InFunctionScope { get; init; }

        /// <summary>
        /// Gets the member symbols as a dictionary.
        /// </summary>
        public IDictionary<string, SymbolBase> Members { get; init; }
    }
}
