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
using Sixty502DotNet.Shared.Error;
using Sixty502DotNet.Shared.Eval.Scope;
using Sixty502DotNet.Shared.Lex;
using Sixty502DotNet.Shared.Parse;
using TransformFunc = System.Func<Sixty502DotNet.Shared.Eval.Value, Sixty502DotNet.Shared.Eval.Value?>;

namespace Sixty502DotNet.Shared.Json;

public sealed class JsonSchemaBuilder
{
    public class KeywordMapper
    {
        private readonly struct Mapping
        {
            public string KeywordName { get; }

            public TransformFunc Transform { get; }

            public Mapping(string keywordName)
                => (KeywordName, Transform, RaiseExceptionOnTypeMismatch) =
                 (keywordName, jin => jin, true);
            
            public Mapping(string propertyName, TransformFunc transform, bool raiseExceptionOnTypeMismatch)
                => (KeywordName, Transform, RaiseExceptionOnTypeMismatch) = (propertyName, transform, raiseExceptionOnTypeMismatch);

            public bool RaiseExceptionOnTypeMismatch { get; }

            public override string ToString() => KeywordName;
        }

        readonly Dictionary<(string, TypeTag), Mapping> _typeMapping = new();

        public KeywordMapper Map(string name, TypeTag typeTag)
            => Map(name, typeTag, name);
        
        public KeywordMapper Map(string draftName, TypeTag typeTag, string keywordName)
        {
            _typeMapping[(draftName, typeTag)] = new Mapping(keywordName);
            return this;
        }

        public KeywordMapper Map(string name, TypeTag typeTag, TransformFunc transform)
            => Map(name, typeTag, name, transform, true);

        public KeywordMapper Map(string draftName, TypeTag typeTag, string keywordName, TransformFunc transform)
            => Map(draftName, typeTag, keywordName, transform, true);
        
        public KeywordMapper Map(string name, TypeTag typeTag, TransformFunc transform, bool raiseExceptionOnTypeMismatch)
            => Map(name, typeTag, name, transform, raiseExceptionOnTypeMismatch);
        
        public KeywordMapper Map(string draftName, TypeTag typeTag, string keywordName, TransformFunc transform, bool raiseExceptionOnTypeMismatch)
        {
            _typeMapping[(draftName, typeTag)] = new Mapping(keywordName, transform, raiseExceptionOnTypeMismatch);
            return this;
        }
        
        public KeywordMapper CombineWith(KeywordMapper other)
        {
            other._typeMapping.ToList().ForEach(kvp => _typeMapping[kvp.Key] = kvp.Value);
            return this;
        }
        
        public (string? propertyName, Value? value) Transform(string draftName, Value? token)
        {
            var type = token?.TypeTag ?? TypeTag.Undefined;
            if (token != null)
            {
                if (_typeMapping.TryGetValue((draftName, type), out var mapping) ||
                _typeMapping.TryGetValue((draftName, TypeTag.Dictionary), out mapping))
                    return (mapping.KeywordName, mapping.Transform(token));
            }
            var expected = _typeMapping.Keys.FirstOrDefault(nameType => nameType.Item1.Equals(draftName));
            if (_typeMapping.TryGetValue(expected, out var expectedMapping) && expectedMapping.RaiseExceptionOnTypeMismatch)
                throw new JsonSchemaBuilderException($"Property '{draftName}' expects a different type from '{token?.TypeTag.ToString().ToLower() ?? "null"}'.");
            return (null, null);
        }
    }

    private KeywordMapper _mapper = new();

    private KeywordMapper Draft3()
            => new KeywordMapper().Map("type", TypeTag.Array, "anyOf", AnyOfType)
                                  .Map("divisibleBy", TypeTag.Int, "multipleOf")
                                  .Map("divisibleBy", TypeTag.Float, "multipleOf")
                                  .Map("disallow", TypeTag.Array, "not", NotAnyOf)
                                  .Map("disallow", TypeTag.String, "not", NotType)
                                  .Map("extends", TypeTag.Dictionary, "allOf", Extends)
                                  .Map("extends", TypeTag.Array, "allOf", ArrayOfSchemasDraft3And4)
                                  .CombineWith(Drafts3And4())
                                  .CombineWith(AllDraftsPre2020Mapper())
                                  .CombineWith(AllDraftsMapper());

    private KeywordMapper Draft4()
        => new KeywordMapper().Map("allOf", TypeTag.Array, ArrayOfSchemasPostDraft4)
                              .Map("anyOf", TypeTag.Array, ArrayOfSchemasPostDraft4)
                              .Map("oneOf", TypeTag.Array, ArrayOfSchemasPostDraft4)
                              .CombineWith(Drafts3And4())
                              .CombineWith(AllDraftsPost3())
                              .CombineWith(AllDraftsPre2020Mapper())
                              .CombineWith(AllDraftsMapper());

    private KeywordMapper Draft6()
        => Drafts6And7().CombineWith(AllDraftsPost4Pre2020())
                        .CombineWith(AllDraftsMapper());

    private KeywordMapper Draft7()
        => Drafts6And7().CombineWith(AllDraftsPost4Pre2020())
                        .CombineWith(AllDraftsPost6())
                        .CombineWith(AllDraftsMapper());

    private KeywordMapper Draft2019()
        => new KeywordMapper().Map("$recursiveAnchor", TypeTag.Boolean, "$dynamicAnchor", (_) => new Value("\"\""))
                              .Map("$recursiveRef", TypeTag.String, "$dynamicRef")
                              .CombineWith(AllDraftsPost4Pre2020())
                              .CombineWith(AllDraftsPost7())
                              .CombineWith(AllDraftsMapper());

    private KeywordMapper AllSchemas()
        => new KeywordMapper().Map("$dynamicAnchor", TypeTag.String)
                              .Map("$dynamicRef", TypeTag.String)
                              .Map("contentSchema", TypeTag.Dictionary, MapToSchema)
                              .Map("contentSchema", TypeTag.Boolean)
                              .Map("prefixItems", TypeTag.Array, ArrayOfSchemasPostDraft4)
                              .CombineWith(Draft3())
                              .CombineWith(Drafts6And7())
                              .CombineWith(Draft2019());

    private KeywordMapper AllDraftsPost7()
        => new KeywordMapper().Map("$anchor", TypeTag.String)
                              .Map("$defs", TypeTag.Dictionary, ObjectOfSchemasPostDraft4)
                              .Map("dependentSchemas", TypeTag.Dictionary, ObjectOfSchemasPostDraft4)
                              .Map("dependentRequired", TypeTag.Dictionary)
                              .Map("unevaluatedItems", TypeTag.Dictionary, MapToSchema)
                              .Map("unevaluatedItems", TypeTag.Boolean)
                              .Map("unevaluatedProperties", TypeTag.Dictionary, MapToSchema)
                              .Map("unevaluatedProperties", TypeTag.Boolean)
                              .Map("minContains", TypeTag.Int)
                              .Map("maxContains", TypeTag.Int)
                              .CombineWith(AllDraftsPost6());

    private KeywordMapper AllDraftsPost6()
        => new KeywordMapper().Map("contentEncoding", TypeTag.String)
                              .Map("contentMediaType", TypeTag.String)
                              .Map("if", TypeTag.Dictionary, MapToSchema)
                              .Map("if", TypeTag.Boolean)
                              .Map("then", TypeTag.Dictionary, MapToSchema)
                              .Map("then", TypeTag.Boolean)
                              .Map("else", TypeTag.Dictionary, MapToSchema)
                              .Map("else", TypeTag.Boolean);

    private KeywordMapper Drafts6And7()
        => new KeywordMapper().Map("definitions", TypeTag.Dictionary, "$defs", ObjectOfSchemasPostDraft4)
                              .Map("dependencies", TypeTag.Dictionary, "dependentSchemas", DependenciesDrafts6And7);

    private KeywordMapper AllDraftsPost4Pre2020()
        => new KeywordMapper().Map("items", TypeTag.Array, "prefixItems", ArrayOfSchemasPostDraft4)
                              .CombineWith(AllDraftsPost3())
                              .CombineWith(AllDraftsPost4())
                              .CombineWith(AllDraftsPre2020Mapper())
                              .CombineWith(AllDraftsMapper());

    private KeywordMapper AllDraftsPost4()
        => new KeywordMapper().Map("$id", TypeTag.String)
                              .Map("properties", TypeTag.Dictionary, ObjectOfSchemasPostDraft4)
                              .Map("patternProperties", TypeTag.Dictionary, ObjectOfSchemasPostDraft4)
                              .Map("propertyNames", TypeTag.Dictionary, MapToSchema)
                              .Map("propertyNames", TypeTag.Boolean)
                              .Map("items", TypeTag.Boolean)
                              .Map("required", TypeTag.Array)
                              .Map("contains", TypeTag.Dictionary, MapToSchema)
                              .Map("contains", TypeTag.Boolean)
                              .Map("exclusiveMaximum", TypeTag.Int)
                              .Map("exclusiveMinimum", TypeTag.Float)
                              .Map("exclusiveMinimum", TypeTag.Int)
                              .Map("exclusiveMaximum", TypeTag.Float)
                              .Map("minProperties", TypeTag.Int)
                              .Map("maxProperties", TypeTag.Int)
                              .Map("allOf", TypeTag.Array, ArrayOfSchemasPostDraft4)
                              .Map("anyOf", TypeTag.Array, ArrayOfSchemasPostDraft4)
                              .Map("oneOf", TypeTag.Array, ArrayOfSchemasPostDraft4)
                              .Map("not", TypeTag.Boolean)
                              .Map("const", TypeTag.Int);

    private KeywordMapper AllDraftsPost3()
        => new KeywordMapper().Map("multipleOf", TypeTag.Int)
                              .Map("multipleOf", TypeTag.Int)
                              .Map("not", TypeTag.Dictionary, MapToSchema)
                              .Map("type", TypeTag.Array);

    private KeywordMapper Drafts3And4()
        => new KeywordMapper().Map("required", TypeTag.Boolean, AddRequiredProperty, false)
                              .Map("properties", TypeTag.Dictionary, ObjectOfSchemasDraft3And4)
                              .Map("patternProperties", TypeTag.Dictionary, ObjectOfSchemasDraft3And4)
                              .Map("exclusiveMinimum", TypeTag.Boolean, GetExclusive)
                              .Map("exclusiveMaximum", TypeTag.Boolean, GetExclusive)
                              .Map("id", TypeTag.String, "$id")
                              .Map("dependencies", TypeTag.Dictionary, "dependentSchemas", DependenciesDraft3And4)
                              .Map("items", TypeTag.Array, "prefixItems", ArrayOfSchemasDraft3And4);

    private KeywordMapper AllDraftsPre2020Mapper()
        => new KeywordMapper().Map("additionalItems", TypeTag.Dictionary, "items", MapToSchema)
                              .Map("additionalItems", TypeTag.Boolean, "items");

    private KeywordMapper AllDraftsMapper()
        => new KeywordMapper().Map("additionalProperties", TypeTag.Dictionary, MapToSchema)
                              .Map("additionalProperties", TypeTag.Boolean)
                              .Map("items", TypeTag.Dictionary, MapToSchema)
                              .Map("minimum", TypeTag.Int)
                              .Map("minimum", TypeTag.Float)
                              .Map("maximum", TypeTag.Int)
                              .Map("maximum", TypeTag.Float)
                              .Map("minItems", TypeTag.Int)
                              .Map("maxItems", TypeTag.Int)
                              .Map("uniqueItems", TypeTag.Boolean)
                              .Map("pattern", TypeTag.String)
                              .Map("format", TypeTag.String)
                              .Map("minLength", TypeTag.Int)
                              .Map("maxLength", TypeTag.Int)
                              .Map("enum", TypeTag.Array)
                              .Map("$ref", TypeTag.String)
                              .Map("$schema", TypeTag.String)
                              .Map("type", TypeTag.String);

    public JsonSchema BuildFromJson(string draftSchemaJson, bool allowAllIfNotSpecified)
    {
        Parser parser = new("ConfigSchema.json", draftSchemaJson, LexerBehavior.Json);
        var token = parser.Json();
        
        var schemaUri = string.Empty;
        if (token.AsDictionary() is {} obj)
        {
            _ = obj.TryGetValue("$schema",  out var schemaUriToken);
            if (schemaUriToken is { TypeTag: TypeTag.String })
                schemaUri = schemaUriToken.AsString();
        }
        if ((!allowAllIfNotSpecified && string.IsNullOrEmpty(schemaUri)) ||
            schemaUri.Equals("https://json-schema.org/draft/2020-12/schema"))
            return new JsonSchema(token);
        _mapper = schemaUri.Trim('#') switch
        {
            "http://json-schema.org/draft-03/schema" => Draft3(),
            "http://json-schema.org/draft-04/schema" => Draft4(),
            "http://json-schema.org/draft-06/schema" => Draft6(),
            "http://json-schema.org/draft-07/schema" => Draft7(),
            "https://json-schema.org/draft/2019-09/schema" => Draft2019(),
            _ => AllSchemas()
        };
        return new JsonSchema(MapToSchema(token));
    }
    
    private Value? DependenciesDrafts6And7(Value value)
            => Dependencies(value, false);

    private Value? DependenciesDraft3And4(Value value)
        => Dependencies(value, true);

    private Value? ObjectOfSchemasPostDraft4(Value value)
    => ObjectOfSchemas(value, true);

    private Value? ObjectOfSchemasDraft3And4(Value value)
        => ObjectOfSchemas(value, false);

    private Value? ArrayOfSchemasPostDraft4(Value value)
            => ArrayOfSchemas(value, true);

    private Value? ArrayOfSchemasDraft3And4(Value value)
        => ArrayOfSchemas(value, false);


    private Value? MapToSchema(Value? schemaToken)
    {
        if (schemaToken?.AsDictionary() is {} jSchemaObject && schemaToken.Length > 0)
        {
            var schemaResult = new Dictionary();
            for (var i = 0; i < jSchemaObject.Count; i++)
            {
                var prop = jSchemaObject.Keys.ElementAt(i);
                var (propertyName, propertyValue) = _mapper.Transform(prop.ToString(), jSchemaObject[prop]);
                if (propertyName == null || propertyValue == null)
                    continue;
                schemaResult.Add(propertyName, propertyValue);
            }
            if (jSchemaObject.TryGetValue("required", out var requiredToken) && requiredToken?.TypeTag == TypeTag.Array)
                schemaResult.Add("required", requiredToken);
            return new Value(schemaResult);
        }
        return null;
    }

    private Value? Dependencies(Value value, bool postDraft4)
    {
        if (value.IsDefined && value.AsDictionary() is {} dependentObj)
        {
            var dependentSchemas = new Dictionary();
            foreach (var prop in dependentObj)
            {
                var val = prop.Value;
                Value? dependentSchema;
                switch (val.TypeTag)
                {
                    case TypeTag.String when !postDraft4:
                    case TypeTag.Array:
                    {
                        List<Value> reqArray = val.TypeTag == TypeTag.String 
                            ? [val] 
                            : [..val.AsArray() ?? new List<Value>()];
                        var dependentSchemaObj = new Dictionary
                        {
                            { "required", new Value(reqArray, TypeTag.Array) }
                        };
                        dependentSchema = new Value(dependentSchemaObj);
                        break;
                    }
                    case TypeTag.Dictionary:
                        dependentSchema = MapToSchema(val);
                        break;
                    case TypeTag.Boolean when postDraft4:
                        dependentSchema = new Value(val.AsBoolean());
                        break;
                    default:
                        throw new JsonSchemaBuilderException($"Property '{prop}' type is invalid.");
                }
                if (dependentSchema != null)
                    dependentSchemas.Add(prop.Key.ToString(), dependentSchema);
            }
            return new Value(dependentSchemas);
        }
        return null;
    }

    private Value? ObjectOfSchemas(Value value, bool postDraft4)
    {
        if (value.AsDictionary() is {} obj && value.Length > 0)
        {
            var objSchemas = new Dictionary();
            foreach (var prop in obj)
            {
                var schemaToken = prop.Value;
                Value? schema = schemaToken.TypeTag switch
                {
                    TypeTag.Dictionary => MapToSchema(schemaToken),
                    TypeTag.Boolean when postDraft4 => new Value(schemaToken.AsBoolean()),
                    _ => throw new JsonSchemaBuilderException($"Property '{prop}' type is invalid.")
                };
                if (schema != null)
                    objSchemas.Add(prop.Key.ToString(), schema);
            }
            return new Value(objSchemas);
        }
        return null;
    }

    private Value? ArrayOfSchemas(Value value, bool postDraft4)
    {
        if (value.AsArray() is {} arr && value.Length > 0)
        {
            var schemaList = new List<Value>();
            for (var i = 0; i < arr.Count; i++)
            {
                if (arr[i].TypeTag == TypeTag.Dictionary)
                    schemaList.Add(MapToSchema(arr[i]) ?? new Value());
                else if (arr[i].TypeTag == TypeTag.Boolean && postDraft4)
                    schemaList.Add(new Value(arr[i].AsBoolean()));
                else
                    throw new JsonSchemaBuilderException($"Schema must be an object.");
            }
            return new Value(schemaList, TypeTag.Array);
        }
        return null;
    }

    private Value? Extends(Value value)
    {
        var arr = new Value([value], TypeTag.Array);
        return ArrayOfSchemas(arr, false);
    }

    private Value? NotAnyOf(Value value)
    {
        if (value.AsArray() is {} arr && value.Length > 0)
        {
            var notSchema = new Dictionary
            {
                { "type", new Value(arr.ToList(),  TypeTag.Array) }
            };
            return MapToSchema(new Value(notSchema));
        }
        return null;
    }

    private static Value NotType(Value value)
    {
        Dictionary obj = new()
        {
            { "type", value }
        };
        return new Value(obj);
    }


    private Value? AnyOfType(Value value)
    {
        if (value.AsArray() is {} arr && value.Length > 0)
        {
            var schemaList = new List<Value>();
            for (var i = 0; i < arr.Count; i++)
            {
                var val = arr[i];
                switch (val.TypeTag)
                {
                    case TypeTag.String:
                    {
                        var typeSchema = new Dictionary
                        {
                            { "type", val }
                        };
                        schemaList.Add(new Value(typeSchema));
                        break;
                    }
                    case TypeTag.Dictionary:
                    {
                        var schemaObj = MapToSchema(val) ?? new Value();
                        schemaList.Add(schemaObj);
                        break;
                    }
                    default:
                        throw new JsonSchemaBuilderException($"Property must be a string or object.");
                }
            }
            return new Value(schemaList, TypeTag.Array);
        }
        return null;
    }

    private static Value? GetExclusive(Value value)
    {
        if (value.TypeTag == TypeTag.Boolean && value.AsBoolean())
        {
            var properties = value.Parent;
            if (properties is { TypeTag: TypeTag.String })
            {
                var parentSchema = properties.Parent;
                if (parentSchema?.AsDictionary() is {} parentObject)
                {
                    var dependentProp = properties.AsString().Replace("exclusive", "").ToLower();
                    if (parentObject.ContainsKey(dependentProp))
                    {
                        var minMax = parentObject[dependentProp];
                        if (minMax.TypeTag is TypeTag.Int or TypeTag.Float)
                            return new Value(minMax.AsInt());
                    }
                }
            }
        }
        return null;
    }

    private Value? AddRequiredProperty(Value value)
    {
        if (value.TypeTag == TypeTag.Boolean && value.AsBoolean())
        {
            var ancestor = value;
            var property = value;
            var ax = 0;
            var px = 0;
            while (ax < 6 && ancestor != null)
            {
                ax++;
                ancestor = ancestor.Parent;
                if (++px == 3)
                    property = ancestor;
            }
            if (ancestor != null &&
                property != null &&
                ancestor.TypeTag == TypeTag.Dictionary &&
                property.TypeTag == TypeTag.String)
            {
                var obj = ancestor.AsDictionary();
                var prop = property.AsString();
                Value? required = null;
                _ = obj?.TryGetValue("required", out required);
                if (required == null)
                {
                    List<Value> reqArray = [new Value(prop)];
                    obj?.Add("required", new Value(reqArray, TypeTag.Array));
                }
                else
                {
                    var reqArr = required.AsArray();
                    reqArr?.Add(new Value(prop));
                }
            }
        }
        return null;
    }
}
