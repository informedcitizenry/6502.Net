//-----------------------------------------------------------------------------
// Copyright (c) 2017-2024 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using System.Text;

namespace Sixty502DotNet.Shared;

/// <summary>
/// A class that transforms a JSON string into a value, which if a primitive
/// or string is a <see cref="ValueBase"/>, if an array a
/// <see cref="JsonArray"/>, and if a JSON object a <see cref="JsonObject"/>.
/// </summary>
public class JsonDeserializer : JsonBaseVisitor<ValueBase?>
{
    private readonly Stack<string> _path;

    private string GetPath()
    {
        if (_path.Count == 0) return "";
        List<string> rev = new(_path.Reverse());
        return $"/{string.Join('/', rev)}";
    }

    /// <summary>
    /// Construct a new instance of the <see cref="JsonDeserializer"/> class.
    /// </summary>
    public JsonDeserializer()
    {
        _path = new();
        Errors = new();
    }

    public override ValueBase? VisitObject([NotNull] JsonParser.ObjectContext context)
    {
        JsonObject obj = new()
        {
            JsonPath = GetPath()
        };
        JsonParser.MemberContext[] members = context.members().member();
        for (int i = 0; i < members.Length; i++)
        {
            string key = GetString(members[i].String());
            _path.Push(key);
            ValueBase? val = Visit(members[i].value());
            if (val != null)
            {
                val.Parent = obj;
            }
            _path.Pop();
            obj.Add(key, val);
        }
        return obj;
    }

    public override ValueBase? VisitArray([NotNull] JsonParser.ArrayContext context)
    {
        JsonArray array = new()
        {
            JsonPath = GetPath()
        };
        for (int i = 0; i < context.elements().value().Length; i++)
        {
            _path.Push(i.ToString());
            ValueBase? val = Visit(context.elements().value()[i]);
            if (val != null)
            {
                val.Parent = array;
            }
            array.Add(val);
            _path.Pop();
        }
        return array;
    }

    private static string GetString(ITerminalNode stringNode)
    {
        return StringConverter.ConvertString(stringNode.GetText(), Encoding.UTF8, null).AsString();
    }

    public override ValueBase? VisitValue([NotNull] JsonParser.ValueContext context)
    {
        if (context.Number() != null)
        {
            bool isFloat = context.Start.Text.Contains('.');
            return new NumericValue(double.Parse(context.Number().GetText()), isFloat)
            {
                JsonPath = GetPath()
            };
        }
        if (context.String() != null)
        {
            ValueBase stringValue = StringConverter.ConvertString(context.Start.Text, Encoding.UTF8, null);
            stringValue.JsonPath = GetPath();
            return stringValue;
        }
        if (context.Start.Text.Equals("null", StringComparison.Ordinal))
        {
            return null;
        }
        if (bool.TryParse(context.Start.Text, out bool value))
        {
            return new BoolValue(value)
            {
                JsonPath = GetPath()
            };
        }
        return base.VisitChildren(context);
    }

    public override ValueBase? VisitJson([NotNull] JsonParser.JsonContext context)
    {
        return Visit(context.value());
    }

    public List<Error> Errors { get; init; }

    /// <summary>
    /// Deserialize a JSON string into a value.
    /// </summary>
    /// <param name="json">The JSON source.</param>
    /// <returns>A <see cref="ValueBase"/> representing the parsed JSON, which
    /// could be a <c>null</c> value.</returns>
    public ValueBase? Deserialize(string json)
    {
        JsonLexer lexer = new(CharStreams.fromString(json));
        JsonParser parser = new(new CommonTokenStream(lexer));
        ErrorListener errorListener = new();
        parser.RemoveErrorListeners();
        parser.AddErrorListener(errorListener);
        JsonParser.JsonContext parsed = parser.json();
        if (errorListener.Errors.Count > 0)
        {
            Errors.AddRange(errorListener.Errors);
            return null;
        }
        return VisitJson(parsed);
    }
}

