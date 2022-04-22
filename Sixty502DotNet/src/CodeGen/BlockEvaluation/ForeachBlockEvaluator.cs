//-----------------------------------------------------------------------------
// Copyright (c) 2017-2022 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

namespace Sixty502DotNet
{
    /// <summary>
    /// A class that iterates through a collection or string and activates its
    /// block for assembly at each iteration.
    /// </summary>
    public class ForeachBlockEvaluator : BlockEvaluatorBase
    {
        /// <summary>
        /// Construct a new instance of the <see cref="ForeachBlockEvaluator"/>
        /// class.
        /// </summary>
        /// <param name="visitor">The block visitor to visit the block's
        /// parsed statement nodes.</param>
        /// <param name="services">The shared <see cref="AssemblyServices"/>
        /// object.</param>
        public ForeachBlockEvaluator(BlockVisitor visitor, AssemblyServices services)
            : base(visitor, services)
        { }

        public override BlockState Evaluate(Sixty502DotNetParser.BlockStatContext context)
        {
            var state = new BlockState();
            var iterator = context.enterBlock().iter;
            if (iterator.refExpr()?.identifier()?.Ident() == null)
            {
                Services.Log.LogEntry(iterator, "Not a valid foreach expression.");
                return state;
            }
            var iterVarName = iterator.refExpr().GetText();
            if (!Services.Symbols.SymbolAssignmentIsLegal(iterVarName, true))
            {
                Services.Log.LogEntry(iterator.refExpr(),
                    string.Format(Errors.SymbolExistsError, iterator.refExpr().GetText()));
                return state;
            }
            ArrayValue? sequence;
            var val = Services.ExpressionVisitor.Visit(context.enterBlock().seq);
            var valString = val.ToString(true);
            sequence = val as ArrayValue;
            if (sequence == null)
            {
                if (!val.IsString)
                {
                    Services.Log.LogEntry(context.enterBlock().seq, "Not a valid foreach expression.");
                    return state;
                }
            }
            else if (!sequence.ElementsSameType)
            {
                if (!sequence.ContainsUndefinedElement)
                {
                    Services.Log.LogEntry(context.enterBlock().seq, "Not a valid array or dictionary.");
                }
                return state;
            }
            if (sequence is DictionaryValue dict)
            {
                return IterateDictionary(context.block(), iterator.refExpr(), dict);
            }
            if (Services.Symbols.Scope.Resolve(iterVarName) is not Variable iterVar)
            {
                iterVar = new Variable(iterVarName);
                Services.Symbols.Scope.Define(iterVarName, iterVar);
                Services.Symbols.DeclareVariable(iterVar);
            }
            else if (!ReferenceEquals(iterVar.Scope, Services.Symbols.Scope))
            {
                // Do not re-use a variable declared at a higher scope
                Services.Log.LogEntry(iterator.refExpr(),
                    string.Format(Errors.SymbolExistsError, iterator.refExpr().GetText()));
                return state;
            }
            for (var i = 0; i < val.ElementCount && state.IsContinuing() && !Services.Log.HasErrors; i++)
            {
                if (val.IsString)
                {
                    iterVar.Value.SetAs(new Value($"'{valString[i]}'"));
                }
                else if (!iterVar.Value.IsDefined)
                {
                    iterVar.Value = sequence![i];
                }
                else
                {
                    iterVar.Value.SetAs(sequence![i]);
                }
                state = Visitor.Visit(context.block());
            }
            return state;
        }

        private BlockState IterateDictionary(Sixty502DotNetParser.BlockContext block,
            Sixty502DotNetParser.RefExprContext refExpr, DictionaryValue dict)
        {
            if (!dict.IsValid)
            {
                return BlockState.Evaluating();
            }
            var iterVarName = refExpr.GetText();
            var currentScope = block.scope ?? Services.Symbols.Scope;

            if (currentScope.Resolve(iterVarName) != null)
            {
                Services.Log.LogEntry(refExpr, string.Format(Errors.SymbolExistsError, iterVarName));
                return BlockState.Evaluating();
            }
            var iterVarScope = new Namespace(iterVarName, currentScope);
            currentScope.Define(iterVarName, iterVarScope);

            var key = new Constant("key", Value.Undefined());
            var value = new Constant("value", Value.Undefined());
            iterVarScope.Define("key", key);
            iterVarScope.Define("value", value);
            try
            {
                var state = BlockState.Evaluating();
                var entries = dict.Entries;
                foreach (var kvp in entries)
                {
                    key.Value.SetAs(kvp.Key);
                    value.Value.SetAs(kvp.Value);
                    state = Visitor.Visit(block);
                    if (state.status != Status.Evaluating || Services.Log.HasErrors)
                    {
                        break;
                    }
                }
                return state;
            }
            finally
            {
                currentScope.Remove(iterVarName);
            }
        }
    }
}
