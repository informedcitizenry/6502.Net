//-----------------------------------------------------------------------------
// Copyright (c) 2017-2022 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using Antlr4.Runtime.Misc;
using System;
using System.Linq;
using System.Text;
namespace Sixty502DotNet
{
    /// <summary>
    /// A visitor class for parsed source statements whose methods return a
    /// <see cref="BlockState"/> value. Parse tree nodes of higher level
    /// statement blocks and assembly directive statements are visited. Nodes 
    /// for code gen and expressions are not visited. 
    /// </summary>
    public class BlockVisitor : Sixty502DotNetParserBaseVisitor<BlockState>
    {
        /// <summary>
        /// Construct a new instance of the <see cref="BlockVisitor"/> class.
        /// </summary>
        /// <param name="services">The shared <see cref="AssemblyServices"/>
        /// object.</param>
        public BlockVisitor(AssemblyServices services) => Services = services;

        /// <summary>
        /// Update the value of the <see cref="Label"/> or
        /// <see cref="Constant"/> symbol.
        /// </summary>
        /// <param name="symName">The symbol name.</param>
        /// <param name="value">The value to update the symbol.</param>
        /// <param name="isGlobal">The flag whether the symbol is defined
        /// in the global scope.</param>
        /// <returns>The symbol updated, if resolved, otherwise <c>null</c>.
        /// </returns>
        protected IValueResolver? UpdateLabelOrConstant(string symName, Value value, bool isGlobal = false)
        {
            var scope = isGlobal ? Services.Symbols.GlobalScope : Services.Symbols.Scope;
            var symbol = scope.Resolve(symName) as IValueResolver;
            if (symbol != null)
            {
                var symVal = new Value(symbol.Value);
                Services.State.PassNeeded |= value.IsDefined && symVal.IsDefined && !value.Equals(symVal);
                var updated = symbol.Value.SetAs(value);
                if (updated && !Services.State.PassNeeded && !Services.Symbols.Scope.InFunctionScope)
                {
                    Services.LabelListing.Log(symbol);
                    return symbol;
                }
                if (!updated)
                {
                    Services.Log.LogEntry(((SymbolBase)symbol).Token!, Errors.TypeMismatchError);
                }
            }
            return symbol;
        }

        public override BlockState VisitLabelStat([NotNull] Sixty502DotNetParser.LabelStatContext context)
        {
            if (context.assignExpr() != null)
            {
                _ = Services.ExpressionVisitor.Visit(context.assignExpr());
                if (context.assignExpr().assignOp()?.Start.Type == Sixty502DotNetParser.Equal)
                {
                    if (context.assignExpr().identifier() != null)
                    {
                        var startSym = Evaluator.ResolveIdentifierSymbol(Services.Symbols.Scope,
                            Services.Symbols.ImportedScopes,
                            context.assignExpr().identifier());
                        if (startSym is Variable)
                        {
                            return BlockState.Evaluating;
                        }
                    }
                    var expr = (context.assignExpr().pc == null &&
                    context.assignExpr().programCounter() == null) ?
                    context.assignExpr().expr() : null;
                    GenAssignmentListing(expr);
                }
                return BlockState.Evaluating;
            }
            if (context.expr() == null)
            {
                if (context.label().programCounter() != null)
                {
                    Services.Log.LogEntry(context.label().programCounter(), "Program counter symbol is reserved.");
                    return BlockState.Evaluating;
                }
                GenLineLabelOnly();
                return Visit(context.label());
            }
            if (context.Ident() != null && Services.ExpressionVisitor.ExpressionHasNonConstants(context.expr()))
            {
                Services.Log.LogEntry(context.expr(), Errors.ConstantAssignment);
                return BlockState.Evaluating;
            }
            var value = Services.ExpressionVisitor.Visit(context.expr());
            if (!value.IsDefined) return new BlockState();
            var ident = context.Ident() ?? context.label()?.Ident();
            if (ident != null && (context.op.Type == Sixty502DotNetParser.Equ || context.op.Type == Sixty502DotNetParser.Global))
            {
                UpdateLabelOrConstant(context.Start.Text, value, context.op.Type == Sixty502DotNetParser.Global);
                GenAssignmentListing(context.expr());
            }
            else if (context.label()?.anonymousLabel() != null)
            {
                Services.Log.LogEntry(context, Errors.UnexpectedExpression);
            }
            else if (value.IsDefined && !value.IsNumeric)
            {
                Services.Log.LogEntry(context.expr(), Errors.TypeMismatchError);
            }
            else
            {
                Services.Output.SetPC(value.ToInt());
                GenLineLabelOnly();
            }
            return BlockState.Evaluating;
        }

        private void Output(int directive, Sixty502DotNetParser.ExprContext[] expressions, int expected, int outputIndex)
        {
            if (expressions.Length != expected)
            {
                Services.Log.LogEntry(expressions[expected], Errors.UnexpectedExpression);
                return;
            }
            if (expressions.Length == expected)
            {
                var output = Services.ExpressionVisitor.Visit(expressions[outputIndex]);
                if (output.IsString || output.DotNetType == TypeCode.Char)
                {
                    if (directive == Sixty502DotNetParser.Echo)
                    {
                        if (Services.Options.EchoEachPass || Services.State.CurrentPass == 0)
                        {
                            Console.WriteLine(output.ToString(true));
                        }
                    }
                    else if (!Services.State.PassNeeded && Services.State.CurrentPass == 0)
                    {
                        var error = directive == Sixty502DotNetParser.Error || directive == Sixty502DotNetParser.Errorif;
                        Services.Log.LogEntry(expressions[outputIndex], output.ToString(true), error);
                    }
                }
                else if (output.IsDefined)
                {
                    Console.WriteLine(output);
                }
                return;
            }
        }

        private void Invoke(Sixty502DotNetParser.ExprContext[] expressions)
        {
            if (expressions.Length == 1)
            {
                // is the expression just a function call?
                // identifier: identifier '(' expressionList ')'
                var ident = expressions[0].refExpr()?.identifier();
                if (ident?.LeftParen() != null)
                {
                    var symbol = Evaluator.ResolveIdentifierSymbol(Services.Symbols.GlobalScope, null,
                        ident);
                    if (symbol is FunctionDefinitionBase func)
                    {
                        ArrayValue? args;
                        if (ident.expressionList() != null)
                        {
                            args = Services.ExpressionVisitor.Visit(ident.expressionList()) as ArrayValue;
                            if (!args!.ElementsDefined)
                            {
                                return;
                            }
                        }
                        else
                        {
                            args = new ArrayValue();
                        }
                        // we have to invoke the function directly instead of using the ExpressionVisitor,
                        // in case the function doesn't return a value (which is legal with '.invoke')
                        try
                        {
                            _ = func.Invoke(args);
                        }
                        catch (Exception ex)
                        {
                            Services.Log.LogEntry(expressions[0].refExpr().identifier(),
                                $"Call to function \"{expressions[0].refExpr().identifier().Start.Text}\" failed: {ex.Message}");
                        }
                        return;
                    }
                    Services.Log.LogEntry(expressions[0].refExpr().identifier(), "Symbol is not a function.");
                    return;
                }
                Services.Log.LogEntry(expressions[0], "Expression is not a function call.");
                return;
            }
            Services.Log.LogEntry(expressions[1], Errors.UnexpectedExpression);
        }

        private void Assertion(int directive, Sixty502DotNetParser.ExprContext[] expressions)
        {
            var parameters = expressions.Length;
            if (parameters < 3)
            {
                var isAssert = directive == Sixty502DotNetParser.Assert;
                var cond = Services.ExpressionVisitor.Visit(expressions[0]);
                if (Evaluator.IsCondition(cond))
                {
                    var fail = cond.ToBool();
                    if (isAssert) fail = !fail;
                    if (fail)
                    {
                        if (isAssert && parameters == 1)
                        {
                            Services.Log.LogEntry(expressions[0], "Assertion failed.");
                        }
                        else
                        {
                            Output(directive, expressions, 2, 1);
                        }
                    }
                    return;
                }
                Services.Log.LogEntry(expressions[0], Errors.ExpressionNotCondition);
                return;
            }
            Services.Log.LogEntry(expressions[1], Errors.UnexpectedExpression);
        }

        private void Dsection(Sixty502DotNetParser.AsmDirectiveContext context)
        {
            var stat = (Sixty502DotNetParser.StatContext)context.Parent.Parent.Parent;
            if (stat.label() != null)
            {
                Services.Log.LogEntry(stat, stat.Start, "Label declaration is illegal in the current context " +
                    "because its address is undetermined.");
                return;
            }
            if (Services.State.CurrentPass == 0)
            {
                var expressions = context.expressionList().expr();
                _ = SectionDefiner.Define(context, expressions, Services);
            }
        }

        private void Let(Sixty502DotNetParser.ExprContext[] expressions)
        {
            foreach(var expr in expressions)
            {
                if (expr.assignExpr() != null)
                {
                    _ = Services.ExpressionVisitor.Visit(expr.assignExpr());
                }
                else
                {
                    Services.Log.LogEntry(expr, "\".let\" directive expects an assignment.");
                    break;
                }
            }
        }

        private void MapUnmap(Sixty502DotNetParser.ExprContext[] expressions, bool map)
        {
            if (expressions.Length > 3)
            {
                Services.Log.LogEntry(expressions[3], Errors.UnexpectedExpression);
                return;
            }
            if (expressions.Length >= 2)
            {
                if (!Services.ExpressionVisitor.TryGetPrimaryExpression(expressions[0], out var startExpr))
                {
                    return;
                }
                string mapping;
                if (startExpr.DotNetType == TypeCode.String || startExpr.DotNetType == TypeCode.Char)
                {
                    mapping = startExpr.ToString(true);
                }
                else if (startExpr.IsIntegral && startExpr.ToInt() >= 0 && startExpr.ToInt() <= 0x10FFFF)
                {
                    mapping = char.ConvertFromUtf32(startExpr.ToInt());
                }
                else
                {
                    Services.Log.LogEntry(expressions[0], Errors.ExpectedConstant);
                    return;
                }
                if ( expressions.Length == 3)
                {
                    if (map)
                    {
                        if (!Services.ExpressionVisitor.TryGetPrimaryExpression(expressions[1], out var endExpr))
                        {
                            return;
                        }
                        if (endExpr.DotNetType == TypeCode.String || endExpr.DotNetType == TypeCode.Char)
                        {
                            mapping += endExpr.ToString(true);
                        }
                        else if (endExpr.IsIntegral)
                        {
                            if (endExpr.ToInt() >= 0 && endExpr.ToInt() <= 0x10FFFF)
                            {
                                mapping += char.ConvertFromUtf32(endExpr.ToInt());
                            }
                            else
                            {
                                Services.Log.LogEntry(expressions[1], Errors.IllegalQuantity);
                            }
                        }
                        else
                        {
                            Services.Log.LogEntry(expressions[1], Errors.ExpectedConstant);
                            return;
                        }
                    }
                    else
                    {
                        Services.Log.LogEntry(expressions[2], Errors.UnexpectedExpression);
                        return;
                    }
                }
                if (mapping.Length > 2)
                {
                    Services.Log.LogEntry(expressions[0], "Invalid parameter.");
                    return;
                }
                if (map)
                {
                    if (!Services.ExpressionVisitor.TryGetPrimaryExpression(expressions[^1], out var code))
                    {
                        return;
                    }
                    int codePoint;
                    if (code.IsString && code.ElementCount == 1)
                    {
                        codePoint = char.ConvertToUtf32(code.ToString(true), 0);
                    }
                    else if (code.DotNetType == TypeCode.Char)
                    {
                        codePoint = Services.Encoding.GetEncodedValue(code.ToString(true));
                    }
                    else if (code.IsIntegral)
                    {
                        codePoint = code.ToInt();
                        if (codePoint < 0)
                        {
                            Services.Log.LogEntry(expressions[^1], Errors.IllegalQuantity);
                            return;
                        }
                    }
                    else
                    {
                        Services.Log.LogEntry(expressions[^1], Errors.ExpectedConstant);
                        return;
                    }
                    if (codePoint >= 0 && codePoint <= 0x10FFFF)
                    {
                        Services.Encoding.Map(mapping, codePoint);
                    }
                    else
                    {
                        Services.Log.LogEntry(expressions[^1], Errors.IllegalQuantity);
                    }
                }
                else
                {
                    Services.Encoding.Unmap(mapping);
                }
            }
        }

        private void Encoding(Sixty502DotNetParser.ExprContext[] expressions)
        {
            if (expressions.Length == 1)
            {
                if (!Services.ExpressionVisitor.TryGetPrimaryExpression(expressions[0], out var encoding))
                {
                    return;
                }
                if (encoding.IsString)
                {
                    Services.Encoding.SelectEncoding(encoding.ToString());
                }
                else
                {
                    Services.Log.LogEntry(expressions[0], Errors.StringExpected);
                }
            }
            else
            {
                Services.Log.LogEntry(expressions[1], Errors.UnexpectedExpression);
            }
        }

        private void Section(Sixty502DotNetParser.ExprContext[] expressions)
        {
            if (expressions.Length == 1)
            {
                if (!Services.ExpressionVisitor.TryGetPrimaryExpression(expressions[0], out var section))
                {
                    return;
                }
                if (section.IsString)
                {
                    Services.Output.SetSection(section.ToString(true));
                    if (ListingReady(Services))
                    {
                        Services.StatementListings.Add($"* = ${Services.Output.LogicalPC:x4}  // section {section}");
                    }
                    return;
                }
                Services.Log.LogEntry(expressions[0], Errors.StringExpected);
                return;
            }
            Services.Log.LogEntry(expressions[1], Errors.UnexpectedExpression);
        }

        private void Relocate(Sixty502DotNetParser.ExprContext[] expressions)
        {
            if (expressions.Length == 1)
            {
                if (Services.ExpressionVisitor.TryGetArithmeticExpr(expressions[0], short.MinValue, ushort.MaxValue, out var addr))
                {
                    Services.Output.SetLogicalPC((int)addr & 0xFFFF);
                }
            }
            else
            {
                Services.Log.LogEntry(expressions[1], Errors.UnexpectedExpression);
            }
        }

        private void SynchPC(Sixty502DotNetParser.ExprContext[] expressions)
        {
            if (expressions.Length == 0)
            {
                Services.Output.SynchPC();
            }
            else
            {
                Services.Log.LogEntry(expressions[0], Errors.UnexpectedExpression);
            }
        }

        private void Org(Sixty502DotNetParser.ExprContext[] expressions)
        {
            if (expressions.Length > 1)
            {
                Services.Log.LogEntry(expressions[1], Errors.UnexpectedExpression);
            }
            else
            {
                if (Services.ExpressionVisitor.TryGetArithmeticExpr(expressions[0], short.MinValue, ushort.MaxValue, out var pc))
                {
                    try
                    {
                        Services.Output.SetPC((int)pc);
                        GenLineLabelOnly();
                    }
                    catch (InvalidPCAssignmentException ex)
                    {
                        if (ex.SectionNotUsedError)
                        {
                            throw ex;
                        }
                        if (!Services.State.PassNeeded)
                        {
                            Services.Log.LogEntry(expressions[0], $"Invalid program counter assignment {ex.Message}");
                        }
                    }
                }
                else if (!Services.State.PassNeeded)
                {
                    Services.Log.LogEntry(expressions[0], Errors.IllegalQuantity);
                }
            }
        }

        private void Bank(Sixty502DotNetParser.ExprContext[] expressions)
        {
            if (expressions.Length > 1)
            {
                Services.Log.LogEntry(expressions[1], Errors.UnexpectedExpression);
            }
            else if (Services.ExpressionVisitor.TryGetArithmeticExpr(expressions[0], byte.MinValue, byte.MaxValue, out var bank))
            {
                Services.Output.SetBank((int)bank, Services.Options.ResetPCOnBank);
            }
            else
            {
                Services.Log.LogEntry(expressions[0], Errors.IllegalQuantity);
            }
        }
        
        private void Format(Sixty502DotNetParser.AsmDirectiveContext context)
        {
            if (Services.State.CurrentPass != 0)
            {
                return;
            }
            if (Services.Output.OutputFormat != null)
            {
                Services.Log.LogEntry(context, "\".format\" directive ignored.", false);
                return;
            }
            if (!Services.Output.HasOutput)
            {
                var expressions = context.expressionList().expr();
                if (expressions.Length == 1)
                {
                    if (!Services.ExpressionVisitor.TryGetPrimaryExpression(expressions[0], out var formatName))
                    {
                        return;
                    }
                    if (formatName.IsString)
                    {
                        var format = formatName.ToString(true);
                        try
                        {
                            Services.Output.OutputFormat = OutputFormatSelector.Select(format, Services.CPU);
                            return;
                        }
                        catch (Error err)
                        {
                            Services.Log.LogEntry(expressions[0], err.Message);
                        }
                    }
                    Services.Log.LogEntry(expressions[0], "\".format\" argument must be a string.");
                    return;
                }
                Services.Log.LogEntry(expressions[1], Errors.UnexpectedExpression);
                return;
            }
            Services.Log.LogEntry(context, "Cannot specify target format after assembly has started.");
        }

        protected void Import(Sixty502DotNetParser.DirectiveStatContext context)
        {
            var identifier = context.identifier();
            if (identifier == null)
            {
                Services.Log.LogEntry(context, "Identifier expected.");
                return;
            }
            if (identifier.LeftSquare().Length > 0 || identifier.LeftParen() != null)
            {
                Services.Log.LogEntry(identifier, $"Cannot import \"{identifier.GetText()}\" because it is not a scope.");
                return;
            }
            var ns = Evaluator.ResolveIdentifierSymbol(Services.Symbols.GlobalScope, null, identifier) as NamedMemberSymbol;
            if (!SymbolManager.SymbolIsAScope(ns))
            {
                Services.Log.LogEntry(identifier, $"Cannot import \"{identifier.GetText()}\" because it is not a scope.");
                return;
            }
            if (ReferenceEquals(ns, Services.Symbols.Scope))
            {
                Services.Log.LogEntry(identifier, $"Cannot import \"{identifier.GetText()}\" because it is the current scope.");
                return;
            }
            if (!Services.Symbols.ImportedScopes.Any(s => ReferenceEquals(s, ns)))
            {
                // only import once.
                Services.Symbols.ImportedScopes.Add(ns!);
            }
        }

        public override BlockState VisitAsmDirective([NotNull] Sixty502DotNetParser.AsmDirectiveContext context)
        {
            var directive = context.directive.Type;
            if (context.expressionList() != null)
            {
                var expressions = context.expressionList().expr();
                switch (directive)
                {
                    case Sixty502DotNetParser.Assert:
                    case Sixty502DotNetParser.Errorif:
                    case Sixty502DotNetParser.Warnif:
                        Assertion(directive, expressions);
                        break;
                    case Sixty502DotNetParser.Bank:
                        Bank(expressions);
                        break;
                    case Sixty502DotNetParser.Dsection:
                        Dsection(context);
                        break;
                    case Sixty502DotNetParser.Echo:
                    case Sixty502DotNetParser.Error:
                    case Sixty502DotNetParser.Warn:
                        Output(directive, expressions, 1, 0);
                        break;
                    case Sixty502DotNetParser.Encoding:
                        Encoding(expressions);
                        break;
                    case Sixty502DotNetParser.Endrelocate:
                    case Sixty502DotNetParser.Realpc:
                        SynchPC(expressions);
                        break;
                    case Sixty502DotNetParser.Format:
                        Format(context);
                        break;
                    case Sixty502DotNetParser.Invoke:
                        Invoke(expressions);
                        break;
                    case Sixty502DotNetParser.Let:
                        Let(expressions);
                        break;
                    case Sixty502DotNetParser.Map:
                    case Sixty502DotNetParser.Unmap:
                        MapUnmap(expressions, directive == Sixty502DotNetParser.Map);
                        break;
                    case Sixty502DotNetParser.Org:
                        Org(expressions);
                        break;
                    case Sixty502DotNetParser.Pseudopc:
                    case Sixty502DotNetParser.Relocate:
                        Relocate(expressions);
                        break;
                    case Sixty502DotNetParser.Section:
                        Section(expressions);
                        break;
                    default:
                        Services.Log.LogEntry(context.expressionList(), Errors.UnexpectedExpression);
                        break;
                }
            }
            else
            {
                switch (directive)
                {
                    case Sixty502DotNetParser.Label:
                        Services.Log.LogEntry(context, "\".label\" directive is deprecated.", false);
                        break;
                    case Sixty502DotNetParser.Endrelocate:
                    case Sixty502DotNetParser.Realpc:
                        Services.Output.SynchPC();
                        break;
                    case Sixty502DotNetParser.Forcepass:
                        Services.State.PassNeeded |= Services.State.CurrentPass == 0;
                        break;
                    case Sixty502DotNetParser.Proff:
                    case Sixty502DotNetParser.Pron:
                        Services.State.PrintOff = directive == Sixty502DotNetParser.Proff;
                        break;
                    default:
                        Services.Log.LogEntry(context, Errors.ExpectedExpression);
                        break;
                }
            }
            return BlockState.Evaluating;
        }

        private BlockState Goto(Sixty502DotNetParser.DirectiveStatContext context)
        {
            if (Services.Symbols.Scope.Resolve(context.@goto.Text) is Label label)
            {
                // label exists but is it a valid destination?
                var stat = label.DefinedAt;
                if (stat!.labelStat()?.assignExpr() == null && stat.labelStat()?.Global() == null && stat.labelStat()?.Equ() == null)
                {
                    return new BlockState { gotoDestination = label.DefinedAt, status = Status.Goto };
                }
            }
            Services.Log.LogEntry(context, context.@goto, "Goto destination not valid.");
            return BlockState.Evaluating;
        }

        public override BlockState VisitDirectiveStat([NotNull] Sixty502DotNetParser.DirectiveStatContext context)
        {
            if (context.control != null)
            {
                var stat = (Sixty502DotNetParser.StatContext)context.Parent.Parent;
                var block = (Sixty502DotNetParser.BlockContext)stat.Parent;
                var finalBlockStat = block.stat()[^1];
                if (!ReferenceEquals(stat, finalBlockStat))
                {
                    var statIx = block.stat().ToList().IndexOf(stat);
                    Services.Log.LogEntry(block.stat()[statIx], "Possible unreachable code detected.", false);
                }
                switch (context.control.Type)
                {
                    case Sixty502DotNetParser.Break:
                        return new BlockState { status = Status.Break };
                    case Sixty502DotNetParser.Continue:
                        return new BlockState { status = Status.Continue };
                    case Sixty502DotNetParser.Return:
                        Value? returnValue = null;
                        if (context.expr() != null)
                        {
                            returnValue = Services.ExpressionVisitor.Visit(context.expr());
                        }
                        return new BlockState { returnValue = returnValue, status = Status.Return };
                    default:
                        return Goto(context);
                }
            }
            if (context.Import() != null)
            {
                Import(context);
                return BlockState.Evaluating;
            }
            return base.VisitChildren(context);
        }

        public override BlockState VisitStat([NotNull] Sixty502DotNetParser.StatContext context)
        {
            if (!Services.Symbols.Scope.InFunctionScope)
            {
                Services.State.CurrentStatement = context;
                Services.State.LongLogicalPCOnAssemble = Services.Output.LongLogicalPC;
                Services.State.LogicalPCOnAssemble = Services.Output.LogicalPC;
                if (context.scope != null)
                {
                    Services.Symbols.Scope = context.scope;
                }
            }
            try
            {
                return base.VisitChildren(context);
            }
            catch (Exception ex)
            {
                if (ex is InvalidPCAssignmentException invalidEx && invalidEx.SectionNotUsedError)
                {
                    
                    Services.Log.LogCriticalError(ex.Message);
                    return new BlockState(null, Status.Complete);
                }
                Services.Log.LogEntry(context, ex.Message);
                return BlockState.Evaluating;
            }
        }

        private BlockState SetFunctionVisitor(string fcnName)
        {
            var fcnSym = Services.Symbols.GlobalScope.Resolve(fcnName) as UserFunctionDefinition;
            if (fcnSym?.CanBeInvoked == false)
            {
                fcnSym.Visitor = new BlockVisitor(Services);
            }
            return BlockState.Evaluating;
        }

        public override BlockState VisitBlockStat([NotNull] Sixty502DotNetParser.BlockStatContext context)
        {
            var directive = context.enterBlock()?.directive.Type;
            var state = new BlockState();
            var statContext = (Sixty502DotNetParser.StatContext)context.Parent;
            if (context.enterBlock() != null && Services.Symbols.Scope.InFunctionScope)
            {
                var label = statContext.label()?.Ident();
                if (directive == Sixty502DotNetParser.Block && label != null)
                {
                    var blockLabel = new Label(label.GetText(), Services.Symbols.Scope, statContext, false)
                    {
                        IsBlockScope = true
                    };
                    Services.Symbols.Scope.Define(label.GetText(), blockLabel);
                    Services.Symbols.PushScope(blockLabel);
                }
                else
                {
                    Services.Symbols.PushScope(new AnonymousScope(statContext, Services.Symbols.Scope));
                }
            }
            if (context.block() != null && directive != null)
            {
                if (directive == Sixty502DotNetParser.Function)
                {
                    return SetFunctionVisitor(statContext.label()!.Ident().GetText());
                }
                Sixty502DotNetParser.BlockContext block = context.block();
                if (directive != Sixty502DotNetParser.Block &&
                    directive != Sixty502DotNetParser.Namespace &&
                    directive != Sixty502DotNetParser.Proc)
                {
                    BlockEvaluatorBase blockEvaluator = directive switch
                    {
                        Sixty502DotNetParser.Do      => new DoBlockEvaluator(this, Services, context.exitBlock().expr()),
                        Sixty502DotNetParser.For     => new ForBlockEvaluator(this, Services),
                        Sixty502DotNetParser.Foreach => new ForeachBlockEvaluator(this, Services),
                        Sixty502DotNetParser.Page    => new PageBlockEvaluator(this, Services),
                        Sixty502DotNetParser.Repeat  => new RepeatBlockEvaluator(this, Services),
                        _                            => new WhileBlockEvaluator(this, Services)
                    };
                    state = blockEvaluator.Evaluate(context);
                    if (state.status == Status.Break || state.status == Status.Continue)
                    {
                        state.status = Status.Evaluating;
                    }
                }
                else
                {
                    if ((directive == Sixty502DotNetParser.Block && statContext.label() != null) ||
                        directive == Sixty502DotNetParser.Proc)
                    {
                        if (statContext.label() == null)
                        {
                            // cannot be referenced so just exit.
                            if (Services.State.CurrentPass == 0)
                            {
                                Services.Log.LogEntry(context.enterBlock(), "Unlabeled procedure will not be evaluated.", false);
                            }
                            return BlockState.Evaluating;
                        }
                        var labelToken = statContext.label().Ident();
                        if (directive == Sixty502DotNetParser.Proc && labelToken != null)
                        {
                            Services.State.PassNeeded |= Services.State.CurrentPass == 0;
                            var label = Services.Symbols.Scope.Resolve(labelToken.GetText()) as Label;
                            if (!Services.State.PassNeeded && !label!.IsReferenced)
                            {
                                return BlockState.Evaluating;
                            }
                        }
                        _ = GenLineLabelOnly();
                    }
                    state = Visit(block);
                }
                _ = Visit(context.exitBlock());
                return state;
            }
            if ((context.enterIf() != null && context.ifBlock().block().Length > 0) ||
               (context.enterSwitch() != null && context.switchBlock().caseBlock().Length > 0))
            {
                BlockSelectorBase blockSelector = context.enterIf() != null ?
                    new IfBlockSelector(Services) :
                    new SwitchBlockSelector(Services);
                var selectedBlock = blockSelector.Select(context);
                if (selectedBlock > -1)
                {
                    if (context.enterIf() != null)
                    {
                        return Visit(context.ifBlock().block()[selectedBlock]);
                    }
                    var caseBlocks = context.switchBlock().caseBlock();
                    while (selectedBlock < caseBlocks.Length)
                    {
                        state = Visit(caseBlocks[selectedBlock++].block());
                        if (state.status != Status.Evaluating)
                        {
                            break;
                        }
                        // for fall-through cases we just keep looping.
                        if (selectedBlock < caseBlocks.Length - 1)
                        {
                            // but warn anyway
                            Services.Log.LogEntry(caseBlocks[selectedBlock].enterCase()[0],
                                "Case fell through.", false);
                        }
                    }
                    if (state.status == Status.Break)
                    {
                        // breaking out of a .switch statement should not signal a
                        // break to the outer block.
                        state.status = Status.Evaluating;
                    }
                }
                return state;
            }
            return base.VisitChildren(context);
        }

        public override BlockState VisitEnterEnum([NotNull] Sixty502DotNetParser.EnterEnumContext context)
        {
            if (Services.Symbols.LookupToScope(context.Start.Text) is Enum enumScope)
            {
                Services.Symbols.Scope = enumScope;
            }
            return base.VisitChildren(context);
        }

        public override BlockState VisitEnumDef([NotNull] Sixty502DotNetParser.EnumDefContext context)
        {
            Services.ExpressionVisitor.Visit(context);
            return base.VisitChildren(context);
        }

        public override BlockState VisitBlock([NotNull] Sixty502DotNetParser.BlockContext context)
        {
            var state = new BlockState();
            var blockStats = context.children;
            for(var i = 0; i < blockStats.Count; i++)
            {
                if (blockStats[i] is Sixty502DotNetParser.StatContext statContext)
                {
                    state = Visit(statContext);
                    if (state.status != Status.Evaluating)
                    {
                        if (state.status == Status.Goto &&
                            ReferenceEquals(state.gotoDestination!.Parent, context))
                        {
                            i = state.gotoDestination.index - 1;
                            continue;
                        }
                        break;
                    }
                }
            }
            return state;
        }

      
        public override BlockState VisitExpr([NotNull] Sixty502DotNetParser.ExprContext context)
        {
            var arrowFunc = context.designator()?.arrowFunc();
            Value v = arrowFunc != null ?
                new FunctionValue(arrowFunc, Services) :
                Services.ExpressionVisitor.Visit(context);
            return new BlockState(v, Status.Return);
        }

        public override BlockState VisitExitBlock([NotNull] Sixty502DotNetParser.ExitBlockContext context)
        {
            _ = base.VisitChildren(context);
            Services.Symbols.PopScope();
            if (context.label() != null && ListingReady(Services))
            {
                var pc = Services.Output.LogicalPC;
                Services.StatementListings.Add($"{GenLineListing(Services)}.{pc,-42:x4}{context.label().GetText()}");
            }
            return BlockState.Evaluating;
        }

        public override BlockState VisitExitEnum([NotNull] Sixty502DotNetParser.ExitEnumContext context)
        {
            Services.Symbols.Scope = Services.Symbols.Scope.EnclosingScope!;
            return base.VisitChildren(context);
        }

        private static string GenLineListing(AssemblyServices services)
        {
            if (services.Options.VerboseList && ListingReady(services))
            {
                var startToken = services.State.CurrentStatement!.Start as Token;
                return $"{startToken!.Filename}({startToken.Line + 1}):";
            }
            return string.Empty;
        }

        /// <summary>
        /// Determines whether the current state of assembly allows listing
        /// output.
        /// </summary>
        /// <param name="services">The shared <see cref="AssemblyServices"/>
        /// object.</param>
        /// <returns><c>true</c> if the state allows listing, <c>false</c>
        /// otherwise.</returns>
        protected static bool ListingReady(AssemblyServices services)
             => !string.IsNullOrEmpty(services.Options.ListingFile) &&
                !services.State.PrintOff &&
                !services.Log.HasErrors &&
                !services.Symbols.Scope.InFunctionScope;

        /// <summary>
        /// Generate an assignment listing to the listing object in the
        /// <see cref="AssemblyServices"/>.
        /// </summary>
        /// <param name="expr">The parsed assignment expression.</param>
        /// <returns>The generated listing as a string.</returns>
        public string GenAssignmentListing(Sixty502DotNetParser.ExprContext? expr)
        {
            if (expr != null &&
                ListingReady(Services))
            {
                if (Services.ExpressionVisitor.ExpressionHasNonConstants(expr))
                {
                    return string.Empty;
                }
                var unparsedSource = Services.State.CurrentStatement!.GetSourceLine(Services.Options.VerboseList);
                var value = Services.ExpressionVisitor.Visit(expr);
                if (value.IsDefined && value.IsPrimitiveType && !Services.State.PrintOff)
                {
                    var valuePrintOut = Evaluator.IsBinHexValue(expr) ?
                    $"=${value.ToInt(),-41:x}" :
                    $"={value.ToString().Elliptical(40),-42}";
                    var listing = $"{GenLineListing(Services)}{valuePrintOut}{unparsedSource}";
                    Services.StatementListings.Add(listing);
                    return listing;
                }
                return string.Empty;
            }
            return GenLineLabelOnly();
        }

        private string GenLineLabelOnly()
        {
            if (ListingReady(Services))
            {
                var src = Services.State.CurrentStatement!.GetSourceLine(Services.Options.VerboseList);
                if (Services.State.CurrentStatement!.blockStat() != null)
                {
                    var startToken = Services.State.CurrentStatement.Start;
                    // hide the block directives from listing output
                    src = startToken.Type == Sixty502DotNetParser.Block ? string.Empty : startToken.Text;
                }
                var pc = $".{Services.Output.LogicalPC,-42:x4}";
                var listing = $"{GenLineListing(Services)}{pc}{src}";
                Services.StatementListings.Add(listing);
                return listing;
            }
            return string.Empty;
        }

        /// <summary>
        /// Generate a source listing to the listing object in the
        /// <see cref="AssemblyServices"/>.
        /// </summary>
        /// <param name="services">The shared <see cref="AssemblyServices"/>
        /// object.</param>
        /// <param name="disassembly">The statement's form as disassembly.
        /// </param>
        /// <returns>The generated listing as a string.</returns>
        public static string GenLineListing(AssemblyServices services, string disassembly)
        {
            if (services.State.CurrentStatement != null &&
                ListingReady(services))
            {
                var lineGen = services.Output.LogicalPC - services.State.LogicalPCOnAssemble > 0;
                var sb = new StringBuilder(GenLineListing(services));
                if (!services.Options.NoAssembly && lineGen)
                {
                    var byteString = services.Output.GetBytesFrom(services.State.LongLogicalPCOnAssemble).ToString(services.State.LogicalPCOnAssemble, '.');
                    sb.Append(byteString.PadRight(25));
                }
                if (!services.Options.NoDisassembly && lineGen)
                {
                    if (sb.Length > 29)
                    {
                        sb.Append(' ');
                    }
                    sb.Append(disassembly.PadRight(18));
                }
                if (!services.Options.NoSource && (lineGen || services.Options.VerboseList) && services.State.CurrentStatement.blockStat() == null)
                {
                    sb.Append(services.State.CurrentStatement.GetSourceLine(services.Options.VerboseList));
                }
                services.StatementListings.Add(sb.ToString());
                return sb.ToString();
            }
            return string.Empty;
        }

        /// <summary>
        /// Get the shared <see cref="AssemblyServices"/> object that provides
        /// diagnostic logging, code output, and symbol resolution services.
        /// </summary>
        protected AssemblyServices Services { get; init; }
    }
}
