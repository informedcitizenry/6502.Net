//-----------------------------------------------------------------------------
// Copyright (c) 2017-2021 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Core6502DotNet
{
    /// <summary>
    /// An enumeration of the token's type in an expression.
    /// </summary>
    [Flags]
    public enum TokenType : uint
    {
        None = 0,
        Start           = 0b000000000001,
        Open            = 0b000000000010,
        Operand         = 0b000000000100,
        Unary           = 0b000000001000,
        Radix           = 0b000000010000,
        StartOrOperand  = 0b000000011111,
        Closed          = 0b000000100000,
        Separator       = 0b000001000000,
        End             = 0b000001100000,
        Binary          = 0b000010000000,
        Function        = 0b000100000000,
        EndOrBinary     = 0b000111100000,
        Instruction     = 0b001000000000,
        Label           = 0b010000000000,
        LabelInstr      = 0b011000000000,
        Misc            = 0b100000000000,
        MoreTokens      = Binary | Separator | Open,
        Evaluation      = Binary | Separator | Function
    }

    /// <summary>
    /// Represents a token class useed in lexical analysis of 
    /// assembly source code.
    /// </summary>
    public class Token : IEquatable<Token>, IComparable<Token>
    {
        #region Members

        /// <summary>
        /// Gets the key-value pairs of opens and closures.
        /// </summary>
        public static readonly Dictionary<StringView, StringView> OpenClose = new Dictionary<StringView, StringView>
        {
            { "(", ")" },
            { "{", "}" },
            { "[", "]" }
        };

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a new token object.
        /// </summary>
        public Token()
        {

        }

        /// <summary>
        /// Constructs a new token object
        /// </summary>
        /// <param name="name">The token's (parsed) name.</param>
        /// <param name="type">The token's <see cref="TokenType"/>.</param>
        /// <param name="position">The token's position in its line.</param>
        public Token(StringView name, TokenType type, int position)
        {
            Type = type;
            Name = name;
            Position = position;
        }

        /// <summary>
        /// Constructs a new token object
        /// </summary>
        /// <param name="name">The token's (parsed) name.</param>
        /// <param name="type">The token's <see cref="TokenType"/>.</param>
        public Token(StringView name, TokenType type)
        {
            Name = name;
            Type = type;
            Position = 1;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Determines whether the token is a quoted string or char literal.
        /// </summary>
        /// <returns><c>true</c> if the token is a quote, <c>false</c> otherwise.</returns>
        public bool IsQuote() => Name[0] == '"' || Name[0] == '\'';

        /// <summary>
        /// Determines whether the token is a double-quoted string literal.
        /// </summary>
        /// <returns><c>true</c> if the token is a double-quoted string, <c>false</c> otherwise.</returns>
        public bool IsDoubleQuote() => Name[0] == '"';

        /// <summary>
        /// Determines whether the token is a separator.
        /// </summary>
        /// <returns><c>true</c> if the token is a separator, <c>false</c> otherwise.</returns>
        public bool IsSeparator() => Type.HasFlag(TokenType.Separator);

        /// <summary>
        /// Determines whether the token is a special operator type.
        /// </summary>
        /// <returns><c>true</c> if the token is a special operator type, <c>false</c> otherwise.</returns>
        public bool IsSpecialOperator() => Name[0] == '+' || Name[0] == '-' || Name[0] == '*';

        /// <summary>
        /// Determines whether the token is a group/expression opening.
        /// </summary>
        /// <returns><c>true</c> if the token is an opening, <c>false</c> otherwise.</returns>
        public bool IsOpen() => Type.HasFlag(TokenType.Open);

        /// <summary>
        /// Indicates whether the current token name is equal to the given string.
        /// </summary>
        /// <param name="name">The name string.</param>
        /// <returns><c>true</c> if the token name equals the string, <c>false</c> otherwise.</returns>
        public bool Equals(string name)
            => Name.Equals(name);

        public bool Equals(Token other) 
            => Name.Equals(other.Name) && Type == other.Type;


        public int CompareTo(Token other)
        {
            if (other != null)
            {
                var nameComp = StringViewComparer.Ordinal.Compare(Name, other.Name);
                if (nameComp == 0)
                    return Type.CompareTo(other.Type);

                return nameComp;
            }
            return 1;
        }

        public override bool Equals(object obj)
        {
            if (obj is Token other)
                return Equals(other);
            return false;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17 * 23 + Name.GetHashCode();
                hash = hash * 23 + Type.GetHashCode();
                return hash;
            }
        }

        public override string ToString()
        {
            return Name.ToString();

        }

        #region Static Methods

        /// <summary>
        /// Determines whether the token is an end of an expression, either as a closure,
        /// separator, or a null.
        /// </summary>
        /// <param name="token">The token.</param>
        /// <returns></returns>
        public static bool IsEnd(Token token) => token == null || TokenType.End.HasFlag(token.Type);

        /// <summary>
        /// Gets a grouping of tokens.
        /// </summary>
        /// <param name="tokens">The iterator to the full token expression.</param>
        /// <returns>The grouped tokens</returns>
        public static IEnumerable<Token> GetGroup(RandomAccessIterator<Token> tokens)
        {
            var list = new List<Token> { tokens.Current };
            var open = tokens.Current.Name;
            var closed = OpenClose[open];
            var opens = 1;
            while (tokens.MoveNext() && opens > 0)
            {
                list.Add(tokens.Current);
                if (tokens.Current.Name.Equals(open))
                    opens++;
                else if (tokens.Current.Name.Equals(closed))
                    opens--;
            }
            return list;
        }

        /// <summary>
        /// Joins the collection of tokens into a string.
        /// </summary>
        /// <param name="tokens">The collection of tokens.</param>
        /// <returns>A string representing the joint tokens.</returns>
        public static string Join(IEnumerable<Token> tokens)
        {
            var first = tokens.First();
            var source = first.Name.String;
            if (tokens.Any(t => !ReferenceEquals(t.Name.String, source)))
            {
                var leadingNonTokenString = first.Name.Position == 0 ? string.Empty : source.Substring(0, first.Name.Position);

                var sb = new StringBuilder(leadingNonTokenString);
                var it = tokens.GetIterator();
                while (it.MoveNext())
                {
                    var t = it.Current;
                    var n = it.PeekNext();
                    var offs = t.Name.Position;
                    int size;
                    if (n == null || !ReferenceEquals(t.Name.String, n.Name.String))
                    {
                        sb.Append(t.Name.String.Substring(offs, t.Name.Length));
                        var afterChars = t.Name.String.Substring(offs + t.Name.Length);
                        var firstNonWhite = afterChars.ToList()
                            .FindIndex(c => !char.IsWhiteSpace(c));
                        if (firstNonWhite > 0)
                            sb.Append(afterChars.Substring(0, firstNonWhite));   
                    }
                    else
                    {
                        size = n.Name.Position - offs;
                        sb.Append(t.Name.String.Substring(offs, size));
                    }
                }
                return sb.ToString();
            }
            else
            {
                var offs = first.Name.Position;
                if (offs > 0)
                {
                    var leadingWs = source.Substring(0, first.Name.Position).ToList().FindLastIndex(c => !char.IsWhiteSpace(c));
                    if (leadingWs >= 0)
                        offs = leadingWs + 1;
                }
                int size = tokens.Last().Name.Position + tokens.Last().Name.Length - offs;
                return source.Substring(offs, size);
            }
        }

        #endregion

        #endregion

        #region Properties

        /// <summary>
        /// Gets the token's type.
        /// </summary>
        public TokenType Type { get; }

        /// <summary>
        /// Gets the token's position in its source line.
        /// </summary>
        public int Position { get; }

        /// <summary>
        /// Gets the token's parsed name.
        /// </summary>
        public StringView Name { get; }

        /// <summary>
        /// Gets or sets the token's parsed <see cref="SourceLine"/> in which it appears.
        /// </summary>
        public SourceLine Line { get; internal set; }

        #endregion
    }
}