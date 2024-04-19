//-----------------------------------------------------------------------------
// Copyright (c) 2017-2024 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System.Text;
using Antlr4.Runtime.Misc;

namespace Sixty502DotNet.Shared;

public sealed partial class Interpreter : SyntaxParserBaseVisitor<int>
{
    private void Print(SyntaxParser.DirectiveContext directive, SyntaxParser.ExprContext expression)
    {
        ValueBase outputVal = Services.Evaluator.Eval(expression);
        string output = (outputVal.ValueType == ValueType.String || outputVal.ValueType == ValueType.Char)
                ? outputVal.AsString()
                : outputVal.ToString()!;
        if (!Services.State.InFirstPass)
        {
            return;
        }
        switch (directive.Start.Type)
        {
            case SyntaxParser.Echo:
                Console.WriteLine(output);
                break;
            case SyntaxParser.Error:
                throw new Error(directive, output);
            default:
                throw new Warning(directive, output);
        }
    }

    private void CustomError(SyntaxParser.DirectiveContext directive, SyntaxParser.ExprListContext? operand)
    {
        bool assertTrue = directive.Start.Type == SyntaxParser.Assert;
        string directiveName = $"'{directive.Start.Text.ToLower()}'";
        SyntaxParser.ExprContext[]? operands = operand?.expr();
        if (operands?.Length < 1 || (!assertTrue && operands!.Length < 2))
        {
            throw new Error(directive, $"Too few arguments for {directiveName}");
        }
        if (operands!.Length > 2)
        {
            throw new Error(operands[2], $"Too many arguments for {directiveName}");
        }
        ValueBase assertion = Services.Evaluator.Eval(operands[0]);
        if (assertion.IsDefined && assertion.AsBool() != assertTrue)
        {
            string error = "Assertion failed";
            if (operands.Length > 1)
            {
                ValueBase errorString = Services.Evaluator.Eval(operands[1]);
                if (errorString.IsDefined)
                {
                    error = errorString.AsString();
                }
            }
            if (directive.Start.Type == SyntaxParser.Warnif)
            {
                Services.State.Warnings.Add(new Warning(directive, error));
                return;
            }
            throw new Error(operands[0], error);
        }
    }

    private void DefineSection(SyntaxParser.DirectiveContext directive, SyntaxParser.ExprListContext? operand)
    {
        if (!Services.State.InFirstPass)
        {
            return;
        }
        SyntaxParser.StatInstructionContext? stat = directive.Parent.Parent as SyntaxParser.StatInstructionContext;
        if (stat?.label() != null)
        {
            throw new Error(stat.label(),
                "Label declaration is illegal in the current context because its address is undetermined");
        }
        if (operand?.expr().Length < 2)
        {
            throw new Error(directive, "One or more parameters is missing");
        }
        SyntaxParser.ExprContext[] operands = operand!.expr();
        if (operands.Length > 3)
        {
            throw new Error(operands[3], "Unexpected parameter");
        }
        string name = Evaluator.EvalStringLiteral(operands[0]);
        ValueBase startVal = Services.Evaluator.Eval(operands[1]);
        if (!startVal.IsDefined)
        {
            throw new Error(operands[1], "Expression must be evaluated in first pass");
        }
        int start = startVal.AsInt();
        int end;
        if (operands.Length == 3)
        {
            ValueBase endVal = Services.Evaluator.Eval(operands[2]);
            if (!endVal.IsDefined)
            {
                throw new Error(operands[2], "Expression must be evaluated in first pass");
            }
            end = endVal.AsInt();
        }
        else
        {
            end = 0x10000;
        }
        Services.State.Output.DefineSection(name, start, end);
    }

    private void Import(SyntaxParser.ExprContext operand)
    {
        SyntaxParser.ExprContext root = operand;
        if (operand is SyntaxParser.ExpressionDotMemberContext)
        {
            while (operand is SyntaxParser.ExpressionDotMemberContext dotMember)
            {
                operand = dotMember.expr();
            }
        }
        if (operand is not SyntaxParser.ExpressionSimpleIdentifierContext)
        {
            throw new Error(operand, "Scope must be a valid identifier");
        }
        SymbolBase? sym = Services.Evaluator.Resolve(root, false, true);
        if (sym == null || sym is not ScopedSymbol scopedSym)
        {
            throw new Error(root, "'.import' directive expects a declared named scope");
        }
        if (Services.State.Symbols.ScopeIsActive(scopedSym))
        {
            throw new Error(root, "Cannot import active scope");
        }
        if (Services.State.Symbols.ImportedScopes.Any(s => ReferenceEquals(s, scopedSym)))
        {
            throw new Warning(root, "Scope previously imported");
        }
        Services.State.Symbols.ImportedScopes.Add(scopedSym);
    }

    private void MapUnmap(SyntaxParser.DirectiveContext directive, SyntaxParser.ExprListContext? operand)
    {
        int min = 1, max = 2;
        if (directive.Start.Type == SyntaxParser.Map)
        {
            min++;
            max++;
        }
        if (operand?.expr().Length < min)
        {
            throw new Error(operand, "One or more parameters is missing");
        }
        if (operand!.expr().Length > max)
        {
            throw new Error(operand, "Too many parameter");
        }
        string mapping;
        ValueBase startExpr = Services.Evaluator.Eval(operand.expr()[0]);
        if (startExpr.ValueType == ValueType.String || startExpr.ValueType == ValueType.Char)
        {
            mapping = startExpr.AsString();
        }
        else
        {
            mapping = char.ConvertFromUtf32(startExpr.AsInt());
        }
        if (operand.expr().Length == 3)
        {
            ValueBase endExpr = Services.Evaluator.Eval(operand.expr()[1]);
            if (startExpr.ValueType == ValueType.String || startExpr.ValueType == ValueType.Char)
            {
                mapping += endExpr.AsString();
            }
            else
            {
                mapping += char.ConvertFromUtf32(endExpr.AsInt());
            }
        }
        if (mapping.Length > 2)
        {
            throw new Error(operand.expr()[0], "Invalid parameter");
        }
        if (directive.Start.Type == SyntaxParser.Map)
        {
            int codepoint = Services.Evaluator.Eval(operand.expr()[^1]).AsInt();
            if (codepoint < 0 || codepoint > 0x10ffff)
            {
                throw new IllegalQuantityError(operand.expr()[^1]);
            }
            Services.Encoding.Map(mapping, codepoint);
        }
        else
        {
            Services.Encoding.Unmap(mapping);
        }
    }

    private void MultiExpressionOperand(SyntaxParser.DirectiveContext directive, SyntaxParser.ExprListContext? operand)
    {
        if (operand == null)
        {
            throw new Error(directive, "Expression expected");
        }
        SyntaxParser.ExprContext[] exprs = operand.expr();
        for (int i = 0; i < exprs.Length; i++)
        {
            if (directive.Start.Type == SyntaxParser.Let)
            {
                Services.Evaluator.SetVariable(exprs[i]);
                continue;
            }
            ValueBase str = Services.Evaluator.Eval(exprs[i]);
            if (str.ValueType == ValueType.String || str.ValueType == ValueType.Char)
            {
                Services.State.Output.AddBytes(Services.Encoding.GetBytes(str.AsString()));
                continue;
            }
            Services.State.Output.AddBytes(Services.Encoding.GetBytes(str.ToString()!));
        }
        if (directive.Start.Type == SyntaxParser.Stringify)
        {
            _ = GenListing((SyntaxParser.InstructionContext)directive.Parent, '>', false);
        }
    }

    private void NoOperandDirective(SyntaxParser.DirectiveContext directive, SyntaxParser.ExprListContext? operands)
    {
        if (operands != null)
        {
            throw new Error(operands, "Unexpected parameter");
        }
        switch (directive.Start.Type)
        {
            case SyntaxParser.Break:
            case SyntaxParser.Continue:
                if (directive.Start.Type == SyntaxParser.Break) throw new Break(directive.Start);
                throw new Continue(directive.Start);
            case SyntaxParser.Endrelocate:
            case SyntaxParser.Realpc:
                Services.State.Output.SynchPC();
                break;
            case SyntaxParser.Forcepass:
                Services.State.PassNeeded |= Services.State.InFirstPass;
                break;
            default:
                Services.State.PrintOff = directive.Start.Type == SyntaxParser.Proff;
                break;
        }
    }

    private void SingleExpressionOperand(SyntaxParser.DirectiveContext directive, SyntaxParser.ExprListContext? expressions)
    {
        if (expressions == null)
        {
            throw new Error(directive, "Expression paramter missing");
        }
        var stat = (SyntaxParser.StatContext)directive.Parent.Parent;
        SyntaxParser.ExprContext[] exprs = expressions.expr();
        if (exprs.Length > 1)
        {
            throw new Error(exprs[1], "Unexpected parameter");
        }
        switch (directive.Start.Type)
        {
            case SyntaxParser.Cpu:
                string cpuid = Evaluator.EvalStringLiteral(exprs[0]);
                try
                {
                    _encoder.SetCpu(cpuid);
                    return;
                }
                catch
                {
                    throw new Error(exprs[0], "Invalid cpuid specified");
                }
            case SyntaxParser.Echo:
            case SyntaxParser.Error:
            case SyntaxParser.Warn:
                Print(directive, exprs[0]);
                return;
            case SyntaxParser.Encoding:
                StringValue encoding = new($"\"{Evaluator.EvalStringLiteral(exprs[0])}\"", Encoding.UTF8, null);
                Services.Encoding.SelectEncoding(encoding.ToString());
                return;
            case SyntaxParser.Format:
                if (Services.State.InFirstPass)
                {
                    if (Services.State.Output.OutputFormat != null)
                    {
                        throw new Warning(directive, "'.format' directive ignored");
                    }
                    Services.State.Output.OutputFormat =
                        OutputFormatSelector.Select(Evaluator.EvalStringLiteral(exprs[0]), _encoder.Cpuid);
                }
                return;
            case SyntaxParser.Import:
                Import(exprs[0]);
                return;
            case SyntaxParser.Invoke:
                _ = Services.Evaluator.Invoke(exprs[0]);
                return;
            case SyntaxParser.Section:
                string section = Evaluator.EvalStringLiteral(exprs[0]);
                if (!Services.State.Output.SetSection(section))
                {
                    throw new Error(exprs[0], "Could not select section");
                }
                GenListing(stat.Start, $"* = ${Services.State.Output.LogicalPC:x4}  // section \"{section}\"");
                return;
            default:
                break;
        }
        double minValue = sbyte.MinValue, maxValue = byte.MaxValue;
        if (directive.Start.Type.IsOneOf(SyntaxParser.Org, SyntaxParser.Pseudopc, SyntaxParser.Relocate))
        {
            minValue = short.MinValue;
            maxValue = ushort.MaxValue;
        }
        int val = Services.Evaluator.SafeEvalNumber(exprs[0], minValue, maxValue, 0);
        switch (directive.Start.Type)
        {
            case SyntaxParser.Bank:
                Services.State.Output.SetBank(val, _options.GeneralOptions.ResetPCOnBank);
                GenListing(stat.Start, $"// bank {val} selected (effective address ${val*0x100000|Services.State.Output.LogicalPC:x6}");
                break;
            case SyntaxParser.DotEor:
                Services.State.Output.Transform = b => (byte)(b ^ val);
                break;
            case SyntaxParser.Initmem:
                Services.State.Output.InitMemory((byte)val);
                break;
            case SyntaxParser.Org:
            case SyntaxParser.Pseudopc:
            case SyntaxParser.Relocate:
                Services.Evaluator.UpdatePC(exprs[0], val, directive.Start.Type != SyntaxParser.Org);
                if (!_options.OutputOptions.NoAssembly)
                {
                    if (_options.OutputOptions.NoSource)
                    {
                        GenListing(stat.Start, $".{val:x4}");
                        break;
                    }
                    GenListing(stat.Start, $".{val,-55:x4}{stat!.GetSourceLine(_options.OutputOptions.VerboseList)}");
                }
                break;
        }
    }

    private int DirectiveInstruction(SyntaxParser.DirectiveContext directive, SyntaxParser.ExprListContext? operands)
    {
        switch (directive.Start.Type)
        {
            case SyntaxParser.Align:
            case SyntaxParser.Fill:
                GenFills(directive, operands);
                break;
            case SyntaxParser.Assert:
            case SyntaxParser.Errorif:
            case SyntaxParser.Warnif:
                CustomError(directive, operands);
                break;
            case SyntaxParser.Bank:
            case SyntaxParser.Cpu:
            case SyntaxParser.DotEor:
            case SyntaxParser.Echo:
            case SyntaxParser.Encoding:
            case SyntaxParser.Equ:
            case SyntaxParser.Error:
            case SyntaxParser.Format:
            case SyntaxParser.Global:
            case SyntaxParser.Import:
            case SyntaxParser.Initmem:
            case SyntaxParser.Invoke:
            case SyntaxParser.Org:
            case SyntaxParser.Pseudopc:
            case SyntaxParser.Relocate:
            case SyntaxParser.Section:
            case SyntaxParser.Warn:
                SingleExpressionOperand(directive, operands);
                break;
            case SyntaxParser.Binary:
                Binary(directive, operands);
                break;
            case SyntaxParser.Break:
            case SyntaxParser.Continue:
            case SyntaxParser.Endrelocate:
            case SyntaxParser.Forcepass:
            case SyntaxParser.Label:
            case SyntaxParser.Proff:
            case SyntaxParser.Pron:
            case SyntaxParser.Realpc:
                NoOperandDirective(directive, operands);
                break;
            case SyntaxParser.Dsection:
                DefineSection(directive, operands);
                break;
            case SyntaxParser.Goto:
                if (operands == null)
                {
                    throw new Error(directive, "Expected label for '.goto'");
                }
                throw new Goto(operands);
            case SyntaxParser.Let:
            case SyntaxParser.Stringify:
                MultiExpressionOperand(directive, operands);
                break;
            case SyntaxParser.Map:
            case SyntaxParser.Unmap:
                MapUnmap(directive, operands);
                break;
            case SyntaxParser.Return:
                if (operands != null)
                {
                    if (operands.expr().Length > 1)
                    {
                        throw new Error(operands.expr()[1], "Unexpected expression");
                    }
                    throw new Return(directive.Start, Services.Evaluator.Eval(operands.expr()[0]));
                }
                throw new Return(directive.Start, null);
        }
        return 0;
    }

    private void Binary(SyntaxParser.DirectiveContext directive, SyntaxParser.ExprListContext? operands)
    {
        if (operands?.expr() == null)
        {
            throw new Error(directive, "Binary file name expected");
        }
        string fileName = Evaluator.EvalStringLiteral(operands.expr()[0]);
        int offset = 0, size = -1;
        if (operands.expr().Length > 1)
        {
            offset = Services.Evaluator.SafeEvalNumber(operands.expr()[1], short.MinValue, ushort.MaxValue - 1, 0);
            if (operands.expr().Length > 2)
            {
                size = Services.Evaluator.SafeEvalNumber(operands.expr()[2], 1, ushort.MaxValue);
                if (operands.expr().Length > 3)
                {
                    throw new Error(operands.expr()[3], "Unexpected operand");
                }
            }
        }
        BinaryFile? f = _incBins.Get(fileName) ?? throw new Error(operands.expr()[0], "File not found");
        if (!f.Open())
        {
            throw new Error(operands.expr()[0], "Could not open file");
        }
        if (size == -1)
        {
            size = f.Data.Length - offset;
        }
        if (size < 0 || size + offset > f.Data.Length)
        {
            throw new Error(operands.expr()[^1], "Size and offset are greater than binary file length");
        }
        Services.State.Output.AddBytes(f.Data.Skip(offset).Take(size));
        if (directive.Parent is SyntaxParser.InstructionContext instruction)
        {
            GenListing(instruction, '>', false);
        }
    }

    public override int VisitInstructionDirective([NotNull] SyntaxParser.InstructionDirectiveContext context)
    {
        switch (context.directive().Start.Type)
        {
            case SyntaxParser.Auto:
            case SyntaxParser.Dp:
            case SyntaxParser.M8:
            case SyntaxParser.M16:
            case SyntaxParser.Manual:
            case SyntaxParser.MX8:
            case SyntaxParser.MX16:
            case SyntaxParser.X8:
            case SyntaxParser.X16:
                if (!_encoder.HandleDirective(context.directive(), context.exprList()))
                {
                    throw new Error(context, $"Directive '{context.directive().GetText()}' invalid in this context");
                }
                return 0;
        }
        return DirectiveInstruction(context.directive(), context.exprList());
    }
}
