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
    public override int VisitStatFuncDecl([NotNull] SyntaxParser.StatFuncDeclContext context)
    {
        if (Services.State.InFirstPass)
        {
            if (Services.State.Symbols.ActiveScope != Services.State.Symbols.GlobalScope)
            {
                throw new Error(context, "Named functions can only be defined in the global scope");
            }
            UserFunctionObject func = new(context.argList(), context.block(), null, null);
            Constant funcSym = new(context.Start, func, Services.State.Symbols.GlobalScope);
            Services.State.Symbols.Define(funcSym);
        }
        return 0;
    }

    public override int VisitStatEnumDecl([NotNull] SyntaxParser.StatEnumDeclContext context)
    {
        ScopedSymbol? enumSym;
        if (Services.State.InFirstPass)
        {
            enumSym = Enum.Define(context, Services.State.Symbols);
        }
        else
        {
            enumSym = Services.State.Symbols.ActiveScope.Lookup(context.Start.Text) as ScopedSymbol;
        }
        if (enumSym != null)
        {
            var defs = enumSym.GetSymbols();
            for (int i = 0; i < defs.Count; i++)
            {
                GenListing(context.Start, defs[i].ToString());
            }
        }
        return 0;
    }

    public override int VisitStatConstant([NotNull] SyntaxParser.StatConstantContext context)
    {
        SyntaxParser.ExprContext rvalue = context.expr();
        if (rvalue is SyntaxParser.ExpressionArrowContext &&
            Services.State.Symbols.GlobalScope != Services.State.Symbols.ActiveScope)
        {
            throw new Error(context, "Named functions can only be defined in the global scope");
        }
        ValueBase val = rvalue.value;
        if (Services.State.InFirstPass || !val.IsDefined)
        {
            val = Services.Evaluator.Eval(rvalue);
            if (val is TypeMethodBase)
            {
                throw new Error(rvalue, "Right-hand side expression is a method and cannot be assigned");
            }
            if (Services.State.InFirstPass)
            {
                if (context.equ.Type == SyntaxParser.Global && Services.State.Symbols.InFunctionScope)
                {
                    throw new Error(context, "Cannot define global constant in a function scope");
                }
                IScope scope = context.equ.Type == SyntaxParser.Global ?
                Services.State.Symbols.GlobalScope : Services.State.Symbols.ActiveScope;
                try
                {
                    Services.State.Symbols.Define(new Constant(context.Start, val, scope));
                }
                catch
                {
                    throw new SymbolRedefinitionError(context.Start, context.Start);
                }
            }
            else
            {
                Constant existing;
                if (context.equ.Type == SyntaxParser.Global)
                    existing = (Constant)Services.State.Symbols.GlobalScope.Lookup(context.Start.Text)!;
                else
                    existing = (Constant)Services.State.Symbols.LookupToScope(context.Start.Text)!;
                existing.Value = val;
            }
        }
        GenListing(context, val);
        return 0;
    }

    public override int VisitStatBlock([NotNull] SyntaxParser.StatBlockContext context)
    {
        Services.State.PassNeeded |= Services.State.InFirstPass;
        CheckWhiteSpaceLeftOfLabel(context.name);
        if (context.name?.ident() == null)
        {
            if (context.name != null)
            {
                DefineAnonymousLabel(context.name);
            }
            if (context.b.Type == SyntaxParser.Proc)
            {
                throw new Warning(context, "Unnamed '.proc' will never assemble");
            }
            PushAnonymousScope(context.b.TokenIndex);
        }
        else
        {
            Label label = DefineLabel(context.name);
            label.DefinesScope = true;
            label.IsProcScope = context.b.Type == SyntaxParser.Proc;
            if (label.IsProcScope)
            {
                if (!Services.State.InFirstPass && !label.IsReferenced)
                {
                    return 0;
                }
                Services.State.PassNeeded = Services.State.InFirstPass;
            }
            if (Services.State.Symbols.InFunctionScope)
            {
                throw new Error(context, "Cannot define named scopes inside function blocks");
            }
            if (label.Name[0] == '_')
            {
                throw new Error(context.name.Start, "Cheap local label cannot define block scope");
            }
            Services.State.Symbols.PushScope(label);
            label.LocalLabel = label;
        }
        if (context.name != null)
        {
            GenListing(context.Start, $".{Services.State.Output.LogicalPC,-55:x4}{context.name.GetAllText(false, false)}");
        }
        try
        {
            if (context.block() == null)
            {
                return 0;
            }
            int result = Visit(context.block());
            if (context.end != null)
            {
                _ = Visit(context.end);
            }
            return result;
        }
        finally
        {
            Services.State.Symbols.PopScope();
        }
    }

    public override int VisitStatExpr([NotNull] SyntaxParser.StatExprContext context)
    {
        if (!Evaluator.ExpressionIsAssignmentType(context.expr()))
        {
            throw new Error(context.expr(), "Assignment expression expected");
        }
        return VisitChildren(context);
    }

    public override int VisitStatLabel([NotNull] SyntaxParser.StatLabelContext context)
    {
        CheckWhiteSpaceLeftOfLabel(context.label());
        SyntaxParser.LabelContext? labelCtx = context.label();
        if (labelCtx?.ident() == null)
        {
            if (labelCtx != null)
            {
                DefineAnonymousLabel(labelCtx);
            }
        }
        else
        {
            _ = DefineLabel(labelCtx);
        }
        GenListing(context.Start, $".{Services.State.Output.LogicalPC,-55:x4}{context.label().GetAllText(false, false)}");
        return 0;
    }

    public override int VisitStatInstruction([NotNull] SyntaxParser.StatInstructionContext context)
    {
        if (context.label() != null &&
            context.instruction() is not SyntaxParser.InstructionCpuContext &&
            context.instruction() is not SyntaxParser.InstructionPseudoOpContext &&
            !context.instruction().Start.Type.IsOneOf(SyntaxParser.Align, SyntaxParser.Fill))
        {
            GenListing(context.Start, $".{Services.State.Output.LogicalPC,-55:x4}{context.label().GetAllText(false, _options.OutputOptions.VerboseList)}");
        }
        return VisitChildren(context);
    }

    public override int VisitBlock([NotNull] SyntaxParser.BlockContext context)
    {
        ExecBlock(context);
        return 0;
    }
}
