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
    public override int VisitExpressionAssignment([NotNull] SyntaxParser.ExpressionAssignmentContext context)
    {
        bool isConstant = context.assignOp().Start.Type == SyntaxParser.Equal;
        ValueBase val = Services.Evaluator.Eval(context, isConstant);
        if (AddListing() && (context.Start.Type == SyntaxParser.Asterisk || isConstant) && val.ValueType != ValueType.Callable)
        {
            SyntaxParser.StatContext stat = (SyntaxParser.StatContext)context.Parent;
            if (context.Start.Type == SyntaxParser.Asterisk)
            {
                if (!_options.OutputOptions.NoSource)
                {
                    GenListing(stat.Start, $".{val.AsInt(),-55:x4}{stat.GetSourceLine(false).Elliptical(66)}");
                }
                else
                {
                    GenListing(stat.Start, $".{val.AsInt():x4}");
                }
                return 0;
            }
            if (context.Start.Text.Equals("_"))
            {
                return 0;
            }
            GenListing(stat, val);
        }
        return 0;
    }

    public override int VisitExpressionIncDec([NotNull] SyntaxParser.ExpressionIncDecContext context)
    {
        _ = Services.Evaluator.Eval(context, false);
        return 0;
    }

}
