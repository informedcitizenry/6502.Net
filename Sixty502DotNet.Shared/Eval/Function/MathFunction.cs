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
using Sixty502DotNet.Shared.Parse.Ast;

namespace Sixty502DotNet.Shared.Eval.Function;

public sealed class MathFunction(Func<double[], double> function, int arity) : IFunction
{
    public Value Invoke(IList<Value> arguments, CallExpression callSite)
    {
        var firstNonDouble = arguments.ToList().FindIndex(v => !v.IsNumber());
        if (firstNonDouble >= 0)
        {
            throw new TypeException
            (
                TypeTag.Float,
                arguments[firstNonDouble],
                callSite.Arguments[firstNonDouble]
            );
        }
        var doubleArgs = arguments
            .Select(arg => arg.AsDouble())
            .ToArray();
        return new Value(function(doubleArgs));
    }

    public int Arity { get; } = arity;

    public int DefaultValues { get; } = 0;
    
    public bool IsVariant { get; } = false;
    
    private static readonly Random s_rng = new();

    public static MathFunction Abs => new(d => Math.Abs(d[0]), 1);
    public static MathFunction Max => new(d => Math.Max(d[0], d[1]), 2);
    public static MathFunction Min => new(d => Math.Min(d[0], d[1]), 2);
    public static MathFunction Pow => new(d => Math.Pow(d[0], d[1]), 2);
    public static MathFunction Round => new(d => Math.Round(d[0]), 1);
    public static MathFunction Sqrt => new(d => Math.Sqrt(d[0]), 1);
    public static MathFunction Sin => new(d => Math.Sin(d[0]), 1);
    public static MathFunction Sgn => new(d => Math.Sign(d[0]), 1);
    public static MathFunction Tan => new(d => Math.Tan(d[0]), 1);
    public static MathFunction Tanh => new(d => Math.Tanh(d[0]), 1);
    public static MathFunction Truncate => new(d => Math.Truncate(d[0]), 1);
    public static MathFunction Acos => new(d => Math.Acos(d[0]), 1);
    public static MathFunction Asin => new(d => Math.Asin(d[0]), 1);
    public static MathFunction Acosh => new(d => Math.Acosh(d[0]), 1);
    public static MathFunction Atan => new(d => Math.Atan(d[0]), 1);
    public static MathFunction Atanh => new(d => Math.Atanh(d[0]), 1);
    public static MathFunction Exp => new(d => Math.Exp(d[0]), 1);
    public static MathFunction Floor => new(d => Math.Floor(d[0]), 1);
     
    public static MathFunction Frac => new(d => d[0] - Convert.ToInt64(d[0]), 1);
    public static MathFunction Ln => new(d => Math.Log(d[0]), 1);
    public static MathFunction Log => new(d => Math.Log(d[0], d[1]), 2);
    public static MathFunction Log10 => new(d => Math.Log10(d[0]), 1);
    public static MathFunction Log2 => new(d => Math.Log2(d[0]), 1);
    public static MathFunction Cos => new(d => Math.Cos(d[0]), 1);
    public static MathFunction Cosh => new(d => Math.Cosh(d[0]), 1);
    public static MathFunction Ceil => new(d => Math.Ceiling(d[0]), 1);
    public static MathFunction Cbrt => new(d => Math.Cbrt(d[0]), 1);
    public static MathFunction Hypot => new(d => Math.Sqrt(d[0] + d[1] * d[1]), 2);
    public static MathFunction Rad => new(d => d[0] * Math.PI / 180.0, 1);
    public static MathFunction Deg => new(d => d[0] * 180 / Math.PI, 1);
    public static MathFunction Byte => new(d => (long)d[0] & 0xff, 1);
    public static MathFunction Dword => new(d => (long)d[0] & 0xffffffff, 1);
    public static MathFunction Long => new(d => (long)d[0] & 0xffffff, 1);
    public static MathFunction Word => new(d => (long)d[0] & 0xffff, 1);
    public static MathFunction Random => new(d => s_rng.Next((int)d[0], (int)d[1]), 2);
}
