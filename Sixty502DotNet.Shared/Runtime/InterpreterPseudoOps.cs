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

    private void GenFills(SyntaxParser.DirectiveContext directive, SyntaxParser.ExprListContext? expressions)
    {
        if (expressions == null)
        {
            throw new Error(directive, "Expression expected");
        }
        SyntaxParser.ExprContext[] operands = expressions.expr();
        int amount = Services.Evaluator.SafeEvalNumber(operands[0], 0, ushort.MaxValue, 0);
        if (operands.Length == 2)
        {
            ValueBase fillvalue = Services.Evaluator.Eval(operands[1]);
            if (fillvalue.IsDefined)
            {
                if (directive.Start.Type == SyntaxParser.Align)
                {
                    Services.State.Output.Align(amount, (long)fillvalue.AsDouble());
                }
                else
                {
                    Services.State.Output.Fill(amount, (long)fillvalue.AsDouble());
                }
                GenListing((SyntaxParser.InstructionContext)directive.Parent, '>', false);
                return;
            }
        }
        if (directive.Start.Type == SyntaxParser.Align)
        {
            Services.State.Output.Align(amount);
            return;
        }
        GenListing((SyntaxParser.InstructionContext)directive.Parent, '>', false);
        Services.State.Output.Fill(amount);
    }

    private void GenBinHexString(SyntaxParser.ExprContext expression, int radix)
    {
        var size = radix == 16 ? 2 : 8;
        var len = size - 1;
        var binHexDigits = new List<string>();
        var startIndex = 0;
        string binHexString = Evaluator.EvalStringLiteral(expression);
        while (startIndex < binHexString.Length)
        {
            if (startIndex + len >= binHexString.Length)
                binHexDigits.Add(binHexString[startIndex..]);
            else
                binHexDigits.Add(binHexString.Substring(startIndex, size));
            startIndex += size;
        }
        try
        {
            binHexDigits.ForEach(d => Services.State.Output.Add(Convert.ToByte(d, radix)));
        }
        catch (FormatException)
        {
            throw new Error(expression, "String is not in the correct format");
        }
    }

    private void GenBinHexStrings(SyntaxParser.ExpressionCollectionContext subArray, int radix)
    {
        SyntaxParser.ExprContext[] exprs = subArray.tuple() != null
                                        ? subArray.tuple().expr()
                                        : subArray.array().exprList().expr();
        for (int i = 0; i < exprs.Length; i++)
        {
            GenBinHexString(exprs[i], radix);
        }
    }

    private void GenBinHexStrings(SyntaxParser.PseudoOpArgListContext args, int radix)
    {
        var size = radix == 16 ? 2 : 8;
        var len = size - 1;
        for (int i = 0; i < args.pseudoOpArg().Length; i++)
        {
            SyntaxParser.PseudoOpArgContext arg = args.pseudoOpArg()[i];
            if (arg.Query() != null)
            {
                Services.State.Output.AddUninitialized(len);
                continue;
            }
            if (arg.expr() is SyntaxParser.ExpressionCollectionContext col)
            {
                GenBinHexStrings(col, radix);
                continue;
            }
            GenBinHexString(arg.expr(), radix);
        }
    }

    private void GenValues(ArrayValue arrayValue, double minValue, double maxValue, int size, Func<double, double>? transform = null)
    {
        for (int i = 0; i < arrayValue.Count; i++)
        {
            ValueBase val = arrayValue[i];
            if (val is ArrayValue subArray)
            {
                GenValues(subArray, minValue, maxValue, size, transform);
                continue;
            }
            if (!val.IsDefined)
            {
                Services.State.Output.AddUninitialized(size);
                continue;
            }
            double gen = val.AsDouble();
            if (transform != null)
            {
                gen = transform(gen);
            }
            if (gen < minValue || gen > maxValue)
            {
                if (Services.State.PassNeeded)
                {
                    Services.State.Output.AddUninitialized(size);
                    continue;
                }
                throw new IllegalQuantityError(val.Expression!);
            }
            Services.State.Output.Add(gen, size);
        }
    }

    private void GenValues(SyntaxParser.PseudoOpArgListContext args, double minValue, double maxValue, int size, Func<double, double>? transform = null)
    {
        ArrayValue values = new();
        SyntaxParser.PseudoOpArgContext[] pseudoOpArgs = args.pseudoOpArg();
        for (int i = 0; i < pseudoOpArgs.Length; i++)
        {
            if (pseudoOpArgs[i].Query() == null)
            {
                ValueBase val = Services.Evaluator.Eval(pseudoOpArgs[i].expr());
                if (_options.DiagnosticOptions.WarnTextInNonTextPseudoOp && val.ValueType == ValueType.String)
                {
                    Services.State.Warnings.Add(
                        new Warning(pseudoOpArgs[i].expr(), 
                                    "Textual data was inserted using non-string pseudo-op"));
                }
                values.Add(val);
            }
            else
            {
                values.Add(new UndefinedValue());
            }
        }
        GenValues(values, minValue, maxValue, size, transform);
    }

    private List<byte> GetStringBytes(ArrayValue array)
    {
        List<byte> stringBytes = new();
        for (int i = 0; i < array.Count; i++)
        {
            ValueBase val = array[i];
            if (!val.IsDefined)
            {
                Services.State.Output.AddUninitialized(1);
                continue;
            }
            if (val.ValueType == ValueType.String)
            {
                stringBytes.AddRange(val.ToBytes());
            }
            else if (val is ArrayValue subArray)
            {
                stringBytes.AddRange(GetStringBytes(subArray));
            }
            else
            {
                double num = val.AsDouble();
                if (num < int.MinValue || num > uint.MaxValue)
                {
                    throw new IllegalQuantityError(array[i].Expression!);
                }
                stringBytes.AddRange(Services.State.Output.ConvertToBytes(num));
            }
        }
        return stringBytes;
    }

    private void GenStrings(int pseudoOp, SyntaxParser.PseudoOpArgListContext args)
    {
        SyntaxParser.PseudoOpArgContext[] argList = args.pseudoOpArg();
        ArrayValue evaluedStrings = new();
        for (int i = 0; i < argList.Length; i++)
        {
            ValueBase val = new UndefinedValue();
            if (argList[i].expr() != null)
            {
                val = Services.Evaluator.Eval(argList[i].expr());
            }
            evaluedStrings.Add(val);
        }
        List<byte> stringBytes = GetStringBytes(evaluedStrings);
        switch (pseudoOp)
        {
            case SyntaxParser.Cstring:
                stringBytes.Add(0);
                break;
            case SyntaxParser.Lstring:
            case SyntaxParser.Nstring:
                int highBitSet = stringBytes.FindIndex(b => b > 0x7f);
                if (highBitSet >= 0)
                {
                    throw new IllegalQuantityError(argList[highBitSet]);
                }
                if (pseudoOp == SyntaxParser.Lstring)
                {
                    for (int i = 0; i < stringBytes.Count; i++)
                    {
                        stringBytes[i] <<= 1;
                    }
                    stringBytes[^1] |= 1;
                }
                else
                {
                    stringBytes[^1] |= 0x80;
                }
                break;
            case SyntaxParser.Pstring:
                if (stringBytes.Count > 255)
                {
                    throw new Error(argList[^1], "Too many bytes in '.pstring'");
                }
                stringBytes.Insert(0, (byte)stringBytes.Count);
                break;
        }
        Services.State.Output.AddBytes(stringBytes);
        if (Services.DiagnosticOptions.WarnTextInNonTextPseudoOp)
        {
            ValueBase? firstNonString = evaluedStrings.FirstOrDefault(v => v is not StringValue);
            if (firstNonString != null)
            {
                if (firstNonString.Expression != null)
                {
                    throw new Warning(firstNonString.Expression, "Non string expression in string pseudo-op");
                }
                throw new Warning((IToken?)null, "Non string expression string pseudo-op");
            }
        }
    }

    private double CalculateBankedAddress(double input)
    {
        if (((long)input & 0xffff0000) / 0x10000 == Services.State.Output.CurrentBank)
        {
            return (int)input & 0xffff;
        }
        return input;
    }

    private void GenData(SyntaxParser.PseudoOpContext pseudoOp, SyntaxParser.PseudoOpArgListContext args)
    {
        int bank = Services.State.Output.CurrentBank;
        bool longAddr = _options.ArchitectureOptions.LongAddressing;
        double addrMax = longAddr ? UInt24.MaxValue : ushort.MaxValue;
        switch (pseudoOp.Start.Type)
        {
            case SyntaxParser.Addr:
                GenValues(args, 0, addrMax, longAddr ? 3 : 2, CalculateBankedAddress);
                break;
            case SyntaxParser.Bankbytes:
                GenValues(args, int.MinValue, uint.MaxValue, 1, v => ((int)v & 0xff0000) / 0x10000);
                break;
            case SyntaxParser.Bstring:
                GenBinHexStrings(args, 2);
                break;
            case SyntaxParser.Byte:
                GenValues(args, byte.MinValue, byte.MaxValue, 1);
                break;
            case SyntaxParser.Cbmflt:
            case SyntaxParser.Cbmfltp:
                bool packed = pseudoOp.Start.Type == SyntaxParser.Cbmfltp;
                int size = packed ? 5 : 6;
                GenValues(args, -2.93783588E+39, 1.70141183E+38, size, fl =>
                {
                    List<byte> bytes = CbmFloatFunction.ToBytes(fl, packed).ToList();
                    while (bytes.Count < 8) bytes.Add(0);
                    long value = BitConverter.ToInt64(bytes.ToArray());
                    return value;
                });
                break;
            case SyntaxParser.Char:
            case SyntaxParser.Sbyte:
                GenValues(args, sbyte.MinValue, sbyte.MaxValue, 1);
                break;
            case SyntaxParser.Cstring:
            case SyntaxParser.Lstring:
            case SyntaxParser.Nstring:
            case SyntaxParser.Pstring:
            case SyntaxParser.String:
                GenStrings(pseudoOp.Start.Type, args);
                break;
            case SyntaxParser.Dint:
                GenValues(args, int.MinValue, int.MaxValue, 4);
                break;
            case SyntaxParser.Dword:
                GenValues(args, uint.MinValue, uint.MaxValue, 4);
                break;
            case SyntaxParser.Hibytes:
                GenValues(args, int.MinValue, uint.MaxValue, 1, v => ((int)v & 0xff00) / 256);
                break;
            case SyntaxParser.Hiwords:
                GenValues(args, int.MinValue, uint.MaxValue, 2, v => ((int)v & 0xffff00) / 256);
                break;
            case SyntaxParser.Hstring:
                GenBinHexStrings(args, 16);
                break;
            case SyntaxParser.Lint:
                GenValues(args, Int24.MinValue, Int24.MaxValue, 3);
                break;
            case SyntaxParser.Lobytes:
                GenValues(args, int.MinValue, uint.MaxValue, 1, v => (int)v & 0xff);
                break;
            case SyntaxParser.Long:
                GenValues(args, UInt24.MinValue, UInt24.MaxValue, 3);
                break;
            case SyntaxParser.Lowords:
                GenValues(args, int.MinValue, uint.MaxValue, 2, v => (int)v & 0xffff);
                break;
            case SyntaxParser.Rta:
                GenValues(args, 1, addrMax + 1, bank == 0 ? 2 : 3, v =>
                CalculateBankedAddress(v) - 1);
                break;
            case SyntaxParser.Short:
            case SyntaxParser.Sint:
                GenValues(args, short.MinValue, short.MaxValue, 2);
                break;
            case SyntaxParser.Word:
                GenValues(args, ushort.MinValue, ushort.MaxValue, 2);
                break;
        }
    }

    public override int VisitInstructionPseudoOp([NotNull] SyntaxParser.InstructionPseudoOpContext context)
    {
        if (Services.State.Symbols.InFunctionScope)
        {
            throw new Error(context, "Data generation not permitted in function blocks");
        }
        if (!Services.State.Output.HasOutput || Services.State.LongLogicalPCOnAssemble <= Services.State.Output.ProgramStart)
        {
            int firstOutput = context.pseudoOpArgList().pseudoOpArg().ToList().FindIndex(po => po.Query() == null);
            if (firstOutput > -1)
            {
                Services.State.LongLogicalPCOnAssemble += firstOutput;
                Services.State.LogicalPCOnAssemble += firstOutput;
            }
        }
        GenData(context.pseudoOp(), context.pseudoOpArgList());
        return GenListing(context, '>', false);
    }
}

