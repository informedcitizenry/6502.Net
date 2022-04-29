//-----------------------------------------------------------------------------
// Copyright (c) 2017-2022 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;

namespace Sixty502DotNet
{
    /// <summary>
    /// A visitor class for parsed expressions whose visit methods return a
    /// <see cref="Value"/>. Higher level parsed source is not visited.
    /// </summary>
    public class ExpressionVisitor : Sixty502DotNetParserBaseVisitor<Value>
    {
        const string Discard = "_";
        private readonly AssemblyServices _services;

        /// <summary>
        /// Construct a new instance of the <see cref="ExpressionVisitor"/>.
        /// </summary>
        /// <param name="services">The <see cref="AssemblyServices"/> for the visitor.</param>
        public ExpressionVisitor(AssemblyServices services) => _services = services;

        public override Value VisitPrimaryExpr([NotNull] Sixty502DotNetParser.PrimaryExprContext context)
            => context.value;

        private bool IdentifierIsNotConstant(Sixty502DotNetParser.IdentifierContext? context)
        {
            if (context != null)
            {
                var symbol = Evaluator.ResolveIdentifierSymbol(_services.Symbols.Scope, _services.Symbols.ImportedScopes, context);
                return symbol is Variable || symbol is UserFunctionDefinition;
            }
            return false;
        }

        /// <summary>
        /// Determines whether the parsed expression is not constant, or contains
        /// non-constant expressions.
        /// </summary>
        /// <param name="context">The <see cref="Sixty502DotNetParser.ExprContext"/>.</param>
        /// <returns><c>true</c> if the expression contains one or more non-constant
        /// expressions, <c>false</c> otherwise.</returns>
        public bool ExpressionHasNonConstants(Sixty502DotNetParser.ExprContext? context)
        {
            if (context != null)
            {
                foreach (var expr in context.expr())
                {
                    if (ExpressionHasNonConstants(expr))
                    {
                        return true;
                    }
                }
                if (ExpressionHasNonConstants(context.assignExpr()?.expr()))
                {
                    return true;
                }
                return IdentifierIsNotConstant(context.refExpr()?.identifier());
            }
            return false;
        }

        public override Value VisitProgramCounter([NotNull] Sixty502DotNetParser.ProgramCounterContext context)
            => new(_services.Output.LogicalPC);

        public override Value VisitExpressionList([NotNull] Sixty502DotNetParser.ExpressionListContext context)
        {
            var arrayValue = new ArrayValue();
            foreach (var expr in context.expr())
            {
                var element = Visit(expr);
                arrayValue.Add(element);
            }
            return arrayValue;
        }

        /// <summary>
        /// Try to evaluate the passed <see cref="Sixty502DotNetParser.ExprContext"/> as a
        /// primary expression.
        /// </summary>
        /// <param name="expr">The <see cref="Sixty502DotNetParser.ExprContext"/>.</param>
        /// <param name="value">An <see cref="Value"/> evaluation of the primary
        /// expression. If the expression is not a primary expression, the result
        /// is undefined.</param>
        /// <returns><c>true</c> if the expression was successfully evaluated as a
        /// primary expression, <c>false</c> otherwise.</returns>
        public bool TryGetPrimaryExpression(Sixty502DotNetParser.ExprContext expr, out Value value)
        {
            try
            {
                value = Evaluator.GetPrimaryExpression(expr);
                return true;
            }
            catch (Exception ex)
            {
                value = Value.Undefined;
                _services.Log.LogEntry(expr, ex.Message);
                return false;
            }
        }

        private Value ResolveValue(IFunction function, Sixty502DotNetParser.IdentifierContext context)
        {
            if (context.LeftParen() == null || (function is UserFunctionDefinition userFunc && !userFunc.CanBeInvoked))
            {
                if (context.LeftParen() == null)
                {
                    _services.Log.LogEntry(context, "Function being used as a type.");
                    return Value.Undefined;
                }
                _services.Log.LogEntry(context, "User function is called before it is defined.");
                return new Value();
            }
            ArrayValue? args = new();
            if (context.expressionList() != null)
            {
                args = Visit(context.expressionList()) as ArrayValue;
                if (!args!.ElementsDefined)
                {
                    return Value.Undefined;
                }
            }
            try
            {
                var returnVal = function.Invoke(args);
                if (returnVal == null || (!returnVal.IsDefined && _services.State.CurrentPass > 0))
                {
                    _services.Log.LogEntry(context, $"The call to {context.name?.Text ?? context.lhs.GetText()}() did not return a value.");
                    return Value.Undefined;
                }
                if (!returnVal.IsPrimitiveType)
                {
                    _services.State.PassNeeded = _services.State.CurrentPass == 0;
                }
                return returnVal;
            }
            catch (Error err)
            {
                _services.Log.LogEntry(context, err.Message);
                return Value.Undefined;
            }
        }

        private Value ResolveValue(IValueResolver resolver, Sixty502DotNetParser.IdentifierContext context)
        {
            if (context.range().Length > 0 || context.LeftParen() != null)
            {
                if (context.range().Length > 0)
                {
                    return GetSubsequence(resolver.Value, context.range()) ?? Value.Undefined;
                }
                _services.Log.LogEntry(context, Errors.TypeMismatchError);
                return Value.Undefined;
            }
            if (resolver is Label || resolver is Constant)
            {
                _services.State.PassNeeded |= !resolver.Value.IsDefined;
                if (resolver is Label label && label.Bank >= 0 && label.Value.IsIntegral && label.Bank != _services.Output.CurrentBank)
                {
                    return new Value((label.Bank * 0x10000) | label.Value.ToInt());
                }
            }
            return resolver.Value;
        }

        public override Value VisitIdentifier([NotNull] Sixty502DotNetParser.IdentifierContext context)
        {
            var symbol = Evaluator.ResolveIdentifierSymbol(_services.Symbols.Scope, _services.Symbols.ImportedScopes, context);
            if (symbol == null)
            {
                _services.Log.LogEntry(context, "Symbol not found.");
                return Value.Undefined;
            }
            if (_services.Options.WarnCaseMismatch && context.name != null && !context.name.Equals(symbol.Name))
            {
                _services.Log.LogEntry(context, "Case mismatch.", false);
            }
            Value identifierValue = Value.Undefined;
            if (symbol is IValueResolver || symbol is IFunction)
            {
                identifierValue = symbol is IValueResolver resolver ? ResolveValue(resolver, context) :
                    ResolveValue((IFunction)symbol, context);
                var currentScopeIsProcScope = _services.Symbols.Scope is Label currScope && currScope.IsProcScope;
                if (symbol is not Label || !currentScopeIsProcScope || !ReferenceEquals(symbol, _services.Symbols.Scope))
                {
                    // If a symbol is a proc scope it can only be tagged as referenced if the current scope
                    // is not a proc scope, or if the symbol is enclosed in the scope.
                    symbol.IsReferenced = identifierValue.IsDefined;
                    if (!currentScopeIsProcScope)
                    {
                        // if we are not being referenced from a proc scope, propagate the reference up
                        IScope? symScope = symbol.Scope;
                        while (symScope is Label symScopeLabel && symScopeLabel.IsProcScope)
                        {
                            symScopeLabel.IsReferenced = symbol.IsReferenced;
                            symScope = symScope.EnclosingScope;
                        }
                    }
                }
                return identifierValue;
            }
            _services.Log.LogEntry(context, Errors.TypeMismatchError);
            return identifierValue;
        }

        public override Value VisitAnonymousLabel([NotNull] Sixty502DotNetParser.AnonymousLabelContext context)
        {
            var scope = _services.Symbols.Scope is Label l && !l.IsBlockScope ? l.EnclosingScope : _services.Symbols.Scope;
            var anonLabel = scope?.ResolveAnonymousLabel(context.Start.Text, context.Start.TokenIndex);
            if (anonLabel == null)
            {
                _services.Log.LogEntry(context, "Unable to resolve anonymous label.");
                return Value.Undefined;
            }
            if (!anonLabel.Value.IsDefined && anonLabel.LabelType == AnonymousLabel.Forward)
            {
                _services.State.PassNeeded = true;
            }
            anonLabel.IsReferenced = true;
            return anonLabel.Value;
        }

        public override Value VisitArray([NotNull] Sixty502DotNetParser.ArrayContext context)
        {
            var array = Visit(context.expressionList()) as ArrayValue;
            if (!array!.ElementsSameType)
            {
                _services.Log.LogEntry(context.expressionList(), "Array elements must be the same type.");
                return Value.Undefined;
            }
            return array;
        }

        public override Value VisitEnumDef([NotNull] Sixty502DotNetParser.EnumDefContext context)
        {
            var enm = _services.Symbols.Scope as Enum;
            var enumDefName = context.Start.Text;
            if (enm?.ResolveMember(enumDefName) is Label def)
            {
                Value val = new(enm.AutoValue);
                var assignExpr = context.arg().assignExpr();
                if (assignExpr != null)
                {
                    if (assignExpr.assignOp()?.Start.Type != Sixty502DotNetParser.Equal ||
                        assignExpr.identifier()?.name == null)
                    {
                        _services.Log.LogEntry(context, "Invalid enumeration definition expression.");
                        return Value.Undefined;
                    }
                    if (assignExpr.assignOp()?.Start.Type == Sixty502DotNetParser.Equal &&
                        assignExpr.identifier()?.name != null)
                    {
                        if (!TryGetPrimaryExpression(assignExpr.expr(), out val) || !val.IsIntegral)
                        {
                            if (!val.IsIntegral)
                            {
                                _services.Log.LogEntry(assignExpr.expr(), def.Value.IsDefined ?
                                    "Enum definition must be an integer" : Errors.ExpectedConstant);
                            }
                            return Value.Undefined;
                        }
                    }
                }
                if (enm.UpdateMember(enumDefName, val))
                {
                    _services.LabelListing.Log(enumDefName, val);
                    return val;
                }
                if (val.IsIntegral && assignExpr != null)
                {
                    _services.Log.LogEntry(assignExpr.expr(), "Enum definition must be greater than previous.");
                }
                return Value.Undefined;
            }
            // if we didn't find it it's because it wasn't a proper label
            // which would have gotten defined during parse.
            _services.Log.LogEntry(context, "Enum definition name is not valid.");
            return Value.Undefined;
        }

        public override Value VisitDictionary([NotNull] Sixty502DotNetParser.DictionaryContext context)
        {
            var keys = new ArrayValue();
            var vals = new ArrayValue();
            foreach (var kvp in context.keyValuePair())
            {
                var k = Visit(kvp.key); if (!k.IsDefined) return k;
                if (!DictionaryValue.CanBeKey(k))
                {
                    _services.Log.LogEntry(kvp.key, "Invalid key type.");
                    return Value.Undefined;
                }
                if (keys.Contains(k))
                {
                    _services.Log.LogEntry(kvp.key, "Duplicate key entry.");
                    return Value.Undefined;
                }
                keys.Add(k);
                if (!keys.ElementsSameType)
                {
                    _services.Log.LogEntry(kvp.key, "Key type mismatch.");
                    return Value.Undefined;
                }
                var v = Visit(kvp.val); if (!v.IsDefined) return v;
                vals.Add(v);
                if (!vals.ElementsSameType)
                {
                    _services.Log.LogEntry(kvp.val, Errors.TypeMismatchError);
                    return Value.Undefined;
                }
            }
            return new DictionaryValue(keys, vals);
        }

        private (Value startIndex, Value endIndex) GetRangeValues(Sixty502DotNetParser.RangeContext context)
        {
            var s = context.startIndex != null ? Visit(context.startIndex) : new Value(0);
            var e = context.endIndex != null ? Visit(context.endIndex) : new Value(-1);
            return (s, e);
        }

        private Value GetRange(Value sequence, Sixty502DotNetParser.RangeContext range)
        {
            if (!sequence.IsString && (sequence is not ArrayValue || sequence is DictionaryValue))
            {
                _services.Log.LogEntry(range, Errors.InvalidOperation);
                return Value.Undefined;
            }
            (Value s, Value e) = GetRangeValues(range);
            if (!s.IsDefined || !e.IsDefined)
            {
                return Value.Undefined;
            }
            if (!sequence.TryGetElements(s, e, out var subsequence))
            {
                string error = "Index is out of range.";
                if (!s.IsIntegral || !e.IsIntegral)
                {
                    error = "Invalid range expression.";
                }
                if (subsequence is DictionaryValue)
                {
                    error = Errors.InvalidOperation;
                }
                _services.Log.LogEntry(range, error);
                return Value.Undefined;
            }
            return subsequence;
        }

        private Value? GetSubsequence(Value sequence, Sixty502DotNetParser.RangeContext[] ranges)
        {
            int i = 0;
            Value? subsequence = null;
            while (i < ranges.Length)
            {
                if (ranges[i].DoubleDot() != null)
                {
                    subsequence = GetRange(sequence, ranges[i++]);
                }
                else
                {
                    var index = Visit(ranges[i++].expr()[0]);
                    if (!index.IsDefined) return index;
                    if (!sequence.TryGetElement(index, out subsequence))
                    {
                        string error = "Index is out of range.";
                        if (!index.IsIntegral)
                        {
                            error = "Invalid index expression.";
                        }
                        if (sequence is DictionaryValue dict)
                        {
                            error = "Key not found in dictionary.";
                            if (dict.KeyType != index.DotNetType &&
                                !(index.IsNumeric && (dict.KeyType == TypeCode.Int32 || dict.KeyType == TypeCode.Double)))
                            {
                                error = Errors.TypeMismatchError;
                            }
                        }
                        _services.Log.LogEntry(ranges[i - 1], error);
                        return Value.Undefined;
                    }
                }
                sequence = subsequence;
            }
            return subsequence;
        }

        public override Value VisitDesignator([NotNull] Sixty502DotNetParser.DesignatorContext context)
        {
            if (context.array() != null)
            {
                return Visit(context.array());
            }
            if (context.dictionary() != null)
            {
                return Visit(context.dictionary());
            }
            var ranges = context.range();
            if (context.designator()?.array() != null || context.designator()?.dictionary() != null || context.StringLiteral() != null)
            {
                Value sequence = context.designator()?.array() != null
                    ? Visit(context.designator().array())
                    : context.designator()?.dictionary() != null
                    ? Visit(context.designator().dictionary())
                    : Evaluator.GetPrimaryExpression(context.StringLiteral().Symbol);
                return GetSubsequence(sequence, ranges) ?? Value.Undefined;
            }
            _services.Log.LogEntry(ranges[^1], Errors.UnexpectedExpression);
            return Value.Undefined;
        }

        private void SetSymbolRef(IValueResolver resolver, Sixty502DotNetParser.ExprContext expr)
        {
            // somevar (:= othervar)? := [1,2,3]
            if (expr.refExpr()?.identifier() != null || expr.assignExpr()?.identifier() != null)
            {
                var identifier = expr.refExpr()?.identifier() ??
                                 expr.assignExpr().identifier();
                var rhsRef = Evaluator.ResolveIdentifierSymbol(_services.Symbols.Scope,
                    _services.Symbols.ImportedScopes, identifier) as IValueResolver;
                resolver.IsAReferenceTo = rhsRef;
            }
        }

        public override Value VisitAssignExpr([NotNull] Sixty502DotNetParser.AssignExprContext context)
        {
            if (context.expr() == null)
            {
                return base.VisitChildren(context);
            }
            var op = context.assignOp()?.Start ?? context.assignPcOp().Start;
            var opType = op.Type;
            var rhs = Visit(context.expr()); if (!rhs.IsDefined) return rhs;
            if (context.identifier() != null)
            {
                Value rhsCopy = rhs;
                if (rhs is not ArrayValue)
                {
                    rhsCopy = new(rhs);
                }
                var lhs = context.identifier()?.name?.Text ?? "";
                if (lhs.Equals(Discard))
                {
                    return rhsCopy;
                }
                var identSymbol = Evaluator.ResolveIdentifierSymbol(_services.Symbols.Scope, _services.Symbols.ImportedScopes, context.identifier());
                if (identSymbol == null)
                {
                    if (Evaluator.IsCompoundAssignment(opType))
                    {
                        _services.Log.LogEntry(context, op, Errors.InvalidOperation);
                        return Value.Undefined;
                    }
                    if (!Label.IsCheapLocal(lhs))
                    {
                        _services.Symbols.PopLocalLabel();
                    }
                    var lhsVariable = new Variable(lhs, rhsCopy)
                    {
                        Token = (Token)context.identifier().name
                    };
                    _services.Symbols.Scope.Define(lhs, lhsVariable);
                    _services.Symbols.DeclareVariable(lhsVariable);
                    if (rhsCopy is ArrayValue)
                    {
                        SetSymbolRef(lhsVariable, context.expr());
                    }
                    return rhsCopy;
                }
                if (identSymbol is not IValueResolver)
                {
                    _services.Log.LogEntry(context.expr(), Errors.TypeMismatchError);
                    return Value.Undefined;
                }
                var resolver = (IValueResolver)identSymbol;
                /* If the operator is a ':=' -OR- if the symbol is NOT a constant (checking the context parent
                     * to be a labelStat context tells us this) then the context we are in is one of a variable
                     * assignment:
                     * 
                     * myvar := 3 // full statement
                     * .let myvar = 3 // .let directive
                     *
                     * Versus:
                     * 
                     * myvar = 3 // constant assignment (and hence 'myvar' is not, in fact, a 'var')
                     */
                var isVarAssignment = opType != Sixty502DotNetParser.Equal ||
                        context.Parent is not Sixty502DotNetParser.LabelStatContext ||
                        context.identifier()?.Ident() == null;
                if (!SymbolManager.SymbolAssignmentIsLegal(identSymbol, isVarAssignment))
                {
                    _services.Log.LogEntry(context, context.Start,
                        string.Format(Errors.SymbolExistsError, context.identifier().Start.Text));
                    return Value.Undefined;
                }
                if (Evaluator.IsCompoundAssignment(opType))
                {
                    rhsCopy = Evaluator.BinaryOp(resolver.Value, Evaluator.CompoundToAssign(opType), rhs);
                }
                else if (identSymbol is Constant && ExpressionHasNonConstants(context.expr()))
                {
                    _services.Log.LogEntry(context.expr(), Errors.ConstantAssignment);
                    return Value.Undefined;
                }
                var prevVal = resolver.Value;
                if (resolver is Constant)
                {
                    _services.State.PassNeeded |= prevVal.IsDefined && !prevVal.Equals(rhs);
                    resolver.Value = rhs;
                    if (!_services.Symbols.Scope.InFunctionScope &&
                        !_services.State.PassNeeded &&
                        !_services.Options.LabelsAddressesOnly)
                    {
                        _services.LabelListing.Log(resolver);
                    }
                }
                else if (!prevVal.IsDefined)
                {
                    resolver.Value = rhsCopy;
                }
                else
                {
                    bool updated = false;
                    if (context.identifier().range().Length > 0)
                    {
                        if (resolver.Value.IsString || resolver.Value is ArrayValue)
                        {
                            var subSequence = GetSubsequence(resolver.Value, context.identifier().range());
                            if (subSequence?.IsDefined == true)
                            {
                                updated = subSequence.SetAs(rhs);
                            }
                        }
                    }
                    else
                    {
                        updated = resolver.Value.SetAs(rhsCopy);
                    }
                    if (!updated)
                    {
                        _services.Log.LogEntry(context.expr(), Errors.TypeMismatchError);
                        return Value.Undefined;
                    }
                }
                if (rhsCopy is ArrayValue)
                {
                    SetSymbolRef(resolver, context.expr());
                }
                return rhsCopy;
            }
            else if (context.programCounter() != null || // '*' ('='|':=') <expr>
                    context.assignPcOp() != null)        // '*=' <expr>
            {
                if (opType == Sixty502DotNetParser.AsteriskEq ||
                    opType == Sixty502DotNetParser.Equal ||
                    opType == Sixty502DotNetParser.ColonEqual)
                {
                    if (TryGetArithmeticExpr(context.expr(), short.MinValue, ushort.MaxValue, out var pc))
                    {
                        _services.Output.SetPC((int)pc);
                        return new Value(pc);
                    }
                    return Value.Undefined;
                }
            }
            _services.Log.LogEntry(context, Errors.InvalidOperation);
            return Value.Undefined;
        }

        private Value PrePostfix(Sixty502DotNetParser.IdentifierContext context, IToken op, bool returnNewValue)
        {
            if (op.Text.Length != 2)
            {
                _services.Log.LogEntry(context, op, Errors.InvalidOperation);
                return Value.Undefined;
            }
            var incdec = op.Text[0] == '+' ? 1 : -1;
            Value returnValue = new(_services.Output.ProgramCounter + incdec);
            if (context != null)
            {
                var symbol = Evaluator.ResolveIdentifierSymbol(_services.Symbols.Scope, _services.Symbols.ImportedScopes, context);
                if (symbol is Variable variable && variable.Value.IsNumeric)
                {
                    returnValue.SetAs(variable.Value);
                    variable.Value.SetAs(Evaluator.BinaryOp(variable.Value,
                        Sixty502DotNetParser.Plus, new Value(incdec)));
                    if (returnNewValue)
                    {
                        return variable.Value;
                    }
                    return returnValue;
                }
                _services.Log.LogEntry(context, Errors.InvalidOperation);
                returnValue = Value.Undefined;
            }
            _services.Output.SetPC(returnValue.ToInt());
            if (returnNewValue)
            {
                return returnValue;
            }
            return new Value(returnValue.ToInt() - incdec);
        }

        public override Value VisitPrefixExpr([NotNull] Sixty502DotNetParser.PrefixExprContext context)
            => PrePostfix(context.identifier(), context.op, true);

        public override Value VisitPostfixExpr([NotNull] Sixty502DotNetParser.PostfixExprContext context)
            => PrePostfix(context.identifier(), context.op, false);

        private Value StringToInt(Value value)
        {
            if (value.IsString || value.DotNetType == TypeCode.Char)
            {
                return new Value(_services.Encoding.GetEncodedValue(value.ToString(true)));
            }
            return value;
        }

        public override Value VisitExpr([NotNull] Sixty502DotNetParser.ExprContext context)
        {
            try
            {
                if (context.rhs != null)
                {
                    Value rhs = Visit(context.rhs);
                    int op = context.op.Type;
                    if (!rhs.IsDefined) return Value.Undefined;
                    if (context.lhs != null)
                    {
                        if (op == Sixty502DotNetParser.TripleEqual)
                        {
                            return Evaluator.IsIdentical(_services.Symbols.Scope, _services.Symbols.ImportedScopes, context.lhs, context.rhs);
                        }
                        if (op == Sixty502DotNetParser.BangDoubleEqual)
                        {
                            return new Value(Evaluator.IsIdentical(_services.Symbols.Scope,
                                _services.Symbols.ImportedScopes,
                                context.lhs, context.rhs).ToBool() == false);
                        }
                        var lhs = Visit(context.lhs); if (!lhs.IsDefined) return Value.Undefined;
                        if (lhs.IsNumeric && (rhs.DotNetType == TypeCode.Char || rhs.IsString) ||
                            rhs.IsNumeric && (lhs.DotNetType == TypeCode.Char || lhs.IsString))
                        {
                            lhs = StringToInt(lhs);
                            rhs = StringToInt(rhs);
                        }
                        return Evaluator.BinaryOp(lhs, op, rhs);
                    }
                    return Evaluator.UnaryOp(op, StringToInt(rhs));
                }
                if (context.op?.Type == Sixty502DotNetParser.Percent)
                {
                    return Evaluator.BinaryNumber(context);
                }
                if (context.lparen != null)
                {
                    return Visit(context.expr()[0]);
                }
                if (context.cond != null)
                {
                    var cond = Visit(context.cond);
                    var then = Visit(context.then);
                    var els = Visit(context.els);
                    return Evaluator.CondOp(cond, then, els);
                }
                return base.VisitChildren(context);
            }
            catch (Exception ex)
            {
                var offendingSymbol = context.Start;
                if (ex is InvalidOperationException)
                {
                    offendingSymbol = context.op ?? context.Query().Symbol;
                }
                _services.Log.LogEntry((Token)offendingSymbol, ex.Message);
                return Value.Undefined;
            }
        }

        /// <summary>
        /// Try to evaluate the <see cref="Sixty502DotNetParser.ExprContext"/> as
        /// an arithmetic expression.
        /// </summary>
        /// <param name="expr">The <see cref="Sixty502DotNetParser.ExprContext"/>.</param>
        /// <param name="minValue">The lower bound of the evaluated value permitted.</param>
        /// <param name="maxValue">The upper bound of the evaluated value permitted.</param>
        /// <param name="value">An <see cref="Value"/> evaluation of the arithmetic
        /// expression. If the expression is not a primary expression, the result
        /// is undefined.</param>
        /// <returns><c>true</c> if the expression was successfully evaluated as an
        /// arithmetic expression, <c>false</c> otherwise.</returns>
        public bool TryGetArithmeticExpr(Sixty502DotNetParser.ExprContext expr, double minValue, double maxValue, out double value)
        {
            var val = Visit(expr);
            if (val?.IsDefined == true)
            {
                if (val.IsString || val.DotNetType == TypeCode.Char)
                {
                    try
                    {
                        value = _services.Encoding.GetEncodedValue(val.ToString(true));
                        return true;
                    }
                    catch (Exception ex)
                    {
                        value = double.NaN;
                        _services.Log.LogEntry(expr, ex.Message);
                        return false;
                    }
                }
                if (!val.IsNumeric || val.ToDouble() < minValue || val.ToDouble() > maxValue)
                {
                    value = double.NaN;
                    _services.Log.LogEntry(expr, val.IsNumeric ? Errors.IllegalQuantity : Errors.TypeMismatchError);
                    return false;
                }
                value = val.ToDouble();
                return true;
            }
            value = double.NaN;
            return false;
        }
    }
}
