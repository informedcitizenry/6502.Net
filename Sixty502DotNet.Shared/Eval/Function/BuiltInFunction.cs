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

using Sixty502DotNet.Shared.Compile;
using Sixty502DotNet.Shared.Error;
using Sixty502DotNet.Shared.Eval.Scope;
using Sixty502DotNet.Shared.Eval.String;
using Sixty502DotNet.Shared.Parse.Ast;

namespace Sixty502DotNet.Shared.Eval.Function;

public sealed class FormatFunction(TextEncodingCollection encodingCollection) : IFunction
{
    public Value Invoke(IList<Value> arguments, CallExpression callSite)
    {
        var format =  arguments[0].AsString();
        if (arguments.Count == 1) return new Value(format);
        var values = new object[arguments.Count - 1];
        for (var i = 1; i < arguments.Count; i++)
        {
            values[i - 1] = arguments[i];
        }
        try
        {
            return new Value(string.Format(new ValueFormatter(encodingCollection), format, values));
        }
        catch
        {
            throw new CompileException(CompileExceptionType.InvalidFormatSpecifier, callSite.Arguments[0]);
        }
        
    }
    
    public int Arity => 1;

    public int DefaultValues => 0;
    
    public bool IsVariant => true;
}

public sealed class RangeFunction : IFunction
{
    public Value Invoke(IList<Value> arguments, CallExpression callSite)
    {
        long step = 1;
        if (!arguments[0].IsNumber())
        {
            throw new TypeException(TypeTag.Float, arguments[0], callSite.Arguments[0]);
        }
        var start = arguments[0].AsInt();
        if (!arguments[1].IsNumber())
        {
            throw new TypeException(TypeTag.Float, arguments[1], callSite.Arguments[1]);
        }
        var end = arguments[1].AsInt();
        if (arguments.Count > 2)
        {
            if (!arguments[2].IsNumber())
            {
                throw new TypeException(TypeTag.Float, arguments[2], callSite.Arguments[2]);
            }
            if (arguments.Count > 3)
            {
                throw new CompileException(CompileExceptionType.TooManyArguments, callSite.Arguments[3]);
            }
            step = arguments[2].AsInt();
            if (step == 0)
            {
                throw new CompileException(CompileExceptionType.ValueCannotBeZero, callSite.Arguments[2]);
            }
        }
        var range = new List<Value>();
        while (true)
        {
            range.Add(new Value(start));
            start += step;
            if ((end > 0 && start >= end) || (end < 0 && start <= end))
            {
                break;
            }
        }
        return new Value(range, TypeTag.Array);
    }

    public bool IsVariant => true; 
    
    public int Arity => 2;

    public int DefaultValues => 0;
    
    public static RangeFunction Function => new();
}

public sealed class CbmFltFunction : IFunction
{
    public Value Invoke(IList<Value> arguments, CallExpression callSite)
    {
        if (!arguments[0].IsNumber())
        {
            throw new TypeException(TypeTag.Float, arguments[0], callSite.Arguments[0]);
        }
        var value = ValueHelper.GetCbmFloat(arguments[0].AsDouble());
        return new Value(value);
    }

    public int Arity => 1;
    
    public bool IsVariant => false;

    public int DefaultValues => 0;
    
    public static CbmFltFunction Function => new();
}

public sealed class DeprecatedFunction
(
    string name,
    ErrorLogger logger, 
    string altName, 
    IFunction altFunction
) : IFunction
{
    public Value? Invoke(IList<Value> arguments, CallExpression callSite)
    {
        logger.LogWarning
        (
            $"Function `{name}` is deprecated. Please call `{altName}` instead", 
            new PrimaryExpression(callSite.LeftToken)
        );
        return altFunction.Invoke(arguments, callSite);
    }

    public int Arity => altFunction.Arity;

    public int DefaultValues => 0;
    
    public bool IsVariant => altFunction.IsVariant;
}

public sealed class SizeofFunction(TextEncodingCollection collection) : IFunction
{
    public Value Invoke(IList<Value> arguments, CallExpression callSite) =>
        arguments[0].IsNumber() 
            ? new Value(arguments[0].AsInt(collection).Size()) 
            : new Value(arguments[0].Length);

    public int Arity => 1;
    public int DefaultValues => 0;
    public bool IsVariant => false;
}

public sealed class PokePeekFunction(bool isPoke, Output output) : IFunction
{
    public Value? Invoke(IList<Value> arguments, CallExpression callSite)
    {
        if (!arguments[0].IsNumber() || arguments[0].AsInt().Size() > 3)
        {
            throw new IntegerOverflowException(3, Int24.MinValue,  UInt24.MaxValue, callSite.Arguments[0]);
        }
        if (!isPoke)
        {
            return new Value(output.PeekInAssembledSpace((int)arguments[0].AsInt()));
        }
        if (!arguments[1].IsNumber() || arguments[1].AsInt().Size() > 1)
        {
            throw new IntegerOverflowException(1, sbyte.MinValue,  byte.MaxValue, callSite.Arguments[1]);
        }
        output.PokeInAssembledSpace((int)arguments[0].AsInt(), (byte)(arguments[1].AsInt() & 0xff));
        return null;
    }

    public int Arity => isPoke ? 2 : 1;

    public int DefaultValues => 0;

    public bool IsVariant => false;
}

public sealed class TypeofFunction : IFunction
{
    public Value Invoke(IList<Value> arguments, CallExpression callSite) 
        => new(arguments[0].TypeDisplayName());

    public int Arity => 1;
    public int DefaultValues => 0;
    public bool IsVariant => false;
}

public sealed class CharFunction(TextEncodingCollection collection) : IFunction
{
    public Value Invoke(IList<Value> arguments, CallExpression callSite)
    {
        if (!arguments[0].IsNumber())
        {
            throw new TypeException(TypeTag.Float, arguments[0], callSite.Arguments[0]);
        }

        var codepoint = arguments[0].AsInt(collection);
        if (codepoint is < 0 or > Unicode.MaxCodepoint)
        {
            throw new CompileException(CompileExceptionType.InvalidCodePoint, callSite.Arguments[0]);
        }
        return new Value(char.ConvertFromUtf32((int)codepoint));
    }

    public int Arity => 1;
    public int DefaultValues => 0;
    public bool IsVariant => false;
}

public sealed class SectionFunction(Output output) : IFunction
{
    public Value? Invoke(IList<Value> arguments, CallExpression callSite)
    {
        if (!arguments[0].IsCharOrString())
        {
            throw new TypeException(TypeTag.String, arguments[0], callSite.Arguments[0]);
        }
        var sectionName = arguments[0].AsString();
        var sectionStart = output.SectionStart(sectionName);
        return sectionStart < 0 ? null : new Value(new Label(sectionStart));
    }

    public int Arity => 1;

    public int DefaultValues => 0;
    
    public bool IsVariant => false;
}