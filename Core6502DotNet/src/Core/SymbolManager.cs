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

        static readonly Dictionary<ExceptionReason, string> s_reasonMessages = new Dictionary<ExceptionReason, string>
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
        public string SymbolName { get; set; }

        public int Position { get; set; }
    }

    /// <summary>
    /// A class managing all valid assembly symbols, including their scope and values.
    /// </summary>
    public class SymbolManager : Core6502Base, IFunctionEvaluator
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

            public int DefinedAtBank { get; set; }

            public bool IsMutable { get; set; }

            public int DefinedAtIndex { get; set; }

            public int DefinedAtPass { get; set; }

            public Symbol()
            {
                DataType = DataType.None;
                SymbolType = SymbolType.Scalar;
                IsMutable = false;
                NumericValue = 0D;
                StringValue = string.Empty;
                DefinedAtBank = 0;
            }

            public Symbol(RandomAccessIterator<SourceLine> LineIterator)
            {
                DataType = DataType.None;
                SymbolType = SymbolType.Scalar;
                IsMutable = false;
                NumericValue = 0D;
                StringValue = string.Empty;
                DefinedAtBank = 0;
                if (LineIterator == null)
                    DefinedAtIndex = -1;
                else
                    DefinedAtIndex = LineIterator.Index;
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
                if (SymbolType == SymbolType.Scalar)
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
                DataType = DataType.String;
                StringValue = value;
            }

            public void SetValue(int address)
            {
                DataType = DataType.Address;
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

            public void SetNumericValueAtIndex(double value, int index)
            {
                if (index >= 0 && NumericVector != null && NumericVector.ContainsKey(index))
                    NumericVector[index] = value;
            }

            public void SetStringValueAtIndex(string value, int index)
            {
                if (index >= 0 && StringVector != null && StringVector.ContainsKey(index))
                    StringVector[index] = value;
            }

            public double GetNumericValue(int currentBank)
            {
                switch (DataType)
                {
                    case DataType.String:
                        return double.NaN;
                    case DataType.Address:
                        if (currentBank == DefinedAtBank)
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
                        return DataType switch
                        {
                            DataType.Address => GetNumericValue(0) == other.GetNumericValue(0),
                            DataType.Numeric => NumericValue == other.NumericValue,
                            _                => StringValue.Equals(other.StringValue),
                        };
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
                    if (SymbolType == SymbolType.Scalar)
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

        class LineReferenceStackFrame : Core6502Base
        {
            struct LineReference
            {
                public string Name { get; }

                public double Value { get; }

                public LineReference(string name, double value)
                    => (Name, Value) = (name, value);
            }

            readonly Dictionary<int, LineReference> _lineReferences;
            readonly LineReferenceStackFrame _parent;

            public LineReferenceStackFrame(AssemblyServices services, 
                                           LineReferenceStackFrame parent,
                                           RandomAccessIterator<SourceLine> lineIterator)
                :base(services)
            {
                _parent = parent;
                _lineReferences = new Dictionary<int, LineReference>();
                LineIterator = lineIterator;
            }

            public double GetLineReferenceValue(string name)
            {
                // cannot evaluate on forward references on first pass
                if (name[0] == '+')
                {
                    if (Services.CurrentPass == 0)
                    {
                        Services.PassNeeded = true;
                        return Services.Output.LogicalPC;
                    }
                }
                var places = name.Length;
                var lastIndex = LineIterator.Index + 1;
                int key = 0;
                while (places > 0)
                {
                    if (name[0] == '-')
                    {
                        while (lastIndex > 0)
                        {
                            key = _lineReferences.Keys.LastOrDefault(k => k < lastIndex);
                            if (key == 0 || _lineReferences[key].Name[0] == '-')
                                break;
                            lastIndex = key;
                        }
                    }
                    else
                    {
                        while (lastIndex <= _lineReferences.Keys.Max())
                        {
                            key = _lineReferences.Keys.FirstOrDefault(k => k > lastIndex);
                            if (key == 0 || _lineReferences.ContainsKey(key) && _lineReferences[key].Name[0] == '+')
                                break;
                            lastIndex = key;
                        }
                    }
                    if (key == 0)
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
                return _lineReferences[key].Value;
            }

            public void DefineLineReferenceValue(string name, double value)
            {
                var index = LineIterator.Index + 1;
                if (_lineReferences.TryGetValue(index, out LineReference lineRef) &&
                    lineRef.Value != value)
                    Services.PassNeeded = true;
                   
                _lineReferences[index] = new LineReference(name, value);
            }

            public RandomAccessIterator<SourceLine> LineIterator { get; set; }
        }

        #endregion

        #region Members

        readonly Dictionary<string, Symbol> _symbols;
        readonly Stack<string> _scope;
        readonly Stack<int> _referenceFrameIndexStack;
        readonly List<Func<string, bool>> _criteria;
        readonly List<LineReferenceStackFrame> _lineReferenceFrames;
        int _referenceFramesCounter, _ephemeralCounter;
        RandomAccessIterator<SourceLine> _lineIterator;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a new instance of a symbol manager class.
        /// </summary>
        /// <param name="services">The shared <see cref="AssemblyServices"/> object.</param>
        public SymbolManager(AssemblyServices services)
            :base(services)
        {
            Services.PassChanged += ProcessPassChange;
            _symbols = new Dictionary<string, Symbol>(services.StringComparer);
            _scope = new Stack<string>();
            _referenceFrameIndexStack = new Stack<int>();
            _referenceFrameIndexStack.Push(0);

            _lineReferenceFrames = new List<LineReferenceStackFrame>
            {
                new LineReferenceStackFrame(services, null, _lineIterator)
            };

            Local = string.Empty;
            _referenceFramesCounter = 0;

            _criteria = new List<Func<string, bool>>
            {
                s =>
                {
                        return s.Equals("+") ||
                               s.Equals("-") ||
                               ((s[0] == '_' || char.IsLetter(s[0])) &&
                               ((char.IsLetterOrDigit(s[^1]) || s[^1] == '_') && !s.Contains('.')));
                }
            };
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
            symbol.DefinedAtBank = Services.Output.CurrentBank;
            symbol.DefinedAtIndex = LineIterator.Index;
            symbol.DefinedAtPass = Services.CurrentPass;
            if (_criteria.Any(f => !f(symbol.Name)))
                throw new SymbolException(symbol.Name, 0, SymbolException.ExceptionReason.NotValid);
            string fqdn;
            if (isGlobal)
                fqdn = symbol.Name;
            else
                fqdn = GetScopedName(symbol.Name);
            var exists = _symbols.ContainsKey(fqdn);
            if (!exists && symbol.IsMutable && !fqdn.Contains('@') && fqdn.Contains('.'))
            {
                var scopes = fqdn.Split('.').ToList();
                while (scopes.Count > 1 && !exists)
                {
                    scopes.RemoveAt(scopes.Count - 2);
                    var oneHigher = string.Join('.', scopes);
                    exists = _symbols.ContainsKey(oneHigher);
                    if (exists)
                        fqdn = oneHigher;
                }
            }
            if (symbol.Name[0] != '_' && !isGlobal && !symbol.IsMutable)
                Local = symbol.Name;

            if (exists)
            {
                var existingSym = _symbols[fqdn];
                if (existingSym.DataType == DataType.Numeric && 
                    (symbol.DataType == DataType.Address || symbol.DataType == DataType.Boolean))
                    existingSym.DataType = symbol.DataType;

                if (!existingSym.IsMutable && existingSym.DefinedAtPass == Services.CurrentPass)
                    throw new SymbolException(symbol.Name, 1, SymbolException.ExceptionReason.Redefined);

                if (!existingSym.IsValueEqual(symbol))
                {
                    // update the existing symbol
                    if (symbol.IsScalar() && !existingSym.IsScalar())
                        throw new SymbolException(symbol.Name, 0, SymbolException.ExceptionReason.NonScalar);
                    if (!symbol.IsScalar() && existingSym.IsScalar())
                        throw new SymbolException(symbol.Name, 0, SymbolException.ExceptionReason.Scalar);
                    existingSym.SetValueFromSymbol(symbol);
                    if (existingSym.DataType != symbol.DataType)
                        throw new SyntaxException(1, "Type mismatch.");

                    // signal to the assembler another pass is needed.
                    // we are setting the PassNeeded flag accordingly, because in very specific circumstances,
                    // we have looped back to this point already but within the same pass (.e.g., from
                    // a loop or goto directive)
                    if (!existingSym.IsMutable && !Services.PassNeeded)
                        Services.PassNeeded = true;
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
                    throw new SymbolException(arrayElementToUpdate.Name, lhs.Position, SymbolException.ExceptionReason.Redefined);
                
                if (subscriptix > -1)
                {
                    if (
                        (arrayElementToUpdate.DataType == DataType.String &&
                         !arrayElementToUpdate.StringVector.ContainsKey(subscriptix)
                        ) ||
                        (arrayElementToUpdate.DataType == DataType.Numeric &&
                         !arrayElementToUpdate.NumericVector.ContainsKey(subscriptix)
                        )
                       )
                    {
                        throw new ExpressionException(tokenList[0].Position, "Argument out of range");
                    }
                }
            }
            bool valueIsArray = tokenList[0].Name.Equals("[");
            if (valueIsArray)
            {
                if (tokenList.Count > 1)
                    throw new SyntaxException(tokenList[1].Position, $"Unexpected expression \"{tokenList[1]}\".");
               
                var array = tokenList[0].Children;
                if (array == null || array.Count == 0 || array[0].Children.Count == 0)
                    throw new SyntaxException(tokenList[1].Position,
                        "Array definition cannot be an empty list");
                
                var firstInAray = array[0].Children[0];
                if (firstInAray.Name.EnclosedInDoubleQuotes())
                {
                    var value = new List<string>(array.Count);
                    foreach (var child in array)
                    {
                        if (child.Children.Count > 0)
                        {
                            if (child.Children.Count > 1 || !child.Children[0].Name.EnclosedInDoubleQuotes())
                                throw new SyntaxException(child.Children[0].Position,
                                    "Expected string literal.");
                            value.Add(child.Children[0].Name.TrimOnce('"'));
                        }

                    }
                    if (arrayElementToUpdate != null)
                    {
                        if (arrayElementToUpdate.DataType != DataType.String)
                            throw new SyntaxException(firstInAray.Position, $"Type mismatch.");
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
                        value.Add(Services.Evaluator.Evaluate(f));
                    if (arrayElementToUpdate != null)
                    {
                        if (arrayElementToUpdate.DataType != DataType.Numeric)
                            throw new SyntaxException(firstInAray.Position, $"Type mismatch.");
                        arrayElementToUpdate.SetValue(new Symbol(string.Empty, isMutable, value), subscriptix);
                    }
                    else
                        return DefineSymbol(new Symbol(symbolName, isMutable, value), isGlobal);
                }

            }
            else
            {
                var firstInRhs = TokenEnumerator.GetEnumerator(tokenList[0])
                                                .FirstOrDefault(t => !string.IsNullOrEmpty(t.Name));
                if (firstInRhs == null)
                    throw new SyntaxException(tokenList[0].Position, "Assignment not specified.");
                if (firstInRhs.Name.EnclosedInDoubleQuotes() && tokenList.Count == 1)
                {
                    var value = tokenList[0].Name.TrimOnce('"');
                    if (arrayElementToUpdate != null)
                    { 
                        if (arrayElementToUpdate.DataType != DataType.String)
                            throw new SyntaxException(firstInRhs.Position, $"Type mismatch.");

                        if (subscriptix > -1)
                            arrayElementToUpdate.SetStringValueAtIndex(value, subscriptix);
                        else
                            arrayElementToUpdate.SetValue(value);
                    }
                    else
                    {
                        return DefineSymbol(new Symbol(symbolName, isMutable, value), isGlobal);
                    }
                }
                else
                {
                    var value = Services.Evaluator.Evaluate(tokenList);
                    if (arrayElementToUpdate != null)
                    {
                         if (arrayElementToUpdate.DataType != DataType.Numeric)
                            throw new SyntaxException(tokenList[0].Position, $"Type mismatch.");

                        if (subscriptix > -1)
                            arrayElementToUpdate.SetNumericValueAtIndex(value, subscriptix);
                        else
                            arrayElementToUpdate.SetValue(value);
                    }
                    else
                    {
                        return DefineSymbol(new Symbol(symbolName, isMutable, value), isGlobal);
                    }
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
                throw new SyntaxException(tokenList[0].Position, $"Assignment error.");


            var isSubscript = tokenList[1].Name.Equals("[");
            var arrayElementToUpdate = isSubscript ? Lookup(tokenList[0]) : null;
            var subscriptix = isSubscript ? (int)Services.Evaluator.Evaluate(tokenList[1].Children, uint.MinValue, int.MaxValue) : -1;
            var assignIx = isSubscript ? 2 : 1;
            var assignment = tokenList[assignIx];

            if (assignment.OperatorType != OperatorType.Binary ||
                (!assignment.Name.Equals("=")))
                throw new SyntaxException(assignment.Position,
                    $"Unrecognized assignment operator \"{assignment.Name}\".");
            
            if (tokenList.Count < 3)
                throw new SyntaxException(tokenList[0].Position,
                    "Missing rhs in assignment.");

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
            var fqdn = GetFullyQualifiedName(name);
            if (_symbols.ContainsKey(fqdn))
                return _symbols[fqdn];
            Services.PassNeeded = true;
            if (raiseExceptionIfNotFound)
            {
                if (Services.CurrentPass > 0)
                    throw new SymbolException(symbolToken, SymbolException.ExceptionReason.NotDefined);
            }
            return null;
        }

        Symbol Lookup(Token symbolToken) => Lookup(symbolToken, true);

        /// <summary>
        /// Determines if the symbol has been defined.
        /// </summary>
        /// <param name="name">The symbol name.</param>
        /// <returns><c>true</c> if the symbol has been defined, 
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

            if (Services.CurrentPass == 0)
            {
                LineReferenceStackFrame parent = _lineReferenceFrames[_referenceFrameIndexStack.Peek()];
                _lineReferenceFrames.Add(new LineReferenceStackFrame(Services, parent, _lineIterator));
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
                    var ephemerals = new List<string>(_symbols.Keys
                                                .Where(k => k.Contains(sc, Services.StringComparison)));
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

        /// <summary>
        /// Define a globally scoped symbol.
        /// </summary>
        /// <param name="symbol">The token that contains the symbol name.</param>
        /// <param name="array">The token that contains the array of values for the non-scalar symbol.</param>
        /// <returns><c>true</c> if the symbol could be defined, otherwise <c>false</c>.</returns>
        public bool DefineGlobal(Token symbol, Token array) => DefineFromTokens(symbol, array, false, true);

        /// <summary>
        /// Define a globally scoped symbol.
        /// </summary>
        /// <param name="symbol">The token that contains the symbol name.</param>
        /// <param name="array">The token that contains the array of values for the non-scalar symbol.</param>
        /// <param name="isMutable">A flag indicating whether the symbol should be treated as a mutable
        /// (variable).</param>
        /// <returns><c>true</c> if the symbol could be defined, otherwise <c>false</c>.</returns>
        public bool DefineGlobal(Token symbol, Token array, bool isMutable)
            => DefineFromTokens(symbol, array, isMutable, true);

        /// <summary>
        /// Define a globally scoped symbol.
        /// </summary>
        /// <param name="tokens">The tokens that contain the definition expression.</param>
        /// (variable).</param>
        /// <returns><c>true</c> if the symbol could be defined, otherwise <c>false</c>.</returns>
        public bool DefineGlobal(IEnumerable<Token> tokens) => DefineFromTokens(tokens, false, true);

        /// <summary>
        /// Define a globally scoped symbol.
        /// </summary>
        /// <param name="tokens">The tokens that contain the definition expression.</param>
        /// <param name="IsMutable">A flag indicating whether the symbol should be treated as a mutable
        /// (variable).</param>
        /// <returns><c>true</c> if the symbol could be defined, otherwise <c>false</c>.</returns>
        public bool DefineGlobal(IEnumerable<Token> tokens, bool IsMutable) => DefineFromTokens(tokens, IsMutable, true);

        /// <summary>
        /// Define a globally scoped symbol.
        /// </summary>
        /// <param name="name">The name of the symbol.</param>
        /// <param name="value">The symbol's floating point value.</param>
        /// <returns><c>true</c> if the symbol could be defined, otherwise <c>false</c>.</returns>
        public bool DefineGlobal(string name, double value) => DefineSymbol(new Symbol(name, false, value), true);

        /// <summary>
        /// Define a globally scoped symbol.
        /// </summary>
        /// <param name="name">The name of the symbol.</param>
        /// <param name="value">The symbol's integral value.</param>
        /// <param name="isMutable">A flag indicating whether the symbol should be treated as a mutable
        /// (variable).</param>
        /// <returns><c>true</c> if the symbol could be defined, otherwise <c>false</c>.</returns>
        public bool DefineGlobal(string name, double value, bool isMutable) => DefineSymbol(new Symbol(name, isMutable, value), true);

        /// <summary>
        /// Define a globally scoped symbol.
        /// </summary>
        /// <param name="name">The name of the symbol.</param>
        /// <param name="value">The symbol's string value.</param>
        /// <returns><c>true</c> if the symbol could be defined, otherwise <c>false</c>.</returns>
        public bool DefineGlobal(string name, string value) => DefineSymbol(new Symbol(name, false, value), true);

        /// <summary>
        /// Define a globally scoped symbol.
        /// </summary>
        /// <param name="name">The name of the symbol.</param>
        /// <param name="value">The symbol's integral value.</param>
        /// <param name="isMutable">A flag indicating whether the symbol should be treated as a mutable
        /// (variable).</param>
        /// <returns><c>true</c> if the symbol could be defined, otherwise <c>false</c>.</returns>
        public bool DefineGlobal(string name, string value, bool isMutable) => DefineSymbol(new Symbol(name, isMutable, value), true);

        /// <summary>
        /// Define a scoped symbol.
        /// </summary>
        /// <param name="name">The name of the symbol.</param>
        /// <param name="value">The symbol's string value.</param>
        /// <param name="isMutable">A flag indicating whether the symbol should be treated as a mutable
        /// (variable).</param>
        /// <returns><c>true</c> if the symbol could be defined, otherwise <c>false</c>.</returns>
        public bool Define(string name, string value, bool isMutable) => DefineSymbol(new Symbol(name, isMutable, value), false);

        /// <summary>
        /// Define a scoped symbol.
        /// </summary>
        /// <param name="name">The name of the symbol.</param>
        /// <param name="value">The symbol's string value.</param>
        /// (variable).</param>
        /// <returns><c>true</c> if the symbol could be defined, otherwise <c>false</c>.</returns>
        public bool Define(string name, string value) => Define(name, value, false);

        /// <summary>
        /// Define a scoped symbol.
        /// </summary>
        /// <param name="name">The name of the symbol.</param>
        /// <param name="value">The symbol's floating point value.</param>
        /// <param name="isMutable">A flag indicating whether the symbol should be treated as a mutable
        /// (variable).</param>
        /// <returns><c>true</c> if the symbol could be defined, otherwise <c>false</c>.</returns>
        public bool Define(string name, double value, bool isMutable) => DefineSymbol(new Symbol(name, isMutable, value), false);

        /// <summary>
        /// Define a scoped symbol.
        /// </summary>
        /// <param name="name">The name of the symbol.</param>
        /// <param name="value">The symbol's floating point value.</param>
        /// (variable).</param>
        /// <returns><c>true</c> if the symbol could be defined, otherwise <c>false</c>.</returns>
        public bool Define(string name, double value) => Define(name, value, false);

        /// <summary>
        /// Define a scoped symbol.
        /// </summary>
        /// <param name="line">The <see cref="SourceLine"/> containing the operand expression that
        /// defines the symbol.</param>
        /// <returns><c>true</c> if the symbol could be defined, otherwise <c>false</c>.</returns>
        public bool Define(SourceLine line) => Define(line.Label, line.Operand, false);

        /// <summary>
        /// Define a scoped symbol.
        /// </summary>
        /// <param name="tokens">The parsed expression as a token collection.</param>
        /// <param name="isMutable">A flag indicating whether the symbol should be treated as a mutable
        /// (variable).</param>
        /// <returns><c>true</c> if the symbol was created, otherwise <c>false</c>.</returns>
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

        /// <summary>
        /// Define a symbol as representing the current state of the program counter.
        /// </summary>
        /// <param name="addressName">The symbolic address name.</param>
        public void DefineSymbolicAddress(string addressName)
            => DefineSymbol(new Symbol(addressName, Services.Output.LogicalPC), false);

        (Symbol symbol, int index)  GetVectorElementAtIndex(Token symbolToken, Token subscriptToken)
        {
            Symbol symbol = Lookup(symbolToken);
            if (symbol == null)
                return (null, 0);
            if (symbol.IsScalar())
                throw new SymbolException(symbolToken, SymbolException.ExceptionReason.Scalar);

            if (subscriptToken == null || !subscriptToken.Name.Equals("["))
                throw new SyntaxException(subscriptToken.Position, "Array subscript expression expected.");
            var index = Services.Evaluator.Evaluate(subscriptToken.Children);
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
            if (symbol == null)
                return double.NaN;
            if (symbol.DataType == DataType.String)
            {
                var value = symbol.GetStringValueAtIndex(index);
                return Services.Encoding.GetEncodedValue(value);
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
            if (symbol == null)
                return string.Empty;

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
            if (symbol == null)
                return string.Empty;
            if (!symbol.IsScalar())
                throw new SymbolException(token, SymbolException.ExceptionReason.NonScalar);
            return symbol.StringValue;
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
            if (symbol == null)
                return double.NaN;
            if (!symbol.IsScalar())
                throw new SymbolException(token, SymbolException.ExceptionReason.NonScalar);

            return symbol.GetNumericValue(Services.Output.CurrentBank);
        }

        /// <summary>
        /// Gets a symbol's numeric value.
        /// </summary>
        /// <param name="symbol">The symbol name.</param>
        /// <returns>The symbol's numeric value.</returns>
        /// <exception cref="SymbolException"></exception>
        public double GetNumericValue(string symbol)
        {
            var symObj = Lookup(new Token(symbol));
            if (symObj == null)
                return double.NaN;
            return symObj.GetNumericValue(Services.Output.CurrentBank);
        }

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
                        listBuilder.Append($"{label.Value} (${(int)label.Value.GetNumericValue(Services.Output.CurrentBank):x})");
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
        /// <returns><c>true</c> if valid, otherwise <c>false</c>.</returns>
        public bool SymbolIsValid(string symbol)
        {
            foreach (var f in _criteria)
            {
                if (!f(symbol))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Determines if the symbol is a scalar (constant) value.
        /// </summary>
        /// <param name="symbol">The symbol name.</param>
        /// <returns><c>true</c> if the symbol is scalar, otherwise <c>false</c>.</returns>
        public bool SymbolIsScalar(string symbol)
        {
            var sym = Lookup(new Token(symbol));
            if (sym == null) return true;
            return sym.IsScalar();
        }

        /// <summary>
        /// Determines if the symbol is numeric.
        /// </summary>
        /// <param name="symbol">The symbol name.</param>
        /// <returns><c>true</c> if the symbol is numeric, otherwise <c>false</c>.</returns>
        public bool SymbolIsNumeric(string symbol)
        {
            var sym = Lookup(new Token(symbol)); 
            if (sym == null) return true;
            return sym.DataType == DataType.Numeric ||
                    sym.DataType == DataType.Address ||
                    sym.DataType == DataType.Boolean;
        }

        public bool EvaluatesFunction(Token function) => function.Name.Equals("len");

        public double EvaluateFunction(Token function, Token parameters)
        {
            if (parameters.Children.Count == 0 || parameters.Children[0].Children.Count == 0)
                throw new SyntaxException(parameters.Position, "Expected argument not provided.");
            if (parameters.Children.Count > 1)
                throw new SyntaxException(parameters.LastChild.Position, $"Unexpected argument \"{parameters.LastChild}\".");
            var symbolLookup = Lookup(parameters.LastChild);
            if (symbolLookup == null)
                return 0;
            return symbolLookup.Length;
        }

        public void InvokeFunction(Token function, Token parameters)
            => _ = EvaluateFunction(function, parameters);

        public bool IsFunctionName(string symbol) => symbol.Equals("len");


        #endregion

        #region Properties

        /// <summary>
        /// Gets the local label scope.
        /// </summary>
        public string Local { get; private set; }

        /// <summary>
        /// Gets or sets the line iterator the symbol manager uses to handle
        /// forward and backward references.
        /// </summary>
        public RandomAccessIterator<SourceLine> LineIterator 
        {
            get => _lineIterator;
            set
            {
                _lineIterator = value;
                _lineReferenceFrames.ForEach(lrf => lrf.LineIterator = value);
            }
        }

        #endregion
    }
}