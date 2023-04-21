//-----------------------------------------------------------------------------
// Copyright (c) 2017-2023 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

namespace Sixty502DotNet.Shared;

/// <summary>
/// Represents the runtime environment symbols for assembly runtime.
/// </summary>
public static class Environment
{
    private readonly static Random s_rng = new();

    static Environment()
    {
        ArrayType = new(TypeIds.Array, null);
        DictionaryType = new(TypeIds.Dictionary, null);
        FunctionType = new(TypeIds.Function, null);
        PrimitiveType = new(TypeIds.Primitive, null);
        StringType = new(TypeIds.String, null);
        TupleType = new(TypeIds.Tuple, null);
    }

    private static void DefineConstant(string name, double value, IScope scope)
    {
        DefineConstant(name, new NumericValue(value, true), scope);
    }

    private static void MathFunction(string name, Func<double[], double> impl, int argCount, IScope scope)
    {
        DefineConstant(name, new MathFunction(name, impl, argCount), scope);
    }

    private static void BuiltInFunction(string name, BuiltInFunctionObject function, IScope scope)
    {
        DefineConstant(name, function, scope);
    }

    /// <summary>
    /// Define a constant symbol.
    /// </summary>
    /// <param name="name">The symbol name.</param>
    /// <param name="value">The value bound to the constant symbol.</param>
    /// <param name="scope">The symbol's scope.</param>
    /// <param name="isBuiltIn">Indicate whether the symbol is built-in.</param>
    public static void DefineConstant(string name, ValueBase value, IScope scope, bool isBuiltIn = true)
    {
        Constant constant = new(name, value, scope, isBuiltIn);
        scope.Define(name, constant);
    }

    /// <summary>
    /// Set up the assembly environment, including defining built-in symbols.
    /// </summary>
    /// <param name="services">The shared <see cref="AssemblyServices"/> for
    /// the assembler runtime.</param>
    public static void Init(AssemblyServices services)
    {
        IScope scope = services.State.Symbols.GlobalScope;
        DefineConstant("CURRENT_PASS", 0, scope);
        DefineConstant("INT8_MAX", sbyte.MaxValue, scope);
        DefineConstant("INT8_MIN", sbyte.MinValue, scope);
        DefineConstant("INT16_MAX", short.MaxValue, scope);
        DefineConstant("INT16_MIN", short.MinValue, scope);
        DefineConstant("INT24_MAX", Int24.MaxValue, scope);
        DefineConstant("INT24_MIN", Int24.MinValue, scope);
        DefineConstant("INT32_MAX", int.MaxValue, scope);
        DefineConstant("INT32_MIN", int.MinValue, scope);
        DefineConstant("MATH_E", Math.E, scope);
        DefineConstant("MATH_PI", Math.PI, scope);
        DefineConstant("MATH_TAU", Math.Tau, scope);
        DefineConstant("UINT8_MAX", byte.MaxValue, scope);
        DefineConstant("UINT8_MIN", byte.MinValue, scope);
        DefineConstant("UINT16_MAX", ushort.MaxValue, scope);
        DefineConstant("UINT16_MIN", ushort.MinValue, scope);
        DefineConstant("UINT24_MAX", UInt24.MaxValue, scope);
        DefineConstant("UINT24_MIN", UInt24.MinValue, scope);
        DefineConstant("UINT32_MAX", uint.MaxValue, scope);
        DefineConstant("UINT32_MIN", uint.MinValue, scope);

        BuiltInFunction("binary", new BinaryFunction(), scope);
        BuiltInFunction("cbmflt", new CbmFloatFunction(services.State.Output, false), scope);
        BuiltInFunction("cbmfltp", new CbmFloatFunction(services.State.Output, true), scope);
        BuiltInFunction("char", new CharFunction(services.Encoding), scope);
        BuiltInFunction("float", new FloatFunction(), scope);
        BuiltInFunction("format", new FormatFunction(), scope);
        BuiltInFunction("int", new IntFunction(), scope);
        BuiltInFunction("peek", new PeekFunction(services.State.Output), scope);
        BuiltInFunction("poke", new PokeFunction(services.State.Output), scope);
        BuiltInFunction("range", new RangeFunction(), scope);
        BuiltInFunction("section", new SectionFunction(services), scope);
        BuiltInFunction("sizeof", new SizeofFunction(), scope);
        BuiltInFunction("typeof", new TypeofFunction(), scope);

        MathFunction("abs", p => Math.Abs(p[0]), 1, scope);
        MathFunction("atan", p => Math.Atan(p[0]), 1, scope);
        MathFunction("atan2", p => Math.Atan2(p[0], p[1]), 2, scope);
        MathFunction("atanh", p => Math.Atanh(p[0]), 1, scope);
        MathFunction("byte", p => (int)p[0] & 0xff, 1, scope);
        MathFunction("cbrt", p => Math.Cbrt(p[0]), 1, scope);
        MathFunction("ceiling", p => Math.Ceiling(p[0]), 1, scope);
        MathFunction("cos", p => Math.Cos(p[0]), 1, scope);
        MathFunction("cosh", p => Math.Cosh(p[0]), 1, scope);
        MathFunction("deg", p => p[0] * 180.0 / Math.PI, 1, scope);
        MathFunction("dword", p => (long)p[0] & 0xffffffff, 1, scope);
        MathFunction("exp", p => Math.Exp(p[0]), 1, scope);
        MathFunction("floor", p => Math.Floor(p[0]), 1, scope);
        MathFunction("frac", p => p[0] - Convert.ToInt64(p[0]), 1, scope);
        MathFunction("hypot", p => Math.Sqrt(p[0] * p[0] + p[1] * p[1]), 2, scope);
        MathFunction("ln", p => Math.Log(p[0]), 1, scope);
        MathFunction("log", p => Math.Log(p[0], p[1]), 2, scope);
        MathFunction("log2", p => Math.Log2(p[0]), 1, scope);
        MathFunction("log10", p => Math.Log10(p[0]), 1, scope);
        MathFunction("long", p => (int)p[0] & 0xffffff, 1, scope);
        MathFunction("max", p => Math.Max(p[0], p[1]), 2, scope);
        MathFunction("min", p => Math.Min(p[0], p[1]), 2, scope);
        MathFunction("pow", p => Math.Pow(p[0], p[1]), 2, scope);
        MathFunction("rad", p => p[0] * Math.PI / 180.0, 1, scope);
        MathFunction("random", p => s_rng.Next((int)p[0], (int)p[1]), 2, scope);
        MathFunction("round", p => Math.Round(p[0]), 1, scope);
        MathFunction("sgn", p => Math.Sign(p[0]), 1, scope);
        MathFunction("sin", p => Math.Sin(p[0]), 1, scope);
        MathFunction("sinh", p => Math.Sinh(p[0]), 1, scope);
        MathFunction("sqrt", p => Math.Sqrt(p[0]), 1, scope);
        MathFunction("tan", p => Math.Tan(p[0]), 1, scope);
        MathFunction("tanh", p => Math.Tanh(p[0]), 1, scope);
        MathFunction("word", p => (int)p[0] & 0xffff, 1, scope);

        scope.Define(TypeIds.Array, ArrayType);
        BuiltInFunction("concat", new ConcatMethod(services), ArrayType);
        BuiltInFunction("contains", new ContainsMethod(services), ArrayType);
        BuiltInFunction("every", new EveryMethod(services), ArrayType);
        BuiltInFunction("filter", new FilterMethod(services), ArrayType);
        BuiltInFunction("indexOf", new IndexOfMethod(services), ArrayType);
        BuiltInFunction("intersect", new IntersectMethod(services), ArrayType);
        BuiltInFunction("len", new LenMethod(services), ArrayType);
        BuiltInFunction("map", new MapMethod(services), ArrayType);
        BuiltInFunction("reduce", new ReduceMethod(services), ArrayType);
        BuiltInFunction("reverse", new ReverseMethod(services), ArrayType);
        BuiltInFunction("size", new SizeMethod(services), ArrayType);
        BuiltInFunction("skip", new SkipMethod(services), ArrayType);
        BuiltInFunction("some", new SomeMethod(services), ArrayType);
        BuiltInFunction("sort", new SortMethod(services), ArrayType);
        BuiltInFunction("take", new TakeMethod(services), ArrayType);
        BuiltInFunction("toString", new ToStringMethod(services), ArrayType);
        BuiltInFunction("toTuple", new ToTupleMethod(services), ArrayType);
        BuiltInFunction("union", new UnionMethod(services), ArrayType);

        scope.Define(TypeIds.Dictionary, DictionaryType);
        BuiltInFunction("concat", new ConcatMethod(services), DictionaryType);
        BuiltInFunction("containsKey", new ContainsKeyMethod(services), DictionaryType);
        BuiltInFunction("keys", new KeysMethod(services), DictionaryType);
        BuiltInFunction("len", new LenMethod(services), DictionaryType);
        BuiltInFunction("size", new SizeMethod(services), DictionaryType);
        BuiltInFunction("toString", new ToStringMethod(services), DictionaryType);

        scope.Define(TypeIds.String, StringType);
        BuiltInFunction("contains", new ContainsMethod(services), StringType);
        BuiltInFunction("concat", new ConcatMethod(services), StringType);
        BuiltInFunction("indexOf", new IndexOfMethod(services), StringType);
        BuiltInFunction("len", new LenMethod(services), StringType);
        BuiltInFunction("size", new SizeMethod(services), StringType);
        BuiltInFunction("substring", new SubstringMethod(services), StringType);
        BuiltInFunction("toArray", new ToArrayMethod(services), StringType);
        BuiltInFunction("toLower", new ToLowerMethod(services), StringType);
        BuiltInFunction("toString", new ToStringMethod(services), StringType);
        BuiltInFunction("toUpper", new ToUpperMethod(services), StringType);

        scope.Define(TypeIds.Tuple, TupleType);
        BuiltInFunction("contains", new ContainsMethod(services), TupleType);
        BuiltInFunction("len", new LenMethod(services), TupleType);
        BuiltInFunction("size", new SizeMethod(services), TupleType);
        BuiltInFunction("skip", new SkipMethod(services), TupleType);
        BuiltInFunction("take", new TakeMethod(services), TupleType);
        BuiltInFunction("toArray", new ToArrayMethod(services), TupleType);
        BuiltInFunction("toString", new ToStringMethod(services), TupleType);

        scope.Define(TypeIds.Primitive, PrimitiveType);
        BuiltInFunction("toCbmFlt", new ToCbmFltMethod(services, false), PrimitiveType);
        BuiltInFunction("toCbmFltp", new ToCbmFltMethod(services, true), PrimitiveType);
        BuiltInFunction("size", new SizeMethod(services), PrimitiveType);
        BuiltInFunction("toString", new ToStringMethod(services), PrimitiveType);

        scope.Define(TypeIds.Function, FunctionType);
        BuiltInFunction("toString", new ToStringMethod(services), FunctionType);

        // set architecture specific encodings
        services.Encoding.SelectEncoding("\"petscii\"");
        services.Encoding.Map("az", 'A');
        services.Encoding.Map("AZ", 0xc1);
        services.Encoding.Map('£', '\\');
        services.Encoding.Map('↑', '^');
        services.Encoding.Map('←', '_');
        services.Encoding.Map('▌', 0xa1);
        services.Encoding.Map('▄', 0xa2);
        services.Encoding.Map('▔', 0xa3);
        services.Encoding.Map('▁', 0xa4);
        services.Encoding.Map('▏', 0xa5);
        services.Encoding.Map('▒', 0xa6);
        services.Encoding.Map('▕', 0xa7);
        services.Encoding.Map('◤', 0xa9);
        services.Encoding.Map('├', 0xab);
        services.Encoding.Map('└', 0xad);
        services.Encoding.Map('┐', 0xae);
        services.Encoding.Map('▂', 0xaf);
        services.Encoding.Map('┌', 0xb0);
        services.Encoding.Map('┴', 0xb1);
        services.Encoding.Map('┬', 0xb2);
        services.Encoding.Map('┤', 0xb3);
        services.Encoding.Map('▎', 0xb4);
        services.Encoding.Map('▍', 0xb5);
        services.Encoding.Map('▃', 0xb9);
        services.Encoding.Map('✓', 0xba);
        services.Encoding.Map('┘', 0xbd);
        services.Encoding.Map('━', 0xc0);
        services.Encoding.Map('♠', 0xc1);
        services.Encoding.Map('│', 0xc2);
        services.Encoding.Map('╮', 0xc9);
        services.Encoding.Map('╰', 0xca);
        services.Encoding.Map('╯', 0xcb);
        services.Encoding.Map('╲', 0xcd);
        services.Encoding.Map('╱', 0xce);
        services.Encoding.Map('●', 0xd1);
        services.Encoding.Map('♥', 0xd3);
        services.Encoding.Map('╭', 0xd5);
        services.Encoding.Map('╳', 0xd6);
        services.Encoding.Map('○', 0xd7);
        services.Encoding.Map('♣', 0xd8);
        services.Encoding.Map('♦', 0xda);
        services.Encoding.Map('┼', 0xdb);
        services.Encoding.Map('π', 0xde);
        services.Encoding.Map('◥', 0xdf);
        services.Encoding.Map("\x80\xff", 0x80);

        services.Encoding.SelectEncoding("\"cbmscreen\"");
        services.Encoding.Map("@Z", '\0');
        services.Encoding.Map("az", 'A');
        services.Encoding.Map('£', '\\');
        services.Encoding.Map('π', '^'); // π is $5e in unshifted
        services.Encoding.Map('↑', '^'); // ↑ is $5e in shifted
        services.Encoding.Map('←', '_');
        services.Encoding.Map('▌', '`');
        services.Encoding.Map('▄', 'a');
        services.Encoding.Map('▔', 'b');
        services.Encoding.Map('▁', 'c');
        services.Encoding.Map('▏', 'd');
        services.Encoding.Map('▒', 'e');
        services.Encoding.Map('▕', 'f');
        services.Encoding.Map('◤', 'i');
        services.Encoding.Map('├', 'k');
        services.Encoding.Map('└', 'm');
        services.Encoding.Map('┐', 'n');
        services.Encoding.Map('▂', 'o');
        services.Encoding.Map('┌', 'p');
        services.Encoding.Map('┴', 'q');
        services.Encoding.Map('┬', 'r');
        services.Encoding.Map('┤', 's');
        services.Encoding.Map('▎', 't');
        services.Encoding.Map('▍', 'u');
        services.Encoding.Map('▃', 'y');
        services.Encoding.Map('✓', 'z');
        services.Encoding.Map('┘', '}');
        services.Encoding.Map('━', '@');
        services.Encoding.Map('♠', 'A');
        services.Encoding.Map('│', 'B');
        services.Encoding.Map('╮', 'I');
        services.Encoding.Map('╰', 'J');
        services.Encoding.Map('╯', 'K');
        services.Encoding.Map('╲', 'M');
        services.Encoding.Map('╱', 'N');
        services.Encoding.Map('●', 'Q');
        services.Encoding.Map('♥', 'S');
        services.Encoding.Map('╭', 'U');
        services.Encoding.Map('╳', 'V');
        services.Encoding.Map('○', 'W');
        services.Encoding.Map('♣', 'X');
        services.Encoding.Map('♦', 'Z');
        services.Encoding.Map('┼', '[');
        services.Encoding.Map('◥', '_');
        services.Encoding.Map("\x80\xff", 0x80);

        services.Encoding.SelectEncoding("\"atascreen\"");
        services.Encoding.Map(" _", '\0');
        services.Encoding.Map("\x80\xff", 0x80);

        services.Encoding.SelectDefaultEncoding();
    }

    /// <summary>
    /// Get the type scope for the array type.
    /// </summary>
    public static Prototype ArrayType { get; }

    /// <summary>
    /// Get the type scope for the dictionary type.
    /// </summary>
    public static Prototype DictionaryType { get; }

    /// <summary>
    /// Get the type scope for a callable object type.
    /// </summary>
    public static Prototype FunctionType { get; }

    /// <summary>
    /// Get the type scope for primitive types.
    /// </summary>
    public static Prototype PrimitiveType { get; }

    /// <summary>
    /// Get the type scope for the string type.
    /// </summary>
    public static Prototype StringType { get; }

    /// <summary>
    /// Get the type scope for the tuple type.
    /// </summary>
    public static Prototype TupleType { get; }
}

