//-----------------------------------------------------------------------------
// Copyright (c) 2017-2022 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using Antlr4.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sixty502DotNet
{
    using Expr = Sixty502DotNetParser.ExprContext;
    using PseudoList = Sixty502DotNetParser.PseudoOpListContext;
    using PseudoArg = Sixty502DotNetParser.PseudoOpArgContext;

    /// <summary>
    /// A class responsible for generating code from various pseudo-ops.
    /// </summary>
    public class PseudoOps
    {
        private readonly BinaryFileCollection _binaryFiles;
        private readonly AssemblyServices _services;

        /// <summary>
        /// Construct a new instance of the <see cref="PseudoOps"/> object.
        /// </summary>
        /// <param name="services">The shared <see cref="AssemblyServices"/>
        /// object.</param>
        public PseudoOps(AssemblyServices services)
        {
            _services = services;
            _binaryFiles = new BinaryFileCollection();
        }

        private Value? GetExpr(PseudoArg[] args, int index)
        {
            if (args[index].expr() != null)
            {
                return _services.ExpressionVisitor.Visit(args[index].expr());
            }
            _services.Log.LogEntry(args[index], Errors.ExpectedExpression);
            return null;
        }

        private bool TryGetMathExpr(PseudoArg[] args, int index, double minValue, double maxValue, out double value)
        {
            if (args[index].expr() != null)
            {
                return _services.ExpressionVisitor.TryGetArithmeticExpr(args[index].expr(), minValue, maxValue, out value);
            }
            _services.Log.LogEntry(args[index], Errors.ExpectedExpression);
            value = double.NaN;
            return false;
        }

        private bool GenValue(Expr expr, double minValue, double maxValue, int setSize, bool isAddr, bool isRta, uint mask)
        {
            long value;
            if (_services.State.PassNeeded)
            {
                _services.Output.AddUninitialized(setSize);
                return true;
            }
            if (isAddr)
            {
                var addrVal = _services.ExpressionVisitor.Visit(expr);
                if (addrVal.ToInt() < minValue || addrVal.ToInt() > maxValue)
                {
                    _services.Log.LogEntry(expr, Errors.IllegalQuantity);
                    return false;
                }
                value = addrVal.ToLong();
                if (isRta)
                {
                    value = (value - 1) & 0xFFFF;
                }
            }
            else
            {
                if (!_services.ExpressionVisitor.TryGetArithmeticExpr(expr, minValue, maxValue, out var numVal))
                {
                    if (_services.State.PassNeeded)
                    {
                        _services.Output.AddUninitialized(setSize);
                        return true;
                    }

                    return false;
                }
                value = (long)numVal;
                if (_services.Options.WarnAboutUsingTextInNonTextPseudoOps &&
                    _services.ExpressionVisitor.TryGetPrimaryExpression(expr, out var valObj) &&
                    valObj.IsString)
                {
                    _services.Log.LogEntry(expr, "Textual data was inserted using non-string pseudo-op.", false);
                }
            }
            if (mask > 0)
            {
                int shift = 0;
                value &= mask;
                while ((mask & 0xff) == 0)
                {
                    if (shift == 0)
                    {
                        shift = 1;
                    }
                    else
                    {
                        shift <<= 1;
                    }
                    mask >>= 8;
                }
                value >>= shift * 8;
            }
            _services.Output.Add(value, setSize);
            return true;
        }

        private bool GenBinHexString(string hexBinString, int radix, Expr expr)
        {
            var size = radix == 16 ? 2 : 8;
            var len = size - 1;
            var hexBinDigits = new List<string>();
            var startIndex = 0;
            while (startIndex < hexBinString.Length)
            {
                if (startIndex + len >= hexBinString.Length)
                    hexBinDigits.Add(hexBinString[startIndex..]);
                else
                    hexBinDigits.Add(hexBinString.Substring(startIndex, size));
                startIndex += size;
            }
            try
            {
                hexBinDigits.ForEach(d => _services.Output.Add(Convert.ToByte(d, radix)));
                return true;
            }
            catch (FormatException)
            {
                _services.Log.LogEntry(expr, "String is not in the correct format.");
                return false;
            }
        }

        /// <summary>
        /// Perform operands on the expression list of the <c>.bstring</c>/
        /// <c>.hstring</c> pseudo-op.
        /// </summary>
        /// <param name="directive">The directive name.</param>
        /// <param name="expressions">The parsed pseudo-op list.</param>
        public void GenBinHexStrings(int directive, PseudoList expressions)
        {
            var radix = directive == Sixty502DotNetParser.Bstring ? 2 : 16;
            foreach (var arg in expressions.pseudoOpArg())
            {
                if (arg.Query() != null)
                {
                    _services.Output.AddUninitialized(1, false);
                }
                else if (!_services.ExpressionVisitor.TryGetPrimaryExpression(arg.expr(), out var str) ||
                    !str.IsString)
                {
                    _services.Log.LogEntry(arg.expr(), Errors.StringExpected);
                    return;
                }
                else if (!GenBinHexString(str.ToString(true), radix, arg.expr()))
                {
                    return;
                }
            }
        }

        /// <summary>
        /// Generate a output based on value-based pseudo-ops.
        /// </summary>
        /// <param name="args">The parsed argument list.</param>
        /// <param name="minValue">The minimum value.</param>
        /// <param name="maxValue">The maximum value.</param>
        /// <param name="setSize">The set size of each output in the
        /// expression list.</param>
        /// <param name="isAddr">Determines whether the pseudo-op is an
        /// addressing output.</param>
        /// <param name="isRta">Determines whether the pseudo-op is an
        /// <c>.rta</c>, so that each expression before output is decremented.
        /// </param>
        /// <param name="mask">The mask to apply to each expression in the
        /// output, for instance if the pseudo-op is <c>.lobytes</c>.</param>
        public void GenValues(PseudoArg[] args, double minValue, double maxValue, int setSize, bool isAddr, bool isRta, uint mask = 0)
        {
            for (int i = 0; i < args.Length; i++)
            {
                PseudoArg? arg = args[i];
                if (arg.Query() != null)
                {
                    _services.Output.AddUninitialized(setSize, false);
                }
                else if (!GenValue(arg.expr(), minValue, maxValue, setSize, isAddr, isRta, mask))
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Generates code output from strings in the argument list.
        /// </summary>
        /// <param name="directive">The pseudo-op name.</param>
        /// <param name="args">The parsed argument list.</param>
        public void GenStrings(int directive, PseudoList args)
        {
            var strBytes = new List<byte>();
            var uninitialized = 0;
            foreach (var arg in args.pseudoOpArg())
            {
                if (arg.Query() != null)
                {
                    uninitialized++;
                }
                else
                {
                    var str = _services.ExpressionVisitor.Visit(arg.expr());
                    if (str.IsPrimitiveType)
                    {
                        if (uninitialized > 0)
                        {
                            var uninitArray = new byte[uninitialized];
                            uninitArray.InitializeTo(_services.Output.UninitializedValue);
                            strBytes.AddRange(uninitArray);
                            uninitialized = 0;
                        }
                        if (str.IsString || str.DotNetType == TypeCode.Char)
                        {
                            strBytes.AddRange(_services.Encoding.GetBytes(str.ToString(true)));
                        }
                        else
                        {
                            strBytes.AddRange(_services.Output.ConvertToBytes(str.ToDouble()));
                        }
                    }
                    else if (str.IsDefined)
                    {
                        _services.Log.LogEntry(arg.expr(), Errors.ExpectedConstant);
                        return;
                    }
                    else if (_services.State.PassNeeded)
                    {
                        strBytes.Add(0);
                    }
                }
            }
            switch (directive)
            {
                case Sixty502DotNetParser.Cstring:
                    strBytes.Add(0x00);
                    break;
                case Sixty502DotNetParser.Pstring:
                    if (strBytes.Count > 255)
                    {
                        _services.Log.LogEntry(args,
                            "String expression exceeds the maximum length of \".pstring\" directive.");
                        return;
                    }
                    strBytes.Insert(0, Convert.ToByte(strBytes.Count));
                    break;
                case Sixty502DotNetParser.Lstring:
                case Sixty502DotNetParser.Nstring:
                    if (strBytes.Any(b => b > 0x7f))
                    {
                        _services.Log.LogEntry(args, "One or more elements in expression exceeds maximum value.");
                        return;
                    }
                    if (directive == Sixty502DotNetParser.Lstring)
                    {
                        strBytes = strBytes.Select(b => Convert.ToByte(b << 1)).ToList();
                        strBytes[^1] |= 1;
                    }
                    else
                    {
                        strBytes[^1] |= 0x80;
                    }
                    break;
                default:
                    break;
            }
            _services.Output.AddBytes(strBytes, true);
        }

        /// <summary>
        /// Generate double floating point values as CBM/MBF encoded output.
        /// </summary>
        /// <param name="directive">The pseudo-op.</param>
        /// <param name="args">The parsed argument list.</param>
        public void GenFloats(int directive, PseudoList args)
        {
            var packed = directive == Sixty502DotNetParser.Cbmfltp;
            var size = packed ? 5 : 6;
            foreach (var arg in args.pseudoOpArg())
            {
                if (arg.Query() != null)
                {
                    _services.Output.AddUninitialized(size, false);
                }
                else if (_services.ExpressionVisitor.TryGetArithmeticExpr(arg.expr(), -2.93783588E+39, 1.70141183E+38, out var dbl))
                {
                    _services.Output.AddBytes(CbmFloatFunction.ToBytes(dbl, packed), true);
                }
                else
                {
                    _services.Log.LogEntry(arg.expr(), Errors.IllegalQuantity);
                }
            }
        }

        /// <summary>
        /// Generate code from a file. This method implements the <c>.binary</c>
        /// pseudo-op.
        /// </summary>
        /// <param name="args">The parsed arguments.</param>
        public void GenFromFile(PseudoArg[] args)
        {
            var fileNameVal = GetExpr(args, 0);
            if (fileNameVal?.IsString == true)
            {
                var file = _binaryFiles.Get(fileNameVal.ToString());
                if (file == null)
                {
                    if (_services.State.CurrentPass != 0)
                    {
                        _services.Log.LogEntry(args[0], "File not found.");
                    }
                    return;
                }
                double offset = 0;
                double size = file.Data.Length;
                if (args.Length > 1)
                {
                    if (args.Length > 3)
                    {
                        _services.Log.LogEntry(args[3], Errors.UnexpectedExpression);
                        return;
                    }
                    if (!TryGetMathExpr(args, 1, short.MinValue, ushort.MaxValue, out offset) ||
                        (args.Length > 2 && !TryGetMathExpr(args, 2, short.MaxValue, ushort.MaxValue, out size)))
                    {
                        return;
                    }
                }
                _services.Output.AddBytes(file.Data.Skip((int)offset).Take((int)size));
                return;
            }
            _services.Log.LogEntry(args[0], Errors.StringExpected);
        }

        /// <summary>
        /// Fills the output by a given amount,or aligns the program counter
        /// to a given amount, and optionally fills the gap with a value. This
        /// method implements the <c>.align</c> and <c>.fill</c> pseudo-ops.
        /// </summary>
        /// <param name="directive">The pseudo-op.</param>
        /// <param name="args">The parsed aguments.</param>
        public void GenFills(int directive, PseudoArg[] args)
        {
            if (args.Length < 3)
            {
                if (TryGetMathExpr(args, 0, 1, ushort.MaxValue, out var alignval))
                {
                    try
                    {
                        if (args.Length == 2 && TryGetMathExpr(args, 1, int.MinValue, uint.MaxValue, out var fillval))
                        {
                            if (directive == Sixty502DotNetParser.Align)
                                _services.Output.Align((int)alignval, (int)fillval);
                            else
                                _services.Output.Fill((int)alignval, (int)fillval);
                        }
                        else
                        {
                            if (directive == Sixty502DotNetParser.Align)
                                _services.Output.Align((int)alignval);
                            else
                                _services.Output.Fill((int)alignval);
                        }
                    }
                    catch (ProgramOverflowException ex)
                    {
                        if (!_services.State.PassNeeded)
                        {
                            _services.Log.LogEntry(args[0], ex.Message);
                        }
                    }
                }
                return;
            }
            _services.Log.LogEntry(args[2], Errors.UnexpectedExpression);
        }

        /// <summary>
        /// Define a section from the operands in the <c>.dsection</c>
        /// pseudo-op.
        /// </summary>
        /// <param name="args">The parsed arguments.</param>
        public void DefineSection(PseudoArg[] args)
        {
            if (_services.State.CurrentPass > 0)
            {
                return;
            }
            var expressions = new Expr[args.Length];
            for (var i = 0; i < expressions.Length; i++)
            {
                if (args[i].Query() != null)
                {
                    _services.Log.LogEntry(args[i], Errors.UnexpectedExpression);
                    return;
                }
                expressions[i] = args[i].expr();
            }
            _ = SectionDefiner.Define((ParserRuleContext)args[0].Parent, expressions, _services);
        }

        /// <summary>
        /// Initializes the output, so that any gaps in assembly default to the
        /// value. This method implements the <c>.initmem</c> pseudo-op.
        /// </summary>
        /// <param name="args">The parsed arguments.</param>
        public void InitMem(PseudoArg[] args)
        {
            if (args.Length == 1 && TryGetMathExpr(args, 0, sbyte.MinValue, byte.MaxValue, out var init))
            {
                _services.Output.InitMemory((byte)((int)init & 0xFF));
                return;
            }
            _services.Log.LogEntry(args[^1], Errors.UnexpectedExpression);
        }

        /// <summary>
        /// Sets a mask for all generated code in the output. This method
        /// implements the <c>.eor</c> pseudo-op.
        /// </summary>
        /// <param name="args">The parsed arguments.</param>
        public void EorOutput(PseudoArg[] args)
        {
            if (args.Length == 1 && TryGetMathExpr(args, 0, sbyte.MinValue, byte.MaxValue, out var eor))
            {
                var eor_b = Convert.ToByte(eor);
                _services.Output.Transform = delegate (byte b)
                {
                    b ^= eor_b;
                    return b;
                };
                return;
            }
            _services.Log.LogEntry(args[^1], Errors.UnexpectedExpression);
        }
    }
}
