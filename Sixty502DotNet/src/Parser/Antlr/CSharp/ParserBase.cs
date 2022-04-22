//-----------------------------------------------------------------------------
// Copyright (c) 2017-2022 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using Antlr4.Runtime;
using System;
using System.IO;
using System.Collections.Generic;

namespace Sixty502DotNet
{
    /// <summary>
    /// The base class parser responsible for parsing assembly source code.
    /// This class is inherited by the generated parser.
    /// </summary>
    public abstract class ParserBase : Parser
    {
        private int _breaks;
        private int _continues;
        private bool _inFunction;

        private readonly static Dictionary<int, int> s_endDirectives = new()
        {
            { Sixty502DotNetParser.Block,        Sixty502DotNetParser.Endblock },
            { Sixty502DotNetParser.Do,            Sixty502DotNetParser.Whiletrue },
            { Sixty502DotNetParser.For,            Sixty502DotNetParser.Next },
            { Sixty502DotNetParser.Foreach,        Sixty502DotNetParser.Next },
            { Sixty502DotNetParser.Function,    Sixty502DotNetParser.Endfunction },
            { Sixty502DotNetParser.Namespace,    Sixty502DotNetParser.Endnamespace },
            { Sixty502DotNetParser.Page,        Sixty502DotNetParser.Endpage },
            { Sixty502DotNetParser.Repeat,        Sixty502DotNetParser.Endrepeat },
            { Sixty502DotNetParser.While,        Sixty502DotNetParser.Endwhile }
        };
        private readonly static HashSet<int> s_controls = new()
        {
            Sixty502DotNetParser.Do,
            Sixty502DotNetParser.Whiletrue,
            Sixty502DotNetParser.For,
            Sixty502DotNetParser.Next,
            Sixty502DotNetParser.Foreach,
            Sixty502DotNetParser.Repeat,
            Sixty502DotNetParser.Endrepeat,
            Sixty502DotNetParser.While,
            Sixty502DotNetParser.Endwhile
        };

        /// <summary>
        /// Construct a new instance of the <see cref="ParserBase"/> class.
        /// </summary>
        /// <param name="input">The input token stream.</param>
        protected ParserBase(ITokenStream input)
            : this(input, TextWriter.Null, TextWriter.Null) { }

        /// <summary>
        /// Construct a new instance of the <see cref="ParserBase"/> class.
        /// </summary>
        /// <param name="input">The input token stream.</param>
        /// <param name="output">The output to send output info.</param>
        /// <param name="errorOutput">The output to report error info.</param>
        protected ParserBase(ITokenStream input, TextWriter output, TextWriter errorOutput)
            : base(input, output, errorOutput)
        {
            _breaks = _continues = 0;
            _inFunction = false;
            Symbols = new SymbolManager(false);
            LabelsAfterWhitespace = new List<IToken>();
        }

        /// <summary>
        /// Set the current block context scope.
        /// </summary>
        /// <param name="context">The parsed block of statements.</param>
        protected void SetScope(Sixty502DotNetParser.BlockContext context)
        {
            if (!_inFunction)
            {
                context.scope = Symbols.Scope;
            }
        }

        /// <summary>
        /// Set the statement annotations.
        /// </summary>
        /// <param name="context">The parsed statement.</param>
        protected void SetAnnotations(Sixty502DotNetParser.StatContext context)
        {
            if (!_inFunction &&
                context.scope == null /* certain directives might have already set
                                         the scope before we arrived here, e.g. .for and .foreach
                                       */)
            {
                context.scope = Symbols.Scope;
            }
            context.index = context.Parent.ChildCount - 1;
        }

        /// <summary>
        /// Create an anonymous label symbol.
        /// </summary>
        /// <param name="context">The parsed label context.</param>
        protected void CreateAnonymousLabel(Sixty502DotNetParser.LabelContext context)
        {
            if (!_inFunction)
            {
                var labelName = context.Start.Text;
                if (labelName.Length == 1)
                {
                    var type = labelName[0] == '+' ? AnonymousLabel.Forward : AnonymousLabel.Backward;
                    var anonymousLabel = new AnonymousLabel(labelName, type, context.Start.TokenIndex);
                    var scopeToResolve = Symbols.Scope;
                    if (Symbols.Scope is Label l && !l.IsBlockScope)
                    {
                        scopeToResolve = l.EnclosingScope ?? Symbols.Scope;
                    }
                    scopeToResolve.Define(context.Start.Text, anonymousLabel);
                }
                else
                {
                    NotifyErrorListeners(context.Start, "Invalid line reference.", new CustomParseError());
                }
            }
            else
            {
                NotifyErrorListeners(context.Start, "Illegal line reference in a function block.", new CustomParseError());
            }
        }

        /// <summary>
        /// Create a global constant.
        /// </summary>
        /// <param name="context">The parsed label statement context.</param>
        protected void CreateGlobal(Sixty502DotNetParser.LabelStatContext context)
            => CreateNamedSymbol((Sixty502DotNetParser.StatContext)context.Parent,
                context.Ident().Symbol, Sixty502DotNetParser.Global);

        /// <summary>
        /// Create a label.
        /// </summary>
        /// <param name="labelContext">The parsed label context.</param>
        protected void CreateLabel(Sixty502DotNetParser.LabelContext labelContext)
        {
            if (labelContext.Ident() != null)
            {
                var statContext = labelContext.Parent as Sixty502DotNetParser.StatContext;
                RuleContext context = labelContext;
                while (statContext == null)
                {
                    context = context.Parent;
                    statContext = context as Sixty502DotNetParser.StatContext;
                }
                if (statContext != null)
                {
                    CreateNamedSymbol(statContext, labelContext.Ident().Symbol, Sixty502DotNetParser.Label);
                }
            }
        }

        /// <summary>
        /// Create an enum definition..
        /// </summary>
        /// <param name="context">The parsed enum definition context.</param>
        protected void CreateEnum(Sixty502DotNetParser.EnumDefContext context) =>
            CreateNamedSymbol((Sixty502DotNetParser.StatContext)context.Parent.Parent,
                context.Start,
                Sixty502DotNetParser.Label);

        /// <summary>
        /// Create a label.
        /// </summary>
        /// <param name="context">The parsed statement context.</param>
        protected void CreateLabel(Sixty502DotNetParser.StatContext context)
        {
            if (context.Start.Type == Sixty502DotNetLexer.Ident)
            {
                CreateNamedSymbol(context, context.Start, Sixty502DotNetParser.Label);
            }
        }

        /// <summary>
        /// Create a label.
        /// </summary>
        /// <param name="context">The parsed label statement context.</param>
        protected void CreateLabel(Sixty502DotNetParser.LabelStatContext context)
        {
            var isIdent = context.Ident() != null ||
                          context.label()?.Ident() != null ||
                          context.assignExpr()?.identifier()?.Ident() != null;
            if (isIdent &&
                (context.assignExpr() == null || context.assignExpr().assignOp()?.Start.Type == Sixty502DotNetParser.Equal))
            {
                int type;
                if (context.assignExpr() != null || context.op?.Type == Sixty502DotNetParser.Equ)
                {
                    type = Sixty502DotNetParser.Equ;
                }
                else if (context.op?.Type == Sixty502DotNetParser.Global)
                {
                    type = Sixty502DotNetParser.Global;
                }
                else
                {
                    type = Sixty502DotNetParser.Label;
                }
                CreateNamedSymbol(
                    (Sixty502DotNetParser.StatContext)context.Parent,
                    context.Start,
                    type);
            }
        }

        /// <summary>
        /// Set the end directive for the block being parsed.
        /// </summary>
        /// <param name="context">The parsed block statement context.</param>
        protected static void SetEndDirective(Sixty502DotNetParser.BlockStatContext context)
            => context.endDirective = s_endDirectives[context.enterBlock().directive.Type];

        private void CreateNamedSymbol(Sixty502DotNetParser.StatContext context, IToken symbol, int type)
        {
            if (symbol?.Type == Sixty502DotNetParser.Ident)
            {
                if (_inFunction && type != Sixty502DotNetParser.Label && type != Sixty502DotNetParser.Equ)
                {
                    NotifyErrorListeners(symbol, "Directive not valid in function call.", new CustomParseError());
                    return;
                }
                if (symbol.Text.Equals("_"))
                {
                    if (type == Sixty502DotNetParser.Enum)
                    {
                        NotifyErrorListeners(symbol, "Invalid use of discard label for enum definition.", new CustomParseError());
                    }
                    return;
                }
                SymbolBase? sym = null;
                Sixty502DotNetParser.ExprContext? exprContext = null;
                switch (type)
                {
                    case Sixty502DotNetParser.Namespace:
                        sym = new Namespace(symbol.Text, Symbols.Scope);
                        break;
                    case Sixty502DotNetParser.Enum:
                        sym = new Enum(symbol.Text, Symbols.Scope);
                        break;
                    case Sixty502DotNetParser.Global:
                    case Sixty502DotNetParser.Equ:
                        exprContext = context.labelStat()?.expr() ??
                                        context.labelStat()?.assignExpr()?.expr();
                        sym = new Constant(symbol.Text, Value.Undefined())
                        {
                            IsReferenced = false
                        };
                        break;
                    case Sixty502DotNetParser.Label:
                        sym = new Label(symbol.Text, Symbols.Scope, context);
                        break;
                }
                if (sym != null)
                {
                    sym.DefinedAt = context;
                    if (sym is Constant c)
                    {
                        c.Expression = exprContext;
                    }
                    try
                    {
                        if (type == Sixty502DotNetParser.Global)
                        {
                            Symbols.GlobalScope.Define(symbol.Text, sym);
                        }
                        else
                        {
                            var symIsCheapLocal = Symbols.Scope is Label &&
                            Sixty502DotNetParser.Global != type &&
                            Label.IsCheapLocal(sym);
                            IScope? localScope = null;
                            if (!symIsCheapLocal)
                            {
                                localScope = Symbols.PopLocalLabel();
                            }
                            Symbols.Scope.Define(symbol.Text, sym);
                            if (!symIsCheapLocal && sym is IScope scope)
                            {
                                Symbols.PushScope(scope);
                            }
                            else if (localScope != null)
                            {
                                Symbols.PushScope(localScope);
                            }
                        }
                        sym.Token = (Token)symbol;
                        if (sym is Label && symbol.Column != 0 &&
                            char.IsWhiteSpace((char)symbol.InputStream.LA(-1)))
                        {
                            LabelsAfterWhitespace.Add(symbol);
                        }
                    }
                    catch (Exception ex)
                    {
                        if (Symbols.Scope.Resolve(symbol.Text) is Namespace existing && sym is Namespace)
                        {
                            Symbols.PushScope(existing);
                        }
                        else
                        {
                            NotifyErrorListeners(symbol, ex.Message, new CustomParseError());
                        }
                    }
                }
            }
        }

        private void CreateNamespace(Sixty502DotNetParser.StatContext context, Sixty502DotNetParser.IdentifierContext identifier)
        {
            var identName = identifier.Ident()?.Symbol;
            if (identName != null)
            {
                CreateNamedSymbol(context, identName, Sixty502DotNetParser.Namespace);
            }
            else if (identifier.op?.Type != Sixty502DotNetParser.Dot)
            {
                NotifyErrorListeners(identifier.op, "Syntax error.", new CustomParseError());
            }
            else
            {
                CreateNamespace(context, identifier.lhs);
                ((Namespace)Symbols.Scope).IsNested = true;
                CreateNamespace(context, identifier.rhs);
            }
        }

        /// <summary>
        /// Update the parser state upon entering a block definition.
        /// </summary>
        /// <param name="context">The enter block context.</param>
        protected void EnterBlock(Sixty502DotNetParser.EnterBlockContext context)
        {
            var statContext = (Sixty502DotNetParser.StatContext)context.Parent.Parent;
            if (statContext.label()?.Ident() != null)
            {
                if (context.directive.Type == Sixty502DotNetParser.Function)
                {
                    _inFunction = true;
                }
                else
                {
                    CreateLabel(statContext.label());
                    if (Symbols.Scope is Label blockLabel)
                    {
                        if (Label.IsCheapLocal(blockLabel))
                        {
                            NotifyErrorListeners(context.Start, "Invalid use of cheap local as a block label.", new CustomParseError());
                            return;
                        }
                        blockLabel.IsBlockScope = true;
                    }
                }
            }
            if (context.directive.Type == Sixty502DotNetParser.Namespace && context.@namespace != null)
            {
                Symbols.PopLocalLabel();
                CreateNamespace(statContext, context.@namespace);
            }
            else if (statContext.label()?.Ident() == null || context.directive.Type == Sixty502DotNetParser.Function)
            {
                Symbols.PopLocalLabel();
                var anon = new AnonymousScope(context, Symbols.Scope);
                if (context.directive.Type == Sixty502DotNetParser.For ||
                    context.directive.Type == Sixty502DotNetParser.Foreach)
                {
                    // enclosing the statement's scope in the anonymous scope for the
                    // variable handling
                    statContext.scope = anon;
                }
                Symbols.PushScope(anon);
            }
            if (s_controls.Contains(context.directive.Type))
            {
                _continues++;
                _breaks++;
            }
        }

        /// <summary>
        /// Declare a function symbol.
        /// </summary>
        /// <param name="context">The parsed block statement context.</param>
        protected void DeclareFunction(Sixty502DotNetParser.BlockStatContext context)
        {
            var statContext = (Sixty502DotNetParser.StatContext)context.Parent;
            if (statContext.Start.Type != Sixty502DotNetParser.Ident)
            {
                NotifyErrorListeners(context.Start, "Function definition requires a valid identifier.", new CustomParseError());
                return;
            }
            try
            {
                if (Symbols.Scope is AnonymousScope anon && ReferenceEquals(anon.EnclosingScope, Symbols.GlobalScope))
                {
                    var userFunction = UserFunctionDefinition.Declare(context.enterBlock(), Symbols);
                    if (Symbols.GlobalScope.Resolve(userFunction.Name) == null)
                    {
                        Symbols.GlobalScope.Define(userFunction.Name, userFunction);
                        /* we declare functions in global but make their enclosing scope the anonymous one in order to
                         * "protect" labels and constants in higher scopes from being "seen" from within an executing fuction if
                         * they share the same name as symbols inside the function.
                         * e.g.:
                         * 
                         * // in global scope:
                         * myconst = 3
                         * myfunc .function
                         * myconst = 2 // in its own static (anonymous) scope, not the same symbol
                         *          .endfunction
                         */
                        userFunction.Scope = anon;
                        return;
                    }
                    NotifyErrorListeners(statContext.label().Start,
                        string.Format(Errors.SymbolExistsError,
                        statContext.label().Start.Text),
                        new CustomParseError());
                }
                else
                {
                    NotifyErrorListeners(context.enterBlock().directive,
                        "Function definition occured inside a block.",
                        new CustomParseError());
                }
            }
            catch (Error e)
            {
                NotifyErrorListeners(e.Token, e.Message, new CustomParseError());
            }
        }

        /// <summary>
        /// Create an enum symbol.
        /// </summary>
        /// <param name="context">The parsed block statement context.</param>
        protected void EnterEnum(Sixty502DotNetParser.BlockStatContext context)
        {
            var statContext = (Sixty502DotNetParser.StatContext)context.Parent;
            if (statContext.Start.Type != Sixty502DotNetParser.Ident)
            {
                NotifyErrorListeners(statContext.Start, "Invalid enumeration definition.", new CustomParseError());
            }
            else
            {
                CreateNamedSymbol(statContext, statContext.Start, Sixty502DotNetParser.Enum);
            }
        }

        /// <summary>
        /// Update parser state upon entering switch statement.
        /// </summary>
        protected void EnterSwitch() => _breaks++;

        /// <summary>
        /// Update parser state upon existing switch statement.
        /// </summary>
        /// <param name="context"></param>
        protected void ExitBlock(Sixty502DotNetParser.ExitBlockContext context)
        {
            if (s_controls.Contains(context.directive.Type))
            {
                _continues--;
                _breaks--;
            }
            if (context.label() != null)
            {
                CreateLabel(context.label());
            }
            Symbols.PopLocalLabel(); // pop local label first before declaring the function
            if (context.directive.Type == Sixty502DotNetParser.Endfunction)
            {
                DeclareFunction((Sixty502DotNetParser.BlockStatContext)context.Parent);
                _inFunction = false;
            }
            Symbols.PopScope();
            var current = Symbols.Scope as Namespace;
            while (current?.IsNested == true)
            {
                Symbols.PopScope();
                current = Symbols.Scope as Namespace;
            }
        }

        /// <summary>
        /// Set the value of the parsed primary expression.
        /// </summary>
        /// <param name="context">The parsed primary expression context.</param>
        protected void SetPrimaryExprValue(Sixty502DotNetParser.PrimaryExprContext context)
        {
            try
            {
                context.value = Evaluator.GetPrimaryExpression(context.constExpr);
            }
            catch (OverflowException)
            {
                NotifyErrorListeners(context.constExpr, Errors.IllegalQuantity, new CustomParseError());
            }
            catch (Error err)
            {
                NotifyErrorListeners(context.constExpr, err.Message, new CustomParseError());
            }
        }

        /// <summary>
        /// Update the parser state upon exiting a parsed switch.
        /// </summary>
        protected void ExitSwitch() => _breaks--;

        /// <summary>
        /// Update the parser state upon existing a parsed enum context.
        /// </summary>
        /// <param name="context">The parsed enum close.</param>
        protected void ExitEnum(Sixty502DotNetParser.ExitEnumContext context)
        {
            Symbols.PopLocalLabel();
            ((Sixty502DotNetParser.StatContext)context.Parent.Parent).scope = Symbols.Scope;
            Symbols.PopScope();
        }

        /// <summary>
        /// Check the validity of an immediate mode statement.
        /// </summary>
        /// <param name="context">The parsed immediate mode statement.</param>
        protected void CheckImm(Sixty502DotNetParser.ImmStatContext context)
        {
            if (context.expr().Start.StartIndex - context.Hash().Symbol.StartIndex > 1)
            {
                NotifyErrorListeners(context.Hash().Symbol, "Unexpected expression.", new CustomParseError());
            }
        }

        /// <summary>
        /// Check the validity of the parse of the binary number.
        /// </summary>
        /// <param name="context">The parsed expression.</param>
        protected void CheckBinary(Sixty502DotNetParser.ExprContext context)
        {
            var digitsSymbol = context.BinaryDigits() ?? context.BinaryDigitsDouble();
            if (digitsSymbol.Symbol.StartIndex - context.op.StartIndex > 1)
            {
                NotifyErrorListeners(context.op, "Unexpected expression.", new CustomParseError());
            }
        }

        /// <summary>
        /// Check the validity of the appearance of a <c>.break</c> in the
        /// current context.
        /// </summary>
        /// <param name="context">The parsed directive statement.</param>
        protected void CheckBreak(Sixty502DotNetParser.DirectiveStatContext context)
        {
            if (_breaks == 0)
            {
                NotifyErrorListeners(context.control, "Nothing to break from.", new CustomParseError());
            }
        }

        /// <summary>
        /// Check the validity of the appearance of a <c>.continue</c> in the
        /// current context.
        /// </summary>
        /// <param name="context">The parsed directive statement.</param>
        protected void CheckContinue(Sixty502DotNetParser.DirectiveStatContext context)
        {
            if (_continues == 0)
            {
                NotifyErrorListeners(context.control, "\".continue\" not valid in this context.", new CustomParseError());
            }
        }

        /// <summary>
        /// Check the validity of the appearance of a <c>.return</c> in the
        /// current context.
        /// </summary>
        /// <param name="context">The parsed directive statement.</param>
        protected void CheckReturn(Sixty502DotNetParser.DirectiveStatContext context)
        {
            if (!_inFunction)
            {
                NotifyErrorListeners(context.control, "\".return\" only valid in functions.", new CustomParseError());
            }
        }

        /// <summary>
        /// Check if the imported symbol is valid.
        /// </summary>
        /// <param name="context">The parsed directive statement.</param>
        protected void CheckImport(Sixty502DotNetParser.DirectiveStatContext context)
        {
            if (!ReferenceEquals(Symbols.Scope, Symbols.GlobalScope))
            {
                NotifyErrorListeners(context.Start,
                    "\".import\" directive can only be made in the global scope.", new CustomParseError());
                return;
            }
            var symbol = Evaluator.ResolveIdentifierSymbol(Symbols.Scope, null, context.identifier()) as NamedMemberSymbol;
            if (!SymbolManager.SymbolIsAScope(symbol))
            {
                NotifyErrorListeners(context.identifier().Start,
                    $"Cannot import \"{context.identifier().GetText()}\" because it is not a scope.", new CustomParseError());
            }
        }

        /// <summary>
        /// Check if code generation is being done in the context of a function
        /// block definition.
        /// </summary>
        /// <param name="context">The parsed assembly statement.</param>
        protected void CheckCodeGenInFunction(Sixty502DotNetParser.AsmStatContext context)
        {
            if (_inFunction)
            {
                if (context.pseudoOpStat() != null)
                    NotifyErrorListeners(context.pseudoOpStat().Start,
                        "Operation not allowed in a function block.", new CustomParseError());
                else if (context.cpuStat() != null)
                    NotifyErrorListeners(context.cpuStat().Start,
                        "Operation not allowed in a function block.", new CustomParseError());
            }
        }

        /// <summary>
        /// Gets or sets the symbols the parser tracks in order to determine
        /// if identifier tokens have already been defined as symbols.
        /// </summary>
        public SymbolManager Symbols { get; set; }

        /// <summary>
        /// Get a list of labels that do not begin a line of source code, but
        /// come after whitespace.
        /// </summary>
        public List<IToken> LabelsAfterWhitespace { get; init; }
    }
}

