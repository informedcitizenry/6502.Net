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
using Environment = Sixty502DotNet.Shared.Compile.Environment;

namespace Sixty502DotNet.Shared.Eval.Scope;

public class Label(int gotoIndex, int address) : IAddress
{
    public Label(int address) : this(-1, address)
    {
    }

    public int Address { get; private set; } = address;

    public int GotoIndex { get; } = gotoIndex;
    
    public bool UpdateAddress(int address)
    {
        if (Address == address) return false;
        Address = address;
        return true;
    }
}

public sealed class ScopeLabel
(
    EnvironmentType type,
    Environment enclosing,
    Environment parent,
    StringComparer comparer,
    int gotoIndex,
    int address
) : Label(gotoIndex, address), IResolver
{
    
    public Environment Env { get; } = new(type, enclosing, parent, comparer);

    public EnvironmentType Type => Env.Type;
    
    public Value? Lookup(string key)
        => Env.Lookup(key);

    public Value? LookupLocally(string enumeration)
        => Env.LookupLocally(enumeration);
    
    public string Report(string parent, bool labelsAddressesOnly, bool viceLabels) 
        => Env.Report(parent,  labelsAddressesOnly, viceLabels);
}