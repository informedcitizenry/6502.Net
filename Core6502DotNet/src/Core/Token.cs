//-----------------------------------------------------------------------------
// Copyright (c) 2017-2020 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
        #region Constructors

        /// <summary>
        /// Constructs a new token object.
        /// </summary>
        public Token() => Children = ImmutableList.Create<Token>();

        /// <summary>
        /// Constructs a new token object.
        /// </summary>
        /// <param name="source">The source for which to derive the token's name.</param>
        public Token(string source) : this() => Name = source;


        /// <summary>
        /// Constructs a new token object.
        /// </summary>
        /// <param name="source">The source for which to derive the token's name.</param>
        /// <param name="type">The token's <see cref="TokenType"/>.</param>
        public Token(string source, TokenType type) : this() =>
           (Name, Type) = (source, type);

        /// <summary>
        /// constructs a new token object.
        /// </summary>
        /// <param name="name">The token's (parsed) name.</param>
        /// <param name="source">The source for which to derive the token's name.</param>
        /// <param name="type">The token's <see cref="TokenType"/>.</param>
        public Token(string name, string source, TokenType type)
            : this(name, source, type, OperatorType.None) { }

        /// <summary>
        /// Constructs a new token object.
        /// </summary>
        /// <param name="source">The source for which to derive the token's name.</param>
        /// <param name="type">The token's <see cref="TokenType"/>.</param>
        /// <param name="operatorType">The token's <see cref="OperatorType"/>.</param>
        public Token(string source, TokenType type, OperatorType operatorType)
            : this(source, source, type, operatorType, 1) { }

        /// <summary>
        /// Constructs a new token object.
        /// </summary>
        /// <param name="name">The token's (parsed) name.</param>
        /// <param name="source">The original unparsed source.</param>
        /// <param name="type">The token's <see cref="TokenType"/>.</param>
        /// <param name="operatorType">The token's <see cref="OperatorType"/>.</param>
        public Token(string name, string source, TokenType type, OperatorType operatorType)
            : this(name, source, type, operatorType, 1) { }

        /// <summary>
        /// Constructs a new token object.
        /// </summary>
        /// <param name="name">The token's (parsed) name.</param>
        /// <param name="source">The original unparsed source.</param>
        /// <param name="type">The token's <see cref="TokenType"/>.</param>
        /// <param name="operatorType">The token's <see cref="OperatorType"/>.</param>
        /// <param name="position">The token's column position in the original source.</param>
        public Token(string name, string source, TokenType type, OperatorType operatorType, int position)
            : this()
        {
            Name = name;
            UnparsedName = source;
            Type = type;
            OperatorType = type == TokenType.Operator ? operatorType : OperatorType.None;
            Position = position;
        }

        /// <summary>
        /// Constructs a new token object.
        /// </summary>
        /// <param name="name">The token's (parsed) name.</param>
        /// <param name="source">The original unparsed source.</param>
        /// <param name="type">The token's <see cref="TokenType"/>.</param>
        /// <param name="operatorType">The token's <see cref="OperatorType"/>.</param>
        /// <param name="position">The token's column position in the original source.</param>
        /// <param name="children">The token's child tokens.</param>
        public Token(string name, string source, TokenType type, OperatorType operatorType, int position, IEnumerable<Token> children)
            : this(name, source, type, operatorType, position) => Children = ImmutableList.CreateRange(children);

        #endregion

        #region Methods

        public bool Equals([AllowNull] Token other)
            => other != null && 
            Name.Equals(other.Name) && 
            Type == other.Type && 
            OperatorType == other.OperatorType;

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17   * 23 + Name.GetHashCode();
                hash = hash     * 23 + Type.GetHashCode();
                hash = hash     * 23 + OperatorType.GetHashCode();
                return hash;
            }
        }

        public override bool Equals(object obj)
        {
            if (obj is Token other)
                return Equals(other);
            return false;
        }

        /// <summary>
        /// Adds a child to the token's children graph.
        /// </summary>
        /// <param name="token">The child token to add.</param>
        public void AddChild(Token token)
        {
            token.Parent = this;
            if (Children == null)
                Children = ImmutableList.Create<Token>();
            if (string.IsNullOrEmpty(Name) && Children.Count == 0)
                Position = token.Position;

            Children = Children.Add(token);
        }

        /// <summary>
        /// Clone's the token and all its children. 
        /// </summary>
        /// <returns>Returns a deep copy of the token, including deep copies of its children.</returns>
        public Token Clone()
        {
            var copy = new Token(new string(Name), new string(Name), Type, OperatorType, Position);
            foreach (var child in Children)
                copy.Children = copy.Children.Add(child.Clone());
            return copy;
        }

        public override string ToString()
        {
            var sb = new StringBuilder(UnparsedName);
            if (Children != null)
            {
                foreach (Token t in Children)
                    sb.Append(t.ToString());
            }
            if (OperatorType == OperatorType.Open)
                sb.Append(LexerParser.Groups[Name]);
            return sb.ToString();
        }

        /// <summary>
        /// Auto-creates a separator token.
        /// </summary>
        /// <returns>A parsed token that represents a separator.</returns>
        public static Token SeparatorToken()
            => new Token(string.Empty, string.Empty, TokenType.Operator, OperatorType.Separator);

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the token's position (or column) in the source line from
        /// which it was decoded.
        /// </summary>
        public int Position { get; set; }

        /// <summary>
        /// Gets the token's type.
        /// </summary>
        public TokenType Type { get; }

        /// <summary>
        /// Gets the token's operator type.
        /// </summary>
        public OperatorType OperatorType { get; }

        /// <summary>
        /// Gets or the token's name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the token's unparsed name.
        /// </summary>
        public string UnparsedName { get; }

        /// <summary>
        /// Gets the token's parent.
        /// </summary>
        public Token Parent { get; private set; }

        /// <summary>
        /// Gets the list of the token's child tokens.
        /// </summary>
        public ImmutableList<Token> Children { get; private set; }

        /// <summary>
        /// Gets the last child in the token's own hierarchy, or itself if it
        /// has no children.
        /// </summary>
        public Token LastChild
        {
            get
            {
                if (Children.Count > 0)
                    return Children[^1].LastChild;
                return this;
            }
        }
        #endregion
    }
}