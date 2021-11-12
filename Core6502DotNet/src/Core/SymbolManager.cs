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
    /// Represents an error in accessing, creating or modifying 
    /// a symbol.
    /// </summary>
    public sealed class SymbolException : Exception
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
            IllegalReference,
            InvalidBackReference,
            InvalidForwardReference,
            Scalar
        }

        static readonly Dictionary<ExceptionReason, string> s_reasonMessages = new Dictionary<ExceptionReason, string>
        {
            { ExceptionReason.MutabilityChanged,        "Cannot redeclare a label as a variable."                     },
            { ExceptionReason.Redefined,                "Cannot redefine \"{0}\"."                                    },
            { ExceptionReason.NonScalar,                "Symbol \"{0}\" is non-scalar but is being used as a scalar." },
            { ExceptionReason.NotDefined,               "Symbol \"{0}\" is not defined."                              },
            { ExceptionReason.NotValid,                 "\"{0}\" is not a valid symbol name."                         },
            { ExceptionReason.IllegalReference,         "Illegal reference to symbol \"{0}\"."                               },
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
    /// A class managing all valid assembly symbols, including their scope and values.
    /// </summary>
    public sealed class SymbolManager
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
        readonly Stack<string> _scope, _localScopes;
        readonly Stack<int> _referenceTableIndexStack;
        readonly List<LineReferenceTable> _lineReferenceTables;
        readonly List<Func<StringView, bool>> _criteria;
        int _referenceTableCounter, _ephemeralCounter;
        readonly bool _caseSensitive;
        readonly Evaluator _evaluator;

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
            _localScopes = new Stack<string>();
            _localScopes.Push(string.Empty);
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
                            (((s[0] == '_' && s.Length > 1 && s.ToString().Any(c => char.IsLetterOrDigit(c))) || char.IsLetter(s[0])) &&
                            (char.IsLetterOrDigit(s[^1]) || s[^1] == '_' ) && !s.Contains('.'));
                },
                s =>
                {
                    var stringComparer = _caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
                    if (s.Equals("true", stringComparer) || s.Equals("false", stringComparer))
                        return false;
                    var fullSymbolName = GetScopedName(s);
                    return !_evaluator.IsReserved(fullSymbolName);
                }
            };
            SearchedNotFound = false;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets the referenced symbol's fully scoped name.
        /// </summary>
        /// <param name="name">The reference symbol name.</param>
        /// <returns>The fully scoped name.</returns>
        public string GetScopedName(StringView name) => GetAncestor(name, 0);

        string GetAncestor(StringView name, int back)
        {
            var symbolPath = new List<string>();
            string child;
            if (name[0] == '_' && !string.IsNullOrEmpty(_localScopes.Peek()))
                child = _localScopes.Peek() + "." + name.ToString();
            else
                child = name.ToString();
            if (_scope.Count > 0)
            {
                if (back > _scope.Count)
                    return child;
                symbolPath.AddRange(_scope.ToList().Skip(back).Reverse());
            }
            symbolPath.Add(child);
            return string.Join('.', symbolPath);
        }

        string GetFullyQualifiedName(StringView name, bool updateSearchNotFound)
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
            if (updateSearchNotFound && !SearchedNotFound)
                SearchedNotFound = !original.Equals(scopedName);
            return scopedName;
        }

        /// <summary>
        /// Gets the fully qualified reference name of the symbol name, from within its scope and
        /// working up. If found at the nearest scope, the scoped symbol name will be the return value.
        /// If no reference for this symbol name is found, it will return the unscoped symbol name.
        /// </summary>
        /// <param name="name">The reference symbol name.</param>
        /// <returns>The fully qualified name of the symbol reference.</returns>
        public string GetFullyQualifiedName(StringView name) => GetFullyQualifiedName(name, true);

        static bool InSameFunctionScope(string symbol1, string symbol2)
        {
            var sym1Ix = symbol1.LastIndexOf('@');
            var sym2Ix = symbol2.LastIndexOf('@');
            if (sym1Ix > -1 && sym1Ix == sym2Ix)
            {
                var dot1Ix = symbol1[sym1Ix..].IndexOf('.');
                var dot2Ix = symbol2[sym2Ix..].IndexOf('.');
                return symbol1.Substring(0, sym1Ix + dot1Ix).Equals(symbol2.Substring(0, sym2Ix + dot2Ix));

            }
            return sym1Ix == -1 && sym2Ix == -1;
        }

        void DefineSymbol(StringView name, Symbol symbol, bool isGlobal, bool isWeak = false)
        {
            if (_criteria.Any(c => !c(name)))
                throw new SymbolException(name, 1, SymbolException.ExceptionReason.NotValid);
            string nameStr = name.ToString();
            string fqdn;
            if (isGlobal)
            {
                fqdn = nameStr;
            }
            else
            {
                // the fqdn is the fully scoped name by default
                fqdn = GetScopedName(nameStr);
                if (isWeak && !_symbolTable.ContainsKey(fqdn) && fqdn.Contains('.'))
                {
                    // but if it is weak (can be shadowed by same named symbol in outer scope)
                    // check if an outer scoped symbol exists
                    var weak = GetFullyQualifiedName(nameStr, false);
                    if (_symbolTable.ContainsKey(weak) && InSameFunctionScope(weak, fqdn) && _symbolTable[weak].IsMutable)
                        fqdn = weak; // if so, do not create the same symbol name in an inner scope
                }
            }
            if (_symbolTable.TryGetValue(fqdn, out var existing))
            {
                if ((existing == null && symbol != null) || (symbol == null && existing != null) || !existing.IsEqualType(symbol))
                    throw new SymbolException(name, 1, SymbolException.ExceptionReason.Redefined);
            }
            if (symbol != null)
                symbol.Name = nameStr;
            _symbolTable[fqdn] = symbol;
        }

        /// <summary>
        /// Determines whether a symbol is mutable.
        /// </summary>
        /// <param name="name">The symbol name.</param>
        /// <returns><c>true</c> if the symbol exists and is mutable,
        /// <c>false</c> otherwise.</returns>
        public bool SymbolIsMutable(StringView name)
        {
            if (_symbolTable.TryGetValue(GetFullyQualifiedName(name, false), out var existing))
                return existing.IsMutable;
            return false;
        }

        /// <summary>
        /// Determines if the symbol has been defined.
        /// </summary>
        /// <param name="name">The symbol name.</param>
        /// <param name="searchUp">Search up the scope hierarchy.</param>
        /// <returns><c>true</c> if the symbol has been defined, 
        /// otherwise <c>false</c>.</returns>
        public bool SymbolIsMutable(StringView name, bool searchUp)
        {
            var fqdn = searchUp ? GetFullyQualifiedName(name) : GetScopedName(name);
            if (_symbolTable.TryGetValue(fqdn, out var symbol))
                return symbol.IsMutable;
            return false;
        }

        /// <summary>
        /// Determines if the symbol has been defined.
        /// </summary>
        /// <param name="name">The symbol name.</param>
        /// <returns><c>true</c> if the symbol has been defined, 
        /// otherwise <c>false</c>.</returns>
        public bool SymbolExists(StringView name)
            => SymbolExists(name, true);
        
        /// <summary>
        /// Determines if the symbol has been defined.
        /// </summary>
        /// <param name="name">The symbol name.</param>
        /// <param name="searchUp">Search up the scope hierarchy.</param>
        /// <returns><c>true</c> if the symbol has been defined, 
        /// otherwise <c>false</c>.</returns>
        public bool SymbolExists(StringView name, bool searchUp)
            => searchUp ? _symbolTable.ContainsKey(GetFullyQualifiedName(name, false)) :
                          _symbolTable.ContainsKey(GetScopedName(name));

        /// <summary>
        /// Gets the named symbol if defined.
        /// </summary>
        /// <param name="symbolToken">The parsed token of the symbol name.</param>
        /// <param name="raiseExceptionIfNotFound">Raise exception if the symbol is not found.</param>
        /// <returns>The <see cref="Symbol"/> if it exists.</returns>
        /// <exception cref="SymbolException"></exception>
        public Symbol GetSymbol(Token symbolToken,
                                bool raiseExceptionIfNotFound)
        {
            var searchNotFound = SearchedNotFound;
            var fqdn = GetFullyQualifiedName(symbolToken.Name);
            if (_symbolTable.ContainsKey(fqdn))
            {
                SearchedNotFound = searchNotFound;
                var sym = _symbolTable[fqdn];
                if (sym == null)
                    throw new SymbolException(symbolToken, SymbolException.ExceptionReason.IllegalReference);
                return _symbolTable[fqdn];
            }
            if (raiseExceptionIfNotFound)
                throw new SymbolException(symbolToken, SymbolException.ExceptionReason.NotDefined);
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
        /// Define a symbol as representing the current state of the program counter.
        /// </summary>
        /// <param name="token">The token representing the address name.</param>
        /// <param name="address">The address.</param>
        /// <param name="bank">The current address bank.</param>
        public void DefineSymbolicAddress(Token token, double address, int bank)
            => DefineSymbol(token, address, bank, false, false, false);

        /// <summary>
        /// Define a globally scoped symbol.
        /// </summary>
        /// <param name="name">The symbol name.</param>
        /// <param name="value">The symbol value.</param>
        /// <exception cref="SymbolException"></exception>
        public void DefineGlobal(StringView name, double value)
            => DefineSymbol(name, new Symbol(value), true);

        void DefineFromExpression(RandomAccessIterator<Token> tokens, bool isGlobal, bool isMutable)
        {
            var lhs = tokens.Current;
            var isWeak = !isGlobal && isMutable;
            var sym = SymbolExists(lhs.Name, isWeak) ? GetSymbol(lhs, false) : null;
            if (sym != null && 
                isWeak && 
                sym.IsMutable != true && 
                !GetFullyQualifiedName(lhs.Name, false).Equals(GetScopedName(lhs.Name)))
            {
                // if a non-mutable symbol of the same name exists outside of the current scope
                // and this symbol is mutable, define the mutable symbol within its scope.
                sym = null;
            }
            var isIndexed = false;
            var subscript = -1;
            var expectedStorageType = StorageType.Scalar;
            var assign = tokens.GetNext();
            if (sym != null)
            {
                if (assign?.Name.Equals("[") == true)
                {
                    isIndexed = true;
                    subscript = (int)_evaluator.Evaluate(tokens, 0, sym.Length);
                    assign = tokens.GetNext();
                }
                else
                {
                    if (sym.IsMutable != isMutable)
                        throw new SymbolException(lhs, SymbolException.ExceptionReason.MutabilityChanged);
                    expectedStorageType = sym.StorageType;
                }
            }
            if (Token.IsTerminal(assign) || !(assign.Name.Equals("=") || assign.Name.Equals(":=") || assign.IsCompoundAssignment()))
                throw new SyntaxException(assign ?? lhs, "Assignment operator expected.");

            var rhsIndex = tokens.Index;
            var rhs = tokens.GetNext();
            if (Token.IsTerminal(rhs))
                throw new SyntaxException(assign, "rhs expression is missing.");
            var rhsNext = tokens.PeekNext();
            var rhsSubscript = -1;
            var storageType = StorageType.Scalar;
            DataType dataType;
            Symbol rhsSym;
            if (rhs.Name.Equals("["))
            {
                if (isIndexed)
                    throw new SyntaxException(rhs, "Unexpected expression.");
                if (Token.IsTerminal(rhsNext))
                    throw new SyntaxException(rhs, "Array cannot be empty.");
                rhsSym = new Symbol(tokens, _evaluator, isMutable);
                expectedStorageType = sym?.StorageType ?? StorageType.Vector;
                storageType = StorageType.Vector;
                dataType = rhsSym.DataType;
            }
            else
            {
                rhsSym = SymbolExists(rhs.Name, true) ? GetSymbol(rhs, false) : null;
                if (rhsNext?.Name.Equals("[") == true)
                {
                    if (rhsSym?.StorageType != StorageType.Vector)
                        throw new SyntaxException(rhsNext, "Subscript operation invalid.");
                    if (Token.IsTerminal(tokens.GetNext()))
                        throw new SyntaxException(rhsNext, "Index expression expected for subscript.");
                    rhsSubscript = (int)_evaluator.Evaluate(tokens, 0, rhsSym.Length);
                    rhsNext = tokens.GetNext();
                }
                else if (rhsSym != null)
                {
                    if (Token.IsTerminal(tokens.GetNext()))
                    {
                        storageType = rhsSym.StorageType;
                        if (sym == null)
                            expectedStorageType = storageType;
                    }
                    else if (rhsSym.StorageType == StorageType.Vector) // ex: .let x = some_array + 2
                    {
                        throw new SyntaxException(rhsNext, "Unexpected expression.");
                    }
                }
                if (rhsSym != null && Token.IsTerminal(rhsNext))
                    dataType = rhsSym.DataType;
                else
                    dataType = !assign.IsCompoundAssignment() && rhs.IsDoubleQuote() && Token.IsTerminal(rhsNext) ? DataType.String : DataType.Numeric;
            }
            if (expectedStorageType != storageType || (sym != null && sym.DataType != dataType))
                throw new SyntaxException(rhs, "Type mismatch.");

            if (assign.IsCompoundAssignment() && (sym == null || !sym.IsMutable || expectedStorageType == StorageType.Vector))
                throw new SyntaxException(assign, "Invalid use of compound assignment operator.");

            if (expectedStorageType == StorageType.Vector)
            {
                if (sym != null)
                    sym.SetVectorTo(rhsSym);
                else
                    DefineSymbol(lhs.Name, rhsSym, isGlobal, isWeak);
            }
            else
            {
                StringView stringValue = null;
                var numericValue = double.NaN;
                if (rhsSym?.DataType == DataType.String && Token.IsTerminal(rhsNext))
                {
                    stringValue = rhsSubscript > -1 ? rhsSym.StringVector[rhsSubscript] : rhsSym.StringValue;
                }
                else if (rhsSym?.DataType == DataType.Numeric && Token.IsTerminal(rhsNext))
                {
                    numericValue = rhsSubscript > -1 ? rhsSym.NumericVector[rhsSubscript] : rhsSym.NumericValue;
                }
                else if (dataType == DataType.String)
                {
                    stringValue = rhs.Name.TrimOnce('"');
                }
                else
                {
                    // reset the token iterator back to the beginning of the rhs expression
                    tokens.SetIndex(rhsIndex);
                    numericValue = _evaluator.Evaluate(tokens);
                    if (assign.IsCompoundAssignment())
                    {
                        // break compound expression <sym> <op>= <rhs> into:
                        //                           <sym_val> <op> <rhs>
                        var compoundExpression = new List<Token>();
                        if (isIndexed)
                            compoundExpression.Add(new Token(sym.NumericVector[subscript].ToString(), TokenType.Operand));
                        else
                            compoundExpression.Add(new Token(sym.NumericValue.ToString(), TokenType.Operand));
                        compoundExpression.Add(new Token(assign.Name[0..^2], TokenType.Binary));
                        compoundExpression.Add(new Token(numericValue.ToString(), TokenType.Operand));
                        numericValue = _evaluator.Evaluate(compoundExpression.GetIterator());
                    }
                }
                if (sym != null)
                {
                    if (isIndexed)
                    {
                        if (dataType == DataType.String)
                            sym.StringVector[subscript] = stringValue;
                        else
                            sym.NumericVector[subscript] = numericValue;
                    }
                    else if (dataType == DataType.String)
                        sym.StringValue = stringValue;
                    else
                        sym.NumericValue = numericValue;
                }
                else
                {
                    if (dataType == DataType.String)
                        DefineSymbol(lhs.Name, new Symbol(stringValue, isMutable), isGlobal, isWeak);
                    else
                        DefineSymbol(lhs.Name, new Symbol(numericValue, isMutable), isGlobal, isWeak);
                }
            }
        }

        /// <summary>
        /// Define a scoped symbol.
        /// </summary>
        /// <param name="tokens">The tokenized symbol assignment expression (lhs and rhs). This operation 
        /// will advance the token iteration through to a terminating token.</param>
        /// <exception cref="SymbolException"></exception>
        /// <exception cref="SyntaxException"></exception>
        public void DefineSymbol(RandomAccessIterator<Token> tokens)
            => DefineFromExpression(tokens, false, true);

        /// <summary>
        /// Define a scoped symbol.
        /// </summary>
        /// <param name="name">The symbol name.</param>
        /// <param name="value">The symbol's value.</param>
        /// <exception cref="SymbolException"></exception>
        public void DefineSymbol(StringView name, double value)
            => DefineSymbol(name, new Symbol(value), false);

        /// <summary>
        /// Define a scoped symbol.
        /// </summary>
        /// <param name="name">The symbol name.</param>
        /// <param name="value">The symbol's value.</param>
        /// <param name="isMutable">Whether the symbol is mutable.</param>
        /// <exception cref="SymbolException"></exception>
        public void DefineSymbol(StringView name, double value, bool isMutable)
            => DefineSymbol(name, new Symbol(value, isMutable), false);

        /// <summary>
        /// Define a scoped symbol.
        /// </summary>
        /// <param name="name">The symbol name.</param>
        /// <param name="value">The symbol's value.</param>
        /// <exception cref="SymbolException"></exception>
        public void DefineSymbol(StringView name, StringView value)
            => DefineSymbol(name, new Symbol(value), false);

        /// <summary>
        /// Define a scoped symbol.
        /// </summary>
        /// <param name="name">The symbol name.</param>
        /// <param name="value">The symbol's value.</param>
        /// <param name="isMutable">Whether the symbol is mutable.</param>
        /// <exception cref="SymbolException"></exception>
        public void DefineSymbol(StringView name, StringView value, bool isMutable)
            => DefineSymbol(name, new Symbol(value, isMutable), false);


        /// <summary>
        /// Define a symbol.
        /// </summary>
        /// <param name="token">The token comprising the symbol name.</param>
        /// <param name="value">The symbol's value.</param>
        /// <param name="isMutable">Whether the symbol is mutable.</param>
        /// <param name="isGlobal">The symbol is globally scoped.</param>
        /// <param name="isWeak">The symbol's scope is weak.</param>
        /// <exception cref="SymbolException"></exception>
        public void DefineSymbol(Token token, StringView value, bool isGlobal, bool isMutable, bool isWeak = false)
        {
            var sym = SymbolExists(token.Name, isWeak) ? GetSymbol(token, false) : null;
            if (sym == null)
                DefineSymbol(token.Name, new Symbol(value, isMutable), isGlobal, isWeak);
            else if (sym.DataType == DataType.String && sym.StorageType == StorageType.Scalar && sym.IsMutable == isMutable)
                sym.StringValue = value;
            else
                throw new SymbolException(token, SymbolException.ExceptionReason.Redefined);
        }

        /// <summary>
        /// Define a symbol.
        /// </summary>
        /// <param name="token">The token comprising the symbol name.</param>
        /// <param name="value">The symbol's value.</param>
        /// <param name="isMutable">Whether the symbol is mutable.</param>
        /// <param name="isGlobal">The symbol is globally scoped.</param>
        /// <param name="isWeak">The symbol's scope is weak.</param>
        /// <exception cref="SymbolException"></exception>
        public void DefineSymbol(Token token, double value, int bank, bool isGlobal, bool isMutable, bool isWeak = false)
        {
            var sym = SymbolExists(token.Name, isWeak) ? GetSymbol(token, false) : null;
            if (sym == null)
            {
                if (isMutable)
                    DefineSymbol(token.Name, new Symbol(value, true), isGlobal, isWeak);
                else
                    DefineSymbol(token.Name, new Symbol(value, bank), isGlobal, isWeak);
                
            }
            else if (sym.IsNumeric && sym.StorageType == StorageType.Scalar && sym.IsMutable == isMutable)
                sym.NumericValue = value;
            else
                throw new SymbolException(token, SymbolException.ExceptionReason.Redefined);
        }

        /// <summary>
        /// Define a symbol.
        /// </summary>
        /// <param name="name">The symbol name.</param>
        /// <param name="tokens">The tokenized symbol assignment expression (rhs).</param>
        /// <param name="isMutable">Whether the symbol is mutable.</param>
        /// <param name="isGlobal">The symbol is globally scoped.</param>
        /// <param name="isWeak">The symbol's scope is weak.</param>
        /// <exception cref="SyntaxException"></exception>
        public void DefineSymbol(StringView name, RandomAccessIterator<Token> tokens, bool isMutable, bool isGlobal, bool isWeak = false)
        {
            if (Token.IsTerminal(tokens.PeekNext()))
                throw new SyntaxException(tokens.Current, "Array cannot be empty.");
            var arraySym = new Symbol(tokens, _evaluator, isMutable);
            if (!Token.IsTerminal(tokens.Current))
                throw new SyntaxException(tokens.Current, "Unexpected expression.");
            DefineSymbol(name, arraySym, isGlobal, isWeak);
        }

        /// <summary>
        /// Reserve a valueless symbol in the symbol table to prevent other symbol definitions of the same name.
        /// </summary>
        /// <param name="name">The symbol name.</param>
        /// <exception cref="SymbolException"></exception>
        public void DefineVoidSymbol(StringView name)
            => DefineSymbol(name, null, false, false);

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
            _localScopes.Push(string.Empty);
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
                    _localScopes.Pop();
                    _referenceTableIndexStack.Pop();
                }
            }
        }

        /// <summary>
        /// Reset the symbol manager's internal state.
        /// </summary>
        public void Reset()
        {
            foreach (var symbol in _symbolTable.Where(s => s.Value?.IsMutable == true))
                _symbolTable.Remove(symbol.Key);

            _localScopes.Clear();
            _localScopes.Push(string.Empty);
            _referenceTableCounter = 0;
            SearchedNotFound = false;
        }

        /// <summary>
        /// Get a string listing of all defined label symbols.
        /// </summary>
        /// <param name="listAll">List all labels, including non-addresses.</param>
        /// <returns>The string listing.</returns>
        public string ListLabels(bool listAll)
        {       //ADD2PLYSTAT                       = 4783         // $12af
            var listBuilder = new StringBuilder(
                "/*************************************************************/\n" +
                "/* Symbol                            Value                   */\n"+
                "/*************************************************************/\n");
            var labels = _symbolTable.Where(s => !s.Key.Equals("CURRENT_PASS") &&
                                                 (char.IsLetter(s.Key[0]) || s.Key[0] == '_') &&
                                                 (s.Value == null || 
                                                 (s.Value.StorageType == StorageType.Scalar &&
                                                 (listAll || s.Value.DataType == DataType.Address))))
                                     .OrderBy(s => s.Key);
            foreach (var label in labels)
            {
                var name = label.Key;
                var paths = name.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                var nonAnons = new List<string>();
                foreach(var path in paths)
                {
                    if (char.IsLetter(path[0]) || path[0] == '_')
                        nonAnons.Add(path);
                    else
                        nonAnons.Add(".");
                }
                name = string.Join('.', nonAnons);
                if (label.Value != null)
                {
                    listBuilder.Append(name.Elliptical(33).PadRight(33)).Append(" = ");
                    switch (label.Value.DataType)
                    {
                        case DataType.String:
                            listBuilder.Append($"\"{label.Value.StringValue}\"");
                            break;
                        default:
                            listBuilder.Append($"{label.Value.NumericValue,-12} // ${(int)label.Value.NumericValue + (label.Value.Bank * 0x10000):x}");
                            break;
                    }
                }
                else
                {
                    listBuilder.Append(name.Elliptical(33));
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
            get => _localScopes.Peek();
            set
            {
                if (value[0] != '_')
                {
                    _localScopes.Pop();
                    _localScopes.Push(value);
                }
            }
        }

        /// <summary>
        /// Gets whether any symbols were searched but not found.
        /// </summary>
        public bool SearchedNotFound { get; private set; }

        #endregion
    }
}