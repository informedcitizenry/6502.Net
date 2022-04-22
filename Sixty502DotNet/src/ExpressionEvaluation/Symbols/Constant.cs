//-----------------------------------------------------------------------------
// Copyright (c) 2017-2022 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

namespace Sixty502DotNet
{
    /// <summary>
    /// A symbol that resolves to a constant value that can only be defined once.
    /// </summary>
    public sealed class Constant : SymbolBase, IValueResolver
    {
        /// <summary>
        /// Construct a new instance of the <see cref="Constant"/> class.
        /// </summary>
        /// <param name="name">The constant symbol name.</param>
        /// <param name="value">The constant's value.</param>
        public Constant(string name, Value value)
            : base(name) => Value = value;

        public Value Value { get; set; }

        /// <summary>
        /// Get or set the right-hand side expression of the assignment. 
        /// </summary>
        public Sixty502DotNetParser.ExprContext? Expression { get; set; }

        public bool IsConst
        {
            get
            {
                try
                {
                    return (Expression != null &&
                    Evaluator.GetPrimaryExpression(Expression).IsDefined) ||
                    (Expression == null && Value.IsDefined) ||
                    Scope is Enum;
                }
                catch
                {
                    return false;
                }
            }
        }

        public IValueResolver? IsAReferenceTo { get; set; }
    }
}
