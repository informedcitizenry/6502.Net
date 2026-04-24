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

using Sixty502DotNet.Shared.Eval;
using Sixty502DotNet.Shared.Eval.Scope;
using Sixty502DotNet.Shared.Lex;

namespace Sixty502DotNet.Shared.Compile;

public class SymbolTable
{
    private readonly List<Environment> _imports;
    private readonly StringComparer _comparer;
    private readonly Stack<Environment> _stack;

    private const int MaxStackSize = 128;
    
    public SymbolTable(StringComparer comparer)
    {
        Root = new Environment(EnvironmentType.Root, null, null, comparer);
        _imports = [];
        _stack = new Stack<Environment>();
        ActiveEnvironment =  Root;
        _comparer = comparer;
    }

    public bool CaseMismatched(string symbol, bool lookGlobally)
        => ActiveEnvironment.CaseMismatched(symbol, lookGlobally);
    
    public bool IsReferenced(string symbol) 
        => ActiveEnvironment.SymbolIsReferenced(symbol);

    public bool CurrentScopeIsReferenced => ActiveEnvironment.IsReferenced;
    
    public void DefineBuiltIn(string symbol, Value value)
        => Root.DefineBuiltIn(symbol, value);
    
    public bool IsConstant(string symbol) => ActiveEnvironment.IsConstant(symbol);
    
    public bool DefineOrUpdateVariable(Token symbol, Value value)
    {
        if (ActiveEnvironment is { Type: EnvironmentType.Local, Parent: not null } &&
            symbol.Text[0] != '_')
        {
            return ActiveEnvironment.Parent.DefineOrUpdateVariable(symbol, value);
        }
        return ActiveEnvironment.DefineOrUpdateVariable(symbol, value);
    }

    public bool TryDefineGlobal(Token symbol, Value value, out Value? existing)
        => Root.TryDefineConstant(symbol, value, out existing);
    
    public bool TryDefineConstant(Token symbol, Value value, out Value? existing)
    {
        if (ActiveEnvironment is { Type: EnvironmentType.Local, Parent: not null } &&
            symbol.Text[0] != '_')
        {
            return ActiveEnvironment.Parent.TryDefineConstant(symbol, value, out existing);
        }
        return ActiveEnvironment.TryDefineConstant(symbol, value, out existing);
    }

    public bool ActivateNamespace(Token name)
    {
        var definingAt = ActiveEnvironment is { Type: EnvironmentType.Local, Parent: not null }
            ? ActiveEnvironment.Parent
            : ActiveEnvironment;
        var symbolName = name.Text.ToString();
        var existing = definingAt.LookupLocally(symbolName);
        if (existing != null)
        {
            if (existing.AsResolver() is not Namespace ns) return false;
            ActiveEnvironment = ns.Env;
        }
        else
        {
            var newNs = new Namespace(definingAt, _comparer);

            _ = definingAt.TryDefineConstant(name, new Value(newNs), out _);
            ActiveEnvironment = newNs.Env;
        }
        ActiveEnvironment.Enter();
        return true;
    }
    
    public bool TryDefineLabel
        (
            Token symbol, 
            EnvironmentType type, 
            int address, 
            int statementIndex,
            out int? existingAddress)
    {
        var symbolName = symbol.Text.ToString();
        if (symbolName[0] == '_')
        {
            var cheapLocal = new Label(statementIndex, address);
            if (!ActiveEnvironment.TryDefineConstant(symbol, new Value(cheapLocal), out var existing))
            {
                existingAddress = existing?.AsAddress()?.Address;
                return false;
            }
            existingAddress = null;
            return true;
        }
        PopLocal();
        existingAddress = ActiveEnvironment.LookupLocally(symbolName)
                                ?.AsAddress()
                                ?.Address;
        var result = ActiveEnvironment.TryDefineLabel
        (
            symbol, 
            type, 
            _comparer,
            address, 
            statementIndex, 
            out var existingLabel
        );
        if (existingLabel != null)
        {
            if (type == EnvironmentType.Local)
            {
                ActiveEnvironment.LocalLabel = existingLabel;
            }
            ActiveEnvironment = existingLabel.Env;
        }
        else
        {
            var existingSymbol = ActiveEnvironment.Lookup(symbolName);
            if (existingSymbol?.AsAddress() is not ScopeLabel label)
            {
                return false;
            }
            if (type == EnvironmentType.Local)
            {
                ActiveEnvironment.LocalLabel = label;
            }
            ActiveEnvironment = label.Env;
        }
        ActiveEnvironment.Enter();
        return result;
    }

    public string Report(bool labelsAddressesOnly, bool viceLabels) 
        => Root.Report(string.Empty, labelsAddressesOnly, viceLabels);

    public bool SymbolExistsInScope(string symbol) => ActiveEnvironment.SymbolExists(symbol);

    public bool IsImported(List<string> path)
    {
        var scopeVal = ActiveEnvironment.Lookup(path[0]);
        if (scopeVal == null) return false;
        Environment scope;
        if (scopeVal.AsResolver() is ScopeLabel l)
        {
            scope = l.Env;
        }
        else if (scopeVal.AsResolver() is Namespace ns)
        {
            scope = ns.Env;
        }
        else
        {
            return false;
        }
        for (var i = 1; i < path.Count; i++)
        {
            if (scopeVal.AsResolver() is ScopeLabel label)
            {
                scope = label.Env;
            }
            else if (scopeVal.AsResolver() is Namespace ns)
            {
                scope = ns.Env;
            }
        }
        return _imports.Any(s => ReferenceEquals(s, scope));
    }
    
    public void Import(List<string> path)
    {
        var scopeVal = ActiveEnvironment.Lookup(path[0]);
        if (scopeVal == null)
        {
            throw new KeyNotFoundException($"Import path `{path[0]}` could not be found");
        }
        Environment scope;
        if (scopeVal.AsResolver() is ScopeLabel l)
        {
            scope = l.Env;
        }
        else if (scopeVal.AsResolver() is Namespace ns)
        {
            scope = ns.Env;
        }
        else
        {
            throw new Exception($"Symbol `{path[0]}` does not define a scope");
        }
        for (var i = 1; i < path.Count; i++)
        {
            scopeVal = scope.LookupLocally(path[i]);
            if (scopeVal == null)
            {
                throw new KeyNotFoundException($"Import path `{path[i]}` could not be found");
            }
            if (scopeVal.AsResolver() is ScopeLabel label)
            {
                scope = label.Env;
            }
            else if (scopeVal.AsResolver() is Namespace ns)
            {
                scope = ns.Env;
            }
            else
            {
                throw new Exception($"Symbol `{path[i]}` does not define a scope");
            }
        }
        _imports.Add(scope);
    }

    public Value? Lookup(string symbol)
    {
        var lookup = ActiveEnvironment.Lookup(symbol);
        for (var i = 0; lookup == null && i < _imports.Count; i++)
        {
            lookup = _imports[i].LookupLocally(symbol);
        }
        return lookup;
    }

    public Value? LookupGlobally(string symbol) => Root.Lookup(symbol);
    
    public void DefineAnonymous(string type, int address, int atIndex)
        => ActiveEnvironment.DefineAnonymousRef(type[0], address, atIndex);
    
    public bool LookupAnonymous(char type, int places, int fromIndex, out int address) 
        => ActiveEnvironment.LookupAnonymousRef(type, places, fromIndex, out address);

    public bool LookupAnonymousAtIndex(char type, int atIndex, out int? address)
        => ActiveEnvironment.LookupAnonymousRefAtIndex(type, atIndex, out address);
    
    public bool InFunction => ActiveEnvironment.Type == EnvironmentType.Func;
    
    
    public bool PushStack(Environment closure)
    {
        if (_stack.Count == MaxStackSize)
        {
            return false;
        }
        Environment parent;
        if (ActiveEnvironment.Type == EnvironmentType.Func)
        {
            parent = ActiveEnvironment.Parent ?? Root;
            _stack.Push(ActiveEnvironment);
        }
        else
        {
            parent = ActiveEnvironment;
        }
        var funcEnv = new Environment(EnvironmentType.Func, closure, parent, _comparer);
        ActiveEnvironment = funcEnv;
        return true;
    }

    public void PopStack()
    {
        if (_stack.TryPop(out var env))
        {
            ActiveEnvironment = env;
        }
        else
        {
            ActiveEnvironment = ActiveEnvironment.Parent ?? Root;
        }
    }
    
    public void Pop()
    {
        PopLocal();
        ActiveEnvironment.LocalLabel = null;
        if (ActiveEnvironment.Parent != null)
        {
            ActiveEnvironment = ActiveEnvironment.Parent.LocalLabel != null
                ? ActiveEnvironment.Parent.LocalLabel.Env
                : ActiveEnvironment = ActiveEnvironment.Parent;
        }
    }

    public void PushAnonymous(bool isProc, int index, int address)
    {
        PopLocal();
        ActiveEnvironment = ActiveEnvironment.DefineAnonymousBlock(isProc, address, index);
        ActiveEnvironment.Enter();
    }

    public void Reset()
    {
        _imports.Clear();
        ActiveEnvironment = Root;
        ActiveEnvironment.Enter();
    }

    private void PopLocal()
    {
        if (ActiveEnvironment is { Type: EnvironmentType.Local, Parent: not null })
        {
            ActiveEnvironment = ActiveEnvironment.Parent;
        }
    }
    
    public IList<KeyValuePair<string, Token>> GetUnreferencedSymbols() 
        => Root.GetUnreferencedSymbols(null);
    
    public bool InRootScope => ActiveEnvironment.Type == EnvironmentType.Root;

    public Environment ActiveEnvironment { get; private set; }

    public Environment Root { get; }
}