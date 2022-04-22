//-----------------------------------------------------------------------------
// Copyright (c) 2017-2022 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

namespace Sixty502DotNet
{
    /// <summary>
    /// A named <see cref="SymbolBase"/> object and value resolver whose value
    /// can be mutated.
    /// </summary>
    public sealed class Variable : SymbolBase, IValueResolver
    {
        /// <summary>
        /// Construct a new instance of the <see cref="Variable"/> class.
        /// </summary>
        /// <param name="name">The variable name.</param>
        public Variable(string name)
            : this(name, new Value()) => IsReferenced = false;

        /// <summary>
        /// Construct a new instance of the <see cref="Variable"/> class.
        /// </summary>
        /// <param name="name">The variable name.</param>
        /// <param name="value">The variable's initial value.</param>
        public Variable(string name, Value value)
            : base(name) => Value = value;

        public Value Value { get; set; }

        public bool IsConst => false;

        public IValueResolver? IsAReferenceTo { get; set; }
    }
}
