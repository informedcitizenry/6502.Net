//-----------------------------------------------------------------------------
// Copyright (c) 2017-2020 informedcitizenry <informedcitizenry@gmail.com>
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
    /// An enumeration of symbol storage types.
    /// </summary>
    public enum StorageType
    {
        Scalar,
        Vector,
        Hash,
        NonScalarVector,
        NonScalarHash
    }

    /// <summary>
    /// Represents an error in accessing, creating or modifying 
    /// a symbol.
    /// </summary>
    public class SymbolException : Exception
    {
        /// <summary>
        /// An enumeration representing categories of symbol errors.
        /// </summary>
        public enum ExceptionReason
        {
            Redefined,
            NonScalar,
            NotDefined,
            MutabilityChanged,
            NotValid,
            InvalidBackReference,
            InvalidForwardReference,
            Scalar
        }

        static readonly Dictionary<ExceptionReason, string> s_reasonMessages = new Dictionary<ExceptionReason, string>
        {
            { ExceptionReason.MutabilityChanged,        "Cannot redeclare a variable as a label."                     },
            { ExceptionReason.Redefined,                "Cannot redefine \"{0}\"."                                    },
            { ExceptionReason.NonScalar,                "Symbol \"{0}\" is non-scalar but is being used as a scalar." },
            { ExceptionReason.NotDefined,               "Symbol \"{0}\" is not defined."                              },
            { ExceptionReason.NotValid,                 "\"{0}\" is not a valid symbol name."                         },
            { ExceptionReason.InvalidBackReference,     "Invalid back reference."                                     },
            { ExceptionReason.InvalidForwardReference,  "Invalid forward reference."                                  },
            { ExceptionReason.Scalar,                   "Symbol \"{0}\" is scalar but is being used as a non-scalar." }
        };

        /// <summary>
        /// Constructs a new instance of a symbol exception.
        /// </summary>
        /// <param name="symbolName">The symbol's name.</param>
        /// <param name="position">The position in the symbol in the original source.</param>
        /// <param name="reason">The exception reason.</param>
        public SymbolException(StringView symbolName, int position, ExceptionReason reason)
            : base(string.Format(s_reasonMessages[reason], symbolName))
        {
            Position = position;
            SymbolToken = null;
            SymbolName = symbolName;
            Reason = reason;
        }

        /// <summary>
        /// Constructs a new instance of a symbol exception.
        /// </summary>
        /// <param name="token">The symbol as a parsed token.</param>
        /// <param name="reason">The exception reason.</param>
        public SymbolException(Token token, ExceptionReason reason)
            : base(string.Format(s_reasonMessages[reason], token.Name))
        {
            SymbolToken = token;
            Position = token.Position;
            SymbolName = token.Name;
            Reason = reason;
        }

        /// <summary>
        /// Gets the symbol exception's reason.
        /// </summary>
        public ExceptionReason Reason { get; set; }

        /// <summary>
        /// Gets the exception's associated token.
        /// </summary>
        public Token SymbolToken { get; set; }

        /// <summary>
        /// Gets the exception's associated symbol name.
        /// </summary>
        public StringView SymbolName { get; set; }

        public int Position { get; set; }
    }

    /// <summary>
    /// Represents a symbol with value and storage type information.
    /// </summary>
    public class Symbol : SymbolBase, IEquatable<Symbol>, IComparable<Symbol>
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

        #region Properties

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

        #endregion
    }

    /// <summary>
    /// A class managing all valid assembly symbols, including their scope and values.
    /// </summary>
    public class SymbolManager
    {
        #region Subclasses

        class LineReferenceTable
        {
            LineReferenceTable _parent;

            Dictionary<int, (StringView name, double value)> _table;

            public LineReferenceTable(LineReferenceTable parent)
            {
                _parent = parent;
                _table = new Dictionary<int, (StringView name, double value)>();
            }

            public void DefineLineReference(Token fromToken, double value)
                => _table[fromToken.Line.IndexInSources + 1] = (fromToken.Name, value);

            public double GetLineReference(StringView reference, Token fromToken)
            {
                var count = reference.Length;
                var index = fromToken.Line.IndexInSources + 1;
                var key = 0;
                while (count > 0)
                {
                    if (fromToken.Name[0] == '-')
                        key = _table.Keys.LastOrDefault(k => k < index);
                    else
                        key = _table.Keys.FirstOrDefault(k => k > index);
                    if (key == 0)
                        break;
                    if (_table[key].name[0] == fromToken.Name[0])
                        count--;
                    index = key;
                }
                if (!_table.TryGetValue(key, out var lineReference))
                {
                    if (_parent != null)
                        return _parent.GetLineReference(reference.Substring(0, count), fromToken);
                    return double.NaN;
                }
                return lineReference.value;
            }

        }

        #endregion

        #region Members

        readonly Dictionary<string, Symbol> _symbolTable;
        readonly Stack<string> _scope;
        readonly Stack<int> _referenceTableIndexStack;
        readonly List<LineReferenceTable> _lineReferenceTables;
        readonly List<Func<StringView, bool>> _criteria;
        int _referenceTableCounter, _ephemeralCounter;
        readonly bool _caseSensitive;
        readonly Evaluator _evaluator;
        string _localScope;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a new instance of a symbol manager class.
        /// </summary>
        /// <param name="caseSensitive">Determine whether this symbol manager is case-sensitive.</param>
        /// <param name="evaluator">The <see cref="Evaluator"/> to assist in evaluating symbol definitions.</param>
        public SymbolManager(bool caseSensitive, Evaluator evaluator)
        {
            _evaluator = evaluator;
            _caseSensitive = caseSensitive;
            _symbolTable = new Dictionary<string, Symbol>(caseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase);
            _scope = new Stack<string>();
            _localScope = string.Empty;
            _referenceTableIndexStack = new Stack<int>();
            _referenceTableIndexStack.Push(0);
            _lineReferenceTables = new List<LineReferenceTable>
            {
                new LineReferenceTable(null)
            };
            _referenceTableCounter = 0;
            var comparer = caseSensitive ? StringViewComparer.Ordinal : StringViewComparer.IgnoreCase;
            
            _criteria = new List<Func<StringView, bool>>
            {
                s =>
                {
                    return s.Equals("+") || s.Equals("-") ||
                            ((s[0] == '_' || char.IsLetter(s[0])) &&
                            (char.IsLetterOrDigit(s[^1]) || s[^1] == '_' ) && !s.Contains('.'));
                }
            };
            SearchedNotFound = false;
        }

        #endregion

        #region Methods

        string GetScopedName(StringView name) => GetAncestor(name, 0);

        string GetAncestor(StringView name, int back)
        {
            var symbolPath = new List<string>();
            var child = name[0] == '_' ? _localScope + "." + name.ToString() : name.ToString();
            if (_scope.Count > 0)
            {
                if (back > _scope.Count)
                    return child;
                symbolPath.AddRange(_scope.ToList().Skip(back).Reverse());
            }
            symbolPath.Add(child);
            return string.Join('.', symbolPath);
        }

        string GetFullyQualifiedName(StringView name)
        {
            var scopedName = GetScopedName(name);
            var original = scopedName;
            var i = 0;
            while (!_symbolTable.ContainsKey(scopedName))
            {
                scopedName = GetAncestor(name, ++i);
                if (i > _scope.Count)
                    break;
            }
            if (!SearchedNotFound)
                SearchedNotFound = !original.Equals(scopedName);
            return scopedName;
        }

        void DefineSymbol(StringView name, Symbol symbol, bool isGlobal)
        {
            if (_criteria.Any(c => !c(name)))
                throw new SymbolException(name, 1, SymbolException.ExceptionReason.NotValid);
            string child;
            if (name[0] == '_')
                child = _localScope + "." + name.ToString();
            else
                LocalScope = child = name.ToString();
            string fqdn;
            if (isGlobal)
                fqdn = child;
            else
                fqdn = GetScopedName(child);

            if (!_symbolTable.TryGetValue(fqdn, out var existing) && symbol.IsMutable && !fqdn.Contains('@') && fqdn.Contains('.'))
            {
                // gotta figure this out!
                var scopes = fqdn.Split('.').ToList();
                while (scopes.Count > 1 && existing == null)
                {
                    scopes.RemoveAt(scopes.Count - 2);
                    var oneHigher = string.Join('.', scopes);
                    _symbolTable.TryGetValue(oneHigher, out existing);
                    if (existing != null)
                        fqdn = oneHigher;
                }
            }
            if (existing != null && !existing.IsEqualType(symbol))
                throw new Exception("Type mismatch.");
            _symbolTable[fqdn] = symbol;
        }

        /// <summary>
        /// Determines if the symbol has been defined.
        /// </summary>
        /// <param name="name">The symbol name.</param>
        /// <returns><c>true</c> if the symbol has been defined, 
        /// otherwise <c>false</c>.</returns>
        public bool SymbolExists(StringView name)
            => _symbolTable.ContainsKey(GetFullyQualifiedName(name));

        /// <summary>
        /// Determines if the symbol has been defined.
        /// </summary>
        /// <param name="name">The symbol name.</param>
        /// <param name="searchUp">Search up the scope hierarchy.</param>
        /// <returns><c>true</c> if the symbol has been defined, 
        /// otherwise <c>false</c>.</returns>
        public bool SymbolExists(StringView name, bool searchUp)
            => searchUp ? _symbolTable.ContainsKey(GetFullyQualifiedName(name)) :
                          _symbolTable.ContainsKey(GetScopedName(name));

        /// <summary>
        /// Gets the named symbol if defined.
        /// </summary>
        /// <param name="symbolToken">The parsed token of the symbol name.</param>
        /// <param name="raiseExceptionIfNotFound">Raise exception if the symbol is not found.</param>
        /// <returns>The <see cref="Symbol"/> if it exists.</returns>
        /// <exception cref="SymbolException"></exception>
        public Symbol GetSymbol(Token symbolToken, bool raiseExceptionIfNotFound)
        {
            var fqdn = GetFullyQualifiedName(symbolToken.Name);
            if (_symbolTable.ContainsKey(fqdn))
                return _symbolTable[fqdn];
            if (raiseExceptionIfNotFound)
                throw new SymbolException(symbolToken.Name, symbolToken.Position, SymbolException.ExceptionReason.NotDefined);
            return null;
        }

        /// <summary>
        /// Define a symbol as representing the current state of the program counter.
        /// </summary>
        /// <param name="name">The address name.</param>
        /// <param name="address">The address.</param>
        /// <param name="bank">The current address bank.</param>
        public void DefineSymbolicAddress(StringView name, double address, int bank)
            => DefineSymbol(name, new Symbol(address, bank), false);

        /// <summary>
        /// Define a globally scoped symbol.
        /// </summary>
        /// <param name="name">The symbol name.</param>
        /// <param name="value">The symbol value.</param>
        public void DefineGlobal(StringView name, double value)
            => DefineSymbol(name, new Symbol(value), true);

        void DefineFromExpression(RandomAccessIterator<Token> tokens, bool isGlobal, bool isMutable)
        {
            var lhs = tokens.Current;
            if (lhs == null)
                throw new ExpressionException(1, "Expression expected.");

            var equ = tokens.GetNext();
            if (equ == null || !equ.Name.Equals("="))
            {
                if (equ != null && equ.Name.Equals("["))
                {
                    var sym = GetSymbol(lhs, true);
                    if (sym.StorageType != StorageType.Vector)
                        throw new SymbolException(lhs, SymbolException.ExceptionReason.Scalar);
                    var subscript = (int)_evaluator.Evaluate(tokens);
                    if ((equ = tokens.GetNext()) != null && equ.Name.Equals("="))
                    {
                        if (!tokens.MoveNext())
                            throw new SyntaxException(equ.Position, "rhs expression missing from assignment.");
                        var rhs = tokens.Current;
                        if (sym.DataType == DataType.String)
                        {
                            if (subscript < 0 || subscript >= sym.StringVector.Count)
                                throw new ExpressionException(lhs.Position, "Index out of range.");
                            if (rhs.IsDoubleQuote())
                                sym.StringVector[subscript] = rhs.Name;
                            else
                                throw new SyntaxException(rhs.Position, "Type mismatch.");
                        }
                        else
                        {
                            if (subscript < 0 || subscript >= sym.NumericVector.Count)
                                throw new ExpressionException(lhs.Position, "Index out of range.");
                            sym.NumericVector[subscript] = _evaluator.Evaluate(tokens, false);
                        }
                        return;
                    }
                }
                throw new SyntaxException(lhs.Position, "Assignment expression must have an expression operator.");
            }
            else
            {
                var rhs = tokens.GetNext();
                if (rhs == null)
                    throw new ExpressionException(equ.Position, "rhs expression missing from assignment.");
                if (rhs.Name.Equals("["))
                {
                    DefineSymbol(lhs.Name, new Symbol(tokens, _evaluator, isMutable), isGlobal);
                }
                else
                {
                    if (rhs.IsDoubleQuote())
                    {
                        if (tokens.PeekNext() == null || TokenType.End.HasFlag(tokens.PeekNext().Type))
                        {
                            DefineSymbol(lhs.Name, new Symbol(rhs.Name.TrimOnce('"'), isMutable), isGlobal);
                            return;
                        }
                        tokens.SetIndex(tokens.Index - 1);
                    }
                    DefineSymbol(lhs.Name, new Symbol(_evaluator.Evaluate(tokens, false), isMutable), isGlobal);
                }
            }

        }

        /// <summary>
        /// Define a scoped symbol.
        /// </summary>
        /// <param name="tokens">The tokenized symbol assignment expression (lhs and rhs).</param>
        public void DefineSymbol(RandomAccessIterator<Token> tokens)
            => DefineFromExpression(tokens, false, true);

        /// <summary>
        /// Define a scoped symbol.
        /// </summary>
        /// <param name="name">The symbol name.</param>
        /// <param name="value">The symbol's value.</param>
        public void DefineSymbol(StringView name, double value)
            => DefineSymbol(name, new Symbol(value), false);

        /// <summary>
        /// Define a scoped symbol.
        /// </summary>
        /// <param name="name">The symbol name.</param>
        /// <param name="value">The symbol's value.</param>
        /// <param name="isMutable">Whether the symbol is mutable.</param>
        public void DefineSymbol(StringView name, double value, bool isMutable)
            => DefineSymbol(name, new Symbol(value, isMutable), false);

        /// <summary>
        /// Define a scoped symbol.
        /// </summary>
        /// <param name="name">The symbol name.</param>
        /// <param name="value">The symbol's value.</param>
        public void DefineSymbol(StringView name, StringView value)
            => DefineSymbol(name, new Symbol(value), false);

        /// <summary>
        /// Define a scoped symbol.
        /// </summary>
        /// <param name="name">The symbol name.</param>
        /// <param name="value">The symbol's value.</param>
        /// <param name="isMutable">Whether the symbol is mutable.</param>
        public void DefineSymbol(StringView name, StringView value, bool isMutable)
            => DefineSymbol(name, new Symbol(value, isMutable), false);

        /// <summary>
        /// Define a scoped symbol.
        /// </summary>
        /// <param name="name">The symbol name.</param>
        /// <param name="tokens">The tokenized symbol assignment expression (rhs).</param>
        public void DefineSymbol(StringView name, RandomAccessIterator<Token> tokens)
            => DefineSymbol(name, new Symbol(tokens, _evaluator, false), false);

        /// <summary>
        /// Gets the reference symbol specified.
        /// </summary>
        /// <param name="name">The reference name.</param>
        /// <param name="fromToken">The token that containing the reference.</param>
        /// <returns>The symbol's numeric value.</returns>
        public double GetLineReference(StringView name, Token fromToken)
        {
            var topFrameIndex = _referenceTableIndexStack.Peek();
            return _lineReferenceTables[topFrameIndex].GetLineReference(name, fromToken);
        }

        /// <summary>
        /// Define an line reference ("+" or "-") symbol.
        /// </summary>
        /// <param name="fromToken">The token from which the line was defined.</param>
        /// <param name="value">The line reference value.</param>
        public void DefineLineReference(Token fromToken, double value)
        {
            var topTableIndex = _referenceTableIndexStack.Peek();
            _lineReferenceTables[topTableIndex].DefineLineReference(fromToken, value);
        }

        /// <summary>
        /// Push an ephemeral scope onto the scope stack.
        /// </summary>
        public void PushScopeEphemeral() => _scope.Push($"@{_ephemeralCounter++}");

        /// <summary>
        /// Pushes the scope onto the stack. If the passed name is 
        /// an empty string, the scope is considered unnamed and symbols defined 
        /// within it not be accessible outside of it.
        /// </summary>
        /// <param name="name">The scope's name.</param>
        public void PushScope(StringView name)
        {
            _scope.Push(name.ToString());
            var parent = _lineReferenceTables[_referenceTableIndexStack.Peek()];
            _lineReferenceTables.Add(new LineReferenceTable(parent));
            _referenceTableIndexStack.Push(++_referenceTableCounter);
        }

        /// <summary>
        /// Pops the current scope off the scope stack.
        /// </summary>
        public void PopScope()
        {
            if (_scope.Count > 0)
            {
                var sc = _scope.Pop();
                var ephemeral = sc[0] == '@';
                if (ephemeral)
                {
                    _ephemeralCounter--;
                    var ephemerals = new List<string>(_symbolTable.Keys
                                .Where(k => k.Contains(sc, _caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase)));
                    foreach (var key in ephemerals)
                        _symbolTable.Remove(key);
                }
                else
                {
                    _referenceTableIndexStack.Pop();
                }
            }
        }

        /// <summary>
        /// Reset the symbol manager's internal state.
        /// </summary>
        public void Reset()
        {
            foreach (var symbol in _symbolTable.Where(s => s.Value.IsMutable))
                _symbolTable.Remove(symbol.Key);
            _localScope = string.Empty;
            _referenceTableCounter = 0;
            SearchedNotFound = false;
        }

        /// <summary>
        /// Get a string listing of all defined label symbols.
        /// </summary>
        /// <returns>The string listing.</returns>
        public string ListLabels()
        {
            var listBuilder = new StringBuilder();
            var labels = _symbolTable.Where(s => !s.Key.Equals("CURRENT_PASS") && s.Value.DataType == DataType.Address)
                                     .OrderBy(s => s.Key);
            foreach (var label in labels)
            {
                var name = label.Key;
                if (!char.IsLetter(name[0]) && name[0] != '_')
                {
                    var paths = name.Split('.', StringSplitOptions.RemoveEmptyEntries);
                    var namesNoAnons = new List<string>();
                    foreach (var path in paths)
                    {
                        if (char.IsLetter(path[0]) || path[0] == '_')
                            namesNoAnons.Add(path);
                        else
                            namesNoAnons.Add("::");
                    }
                    name = string.Join('.', namesNoAnons);
                }
                listBuilder.Append($"{name}".Elliptical(33).PadRight(33)).Append(" = ");
                switch (label.Value.DataType)
                {
                    case DataType.String:
                        listBuilder.Append($"\"{label.Value.StringValue}\"");
                        break;
                    default:
                        listBuilder.Append($"{label.Value.NumericValue} (${(int)label.Value.NumericValue + (label.Value.Bank * 0x10000):x})");
                        break;
                }
                listBuilder.AppendLine();
            }
            return listBuilder.ToString();
        }

        /// <summary>
        /// Add a criterion by which a symbol's name is considered valid.
        /// </summary>
        /// <param name="criterion">The criterion function.</param>
        public void AddValidSymbolNameCriterion(Func<StringView, bool> criterion) => _criteria.Add(criterion);

        /// <summary>
        /// Determines if a symbol is valid.
        /// </summary>
        /// <param name="symbol">The symbol name.</param>
        /// <returns><c>true</c> if valid, otherwise <c>false</c>.</returns>
        public bool SymbolIsValid(StringView symbol)
            => !_criteria.Any(f => !f(symbol));

        /// <summary>
        /// Pop the ephemeral scope from the scope stack.
        /// </summary>
        public void PopScopeEphemeral()
        {
            var ephemeralScope = $"@{_ephemeralCounter - 1}";
            if (_scope.Any(s => s.Equals(ephemeralScope)))
            {
                while (_scope.Count > 0 && !_scope.Peek().Equals(ephemeralScope))
                    PopScope();

                PopScope();
            }
        }
        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the local scope for cheap symbols.
        /// </summary>
        public string LocalScope
        {
            get => _localScope;
            set
            {
                if (value[0] != '_')
                    _localScope = value;
            }
        }

        /// <summary>
        /// Gets whether any symbols were searched but not found.
        /// </summary>
        public bool SearchedNotFound { get; private set; }

        #endregion
    }
}