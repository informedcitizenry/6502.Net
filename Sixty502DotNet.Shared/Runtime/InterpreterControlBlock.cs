//-----------------------------------------------------------------------------
// Copyright (c) 2017-2023 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using Antlr4.Runtime.Misc;

namespace Sixty502DotNet.Shared;

public sealed partial class Interpreter : SyntaxParserBaseVisitor<int>
{
    private bool IfCondition(SyntaxParser.IfBlockContext context, SyntaxParser.ExprContext condExpression)
    {
        if (context.Ifdef() != null ||
            context.Ifndef() != null ||
            context.Elseifdef().Length > 0 ||
            context.Elseifndef().Length > 0)
        {
            if (!Services.State.InFirstPass &&
                _memoizedDefines.TryGetValue(condExpression.Start.TokenIndex, out bool defined))
            {
                return defined;
            }
            if (context.Ifdef() != null || context.Elseifdef().Length > 0)
            {
                defined = Services.Evaluator.Resolve(condExpression, true, false) != null;
            }
            else
            {
                defined = Services.Evaluator.Resolve(condExpression, true, false) == null;
            }
            _memoizedDefines[condExpression.Start.TokenIndex] = defined;
            return defined;
        }
        if (context.Ifconst() != null ||
            context.Ifnconst() != null ||
            context.Elseifconst().Length > 0 ||
            context.Elseifnconst().Length > 0)
        {
            ValueBase constVal = Evaluator.EvalConstant(condExpression);
            if (context.Ifconst() != null || context.Elseifconst().Length > 0)
            {
                return constVal.IsDefined;
            }
            return !constVal.IsDefined;
        }
        if (context.If() != null || context.Elseif().Length > 0)
        {
            ValueBase condVal = Services.Evaluator.Eval(condExpression);
            return condVal.IsDefined && condVal.AsBool();
        }
        return true;
    }

    public override int VisitInstructionIf([NotNull] SyntaxParser.InstructionIfContext context)
    {
        SyntaxParser.IfBlockContext ifBlock = context.ifBlock();
        SyntaxParser.BlockContext[] ifBlocks = ifBlock.block();
        SyntaxParser.ExprContext[] exprBlocks = ifBlock.expr();
        int selected = -1;
        for (int i = 0; i < ifBlocks.Length; i++)
        {
            if (i >= exprBlocks.Length || IfCondition(ifBlock, exprBlocks[i]))
            {
                selected = i;
                break;
            }
        }
        if (selected >= 0 && ifBlocks[selected] != null)
        {
            return Visit(ifBlocks[selected]);
        }
        return 0;
    }

    public override int VisitInstructionSwitch([NotNull] SyntaxParser.InstructionSwitchContext context)
    {
        ValueBase test = Services.Evaluator.Eval(context.expr());
        if (!test.IsDefined)
        {
            return 0;
        }
        int defaultCase = -1;
        int selected = -1;

        SyntaxParser.CaseBlockContext[] caseBlocks = context.caseBlock();
        for (int i = 0; i < caseBlocks.Length; i++)
        {
            SyntaxParser.CaseBlockContext caseBlock = caseBlocks[i];
            if (caseBlock.Default().Length > 0)
            {
                if (caseBlock.Default().Length != 1 || defaultCase > -1)
                {
                    throw new Error(caseBlock.Default()[^1].Symbol, "Default case label already defined");
                }
                defaultCase = i;
            }
            SyntaxParser.ExprContext[] caseLabels = caseBlock.expr();
            for (int c = 0; c < caseLabels.Length; c++)
            {
                ValueBase caseVal = Evaluator.EvalConstant(caseLabels[c]);
                if (!caseVal.IsDefined)
                {
                    throw new Error(caseLabels[c].Start, "Case label must be a constant expression");
                }
                if (test.ValueType != caseVal.ValueType)
                {
                    throw new Error(caseLabels[c].Start, "Case label type mismatch");
                }
                if (test.Equals(caseVal))
                {
                    if (selected > -1)
                    {
                        throw new Error(caseLabels[c].Start, "Case label already defined");
                    }
                    selected = i;
                }
            }
        }
        if (selected < 0)
        {
            selected = defaultCase;
        }
        if (selected > -1)
        {
            for (int i = selected; i < caseBlocks.Length; i++)
            {
                SyntaxParser.CaseBlockContext selectedBlock = caseBlocks[i];
                try { Visit(selectedBlock.block()); }
                catch (Break) { break; }
            }
        }
        return 0;
    }

    public override int VisitInstructionRepeat([NotNull] SyntaxParser.InstructionRepeatContext context)
    {
        PushAnonymousScope(context.Start.TokenIndex);
        try
        {
            int repeat = Services.Evaluator.SafeEvalNumber(context.expr(), 0, int.MaxValue, 0);
            if (context.block() == null)
            {
                return 0;
            }
            while (repeat-- > 0)
            {
                try
                {
                    _ = Visit(context.block());
                }
                catch (Continue) { continue; }
                catch (Break) { break; }
            }
            if (context.label() != null)
            {
                return Visit(context.label());
            }
            return 0;
        }
        finally
        {
            Services.State.Symbols.PopScope();
        }
    }

    public override int VisitInstructionNamespace([NotNull] SyntaxParser.InstructionNamespaceContext context)
    {
        if (Services.State.Symbols.InFunctionScope)
        {
            throw new Error(context, "Cannot define named scopes in function blocks");
        }
        if (Services.State.Symbols.LookupToScope(context.root.Text) is not Namespace rootNs)
        {
            rootNs = new(context.root, Services.State.Symbols.ActiveScope)
            {
                IsProcScope = Services.State.Symbols.ActiveScope is ScopedSymbol s && s.IsProcScope
            };
            Services.State.Symbols.Define(rootNs);
        }
        Services.State.Symbols.PushScope(rootNs);
        int scopeDepth = 1;

        for (int i = 0; i < context.identifierPart().Length; i++)
        {
            string nsPartName = context.identifierPart()[i].NamePart();

            if (rootNs.ResolveMember(nsPartName) is not Namespace nsPart)
            {
                nsPart = new(context.identifierPart()[i].Start.Text.TrimStartOnce('.'), rootNs)
                {
                    IsProcScope = rootNs.IsProcScope
                };
                Services.State.Symbols.Define(nsPart);
            }
            Services.State.Symbols.PushScope(nsPart);
            scopeDepth++;
        }
        try
        {
            return VisitChildren(context);
        }
        finally
        {
            for (int i = 0; i < scopeDepth; i++)
            {
                Services.State.Symbols.PopScope();
            }
        }
    }

    public override int VisitInstructionWhileLoop([NotNull] SyntaxParser.InstructionWhileLoopContext context)
    {
        ValueBase cond = context.Do() != null
           ? new BoolValue(true)
           : Services.Evaluator.Eval(context.@while);
        PushAnonymousScope(context.Start.TokenIndex);
        try
        {
            while (cond.IsDefined &&
                   cond.AsBool() &&
                   Services.State.Errors.Count == 0)
            {
                if (context.block() != null)
                {
                    try { Visit(context.block()); }
                    catch (Break) { break; }
                    catch (Continue) { }
                }
                cond = Services.Evaluator.Eval(context.@while);
                if (cond.IsDefined && cond.AsBool() && context.block() == null)
                {
                    throw new Error(context.expr(), "True condition with an empty block results in an infinite loop");
                }
            }
            if (context.label() != null)
            {
                return Visit(context.label());
            }
            return 0;
        }
        finally
        {
            Services.State.Symbols.PopScope();
        }
    }

    public override int VisitInstructionForeach([NotNull] SyntaxParser.InstructionForeachContext context)
    {
        ValueBase collection = Services.Evaluator.Eval(context.collection);
        if (!collection.IsCollection)
        {
            if (!collection.IsDefined)
            {
                return 0;
            }
            throw new Error(context.collection, "Expression is not an enumerable collection");
        }
        try
        {
            PushAnonymousScope(context.Start.TokenIndex);
            if (collection is DictionaryValue dict)
            {
                ScopedSymbol dictIterator = new(context.Identifier().Symbol, Services.State.Symbols.ActiveScope);
                Services.State.Symbols.Define(dictIterator);

                if (context.block() == null)
                {
                    return 0;
                }
                Constant key = new("key", dictIterator);
                Constant value = new("value", dictIterator);
                dictIterator.Define("key", key);
                dictIterator.Define("value", value);
                foreach (KeyValuePair<ValueBase, ValueBase> kvp in dict)
                {
                    key.Value = kvp.Key.IsObject ? kvp.Key : kvp.Key.AsCopy();
                    value.Value = kvp.Value.IsObject ? kvp.Value : kvp.Value.AsCopy();
                    try { Visit(context.block()); }
                    catch (Break) { break; }
                    catch (Continue) { }
                }
                Services.State.Symbols.ActiveScope.Remove(dictIterator.Name);
                return 0;
            }
            Variable iterator = new(context.Identifier().Symbol, Services.State.Symbols.ActiveScope);
            Services.State.Symbols.Define(iterator);
            if (context.block() == null)
            {
                return 0;
            }
            for (int i = 0; i < collection.Count; i++)
            {
                iterator.Value = collection[i].IsObject ? collection[i] : collection[i].AsCopy();
                try { Visit(context.block()); }
                catch (Break) { break; }
                catch (Continue) { }
            }
            Services.State.Symbols.ActiveScope.Remove(iterator.Name);
            if (context.label() != null)
            {
                return Visit(context.label());
            }
            return 0;
        }
        finally
        {
            Services.State.Symbols.PopScope();
        }
    }

    public override int VisitInstructionFor([NotNull] SyntaxParser.InstructionForContext context)
    {
        PushAnonymousScope(context.Start.TokenIndex);
        if (context.init != null)
        {
            if (!Evaluator.ExpressionIsAssignmentType(context.init))
            {
                throw new Error(context.init, "Assignment-type expression expected");
            }
            Services.Evaluator.SetVariable(context.init);
        }
        ValueBase condition = context.cond != null
            ? Services.Evaluator.Eval(context.cond)
            : new BoolValue(true);
        try
        {
            while (condition.IsDefined && condition.AsBool() && Services.State.Errors.Count == 0)
            {
                if (context.block() != null)
                {
                    try { Visit(context.block()); }
                    catch (Break) { break; }
                    catch (Continue) { }
                }
                SyntaxParser.ExprContext[] incs = context.inc.expr();
                for (int i = 0; i < incs.Length; i++)
                {
                    if (!Services.Evaluator.SetVariable(incs[i]).IsDefined)
                    {
                        return 0;
                    }
                }
                if (context.cond != null)
                {
                    condition = Services.Evaluator.Eval(context.cond);
                }
                else if (context.block() == null)
                {
                    throw new Error(context.Start, "For loop with no condition and an empty block results in an infinite loop");
                }
            }
            if (context.label() != null)
            {
                return Visit(context.label());
            }
            return 0;
        }
        finally
        {
            Services.State.Symbols.PopScope();
        }
    }

    public override int VisitInstructionPage([NotNull] SyntaxParser.InstructionPageContext context)
    {
        int currentPage = Services.State.LogicalPCOnAssemble & 0xFF00;
        if (context.block() != null)
        {
            _ = Visit(context.block());
            if ((Services.State.Output.LogicalPC & 0xFF00) != currentPage && !Services.State.PassNeeded)
            {
                throw new Error(context.block().stat()[^1], "Code crosses page boundary");
            }
        }
        return 0;
    }
}

