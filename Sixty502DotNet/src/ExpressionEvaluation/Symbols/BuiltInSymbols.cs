//-----------------------------------------------------------------------------
// Copyright (c) 2017-2022 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;

namespace Sixty502DotNet
{
    /// <summary>
    /// Defines functionality to initialize a symbol table with pre-defined
    /// constant value and function definition symbols.
    /// </summary>
    public static class BuiltInSymbols
    {
        private static readonly Random s_rng = new();

        /// <summary>
        /// Define a set of constant and function symbols.
        /// </summary>
        /// <param name="services">The shared <see cref="AssemblyServices"/>
        /// object.</param>
        public static void Define(AssemblyServices services)
        {
            var scope = services.Symbols.GlobalScope;
            scope.Define("abs",         MathFunction.OneArg(p => Math.Abs(p[0])));
            scope.Define("acos",        MathFunction.OneArg(p => Math.Acos(p[0])));
            scope.Define("acosh",       MathFunction.OneArg(p => Math.Acosh(p[0])));
            scope.Define("atan",        MathFunction.OneArg(p => Math.Atan(p[0])));
            scope.Define("atan2",       MathFunction.TwoArg(p => Math.Atan2(p[0], p[1])));
            scope.Define("atanh",       MathFunction.OneArg(p => Math.Atanh(p[0])));
            scope.Define("byte",        MathFunction.OneArg(p => (int)p[0] & 0xFF));
            scope.Define("cbmflt",      new CbmFloatFunction(services.Output, false));
            scope.Define("cbmfltp",     new CbmFloatFunction(services.Output, true));
            scope.Define("cbrt",        MathFunction.OneArg(p => Math.Cbrt(p[0])));
            scope.Define("char",        new CharFunction(services.Encoding));
            scope.Define("ceil",        MathFunction.OneArg(p => Math.Ceiling(p[0])));
            scope.Define("concat",      new ConcatFunction());
            scope.Define("cos",         MathFunction.OneArg(p => Math.Cos(p[0])));
            scope.Define("cosh",        MathFunction.OneArg(p => Math.Cosh(p[0])));
            scope.Define("CURRENT_PASS",new Constant("CURRENT_PASS", new Value(0)));
            scope.Define("deg",         MathFunction.OneArg(p => p[0] * 180.0 / Math.PI));
            scope.Define("dword",       MathFunction.OneArg(p => (int)p[0]));
            scope.Define("exp",         MathFunction.OneArg(p => Math.Exp(p[0])));
            scope.Define("false",       new Constant("false", new Value(false)));
            scope.Define("filter",      new FilterFunction());
            scope.Define("float",       new FloatFunction());
            scope.Define("floor",       MathFunction.OneArg(p => Math.Floor(p[0])));
            scope.Define("frac",        MathFunction.OneArg(p => p[0] - Convert.ToInt64(p[0])));
            scope.Define("format",      new FormatFunction());
            scope.Define("hypot",       MathFunction.TwoArg(p => Math.Sqrt(p[0] * p[0] + p[1] * p[1])));
            scope.Define("int",         new IntFunction());
            scope.Define("INT8_MAX",    new Constant("INT8_MAX", new Value(sbyte.MaxValue)));
            scope.Define("INT8_MIN",    new Constant("INT8_MIN", new Value(sbyte.MinValue)));
            scope.Define("INT16_MAX",   new Constant("INT16_MAX", new Value(short.MaxValue)));
            scope.Define("INT16_MIN",   new Constant("INT16_MIN", new Value(short.MaxValue)));
            scope.Define("INT24_MAX",   new Constant("INT24_MAX", new Value(Int24.MaxValue)));
            scope.Define("INT24_MIN",   new Constant("INT24_MIN", new Value(Int24.MinValue)));
            scope.Define("INT32_MAX",   new Constant("INT32_MAX", new Value(int.MaxValue)));
            scope.Define("INT32_MIN",   new Constant("INT32_MIN", new Value(int.MinValue)));
            scope.Define("len",         new LenFunction());
            scope.Define("ln",          MathFunction.OneArg(p => Math.Log(p[0])));
            scope.Define("log",         MathFunction.TwoArg(p => Math.Log(p[0], p[1])));
            scope.Define("log10",       MathFunction.OneArg(p => Math.Log10(p[0])));
            scope.Define("log2",        MathFunction.OneArg(p => Math.Log2(p[0])));
            scope.Define("long",        MathFunction.OneArg(p => (int)p[0] & 0xFFFFFF));
            scope.Define("MATH_E",      new Constant("MATH_E", new Value(Math.E)));
            scope.Define("MATH_PI",     new Constant("MATH_PI", new Value(Math.PI)));
            scope.Define("MATH_TAU",    new Constant("MATH_TAU", new Value(Math.Tau)));
            scope.Define("map",         new MapFunction());
            scope.Define("max",         MathFunction.TwoArg(p => Math.Max(p[0], p[1])));
            scope.Define("min",         MathFunction.TwoArg(p => Math.Min(p[0], p[1])));
            scope.Define("NaN",         new Constant("NaN", Value.NaN));
            scope.Define("peek",        new PeekFunction(services.Output));
            scope.Define("poke",        new PokeFunction(services.Output));
            scope.Define("pow",         MathFunction.TwoArg(p => Math.Pow(p[0], p[1])));
            scope.Define("rad",         MathFunction.OneArg(p => p[0] * Math.PI / 180.0));
            scope.Define("reduce",      new ReduceFunction());
            scope.Define("random",      MathFunction.TwoArg(p => s_rng.Next((int)p[0], (int)p[1])));
            scope.Define("round",       MathFunction.OneArg(p => Math.Round(p[0])));
            scope.Define("section",     new SectionFunction(services.Output));
            scope.Define("sgn",         MathFunction.OneArg(p => Math.Sign(p[0])));
            scope.Define("sizeof",      new SizeofFunction());
            scope.Define("sin",         MathFunction.OneArg(p => Math.Sin(p[0])));
            scope.Define("sinh",        MathFunction.OneArg(p => Math.Sinh(p[0])));
            scope.Define("sort",        new SortFunction());
            scope.Define("sqrt",        MathFunction.OneArg(p => Math.Sqrt(p[0])));
            scope.Define("tan",         MathFunction.OneArg(p => Math.Tan(p[0])));
            scope.Define("tanh",        MathFunction.OneArg(p => Math.Tanh(p[0])));
            scope.Define("true",        new Constant("true", new Value(true)));
            scope.Define("typeof",      new TypeofFunction());
            scope.Define("UINT8_MAX",   new Constant("UINT8_MAX", new Value(byte.MaxValue)));
            scope.Define("UINT8_MIN",   new Constant("UINT8_MIN", new Value(byte.MinValue)));
            scope.Define("UINT16_MAX",  new Constant("UINT16_MAX", new Value(ushort.MaxValue)));
            scope.Define("UINT16_MIN",  new Constant("UINT16_MIN", new Value(ushort.MaxValue)));
            scope.Define("UINT24_MAX",  new Constant("UINT24_MAX", new Value(UInt24.MaxValue)));
            scope.Define("UINT24_MIN",  new Constant("UINT24_MIN", new Value(UInt24.MinValue)));
            scope.Define("UINT32_MAX",  new Constant("UINT32_MAX", new Value(uint.MaxValue)));
            scope.Define("UINT32_MIN",  new Constant("UINT32_MIN", new Value(uint.MinValue)));
            scope.Define("union",       new UnionFunction());
            scope.Define("word",        MathFunction.OneArg(p => (int)p[0] & 0xFFFF));
        }
    }
}