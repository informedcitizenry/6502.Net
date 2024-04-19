//-----------------------------------------------------------------------------
// Copyright (c) 2017-2024 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System.Text;

namespace Sixty502DotNet.Shared;

/// <summary>
/// Represents an implementation of method for a type instance. This class
/// must be inherited.
/// </summary>
public abstract class TypeMethodBase : BuiltInFunctionObject
{
    /// <summary>
    /// Construct a new instance of a <see cref="TypeMethodBase"/>.
    /// </summary>
    /// <param name="name">The method name.</param>
    /// <param name="services">The shared <see cref="AssemblyServices"/> for
    /// the assembler runtime.</param>
    /// <param name="arity">The method arity.</param>
    protected TypeMethodBase(string name, AssemblyServices services, int arity)
        : base(name, arity)
    {
        Services = services;
    }

    /// <summary>
    /// The shared <see cref="AssemblyServices"/> for the assembler runtime.
    /// </summary>
    protected AssemblyServices Services { get; init; }
}

/// <summary>
/// Represents a method to concatenate the instance object with another.
/// </summary>
public sealed class ConcatMethod : TypeMethodBase
{
    /// <summary>
    /// Construct a new instance of the <see cref="ConcatMethod"/>.
    /// </summary>
    /// <param name="services">The shared <see cref="AssemblyServices"/> for
    /// the assembler runtime.</param>
    public ConcatMethod(AssemblyServices services)
        : base("concat", services, 2)
    {

    }

    protected override ValueBase OnInvoke(SyntaxParser.ExpressionCallContext callSite, ArrayValue parameters)
    {
        if (!parameters[0].TypeCompatible(parameters[1]))
        {
            throw new TypeMismatchError(callSite.exprList().expr()[1]);
        }
        try
        {
            return parameters[0].AddWith(parameters[1]);
        }
        catch (ArgumentException)
        {
            throw new Error(callSite.exprList(), "Could not concatenate collections");
        }
    }
}

/// <summary>
/// Represents a method to test if an instance object contains a specified value.
/// </summary>
public sealed class ContainsMethod : TypeMethodBase
{
    /// <summary>
    /// Construct a new instance of the <see cref="ContainsMethod"/>.
    /// </summary>
    /// <param name="services">The shared <see cref="AssemblyServices"/> for
    /// the assembler runtime.</param>
    public ContainsMethod(AssemblyServices services)
        : base("contains", services, 2)
    {

    }

    protected override ValueBase OnInvoke(SyntaxParser.ExpressionCallContext callSite, ArrayValue parameters)
    {
        try
        {
            return new BoolValue(parameters[0].Contains(parameters[1]));
        }
        catch (TypeMismatchError)
        {
            throw new TypeMismatchError(callSite);
        }
    }
}

/// <summary>
/// Represents a method to test if the instance dictionary type contains a
/// specified key.
/// </summary>
public sealed class ContainsKeyMethod : TypeMethodBase
{
    /// <summary>
    /// Construct a new instance of the <see cref="ContainsKeyMethod"/>.
    /// </summary>
    /// <param name="services">The shared <see cref="AssemblyServices"/> for
    /// the assembler runtime.</param>
    public ContainsKeyMethod(AssemblyServices services)
        : base("containsKey", services, 2)
    {

    }

    protected override ValueBase OnInvoke(SyntaxParser.ExpressionCallContext callSite, ArrayValue parameters)
    {
        try
        {
            return new BoolValue(parameters[0].ContainsKey(parameters[1]));
        }
        catch (TypeMismatchError)
        {
            throw new TypeMismatchError(callSite.Start);
        }
    }
}

/// <summary>
/// Represents a method to test if every element in the instance object passes
/// the specified predicate's test.
/// </summary>
public sealed class EveryMethod : TypeMethodBase
{
    /// <summary>
    /// Construct a new instance of the <see cref="EveryMethod"/>.
    /// </summary>
    /// <param name="services">The shared <see cref="AssemblyServices"/> for
    /// assembler runtime.</param>
    public EveryMethod(AssemblyServices services)
        : base("every", services, 2)
    {

    }

    protected override ValueBase OnInvoke(SyntaxParser.ExpressionCallContext callSite, ArrayValue parameters)
    {
        ArrayValue array = (ArrayValue)parameters[0];
        if (parameters[1] is not FunctionObject funcObj)
        {
            throw new Error(callSite.exprList().expr()[0], "Parameter must be a predicate");
        }
        for (int i = 0; i < array.Count; i++)
        {
            ArrayValue callBackParameters = new(new List<ValueBase> { array[i] });
            ValueBase? predicateResult = Services.Evaluator.Invoke(callSite, funcObj, callBackParameters) ?? throw new Error(callSite.Start, "User predicate must return a boolean value");
            if (!predicateResult.IsDefined)
            {
                return new UndefinedValue();
            }
            if (!predicateResult.AsBool())
            {
                return new BoolValue(false);
            }
        }
        return new BoolValue(true);
    }
}

/// <summary>
/// Represents a method that filter's the instance object's members according
/// to the given predicate.
/// </summary>
public sealed class FilterMethod : TypeMethodBase
{
    /// <summary>
    /// Construct a new instance of the <see cref="FilterMethod"/>.
    /// </summary>
    /// <param name="services">The shared <see cref="AssemblyServices"/> for
    /// assembler runtime.</param>
    public FilterMethod(AssemblyServices services)
        : base("filter", services, 2)
    {

    }

    protected override ValueBase OnInvoke(SyntaxParser.ExpressionCallContext callSite, ArrayValue parameters)
    {
        ArrayValue array = (ArrayValue)parameters[0];
        ArrayValue filtered = new();
        if (parameters[1] is not FunctionObject funcObj)
        {
            throw new Error(callSite.exprList().expr()[0], "Parameter must be a predicate");
        }
        for (int i = 0; i < array.Count; i++)
        {
            ArrayValue callBackParameters = new(new List<ValueBase> { array[i] });
            ValueBase? predicateResult = Services.Evaluator.Invoke(callSite, funcObj, callBackParameters) ?? throw new Error(callSite.Start, "User predicate must return a boolean value");
            if (!predicateResult.IsDefined)
            {
                return new UndefinedValue();
            }
            if (predicateResult.AsBool())
            {
                filtered.Add(array[i]);
            }
        }
        return filtered;
    }
}

/// <summary>
/// Represents a method to retrieve the index of an element in the instance
/// object.
/// </summary>
public sealed class IndexOfMethod : TypeMethodBase
{
    /// <summary>
    /// Construct a new instance of the <see cref="IndexOfMethod"/>.
    /// </summary>
    /// <param name="services">The shared <see cref="AssemblyServices"/> for
    /// assembler runtime.</param>
    public IndexOfMethod(AssemblyServices services)
        : base("indexOf", services, 2)
    {

    }

    protected override ValueBase OnInvoke(SyntaxParser.ExpressionCallContext callSite, ArrayValue parameters)
    {
        return new NumericValue(parameters[0].ToList().IndexOf(parameters[1]));
    }
}

/// <summary>
/// Represents an implementation of a method that retrieves the intersection of
/// two arrays.
/// </summary>
public sealed class IntersectMethod : TypeMethodBase
{
    /// <summary>
    /// Construct a new instance of an <see cref="IntersectMethod"/>.
    /// </summary>
    /// <param name="services">The shared <see cref="AssemblyServices"/> for
    /// assembler runtime.</param>
    public IntersectMethod(AssemblyServices services)
        : base("intersect", services, 2)
    {

    }

    protected override ValueBase OnInvoke(SyntaxParser.ExpressionCallContext callSite, ArrayValue parameters)
    {
        if (!parameters[0].TypeCompatible(parameters[1]))
        {
            throw new TypeMismatchError(callSite.Start);
        }
        HashSet<ValueBase> set1 = ((ArrayValue)parameters[0]).ToHashSet();
        set1.IntersectWith((ArrayValue)parameters[1]);
        return new ArrayValue(set1.ToList());
    }
}

/// <summary>
/// Represents a method to retrieve the instance dictionary object's keys.
/// </summary>
public sealed class KeysMethod : TypeMethodBase
{
    /// <summary>
    /// Construct a new instance of the <see cref="KeysMethod"/>.
    /// </summary>
    /// <param name="services">The shared <see cref="AssemblyServices"/> for
    /// assembler runtime.</param>
    public KeysMethod(AssemblyServices services)
        : base("keys", services, 1)
    {

    }

    protected override ValueBase OnInvoke(SyntaxParser.ExpressionCallContext callSite, ArrayValue parameters)
    {
        DictionaryValue thisDict = (DictionaryValue)parameters[0];
        return new ArrayValue(thisDict.Keys.ToList());
    }
}

/// <summary>
/// Represents a method to retrieve the length (number of elements) of the
/// instance object.
/// </summary>
public sealed class LenMethod : TypeMethodBase
{
    /// <summary>
    /// Construct a new instance of the <see cref="LenMethod"/>.
    /// </summary>
    /// <param name="services">The shared <see cref="AssemblyServices"/> for
    /// assembler runtime.</param>
    public LenMethod(AssemblyServices services)
        : base("len", services, 1)
    {

    }

    protected override ValueBase OnInvoke(SyntaxParser.ExpressionCallContext callSite, ArrayValue parameters)
    {
        return new NumericValue(parameters[0].Count);
    }
}

/// <summary>
/// Represents a method to transform each element in the instance object by
/// the given callback.
/// </summary>
public sealed class MapMethod : TypeMethodBase
{
    /// <summary>
    /// Construct a new instance of the <see cref="MapMethod"/>.
    /// </summary>
    /// <param name="services">The shared <see cref="AssemblyServices"/> for
    /// assembler runtime.</param>
    public MapMethod(AssemblyServices services)
        : base("map", services, 2)
    {

    }

    protected override ValueBase OnInvoke(SyntaxParser.ExpressionCallContext callSite, ArrayValue parameters)
    {
        ArrayValue array = (ArrayValue)parameters[0];
        ArrayValue mapped = new();
        if (parameters[1] is not FunctionObject funcObj)
        {
            throw new Error(callSite.exprList().expr()[1], "Parameter must be a function expression");
        }
        for (int i = 0; i < array.Count; i++)
        {
            ArrayValue callBackParameters = new(new List<ValueBase> { array[i] });
            ValueBase? transformed = Services.Evaluator.Invoke(callSite, funcObj, callBackParameters) ?? throw new Error(callSite.Start, "Transform function does not return a value");
            if (!transformed.IsDefined)
            {
                return new UndefinedValue();
            }
            mapped.Add(transformed);
            if (!mapped.ElementsSameType)
            {
                throw new TypeMismatchError(callSite.Start);
            }
        }
        return mapped;
    }
}

/// <summary>
/// Represents a method that reduces the instance object's elements to a value
/// by a transform function.
/// </summary>
public sealed class ReduceMethod : TypeMethodBase
{
    /// <summary>
    /// Construct a new instance of the <see cref="ReduceMethod"/>.
    /// </summary>
    /// <param name="services">The shared <see cref="AssemblyServices"/> for
    /// assembler runtime.</param>
    public ReduceMethod(AssemblyServices services)
        : base("reduce", services, 2)
    {

    }

    protected override ValueBase OnInvoke(SyntaxParser.ExpressionCallContext callSite, ArrayValue parameters)
    {
        ArrayValue array = (ArrayValue)parameters[0];
        ValueBase reduction = array[0].AsCopy();
        if (parameters[1] is not FunctionObject funcObj)
        {
            throw new Error(callSite.exprList().expr()[1], "Parameter must be a function expression");
        }
        for (int i = 1; i < array.Count; i++)
        {
            ArrayValue callBackParameters = new(new List<ValueBase>
            {
                reduction,
                array[i]
            });
            ValueBase? reduced = Services.Evaluator.Invoke(callSite, funcObj, callBackParameters) ?? throw new Error(callSite.Start, "Transform function does not return a value");
            if (!reduced.IsDefined)
            {
                return new UndefinedValue();
            }
            reduction.SetAs(reduced);
        }
        return reduction;
    }
}

/// <summary>
/// Represents a method that reverses the instance object's elements.
/// </summary>
public sealed class ReverseMethod : TypeMethodBase
{
    /// <summary>
    /// Construct a new instance of the <see cref="ReverseMethod"/>.
    /// </summary>
    /// <param name="services">The shared <see cref="AssemblyServices"/> for
    /// assembler runtime.</param>
    public ReverseMethod(AssemblyServices services)
        : base("reverse", services, 1)
    {

    }

    protected override ValueBase OnInvoke(SyntaxParser.ExpressionCallContext callSite, ArrayValue parameters)
    {
        parameters[0].Reverse();
        return parameters[0];
    }
}

/// <summary>
/// Represents a method that retrieves the code gen size of the instance object.
/// </summary>
public sealed class SizeMethod : TypeMethodBase
{
    /// <summary>
    /// Construct a new instance of the <see cref="SizeMethod"/>.
    /// </summary>
    /// <param name="services">The shared <see cref="AssemblyServices"/> for
    /// assembler runtime.</param>
    public SizeMethod(AssemblyServices services)
        : base("size", services, 1)
    {

    }

    protected override ValueBase OnInvoke(SyntaxParser.ExpressionCallContext callSite, ArrayValue parameters)
    {
        if (parameters[0].IsDefined)
        {
            return new NumericValue(parameters[0].Size());
        }
        return new UndefinedValue();
    }
}

/// <summary>
/// Represents a method to skip a specified number of elements in a collection.
/// </summary>
public sealed class SkipMethod : TypeMethodBase
{
    /// <summary>
    /// Construct a new instance of a <see cref="SkipMethod"/>.
    /// </summary>
    /// <param name="services">The shared <see cref="AssemblyServices"/> for
    /// assembler runtime.</param>
    public SkipMethod(AssemblyServices services)
        : base("skip", services, 2)
    {

    }

    protected override ValueBase OnInvoke(SyntaxParser.ExpressionCallContext callSite, ArrayValue parameters)
    {
        int skip = parameters[1].AsInt();
        if (skip < 0 || skip > parameters[0].Count)
        {
            throw new Error(callSite.exprList().expr()[0], "Index out of range");
        }
        if (parameters[0] is StringValue sv)
        {
            return new StringValue(sv.Skip(skip), sv.TextEncoding, sv.EncodingName)
            {
                TextEncoding = sv.TextEncoding
            };
        }
        return new ArrayValue(parameters[0].ToList().Skip(skip).ToList())
        {
            IsTuple = ((ArrayValue)parameters[0]).IsTuple
        };
    }
}

/// <summary>
/// Represents a method to test if some elements in the instance object pass
/// the test of the given predicate.
/// </summary>
public sealed class SomeMethod : TypeMethodBase
{
    /// <summary>
    /// Construct a new instance of the <see cref="SomeMethod"/>.
    /// </summary>
    /// <param name="services">The shared <see cref="AssemblyServices"/> for
    /// assembler runtime.</param>
    public SomeMethod(AssemblyServices services)
        : base("some", services, 2)
    {

    }

    protected override ValueBase OnInvoke(SyntaxParser.ExpressionCallContext callSite, ArrayValue parameters)
    {
        ArrayValue array = (ArrayValue)parameters[0];
        if (parameters[1] is not FunctionObject funcObj)
        {
            throw new Error(callSite.exprList().expr()[0], "Parameter must be a predicate");
        }

        for (int i = 0; i < array.Count; i++)
        {
            ArrayValue callBackParameters = new(new List<ValueBase> { array[i] });
            ValueBase? predicateResult = Services.Evaluator.Invoke(callSite, funcObj, callBackParameters) ?? throw new Error(callSite.Start, "User predicate must return a boolean value");
            if (!predicateResult.IsDefined)
            {
                return new UndefinedValue();
            }
            if (predicateResult.AsBool())
            {
                return new BoolValue(true);
            }
        }
        return new BoolValue(false);
    }
}

/// <summary>
/// Represent a method that sorts the instance object's elements, optionally
/// by a passed comparer function.
/// </summary>
public sealed class SortMethod : TypeMethodBase
{
    /// <summary>
    /// Construct a new instance of the <see cref="SortMethod"/>.
    /// </summary>
    /// <param name="services">The shared <see cref="AssemblyServices"/> for
    /// assembler runtime.</param>
    public SortMethod(AssemblyServices services)
        : base("sort", services, -1)
    {

    }

    protected override ValueBase OnInvoke(SyntaxParser.ExpressionCallContext callSite, ArrayValue parameters)
    {
        if (parameters.Count < 1)
        {
            throw new Error(callSite.expr(), "Too few parameters for function");
        }
        ValueComparer comparer = new();
        if (parameters.Count > 1)
        {
            if (parameters.Count > 2)
            {
                throw new Error(callSite.exprList().expr()[1], "Too many parameters for function");
            }
            if (parameters[1] is not FunctionObject funcObj)
            {
                throw new Error(callSite.exprList().expr()[0], "Parameter must be a function");
            }
            comparer.Function = new(callSite, Services.Evaluator, funcObj);
        }
        parameters[0].Sort(comparer);
        return parameters[0];
    }
}

/// <summary>
/// Represent a method to return a substring of an instance object.
/// </summary>
public sealed class SubstringMethod : TypeMethodBase
{
    /// <summary>
    /// Construct a new instance of the <see cref="SubstringMethod"/>.
    /// </summary>
    /// <param name="services">The shared <see cref="AssemblyServices"/> for
    /// assembler runtime.</param>
    public SubstringMethod(AssemblyServices services)
        : base("substring", services, -1)
    {

    }

    protected override ValueBase OnInvoke(SyntaxParser.ExpressionCallContext callSite, ArrayValue parameters)
    {
        if (parameters.Count > 3)
        {
            throw new Error(callSite.exprList(), "Too many parameters for function");
        }
        if (parameters.Count < 2)
        {
            throw new Error(callSite, "Too few parameters for function");
        }
        int start = parameters[1].AsInt();
        ValueBase str = parameters[0];
        if (start < 0 || start >= str.Count)
        {
            throw new Error(callSite.exprList(), "Index out of range");
        }
        int len = str.Count - start;
        if (parameters.Count == 3)
        {
            len = parameters[2].AsInt();
        }
        if (len < 1 || len + start > str.Count)
        {
            throw new Error(callSite.exprList(), "Index of out range");
        }
        return new StringValue($"\"{str.AsString().Substring(start, len)}\"", str.TextEncoding, str.EncodingName)
        {
            Expression = callSite
        };
    }
}

/// <summary>
/// Represents a method to take a specified number of elements in a collection.
/// </summary>
public sealed class TakeMethod : TypeMethodBase
{
    /// <summary>
    /// Construct a new instance of a <see cref="TakeMethod"/>.
    /// </summary>
    /// <param name="services">The shared <see cref="AssemblyServices"/> for
    /// assembler runtime.</param>
    public TakeMethod(AssemblyServices services)
        : base("take", services, 2)
    {
    }

    protected override ValueBase OnInvoke(SyntaxParser.ExpressionCallContext callSite, ArrayValue parameters)
    {
        int take = parameters[1].AsInt();
        if (take < 0 || take > parameters[0].Count)
        {
            throw new Error(callSite, "Index out of range");
        }
        if (parameters[0] is StringValue sv)
        {
            return new StringValue(sv.Take(take), sv.TextEncoding, sv.EncodingName);
        }
        return new ArrayValue(parameters[0].ToList().Take(take).ToList())
        {
            IsTuple = ((ArrayValue)parameters[0]).IsTuple
        };
    }
}

/// <summary>
/// Represents a method to convert the instance object to an array. 
/// </summary>
public sealed class ToArrayMethod : TypeMethodBase
{
    /// <summary>
    /// Construct a new instance of the <see cref="ToArrayMethod"/>.
    /// </summary>
    /// <param name="services">The shared <see cref="AssemblyServices"/> for
    /// assembler runtime.</param>
    public ToArrayMethod(AssemblyServices services)
        : base("toArray", services, 1)
    {
    }

    protected override ValueBase OnInvoke(SyntaxParser.ExpressionCallContext callSite, ArrayValue parameters)
    {
        if (parameters[0] is StringValue sv)
        {
            ArrayValue charArray = new();
            foreach (ValueBase chr in sv)
            {
                charArray.Add(chr);
            }
            return charArray;
        }
        ArrayValue arr = new((ArrayValue)parameters[0])
        {
            IsTuple = false
        };
        if (arr.ElementsSameType)
        {
            return arr;
        }
        throw new TypeMismatchError(callSite.Start);
    }
}

/// <summary>
/// Represents a method that returns a byte array representation of an
/// instance object's CBM float.
/// </summary>
public sealed class ToCbmFltMethod : TypeMethodBase
{
    private readonly bool _packed;

    /// <summary>
    /// Construct a new instance of the <see cref="ToCbmFltMethod"/>.
    /// </summary>
    /// <param name="services">The shared <see cref="AssemblyServices"/> for
    /// assembler runtime.</param>
    /// <param name="packed">The flag indicating whether this method returns
    /// a packed byte string representation or not.</param>
    public ToCbmFltMethod(AssemblyServices services, bool packed)
        : base("toCbmFlt", services, 1)
    {
        _packed = packed;
    }

    protected override ValueBase OnInvoke(SyntaxParser.ExpressionCallContext callSite, ArrayValue parameters)
    {
        var bytes = CbmFloatFunction.ToBytes(parameters[0].AsDouble(), _packed).ToArray();

        ArrayValue byteArray = new();

        for (int i = 0; i < bytes.Length; i++)
        {
            byteArray.Add(new NumericValue(bytes[i])
            {
                Expression = callSite
            });
        }
        return byteArray;
    }
}

/// <summary>
/// Represent a method that returns the instance object as a lower-case string.
/// </summary>
public sealed class ToLowerMethod : TypeMethodBase
{
    /// <summary>
    /// Construct a new instance of the <see cref="ToLowerMethod"/>.
    /// </summary>
    /// <param name="services">The shared <see cref="AssemblyServices"/> for
    /// assembler runtime.</param>
    public ToLowerMethod(AssemblyServices services)
        : base("toLower", services, 1)
    {

    }

    protected override ValueBase OnInvoke(SyntaxParser.ExpressionCallContext callSite, ArrayValue parameters)
    {
        string original = parameters[0].AsString();
        return new StringValue($"\"{original.ToLower()}\"", parameters[0].TextEncoding, parameters[0].EncodingName);
    }
}

/// <summary>
/// Represents a method that stringifies the instance object.
/// </summary>
public sealed class ToStringMethod : TypeMethodBase
{
    /// <summary>
    /// Construct a new instance of the <see cref="ToStringMethod"/>.
    /// </summary>
    /// <param name="services">The shared <see cref="AssemblyServices"/> for
    /// assembler runtime.</param>
    public ToStringMethod(AssemblyServices services)
        : base("toString", services, 1)
    {

    }

    protected override ValueBase OnInvoke(SyntaxParser.ExpressionCallContext callSite, ArrayValue parameters)
    {
        if (parameters[0] is StringValue)
        {
            return parameters[0];
        }
        return new StringValue($"\"{parameters[0]}\"", Services.Encoding, Services.Encoding.EncodingName);
    }
}

/// <summary>
/// Represents a method to convert the array instance to a tuple.
/// </summary>
public sealed class ToTupleMethod : TypeMethodBase
{
    /// <summary>
    /// Construct a new instance of the <see cref="ToTupleMethod"/>.
    /// </summary>
    /// <param name="services">The shared <see cref="AssemblyServices"/> for
    /// assembler runtime.</param>
    public ToTupleMethod(AssemblyServices services)
        : base("toTuple", services, 1)
    {
    }

    protected override ValueBase OnInvoke(SyntaxParser.ExpressionCallContext callSite, ArrayValue parameters)
    {
        return new ArrayValue((ArrayValue)parameters[0])
        {
            IsTuple = true
        };
    }
}

/// <summary>
/// Represents a method that returns the instance object as an upper-case string.
/// </summary>
public sealed class ToUpperMethod : TypeMethodBase
{
    /// <summary>
    /// Construct a new instance of the <see cref="ToUpperMethod"/>.
    /// </summary>
    /// <param name="services">The shared <see cref="AssemblyServices"/> for
    /// assembler runtime.</param>
    public ToUpperMethod(AssemblyServices services)
        : base("toUpper", services, 1)
    {

    }

    protected override ValueBase OnInvoke(SyntaxParser.ExpressionCallContext callSite, ArrayValue parameters)
    {
        string original = parameters[0].AsString();
        return new StringValue($"\"{original.ToUpper()}\"", Services.Encoding, Services.Encoding.EncodingName);
    }
}

/// <summary>
/// Represents a method that returns a union of the instance object with
/// another object.
/// </summary>
public sealed class UnionMethod : TypeMethodBase
{
    /// <summary>
    /// Construct a new instance of the <see cref="UnionMethod"/>.
    /// </summary>
    /// <param name="services">The shared <see cref="AssemblyServices"/> for
    /// assembler runtime.</param>
    public UnionMethod(AssemblyServices services)
        : base("union", services, 2)
    {

    }

    protected override ValueBase OnInvoke(SyntaxParser.ExpressionCallContext callSite, ArrayValue parameters)
    {
        if (!parameters[0].TypeCompatible(parameters[1]))
        {
            throw new TypeMismatchError(callSite.Start);
        }
        HashSet<ValueBase> unionList = ((ArrayValue)parameters[0]).ToHashSet();
        unionList.UnionWith((ArrayValue)parameters[1]);
        return new ArrayValue(unionList.ToList());
    }
}