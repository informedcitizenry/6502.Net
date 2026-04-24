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

using System.Text.RegularExpressions;
using Sixty502DotNet.Shared.Error;
using Sixty502DotNet.Shared.Eval;
using Sixty502DotNet.Shared.Eval.Scope;

namespace Sixty502DotNet.Shared.Json;

public sealed partial class JsonSchema
{
    private static readonly Dictionary<string, JsonSchemaType> s_schemaTypes = new()
    {
        { "array", JsonSchemaType.Array },
        { "boolean", JsonSchemaType.Boolean },
        { "float", JsonSchemaType.Float },
        { "integer", JsonSchemaType.Integer },
        { "null", JsonSchemaType.Null },
        { "number", JsonSchemaType.Number },
        { "object", JsonSchemaType.Object },
        { "string", JsonSchemaType.String }
    };

    private static readonly Dictionary<TypeTag, JsonSchemaType> s_valueToSchemaTypes = new()
    {
        { TypeTag.Array, JsonSchemaType.Array },
        { TypeTag.Boolean, JsonSchemaType.Boolean },
        { TypeTag.Dictionary, JsonSchemaType.Object },
        { TypeTag.Int, JsonSchemaType.Integer },
        { TypeTag.Undefined, JsonSchemaType.Null },
        { TypeTag.String, JsonSchemaType.String }
    };

    private readonly JsonSchema? _root;
    private readonly string _jsonPointer;
    private readonly string? _basePath;
    private readonly bool _basePathIsAbsoluteUri;
    private readonly bool? _autoAssertion;
    private readonly JsonSchemaType _type;

    private JsonSchema()
    {
        _jsonPointer = "#";
        Anchor =
        ContentEncoding =
        ContentMediaType =
        DynamicAnchor =
        Format =
            Id =
                Pattern =
                    Ref =
                        RootBasePath =
                            SchemaUri = string.Empty;
    }

    private JsonSchema(bool autoEvaluation)
        : this()
    {
        _jsonPointer = "#";
        _autoAssertion = autoEvaluation;
        if (!autoEvaluation)
            Not = true;
    }

    private JsonSchema(bool autoEvaluation, JsonSchema parent)
        : this()
    {
        _jsonPointer = $"{parent._jsonPointer}/not";
        _basePath = parent._basePath;
        _basePathIsAbsoluteUri = parent._basePathIsAbsoluteUri;
        RootBasePath = parent.RootBasePath;
        _autoAssertion = autoEvaluation;
    }

    private JsonSchema(Value? value, JsonSchema? parent)
        : this()
    {
        if (value == null)
        {
            return;
        }
        _root = parent?._root ?? parent;
        _jsonPointer = value.JsonPath;
        if (value.TypeTag == TypeTag.Boolean)
        {
            _autoAssertion = value.AsBoolean();
            _basePath = GetBasePath();
            RootBasePath = GetParentBasePath(parent) ?? "#";
            _basePathIsAbsoluteUri = Uri.TryCreate(_basePath, UriKind.Absolute, out _);
            if (_autoAssertion == false)
                Not = new JsonSchema(true, this);
        }
        else
        {
            if (value.AsDictionary() is not {} obj)
                throw new JsonSchemaException("Not a valid schema object", this);
            if (obj.Count > 0)
            {
                Id = GetStringProperty(obj, "$id") ?? "";
                _basePath = GetBasePath();
                RootBasePath = GetParentBasePath(parent) ?? "#";
                _basePathIsAbsoluteUri = _ = Uri.TryCreate(_basePath, UriKind.Absolute, out _);
                SchemaUri = GetStringProperty(obj, "$schema") ?? "#";
                Anchor = GetStringProperty(obj, "$anchor") ?? "";
                Ref = GetStringProperty(obj, "$ref") ?? "";
                DynamicAnchor = GetStringProperty(obj, "$dynamicAnchor") ?? "";
                Format = GetStringProperty(obj, "format") ?? "";
                Pattern = GetStringProperty(obj, "pattern") ?? "";
                ContentEncoding = GetStringProperty(obj, "contentEncoding") ?? "";
                ContentMediaType = GetStringProperty(obj, "contentMediaType") ?? "";
                Minimum = GetProperty<double>(obj, "minimum");
                Maximum = GetProperty<double>(obj, "maximum");
                ExclusiveMaximum = GetProperty<double>(obj, "exclusiveMaximum");
                ExclusiveMinimum = GetProperty<double>(obj, "exclusiveMinimum");
                MultipleOf = GetProperty<double>(obj, "multipleOf");
                MinLength = GetProperty<long>(obj, "minLength");
                MaxLength = GetProperty<long>(obj, "maxLength");
                MinItems = GetProperty<long>(obj, "minItems");
                MaxItems = GetProperty<long>(obj, "maxItems");
                MinContains = GetProperty<long>(obj, "minContains");
                MaxContains = GetProperty<long>(obj, "maxContains");
                MinProperties = GetProperty<long>(obj, "MinProperties");
                MaxProperties = GetProperty<long>(obj, "MaxProperties");
                UniqueItems = GetProperty<bool>(obj, "uniqueItems");
                Const = GetPolymorphicProperty(obj, "const");
                Required = GetStringPropertyList(obj, "required")?.AsReadOnly();
                Enum = GetEnumProperty(obj);
                DependentRequired = GetDependentProperties(obj);
                AdditionalProperties = GetSchemaProperty(obj, "additionalProperties");
                Items = GetSchemaProperty(obj, "items");
                PropertyNames = GetSchemaProperty(obj, "propertyNames");
                UnevaluatedProperties = GetSchemaProperty(obj, "unevaluatedProperties");
                UnevaluatedItems = GetSchemaProperty(obj, "unevaluatedItems");
                Contains = GetSchemaProperty(obj, "contains");
                Not = GetSchemaProperty(obj, "not");
                If = GetSchemaProperty(obj, "if");
                Then = GetSchemaProperty(obj, "then");
                Else = GetSchemaProperty(obj, "else");
                ContentSchema = GetSchemaProperty(obj, "contentSchema");
                Properties = GetSchemaDictionary(obj, "properties");
                PatternProperties = GetSchemaDictionary(obj, "patternProperties");
                DependentSchemas = GetSchemaDictionary(obj, "dependentSchemas");
                Defs = GetSchemaDictionary(obj, "$defs");
                AllOf = GetSchemaList(obj, "allOf");
                AnyOf = GetSchemaList(obj, "anyOf");
                OneOf = GetSchemaList(obj, "oneOf");
                PrefixItems = GetSchemaList(obj, "prefixItems");
                Type = GetPolymorphicProperty(obj, "type");
                _type = ConvertType();
                Validate();
            }
        }
    }

    public JsonSchema(Value? value)
        : this(value, null)
    {

    }

    private void Validate()
    {
        ValidateProperty("multipleOf", "must be greater than 0", !MultipleOf.HasValue || !MultipleOf.Value.FloatLe(0));
        ValidateProperty("$anchor", "is not a valid anchor name", string.IsNullOrEmpty(Anchor) || AnchorRegex().IsMatch(Anchor));
        ValidateProperty("$dynamicAnchor", "is not a valid anchor name", string.IsNullOrEmpty(DynamicAnchor) || AnchorRegex().IsMatch(DynamicAnchor));
        ValidateProperty("required", "must be a non-empty array of strings", Required?.Count != 0);
        ValidateProperty("dependentRequired", "contains one or more properties whose array is empty.", DependentRequired?.Values.Any(arr => arr.Count == 0) != true);
        ValidateProperty("allOf", "cannot be empty", AllOf?.Count != 0);
        ValidateProperty("anyOf", "cannot be empty", AnyOf?.Count != 0);
        ValidateProperty("oneOf", "cannot be empty", OneOf?.Count != 0);
        ValidateProperty("prefixItems", "cannot be empty", PrefixItems?.Count != 0);
        ValidateProperty("minContains", "must be greater than or equal to zero", MinContains is null or >= 0);
        ValidateProperty("maxContains", "must be greater than or equal to zero", MaxContains is null or  >= 0);
        ValidateProperty("minLength", "must be greater than or equal to zero", MinLength is null or  >= 0);
        ValidateProperty("maxLength", "must be greater than or equal to zero", MaxLength is null or  >= 0);
        ValidateProperty("minItems", "must be greater than or equal to zero", MinItems is null or  >= 0);
        ValidateProperty("maxItems", "must be greater than or equal to zero", MaxItems is null or  >= 0);
        ValidateProperty("minProperties", "must be greater than or equal to zero", MinProperties is null or  >= 0);
        ValidateProperty("maxProperties", "must be greater than or equal to zero", MaxProperties is null or  >= 0);
    }

    private void ValidateProperty(string propertyName, string error, bool isValid)
    {
        if (!isValid)
            throw new JsonSchemaException($"Error in schema: Property '{propertyName}' {error}.", this);
    }

    private JsonSchemaType ConvertType()
    {
        var convertedType = JsonSchemaType.None;
        try
        {
            if (Type != null)
            {
                if (Type.TypeTag == TypeTag.Dictionary)
                {
                    return JsonSchemaType.Object;
                }
                if (Type.AsArray() is {} types)
                {
                    convertedType = types.Aggregate(convertedType, (current, type)
                        => current | s_schemaTypes[type.AsString()]);
                }
                else
                {
                    convertedType = s_schemaTypes[Type.AsString()];
                }
            }
            return convertedType;
        }
        catch
        {
            throw new JsonSchemaException($"Error in schema: Property 'type' contains one or more invalid type names.", this);
        }
    }
    private static string? GetParentBasePath(JsonSchema? parent)
    {
        if (parent is { _basePathIsAbsoluteUri: true } &&
            Uri.TryCreate(parent._basePath, UriKind.Absolute, out var parentUri))
        {
            return parentUri.ToString();
        }
        return null;
    }
    
    public string? GetPath(bool absolute)
    {
        if (absolute)
            return _basePathIsAbsoluteUri ? _basePath ?? "/" : null;
        return _jsonPointer;
    }
    
    public string? GetBasePath()
    {
        Uri? idUri = null;
        if (!string.IsNullOrEmpty(Id) && Uri.TryCreate(Id, UriKind.RelativeOrAbsolute, out idUri) &&
            idUri.IsAbsoluteUri)
        {
            return idUri.ToString();
        }
        if (idUri != null)
        {
            if (!string.IsNullOrWhiteSpace(RootBasePath))
            {
                return new Uri(new Uri(RootBasePath), idUri).ToString();
            }
            return idUri.ToString();
        }
        return null;
    }

    private List<JsonSchema>? GetSchemaList(Dictionary obj, string property)
    {
        if (obj.TryGetValue(property, out var value) &&
            value?.AsArray() is { } arrayProp)
        {
            List<JsonSchema> schemas = [];
            schemas.AddRange(arrayProp.Select(t => new JsonSchema(t, this)));
            return schemas;
        }
        return null;
    }

    private IReadOnlyDictionary<string, JsonSchema>? GetSchemaDictionary(Dictionary obj, string property)
    {
        if (!obj.TryGetValue(property, out var value) ||
            value?.AsDictionary() is not { } prop)
        {
            return null;
        }
        Dictionary<string, JsonSchema> schemaDict = new();
        foreach (var p in prop)
        {
            schemaDict[p.Key.AsString()] = new JsonSchema(p.Value, this);
        }
        return schemaDict;
    }

    private JsonSchema? GetSchemaProperty(Dictionary obj, string property)
    {
        if (obj.TryGetValue(property, out var value))
        {
            return new JsonSchema(value, this);
        }
        return null;
    }

    private IReadOnlyDictionary<string, ISet<string>>? GetDependentProperties(Dictionary obj)
    {
        if (obj.TryGetValue("dependentRequired", out var dependent))
        {
            Dictionary<string, ISet<string>> result = new();
            if (dependent?.AsDictionary() is not { } dependentRequired)
                throw new JsonSchemaException($"Property 'dependentRequired' is not in the correct form", this);
            foreach (var kvp in dependentRequired)
            {
                var propList = GetStringPropertyList(dependentRequired, kvp.Key.AsString()) ?? [];
                ISet<string> required = propList.ToHashSet();
                result[kvp.Key.AsString()] = required;
            }
            return result.AsReadOnly();
        }
        return null;
    }

    private IList<string>? GetStringPropertyList(Dictionary obj, string property)
    {
        if (GetPolymorphicProperty(obj, property)?.AsArray() is {} array)
        {
            List<string> propVals = new();
            for (int i = 0; i < array.Count; i++)
            {
                try
                {
                    propVals.Add(array[i].AsString());
                }
                catch
                {
                    throw new JsonSchemaException($"Property '{property}' requires string values", this);
                }
            }
            return propVals;
        }
        return null;
    }

    private static Value? GetPolymorphicProperty(Dictionary obj, string property)
    {
        _ = obj.TryGetValue(property, out var value);
        return value;
    }

    private static ISet<Value>? GetEnumProperty(Dictionary obj)
    {
        if (!obj.TryGetValue("enum", out var enumArray)) return null;
        if (enumArray?.AsArray() is {} array)
        {
            return new HashSet<Value>(array.ToList());
        }
        return new HashSet<Value>
        {
            enumArray ?? new Value()
        };
    }

    private string? GetStringProperty(Dictionary obj, string property)
    {
        if (obj.TryGetValue(property, out var value))
        {
            return value?.TypeTag == TypeTag.String 
                ? value.AsString() 
                : throw new JsonSchemaException($"Property '{property}' must be a string value", this);
        }
        return null;
    }
    
    private static T? GetProperty<T>(Dictionary obj, string property) where T : struct
    {
        if (!obj.TryGetValue(property, out var value)) return null;
        var o = value?.ToObject();
        if (o == null) return null;
        try
        {
            return (T)Convert.ChangeType(o, typeof(T));
        }
        catch
        {
            return null;
        }
    }
    
    public bool MatchesTokenType(Value? token)
    {
        if (_type == JsonSchemaType.None) return true;
        if (token == null)
        {
            return _type == JsonSchemaType.Null;
        }
        JsonSchemaType tokenSchemaType = s_valueToSchemaTypes[token.TypeTag];
        if ((_type & tokenSchemaType) == tokenSchemaType) return true;
        return token.IsNumber()  &&
            ((token.TypeTag == TypeTag.Int && _type == JsonSchemaType.Number) ||
            (token.TypeTag == TypeTag.Float && _type == JsonSchemaType.Float));
    }

    public override string ToString()
    {
        return GetPath(false) ?? _jsonPointer;
    }

    public static implicit operator JsonSchema(bool autoEvaluate) => new(autoEvaluate);
    
    public static implicit operator bool(JsonSchema s) 
        => s._autoAssertion is true;
    
    public string Id { get; }

    public string SchemaUri { get; }
    
    public string Ref { get; }
    
    public IReadOnlyDictionary<string, JsonSchema>? Defs { get; }
    
    public string Anchor { get; }

    public string DynamicAnchor { get; }
    
    public IReadOnlyDictionary<string, JsonSchema>? Properties { get; }

    public JsonSchema? AdditionalProperties { get; }

    public JsonSchema? UnevaluatedProperties { get; }
    
    public JsonSchema? PropertyNames { get; }
    
    public IReadOnlyDictionary<string, JsonSchema>? PatternProperties { get; }
    
    public long? MinProperties { get; }
    
    public long? MaxProperties { get; }
    
    public IReadOnlyList<string>? Required { get; }
    
    public IReadOnlyDictionary<string, ISet<string>>? DependentRequired { get; }
    
    public IReadOnlyDictionary<string, JsonSchema>? DependentSchemas { get; }
    
    public IList<JsonSchema>? AllOf { get; }
    
    public IList<JsonSchema>? AnyOf { get; }
    
    public IList<JsonSchema>? OneOf { get; }
    
    public JsonSchema? Not { get; }
    
    public JsonSchema? If { get; }
    
    public JsonSchema? Then { get; }
    
    public JsonSchema? Else { get; }
    
    public string Format { get; }
    
    public string Pattern { get; }
    
    public Value? Type { get; }
    
    public string ContentEncoding { get; }
    
    public string ContentMediaType { get; }
    
    public JsonSchema? ContentSchema { get; }

    public Value? Const { get; }
    
    public ISet<Value>? Enum { get; }

    public double? MultipleOf { get; }
    
    public double? Minimum { get; }
    
    public double? Maximum { get; }
    
    public double? ExclusiveMinimum { get; }
    
    public double? ExclusiveMaximum { get; }
    
    public long? MinLength { get; }
    
    public long? MaxLength { get; }
    
    public long? MinItems { get; }

    public long? MaxItems { get; }

    public bool? UniqueItems { get; }

    public JsonSchema? Contains { get; }
    
    public long? MinContains { get; }
    
    public long? MaxContains { get; }

    public JsonSchema? Items { get; }
    
    public IList<JsonSchema>? PrefixItems { get; }
    
    public JsonSchema? UnevaluatedItems { get; }
    
    public bool AutoAssertsToTrue => _autoAssertion == true;

    public bool AutoAssertsToFalse => _autoAssertion == false;

    public string RootBasePath { get; }

    public bool IsMeta => !string.IsNullOrEmpty(SchemaUri) && !string.IsNullOrEmpty(Id) && Id.Equals(SchemaUri);
    
    public bool IsDynamic => _root?.IsMeta == true && !string.IsNullOrEmpty(DynamicAnchor);

    [GeneratedRegex("^#?[a-z_][0-9a-z\\-_.]*$", RegexOptions.Compiled)]
    private static partial Regex AnchorRegex();
}
