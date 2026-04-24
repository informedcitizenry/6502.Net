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
using Sixty502DotNet.Shared.Lex;
using Sixty502DotNet.Shared.Parse.Ast;
using Environment = Sixty502DotNet.Shared.Compile.Environment;

namespace Sixty502DotNet.Shared.Eval.Scope;

public sealed class Enumeration
(
    StringComparer comparer,
    bool assignable
) : IResolver
{
    private readonly Environment _environment = new(EnvironmentType.Block, null, null, comparer);

    public Value? Lookup(string key) => LookupLocally(key);

    public Value? LookupLocally(string enumeration) => _environment.Lookup(enumeration);

    public string Report(string parent, bool labelsAddressesOnly, bool viceLabels)
        => _environment.Report(parent, labelsAddressesOnly, viceLabels);
    
    public bool Define(Token enumerator, Value value, Expression? defaultValue = null)
        => _environment.TryDefineConstant(enumerator, value, out _);
    
    public bool Assignable { get; } = assignable;
}