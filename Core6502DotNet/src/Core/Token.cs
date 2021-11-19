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
        Start           = 0b0000000000001,
        Open            = 0b0000000000010,
        Operand         = 0b0000000000100,
        Unary           = 0b0000000001000,
        Radix           = 0b0000000010000,
        StartOrOperand  = 0b0000000011111,
        Closed          = 0b0000000100000,
        Separator       = 0b0000001000000,
        Terminal        = 0b0000001100000,
        Ternary         = 0b0000010000000,
        Binary          = 0b0000100000000,
        Function        = 0b0001000000000,
        EndOrBinary     = 0b0001111100000,
        Instruction     = 0b0010000000000,
        Label           = 0b0100000000000,
        LabelInstr      = 0b0110000000000,
        Misc            = 0b1000000000000,
        MoreTokens      = Binary | Ternary | Separator | Open,
        Evaluation      = Binary | Ternary | Separator | Function 
    }

    /// <summary>
    /// Represents a token class useed in lexical analysis of 
    /// assembly source code.
    /// </summary>
    public class Token : IEquatable<Token>, IComparable<Token>
    {
        #region Members

        static readonly Dictionary<StringView, StringView> s_openClose = new Dictionary<StringView, StringView>
        {
            { "(", ")" },
            { "{", "}" },
            { "[", "]" }
        };

        static readonly HashSet<StringView> s_compoundAssignments = new HashSet<StringView>
        {
            "+=","-=","*=","/=","%=",">>=","<<=","&=","^=","|=","^^="
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
            IsCompoundAssignment = s_compoundAssignments.Contains(name);
            IsAssignment = Name?[^1] == '=' && (Name.Length == 1 || Name[0] == ':' || IsCompoundAssignment);
        }

        /// <summary>
        /// Constructs a new token object
        /// </summary>
        /// <param name="name">The token's (parsed) name.</param>
        /// <param name="type">The token's <see cref="TokenType"/>.</param>
        public Token(StringView name, TokenType type)
            : this(name, type, 1) { }

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

        public override string ToString() => Name.ToString();

        #region Static Methods

        /// <summary>
        /// Determines whether the token is a terminal, such as a closing bracket,
        /// separator, or a null value.
        /// </summary>
        /// <param name="token">The token.</param>
        /// <returns></returns>
        public static bool IsTerminal(Token token) => token == null || TokenType.Terminal.HasFlag(token.Type);

        /// <summary>
        /// Gets a grouping of tokens.
        /// </summary>
        /// <param name="tokens">The iterator to the full token expression.</param>
        /// <returns>The grouped tokens</returns>
        public static IEnumerable<Token> GetGroup(RandomAccessIterator<Token> tokens)
        {
            var list = new List<Token>();
            var open = tokens.Current.Name;
            var closed = s_openClose[open];
            var opens = 1;
            var token = tokens.Current;
            while (opens > 0)
            {
                list.Add(token);
                if ((token = tokens.GetNext()).Name.Equals("("))
                    opens++;
                else if (token.Name.Equals(closed))
                    opens--;
            }
            list.Add(token);
            return list;
        }

        /// <summary>
        /// Relate the <see cref="Token"/> objects in the collection by expressions.
        /// </summary>
        /// <param name="tokens"></param>
        internal static void GatherIntoExpressions(List<Token> tokens)
        {
            Token firstToken = null, lastToken = null;
            var inFunction = false;
            var brackets = 0;
            foreach (var token in tokens)
            {
                if ((!IsTerminal(token) && !token.IsAssignment && 
                    (token.Type != TokenType.Open || 
                    lastToken?.Type != TokenType.Closed))|| 
                    token.Type == TokenType.Ternary ||
                    inFunction || 
                    token.Type == TokenType.Closed)
                {
                    inFunction |= token.Type == TokenType.Function;
                    if (token.Type == TokenType.Open)
                        brackets++;
                    else if (token.Type == TokenType.Closed)
                    {
                        if (--brackets == 0)
                            inFunction = false;
                    }
                    if (lastToken != null && !lastToken.IsAssignment)
                        lastToken.NextInExpression = token;
                    if (firstToken == null)
                        firstToken = token;
                    token.FirstInExpression = firstToken;
                    token.IsPartOfAnExpression = true;
                }
                else
                {
                    token.FirstInExpression = token;
                    firstToken = lastToken = null;
                }
                lastToken = token;
            }
        }

        /// <summary>
        /// Gets the string representation of the expression of which a specified
        /// <see cref="Token"/> is a part.
        /// </summary>
        /// <param name="token">A token in an expression.</param>
        /// <param name="startAtToken">Only return the expression from 
        /// the point of the specified token.</param>
        /// <param name="toNewLine">Only return the expression of the current line.</param>
        /// <returns>The string representation of the expression string of which 
        /// the token is a part.</returns>
        public static string GetExpression(Token token, bool startAtToken = false, bool toNewLine = false)
        {
            if (string.IsNullOrEmpty(token.Line?.FullSource) || !token.IsPartOfAnExpression)
                return token.Name.ToString();
            if (token.FirstInExpression != null && !startAtToken)
                token = token.FirstInExpression;
            var len = 0;
            var current = token.NextInExpression;
            var previous = token;
            var firstInLine = token;
            var lineSourceIndex = token.LineSourceIndex;
            var startOffset = token.Position - 1;
            var sourceLines = token.Line.FullSource.Split('\n');
            if (startAtToken && lineSourceIndex > 0)
            {
                for (var i = 0; i < lineSourceIndex; i++)
                    startOffset += sourceLines[i].Length + 1;
            }
            while (current?.IsPartOfAnExpression == true)
            {
                if (current.LineSourceIndex > lineSourceIndex)
                {
                    if (toNewLine)
                        break;
                    len += sourceLines[lineSourceIndex].Length - firstInLine.Position + current.Position + 1;
                    firstInLine = current;
                    lineSourceIndex++;
                }
                previous = current;
                current = current.NextInExpression;
            }
            len += previous.Position - firstInLine.Position + previous.Name.Length;
            return token.Line.FullSource.Substring(startOffset, len);
        }

        #endregion

        #endregion

        #region Properties

        /// <summary>
        /// Gets the token's type.
        /// </summary>
        public TokenType Type { get; }

        public ValueType ValueType { get; set; }

        public object Value { get; set; }

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

        /// <summary>
        /// Gets whether the token represents an assignment operator.
        /// </summary>
        public bool IsAssignment { get; }

        /// <summary>
        /// Gets whether the token represents a compound assignment operator.
        /// </summary>
        public bool IsCompoundAssignment { get; }

        /// <summary>
        /// Gets or sets the source index of the line in which the token appears.
        /// </summary>
        public int LineSourceIndex { get; internal set; }

        /// <summary>
        /// Represents a token related to this token that succeeds it in a full expression.
        /// </summary>
        public Token NextInExpression { get; private set; }

        /// <summary>
        /// Represents a token beginning an expression containing this token.
        /// </summary>
        public Token FirstInExpression { get; private set; }

        /// <summary>
        /// Gets whether the token is part of an expression.
        /// </summary>
        public bool IsPartOfAnExpression { get; private set; }

        #endregion
    }
}