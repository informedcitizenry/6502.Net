//-----------------------------------------------------------------------------
// Copyright (c) 2017-2022 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

namespace Sixty502DotNet
{
    /// <summary>
    /// A class representing a label, which is a named reference to a statement
    /// of code. A label is a scoped symbol that resolves to a value, typically
    /// an address.
    /// </summary>
    public sealed class Label : NamedMemberSymbol, IValueResolver
    {
        /// <summary>
        /// Determines if the <see cref="SymbolBase"/> is a cheap local, and should
        /// therefore but under the scope of a label. Cheap local symbols have
        /// names that begin with an underscore <c>_</c> character.
        /// </summary>
        /// <param name="symbol">The symbol.</param>
        /// <returns><c>true</c> if the symbol is a cheap local, <c>false</c>
        /// otherwise.</returns>
        public static bool IsCheapLocal(SymbolBase symbol)
        {
            if (symbol is Variable || symbol is Label || symbol is Constant)
            {
                return IsCheapLocal(symbol.Name);
            }
            return false;
        }

        /// <summary>
        /// Determines if the <see cref="SymbolBase"/> is a cheap local, and should
        /// therefore but under the scope of a label. Cheap local symbols have
        /// names that begin with an underscore <c>_</c> character.
        /// </summary>
        /// <param name="name">The symbol name.</param>
        /// <returns><c>true</c> if the name represents a symbol that is a cheap
        /// local, <c>false</c> otherwise.</returns>
        public static bool IsCheapLocal(string name)
            => !char.IsLetter(name[0]);

        /// <summary>
        /// Construct a new instance of the <see cref="Label"/> class.
        /// </summary>
        /// <param name="name">The label name.</param>
        /// <param name="parent">The label's parent scope.</param>
        /// <param name="definedAt">The parsed source statement at which the
        /// label is defined.</param>
        public Label(string name, IScope? parent, Sixty502DotNetParser.StatContext definedAt)
            : base(name, parent)
        {
            Value = Value.Undefined();
            DefinedAt = definedAt;
            IsBlockScope = false;
            Bank = -1;
        }

        public override void Define(string name, SymbolBase symbol)
        {
            if (IsCheapLocal(symbol))
            {
                Members.Add(name, symbol);
                name = $"{Name}{name}";
                EnclosingScope?.Define(name, symbol);
                symbol.Scope = this;
                return;
            }
            base.Define(name, symbol);
        }

        /// <summary>
        /// Get or set the flag that indicates whether the label also is a
        /// block scope.
        /// </summary>
        public bool IsBlockScope { get; set; }

        /// <summary>
        /// Gets or sets the addressing bank.
        /// </summary>
        public int Bank { get; set; }

        public Value Value { get; set; }

        public bool IsConst => EnclosingScope is Enum;

        public IValueResolver? IsAReferenceTo { get; set; }
    }
}
