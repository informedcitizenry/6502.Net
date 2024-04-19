//-----------------------------------------------------------------------------
// Copyright (c) 2017-2024 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using Antlr4.Runtime.Misc;

namespace Sixty502DotNet.Shared;

public sealed partial class Interpreter : SyntaxParserBaseVisitor<int>
{
    private Label DefineLabel(SyntaxParser.LabelContext context)
    {
        NumericValue pc = new(Services.State.Output.LogicalPC);
        if (Services.Evaluator.Resolve(context) is not Label label)
        {
            label = new(context.Start, Services.State.Symbols.ActiveScope);
            try
            {
                Services.State.Symbols.Define(label);
            }
            catch
            {
                throw new SymbolRedefinitionError(context.Start, context.Start);
            }
            label.Value = pc;
            label.Bank = Services.State.Output.CurrentBank;
            context.visited = true;
        }
        else
        {
            if (!context.visited)
            {
                // label was defined somewhere else
                throw new SymbolRedefinitionError(context.Start, label.Token);
            }
            Services.State.PassNeeded |= !label.Value.Equals(pc) && !Services.State.Symbols.InFunctionScope;
            label.Value = pc;
        }
        if (!label.DefinesScope && label.Name[0] != '_')
        {
            Services.State.Symbols.ActiveScope.LocalLabel = label;
        }
        return label;
    }

    private void DefineAnonymousLabel(SyntaxParser.LabelContext context)
    {
        if (Services.State.Symbols.InFunctionScope)
        {
            throw new Error(context, "Anonymous labels cannot be defined in function scopes");
        }
        AnonymousLabel? label = Services.State.Symbols.ActiveScope.AnonymousLabels.Resolve(Services.State.StatementIndex);
        NumericValue pc = new(Services.State.Output.LogicalPC);
        if (label == null)
        {
            label = new(context.GetText()[0]) { Value = pc };
            Services.State.Symbols.ActiveScope.AnonymousLabels.Define(label, Services.State.StatementIndex);
        }
        else
        {
            Services.State.PassNeeded |= !label!.Value.Equals(pc);
            label.Value = pc;
        }
        context.visited = true;
    }

    private void CheckWhiteSpaceLeftOfLabel(SyntaxParser.LabelContext? context)
    {
        if (_options.DiagnosticOptions.WarnWhitespaceBeforeLabels &&
            context?.Start.Column > 0)
        {
            Services.State.Warnings.Add(new Warning(context, "Label does not begin a new line"));
        }
    }

    public override int VisitLabel([NotNull] SyntaxParser.LabelContext context)
    {
        CheckWhiteSpaceLeftOfLabel(context);
        if (context.ident() != null)
        {
            if (context.ident().registerAsIdentifier() != null &&
                Services.DiagnosticOptions.WarnRegistersAsIdentifiers)
            {
                Services.State.Warnings.Add(new Warning(context, "Register is being used as an identifier"));
            }
            _ = DefineLabel(context);
        }
        else
        {
            DefineAnonymousLabel(context);
        }
        return 0;
    }

}

