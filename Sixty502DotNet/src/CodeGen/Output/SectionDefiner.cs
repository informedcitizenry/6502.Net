//-----------------------------------------------------------------------------
// Copyright (c) 2017-2022 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using Antlr4.Runtime;

namespace Sixty502DotNet
{
    /// <summary>
    /// A utility class to evaluate a parsed section definition and create a
    /// section.
    /// </summary>
    public static class SectionDefiner
    {
        /// <summary>
        /// Define a section from the parsed expression list.
        /// </summary>
        /// <param name="parent">The parent parse tree context.</param>
        /// <param name="expressions">The parsed expression list.</param>
        /// <param name="services">The shared <see cref="AssemblyServices"/>
        /// object.</param>
        /// <returns><c>true</c> if the section was able to be defined without
        /// errors, <c>false</c> otherwise.</returns>
        public static bool Define(ParserRuleContext parent, Sixty502DotNetParser.ExprContext[] expressions,
            AssemblyServices services)
        {
            if (expressions.Length < 2)
            {
                services.Log.LogEntry(parent, Errors.ExpectedExpression);
                return false;
            }
            if (expressions.Length > 3)
            {
                services.Log.LogEntry(expressions[3], Errors.UnexpectedExpression);
                return false;
            }
            if (!services.ExpressionVisitor.TryGetPrimaryExpression(expressions[0], out var name))
            {
                return false;
            }
            if (name.IsDefined && name.IsString)
            {
                if (!services.ExpressionVisitor.TryGetPrimaryExpression(expressions[1], out var start))
                {
                    return false;
                }
                try
                {
                    if (start.IsDefined && start.IsIntegral)
                    {
                        if (start.ToInt() >= 0 && start.ToInt() < UInt24.MaxValue)
                        {
                            if (expressions.Length > 2)
                            {
                                if (!services.ExpressionVisitor.TryGetPrimaryExpression(expressions[2], out var end))
                                {
                                    return false;
                                }
                                if (end.IsDefined && end.IsIntegral)
                                {
                                    if (end.ToInt() > 0 && end.ToInt() <= UInt24.MaxValue)
                                    {
                                        services.Output.DefineSection(name.ToString(true), start.ToInt(), end.ToInt());
                                        return true;
                                    }
                                    services.Log.LogEntry(expressions[2], Errors.IllegalQuantity);
                                    return false;
                                }
                                services.Log.LogEntry(expressions[2], "Integer value expected.");
                                return false;
                            }
                            services.Output.DefineSection(name.ToString(true), start.ToInt(), CodeOutput.MaxAddress + 1);
                            return true;
                        }
                        services.Log.LogEntry(expressions[1], Errors.IllegalQuantity);
                        return false;
                    }
                }
                catch (SectionException ex)
                {
                    services.Log.LogEntry(parent, ex.Message);
                    return false;
                }
                services.Log.LogEntry(expressions[1], "Integer value expected.");
                return false;
            }
            services.Log.LogEntry(expressions[0], Errors.StringExpected);
            return false;
        }
    }
}
