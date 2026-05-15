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

using Sixty502DotNet.Shared.Error;
using Sixty502DotNet.Shared.Eval.Scope;
using Sixty502DotNet.Shared.Eval.String;
using Sixty502DotNet.Shared.Parse.Ast;

namespace Sixty502DotNet.Shared.Eval.Function;

using MethodDelegate = Func<IList<Value>, CallExpression, Value?>;


public class Method : IFunction
{
    private readonly MethodDelegate _methodDelegate;

    protected Method(int arity, MethodDelegate methodDelegate)
    {
        _methodDelegate = methodDelegate;
        Arity = arity;
    }

    public sealed class Instance(Method method, Value instance) : IFunction
    {
        public Value? Invoke(IList<Value> arguments, CallExpression callSite)
        {
            arguments.Insert(0, instance);
            return method.Invoke(arguments, callSite);
        }

        public int Arity => method.Arity;

        public int DefaultValues => method.DefaultValues;

        public bool IsVariant => method.IsVariant;
    }

    public Instance CreateInstance(Value instance) => new(this, instance);

    public virtual Value? Invoke(IList<Value> arguments, CallExpression callSite)
        => _methodDelegate(arguments, callSite);
    
    public int DefaultValues => 0;

    public int Arity { get; }

    public virtual bool IsVariant => false;

    public static readonly Method Any = new(1, (args, callSite) =>
    {
        var pred = args[1].AsFunction() 
                   ?? throw new TypeException(TypeTag.Function, args[1], callSite.Arguments[0]);
        if (pred.Arity != 1)
            throw new CompileException(CompileExceptionType.TypeMismatch, callSite.Arguments[0]);
        var predCall = new CallExpression(callSite.Arguments[0]);
        predCall.Arguments.Add(callSite.Arguments[0]);
        if (args[0].AsArray() is not { } array)
        {
            throw new CompileException(CompileExceptionType.TypeMismatch, callSite.Callee);
        }
        for (var i = 0; i < array.Count; i++)
        {
            var predVal = pred.Invoke(new List<Value> { array[i] }, predCall);
            if (predVal?.TypeTag != TypeTag.Boolean)
            {
                return predVal == null
                    ? null
                    : throw new TypeException(TypeTag.Boolean, predVal, predCall);
            }
            if (predVal.AsBoolean())
            {
                return new Value(true);
            }
        }
        return new Value(false);
    });

    public static Method Concat => new(1, (args, callSite) =>
    {
        EvalValues.CheckTypes(args[0], args[1], callSite.Arguments[0]);
        if (args[0].AsArray() is { } lArray)
        {
            if (args[1].AsArray() is not {} rArray || args[1].TypeTag != TypeTag.Array)
                throw new TypeException(TypeTag.Enumerable, args[1], callSite.Arguments[0]);
            return new Value(lArray.Concat(rArray).ToList(), args[0].TypeTag);
        }
        if (args[0].AsDictionary() is { } lDict && args[1].AsDictionary() is { } rDict)
        {
            var newDict = new Dictionary(lDict.Concat(rDict).ToDictionary());
            return new Value(newDict);
        }
        if (args[0].AsAsmString() is not { } lStr)
        {
            throw new TypeException(TypeTag.String, args[0], callSite.Callee);
        }
        if (args[1].AsAsmString() is not { } rStr)
        {
            throw new TypeException(TypeTag.String, args[1], callSite.Arguments[0]);
        }
        var newStr = new AsmString();
        newStr.Strings.AddRange(lStr.Strings);
        newStr.Strings.AddRange(rStr.Strings);
        return new Value(newStr);
    });

    public static Method Contains => new(1, (args, callSite) =>
    {
        if (args[0].AsArray() is { } arr)
        {
            EvalValues.CheckTypes(arr[0], args[1], callSite.Arguments[0]);
            return new Value(arr.Contains(args[1]));
        }
        if (args[0].AsAsmString() is not { } str)
        {
            throw new TypeException(TypeTag.Enumerable, args[0], callSite.Callee);
        }
        return args[1].TypeTag != TypeTag.Char
            ? throw new TypeException(TypeTag.Char, args[1], callSite.Arguments[0])
            : new Value(str.IndexOf(args[1].AsChar()) >= 0);
    });

    public static Method ContainsKey => new(1, (args, callSite) =>
    {
        if (args[0].AsDictionary() is not { } dict)
            throw new TypeException(TypeTag.Dictionary, args[0], callSite.Callee);
        EvalValues.CheckTypes(dict.Keys.First(), args[1], callSite.Arguments[0]);
        return new Value(dict.ContainsKey(args[1]));
    });

    public static Method Every => new(1, (args, callSite) =>
    {
        var pred = args[1].AsFunction() 
                   ?? throw new TypeException(TypeTag.Function, args[1], callSite.Arguments[0]);
        if (pred.Arity != 1)
            throw new CompileException(CompileExceptionType.TypeMismatch, callSite.Arguments[0]);
        var predCall = new CallExpression(callSite.Arguments[0]);
        predCall.Arguments.Add(callSite.Arguments[0]);
        if (args[0].AsArray() is not { } array)
        {
            throw new CompileException(CompileExceptionType.TypeMismatch, callSite.Callee);
        }
        for (var i = 0; i < array.Count; i++)
        {
            var predVal = pred.Invoke(new List<Value> { array[i] }, predCall);
            if (predVal?.TypeTag != TypeTag.Boolean)
            {
                return predVal == null
                    ? null
                    : throw new TypeException(TypeTag.Boolean, predVal, predCall);
            }
            if (!predVal.AsBoolean())
            {
                return new Value(false);
            }
        }
        return new Value(true);
    });

    public static Method Filter => new(1, (args, callSite) =>
    {
        var array = args[0].AsArray() ??
                    throw new TypeException(TypeTag.Array, args[0], callSite.Callee);

        var pred = args[1].AsFunction() ??
                   throw new TypeException(TypeTag.Function, args[1], callSite.Arguments[0]);

        if (pred.Arity != 1)
            throw new CompileException(CompileExceptionType.TypeMismatch, callSite.Arguments[0]);

        var filtered = new List<Value>();
        for (var i = 0; i < array.Count; i++)
        {
            var predCall = new CallExpression(callSite.Arguments[0]);
            predCall.Arguments.Add(callSite.Arguments[0]);
            var predVal = pred.Invoke(new List<Value> { array[i] }, predCall);
            if (predVal?.TypeTag != TypeTag.Boolean)
            {
                return predVal == null
                    ? null
                    : throw new TypeException(TypeTag.Boolean, predVal, predCall);
            }

            if (predVal.AsBoolean())
                filtered.Add(array[i]);
        }
        return new Value(filtered, TypeTag.Array);
    });

    public static Method IndexOf => new(1, (args, callSite) =>
    {
        if (args[0].AsArray() is { } lArr)
        {
            EvalValues.CheckTypes(lArr[0], args[1], callSite.Arguments[0]);
            return new Value(lArr.IndexOf(args[1]));
        }
        if (args[0].AsAsmString() is { } lStr && args[1].TypeTag == TypeTag.Char)
        {
            return new Value(lStr.IndexOf(args[1].AsChar()));
        }
        if (args[0].TypeTag != TypeTag.String)
            throw new TypeException(TypeTag.Enumerable, args[0], callSite.Callee);
        throw new TypeException(TypeTag.Char, args[1], callSite.Arguments[0]);
    });

    public static Method Intersect => new(1, (args, callSite) =>
    {
        EvalValues.CheckTypes(args[0], args[1], callSite.Arguments[0]);
        var lArr = args[0].AsArray() ?? new List<Value>();
        var rArr = args[1].AsArray() ?? new List<Value>();
        var intersection = lArr.Intersect(rArr).ToList();
        return new Value(intersection, TypeTag.Array);
    });

    public static Method Len => new(0, (args, _) => new Value(args[0].Length));

    public static Method Map => new(1, (args, callSite) =>
    {
        if (args[0].AsArray() is not {} array || args[0].TypeTag != TypeTag.Array)
            throw new TypeException(TypeTag.Enumerable, args[0], callSite.Callee);

        var predAst = callSite.Arguments[0];
        var pred = args[1].AsFunction() ??
                   throw new TypeException(TypeTag.Function, args[1], predAst);

        if (pred.Arity != 1)
            throw new CompileException(CompileExceptionType.TypeMismatch, predAst);

        var predCall = new CallExpression(callSite.Arguments[0]);
        predCall.Arguments.Add(predAst);
        var mapped = new List<Value>();
        for (var i = 0; i < array.Count; i++)
        {
            var v = pred.Invoke(new List<Value>{ array[i] }, predCall);
            if (v == null) return null;
            mapped.Add(v);
        }
        return new Value(mapped, TypeTag.Array);
    });

    public static readonly Method Reduce = new(1, (args, callSite) =>
    {
        if (args[0].AsArray() is not {} array || args[0].TypeTag != TypeTag.Array)
            throw new TypeException(TypeTag.Enumerable, args[0], callSite.Callee);

        var predAst = callSite.Arguments[0];
        var pred = args[1].AsFunction() ??
                   throw new TypeException(TypeTag.Function, args[1], predAst);

        if (pred.Arity != 2)
            throw new CompileException(CompileExceptionType.TypeMismatch, predAst);

        var reduction = new Value(array[0]);
        for (var i = 1; i < array.Count; i++)
        {
            var predArgs = new List<Value>
            {
                reduction,
                array[i]
            };
            var predCall = new CallExpression(callSite.Arguments[0]);
            predCall.Arguments.Add(predAst);
            var reduced = pred.Invoke(predArgs, predCall);
            if (reduced == null) return reduced;
            reduction = new Value(reduced);
        }
        return reduction;
    });

    public static Method Reverse => new(0, (args, callSite)
        => args[0].AsArray() is not { } arr 
        ? throw new TypeException(TypeTag.Enumerable, args[0], callSite.Callee) 
        : new Value(arr.Reverse().ToList(), TypeTag.Array));

    public static Method Skip => new(1, (args, callSite) =>
    {
        if (args[0].AsArray() is not { } arr)
            throw new TypeException(TypeTag.Enumerable, args[0], callSite.Callee);
        if (!args[1].IsNumber())
        {
            throw new TypeException(TypeTag.Float, args[1], callSite.Arguments[0]);
        }
        var amount = args[1].AsInt();
        if (amount < 0 || amount > arr.Count)
        {
            throw new CompileException(CompileExceptionType.IndexOutOfRange, callSite.Arguments[0]);
        }
        return new Value(arr.Skip((int)amount).ToList(), args[0].TypeTag);
    });

    public static Method Substring => new(2, (args, callSite) =>
    {
        if (args[0].AsAsmString() is not { } str)
        {
            throw new TypeException(TypeTag.String, args[0], callSite.Callee);
        }
        if (!args[1].IsNumber())
        {
            throw new TypeException(TypeTag.Float, args[1], callSite.Arguments[0]);
        }
        if (!args[2].IsNumber())
        {
            throw new TypeException(TypeTag.Float, args[2], callSite.Arguments[1]);
        }
        var start = args[1].AsInt();
        if (start < 0 || start >= str.Length)
        {
            throw new CompileException(CompileExceptionType.IndexOutOfRange, callSite.Arguments[0]);
        }
        var len = args[2].AsInt();
        if (len < 0 || start + len >= str.Length)
        {
            throw new CompileException(CompileExceptionType.IndexOutOfRange, callSite.Arguments[1]);
        }
        return new Value(str.Substring((int)start, (int)len));
    });
    
    public static Method Take => new(1, (args, callSite) =>
    {
        if (args[0].AsArray() is not { } arr)
            throw new TypeException(TypeTag.Enumerable, args[0], callSite.Callee);
        if (!args[1].IsNumber())
        {
            throw new TypeException(TypeTag.Float, args[1], callSite.Arguments[0]);
        }
        var amount = args[1].AsInt();
        if (amount < 0 || amount > arr.Count)
        {
            throw new CompileException(CompileExceptionType.IndexOutOfRange, callSite.Arguments[0]);
        }
        return new Value(arr.Take((int)amount).ToList(), args[0].TypeTag);
    });
    
    public static Method ToCbmFlt
        => new(0, (args, _) => 
            new Value(ValueHelper.GetCbmFloat(args[0].AsDouble())));

    public static Method ToLower => new(0, (args, _)
        => new Value(args[0].AsString().ToLower()));

    public static Method ToTuple => new(0, (args, callSite) =>
    {
        if (args[0].AsArray() is { } array)
        {
            return new Value([..array], TypeTag.Tuple);
        }
        throw new TypeException(TypeTag.Array, args[0], callSite.Callee);
    });
    
    public static Method ToUpper => new(0, (args, _)
        => new Value(args[0].AsString().ToUpper()));
    
    public static Method Keys => new(0, (args, callSite) =>
    {
        var keys = args[0].AsDictionary()?.Keys.ToList();
        return keys == null 
            ? throw new TypeException(TypeTag.Dictionary, args[0], callSite.Callee) 
            : new Value(keys, TypeTag.Array);
    });
    
    public static Method Union => new(1, (args, callSite) => 
    {
        EvalValues.CheckTypes(args[0], args[1], callSite.Arguments[0]);
        var arr1 = args[0].AsArray() ?? new List<Value>();
        var arr2 = args[1].AsArray() ?? new List<Value>();
        return new Value(arr1.Union(arr2).ToList(), TypeTag.Array);
    });
}

public sealed class SizeMethod(TextEncodingCollection encodings)
 : Method(0, (_, _) => null)
{
    public override Value Invoke(IList<Value> arguments, CallExpression callSite) 
        => new(arguments[0].Size(encodings));
}

public sealed class SortMethod(AssemblyState state)
    : Method(0, (args, callSite)
        =>
    {
        if (args[0].AsArray() is not { } array)
        {
            throw new TypeException(TypeTag.Array, args[0], callSite.Callee);
        }
        if (args.Count == 1)
        {
            return new Value(array.Order().ToList(), TypeTag.Array);
        }
        if (args[1].AsFunction() is not { Arity: 2 } sortFn)
        {
            throw new CompileException(CompileExceptionType.TypeMismatch, callSite.Arguments[0]);
        }
        var comparer = new ValueComparer(state, sortFn, callSite);
        var arr = array.ToArray();
        arr.Sort(comparer);
        return new Value(arr.ToList(), TypeTag.Array);
    })
{
    public override bool IsVariant => true;
}

public sealed class ToArrayMethod(TextEncodingCollection encodings)
: Method(0, (_, _) => null)
{
    public override Value Invoke(IList<Value> arguments, CallExpression callSite)
    {
        if (arguments[0].AsAsmString() is not { } str)
        {
            throw new TypeException(TypeTag.String, arguments[0], callSite.Callee);
        }
        var chars = str
            .ToArray(encodings.CurrentTextEncoding)
            .Select(c => new Value(c)).ToList();
        return new Value(chars, TypeTag.Array);
    }
}

public sealed class ToStringMethod(TextEncodingCollection encodings)
    : Method(0, (_, _) => null)
{
    public override Value Invoke(IList<Value> arguments, CallExpression callSite)
    {
        switch (arguments.Count)
        {
            case 1:
                return new Value(arguments[0].AsString());
            case > 2:
                throw new CompileException(CompileExceptionType.TooManyArguments, callSite);
        }

        try
        {
            if (!arguments[1].IsCharOrString())
            {
                throw new TypeException(TypeTag.String, arguments[1], callSite.Arguments[0]);
            }
            var frmt = arguments[1].AsString();
            var adjusted = $"{{0:{frmt}}}";
            var formatted = frmt.StartsWith("{0") && frmt.EndsWith('}')
                ? arguments[0].AsString()
                : string.Format(new ValueFormatter(encodings), adjusted, arguments[0]);
            return new Value(formatted, encodings.EncodingType);
        }
        catch (FormatException)
        {
            throw new CompileException(CompileExceptionType.InvalidFormatSpecifier, callSite.Arguments[0]);
        }
    }
    
    public override bool IsVariant => true;
}