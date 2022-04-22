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
    /// A partial implementation of an <see cref="IScope"/> interface. This
    /// class must be inherited.
    /// </summary>
    public abstract class BaseScope : IScope
    {
        private readonly AnonymousLabelCollection _anonymousLabels;
        private readonly Dictionary<string, SymbolBase> _symbols;

        /// <summary>
        /// Construct a new instance of a <see cref="BaseScope"/> class.
        /// </summary>
        /// <param name="parent">The parent <see cref="IScope"/>.</param>
        /// <param name="caseSensitive">The case-sensitivity.</param>
        protected BaseScope(IScope? parent, bool caseSensitive)
        {
            Name = string.Empty;
            EnclosingScope = parent;
            IsCaseSensitive = caseSensitive;
            var comparer = caseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
            _symbols = new Dictionary<string, SymbolBase>(comparer);
            _anonymousLabels = new AnonymousLabelCollection(this);
        }

        public string Name { get; init; }

        public IScope? EnclosingScope { get; set; }

        public void Define(string name, SymbolBase symbol)
        {
            if (symbol is AnonymousLabel anonymousLabel)
            {
                _anonymousLabels.Define(anonymousLabel);
            }
            else
            {
                try
                {
                    _symbols.Add(name, symbol);
                }
                catch
                {
                    throw new Error(string.Format(Errors.SymbolExistsError, name));
                }
            }
            symbol.Scope = this;
        }

        public SymbolBase? Resolve(string name)
        {
            if (_symbols.TryGetValue(name, out var sym))
            {
                return sym;
            }
            return EnclosingScope?.Resolve(name);
        }

        public void Remove(string name)
        {
            _symbols.Remove(name);
        }

        public IReadOnlyCollection<SymbolBase> GetUnreferencedSymbols()
        {
            var syms = new List<SymbolBase>();
            foreach (var sym in _symbols)
            {
                if (!sym.Value.IsReferenced)
                    syms.Add(sym.Value);
                if (sym.Value is IScope scope)
                    syms.AddRange(scope.GetUnreferencedSymbols());
            }
            syms.AddRange(_anonymousLabels.GetUnreferencedSymbols());
            return syms.AsReadOnly();
        }

        public AnonymousLabel? ResolveAnonymousLabel(int atIndex)
            => _anonymousLabels.Resolve(atIndex);


        public AnonymousLabel? ResolveAnonymousLabel(string name, int fromIndex)
            => _anonymousLabels.Resolve(name, fromIndex);

        public bool IsCaseSensitive { get; }

        public bool InFunctionScope => false;
    }
}
