//-----------------------------------------------------------------------------
// Copyright (c) 2017-2022 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using Antlr4.Runtime;
using System.Collections.Generic;

namespace Sixty502DotNet
{
    /// <summary>
    /// A scoped symbol that represents the parsed function definition including
    /// the function parameter signature and body.
    /// </summary>
    public class UserFunctionDefinition : FunctionDefinitionBase
    {
        private static List<FunctionArg> GetArgs(Sixty502DotNetParser.ArgListContext args)
        {
            var functionArgs = new List<FunctionArg>();
            if (args != null)
            {
                foreach (var arg in args.arg())
                {
                    IToken argName = arg.assignExpr()?.identifier()?.name ?? arg.name;
                    if (argName == null)
                    {
                        throw new Error(arg, "Missing or invalid parameter name.");
                    }
                    Value defaultValue;
                    var expr = arg.assignExpr()?.expr();
                    if (expr != null)
                    {
                        defaultValue = Evaluator.GetPrimaryExpression(expr.primaryExpr());
                        if (!defaultValue.IsDefined)
                        {
                            throw new Error(expr, "Parameter default must be a constant expression.");
                        }
                        functionArgs.Add(new FunctionArg(argName.Text, defaultValue));
                    }
                    else
                    {
                        functionArgs.Add(new FunctionArg(argName.Text, variantType: true));
                    }
                }
            }
            return functionArgs;
        }

        /// <summary>
        /// Declares a function from the parsed
        /// <see cref="Sixty502DotNetParser.EnterBlockContext"/> tree and
        /// returns a <see cref="UserFunctionDefinition"/> symbol.
        /// </summary>
        /// <param name="context">The parsed block context.</param>
        /// <param name="symbols">A <see cref="SymbolManager"/> to define local
        /// symbols.</param>
        /// <returns>A <see cref="UserFunctionDefinition"/> object.</returns>
        public static UserFunctionDefinition Declare(Sixty502DotNetParser.EnterBlockContext context, SymbolManager symbols)
        {
            var statContext = (Sixty502DotNetParser.StatContext)context.Parent.Parent;
            if (statContext.label()?.Ident().Symbol == null)
            {
                throw new Error(statContext, "Function definition requires valid identifier.");
            }
            var functionArgs = GetArgs(context.argList());
            var blockStat = (Sixty502DotNetParser.BlockStatContext)context.Parent;
            IScope blockScope = symbols.Scope;
            return new UserFunctionDefinition((Token)statContext.label().Ident().Symbol, functionArgs, blockStat.block(), blockScope, symbols);
        }

        /// <summary>
        /// Declares a function from the parsed
        /// <see cref="Sixty502DotNetParser.ArrowFuncContext"/> tree and
        /// returns a <see cref="UserFunctionDefinition"/> symbol.
        /// </summary>
        /// <param name="context">The parsed arrow function context.</param>
        /// <param name="symbols">A <see cref="SymbolManager"/> to define local
        /// symbols.</param>
        /// <returns>A <see cref="UserFunctionDefinition"/> object.</returns>
        public static UserFunctionDefinition Declare(Sixty502DotNetParser.ArrowFuncContext context, SymbolManager symbols)
        {
            var functionArgs = GetArgs(context.argList());
            IScope blockScope = symbols.Scope;
            if (context.block() != null)
            {
                return new UserFunctionDefinition((Token)context.Arrow().Symbol, functionArgs, context.block(), blockScope, symbols);
            }
            return new UserFunctionDefinition((Token)context.Arrow().Symbol, functionArgs, context.expr(), blockScope, symbols);
        }

        private readonly Sixty502DotNetParser.BlockContext? _block;
        private readonly Sixty502DotNetParser.ExprContext? _inlineExpr;
        private readonly SymbolManager _symbolManager;

        private UserFunctionDefinition(Token token,
                             IList<FunctionArg> args,
                             Sixty502DotNetParser.ExprContext? expr,
                             IScope blockScope,
                             SymbolManager symbolManager)
            : base("=>", args)
        {
            _block = null;
            _inlineExpr = expr;
            _symbolManager = symbolManager;
            Scope = blockScope;
            Token = token;IsReferenced = false;
        }

        private UserFunctionDefinition(Token token,
                             IList<FunctionArg> args,
                             Sixty502DotNetParser.BlockContext? block,
                             IScope blockScope,
                             SymbolManager symbolManager)
            : base(token.Text, args)
        {
            _inlineExpr = null;
            _block = block;
            _symbolManager = symbolManager;
            Scope = blockScope;
            Token = token;
            IsReferenced = false;
        }

        protected override Value? OnInvoke(ArrayValue args)
        {
            if (Visitor == null || (_block == null && _inlineExpr == null))
            {
                return null;
            }
            var currentScope = _symbolManager.Scope;
            try
            {
                var functionScope = new FunctionCallScope(Name, Scope!);
                _symbolManager.Scope = functionScope;
                for (int i = 0; i < Args.Count; i++)
                {
                    Value value;
                    if (i < args.Count)
                    {
                        value = args[i];
                    }
                    else
                    {
                        value = Args[i].DefaultValue;
                    }
                    var arg = new Variable(Args[i].Name, value);
                    functionScope.Define(Args[i].Name, arg);
                    _symbolManager.DeclareVariable(arg);
                }
                var result = _block == null ?
                    Visitor.Visit(_inlineExpr) :
                    Visitor.Visit(_block);
                return result.returnValue;
            }
            finally
            {
                _symbolManager.Scope = currentScope;
            }
        }

        /// <summary>
        /// Get or set the function's <see cref="BlockVisitor"/>.
        /// </summary>
        public BlockVisitor? Visitor { get; set; }

        /// <summary>
        /// Get if the function is able to be invoked.
        /// </summary>
        public bool CanBeInvoked => Visitor != null;
    }
}
