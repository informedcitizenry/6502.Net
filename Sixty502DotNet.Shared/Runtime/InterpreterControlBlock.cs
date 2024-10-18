//-----------------------------------------------------------------------------
// Copyright (c) 2017-2024 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using Antlr4.Runtime;
using Antlr4.Runtime.Misc;

namespace Sixty502DotNet.Shared;

public sealed partial class Interpreter : SyntaxParserBaseVisitor<int>
{

    private bool IfCondition(int ifType, SyntaxParser.ExprContext condExpression)
    {
        if (ifType.IsOneOf(SyntaxParser.Ifdef, 
                           SyntaxParser.Ifndef, 
                           SyntaxParser.Elseifdef, 
                           SyntaxParser.Elseifndef))
        {
            if (!Services.State.InFirstPass &&
                _memoizedDefines.TryGetValue(condExpression.Start.TokenIndex, out bool defined))
            {
                return defined;
            }
            if (ifType == SyntaxParser.Ifdef || ifType == SyntaxParser.Elseifdef)
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
        if (ifType.IsOneOf(SyntaxParser.Ifconst,
                           SyntaxParser.Ifnconst,
                           SyntaxParser.Elseifconst,
                           SyntaxParser.Elseifnconst))
        {
            ValueBase constVal = Evaluator.EvalConstant(condExpression);
            if (ifType == SyntaxParser.Ifconst || ifType == SyntaxParser.Elseifconst)
            {
                return constVal.IsDefined;
            }
            return !constVal.IsDefined;     
        }
        if (ifType == SyntaxParser.If || ifType == SyntaxParser.Elseif)
        {
            ValueBase condVal = Services.Evaluator.Eval(condExpression);
            return condVal.IsDefined && condVal.AsBool();
        }
        return true;
    }

    public override int VisitInstructionIf([NotNull] SyntaxParser.InstructionIfContext context)
    {
        SyntaxParser.IfBlockContext ifBlock = context.ifBlock();
        if (IfCondition(ifBlock.Start.Type, ifBlock.expr()))
        {
            if (ifBlock.ifbl != null)
            {
                return Visit(ifBlock.ifbl);
            }
            return 0;
        }
        int selected = -1;
        var elseIfBlocks = ifBlock.elseIfBlock();
        for (int i = 0; i < elseIfBlocks.Length; i++)
        {
            var elseIfBlock = elseIfBlocks[i];
            if (IfCondition(elseIfBlock.Start.Type, elseIfBlock.expr()))
            {
                selected = i;
                break;
            }
        }
        if (selected >= 0)
        {
            if (elseIfBlocks[selected].block() != null)
            {
                return Visit(ifBlock.elseIfBlock()[selected].block());
            }
            return 0;
        }
        if (ifBlock.elsebl != null)
        {
            return Visit(ifBlock.elsebl);
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
        HashSet<ValueBase> caseLabelValues = new();
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
                    throw new Error(caseLabels[c], "Case label must be a constant expression");
                }
                if (test.ValueType != caseVal.ValueType)
                {
                    throw new Error(caseLabels[c], "Case label type mismatch");
                }
                if (!caseLabelValues.Add(caseVal))
                {
                    throw new Error(caseLabels[c], "Case label already defined");
                }
                if (test.Equals(caseVal))
                {
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
            var fellthrough = selected < caseBlocks.Length - 1;
            SyntaxParser.BlockContext? fallthroughBlock = null;
            for (int i = selected; i < caseBlocks.Length; i++)
            {
                SyntaxParser.CaseBlockContext selectedBlock = caseBlocks[i];
                if (i < caseBlocks.Length - 1)
                {
                    fallthroughBlock = selectedBlock.block();
                }
                try 
                { 
                    Visit(selectedBlock.block()); 
                }
                catch (Break) 
                {
                    fellthrough = false;
                    break; 
                }
            }
            if (fellthrough)
            {
                var selectedBlock = fallthroughBlock!.children;
                for (int i = selectedBlock.Count - 1; i >= 0; i--)
                {
                    if (selectedBlock[i] is ParserRuleContext stat)
                    {
                        Services.State.Warnings.Add(new Warning(stat, "Switch case falls through here"));
                        return 0;
                    }
                }
                Services.State.Warnings.Add(new Warning(fallthroughBlock!, "Switch case falls through here"));
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
        if (Services.State.Symbols.LookupToScope(context.root.Start.Text) is not Namespace rootNs)
        {
            rootNs = new(context.root.Start, Services.State.Symbols.ActiveScope)
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
                ScopedSymbol dictIterator = new(context.iterator.Start, Services.State.Symbols.ActiveScope);
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
            Variable iterator = new(context.iterator.Start, Services.State.Symbols.ActiveScope);
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
                    throw new Error(context, "For loop with no condition and an empty block results in an infinite loop");
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
        uint currentPage = (uint)Services.State.LogicalPCOnAssemble & 0xFFFFFF00;
        bool usingLogical = Services.State.LogicalPCOnAssemble != Services.State.Output.ProgramCounter;
        if (context.block() != null)
        {
            _ = Visit(context.block());
            if (((uint)Services.State.Output.LogicalPC & 0xFFFFFF00) != currentPage && !Services.State.PassNeeded)
            {
                if (Services.State.CurrentPass > 4)
                {
                    if (usingLogical && Services.State.LogicalPCOnAssemble == Services.State.Output.ProgramCounter)
                    {
                        throw new Error(context, "Cannot determine a page boundary crossing if `.relocate` and `.endrelocate` are used in a `.page` block");
                    }
                    throw new Error(context.block().stat()[^1], "Code crosses page boundary");
                }
                Services.State.PassNeeded = true;
            }
        }
        return 0;
    }
}

