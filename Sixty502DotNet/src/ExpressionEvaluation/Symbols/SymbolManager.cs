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
    /// A class that maintains a reference to all scopes and symbols defined
    /// during parsing
    /// and assembly.
    /// </summary>
    public class SymbolManager
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
        }

        /// <summary>
        /// Lookup a symbol by name up to the given scope.
        /// </summary>
        /// <param name="name">The name to search.</param>
        /// <returns>The symbol if found, otherwise <c>null</c><./returns>
        public SymbolBase? LookupToScope(string name)
        {
            if (Scope is NamedMemberSymbol symbolScope)
            {
                return symbolScope.ResolveMember(name);
            }
            return Scope.Resolve(name);
        }

        /// <summary>
        /// Determines if the symbol assignment is legal.
        /// </summary>
        /// <param name="name">The symbol name.</param>
        /// <param name="variableAssignment">The assignment operation is on a
        /// variable.</param>
        /// <returns><c>true</c> if the assignment is legal, <c>false</c>
        /// otherwise.</returns>
        public bool SymbolAssignmentIsLegal(string name, bool variableAssignment)
        {
            /* Check whether the assignment we are doing matches the symbol 
             * we found, so assuming these symbols are defined as 'myvar' a variable and 'myconst'
             * a constant:
             * 
             * myvar := 3 // good
             * myconst = 3 // also good
             * 
             * but:
             * 
             * myvar = 3 // error
             * .let myconst = 3 // also error
             * myenum = 3 // very bad
             * myfunction := 42 // even worse
             */
            return SymbolAssignmentIsLegal(Scope.Resolve(name), variableAssignment);
        }

        /// <summary>
        /// Determines if the symbol assignment is legal.
        /// </summary>
        /// <param name="symbol">The <see cref="SymbolBase"/> object.</param>
        /// <param name="variableAssignment">The assignment operation is on a
        /// variable.</param>
        /// <returns><c>true</c> if the assignment is legal, <c>false</c>
        /// otherwise.</returns>
        public static bool SymbolAssignmentIsLegal(SymbolBase? symbol, bool variableAssignment)
            => symbol == null || (variableAssignment && symbol is Variable) ||
                   (!variableAssignment && symbol is Constant);

        /// <summary>
        /// Resets the <see cref="SymbolManager"/>, including setting
        /// the current scope to the global socpe.
        /// </summary>
        public void Reset()
        {
            ClearVariables();
            ImportedScopes.Clear();
            Scope = GlobalScope;
        }

        /// <summary>
        /// Push the scope onto the scope stack and define all subsequent
        /// symbols within the scope.
        /// </summary>
        /// <param name="scope">The <see cref="IScope"/>.</param>
        public void PushScope(IScope scope)
        {
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
            return current;
        }

        /// <summary>
        /// Pop the local <see cref="Label"/> scope off the stack.
        /// </summary>
        /// <returns>The scope, if it is a label, otherwise
        /// <c>null</c>.</returns>
        public IScope? PopLocalLabel()
        {
            if (Scope is Label l && !l.IsBlockScope)
            {
                return PopScope();
            }
            return null;
        }

        /// <summary>
        /// Determines if the passed <see cref="SymbolBase"/> is as a block
        /// scope label or namespace.
        /// </summary>
        /// <param name="symbol">The symbol.</param>
        /// <returns><c>true</c> if the symbol is a block scope label or
        /// a namespace, <c>false</c> otherwise.</returns>
        public static bool SymbolIsAScope(SymbolBase? symbol)
        {
            if (symbol != null)
            {
                return (symbol is Label l && l.IsBlockScope) || symbol is Namespace;
            }
            return false;
        }

        /// <summary>
        /// Track a variable definition for later removal during
        /// passes.
        /// </summary>
        /// <param name="variable">The <see cref="Variable"/>
        /// object.</param>
        public void DeclareVariable(Variable variable)
            => _declaredVariables.Add(variable);

        /// <summary>
        /// Remove all variables from their scope.
        /// </summary>
        public void ClearVariables()
        {
            foreach (var sym in _declaredVariables)
            {
                sym.Scope?.Remove(sym.Name);
            }
            _declaredVariables.Clear();
        }

        /// <summary>
        /// Get a collection of all unreferenced symbols.
        /// </summary>
        /// <returns>The list of unreferenced symbols.</returns>
        public IReadOnlyCollection<SymbolBase> GetUnreferencedSymbols()
            => GlobalScope.GetUnreferencedSymbols();

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
    }
}
