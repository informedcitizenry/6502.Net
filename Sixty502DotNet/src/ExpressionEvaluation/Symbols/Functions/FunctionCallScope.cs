//-----------------------------------------------------------------------------
// Copyright (c) 2017-2022 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

namespace Sixty502DotNet
{
    /// <summary>
    /// A class for managing function call scopes, simulating the call stack of
    /// a traditional programming language. When a user-defined function
    /// defined with <c>.function</c> is called, an instance of this class can
    /// be used as a scope for local variables.
    /// </summary>
    public class FunctionCallScope : NamedMemberSymbol
    {
        /// <summary>
        /// Construct a new instance of the <see cref="FunctionCallScope"/>.
        /// </summary>
        /// <param name="name">The scope (function) name.</param>
        /// <param name="enclosingScope">The enclosing scope.</param>
        public FunctionCallScope(string name, IScope enclosingScope)
            : base($"{name}()", enclosingScope) { }


        public override SymbolBase? Resolve(string name)
        {
            var sym = base.ResolveMember(name);
            if (sym == null)
            {
                sym = base.Resolve(name);
            }
            return sym;
        }

        public override bool InFunctionScope => true;
    }
}
