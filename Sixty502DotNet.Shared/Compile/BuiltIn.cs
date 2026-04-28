// Copyright (c) 2026 informedcitizenry
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using Sixty502DotNet.Shared.Arch;
using Sixty502DotNet.Shared.Eval;
using Sixty502DotNet.Shared.Eval.Function;
using Sixty502DotNet.Shared.Eval.Scope;
using Sixty502DotNet.Shared.Eval.String;

namespace Sixty502DotNet.Shared.Compile;

public static class BuiltIn
{
    public static void Define(AssemblyState state)
    {
        state.SymbolTable.DefineBuiltIn(Evaluator.CpuIdConst, new Value(CpuLookup.ReverseLookup(state.Cpu)));
        state.SymbolTable.DefineBuiltIn(Evaluator.FileConst, new Value("<unnamed>", TextEncodingType.Default));
        state.SymbolTable.DefineBuiltIn(Evaluator.LineConst, new Value(1));

        state.SymbolTable.DefineBuiltIn("INT8_MAX", new Value(sbyte.MaxValue));
        state.SymbolTable.DefineBuiltIn("INT8_MIN", new Value(sbyte.MinValue));
        state.SymbolTable.DefineBuiltIn("INT16_MAX", new Value(short.MaxValue));
        state.SymbolTable.DefineBuiltIn("INT16_MIN", new Value(short.MinValue));
        state.SymbolTable.DefineBuiltIn("INT24_MAX", new Value(Int24.MaxValue));
        state.SymbolTable.DefineBuiltIn("INT24_MIN", new Value(Int24.MinValue));
        state.SymbolTable.DefineBuiltIn("INT32_MAX", new Value(UInt24.MaxValue));
        state.SymbolTable.DefineBuiltIn("INT32_MIN", new Value(int.MinValue));
        state.SymbolTable.DefineBuiltIn("MATH_E", new Value(Math.E));
        state.SymbolTable.DefineBuiltIn("MATH_PI", new Value(Math.PI));
        state.SymbolTable.DefineBuiltIn("MATH_TAU", new Value(Math.Tau));
        state.SymbolTable.DefineBuiltIn("NaN", new Value(double.NaN));
        state.SymbolTable.DefineBuiltIn("UINT8_MAX", new Value(byte.MaxValue));
        state.SymbolTable.DefineBuiltIn("UINT8_MIN", new Value(byte.MinValue));
        state.SymbolTable.DefineBuiltIn("UINT16_MAX", new Value(ushort.MaxValue));
        state.SymbolTable.DefineBuiltIn("UINT16_MIN", new Value(ushort.MinValue));
        state.SymbolTable.DefineBuiltIn("UINT24_MAX", new Value(UInt24.MaxValue));
        state.SymbolTable.DefineBuiltIn("UINT24_MIN", new Value(UInt24.MinValue));
        state.SymbolTable.DefineBuiltIn("UINT32_MAX", new Value(uint.MaxValue));
        state.SymbolTable.DefineBuiltIn("UINT32_MIN", new Value(uint.MinValue));
        
        state.SymbolTable.DefineBuiltIn("abs", new Value(MathFunction.Abs));
        state.SymbolTable.DefineBuiltIn("acos", new Value(MathFunction.Acos));
        state.SymbolTable.DefineBuiltIn("acosh", new Value(MathFunction.Acosh));
        state.SymbolTable.DefineBuiltIn("asin", new Value(MathFunction.Asin));
        state.SymbolTable.DefineBuiltIn("atan", new Value(MathFunction.Atan));
        state.SymbolTable.DefineBuiltIn("atanh", new Value(MathFunction.Atanh));
        state.SymbolTable.DefineBuiltIn("byte",  new Value(MathFunction.Byte));
        state.SymbolTable.DefineBuiltIn("cbmflt", new Value(CbmFltFunction.Function));
        state.SymbolTable.DefineBuiltIn("cbrt", new Value(MathFunction.Cbrt));
        state.SymbolTable.DefineBuiltIn("ceil", new Value(MathFunction.Ceil));
        state.SymbolTable.DefineBuiltIn("char", new Value(new CharFunction(state.TextEncodingCollection)));
        state.SymbolTable.DefineBuiltIn("cos",  new Value(MathFunction.Cos));
        state.SymbolTable.DefineBuiltIn("cosh", new Value(MathFunction.Cosh));
        state.SymbolTable.DefineBuiltIn("deg", new Value(MathFunction.Deg));
        state.SymbolTable.DefineBuiltIn("dword", new Value(MathFunction.Dword));
        state.SymbolTable.DefineBuiltIn("exp", new Value(MathFunction.Exp));
        state.SymbolTable.DefineBuiltIn("floor", new Value(MathFunction.Floor));
        state.SymbolTable.DefineBuiltIn("format", new Value(new FormatFunction(state.TextEncodingCollection)));
        state.SymbolTable.DefineBuiltIn("frac", new Value(MathFunction.Frac));
        state.SymbolTable.DefineBuiltIn("hypot", new Value(MathFunction.Hypot));
        state.SymbolTable.DefineBuiltIn("ln", new Value(MathFunction.Ln));
        state.SymbolTable.DefineBuiltIn("log", new Value(MathFunction.Log));
        state.SymbolTable.DefineBuiltIn("log10", new Value(MathFunction.Log10));
        state.SymbolTable.DefineBuiltIn("log2", new Value(MathFunction.Log2));
        state.SymbolTable.DefineBuiltIn("long", new Value(MathFunction.Long));
        state.SymbolTable.DefineBuiltIn("max", new Value(MathFunction.Max));
        state.SymbolTable.DefineBuiltIn("min", new Value(MathFunction.Min));
        state.SymbolTable.DefineBuiltIn("peek", new Value(new PokePeekFunction(false, state.Output)));
        state.SymbolTable.DefineBuiltIn("poke", new Value(new PokePeekFunction(true, state.Output)));
        state.SymbolTable.DefineBuiltIn("pow", new Value(MathFunction.Pow));
        state.SymbolTable.DefineBuiltIn("rad", new Value(MathFunction.Rad));
        state.SymbolTable.DefineBuiltIn("random",  new Value(MathFunction.Random));
        state.SymbolTable.DefineBuiltIn("range", new Value(RangeFunction.Function));
        state.SymbolTable.DefineBuiltIn("round", new Value(MathFunction.Round));
        state.SymbolTable.DefineBuiltIn("section", new Value(new SectionFunction(state.Output)));
        state.SymbolTable.DefineBuiltIn("sgn", new Value(MathFunction.Sgn));
        state.SymbolTable.DefineBuiltIn("sin", new Value(MathFunction.Sin));
        state.SymbolTable.DefineBuiltIn("sqrt", new Value(MathFunction.Sqrt));
        state.SymbolTable.DefineBuiltIn("tan", new Value(MathFunction.Tan));
        state.SymbolTable.DefineBuiltIn("tanh", new Value(MathFunction.Tanh));
        state.SymbolTable.DefineBuiltIn("truncate", new Value(MathFunction.Truncate));
        state.SymbolTable.DefineBuiltIn("typeof", new Value(new TypeofFunction()));
        state.SymbolTable.DefineBuiltIn("word",  new Value(MathFunction.Word));
        state.SymbolTable.DefineBuiltIn
        (
            "cbmfltp", 
            new Value
            (
                new DeprecatedFunction
                (
                    "cbmfltp", 
                    state.Logger, 
                    "cbmflt", 
                    CbmFltFunction.Function
                )
            )
        );
        state.SymbolTable.DefineBuiltIn
        (
            "sizeof", 
            new Value
            (
                new DeprecatedFunction
                (
                    "sizeof", 
                    state.Logger, 
                    "expression.size", 
                    new SizeofFunction(state.TextEncodingCollection)
                )
            )
        );

        var addressMethods = new Namespace(state.SymbolTable.Root, state.Comparer);
        var arrayMethods = new Namespace(state.SymbolTable.Root, state.Comparer);
        var boolMethods = new Namespace(state.SymbolTable.Root, state.Comparer);
        var charMethods = new Namespace(state.SymbolTable.Root, state.Comparer);
        var dictMethods = new Namespace(state.SymbolTable.Root, state.Comparer);
        var intMethods = new Namespace(state.SymbolTable.Root, state.Comparer);
        var floatMethods = new Namespace(state.SymbolTable.Root, state.Comparer);
        var stringMethods = new Namespace(state.SymbolTable.Root, state.Comparer);
        var funcMethods = new Namespace(state.SymbolTable.Root, state.Comparer);
        var tupleMethods = new  Namespace(state.SymbolTable.Root, state.Comparer);
        var toStringMethod = new ToStringMethod(state.TextEncodingCollection);
        var sizeMethod = new SizeMethod(state.TextEncodingCollection);
        
        addressMethods.Env.DefineBuiltIn("toCbmFlt", new Value(Method.ToCbmFlt));
        addressMethods.Env.DefineBuiltIn("toString", new Value(toStringMethod));
        addressMethods.Env.DefineBuiltIn("size", new Value(sizeMethod));
        
        arrayMethods.Env.DefineBuiltIn("any", new Value(Method.Any));
        arrayMethods.Env.DefineBuiltIn("concat", new Value(Method.Concat));
        arrayMethods.Env.DefineBuiltIn("contains", new Value(Method.Contains));
        arrayMethods.Env.DefineBuiltIn("every", new Value(Method.Every));
        arrayMethods.Env.DefineBuiltIn("filter", new  Value(Method.Filter));
        arrayMethods.Env.DefineBuiltIn("indexOf", new Value(Method.IndexOf));
        arrayMethods.Env.DefineBuiltIn("intersect", new Value(Method.Intersect));
        arrayMethods.Env.DefineBuiltIn("len", new Value(Method.Len));
        arrayMethods.Env.DefineBuiltIn("map", new  Value(Method.Map));
        arrayMethods.Env.DefineBuiltIn("reduce", new Value(Method.Reduce));
        arrayMethods.Env.DefineBuiltIn("reverse", new Value(Method.Reverse));
        arrayMethods.Env.DefineBuiltIn("sort", new Value(new SortMethod(state)));
        arrayMethods.Env.DefineBuiltIn("toString", new Value(toStringMethod));
        arrayMethods.Env.DefineBuiltIn("toTuple", new Value(Method.ToTuple));
        arrayMethods.Env.DefineBuiltIn("size", new Value(sizeMethod));
        arrayMethods.Env.DefineBuiltIn("skip", new Value(Method.Skip));
        arrayMethods.Env.DefineBuiltIn("take", new Value(Method.Take));
        arrayMethods.Env.DefineBuiltIn("union", new Value(Method.Union));
        
        intMethods.Env.DefineBuiltIn("toCbmFlt", new Value(Method.ToCbmFlt));
        intMethods.Env.DefineBuiltIn("toString", new Value(toStringMethod));
        intMethods.Env.DefineBuiltIn("size", new Value(sizeMethod));
        
        floatMethods.Env.DefineBuiltIn("toCbmFlt", new Value(Method.ToCbmFlt));
        floatMethods.Env.DefineBuiltIn("toString", new Value(toStringMethod));
        floatMethods.Env.DefineBuiltIn("size", new Value(sizeMethod));
        
        boolMethods.Env.DefineBuiltIn("toString", new Value(toStringMethod));
        boolMethods.Env.DefineBuiltIn("size", new Value(sizeMethod));
        
        charMethods.Env.DefineBuiltIn("toCbmFlt", new Value(Method.ToCbmFlt));
        charMethods.Env.DefineBuiltIn("toString", new Value(toStringMethod));
        charMethods.Env.DefineBuiltIn("size", new Value(sizeMethod));
        
        funcMethods.Env.DefineBuiltIn("toString", new Value(toStringMethod));
        funcMethods.Env.DefineBuiltIn("size", new Value(sizeMethod));
        
        dictMethods.Env.DefineBuiltIn("any", new Value(Method.Any));
        dictMethods.Env.DefineBuiltIn("containsKey", new Value(Method.ContainsKey));
        dictMethods.Env.DefineBuiltIn("concat", new Value(Method.Concat));
        dictMethods.Env.DefineBuiltIn("keys", new  Value(Method.Keys));
        dictMethods.Env.DefineBuiltIn("len",  new Value(Method.Len));
        dictMethods.Env.DefineBuiltIn("toString", new Value(toStringMethod));
        dictMethods.Env.DefineBuiltIn("size", new Value(sizeMethod));
        
        stringMethods.Env.DefineBuiltIn("concat", new Value(Method.Concat));
        stringMethods.Env.DefineBuiltIn("contains",  new Value(Method.Contains));
        stringMethods.Env.DefineBuiltIn("every", new Value(Method.Every));
        stringMethods.Env.DefineBuiltIn("indexOf", new Value(Method.IndexOf));
        stringMethods.Env.DefineBuiltIn("substring", new Value(Method.Substring));
        stringMethods.Env.DefineBuiltIn("toLower", new Value(Method.ToLower));
        stringMethods.Env.DefineBuiltIn("toString", new Value(toStringMethod));
        stringMethods.Env.DefineBuiltIn("toUpper", new Value(Method.ToUpper));
        stringMethods.Env.DefineBuiltIn("len", new Value(Method.Len));
        stringMethods.Env.DefineBuiltIn("size", new Value(sizeMethod));
        stringMethods.Env.DefineBuiltIn("toArray", new Value(new ToArrayMethod(state.TextEncodingCollection)));
        
        tupleMethods.Env.DefineBuiltIn("any", new Value(Method.Any));
        tupleMethods.Env.DefineBuiltIn("concat", new Value(Method.Concat));
        tupleMethods.Env.DefineBuiltIn("every", new Value(Method.Every));
        tupleMethods.Env.DefineBuiltIn("len", new Value(Method.Len));
        tupleMethods.Env.DefineBuiltIn("size", new Value(sizeMethod));
        tupleMethods.Env.DefineBuiltIn("toString", new Value(toStringMethod));
        tupleMethods.Env.DefineBuiltIn("skip", new Value(Method.Skip));
        tupleMethods.Env.DefineBuiltIn("take", new Value(Method.Take));
        
        state.SymbolTable.DefineBuiltIn(TypeTag.Address.Name(), new Value(addressMethods));
        state.SymbolTable.DefineBuiltIn(TypeTag.Int.Name(), new Value(intMethods));
        state.SymbolTable.DefineBuiltIn(TypeTag.Float.Name(), new Value(floatMethods));
        state.SymbolTable.DefineBuiltIn(TypeTag.Char.Name(), new Value(charMethods));
        state.SymbolTable.DefineBuiltIn(TypeTag.Boolean.Name(), new Value(boolMethods));
        state.SymbolTable.DefineBuiltIn(TypeTag.String.Name(), new Value(stringMethods));
        state.SymbolTable.DefineBuiltIn(TypeTag.Array.Name(), new Value(arrayMethods));
        state.SymbolTable.DefineBuiltIn(TypeTag.Dictionary.Name(), new Value(dictMethods));
        state.SymbolTable.DefineBuiltIn(TypeTag.Function.Name(), new Value(funcMethods));
        state.SymbolTable.DefineBuiltIn(TypeTag.Tuple.Name(), new Value(tupleMethods));
    }
}