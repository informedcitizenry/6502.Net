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
    /// A class representing a named symbol defined within a scope. This class
    /// must be inherited.
    /// </summary>
    public abstract class SymbolBase
    {
        /// <summary>
        /// Construct a new instance of the <see cref="SymbolBase"/> class.
        /// </summary>
        /// <param name="name">The symbol's name.</param>
        protected SymbolBase(string name)
            => (Name, IsReferenced) = (name, true);

        /// <summary>
        /// Get the symbol's name.
        /// </summary>
        public string Name { get; init; }

        /// <summary>
        /// Get or set the symbol's current scope.
        /// </summary>
        public IScope? Scope { get; set; }

        public override string ToString()
        {
            var fqdn = new List<string> { Name };
            var enclosing = Scope;
            while (enclosing != null && enclosing is not GlobalScope)
            {
                if (enclosing is AnonymousScope)
                {
                    fqdn.Insert(0, "::");
                }
                else
                {
                    fqdn.Insert(0, enclosing.Name);
                }
                enclosing = enclosing.EnclosingScope;
            }
            return string.Join('.', fqdn);
        }

        /// <summary>
        /// Get or set the <see cref="Token"/> in the parsed source that
        /// defines the symbol.
        /// </summary>
        public Token? Token { get; set; }

        /// <summary>
        /// Get or set the flag indicating this symbol is referenced from
        /// some other parsed source.
        /// </summary>
        public bool IsReferenced { get; set; }

        /// <summary>
        /// Get or set the parsed source statement where the symbol is defined.
        /// </summary>
        public Sixty502DotNetParser.StatContext? DefinedAt { get; set; }
    }
}
