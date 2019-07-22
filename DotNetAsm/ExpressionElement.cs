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
        #region Enums

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
        /// The Arithmetic type for the operation.
        /// </summary>
        public enum ArithmeticType
        {
            Float = 0,
            Integral,
            Boolean
        };

        #endregion

        #region Members

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
        public Subtype subType;

        /// <summary>
        /// The type of the arithmetic operation.
        /// </summary>
        public ArithmeticType arithmeticType;

        #endregion

        #region Methods

        #region Override Methods

        public override string ToString() => string.Format("{0} [{1}.{2}]", word, type, subType);

        public override bool Equals(object obj) => obj is ExpressionElement && this == (ExpressionElement)obj;

        public override int GetHashCode() => word.GetHashCode() |
                                             type.GetHashCode() |
                                             subType.GetHashCode();

        public static bool operator ==(ExpressionElement lhs, ExpressionElement rhs)
                                        => lhs.word.Equals(rhs.word) &&
                                           lhs.type == rhs.type &&
                                           lhs.subType == rhs.subType;

        public static bool operator !=(ExpressionElement lhs, ExpressionElement rhs)
                                        => !lhs.word.Equals(rhs.word) ||
                                            lhs.type != rhs.type ||
                                            lhs.subType != rhs.subType;

        #endregion

        #region IEquitable Methods

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
                                                       subType == other.subType;

        #endregion

        #endregion
    }
}
