//-----------------------------------------------------------------------------
// Copyright (c) 2017-2021 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;

namespace Core6502DotNet
{
    /// <summary>
    /// An enumeration of the symbol data type.
    /// </summary>
    public enum DataType
    {
        None = 0,
        Address,
        Numeric,
        String,
        Boolean
    };

    /// <summary>
    /// An enumeration of symbol storage types.
    /// </summary>
    public enum StorageType
    {
        Scalar,
        Vector,
        Hash,
        NonScalarVector,
        NonScalarHash
    };

    /// <summary>
    /// Represents a symbol with value and storage type information.
    /// </summary>
    public class Symbol : IEquatable<Symbol>, IComparable<Symbol>
    {
        #region Constructors

        /// <summary>
        /// Construct a new instance of a symbol class.
        /// </summary>
        Symbol()
        {
            IsMutable = false;
            NumericVector = new Dictionary<int, double>();
            StringVector = new Dictionary<int, StringView>();
            Bank = 0;
        }

        /// <summary>
        /// Construct a new instanc eof a symbol class.
        /// </summary>
        /// <param name="value">The symbol's value.</param>
        public Symbol(double value)
            : this(value, false)
        {

        }

        /// <summary>
        ///  Construct a new instanc eof a symbol class.
        /// </summary>
        /// <param name="value">The symbol's value.</param>
        /// <param name="isMutable">The symbol's mutability flag.</param>
        public Symbol(double value, bool isMutable)
            : this()
        {
            IsMutable = isMutable;
            NumericValue = value;
            DataType = DataType.Numeric;
            StorageType = StorageType.Scalar;
        }

        /// <summary>
        /// Construct a new instance of a symbol class.
        /// </summary>
        /// <param name="value">The symbol's value.</param>
        /// <param name="bank">The address bank it is defined at.</param>
        public Symbol(double value, int bank)
            : this()
        {
            NumericValue = value;
            DataType = DataType.Address;
            StorageType = StorageType.Scalar;
            Bank = bank;
            IsMutable = false;
        }

        /// <summary>
        /// Construct a enw instance of a symbol class.
        /// </summary>
        /// <param name="value">The symbol's value.</param>
        public Symbol(StringView value)
            : this(value, false)
        {
        }

        /// <summary>
        /// Construct a enw instance of a symbol class.
        /// </summary>
        /// <param name="value">The symbol's value.</param>
        /// <param name="isMutable">The symbol's mutability flag.</param>
        public Symbol(StringView value, bool isMutable)
            : this()
        {
            StringValue = value;
            DataType = DataType.String;
            StorageType = StorageType.Scalar;
            IsMutable = isMutable;
        }

        /// <summary>
        /// Construct a new instance of a symbol class.
        /// </summary>
        /// <param name="tokens">The tokenized expression of the symbol definition.</param>
        /// <param name="eval">The <see cref="Evaluator"/> to evaluate the expression.</param>
        /// <param name="isMutable">The symbol's mutability flag.</param>
        public Symbol(RandomAccessIterator<Token> tokens, Evaluator eval, bool isMutable)
            : this()
        {
            IsMutable = isMutable;
            StorageType = StorageType.Vector;

            var opens = 1;
            var token = tokens.GetNext();
            if (TokenType.End.HasFlag(token.Type))
                throw new SyntaxException(token.Position, "Expression expected.");

            if (StringHelper.IsStringLiteral(tokens))
                DataType = DataType.String;
            else
                DataType = DataType.Numeric;

            int index = 0;

            while (opens > 0)
            {
                if (token.Type == TokenType.Open && token.Name.Equals("["))
                    opens++;
                else if (token.Name.Equals("]"))
                    opens--;
                else
                {
                    if (DataType == DataType.String)
                    {
                        if (!StringHelper.IsStringLiteral(tokens))
                            throw new SyntaxException(token.Position, "Type mismatch.");
                        StringVector.Add(index++, token.Name.TrimOnce('"'));
                        token = tokens.GetNext();
                        if (token.Name.Equals("]"))
                            opens--;
                    }
                    else
                    {
                        NumericVector.Add(index++, eval.Evaluate(tokens, false));
                        token = tokens.Current;
                        if (token.Name.Equals("]"))
                            continue;
                    }
                }
                token = tokens.GetNext();
            }
        }

        /// <summary>
        /// Construct a new instance of a symbol class.
        /// </summary>
        /// <param name="tokens">The tokenized expression of the symbol definition.</param>
        /// <param name="eval">The <see cref="Evaluator"/> to evaluate the expression.</param>
        public Symbol(RandomAccessIterator<Token> tokens, Evaluator eval)
            : this(tokens, eval, true)
        {

        }

        #endregion

        #region Methods

        /// <summary>
        /// Determine of the symbol is equal in type (both data and storage).
        /// </summary>
        /// <param name="other">The other symbol to compare against.</param>
        /// <returns><c>true</c> if both symbols are equal in type, <c>false</c> otherwise.</returns>
        public bool IsEqualType(Symbol other) =>
            StorageType == other.StorageType &&
            (DataType == other.DataType || (IsNumeric && other.IsNumeric));

        public int CompareTo(Symbol other)
        {
            if (IsEqualType(other))
            {
                if (Equals(other))
                    return 0;
                if (StorageType == StorageType.Scalar)
                {
                    if (IsNumeric)
                        return Comparer<double>.Default.Compare(NumericValue, other.NumericValue);
                    else
                        return Comparer<StringView>.Default.Compare(StringValue, other.StringValue);
                }
                if (DataType == DataType.Numeric)
                    return Comparer<int>.Default.Compare(NumericVector.Count, other.NumericVector.Count);
                else
                    return Comparer<int>.Default.Compare(StringVector.Count, other.StringVector.Count);
            }
            if (StorageType == StorageType.Vector)
            {
                if (other.StorageType == StorageType.Vector)
                {
                    if (DataType == DataType.Numeric)
                        return Comparer<int>.Default.Compare(NumericVector.Count, other.NumericVector.Count);
                    else
                        return Comparer<int>.Default.Compare(StringVector.Count, other.StringVector.Count);
                }
                return 1;
            }
            if (other.StorageType == StorageType.Vector)
                return -1;
            if (IsNumeric)
            {
                if (other.StringValue.Length == 0)
                    return 1;
                return Comparer<double>.Default.Compare(NumericValue, (double)other.StringValue[0]);
            }
            if (StringValue.Length == 0)
                return -1;
            return Comparer<double>.Default.Compare((double)StringValue[0], other.NumericValue);
        }

        public bool Equals(Symbol other)
        {
            if (IsEqualType(other) && StorageType == StorageType.Scalar)
            {
                if (IsNumeric)
                    return EqualityComparer<double>.Default.Equals(NumericValue, other.NumericValue);
                return EqualityComparer<StringView>.Default.Equals(StringValue, other.StringValue);
            }
            return false;
        }

        public override bool Equals(object obj)
        {
            if (obj is Symbol other)
                return Equals(other);
            return false;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                hash += 23 * StorageType.GetHashCode();
                hash += 23 * DataType.GetHashCode();
                if (IsNumeric)
                    hash += 23 * NumericValue.GetHashCode();
                else
                    hash += 23 * StringValue.GetHashCode();
                return hash;
            }
        }

        /// <summary>
        /// Set the vector to the instance.
        /// </summary>
        /// <param name="vector">The other vector.</param>
        public void SetVectorTo(Dictionary<int, double> vector)
        {
            NumericVector.Clear();
            foreach (var kvp in vector)
                NumericVector.Add(kvp.Key, kvp.Value);
        }

        /// <summary>
        /// Set the vector to the instance.
        /// </summary>
        /// <param name="vector">The other vector.</param>
        public void SetVectorTo(Dictionary<int, StringView> vector)
        {
            StringVector.Clear();
            foreach (var kvp in vector)
                StringVector.Add(kvp.Key, kvp.Value);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            if (StorageType == StorageType.Scalar)
            {
                if (DataType == DataType.String)
                {
                    sb.Append('"')
                      .Append(StringValue.ToString())
                      .Append('"');
                }
                else
                {
                    sb.Append($"${(int)NumericValue:x} ({NumericValue})");
                }
            }
            else
            {
                sb.Append('[');
                for (var i = 0; i < StringVector.Count; i++)
                {
                    if (i == 5)
                    {
                        sb.Append(",...");
                        break;
                    }
                    if (DataType == DataType.String)
                        sb.Append('"').Append(StringVector[i].ToString()).Append('"');
                    else
                        sb.Append(NumericValue);

                    if (i < StringVector.Count - 1)
                        sb.Append(", ");
                }
                sb.Append(']');
                if (sb.Length > 8)
                    sb.Length = 8;
            }
            return sb.ToString();
        }

        #endregion

        #region Properties

        /// <summary>
        /// The symbol's internal data type.
        /// </summary>
        public DataType DataType { get; set; }

        /// <summary>
        /// Gets the symbol's storage type.
        /// </summary>
        public StorageType StorageType { get; }

        /// <summary>
        /// Gets whether the symbol is a numeric type.
        /// </summary>
        public bool IsNumeric => DataType == DataType.Numeric || DataType == DataType.Address;

        /// <summary>
        /// Gets or sets the symbol's numeric value.
        /// </summary>
        public double NumericValue { get; set; }

        /// <summary>
        /// Gets the symbol's address bank.
        /// </summary>
        public int Bank { get; }

        /// <summary>
        /// Gets or sets the symbol's string value.
        /// </summary>
        public StringView StringValue { get; set; }

        /// <summary>
        /// Gets the symbol's numeric vector.
        /// </summary>
        public Dictionary<int, double> NumericVector { get; }

        /// <summary>
        /// Gets the symbol's string vector.
        /// </summary>
        public Dictionary<int, StringView> StringVector { get; }

        /// <summary>
        /// Gets the symbol's length.
        /// </summary>
        public int Length
        {
            get
            {
                if (StorageType == StorageType.Vector)
                {
                    if (DataType == DataType.String)
                        return StringVector.Count;
                    return NumericVector.Count;
                }
                if (DataType == DataType.String)
                    return StringValue.Length;
                return NumericValue.Size();
            }
        }

        /// <summary>
        /// Gets whether the symbol is mutable.
        /// </summary>
        public bool IsMutable { get; }

        /// <summary>
        /// Gets or sets the symbol's name.
        /// </summary>
        public string Name { get; set; }

        #endregion
    }
}