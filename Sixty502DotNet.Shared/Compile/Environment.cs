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

using System.Text;
using Sixty502DotNet.Shared.Eval;
using Sixty502DotNet.Shared.Eval.Scope;
using Sixty502DotNet.Shared.Lex;
using Sixty502DotNet.Shared.Parse.Ast;

namespace Sixty502DotNet.Shared.Compile;

public enum EnvironmentType
{
    Root,
    Block,
    Proc,
    Local,
    Func
}

public sealed class Environment
(
    EnvironmentType type,
    Environment? closure,
    Environment? parent,
    StringComparer comparer
)
{
    private readonly Dictionary<string, Symbol> _symbols = new(comparer);

    private readonly Dictionary<string, Ast> _constantNames = new(comparer);
    
    private readonly AnonymousCollection _anonymousRefs = new();

    public string Report(string parent, bool labelsAddressesOnly, bool viceLabels)
    {
        if (!IsReferenced && Type == EnvironmentType.Proc) return string.Empty;
        var sb = new  StringBuilder();
        var delimiter = viceLabels ? ':' : '.';
        var parentRoot = !string.IsNullOrEmpty(parent) ? $"{parent}{delimiter}" : string.Empty;
        foreach (var symbol in _symbols
                     .OrderBy(kvp => kvp.Key)
                     .Where(sym => 
                         sym.Value.Type != SymbolType.BuiltIn && 
                         sym.Key[0] != '<')
                 )
        {
            var len = parentRoot.Length + symbol.Key.Length;
            var key = symbol.Key;
            if (len > 53)
            {
                len -= parentRoot.Length;
                if (len > 52 && !string.IsNullOrEmpty(parentRoot))
                {
                    parentRoot = "..";
                    if (len > 54)
                    {
                        len = 50;
                    }
                }
                key = $"{symbol.Key[..len]}...";
            }
            var resolver = symbol.Value.Value.AsResolver();
            var addressable = symbol.Value.Value.AsAddress();
            if (resolver is Enumeration || 
                ((labelsAddressesOnly || viceLabels) && addressable is not Label))
            {
                continue;
            }
            if (viceLabels)
            {
                sb.Append($"al {symbol.Value.Value.AsInt():x} .");
                if (!string.IsNullOrEmpty(parentRoot))
                {
                    sb.Append($"{parentRoot}{key}");
                }
                else
                {
                    sb.Append($"{key}");
                }
                sb.AppendLine();
                if (resolver != null)
                {
                    sb.Append(resolver.Report($"{parentRoot}{key}", true, true));
                }
                continue;
            }
            if (!string.IsNullOrEmpty(parentRoot))
            {
                sb.Append($"{parentRoot}{key}".PadRight(56));
            }
            else
            {
                sb.Append($"{key,-56}");
            }
            sb.Append("= ");
            if (addressable != null)
            {
                sb.AppendLine($"${addressable.Address:x}");
            }
            else
            {
                sb.AppendLine($"{symbol.Value.Value}");
            }
            if (resolver != null)
            {
                sb.Append(resolver.Report($"{parentRoot}{key}", labelsAddressesOnly, viceLabels));
            }
        }
        return sb.ToString();
    }   
    
    public bool IsConstant(string symbol)
    {
        if (Parent?._symbols.ContainsKey(symbol) ?? false)
        {
            return Parent.IsConstant(symbol);
        }
        return _symbols.TryGetValue(symbol, out var existing)
               && existing.Type != SymbolType.Variable;
    }

    public bool SymbolExists(string name)
        => _symbols.ContainsKey(name);

    public void LogConstant(string name, Ast ast)
        => _ = _constantNames.TryAdd(name, ast);
    
    public bool TryDefineConstant(Token name, Value value, out Value? existing)
    {
        if (name.Text[0] == '_' && name.Text.Length == 1)
        {
            existing = null;
            return true;
        }
        var symbolName = name.Text.ToString();
        existing = null;
        if (!_symbols.TryGetValue(symbolName, out var symbol))
        {
            symbol = new Symbol(name, value, SymbolType.Constant);
            _symbols[symbolName] = symbol;
            existing = value;
            return true;
        }
        if (symbol.Type != SymbolType.Constant) return false;
        existing = symbol.Value;
        symbol.Value = value;
        return false;
    }

    public Ast? ConstantDeclaration(string name)
    {
        _ = _constantNames.TryGetValue(name, out var ast);
        return ast;
    }
    
    public bool DefineOrUpdateThis(Value argument)
    {
        if (LookupLocally("this") != null)
        {
            return false;
        }
        var symbol = new Symbol(new Token(), argument, SymbolType.BuiltIn);
        _symbols.Add("this", symbol);
        return true;
    }

    public bool DefineOrUpdateVariable(Token name, Value value)
    {
        if (name.Text[0] == '_' && name.Text.Length == 1) return true;
        var symbolName = name.Text.ToString();
        if (Parent?._symbols.ContainsKey(symbolName) ?? false)
        {
            return Parent.DefineOrUpdateVariable(name, value);
        }
        if (_symbols.TryGetValue(symbolName, out var existing))
        {
            if (existing.Type != SymbolType.Variable)
            {
                return false;
            }
        }
        _symbols[symbolName] = new Symbol(name, value, SymbolType.Variable);
        return true;
    }

    public void DefineBuiltIn(string name, Value value) 
        => _symbols[name] = new Symbol(new Token(), value, SymbolType.BuiltIn);

    public Environment DefineAnonymousBlock(bool isProc, int address, int index)
    {
        var anonymousSymbol = $"<anonymous@{index}>";
        if (_symbols.TryGetValue(anonymousSymbol, out var existingSymbol) && 
            existingSymbol.Value.AsAddress() is ScopeLabel label)
        {
            label.UpdateAddress(address);
            return label.Env;
        }
        var newAnon = new ScopeLabel
        (
            isProc ? EnvironmentType.Proc : EnvironmentType.Block, 
            this, 
            this, 
            StringComparer.Ordinal, 
            index, 
            address
        );
        _symbols.Add
        (
            anonymousSymbol, 
            new Symbol(new Token(), new Value((IAddress)newAnon), SymbolType.Constant)
        );
        return newAnon.Env;
    }
   
    public void DefineAnonymousRef(char type, int address, int atIndex)
    {
        if (Type == EnvironmentType.Local)
        {
            Parent?.DefineAnonymousRef(type, address, atIndex);
        }
        _anonymousRefs.Define(type, address, atIndex);
    }

    public bool LookupAnonymousRef(char type, int places, int fromIndex, out int address)
    {
        if (Type == EnvironmentType.Local)
        {
            address = Address.BadAddress;
            return Parent?.LookupAnonymousRef(type, places, fromIndex, out address) == true;
        }
        if (_anonymousRefs.TryLookup(type, places, fromIndex, out address))
        {
            return true;
        }
        places -= _anonymousRefs.CountOfRefsOfType(type, fromIndex);
        return Parent?.LookupAnonymousRef(type, places, fromIndex, out address) == true;
    }

    public bool LookupAnonymousRefAtIndex(char type, int atIndex, out int? address)
    {
        if (Type == EnvironmentType.Local)
        {
            address = null;
            return Parent?.LookupAnonymousRefAtIndex(type, atIndex, out address) == true;
        }
        return _anonymousRefs.TryLookupAtIndex(type, atIndex, out address);
    }
    
    public bool TryDefineLabel
    (
        Token name, 
        EnvironmentType type, 
        StringComparer comparer, 
        int address,
        int statementIndex,
        out ScopeLabel? labelValue
    )
    {
        var symbolName =  name.Text.ToString();
        labelValue = null;
        if (_symbols.TryGetValue(symbolName, out var existingValue))
        {
            if (existingValue.Type != SymbolType.Constant ||
                existingValue.Value.AsAddress() is not ScopeLabel label) return false;
            _ = label.UpdateAddress(address);
            labelValue = label;
            return false;
        }
        labelValue = new ScopeLabel(type, this,this, comparer, statementIndex, address);
        _symbols.Add
        (
            symbolName, 
            new Symbol(name, new Value((IAddress)labelValue), SymbolType.Constant)
        );
        return true;
    }

    public bool SymbolIsVariable(string symbolName)
        => _symbols.TryGetValue(symbolName, out var symbol) && symbol.Type == SymbolType.Variable;
    
    public bool SymbolIsReferenced(string symbolName) 
        => _symbols.TryGetValue(symbolName, out var symbol) && symbol.IsReferenced;

    public Value? Lookup(string name)
    {
        if (!_symbols.TryGetValue(name, out var symbol))
        {
            return Parent?.Lookup(name) ?? closure?.Lookup(name);
        }
        symbol.IsReferenced |= symbol.Type != SymbolType.BuiltIn;
        if (symbol.Value.AsResolver() is ScopeLabel label)
        {
            label.Env.IsReferenced |= symbol.IsReferenced;
        }
        if (Type != EnvironmentType.Proc)
        {
            IsReferenced |= symbol.IsReferenced;
        }
        return symbol.Value;
    }

    public Value? PeekLocally(string name)
    {
        if (!_symbols.TryGetValue(name, out var symbol))
        {
            return null;
        }
        return symbol.Value;
    }
    
    public Value? LookupLocally(string name)
    {
        if (!_symbols.TryGetValue(name, out var symbol))
        {
            return null;
        }
        symbol.IsReferenced |= symbol.Type != SymbolType.BuiltIn;
        if (symbol.Value.AsResolver() is ScopeLabel scopeLabel)
        {
            scopeLabel.Env.IsReferenced |= symbol.IsReferenced;
        }
        if (Type != EnvironmentType.Proc)
        {
            IsReferenced |= symbol.IsReferenced;
        }
        return symbol.Value;
    }
    
    public bool CaseMismatched(string name, bool lookGlobally)
    {
        if (_symbols.ContainsKey(name))
        {
            return _symbols.Keys.FirstOrDefault(k =>
                k.Equals(name, StringComparison.OrdinalIgnoreCase)) != null;
        }
        return lookGlobally && 
               (Parent?.CaseMismatched(name, true) ?? false) &&
               (closure?.CaseMismatched(name, true) ?? false);
    }
    
    public Environment? Parent { get; } = parent;

    public EnvironmentType Type { get; } = type;

    public IList<KeyValuePair<string, Token>> GetUnreferencedSymbols(string? parentName)
    {
        var unreferenced = new List<KeyValuePair<string, Token>>();
        foreach (var entry in 
                 _symbols.Where(kvp 
                     => !kvp.Value.IsReferenced && 
                        kvp.Value.Type != SymbolType.BuiltIn &&
                        kvp.Value.Token.Type != TokenType.Eof))
        {
            var entryPath = parentName != null ? $"{parentName}.{entry.Key}" : entry.Key;
            unreferenced.Add(new KeyValuePair<string, Token>(entryPath, entry.Value.Token));
            if (entry.Value.Value.AsResolver() is ScopeLabel l)
            {
                unreferenced.AddRange(l.Env.GetUnreferencedSymbols(entryPath));
            }
            else if (entry.Value.Value.AsResolver() is Namespace ns)
            {
                unreferenced.AddRange(ns.Env.GetUnreferencedSymbols(entryPath));
            }
        }
        return unreferenced;
    }

    public bool IsReferenced
    {
        get;
        private set
        {
            field = value;
            Parent?.IsReferenced |= value;
        }
    }

    public ScopeLabel? LocalLabel { get; set; }
    
    public void Enter()
    {
        LocalLabel = null;
        foreach (var kvp in _symbols.Where(kvp => kvp.Value.Type == SymbolType.Variable))
        {
            _symbols.Remove(kvp.Key);
        }
    }
}