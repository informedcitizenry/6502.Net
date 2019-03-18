//-----------------------------------------------------------------------------
// Copyright (c) 2017-2019 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------
using System;

namespace DotNetAsm
{
    /// <summary>
    /// A discrete unit in a mathematical expression, representing an 
    /// operand, operator or function
    /// </summary>
    public struct ExpressionElement : IEquatable<ExpressionElement>
    {
        /// <summary>
        /// The element type
        /// </summary>
        public enum Type
        {
            Group = 0,
            Operand,
            Function,
            Operator          
        };

        /// <summary>
        /// The element subtype
        /// </summary>
        public enum Subtype
        {
            Open = 0,
            Close,
            Binary,
            Unary,
            None
        };

        /// <summary>
        /// The symbol or value of the element.
        /// </summary>
        public string word;

        /// <summary>
        /// The element's type.
        /// </summary>
        public Type type;

        /// <summary>
        /// The element's subtype.
        /// </summary>
        public Subtype subtype;

        /// <summary>
        /// The integral flag. Set to true if the element's value should
        /// only be an integer.
        /// </summary>
        public bool integral;

        public override string ToString() => string.Format("{0} [{1}.{2}]", word, type, subtype);

        public override bool Equals(object obj) => Equals(obj);

        public override int GetHashCode() => word.GetHashCode() | 
                                             type.GetHashCode() | 
                                             subtype.GetHashCode();

        public static bool operator ==(ExpressionElement lhs, ExpressionElement rhs) 
                                        => lhs.Equals(rhs);

        public static bool operator !=(ExpressionElement lhs, ExpressionElement rhs) 
                                        => !lhs.Equals(rhs);

        /// <summary>
        /// Determines whether the specified <see cref="DotNetAsm.ExpressionElement"/> is 
        /// equal to the current <see cref="T:DotNetAsm.ExpressionElement"/>.
        /// </summary>
        /// <param name="other">The <see cref="DotNetAsm.ExpressionElement"/> to compare with 
        /// the current <see cref="T:DotNetAsm.ExpressionElement"/>.</param>
        /// <returns><c>true</c> if the specified <see cref="DotNetAsm.ExpressionElement"/> is equal to the current
        /// <see cref="T:DotNetAsm.ExpressionElement"/>; otherwise, <c>false</c>.</returns>
        public bool Equals(ExpressionElement other) => word.Equals(other.word) &&
                                                       type == other.type &&
                                                       subtype == other.subtype;
    }
}
