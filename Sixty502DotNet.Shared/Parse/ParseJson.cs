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
using Sixty502DotNet.Shared.Eval;
using Sixty502DotNet.Shared.Eval.Scope;
using Sixty502DotNet.Shared.Lex;
using Sixty502DotNet.Shared.Parse.Ast;

namespace Sixty502DotNet.Shared.Parse;

public partial class Parser
{
    private readonly Stack<string> _path = new();

    private string GetPath()
    {
        if (_path.Count == 0) return "";
        List<string> rev = new(_path.Reverse());
        return $"/{string.Join('/', rev)}";
    }
    
    private Value JsonValue()
    {
        switch (_current.Type)
        {
            case TokenType.False:
            case TokenType.True:
                Advance();
                return new Value(_previous.Type == TokenType.True)
                {
                    JsonPath = GetPath()
                };
            case TokenType.IntLiteral:
            case TokenType.FloatLiteral:
                return Number();
            case TokenType.Ident:
                return Null();
            case TokenType.Minus:
                Advance();
                return new Value
                (
                    -Number().AsDouble()
                )
                {
                    JsonPath = GetPath()
                };
            case TokenType.OpenBracket:
                return Array();
            case TokenType.OpenBrace:
                return Object();
            case TokenType.StringLiteral:
                return String();
            default:
                throw new CompileException(CompileExceptionType.InvalidJson, _current);
        }
    }

    private Value String()
    {
        Consume(TokenType.StringLiteral);
        return new Value
        (
            _previous.Text.ToString().Trim('"')
        )
        {
            JsonPath = GetPath()
        };
    }
    
    private Value Array()
    {
        var arrayValue = new Value([], TypeTag.Array)
        {
            JsonPath = GetPath()
        };
        Advance();
        if (!Check(TokenType.CloseBracket))
        {
            var i = 0;
            while (true)
            {
                _path.Push(i.ToString());
                var val = JsonValue();
                val.Parent = arrayValue;
                arrayValue.AsArray()?.Add(val);
                _path.Pop();
                if (!Match(TokenType.Comma))
                {
                    break;
                }
                i++;
            }
        }
        Consume(TokenType.CloseBracket);
        return arrayValue;
    }

    private Value Object()
    {
        Advance();
        var obj = new Value(new Dictionary())
        {
            JsonPath = GetPath()
        };
        if (!Check(TokenType.CloseBrace))
        {
            while (true)
            {
                var key = String();
                _path.Push(key.AsString());
                Consume(TokenType.Colon);
                var val = JsonValue();
                val.Parent = obj;
                _ = obj.AsDictionary()?.TryAdd(key, val);
                _path.Pop();
                if (!Match(TokenType.Comma))
                {
                    break;
                }
            }
        }
        Consume(TokenType.CloseBrace);
        return obj;
    }

    private Value Number()
    {
        var valToken = _current;
        if (!Match(TokenType.FloatLiteral))
        {
            Consume(TokenType.IntLiteral);   
        }
        var val = new ExpressionFolder().Visit(new PrimaryExpression(valToken));
        if (val == null)
        {
            throw new CompileException(CompileExceptionType.InvalidJson, valToken);
        }
        if (val.TypeTag != TypeTag.Float)
        {
            val = new Value(val.AsDouble());
        }
        val.JsonPath = GetPath();
        return val;
    }

    private Value Null()
    {
        var nullToken = _current;
        Advance();
        return nullToken.Text is "null"
            ? new Value { JsonPath = GetPath() }
            : throw new CompileException(CompileExceptionType.InvalidJson, nullToken);
    }
}