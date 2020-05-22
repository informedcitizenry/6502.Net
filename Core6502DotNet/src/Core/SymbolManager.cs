//-----------------------------------------------------------------------------
// Copyright (c) 2017-2020 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;

namespace Core6502DotNet
{
    /// <summary>
    /// An enumeration representing symbol types.
    /// </summary>
    public enum SymbolType
    {
        Scalar,
        Vector,
        Hash,
        NonScalarVector,
        NonScalarHash
    };

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
            NotValid,
            InvalidBackReference,
            InvalidForwardReference,
            Scalar
        }

        static readonly Dictionary<ExceptionReason, string> _reasonMessages = new Dictionary<ExceptionReason, string>
        {
            { ExceptionReason.Redefined,                "Cannot assign \"{0}\" after it has been assigned."           },
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
        public SymbolException(string symbolName, int position, ExceptionReason reason)
            : base(string.Format(_reasonMessages[reason], symbolName))
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
            : base(string.Format(_reasonMessages[reason], token.Name))
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
        public string SymbolName { get; set; }

        public int Position { get; set; }
    }

    /// <summary>
    /// A class managing all valid assembly symbols, including their scope and values.
    /// </summary>
    public class SymbolManager : IFunctionEvaluator
    {
        #region Subclasses

        class Symbol : SymbolBase
        {
            public double NumericValue { get; set; }

            public string StringValue { get; set; }

            public SymbolType SymbolType { get; set; }

            public Dictionary<int, double> NumericVector { get; set; }

            public Dictionary<int, string> StringVector { get; set; }

            public Dictionary<string, double> FloatHash { get; set; }

            public Dictionary<string, string> StringHash { get; set; }

            public Dictionary<int, Symbol> SymbolVector { get; set; }

            public Dictionary<string, Symbol> SymbolHash { get; set; }

            public int DefinedAtBank { get; private set; }

            public bool IsMutable { get; set; }

            public int DefinedAtIndex { get; private set; }

            public Symbol()
            {
                DataType = DataType.None;
                SymbolType = SymbolType.Scalar;
                IsMutable = false;
                NumericValue = 0D;
                StringValue = string.Empty;
                DefinedAtBank = 0;
                if (Assembler.LineIterator == null)
                    DefinedAtIndex = -1;
                else
                    DefinedAtIndex = Assembler.LineIterator.Index;
            }

            public Symbol(string name)
                : this() => Name = name;

            public Symbol(string name, bool mutable)
                : this(name) => IsMutable = mutable;

            public Symbol(string name, int address)
                : this(name) => SetValue(address);

            public Symbol(string name, bool mutable, double value)
                : this(name, mutable) => SetValue(value);

            public Symbol(string name, bool mutable, string value)
                : this(name, mutable) => SetValue(value);

            public Symbol(string name, bool mutable, IEnumerable<double> value)
                : this(name, mutable) => SetValue(value);

            public Symbol(string name, bool mutable, IEnumerable<string> value)
                : this(name, mutable) => SetValue(value);

            public Symbol(string name, bool mutable, Symbol other)
                : this(name, mutable)
            {
                DataType = other.DataType;
                SymbolType = other.SymbolType;
                if (IsScalar())
                {
                    StringValue = new string(other.StringValue);
                    NumericValue = other.NumericValue;
                }
                else
                {
                    if (DataType == DataType.String)
                        StringVector = new Dictionary<int, string>(other.StringVector);
                    else
                        NumericVector = new Dictionary<int, double>(other.NumericVector);
                }
            }

            public void SetValue(string value)
            {
                SymbolType = SymbolType.Scalar;
                DataType = DataType.String;
                StringValue = value;
            }

            public void SetValue(int address)
            {
                DataType = DataType.Address;
                DefinedAtBank = Assembler.Output.CurrentBank;
                NumericValue = address;
            }

            public void SetValue(double value)
            {
                SymbolType = SymbolType.Scalar;
                DataType = DataType.Numeric;
                NumericValue = value;
            }

            public void SetValue(IEnumerable<string> values)
            {
                SymbolType = SymbolType.Vector;
                DataType = DataType.String;
                StringVector = new Dictionary<int, string>();
                int i = 0;
                foreach (var value in values)
                    StringVector[i++] = value;
            }

            public void SetValue(IEnumerable<int> addresses)
            {
                SymbolType = SymbolType.Vector;
                DataType = DataType.Address;
                NumericVector = new Dictionary<int, double>();
                int i = 0;
                foreach (var addr in addresses)
                    NumericVector[i++] = addr;
            }

            public void SetValue(IEnumerable<double> values)
            {
                SymbolType = SymbolType.Vector;
                DataType = DataType.Numeric;
                NumericVector = new Dictionary<int, double>();
                int i = 0;
                foreach (var value in values)
                    NumericVector[i++] = value;
            }

            public void SetValue(IDictionary<string, string> values)
            {
                SymbolType = SymbolType.Hash;
                DataType = DataType.String;
                StringHash = new Dictionary<string, string>(values);
            }

            public void SetValue(IDictionary<string, double> values)
            {
                SymbolType = SymbolType.Hash;
                DataType = DataType.Numeric;
                FloatHash = new Dictionary<string, double>(values);
            }

            public void SetValueFromSymbol(Symbol other)
            {
                DataType = other.DataType;
                NumericValue = other.NumericValue;
                StringValue = other.StringValue;
                SymbolType = other.SymbolType;

                if (other.SymbolType != SymbolType.Scalar)
                {
                    if (other.SymbolType == SymbolType.NonScalarVector)
                    {
                        SymbolVector = new Dictionary<int, Symbol>(other.SymbolVector);
                    }
                    else if (other.SymbolType == SymbolType.NonScalarHash)
                    {
                        SymbolHash = new Dictionary<string, Symbol>(other.SymbolHash);
                    }
                    else
                    {
                        switch (DataType)
                        {
                            case DataType.Address:
                            case DataType.Numeric:
                                if (SymbolType == SymbolType.Vector)
                                    NumericVector = new Dictionary<int, double>(other.NumericVector);
                                else
                                    FloatHash = new Dictionary<string, double>(other.FloatHash);
                                if (DataType == DataType.Address)
                                    DefinedAtBank = other.DefinedAtBank;
                                break;
                            default:
                                if (SymbolType == SymbolType.Vector)
                                    StringVector = new Dictionary<int, string>(other.StringVector);
                                else
                                    StringHash = new Dictionary<string, string>(other.StringHash);
                                break;
                        }
                    }
                }
            }


            public void SetValue(double value, int index)
            {
                SymbolType = SymbolType.Vector;
                if (NumericVector == null)
                    NumericVector = new Dictionary<int, double>();
                NumericVector[index] = value;
            }

            public void SetValue(string value, int index)
            {
                SymbolType = SymbolType.Vector;
                if (StringVector == null)
                    StringVector = new Dictionary<int, string>();
                StringVector[index] = value;
            }

            public void SetValue(Symbol value, int index)
            {
                SymbolType = SymbolType.Vector;
                if (SymbolVector == null)
                    SymbolVector = new Dictionary<int, Symbol>();
                SymbolVector[index] = value;
            }

            public void SetValue(double value, string key)
            {
                SymbolType = SymbolType.Hash;
                if (FloatHash == null)
                    FloatHash = new Dictionary<string, double>();
                FloatHash[key] = value;
            }

            public void SetValue(string value, string key)
            {
                SymbolType = SymbolType.Hash;
                if (StringHash == null)
                    StringHash = new Dictionary<string, string>();
                StringHash[key] = value;
            }

            public void SetValue(Symbol value, string key)
            {
                SymbolType = SymbolType.Hash;
                if (SymbolHash == null)
                    SymbolHash = new Dictionary<string, Symbol>();
                SymbolHash[key] = value;
            }

            public double GetNumericValueAtIndex(int index)
            {
                if (SymbolType != SymbolType.Vector)
                    return double.NaN;
                if (index < 0 || NumericVector == null || index >= NumericVector.Count)
                    return double.NegativeInfinity;
                return NumericVector[index];
            }

            public string GetStringValueAtIndex(int index)
            {
                if (SymbolType != SymbolType.Vector)
                    return string.Empty;
                if (index < 0 || StringVector == null || index >= StringVector.Count)
                    return null;
                return StringVector[index];
            }

            public double GetNumericValue()
            {
                switch (DataType)
                {
                    case DataType.String:
                        return double.NaN;
                    case DataType.Address:
                        if (Assembler.Output.CurrentBank == DefinedAtBank)
                            return NumericValue;
                        return (int)NumericValue | (DefinedAtBank * 0x10000);
                    default:
                        return NumericValue;
                }
            }

            public override string ToString()
            {
                if (SymbolType != SymbolType.Scalar)
                    return "[Object]";
                switch (DataType)
                {
                    case DataType.Numeric:
                    case DataType.Address:
                        return NumericValue.ToString();
                    default: 
                        return StringValue;
                }
            }

            public bool IsScalar() => SymbolType == SymbolType.Scalar;

            public bool IsValueEqual(Symbol other)
            {
                if (SymbolType == other.SymbolType &&
                    DataType == other.DataType)
                {
                    if (SymbolType == SymbolType.Scalar)
                    {
                        switch (DataType)
                        {
                            case DataType.Address:
                                return GetNumericValue() == other.GetNumericValue();
                            case DataType.Numeric:
                                return NumericValue == other.NumericValue;
                            default:
                                return StringValue.Equals(other.StringValue);
                        }
                    }
                    else
                    {
                        if (!IsMutable)
                            return true;
                        switch (DataType)
                        {
                            case DataType.Numeric:
                                if (NumericVector != null && other.NumericVector != null) 
                                    return NumericVector.SequenceEqual(other.NumericVector);
                                break;
                            case DataType.String:
                                if (StringVector != null && other.StringVector != null)
                                    return StringVector.SequenceEqual(other.StringVector);
                                break;
                        }
                    }
                }
                return false;
            }

            public int Length
            {
                get
                {
                    if (IsScalar())
                    {
                        if (DataType == DataType.String)
                            return StringValue.Length;
                        return 1;
                    }
                    if (DataType == DataType.String)
                        return StringVector.Count;
                    return NumericVector.Count;
                }
            }
        }

        class LineReferenceStackFrame
        {
            readonly List<Symbol> _lineReferences;
            readonly LineReferenceStackFrame _parent;

            public LineReferenceStackFrame(LineReferenceStackFrame parent)
            {
                _parent = parent;
                _lineReferences = new List<Symbol>();
            }

            public double GetLineReferenceValue(string name)
            {
                // cannot evaluate on forward references on first pass
                if (name[0] == '+')
                {
                    if (Assembler.CurrentPass == 0)
                    {
                        Assembler.PassNeeded = true;
                        return Assembler.Output.LogicalPC;
                    }
                }
                var places = name.Length;
                var lastIndex = Assembler.LineIterator.Index;
                var nextIndex = 0;
                Symbol anonymous = null;
                while (places > 0 && lastIndex >= 0)
                {

                    if (name[0] == '-')
                    {
                        anonymous = _lineReferences.LastOrDefault(s => s.Name[0] == name[0] && s.DefinedAtIndex >= 0 && s.DefinedAtIndex <= lastIndex);
                        nextIndex = anonymous == null ? -1 : anonymous.DefinedAtIndex;
                        lastIndex = nextIndex - 1;
                    }
                    else
                    {
                        anonymous = _lineReferences.FirstOrDefault(s => s.Name[0] == name[0] && s.DefinedAtIndex >= lastIndex);
                        nextIndex = anonymous == null ? -1 : anonymous.DefinedAtIndex;
                        lastIndex = nextIndex + 1;
                    }
                    if (anonymous == null)
                    {
                        if (_parent != null)
                            return _parent.GetLineReferenceValue(name.Substring(0, places));
                        if (name[0] == '+')
                            throw new SymbolException(name, 0, SymbolException.ExceptionReason.InvalidForwardReference);
                        throw new SymbolException(name, 0, SymbolException.ExceptionReason.InvalidBackReference);
                    }
                    else
                        places--;
                }
                return anonymous.NumericValue;
            }

            public void DefineLineReferenceValue(string name, double value)
            {
                var reference = _lineReferences.FirstOrDefault(s => s.Name[0] == name[0] &&
                                                                 s.DefinedAtIndex == Assembler.LineIterator.Index);
                if (reference != null)
                {
                    if (reference.GetNumericValue() != value)
                    {
                        reference.SetValue(value);
                        Assembler.PassNeeded = true;
                    }
                }
                else
                {
                    _lineReferences.Add(new Symbol(name, false, value));
                }
            }
        }

        #endregion

        #region Members

        Dictionary<string, Symbol> _symbols;
        Dictionary<string, Symbol> _constants;
        readonly Stack<string> _scope;
        readonly Stack<int> _referenceFrameIndexStack;
        readonly List<Func<string, bool>> _criteria;
        readonly List<LineReferenceStackFrame> _lineReferenceFrames;
        int _referenceFramesCounter, _ephemeralCounter;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a new instance of a symbol manager class.
        /// </summary>
        public SymbolManager()
        {

            Assembler.PassChanged += ProcessPassChange;

            _scope = new Stack<string>();
            _symbols = new Dictionary<string, Symbol>(Assembler.StringComparer);
            _constants = new Dictionary<string, Symbol>(Assembler.StringComparer);
            _referenceFrameIndexStack = new Stack<int>();
            _referenceFrameIndexStack.Push(0);

            _lineReferenceFrames = new List<LineReferenceStackFrame>
            {
                new LineReferenceStackFrame(null)
            };

            Local = string.Empty;

            _criteria = new List<Func<string, bool>>
            {
                s =>
                {
                      return s.Equals("+") ||
                             s.Equals("-") ||
                              ((s[0] == '_' || char.IsLetter(s[0])) &&
                              ((char.IsLetterOrDigit(s[^1]) || s[^1] == '_') && !s.Contains('.')));
                },
                s =>!_constants.ContainsKey(s)
            };
            _referenceFramesCounter = 0;
        }

        #endregion

        #region Methods

        void ProcessPassChange(object sender, EventArgs args)
        {
            // remove mutable symbols on pass change
            IEnumerable<string> mutables = _symbols.Keys.Where(k => _symbols[k].IsMutable);
            foreach (var key in mutables)
                _symbols.Remove(key);

            // reset the anonymous frames counter
            _referenceFramesCounter = 0;
        }

        /// <summary>
        /// Add a criterion by which a symbol's name is considered valid.
        /// </summary>
        /// <param name="criterion">The criterion function.</param>
        public void AddValidSymbolNameCriterion(Func<string, bool> criterion) => _criteria.Add(criterion);

        string GetScopedName(string name) => GetAncestor(name, 0);

        string GetAncestor(string name, int back)
        {
            var symbolPath = new List<string>();
            if (_scope.Count > 0)
            {
                if (back > _scope.Count)
                    return name;
                symbolPath.AddRange(_scope.ToList().Skip(back).Reverse());
            }
            if (!string.IsNullOrEmpty(Local) && name[0] == '_')
                symbolPath.Add(Local + name);
            else
                symbolPath.Add(name);
            return string.Join('.', symbolPath);
        }

        bool DefineSymbol(Symbol symbol, bool isGlobal)
        {

            if (_criteria.Any(f => !f(symbol.Name)))
                throw new SymbolException(symbol.Name, 0, SymbolException.ExceptionReason.NotValid);
            var fqdn = GetScopedName(symbol.Name);
            var exists = _symbols.ContainsKey(fqdn);

            if (symbol.Name[0] != '_' && !isGlobal && !symbol.IsMutable)
                Local = symbol.Name;

            if (exists)
            {
                var sym = _symbols[fqdn];
                if ((!sym.IsMutable &&
                    ((sym.DefinedAtIndex == -1 && Assembler.LineIterator == null) || sym.DefinedAtIndex != Assembler.LineIterator.Index)) ||
                    sym.DataType != symbol.DataType)
                {
                    throw new SymbolException(symbol.Name, 0, SymbolException.ExceptionReason.Redefined);
                }
                if (!sym.IsValueEqual(symbol))
                {
                    // update the existing symbol
                    if (symbol.IsScalar() && !sym.IsScalar())
                        throw new SymbolException(symbol.Name, 0, SymbolException.ExceptionReason.NonScalar);
                    else if (!symbol.IsScalar() && sym.IsScalar())
                        throw new SymbolException(symbol.Name, 0, SymbolException.ExceptionReason.Scalar);
                    sym.SetValueFromSymbol(symbol);

                    // signal to the assembler another pass is needed.
                    if (!sym.IsMutable && !Assembler.PassNeeded)
                        Assembler.PassNeeded = true;
                }
            }
            else
            {
                _symbols[fqdn] = symbol;
            }
            return exists;
        }

        bool DefineFromTokens(Token lhs, IEnumerable<Token> rhs, bool isMutable, bool isGlobal, Symbol arrayElementToUpdate, int subscriptix)
        {
            var tokenList = rhs.ToList();

            var symbolName = lhs.Name;
            if (arrayElementToUpdate != null)
            {
                if (!arrayElementToUpdate.IsMutable)
                {
                    Assembler.Log.LogEntry(Assembler.CurrentLine, tokenList[0].Position,
                      $"Symbol \"{symbolName}\" cannot be re-defined.");
                    return false;
                }
                if (subscriptix > -1)
                {
                    if (arrayElementToUpdate.IsScalar())
                        throw new SymbolException(lhs, SymbolException.ExceptionReason.Scalar);
                    if (
                        (arrayElementToUpdate.DataType == DataType.String &&
                         !arrayElementToUpdate.StringVector.ContainsKey(subscriptix)
                        ) ||
                        (arrayElementToUpdate.DataType == DataType.Numeric &&
                         !arrayElementToUpdate.NumericVector.ContainsKey(subscriptix)
                        )
                       )
                    {
                        Assembler.Log.LogEntry(Assembler.CurrentLine, tokenList[0].Position,
                            "Argument out of range.");
                        return false;
                    }
                }
            }
            bool valueIsArray = tokenList[0].Name.Equals("[");
            if (valueIsArray)
            {
                if (tokenList.Count > 1)
                {
                    var unexpected = tokenList[1];
                    Assembler.Log.LogEntry(Assembler.CurrentLine, unexpected.Position,
                        $"Unknown expression \"{unexpected}\".");
                    return false;
                }
                var array = tokenList[0].Children;
                if (array == null || array.Count == 0 || !array[0].HasChildren)
                {
                    Assembler.Log.LogEntry(Assembler.CurrentLine, tokenList[1].Position,
                        "Array definition cannot be an empty list.");
                    return false;
                }
                var firstInAray = array[0].Children[0];
                if (firstInAray.Name.EnclosedInDoubleQuotes())
                {
                    var value = new List<string>(array.Count);
                    foreach (var child in array)
                    {
                        if (child.HasChildren)
                        {
                            if (child.Children.Count > 1 || !child.Children[0].Name.EnclosedInDoubleQuotes())
                            {
                                Assembler.Log.LogEntry(Assembler.CurrentLine, child.Children[0].Position,
                                    "Expected string literal.");
                                return false;
                            }
                            value.Add(child.Children[0].Name.TrimOnce('"'));
                        }

                    }
                    if (arrayElementToUpdate != null)
                    {
                        if (arrayElementToUpdate.DataType != DataType.String)
                        {
                            Assembler.Log.LogEntry(Assembler.CurrentLine, firstInAray.Position,
                                $"Type mismatch.");
                            return false;
                        }
                        arrayElementToUpdate.SetValue(new Symbol(string.Empty, isMutable, value), subscriptix);
                    }
                    else
                    {
                        return DefineSymbol(new Symbol(symbolName, isMutable, value), isGlobal);
                    }
                }
                else
                {
                    var value = new List<double>(array.Count);
                    foreach (var f in array)
                        value.Add(Evaluator.Evaluate(f));
                    if (arrayElementToUpdate != null)
                    {
                        if (arrayElementToUpdate.DataType != DataType.Numeric)
                        {
                            Assembler.Log.LogEntry(Assembler.CurrentLine, firstInAray.Position,
                                $"Type mismatch.");
                            return false;
                        }
                        arrayElementToUpdate.SetValue(new Symbol(string.Empty, isMutable, value), subscriptix);
                    }
                    else
                        return DefineSymbol(new Symbol(symbolName, isMutable, value), isGlobal);
                }

            }
            else
            {
                if (tokenList[0].Name.EnclosedInDoubleQuotes() && tokenList.Count == 1)
                {
                    var value = tokenList[0].Name.TrimOnce('"');
                    if (arrayElementToUpdate.DataType != DataType.String)
                    {
                        Assembler.Log.LogEntry(Assembler.CurrentLine, tokenList[0].Position,
                                    $"Type mismatch.");
                        return false;
                    }
                    if (arrayElementToUpdate != null)
                        arrayElementToUpdate.SetValue(value);
                    else
                        return DefineSymbol(new Symbol(symbolName, isMutable, value), isGlobal);
                }
                else
                {
                    if (tokenList.Count == 1 && (char.IsLetter(tokenList[0].Name[0]) || tokenList[0].Name[0] == '_'))
                    {
                        var symbolLookup = Lookup(tokenList[0], false);
                        if (symbolLookup != null)
                            return DefineSymbol(new Symbol(symbolName, isMutable, symbolLookup), isGlobal);
                    }
                    var value = Evaluator.Evaluate(tokenList);
                    if (arrayElementToUpdate != null)
                    {
                        if (arrayElementToUpdate.DataType != DataType.Numeric)
                        {
                            Assembler.Log.LogEntry(Assembler.CurrentLine, tokenList[0].Position,
                                $"Type mismatch.");
                            return false;
                        }
                        arrayElementToUpdate.SetValue(value);
                    }
                    else
                        return DefineSymbol(new Symbol(symbolName, isMutable, value), isGlobal);
                }
            }
            return true;
        }

        bool DefineFromTokens(Token lhs, IEnumerable<Token> rhs, bool isMutable, bool isGlobal)
            => DefineFromTokens(lhs, rhs, isMutable, isGlobal, null, -1);

        bool DefineFromTokens(Token lhs, Token rhs, bool isMutable, bool isGlobal)
            => DefineFromTokens(lhs, rhs.Children, isMutable, isGlobal);

        bool DefineFromTokens(IEnumerable<Token> tokens, bool isMutable, bool isGlobal)
        {
            var tokenList = tokens.ToList();
            if (tokenList.Count < 3)
                throw new ExpressionException(tokenList[0], $"Assignment error.");


            var isSubscript = tokenList[1].Name.Equals("[");
            var arrayElementToUpdate = isSubscript ? Lookup(tokenList[0]) : null;
            var subscriptix = isSubscript ? (int)Evaluator.Evaluate(tokenList[1].Children, uint.MinValue, int.MaxValue) : -1;
            var assignIx = isSubscript ? 2 : 1;
            var assignment = tokenList[assignIx];

            if (assignment.OperatorType != OperatorType.Binary ||
                (!assignment.Name.Equals("=")))
            {
                Assembler.Log.LogEntry(Assembler.CurrentLine, assignment.Position,
                    $"Unrecognized assignment operator \"{assignment.Name}\".");
                return false;
            }
            if (tokenList.Count < 3)
            {
                Assembler.Log.LogEntry(Assembler.CurrentLine, tokenList[0].Position,
                    "Missing rhs in assignment.");
                return false;
            }
            return DefineFromTokens(tokenList[0], tokenList.Skip(assignIx + 1), isMutable, isGlobal, arrayElementToUpdate, subscriptix);
        }

        string GetFullyQualifiedName(string name)
        {
            var scopedName = GetScopedName(name);
            var i = 0;
            while (!_symbols.ContainsKey(scopedName))
            {
                scopedName = GetAncestor(name, ++i);
                if (i > _scope.Count)
                    break;
            }
            return scopedName;
        }

        Symbol Lookup(Token symbolToken, bool raiseExceptionIfNotFound)
        {
            var name = symbolToken.Name;
            if (_constants.ContainsKey(name))
                return _constants[name];
            var fqdn = GetFullyQualifiedName(name);
            if (_symbols.ContainsKey(fqdn))
                return _symbols[fqdn];
            Assembler.PassNeeded = true;
            if (raiseExceptionIfNotFound)
                throw new SymbolException(symbolToken, SymbolException.ExceptionReason.NotDefined);
            return null;
        }

        Symbol Lookup(Token symbolToken) => Lookup(symbolToken, true);

        /// <summary>
        /// Determines if the symbol has been defined.
        /// </summary>
        /// <param name="name">The symbol name.</param>
        /// <returns><c>True</c> if the symbol has been defined, 
        /// otherwise <c>false</c>.</returns>
        public bool SymbolExists(string name) => _symbols.ContainsKey(GetFullyQualifiedName(name));

        /// <summary>
        /// Pushes an ephemeral scope onto the stack. Used for function invocations.
        /// </summary>
        public void PushScopeEphemeral() => _scope.Push($"@{_ephemeralCounter++}");

        /// <summary>
        /// Pushes the scope onto the stack. If the passed name is 
        /// an empty string, the scope is considered unnamed and symbols defined 
        /// within it not be accessible outside of it.
        /// </summary>
        /// <param name="name">The scope's name.</param>
        public void PushScope(string name)
        {
            _scope.Push(name);

            if (Assembler.CurrentPass == 0)
            {
                LineReferenceStackFrame parent = _lineReferenceFrames[_referenceFrameIndexStack.Peek()];
                _lineReferenceFrames.Add(new LineReferenceStackFrame(parent));
            }
            _referenceFrameIndexStack.Push(++_referenceFramesCounter);
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
                    IEnumerable<string> ephemerals = _symbols.Keys.Where(k => k.Contains(sc, StringComparison.Ordinal));
                    foreach (var key in ephemerals)
                        _symbols.Remove(key);
                }
                else
                {
                    Local = string.Empty;
                    _referenceFrameIndexStack.Pop();
                }
            }
        }


        /// <summary>
        /// Define a global constant symbol whose name is unique for all scopes.
        /// </summary>
        /// <param name="name">Constant name.</param>
        /// <param name="value">Constant value.</param>
        public void DefineConstant(string name, string value)
            => _constants.Add(name, new Symbol(name, false, value));

        /// <summary>
        /// Define a global constant symbol whose name is unique for all scopes.
        /// </summary>
        /// <param name="name">Constant name.</param>
        /// <param name="value">Constant value.</param>
        public void DefineConstant(string name, double value)
            => _constants.Add(name, new Symbol(name, false, value));

        /// <summary>
        /// Define a globally scoped symbol.
        /// </summary>
        /// <param name="symbol">The token that contains the symbol name.</param>
        /// <param name="array">The token that contains the array of values for the non-scalar symbol.</param>
        /// <returns><c>True</c> if the symbol could be defined, otherwise <c>false</c>.</returns>
        public bool DefineGlobal(Token symbol, Token array) => DefineFromTokens(symbol, array, false, true);

        /// <summary>
        /// Define a globally scoped symbol.
        /// </summary>
        /// <param name="symbol">The token that contains the symbol name.</param>
        /// <param name="array">The token that contains the array of values for the non-scalar symbol.</param>
        /// <param name="isMutable">A flag indicating whether the symbol should be treated as a mutable
        /// (variable).</param>
        /// <returns><c>True</c> if the symbol could be defined, otherwise <c>false</c>.</returns>
        public bool DefineGlobal(Token symbol, Token array, bool isMutable)
            => DefineFromTokens(symbol, array, isMutable, true);

        /// <summary>
        /// Define a globally scoped symbol.
        /// </summary>
        /// <param name="tokens">The tokens that contain the definition expression.</param>
        /// (variable).</param>
        /// <returns><c>True</c> if the symbol could be defined, otherwise <c>false</c>.</returns>
        public bool DefineGlobal(IEnumerable<Token> tokens) => DefineFromTokens(tokens, false, true);

        /// <summary>
        /// Define a globally scoped symbol.
        /// </summary>
        /// <param name="tokens">The tokens that contain the definition expression.</param>
        /// <param name="IsMutable">A flag indicating whether the symbol should be treated as a mutable
        /// (variable).</param>
        /// <returns><c>True</c> if the symbol could be defined, otherwise <c>false</c>.</returns>
        public bool DefineGlobal(IEnumerable<Token> tokens, bool IsMutable) => DefineFromTokens(tokens, IsMutable, true);

        /// <summary>
        /// Define a globally scoped symbol.
        /// </summary>
        /// <param name="name">The name of the symbol.</param>
        /// <param name="value">The symbol's floating point value.</param>
        /// <returns><c>True</c> if the symbol could be defined, otherwise <c>false</c>.</returns>
        public bool DefineGlobal(string name, double value) => DefineSymbol(new Symbol(name, false, value), true);

        /// <summary>
        /// Define a globally scoped symbol.
        /// </summary>
        /// <param name="name">The name of the symbol.</param>
        /// <param name="value">The symbol's integral value.</param>
        /// <param name="isMutable">A flag indicating whether the symbol should be treated as a mutable
        /// (variable).</param>
        /// <returns><c>True</c> if the symbol could be defined, otherwise <c>false</c>.</returns>
        public bool DefineGlobal(string name, double value, bool isMutable) => DefineSymbol(new Symbol(name, isMutable, value), true);

        /// <summary>
        /// Define a globally scoped symbol.
        /// </summary>
        /// <param name="name">The name of the symbol.</param>
        /// <param name="value">The symbol's string value.</param>
        /// <returns><c>True</c> if the symbol could be defined, otherwise <c>false</c>.</returns>
        public bool DefineGlobal(string name, string value) => DefineSymbol(new Symbol(name, false, value), true);

        /// <summary>
        /// Define a globally scoped symbol.
        /// </summary>
        /// <param name="name">The name of the symbol.</param>
        /// <param name="value">The symbol's integral value.</param>
        /// <param name="isMutable">A flag indicating whether the symbol should be treated as a mutable
        /// (variable).</param>
        /// <returns><c>True</c> if the symbol could be defined, otherwise <c>false</c>.</returns>
        public bool DefineGlobal(string name, string value, bool isMutable) => DefineSymbol(new Symbol(name, isMutable, value), true);

        /// <summary>
        /// Define a scoped symbol.
        /// </summary>
        /// <param name="name">The name of the symbol.</param>
        /// <param name="value">The symbol's string value.</param>
        /// <param name="isMutable">A flag indicating whether the symbol should be treated as a mutable
        /// (variable).</param>
        /// <returns><c>True</c> if the symbol could be defined, otherwise <c>false</c>.</returns>
        public bool Define(string name, string value, bool isMutable) => DefineSymbol(new Symbol(name, isMutable, value), false);

        /// <summary>
        /// Define a scoped symbol.
        /// </summary>
        /// <param name="name">The name of the symbol.</param>
        /// <param name="value">The symbol's string value.</param>
        /// (variable).</param>
        /// <returns><c>True</c> if the symbol could be defined, otherwise <c>false</c>.</returns>
        public bool Define(string name, string value) => Define(name, value, false);

        /// <summary>
        /// Define a scoped symbol.
        /// </summary>
        /// <param name="name">The name of the symbol.</param>
        /// <param name="value">The symbol's floating point value.</param>
        /// <param name="isMutable">A flag indicating whether the symbol should be treated as a mutable
        /// (variable).</param>
        /// <returns><c>True</c> if the symbol could be defined, otherwise <c>false</c>.</returns>
        public bool Define(string name, double value, bool isMutable) => DefineSymbol(new Symbol(name, isMutable, value), false);

        /// <summary>
        /// Define a scoped symbol.
        /// </summary>
        /// <param name="name">The name of the symbol.</param>
        /// <param name="value">The symbol's floating point value.</param>
        /// (variable).</param>
        /// <returns><c>True</c> if the symbol could be defined, otherwise <c>false</c>.</returns>
        public bool Define(string name, double value) => Define(name, value, false);

        /// <summary>
        /// Define a scoped symbol.
        /// </summary>
        /// <param name="line">The <see cref="SourceLine"/> containing the operand expression that
        /// defines the symbol.</param>
        /// <returns><c>True</c> if the symbol could be defined, otherwise <c>false</c>.</returns>
        public bool Define(SourceLine line) => Define(line.Label, line.Operand, false);

        /// <summary>
        /// Define a scoped symbol.
        /// </summary>
        /// <param name="tokens">The parsed expression as a token collection.</param>
        /// <param name="isMutable">A flag indicating whether the symbol should be treated as a mutable
        /// (variable).</param>
        /// <returns><c>True</c> if the symbol was created, otherwise <c>false</c>.</returns>
        public bool Define(IEnumerable<Token> tokens, bool isMutable) => DefineFromTokens(tokens, isMutable, false);


        public bool Define(Token symbol, Token array, bool isMutable) => DefineFromTokens(symbol, array, isMutable, false);


        /// <summary>
        /// Gets the reference symbol specified.
        /// </summary>
        /// <param name="token">The symbol as a parsed token.</param>
        /// <returns>The symbol's numeric value.</returns>
        public double GetLineReference(Token token)
        {
            var name = token.Name;
            var topFrameIndex = _referenceFrameIndexStack.Peek();
            return _lineReferenceFrames[topFrameIndex].GetLineReferenceValue(name);
        }

        /// <summary>
        /// Define an line reference ("+" or "-") symbol.
        /// </summary>
        /// <param name="name">The symbol name.</param>
        /// <param name="value">The symbol's value.</param>
        public void DefineLineReference(string name, double value)
        {
            var topFrameIndex = _referenceFrameIndexStack.Peek();
            _lineReferenceFrames[topFrameIndex].DefineLineReferenceValue(name, value);
        }

        public void DefineSymbolicAddress(string addressName)
            => DefineSymbol(new Symbol(addressName, Assembler.Output.LogicalPC), false);

        (Symbol symbol, int index)  GetVectorElementAtIndex(Token symbolToken, Token subscriptToken)
        {
            Symbol symbol = Lookup(symbolToken);
            if (symbol.IsScalar())
                throw new SymbolException(symbolToken, SymbolException.ExceptionReason.Scalar);

            if (subscriptToken == null || !subscriptToken.Name.Equals("["))
                throw new ExpressionException(subscriptToken.Position, "Array subscript expression expected.");
            var index = Evaluator.Evaluate(subscriptToken.Children);
            if (index != (int)index)
                throw new ExpressionException(subscriptToken.Position, "Subscript index must be an integral value.");

            return (symbol, (int)index);
        }

        /// <summary>
        /// Gets the vector element value.
        /// </summary>
        /// <param name="symbolToken">The symbol as a parsed token.</param>
        /// <param name="subscriptToken">The symbol's subscript expression.</param>
        /// <returns></returns>
        /// <exception cref="ExpressionException"></exception>
        /// <exception cref="SymbolException"></exception>
        public double GetNumericVectorElementValue(Token symbolToken, Token subscriptToken)
        {
            var (symbol, index) = GetVectorElementAtIndex(symbolToken, subscriptToken);

            if (symbol.DataType == DataType.String)
            {
                var value = symbol.GetStringValueAtIndex(index);
                return Assembler.Encoding.GetEncodedValue(value);
            }
            
            return symbol.GetNumericValueAtIndex(index);
        }


        /// <summary>
        /// Gets the vector element string.
        /// </summary>
        /// <param name="symbolToken">The symbol as a parsed token.</param>
        /// <param name="subscriptToken">The symbol's subscript expression.</param>
        /// <returns></returns>
        /// <exception cref="ExpressionException"></exception>
        /// <exception cref="SymbolException"></exception>
        public string GetStringVectorElementValue(Token symbolToken, Token subscriptToken)
        {
            var (symbol, index) = GetVectorElementAtIndex(symbolToken, subscriptToken);

            return symbol.GetStringValueAtIndex(index);
        }

        /// <summary>
        /// Gets a symbol's string value.
        /// </summary>
        /// <param name="token">The <see cref="Token"/> representing the symbol.</param>
        /// <returns>The symbol's (text string) value.</returns>
        /// <exception cref="SymbolException"></exception>
        public string GetStringValue(Token token)
        {
            Symbol symbol = Lookup(token);
            if (!symbol.IsScalar())
                throw new SymbolException(token, SymbolException.ExceptionReason.NonScalar);
            return symbol.ToString();
        }

        /// <summary>
        /// Gets a symbol's string value.
        /// </summary>
        /// <param name="symbol">The symbol name.</param>
        /// <returns>The symbol's (text string) value.</returns>
        /// <exception cref="SymbolException"></exception>
        public string GetStringValue(string symbol) => GetStringValue(new Token(symbol));

        /// <summary>
        /// Gets a symbol's numeric value.
        /// </summary>
        /// <param name="token">The <see cref="Token"/> representing the symbol.</param>
        /// <returns>The symbol's (numeric) value.</returns>
        /// <exception cref="SymbolException"></exception>
        public double GetNumericValue(Token token)
        {
            var symbol = Lookup(token);
            if (!symbol.IsScalar())
                throw new SymbolException(token, SymbolException.ExceptionReason.NonScalar);

            return symbol.GetNumericValue();
        }

        /// <summary>
        /// Gets a symbol's numeric value.
        /// </summary>
        /// <param name="symbol">The symbol name.</param>
        /// <returns>The symbol's numeric value.</returns>
        /// <exception cref="SymbolException"></exception>
        public double GetNumericValue(string symbol) => Lookup(new Token(symbol)).GetNumericValue();

        /// <summary>
        /// Get a string listing of all defined label symbols.
        /// </summary>
        /// <returns>The string listing.</returns>
        public string ListLabels()
        {
            var listBuilder = new StringBuilder();
            var labels = _symbols.Where(s => !s.Value.IsMutable);
            foreach (var label in labels)
            {
                listBuilder.Append($"{label.Key}=");
                switch (label.Value.DataType)
                {
                    case DataType.String:
                        listBuilder.Append($"\"{label.Value.StringValue}\"");
                        break;
                    default:
                        listBuilder.Append($"{label.Value} (${(int)label.Value.GetNumericValue():x})");
                        break;
                }
                listBuilder.AppendLine();
            }
            return listBuilder.ToString();
        }

        /// <summary>
        /// Determines if a symbol is valid.
        /// </summary>
        /// <param name="symbol">The symbol name.</param>
        /// <returns><c>True</c> if valid, otherwise <c>false</c>.</returns>
        public bool SymbolIsValid(string symbol)
        // => _criteria.Any(c => !c(symbol));
        {
            foreach (var f in _criteria)
            {
                if (!f(symbol))
                    return false;
            }
            return true;
        }

        public bool SymbolIsScalar(string symbol)
        {
            var sym = Lookup(new Token(symbol));
            return sym.IsScalar();
        }

        public bool EvaluatesFunction(Token function) => function.Name.Equals("len");

        public double EvaluateFunction(Token function, Token parameters)
        {
            if (!parameters.HasChildren)
                throw new ExpressionException(parameters.Position, "Expected argument not provided.");
            if (parameters.Children.Count > 1)
                throw new ExpressionException(parameters.LastChild.Position, $"Unexpected argument \"{parameters.LastChild}\".");
            var symbolLookup = Lookup(parameters.LastChild);
            return symbolLookup.Length;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the local label scope.
        /// </summary>
        public string Local { get; set; }

        #endregion
    }
}