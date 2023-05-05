//-----------------------------------------------------------------------------
// Copyright (c) 2017-2023 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using Antlr4.Runtime.Misc;

namespace Sixty502DotNet.Shared;

/// <summary>
/// Performs symbol resolution features against a given <see cref="SymbolManager"/>.
/// </summary>
public class SymbolResolver : SyntaxParserBaseVisitor<SymbolBase?>
{
    private readonly SymbolManager _symbols;
    private readonly AssemblyServices _services;

    /// <summary>
    /// Construct a new instance of a symbol resolver.
    /// </summary>
    /// <param name="services">The shared <see cref="AssemblyServices"/> for
    /// assembly runtime.</param>
	public SymbolResolver(AssemblyServices services)
    {
        _services = services;
        _symbols = services.State.Symbols;
    }

    /// <summary>
    /// Resolves a symbol encapsulated by the parsed expression.
    /// </summary>
    /// <param name="expr">The parsed expression.</param>
    /// <returns>The <see cref="SymbolBase"/> if resolution was successful,
    /// <c>null</c> otherwise.</returns>
    public SymbolBase? Resolve(SyntaxParser.ExprContext expr)
    {
        SymbolBase? sym = Visit(expr);
        if (sym != null)
        {
            sym.IsReferenced |= (_symbols.ActiveScope is not ScopedSymbol scope || !scope.IsProcScope || scope.IsReferenced) && !ResolveMemberOnly;
        }
        return sym;
    }

    public override SymbolBase? VisitExpressionIncDec([NotNull] SyntaxParser.ExpressionIncDecContext context)
    {
        return Visit(context.expr());
    }

    public override SymbolBase? VisitExpressionCall([NotNull] SyntaxParser.ExpressionCallContext context)
    {
        // we don't know the type of the return of the call expression so we
        // have to evaluate it first, then save the result to the evaluator for
        // efficiency.
        ValueBase target = _services.Evaluator.Eval(context);
        _services.Evaluator.CachedEvaluations.Push(target);
        return target.Prototype;
    }

    public override SymbolBase? VisitExpressionCollection([NotNull] SyntaxParser.ExpressionCollectionContext context)
    {
        return context.tuple() != null ? Environment.TupleType : Environment.ArrayType;
    }

    public override SymbolBase? VisitExpressionDictionary([NotNull] SyntaxParser.ExpressionDictionaryContext context)
    {
        return Environment.DictionaryType;
    }

    public override SymbolBase? VisitExpressionStringLiteral([NotNull] SyntaxParser.ExpressionStringLiteralContext context)
    {
        return Environment.StringType;
    }

    public override SymbolBase? VisitExpressionPrimary([NotNull] SyntaxParser.ExpressionPrimaryContext context)
    {
        return Environment.PrimitiveType;
    }

    public override SymbolBase? VisitLabel([NotNull] SyntaxParser.LabelContext context)
    {
        return _symbols.LookupToScope(context.GetText());
    }

    public override SymbolBase? VisitExpressionDotMember([NotNull] SyntaxParser.ExpressionDotMemberContext context)
    {
        SymbolBase? parentSymbol = Visit(context.expr());
        string memberName = context.identifierPart().NamePart();
        SymbolBase? memberSym = null;
        if (parentSymbol is IValueResolver resolver)
        {
            memberSym = resolver.Value.Prototype?.Lookup(memberName);
        }
        if (memberSym == null && parentSymbol is ScopedSymbol scopedsymbol)
        {
            memberSym = scopedsymbol.ResolveMember(memberName);
        }
        return memberSym;
    }

    public override SymbolBase? VisitExpressionGrouped([NotNull] SyntaxParser.ExpressionGroupedContext context)
    {
        ValueBase grouped = _services.Evaluator.Eval(context.expr());
        _services.Evaluator.CachedEvaluations.Push(grouped);
        return grouped.Prototype;
    }

    public override SymbolBase? VisitExpressionSubscript([NotNull] SyntaxParser.ExpressionSubscriptContext context)
    {
        SymbolBase? subscript = Visit(context.target);
        if (subscript == null)
        {
            return _services.Evaluator.Eval(context.target).Prototype;
        }
        return subscript;
    }

    public override SymbolBase? VisitExpressionSimpleIdentifier([NotNull] SyntaxParser.ExpressionSimpleIdentifierContext context)
    {
        SymbolBase? sym;
        if (ResolveMemberOnly)
        {
            sym = _symbols.LookupToScope(context.GetText());
        }
        else
        {
            sym = _symbols.Lookup(context.GetText());
        }
        for (int i = 0; i < _symbols.ImportedScopes.Count && sym == null; i++)
        {
            sym = _symbols.ImportedScopes[i].Lookup(context.GetText());
        }
        if (sym != null && _services.DiagnosticOptions.WarnCaseMismatch && !sym.Name.Equals(context.GetText()))
        {
            string message = $"Symbol '{sym.Name}' case does not match";
            if (_services.DiagnosticOptions.WarningsAsErrors)
            {
                throw new Error(context, message);
            }
            _services.State.Warnings.Add(new Warning(context, message));
        }
        return sym;
    }

    /// <summary>
    /// Get or set the flag to resolve members of the selected scope only.
    /// </summary>
    public bool ResolveMemberOnly { get; set; }
}

