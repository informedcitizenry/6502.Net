//-----------------------------------------------------------------------------
// Copyright (c) 2017-2022 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

namespace Sixty502DotNet
{
    /// <summary>
    /// Looks for the first matching value in <c>.case</c> statements for the
    /// expression in the <c>.switch</c> statement, or for a <c>.default</c>
    /// case, and if found, activates the block for that case.
    /// </summary>
    public class SwitchBlockSelector : BlockSelectorBase
    {
        /// <summary>
        /// Construct a new instance of the <see cref="SwitchBlockSelector"/>
        /// class.
        /// </summary>
        /// <param name="services">The shared <see cref="AssemblyServices"/>
        /// object.</param>
        public SwitchBlockSelector(AssemblyServices services)
            : base(services)
        { }

        public override int Select(Sixty502DotNetParser.BlockStatContext context)
        {
            var switchStatement = context.enterSwitch();
            var compVal = Services.ExpressionVisitor.Visit(switchStatement.expr());
            if (!compVal.IsDefined || !compVal.IsPrimitiveType)
            {
                if (compVal.IsDefined)
                {
                    Services.Log.LogEntry(switchStatement.expr(), "Switch expression is not valid type.");
                }
                return -1;
            }
            var caseBlocks = context.switchBlock().caseBlock();
            int defaultIndex = -1;
            int matchIndex = -1;
            for (var i = 0; i < caseBlocks.Length; i++)
            {
                var caseStatements = caseBlocks[i].enterCase();
                for (var j = 0; j < caseStatements.Length; j++)
                {
                    var caseExpression = caseStatements[j].expr();
                    if (caseExpression != null)
                    {
                        if (!Services.ExpressionVisitor.TryGetPrimaryExpression(caseExpression, out var caseVal))
                        {
                            return -1;
                        }
                        if (caseVal.DotNetType != compVal.DotNetType)
                        {
                            if (caseVal.IsDefined)
                            {
                                Services.Log.LogEntry(caseExpression, "Case type mismatch with switch expression.");
                            }
                            else
                            {
                                Services.Log.LogEntry(caseExpression, "Case value must be a constant expression.");
                            }
                            return -1;
                        }
                        if (caseVal.Equals(compVal))
                        {
                            if (matchIndex > -1)
                            {
                                Services.Log.LogEntry(caseExpression, "Previous case match found.");
                                return -1;
                            }
                            matchIndex = i;
                        }
                    }
                    else if (defaultIndex > -1)
                    {
                        Services.Log.LogEntry(caseStatements[j], "Default case already defined.");
                        return -1;
                    }
                    else
                    {
                        defaultIndex = i;
                    }
                }
            }
            if (matchIndex > -1 || defaultIndex > -1)
            {
                if (matchIndex < 0)
                {
                    matchIndex = defaultIndex;
                }
                return matchIndex;
            }
            return -1;
        }
    }
}
