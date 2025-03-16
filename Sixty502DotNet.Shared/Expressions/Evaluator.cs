//-----------------------------------------------------------------------------
// Copyright (c) 2017-2024 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using Microsoft.Build.Framework;
using System.Text;
using System.Text.RegularExpressions;

namespace Sixty502DotNet.Shared;

/// <summary>
/// The class responsible for evaluating parsed expressions, including
/// symbol declarations and definitions.
/// </summary>
public sealed class Evaluator : SyntaxParserBaseVisitor<ValueBase>
{
    private readonly SymbolResolver? _symbolResolver;

    private Evaluator()
    {
        Services = null;
        CachedEvaluations = new();
        _symbolResolver = null;
    }

    /// <summary>
    /// Constructs an instance of an evaluator.
    /// </summary>
    /// <param name="services">The shared <see cref="AssemblyServices"/> for
    /// the assembly process, which the evaluator depends on for symbol
    /// resolution and text encoding.</param>
    public Evaluator(AssemblyServices services)
    {
        Services = services;
        CachedEvaluations = new();
        _symbolResolver = new(Services);
    }

    /// <summary>
    /// Evaluate a parsed expression to a <see cref="ValueBase"/>, which represents
    /// a array, boolean, character, dictionary, numeric, string, or tuple value,
    /// or a function object.
    /// </summary>
    /// <param name="expr">The parsed expression</param>
    /// <param name="allowConstantAssignment">If the expression contains an
    /// assignment, treat it as a constant.</param>
    /// <returns>The value result of the expression evaluation.</returns>
    /// <exception cref="Error">A runtime error encountered during evaluation.</exception>
    public ValueBase Eval(SyntaxParser.ExprContext expr, bool allowConstantAssignment = false)
    {
        if (expr.IsConstant())
        {
            return expr.value;
        }
        if (!allowConstantAssignment &&
            expr is SyntaxParser.ExpressionAssignmentContext assign &&
            assign.assignOp().Start.Type == SyntaxParser.Equal)
        {
            throw new Error(expr.Start, "Constant assignment is invalid");
        }
        ValueBase val = Visit(expr);
        if (!val.IsDefined && Services?.State.InFirstPass == false)
        {
            if (Services.State.CurrentPass > 3)
            {
                throw new Error(expr, "Cannot resolve expression after multiple passes");
            }
            Services.State.PassNeeded = true;
        }
        val.Expression = expr;
        return val;
    }

    public override ValueBase VisitExprList([NotNull] SyntaxParser.ExprListContext context)
    {
        ArrayValue exprs = new();
        for (int i = 0; i < context.expr().Length; i++)
        {
            exprs.Add(Eval(context.expr()[i]));
        }
        return exprs;
    }

    public override ValueBase VisitExpressionCollection([NotNull] SyntaxParser.ExpressionCollectionContext context)
    {
        ArrayValue array = new()
        {
            Expression = context,
            IsTuple = context.tuple() != null
        };
        SyntaxParser.ExprContext[] elements = array.IsTuple
            ? context.tuple().expr()
            : context.array().exprList().expr();
        bool isConstant = true;
        for (int i = 0; i < elements.Length; i++)
        {
            ValueBase element = Eval(elements[i]);
            if (!element.IsDefined)
            {
                return element;
            }
            array.Add(element);
            if (!array.IsTuple && !array.ElementsSameType)
            {
                throw new TypeMismatchError(elements[i]);
            }
            isConstant &= elements[i].IsConstant();
        }
        if (isConstant)
        {
            context.value = array;
        }
        return array;
    }

    /// <summary>
    /// Evaluate a parsed constant expression.
    /// </summary>
    /// <param name="expression">The parsed expression to evaluate.</param>
    /// <returns>The value of the parsed constant expression, if it the
    /// expression is constant. If the expression is not constant, the value
    /// itself will be undefined.</returns>
    public static ValueBase EvalConstant(SyntaxParser.ExprContext expression)
    {
        if (expression.IsConstant())
        {
            return expression.value;
        }
        expression.value = new Evaluator().Eval(expression);
        return expression.value;
    }

    private static (Encoding, string) EncodingFromPrefix(IToken token)
    {
        if (token.Type == SyntaxParser.CbmScreenStringLiteral ||
            token.Type == SyntaxParser.PetsciiStringLiteral)
        {
            return (Encoding.UTF8, token.Text[1..]);
        }
        if (token.Type == SyntaxParser.StringLiteral ||
            (token.Type == SyntaxParser.UnicodeStringLiteral && token.Text[1] == '8'))
        {
            if (token.Type == SyntaxParser.StringLiteral)
            {
                return (Encoding.UTF8, token.Text);
            }
            return (Encoding.UTF8, token.Text[2..]);
        }
        return token.Text[0] == 'u' ?
            (Encoding.Unicode, token.Text[1..]) :
            (Encoding.UTF32, token.Text[1..]);
    }

    private static ValueBase GetConvertedString(string literalText, Encoding encoding, string? encodingName, ParserRuleContext context)
    {
        try
        {
            return StringConverter.ConvertString(literalText, encoding, encodingName);
        }
        catch (RegexParseException)
        {
            throw new Error(context, "Invalid escape sequence in string");
        }
    }

    private static int EvalIntegerLiteral(SyntaxParser.PrimaryExprContext context)
    {
        return context.Start.Type switch
        {
            SyntaxParser.BinLiteral => NumberConverter.ConvertLiteral(context.BinLiteral()),
            SyntaxParser.DecLiteral => NumberConverter.ConvertLiteral(context.DecLiteral()),
            SyntaxParser.HexLiteral => NumberConverter.ConvertLiteral(context.HexLiteral()),
            SyntaxParser.OctLiteral => NumberConverter.ConvertLiteral(context.OctLiteral()),
            _ => throw new Error(context, "Integer literal value expected")
        };
    }

    public static int EvalIntegerLiteral(SyntaxParser.PrimaryExprContext context, params int[] values)
    {
        int value = EvalIntegerLiteral(context);
        if (values.Contains(value))
        {
            return value;
        }
        throw new IllegalQuantityError(context);
    }

    public static int EvalNumberLiteralType(SyntaxParser.PrimaryExprContext context, string error, int start, int end)
    {
        if (context.Start.Type.IsOneOf(SyntaxParser.CharLiteral, SyntaxParser.StringLiteral, SyntaxParser.UnicodeStringLiteral))
        {
            if (context.Start.Type == SyntaxParser.CharLiteral)
            {
                try
                {
                    return StringConverter.ConvertChar(context.Start, Encoding.UTF8, null).AsInt();
                }
                catch
                {
                    throw new IllegalQuantityError(context);
                }
            }
            (Encoding enc, string textString) = EncodingFromPrefix(context.Start);
            double val = GetConvertedString(textString, enc, null, context).AsDouble();
            if (!double.IsFinite(val) || double.IsNaN(val) || val < start || val > end)
            {
                throw new Error(context, error);
            }
            return (int)val;
        }
        return EvalIntegerLiteral(context);
    }

    /// <summary>
    /// Evaluate a constant expression as an integer literal.
    /// </summary>
    /// <param name="context">The parsed expression.</param>
    /// <param name="error">The error message if the evaluation fails.</param>
    /// <param name="min">The minimum value of the expression.</param>
    /// <param name="max">The first value beyond the maximum value of the expression.</param>
    /// <returns>The evaluated expression as an integer.</returns>
    /// <exception cref="Error">A runtime error encountered during evaluation.</exception>
    public static int EvalIntegerLiteral(SyntaxParser.PrimaryExprContext context, string error, int min, int max)
    {
        int value = EvalIntegerLiteral(context);
        return value < min || value >= max
            ? throw new Error(context, error) : value;
    }

    /// <summary>
    /// Evaluate a <see cref="ITerminalNode"/> as an integeral literal.
    /// </summary>
    /// <param name="terminal">The terminal node.</param>
    /// <param name=""></param>
    /// <param name="error">The error message if the evaluation fails.</param>
    /// <param name="min">The minimum value of the expression.</param>
    /// <param name="max">The maximum value of the expression.</param>
    /// <returns>The evaluated expression as an integer.</returns>
    /// <exception cref="Error">A runtime error encountered during evaluation.</exception>
    public static int EvalIntegerLiteral([NotNull] ITerminalNode terminal, string error, int min, int max)
    {
        int value = NumberConverter.ConvertIntLiteral(terminal);
        return value < min || value >= max
            ? throw new Error(terminal.Symbol, error) : value;
    }

    /// <summary>
    /// Evaluate an expression as a string literal.
    /// </summary>
    /// <param name="context">The parsed expression.</param>
    /// <returns>The string result of the expression evaluation.</returns>
    /// <exception cref="Error"></exception>
    public static string EvalStringLiteral(SyntaxParser.ExprContext context)
    {
        if (context is SyntaxParser.ExpressionStringLiteralContext stringContext)
        {
            return new Evaluator().Eval(stringContext).AsString();
        }
        throw new Error(context, "String literal value expected");
    }

    /// <summary>
    /// Evaluate the expression as an address if possible. If any part of the
    /// evaluation fails due to unresolved symbols or value quantity falling outside
    /// of the given bounds, then only raise a runtime error if an assembly
    /// pass is not pending. If a pass is pending, then return a default value.
    /// </summary>
    /// <param name="expression">The parsed expression.</param>
    /// <param name="truncateToPage">Truncate the result to a given page.</param>
    /// <param name="pageValue">The current page to truncate the result if the
    /// result and the page match. For instance, if the page is $0f and the
    /// value (as a hexadecimal value) is $0ff3, then the returned value will
    /// be $f3.</param>
    /// <returns>The value as an integer.</returns>
    /// <exception cref="Error"></exception>
    public int SafeEvalAddress(SyntaxParser.ExprContext expression, bool truncateToPage = false, int pageValue = 0)
    {
        double minValue = Services?.ArchitectureOptions.LongAddressing == true ? Int24.MinValue : short.MinValue;
        double maxValue = Services?.ArchitectureOptions.LongAddressing == true ? UInt24.MaxValue : ushort.MaxValue;
        double address = SafeEvalNumber(expression, minValue, maxValue, truncateToPage, pageValue);
        if (((int)(address / 0x10000) & 0xff) == Services?.State.Output.CurrentBank)
        {
            return (int)address & 0xffff;
        }
        return (int)address;
    }

    /// <summary>
    /// Evaluate the expression as an address if possible. If any part of the
    /// evaluation fails due to unresolved symbols or value quantity falling outside
    /// of the given bounds, then only raise a runtime error if an assembly
    /// pass is not pending. If a pass is pending, then return a default value.
    /// </summary>
    /// <param name="expression">The parsed primary expression.</param>
    /// <param name="truncateToPage">Truncate the result to a given page.</param>
    /// <param name="pageValue">The current page to truncate the result if the
    /// result and the page match. For instance, if the page is $0f and the
    /// value (as a hexadecimal value) is $0ff3, then the returned value will
    /// be $f3.</param>
    /// <returns>The value as an integer.</returns>
    /// <exception cref="Error"></exception>
    public int SafeEvalAddress(SyntaxParser.PrimaryExprContext expression, bool truncateToPage = false, int pageValue = 0)
    {
        int minValue = Services?.ArchitectureOptions.LongAddressing == true ? Int24.MinValue : short.MinValue;
        int maxValue = Services?.ArchitectureOptions.LongAddressing == true ? UInt24.MaxValue : ushort.MaxValue;
        int address = EvalNumberLiteralType(expression, "Type mismatch", minValue, maxValue);
        if (((address / 0x10000) & 0xff) == Services?.State.Output.CurrentBank)
        {
            return address & 0xffff;
        }
        return address;
    }

    /// <summary>
    /// Evaluate the expression as a numeric value if possible. If any part of the
    /// evaluation fails due to unresolved symbols or value quantity falling outside
    /// of the given bounds, then only raise a runtime error if an assembly
    /// pass is not pending. If a pass is pending, then return a default value.
    /// </summary>
    /// <param name="expression">The parsed expression.</param>
    /// <param name="minValue">The minimum value of the expression result.</param>
    /// <param name="maxValue">The maximum value of the expression result.</param>
    /// <param name="defaultValue">The default value to return if evaluation fails and
    /// another pass is needed.</param>
    /// <param name="truncateToPage">Truncate the result to a given page.</param>
    /// <param name="pageValue">The current page to truncate the result if the
    /// result and the page match. For instance, if the page is $0f and the
    /// value (as a hexadecimal value) is $0ff3, then the returned value will
    /// be $f3.</param>
    /// <returns>The value as an integer.</returns>
    /// <exception cref="Error"></exception>
    public int SafeEvalNumber(SyntaxParser.ExprContext expression, double minValue, double maxValue, int defaultValue, bool truncateToPage = false, int pageValue = 0)
    {
        double originalMin = minValue, originalMax = maxValue;
        if (truncateToPage)
        {
            minValue = Int24.MinValue;
            maxValue = UInt24.MaxValue;
        }
        ValueBase val = EvalNumber(expression, minValue, maxValue);
        if (val.IsDefined)
        {
            int valInt = val.AsInt();
            if (truncateToPage && valInt / 256 == pageValue)
            {
                return valInt & 0xff;
            }
            if (valInt < originalMin || valInt > originalMax)
            {
                if (Services?.State.PassNeeded == true)
                {
                    return defaultValue;
                }
                throw new IllegalQuantityError(expression);
            }
            return valInt;
        }
        return defaultValue;
    }

    /// <summary>
    /// Evaluate the expression as a numeric value if possible. If any part of the
    /// evaluation fails due to unresolved symbols or value quantity falling outside
    /// of the given bounds, then only raise a runtime error if an assembly
    /// pass is not pending. If a pass is pending, then return a default value,
    /// which is the given maximum value.
    /// </summary>
    /// <param name="expression">The parsed expression.</param>
    /// <param name="minValue">The minimum value of the expression result.</param>
    /// <param name="maxValue">The maximum value of the expression result.</param>
    /// <param name="truncateToPage">Truncate the result to a given page.</param>
    /// <param name="pageValue">The current page to truncate the result if the
    /// result and the page match. For instance, if the page is $0f and the
    /// value (as a hexadecimal) is $0ff3, then the returned value will
    /// be $f3.</param>
    /// <returns>The value as an integer.</returns>
    /// <exception cref="Error"></exception>
    public int SafeEvalNumber(SyntaxParser.ExprContext expression, double minValue, double maxValue, bool truncateToPage = false, int pageValue = 0)
    {
        return SafeEvalNumber(expression, minValue, maxValue, (int)maxValue, truncateToPage, pageValue);
    }

    /// <summary>
    /// Evaluate the parsed expression as a numeric expression.
    /// </summary>
    /// <param name="expression">The parsed expression.</param>
    /// <param name="minValue">The minimum value of the expression result.</param>
    /// <param name="maxValue">The maximum value of the expression result.</param>
    /// <param name="errorMsg">The custom error message report if evaluation fails.</param>
    /// <returns>The evaluation result as a <see cref="ValueBase"/>.</returns>
    /// <exception cref="Error"></exception>
    public ValueBase EvalNumber(SyntaxParser.ExprContext expression, double minValue = int.MinValue, double maxValue = uint.MaxValue)
    {
        ValueBase value = CoerceToNumber(Eval(expression));
        if (value.IsDefined && (value.AsDouble() < minValue || value.AsDouble() > maxValue))
        {
            throw new IllegalQuantityError(expression);
        }
        return value;
    }

    private void SetupFrame(FunctionScope frame, SyntaxParser.ExpressionCallContext callSite, SyntaxParser.ArgListContext? argList, ArrayValue parameters)
    {
        int paramIndex = 0;
        SyntaxParser.IdentContext[] args;
        if (argList?.argList() != null)
        {
            args = argList.argList().ident();
        }
        else
        {
            args = argList?.ident() ?? Array.Empty<SyntaxParser.IdentContext>();
        }
        for (int i = 0; i < args.Length; i++)
        {
            if (paramIndex >= parameters.Count)
            {
                throw new Error(callSite, "Too few parameters for function");
            }
            ValueBase paramVal = parameters[paramIndex++];
            Variable paramVar = new(args[i].GetText(), paramVal, frame);
            frame.Define(paramVar.Name, paramVar);
        }
        SyntaxParser.IdentContext[]? defaultArgs = argList?.defaultArgList()?.ident();
        SyntaxParser.ExprContext[]? defaultVals = argList?.defaultArgList()?.expr();
        for (int i = 0; i < defaultArgs?.Length; i++)
        {
            ValueBase paramVal;
            if (paramIndex >= parameters.Count)
            {
                paramVal = Eval(defaultVals![i]);
                paramIndex++;
            }
            else
            {
                paramVal = parameters[paramIndex++];
            }
            Variable paramVar = new(defaultArgs[i].GetText(), paramVal, frame);
            frame.Define(paramVar.Name, paramVar);
        }
    }

    /// <summary>
    /// Invoke the parsed expression as a function call, if it is a function
    /// call. If not, the action will fail and a runtime error will result.
    /// </summary>
    /// <param name="context">The parsed expression.</param>
    /// <returns>The (possibly null) <see cref="ValueBase"/> that resulted
    /// from the invocation. If the value returned is null, then the
    /// callee did not return a value.</returns>
    /// <exception cref="Error"></exception>
    public ValueBase? Invoke(SyntaxParser.ExprContext context)
    {
        if (context is SyntaxParser.ExpressionCallContext call)
        {
            if (Eval(call.expr()) is not FunctionObject obj)
            {
                throw new Error(call.expr(), "Expression is not callable");
            }
            ArrayValue p = call.exprList() != null
                    ? (ArrayValue)Visit(call.exprList())
                    : new ArrayValue();

            return Invoke(call, obj, p);
        }
        throw new Error(context, "Expression is not callable");
    }

    /// <summary>
    /// Invoke the parsed expression as a function call, if it is a function
    /// call. If not, the action will fail and a runtime error will result.
    /// </summary>
    /// <param name="callSite">The call site, included the function member
    /// and argument list.</param>
    /// <param name="function">The function value to perform the invocation
    /// upon.</param>
    /// <param name="parameters">The evaluated parameters from the call site
    /// as an <see cref="ArrayValue"/>.</param>
    /// <returns>The (possibly null) <see cref="ValueBase"/> that resulted
    /// from the invocation. If the value returned is null, then the
    /// callee did not return a value.</returns>
    /// <exception cref="Error"></exception>
    public ValueBase? Invoke(SyntaxParser.ExpressionCallContext callSite, FunctionObject function, ArrayValue parameters)
    {
        if (function is BuiltInFunctionObject builtInFunc)
        {
            return builtInFunc.Invoke(callSite, parameters);
        }
        // do user func call
        UserFunctionObject userFunc = (UserFunctionObject)function;

        if (userFunc.Statements == null && userFunc.SingleExpression == null)
        {
            return null;
        }
        if (Services == null || _symbolResolver == null)
        {
            return new UndefinedValue();
        }
        try
        {
            FunctionScope frame = new(function.Name,
                userFunc.Closure ?? Services.State.Symbols.GlobalScope);
            Services.State.Symbols.CallStack.Push(frame);
            if (callSite.exprList()?.expr().Length > userFunc.Arity)
            {
                throw new Error(callSite.exprList().expr()[userFunc.Arity],
                    "Too many parameters for function");
            }
            SetupFrame(frame, callSite, userFunc.Arguments, parameters);

            if (userFunc.SingleExpression != null)
            {
                return Eval(userFunc.SingleExpression);
            }
            _ = Services.Interpreter?.Visit(userFunc.Statements);
        }
        catch (Return ret)
        {
            if (ret.ReturnValue != null)
            {
                return ret.ReturnValue;
            }
            return null;
        }
        finally
        {
            Services.State.Symbols.CallStack.Pop();
        }
        return null;
    }

    private static ValueBase CoerceToNumber(ValueBase value)
    {
        if (value.ValueType == ValueType.Char || value.ValueType == ValueType.String)
        {
            return new NumericValue(value.AsDouble());
        }
        return value;
    }

    public override ValueBase VisitInterpolString([NotNull] SyntaxParser.InterpolStringContext context)
    {
        StringBuilder sb = new();
        List<ParserRuleContext> strPartList = new(context.interpolText());
        strPartList.AddRange(context.mInterpolText());
        for (int i = 0; i < strPartList.Count; i++)
        {
            SyntaxParser.ExprContext? expr = null;
            SyntaxParser.FormatSpecifierContext? format = null;
            if (strPartList[i] is SyntaxParser.InterpolTextContext iText)
            {
                expr = iText.interpolExpr()?.expr();
                format = iText.interpolExpr()?.formatSpecifier();
            }
            if (strPartList[i] is SyntaxParser.MInterpolTextContext mText)
            {
                expr = mText.interpolExpr()?.expr();
                format = mText.interpolExpr()?.formatSpecifier();
            }
            if (expr != null)
            {
                ValueBase val = Eval(expr);
                if (!val.IsDefined) return val;
                if (format != null)
                {
                    try
                    {
                        string f = $"{{0{format.GetText()}}}";
                        _ = sb.AppendFormat(f, val.Data());
                    }
                    catch
                    {
                        throw new Error(format, "Invalid format specified");
                    }
                }
                else if (val.ValueType == ValueType.String || val.ValueType == ValueType.Char)
                {
                    _ = sb.Append(val.AsString());
                }
                else if (val.IsNumeric)
                {
                    _ = sb.Append(val.AsDouble());
                }
                else
                {
                    _ = sb.Append(val.ToString());
                }
            }
            else
            {
                _ = sb.Append(strPartList[i].GetText().Replace("{{", "{").Replace("}}", "}"));
            }
        }
        return GetConvertedString(sb.ToString(), 
                                  Services?.Encoding ?? Encoding.UTF8, 
                                  Services?.Encoding?.EncodingName, 
                                  context);
    }

    /// <summary>
    /// Perform the variable setting represeted by the parsed expression.
    /// </summary>
    /// <param name="context">The expression context.</param>
    /// <returns>The result of the assignment, if successful.</returns>
    /// <exception cref="Error"></exception>
    public ValueBase SetVariable(SyntaxParser.ExprContext context)
    {
        return context switch
        {
            SyntaxParser.ExpressionAssignmentContext assign => VisitAssignment(assign, false),
            SyntaxParser.ExpressionIncDecContext incDec => VisitExpressionIncDec(incDec),
            _ => throw new Error(context, "Assignment or increment expression expected")
        };
    }

    private ValueBase Additive(SyntaxParser.ExprContext context, ValueBase lhs, IToken op, ValueBase rhs)
    {
        if ((rhs.IsNumeric && !lhs.IsNumeric) ||
            ((lhs.ValueType == ValueType.Char || lhs.ValueType == ValueType.String) && rhs.IsNumeric))
        {
            // set up type coersion for mixed string/char and numeric expressions
            if (op.Type == SyntaxParser.Plus && (lhs.ValueType == ValueType.String || rhs.ValueType == ValueType.String))
            {
                if (lhs.ValueType == ValueType.Char)
                {
                    lhs = new StringValue($"\"{lhs.AsString()}\"",
                                          Services?.Encoding ?? Encoding.UTF8,
                                          Services?.Encoding?.EncodingName);
                }
                if (rhs.IsNumeric && lhs.ValueType == ValueType.String) rhs = 
                new StringValue($"\"{rhs}\"", 
                                Services?.Encoding ?? Encoding.UTF8,
                                Services?.Encoding?.EncodingName);
            }
            else
            {
                if (!lhs.IsNumeric) lhs = CoerceToNumber(lhs);
                if (!rhs.IsNumeric) rhs = CoerceToNumber(rhs);
            }
        }
        try
        {
            return op.Type.IsOneOf(SyntaxParser.Plus, SyntaxParser.PlusEqual)
                ? lhs.AddWith(rhs) : lhs.Subtract(rhs);
        }
        catch (InvalidOperationError invalidErr)
        {
            throw new Error(op, invalidErr.Message);
        }
        catch (TypeMismatchError typeErr)
        {
            throw new Error(context, typeErr.Message);
        }
    }

    private ValueBase AssignOperation(SyntaxParser.ExprContext context, IToken lhsToken, ValueBase lhs, IToken op, ValueBase rhs)
    {
        if (op.Type.IsOneOf(SyntaxParser.PlusEqual, SyntaxParser.HyphenEqual))
        {
            return Additive(context, lhs, op, rhs);
        }
        if (lhs.IsNumeric && !rhs.IsNumeric) rhs = CoerceToNumber(rhs);
        else if (rhs.IsNumeric && !lhs.IsNumeric) lhs = CoerceToNumber(lhs);
        if (op.Type == SyntaxParser.SolidusEqual && rhs.AsDouble() == 0)
        {
            throw new Error(context, "Attempted to divide by zero");
        }
        try
        {
            switch (op.Type)
            {
                case SyntaxParser.AsteriskEqual: lhs = lhs.MultiplyBy(rhs); break;
                case SyntaxParser.SolidusEqual:  lhs = lhs.DivideBy(rhs); break;
                case SyntaxParser.PercentEqual:  lhs = lhs.Mod(rhs); break;
                case SyntaxParser.LShiftEqual:   lhs = lhs.LeftShift(rhs); break;
                case SyntaxParser.RShiftEqual:   lhs = lhs.RightShift(rhs); break;
                case SyntaxParser.ARShiftEqual:  lhs = lhs.UnsignedRightShift(rhs); break;
                case SyntaxParser.AmpersandEqual:
                    if (lhs is BoolValue) lhs = lhs.And(rhs);
                    else lhs = lhs.BitwiseAnd(rhs);
                    break;
                case SyntaxParser.CaretEqual: lhs = lhs.BitwiseXor(rhs); break;
                case SyntaxParser.BarEqual:
                    if (lhs is BoolValue) lhs = lhs.Or(rhs);
                    else lhs = lhs.BitwiseOr(rhs);
                    break;
                default:
                    if (lhs.IsDefined && !lhs.TypeCompatible(rhs))
                    {
                        throw new TypeMismatchError(lhsToken);
                    }
                    lhs = rhs.IsObject ? rhs : rhs.AsCopy();
                    break;
            }
            if (op.Type != SyntaxParser.SolidusEqual && IsBinary(lhs, rhs))
            {
                ((NumericValue)lhs).IsBinary = true;
            }
            return lhs;
        }
        catch (InvalidOperationError invalidErr)
        {
            throw new Error(op, invalidErr.Message);
        }
        catch (TypeMismatchError typeErr)
        {
            throw new Error(context, typeErr.Message);
        }
    }

    public override ValueBase VisitExpressionArrow([NotNull] SyntaxParser.ExpressionArrowContext context)
    {
        if (Services == null || _symbolResolver == null)
        {
            return new UndefinedValue();
        }
        if (!context.value.IsDefined)
        {
            context.value = new UserFunctionObject(context.arrow().argList(),
                                                   context.arrow().block(),
                                                   context.arrow().expr(),
                                                   Services.State.Symbols.ActiveScope)
            {
                Name = "()=>"
            };
        }
        return context.value;
    }

    public override ValueBase VisitExpressionDotMember([NotNull] SyntaxParser.ExpressionDotMemberContext context)
    {
        if (Services == null || _symbolResolver == null)
        {
            return new UndefinedValue();
        }
        SymbolBase? sym = Resolve(context, false, !Services.State.InFirstPass);

        if (sym is not IValueResolver resolver)
        {
            if (!Services.State.InFirstPass)
            {
                throw new Error(context.identifierPart(), "Symbol is not an expression");
            }
            Services.State.PassNeeded = true;
            return new UndefinedValue();
        }
        if (sym is Label l && l.Bank == Services.State.Output.CurrentBank)
        {
            return l.Value.Word();
        }
        return resolver.Value;
    }

    public override ValueBase VisitExpressionSubscript([NotNull] SyntaxParser.ExpressionSubscriptContext context)
    {
        ValueBase collection = Eval(context.target);
        if (!collection.IsCollection)
        {
            if (!collection.IsDefined)
            {
                return collection;
            }
            throw new Error(context.LeftSquare().Symbol, "Invalid operation");
        }
        ValueBase result;
        bool isConstant = context.target.IsConstant();
        try
        {
            if (collection.ValueType == ValueType.Dictionary)
            {
                if (context.range() != null)
                {
                    throw new Error(context.range().DoubleDot().Symbol, "Invalid operation");
                }
                ValueBase key = Eval(context.ix);
                isConstant &= context.ix.IsConstant();
                result = collection[key];
            }
            else
            {
                if (context.range() != null)
                {
                    int start = context.range().start != null
                            ? EvalNumber(context.range().start, int.MinValue, int.MaxValue).AsInt()
                            : 0;
                    int end = context.range().end != null
                            ? EvalNumber(context.range().end, int.MinValue, int.MaxValue).AsInt()
                            : collection.Count;
                    if (start < 0) start = collection.Count + start;
                    if (end < 0) end = collection.Count + end;
                    if (context.range().Caret() != null)
                    {
                        end++;
                    }
                    result = collection.FromRange(start..end);
                    isConstant &= context.range().start == null || context.range().start.IsConstant();
                    isConstant &= context.range().end == null || context.range().end.IsConstant();
                }
                else
                {
                    int index = EvalNumber(context.ix, int.MinValue, int.MaxValue).AsInt();
                    if (index < 0) index = collection.Count + index;
                    result = collection[index];
                    isConstant &= context.ix.IsConstant();
                }
            }
        }
        catch (ArgumentOutOfRangeException)
        {
            if (Services?.State.PassNeeded == true)
            {
                return new UndefinedValue();
            }
            throw new Error((ParserRuleContext?)context.ix ?? context.range(), "Index out of range");
        }
        catch (KeyNotFoundException)
        {
            throw new Error(context.ix!, "Key not present in dictionary");
        }
        if (isConstant)
        {
            context.value = result;
        }
        return result;
    }

    public override ValueBase VisitExpressionCall([NotNull] SyntaxParser.ExpressionCallContext context)
    {
        if (context.expr() is SyntaxParser.ExpressionArrowContext)
        {
            // (x) => {.return x*3}(3) not allowed
            throw new Error(context.expr(), "Invalid call expression");
        }
        ValueBase callable = Eval(context.expr());
        if (callable is not FunctionObject callee)
        {
            if (Services == null || _symbolResolver == null || Services.State.InFirstPass)
            {
                if (Services?.State.InFirstPass == true)
                {
                    Services.State.PassNeeded = true;
                }
                return new UndefinedValue();
            }
            throw new Error(context.expr(), "Expression is not callable");
        }
        ArrayValue parms = context.exprList() != null
                    ? (ArrayValue)Visit(context.exprList())
                    : new ArrayValue();
        if (parms.ContainsUndefinedElements)
        {
            return new UndefinedValue();
        }
        if (callable is TypeMethodBase)
        {
            if (context.expr() is not SyntaxParser.ExpressionDotMemberContext member)
            {
                throw new Error(context, "Method call is being used as a function");
            }
            if (!CachedEvaluations.TryPop(out ValueBase? thisObj))
            {
                thisObj = Eval(member.target);
                if (!thisObj.IsDefined)
                {
                    return thisObj;
                }
            }
            parms.Insert(0, thisObj);
        }
        return Invoke(context, callee, parms) ?? throw new Error(context, "Function does not return a value");
    }

    public override ValueBase VisitExpressionIncDec([NotNull] SyntaxParser.ExpressionIncDecContext context)
    {
        if (Services == null || _symbolResolver == null)
        {
            return new UndefinedValue();
        }
        if (Resolve(context.expr(), false, true) is not IValueResolver symbol)
        {
            throw new Error(context, "Identifier expected");
        }
        if (symbol.IsConstant)
        {
            throw new Error(context, "Expression is constant and cannot be modified");
        }
        ValueBase original;
        if (context.expr() is SyntaxParser.ExpressionSubscriptContext subscript)
        {
            ValueBase mutated = Eval(subscript);
            original = mutated.AsCopy();
            mutated.SetAs(context.DoubleHyphen() != null ?
                          original.Decrement() :
                          original.Increment());
            return context.postfix != null ? original : mutated;
        }
        original = symbol.Value.AsCopy();
        symbol.Value = context.DoubleHyphen() != null
                    ? original.Decrement()
                    : original.Increment();
        return context.postfix != null ? original : symbol.Value;
    }

    public override ValueBase VisitExpressionUnary([NotNull] SyntaxParser.ExpressionUnaryContext context)
    {
        ValueBase val = Eval(context.expr()); if (!val.IsDefined) return val;
        if (context.unary_op.Type != SyntaxParser.Bang)
        {
            val = CoerceToNumber(val);
        }
        bool isConst = context.expr().IsConstant();
        val = context.unary_op.Type switch
        {
            SyntaxParser.Hyphen      => val.Negative(),
            SyntaxParser.Tilde       => val.Complement(),
            SyntaxParser.Bang        => val.Not(),
            SyntaxParser.LeftAngle   => val.LSB(),
            SyntaxParser.RightAngle  => val.MSB(),
            SyntaxParser.Ampersand   => val.Word(),
            SyntaxParser.Caret       => val.Bank(),
            SyntaxParser.DoubleCaret => val.HigherWord(),
            _                        => val
        };
        if (isConst)
        {
            context.value = val;
        }
        return val;
    }

    private static bool IsBinary(ValueBase value)
    {
        return value is NumericValue numValue && numValue.IsBinary;
    }

    private static bool IsIntegral(ValueBase value)
    {
        return value.ValueType == ValueType.Integer;
    }

    private static bool IsBinary(ValueBase lhs, ValueBase rhs)
    {
        return (IsBinary(lhs) && (IsBinary(rhs) || IsIntegral(rhs))) ||
               (IsBinary(rhs) && IsIntegral(lhs));
    }

    public override ValueBase VisitExpressionNumericBinary([NotNull] SyntaxParser.ExpressionNumericBinaryContext context)
    {
        ValueBase lhs = CoerceToNumber(Eval(context.lhs)); if (!lhs.IsDefined) return lhs;
        ValueBase rhs = CoerceToNumber(Eval(context.rhs)); if (!rhs.IsDefined) return rhs;
        try
        {
            ValueBase val = context.op.Type switch
            {
                SyntaxParser.DoubleCaret    => lhs.PowerOf(rhs),
                SyntaxParser.Asterisk       => lhs.MultiplyBy(rhs),
                SyntaxParser.Solidus        => lhs.DivideBy(rhs),
                SyntaxParser.Percent        => lhs.Mod(rhs),
                SyntaxParser.LShift         => lhs.LeftShift(rhs),
                SyntaxParser.RShift         => lhs.RightShift(rhs),
                SyntaxParser.ARShift        => lhs.UnsignedRightShift(rhs),
                SyntaxParser.Ampersand      => lhs.BitwiseAnd(rhs),
                SyntaxParser.Caret          => lhs.BitwiseXor(rhs),
                _                           => lhs.BitwiseOr(rhs)
            };
            if (context.lhs.IsConstant() && context.rhs.IsConstant())
            {
                context.value = val;
            }
            if (context.op.Type != SyntaxParser.Solidus && IsBinary(lhs, rhs))
            {
                ((NumericValue)val).IsBinary = true;
            }
            return val;
        }
        catch (DivideByZeroException dbzErr)
        {
            throw new Error(context, dbzErr.Message);
        }
        catch (InvalidOperationError invalidErr)
        {
            throw new Error(context.op, invalidErr.Message);
        }
        catch (TypeMismatchError typeErr)
        {
            throw new Error(context, typeErr.Message);
        }
    }

    public override ValueBase VisitExpressionAdditive([NotNull] SyntaxParser.ExpressionAdditiveContext context)
    {
        ValueBase lhs = Eval(context.lhs); if (!lhs.IsDefined) return lhs;
        ValueBase rhs = Eval(context.rhs); if (!rhs.IsDefined) return rhs;
        ValueBase val = Additive(context, lhs, context.op, rhs);
        if (context.lhs.IsConstant() && context.rhs.IsConstant())
        {
            context.value = val;
        }
        if (IsBinary(lhs, rhs))
        {
            ((NumericValue)val).IsBinary = true;
        }
        return val;
    }

    public override ValueBase VisitExpressionBooleanBinary([NotNull] SyntaxParser.ExpressionBooleanBinaryContext context)
    {
        ValueBase lhs = Eval(context.lhs);
        if (!lhs.IsDefined ||
            (context.op.Type == SyntaxParser.DoubleAmpersand && !lhs.AsBool()) ||
            (context.op.Type == SyntaxParser.DoubleBar && lhs.AsBool()))
        {
            return lhs;
        }
        ValueBase rhs = Eval(context.rhs); if (!rhs.IsDefined) return rhs;
        if (!lhs.TypeCompatible(rhs))
        {
            throw new TypeMismatchError(context.rhs);
        }
        try
        {
            ValueBase val = context.op.Type switch
            {
                SyntaxParser.LeftAngle       => lhs.LessThan(rhs),
                SyntaxParser.LTE             => lhs.LTE(rhs),
                SyntaxParser.GTE             => lhs.GTE(rhs),
                SyntaxParser.RightAngle      => lhs.GreaterThan(rhs),
                SyntaxParser.Spaceship       => new NumericValue(lhs.CompareTo(rhs)),
                SyntaxParser.DoubleEqual     => new BoolValue(lhs.Equals(rhs)),
                SyntaxParser.BangEqual       => new BoolValue(!lhs.Equals(rhs)),
                SyntaxParser.TripleEqual     => new BoolValue(lhs.IsIdenticalTo(rhs)),
                SyntaxParser.BangDoubleEqual => new BoolValue(!lhs.IsIdenticalTo(rhs)),
                SyntaxParser.DoubleAmpersand => lhs.And(rhs),
                _                            => lhs.Or(rhs)
            };
            if (context.lhs.IsConstant() && context.rhs.IsConstant())
            {
                context.value = val;
            }
            return val;
        }
        catch (InvalidOperationError err)
        {
            throw new Error(context.op, err.Message);
        }
        catch (TypeMismatchError typeErr)
        {
            throw new Error(context, typeErr.Message);
        }
    }

    public override ValueBase VisitExpressionConditional([NotNull] SyntaxParser.ExpressionConditionalContext context)
    {
        ValueBase cond = Eval(context.cond); if (!cond.IsDefined) return cond;
        return cond.AsBool() ? Eval(context.then) : Eval(context.els);
    }

    private ValueBase BindSubscriptAssignment(SyntaxParser.ExpressionSubscriptContext subscript, IToken assign, ValueBase rvalue, bool asConstant)
    {
        if (subscript.ix == null)
        {
            throw new Error(subscript, "Left-hand side expression is not a valid lvalue expression");
        }
        if (assign.Type == SyntaxParser.Equal && asConstant)
        {
            throw new Error(subscript, "Invalid operator syntax for assignment");
        }
        ValueBase callee = Eval(subscript.target);
        if (!callee.IsDefined)
        {
            return callee;
        }
        if (!callee.IsObject)
        {
            throw new InvalidOperationError(subscript.Start);
        }
        ValueBase index = Eval(subscript.ix);

        if (callee is ArrayValue || callee.ValueType == ValueType.String)
        {
            if (index.IsDefined && !index.IsNumeric)
            {
                throw new Error(subscript.ix, "Invalid index expression");
            }
            int ix = index.AsInt();
            if (ix < 0)
            {
                ix += callee.Count;
            }
            if (callee.Count <= ix)
            {
                if (Services?.State.PassNeeded == true)
                {
                    return new UndefinedValue();
                }
                throw new Error(subscript.ix, "Index out of range");
            }
        }
        else if (callee is DictionaryValue dict && index.IsDefined && !dict.ContainsKey(index))
        {
            throw new Error(subscript.ix, "Key not found");
        }
        return callee.UpdateMember(index, AssignOperation(subscript, subscript.target.Start, callee[index], assign, rvalue));
    }

    private ValueBase BindTupleAssignment(SyntaxParser.ExpressionCollectionContext coll, IToken assign, ValueBase rvalue, bool asConstant)
    {
        ArrayValue result = new()
        {
            IsTuple = true
        };
        if (coll.tuple().expr().Length != rvalue.Count ||
            rvalue is not ArrayValue rhsArray
            || !rhsArray.IsTuple)
        {
            throw new TypeMismatchError(rvalue.Expression ?? coll);
        }
        for (int i = 0; i < coll.tuple().expr().Length; i++)
        {
            result.Add(BindAssignment(coll.tuple().expr()[i], assign, rhsArray[i], asConstant));
        }
        return result;
    }

    private ValueBase BindAssignment(SyntaxParser.ExprContext lexpr, IToken assign, ValueBase rvalue, bool asConstant)
    {
        if (rvalue is TypeMethodBase)
        {
            throw new Error(rvalue.Expression ?? lexpr, "Right-hand side expression is a method and cannot be assigned");
        }
        if (lexpr is SyntaxParser.ExpressionSubscriptContext subscript)
        {
            return BindSubscriptAssignment(subscript, assign, rvalue, asConstant);
        }
        if (lexpr is SyntaxParser.ExpressionCollectionContext coll && coll.tuple() != null)
        {
            return BindTupleAssignment(coll, assign, rvalue, asConstant);
        }
        if (lexpr is SyntaxParser.ExpressionGroupedContext grouped)
        {
            return BindAssignment(grouped.expr(), assign, rvalue, asConstant);
        }
        if (lexpr is not SyntaxParser.ExpressionSimpleIdentifierContext &&
            lexpr is not SyntaxParser.ExpressionDotMemberContext)
        {
            if (lexpr is not SyntaxParser.ExpressionIncDecContext incDec)
            {
                throw new Error(lexpr, "Left-hand side expression is not a valid lvalue expression.");
            }
            _ = Eval(incDec);
        }
        SymbolBase? lsym = Resolve(lexpr, asConstant, false);
        IValueResolver? lresolver;
        if (lsym != null)
        {
            if (lsym is not IValueResolver resolver)
            {
                throw new TypeMismatchError(lexpr.Start);
            }
            if (resolver is not Variable &&
                (resolver is not Constant ||
                (resolver is Constant && (assign.Type != SyntaxParser.Equal || Services!.State.InFirstPass))))
            {
                throw new SymbolRedefinitionError(lexpr.Start, lsym.Token);
            }
            lresolver = resolver;
        }
        else
        {
            if (!assign.Type.IsOneOf(SyntaxParser.Equal, SyntaxParser.ColonEqual))
            {
                throw new Error(lexpr, "Invalid assignment operation on undefined symbol");
            }
            lresolver = asConstant
                    ? new Constant(lexpr.Start, rvalue, Services!.State.Symbols.ActiveScope)
                    : new Variable(lexpr.Start, rvalue, Services!.State.Symbols.ActiveScope);
            try
            {
                Services.State.Symbols.Define((SymbolBase)lresolver);
            }
            catch
            {
                throw new SymbolRedefinitionError(lexpr.Start, lexpr.Start);
            }
            return rvalue;
        }
        lresolver.Value = AssignOperation(lexpr, lexpr.Start, lresolver.Value, assign, rvalue);
        return lresolver.Value;
    }

    /// <summary>
    /// Update the program counter.
    /// </summary>
    /// <param name="context">The parsed expression of the assignment.</param>
    /// <param name="val">The program counter value.</param>
    /// <param name="logical">The logical program counter should be updated.</param>
    /// <exception cref="Error"></exception>
    public void UpdatePC(SyntaxParser.ExprContext context, int val, bool logical)
    {
        try
        {
            if (!logical)
            {
                Services!.State.Output.SetPC(val);
            }
            else
            {
                Services!.State.Output.SetLogicalPC(val);
            }
        }
        catch (Exception ex)
        {
            if (!Services!.State.PassNeeded || (ex is not ProgramOverflowException && ex is not InvalidPCAssignmentException))
            {
                throw new Error(context, ex.Message);
            }
        }
    }

    private ValueBase VisitAssignment(SyntaxParser.ExpressionAssignmentContext context, bool asConstant)
    {
        if (Services == null || _symbolResolver == null)
        {
            return new UndefinedValue();
        }
        ValueBase rhs = Eval(context.rhs); if (!rhs.IsDefined) return rhs;
        IToken assign = context.assignOp().Start;
        if (context.lhs is SyntaxParser.ExpressionProgramCounterContext)
        {
            ValueBase currentPc = new NumericValue(Services.State.LongLogicalPCOnAssemble);
            ValueBase newPc = AssignOperation(context, context.lhs.Start, currentPc, assign, rhs);
            UpdatePC(context.rhs, newPc.AsInt(), false);
            ((NumericValue)newPc).IsBinary = true;
            return newPc;
        }
        if (context.lhs is SyntaxParser.ExpressionSimpleIdentifierContext ident &&
            ident.registerAsIdentifier() != null &&
            Services.DiagnosticOptions.WarnRegistersAsIdentifiers)
        {
            Services.State.Warnings.Add(new Warning(ident, "Register is being used as an identifier"));
        }
        return BindAssignment(context.lhs, assign, rhs, asConstant);
    }

    public override ValueBase VisitExpressionAssignment([NotNull] SyntaxParser.ExpressionAssignmentContext context)
    {
        return VisitAssignment(context, context.assignOp().Start.Type == SyntaxParser.Equal);
    }

    public override ValueBase VisitExpressionSimpleIdentifier([NotNull] SyntaxParser.ExpressionSimpleIdentifierContext context)
    {
        if (Services == null || _symbolResolver == null)
        {
            return new UndefinedValue();
        }
        if (Resolve(context, false, !Services.State.InFirstPass) is not IValueResolver symbol)
        {
            Services.State.PassNeeded |= !Services.State.Symbols.InFunctionScope;
            return new UndefinedValue();
        }
        if (symbol is Label l && l.Bank == Services.State.Output.CurrentBank)
        {
            return l.Value.Word();
        }
        return symbol.Value;
    }

    public override ValueBase VisitPrimaryExpr([NotNull] SyntaxParser.PrimaryExprContext context)
    {
        string valueText = context.GetText();
        int t = context.Start.Type;
        ValueBase primaryVal;
        try
        {
            primaryVal = t switch
            {
                SyntaxParser.AltBinLiteral or
                SyntaxParser.BinLiteral or
                SyntaxParser.BinFloatLiteral => NumberConverter.ConvertBinary(valueText, t == SyntaxParser.BinFloatLiteral),
                SyntaxParser.CbmScreenCharLiteral or
                SyntaxParser.PetsciiCharLiteral 
                                             => StringConverter.ConvertCbmPetsciiChar(context.Start, Services?.Encoding),
                SyntaxParser.CharLiteral     => StringConverter.ConvertChar(context.Start, Services?.Encoding ?? Encoding.UTF8, Services?.Encoding?.EncodingName),
                SyntaxParser.False           => new BoolValue(false),
                SyntaxParser.HexLiteral or
                SyntaxParser.HexFloatLiteral => NumberConverter.ConvertHex(valueText, t == SyntaxParser.HexFloatLiteral),
                SyntaxParser.NaN             => new NumericValue(double.NaN),
                SyntaxParser.OctLiteral or
                SyntaxParser.OctFloatLiteral => NumberConverter.ConvertOctal(valueText, t == SyntaxParser.OctFloatLiteral),
                SyntaxParser.True            => new BoolValue(true),
                _                            => NumberConverter.ConvertDecimal(valueText, t == SyntaxParser.DecFloatLiteral)
            };
            if (primaryVal.ValueType == ValueType.Integer && (primaryVal.AsDouble() < int.MinValue || primaryVal.AsDouble() > uint.MaxValue))
            {
                throw new IllegalQuantityError(context);
            }
            return primaryVal;
        }
        catch (InvalidCharLiteralError)
        {
            throw new InvalidCharLiteralError(context);
        }
        catch
        {
            throw new IllegalQuantityError(context);
        }
    }

    public override ValueBase VisitExpressionPrimary([NotNull] SyntaxParser.ExpressionPrimaryContext context)
    {
        context.value = VisitChildren(context);
        return context.value;
    }

    public override ValueBase VisitExpressionProgramCounter([NotNull] SyntaxParser.ExpressionProgramCounterContext context)
    {
        if (Services == null || _symbolResolver == null)
        {
            return new UndefinedValue();
        }
        return new NumericValue(Services.State.Output.LogicalPC)
        {
            IsBinary = true
        };
    }

    public override ValueBase VisitExpressionAnonymousLabel([NotNull] SyntaxParser.ExpressionAnonymousLabelContext context)
    {
        if (Services == null || _symbolResolver == null)
        {
            return new UndefinedValue();
        }
        AnonymousLabel? label = Services.State.Symbols.ActiveScope.AnonymousLabels.Resolve(context.Start.Text, Services.State.StatementIndex);
        if (label == null)
        {
            if (Services.State.InFirstPass)
            {
                Services.State.PassNeeded = true;
                return new UndefinedValue();
            }
            throw new Error(context, "Could not resolve anonymous label");
        }
        return label.Value;
    }

    public override ValueBase VisitExpressionStringLiteral([NotNull] SyntaxParser.ExpressionStringLiteralContext context)
    {
        ValueBase stringValue;
        string? encodingName = Services?.Encoding.EncodingName;
        if (context.stringLiteral().StringLiteral() != null || 
            context.stringLiteral().CbmScreenStringLiteral() != null ||
            context.stringLiteral().PetsciiStringLiteral() != null ||
            context.stringLiteral().UnicodeStringLiteral() != null)
        {
            (Encoding enc, string literalText) = EncodingFromPrefix(context.stringLiteral().Start);
            if (Services != null && 
                context.stringLiteral().UnicodeStringLiteral() == null)
            {
                enc = Services.Encoding;
                if (context.stringLiteral().PetsciiStringLiteral() != null)
                {
                    encodingName = "\"petscii\"";
                }
                else if (context.stringLiteral().CbmScreenStringLiteral() != null)
                {
                    encodingName = "\"cbmscreen\"";
                }
            }
            stringValue = GetConvertedString(literalText, enc, encodingName, context);
            context.value = stringValue;
        }
        else stringValue = VisitChildren(context);
        return stringValue;
    }

    public override ValueBase VisitExpressionDictionary([NotNull] SyntaxParser.ExpressionDictionaryContext context)
    {
        DictionaryValue dict = new();
        bool isConstant = true;
        SyntaxParser.KeyValuePairContext[] members = context.dictionary().keyValuePair();
        for (int i = 0; i < members.Length; i++)
        {
            SyntaxParser.KeyContext keyContext = members[i].key();
            ValueBase key;
            if (keyContext.identifierPart() != null)
            {
                key = new StringValue($"\"{members[i].key().identifierPart().NamePart()}\"", 
                                      Services?.Encoding ?? Encoding.UTF8, 
                                      Services?.Encoding.EncodingName);
            }
            else if (keyContext.primaryExpr() != null)
            {
                key = Visit(keyContext.primaryExpr());
            }
            else 
            {
                (Encoding enc, string literalText) = EncodingFromPrefix(keyContext.Start);
                if (Services != null && keyContext.UnicodeStringLiteral() == null)
                {
                    enc = Services.Encoding;
                }
                string? name = null;
                if (enc is AsmEncoding asmEncoding)
                {
                    name = asmEncoding.EncodingName;
                }
                else if (keyContext.CbmScreenStringLiteral() != null)
                {
                    name = "\"cbmscreen\"";
                }
                else if (keyContext.PetsciiStringLiteral() != null)
                {
                    name = "\"petscii\"";
                }
                key = GetConvertedString(literalText, enc, name, context);
            }
            ValueBase val = Eval(members[i].val); isConstant &= members[i].val.IsConstant();
            if (!dict.TryAdd(key, val, out DictionaryValue.AddStatus why))
            {
                throw why switch
                {
                    DictionaryValue.AddStatus.DuplicateKey      => new Error(context, "Duplicate key"),
                    DictionaryValue.AddStatus.KeyTypeInvalid    => new Error(keyContext, "Invalid key type"),
                    DictionaryValue.AddStatus.KeyTypeMismatch   => new Error(keyContext, "Key type mismatch"),
                    _                                           => new Error(members[i].val, "Value type mismatch"),
                };
            }
        }
        if (isConstant)
        {
            context.value = dict;
        }
        return dict;
    }

    public override ValueBase VisitExpressionGrouped([NotNull] SyntaxParser.ExpressionGroupedContext context)
    {
        ValueBase grouped = Eval(context.expr());
        if (context.expr().IsConstant())
        {
            context.value = grouped;
        }
        return grouped;
    }

    /// <summary>
    /// Resolve a label symbol.
    /// </summary>
    /// <param name="context">The label context.</param>
    /// <returns>A <see cref="SymbolBase"/> if resolved, otherwise <c>null</c>.
    /// </returns>
    public SymbolBase? Resolve(SyntaxParser.LabelContext context)
    {
        _symbolResolver!.ResolveMemberOnly = true;
        return _symbolResolver.VisitLabel(context);
    }

    /// <summary>
    /// Resolve a symbol in an expression.
    /// </summary>
    /// <param name="context">The expression context.</param>
    /// <param name="resolveAsScopeMemberOnly">Resolve up to the current scope.</param>
    /// <param name="errorIfRootNotFound">Raise an error if the symbol could not be resolved.</param>
    /// <returns>A <see cref="SymbolBase"/> if resolved, otherwise <c>null</c>.
    /// </returns>
    /// <exception cref="Error"></exception>
    public SymbolBase? Resolve(SyntaxParser.ExprContext context, bool resolveAsScopeMemberOnly, bool errorIfRootNotFound)
    {
        _symbolResolver!.ResolveMemberOnly = resolveAsScopeMemberOnly;
        SymbolBase? sym = _symbolResolver.Resolve(context);
        if (sym == null && errorIfRootNotFound)
        {
            if (context is SyntaxParser.ExpressionDotMemberContext dotMember)
            {
                if (_symbolResolver.Visit(dotMember.expr()) != null)
                {
                    throw new Error(dotMember.identifierPart().Start,
                        $"Target does not contain member named '{dotMember.identifierPart().NamePart()}'");
                }
            }
            throw new Error(context, $"Symbol '{context.Start.Text}' not found");
        }
        return sym;
    }

    /// <summary>
    /// Determines whether the expression is an assignment type, such as:
    /// <code>ID '=' expr</code>
    /// <code>ID '++'</code>
    /// </summary>
    /// <param name="context">The parsed expression.</param>
    /// <returns>True if the expression represents an assignment type of
    /// expression.</returns>
    public static bool ExpressionIsAssignmentType(SyntaxParser.ExprContext context)
    {
        return context is SyntaxParser.ExpressionIncDecContext ||
               context is SyntaxParser.ExpressionAssignmentContext;
    }

    /// <summary>
    /// Get the cached evaluations encountered when resolving certain expressions.
    /// </summary>
    public Stack<ValueBase> CachedEvaluations { get; }

    /// <summary>
    /// The shared <see cref="AssemblyServices"/> for assembly runtime.
    /// </summary>
    public AssemblyServices? Services { get; }
}
