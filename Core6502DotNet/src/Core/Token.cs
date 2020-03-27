//-----------------------------------------------------------------------------
// Copyright (c) 2017-2020 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Core6502DotNet
{
    /// <summary>
    /// An enumeration of the token's type.
    /// </summary>
    public enum TokenType
    {
        None = 0,
        Operator,
        Operand,
        Instruction
    };

    /// <summary>
    /// An enumeration of the token's operator type, if applicable.
    /// </summary>
    public enum OperatorType
    {
        None = 0,
        Open,
        Closed,
        Separator,
        Function,
        Unary,
        Binary
    };

    /// <summary>
    /// Represents a token class useed in lexical analysis of 
    /// assembly source code.
    /// </summary>
    public class Token : IEquatable<Token>
    {
        #region Members

        TokenType _type;
        OperatorType _opType;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a new token object.
        /// </summary>
        public Token()
            : this(null, string.Empty, 1)
        {

        }

        /// <summary>
        /// Constructs a new token object.
        /// </summary>
        /// <param name="source">The source for which to derive the token's name.</param>
        public Token(string source)
            : this(null, source, 1)
        {

        }

        /// <summary>
        /// Constructs a new token object.
        /// </summary>
        /// <param name="parent">The token's parent token.</param>
        /// <param name="source">The source for which to derive the token's name.</param>
        /// <param name="position">The token's position (column) in the source code line.</param>
        public Token(Token parent, string source, int position)
        {
            Parent = parent;
            Name = source;
            Position = position;
            Type = TokenType.None;
        }

        #endregion

        #region Methods

        public bool Equals([AllowNull] Token other)
            => Name.Equals(other.Name) && Type == other.Type && OperatorType == other.OperatorType;

        public override int GetHashCode()
            => Name.GetHashCode() | Type.GetHashCode() | OperatorType.GetHashCode();

        public override bool Equals(object obj)
        {
            if (obj is Token)
            {
                var other = obj as Token;
                return this.Equals(other);
            }
            return false;
        }

        public void AddChild(Token token)
        {
            token.Parent = this;
            if (Children == null)
                Children = new List<Token>();
            if (string.IsNullOrEmpty(Name) && Children.Count == 0)
                Position = token.Position;

            Children.Add(token);
        }

        public Token Clone()
        {
            var copy = new Token
            {
                Name = new string(Name),
                Type = Type,
                OperatorType = OperatorType
            };
            if (Children != null)
            {
                copy.Children = new List<Token>(Children.Count);
                foreach (Token child in Children)
                    copy.Children.Add(child.Clone());
            }
            return copy;
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <param name="useUnparsed">Determines whether the ToString
        /// method should be based on the Unparsed name.</param>
        /// <returns>The string representation of the object.</returns>
        public string ToString(bool useUnparsed)
        {
            StringBuilder sb;
            if (useUnparsed)
                sb = new StringBuilder(UnparsedName);
            else
                sb = new StringBuilder(Name);
            if (Children != null)
            {
                foreach (Token t in Children)
                    sb.Append(t.ToString(useUnparsed));
            }
            return sb.ToString();
        }

        public override string ToString()
            => ToString(false);

        #endregion

        #region Properties

        /// <summary>
        /// Gets the token's position (or column) in the source line from
        /// which it was decoded.
        /// </summary>
        public int Position { get; set; }

        /// <summary>
        /// Gets the token's type.
        /// </summary>
        public TokenType Type
        {
            get => _type;
            set
            {
                _type = value;
                OperatorType = OperatorType.None;
            }
        }

        /// <summary>
        /// Gets the token's operator type.
        /// </summary>
        public OperatorType OperatorType
        {
            get => _opType;
            set
            {
                if (Type == TokenType.Operator)
                    _opType = value;
                else
                    _opType = OperatorType.None;
            }
        }

        /// <summary>
        /// Gets or sets the token's name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the token's unparsed name.
        /// </summary>
        public string UnparsedName { get; set; }

        /// <summary>
        /// Gets or sets the token's parent.
        /// </summary>
        public Token Parent { get; set; }

        /// <summary>
        /// Gets or sets the list of the token's child tokens.
        /// </summary>
        public List<Token> Children { get; set; }

        /// <summary>
        /// Determines if the token has any child tokens.
        /// </summary>
        public bool HasChildren => Children != null && Children.Count > 0;

        /// <summary>
        /// Gets the last child in the token's own hierarchy, or itself if it
        /// has no children.
        /// </summary>
        public Token LastChild
        {
            get
            {
                if (HasChildren)
                    return Children[^1].LastChild;
                return this;
            }
        }
        #endregion
    }
}