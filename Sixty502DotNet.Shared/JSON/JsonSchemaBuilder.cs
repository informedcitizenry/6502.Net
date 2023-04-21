//-----------------------------------------------------------------------------
// Copyright (c) 2017-2023 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using Antlr4.Runtime;

using TransformFunc = System.Func<Sixty502DotNet.Shared.ValueBase, Sixty502DotNet.Shared.ValueBase?>;

namespace Sixty502DotNet.Shared;

/// <summary>
/// Builds a schema from a specified JSON string. 
/// </summary>
public sealed class JsonSchemaBuilder
{
    /// <summary>
    /// Represents a collection of keyword mappings a source draft schema to draft 2020-12, including
    /// the source keyword name, the associated target draft's keyword name, and the logic 
    /// to transform the draft keyword's value to the target's keyword value.
    /// </summary>
    public class KeywordMapper
    {
        readonly struct Mapping
        {
            public string KeywordName { get; }

            public TransformFunc Transform { get; }

            public Mapping(string keywordName)
                => (KeywordName, Transform, RaiseExceptionOnTypeMismatch) =
                 (keywordName, (ValueBase jin) => jin, true);

            public Mapping(string propertyName, TransformFunc transform)
                => (KeywordName, Transform, RaiseExceptionOnTypeMismatch) = (propertyName, transform, true);

            public Mapping(string propertyName, TransformFunc transform, bool raiseExceptionOnTypeMismatch)
                => (KeywordName, Transform, RaiseExceptionOnTypeMismatch) = (propertyName, transform, raiseExceptionOnTypeMismatch);

            public bool RaiseExceptionOnTypeMismatch { get; }

            public override string ToString() => KeywordName;
        }

        readonly Dictionary<(string, ValueType), Mapping> _typeMapping;

        /// <summary>
        /// Creates a new instance of a <see cref="KeywordMapper"/> class.
        /// </summary>
        public KeywordMapper()
            => _typeMapping = new Dictionary<(string, ValueType), Mapping>();

        /// <summary>
        /// Defines mapping between a draft keyword to its associated draft 2020-12 keyword.
        /// </summary>
        /// <param name="name">The draft and target keyword name.</param>
        /// <param name="type">The draft and target keyword type.</param>
        /// <returns>This mapper.</returns>
        public KeywordMapper Map(string name, ValueType type)
            => Map(name, type, name);

        /// <summary>
        /// Defines mapping between a draft keyword to its associated draft 2020-12 keyword.
        /// </summary>
        /// <param name="draftName">The draft keyword name.</param>
        /// <param name="type">The draft and target keyword type.</param>
        /// <param name="keywordName">The target keyword name.</param>
        /// <returns>This mapper.</returns>
        public KeywordMapper Map(string draftName, ValueType type, string keywordName)
        {
            _typeMapping[(draftName, type)] = new Mapping(keywordName);
            return this;
        }

        /// <summary>
        /// Defines mapping between a draft keyword to its associated draft 2020-12 keyword.
        /// </summary>
        /// <param name="name">The draft and target keyword name.</param>
        /// <param name="type">The draft and target keyword type.</param>
        /// <param name="transform">The function to transform the draft keyword value
        /// to the associated target's keyword value.</param>
        /// <returns>This mapper.</returns>
        public KeywordMapper Map(string name, ValueType type, TransformFunc transform)
            => Map(name, type, name, transform, true);

        /// <summary>
        /// Defines mapping between a draft keyword to its associated draft 2020-12 keyword.
        /// </summary>
        /// <param name="draftName">The draft keyword name.</param>
        /// <param name="type">The draft and target keyword type.</param>
        /// <param name="keywordName">The target keyword name.</param>
        /// <param name="transform">The function to transform the draft keyword value
        /// to the associated target's keyword value.</param>
        /// <returns>This mapper.</returns>
        public KeywordMapper Map(string draftName, ValueType type, string keywordName, TransformFunc transform)
            => Map(draftName, type, keywordName, transform, true);

        /// <summary>
        /// Defines mapping between a draft keyword to its associated draft 2020-12 keyword.
        /// </summary>
        /// <param name="name">The draft and target keyword name.</param>
        /// <param name="type">The draft and target keyword type.</param>
        /// <param name="transform">The function to transform the draft keyword value
        /// to the associated target's keyword value.</param>
        /// <param name="raiseExceptionOnTypeMismatch">Raise an exception if source draft's keyword value.
        /// is an unexpected type.</param>
        /// <returns>This mapper.</returns>
        public KeywordMapper Map(string name, ValueType type, TransformFunc transform, bool raiseExceptionOnTypeMismatch)
            => Map(name, type, name, transform, raiseExceptionOnTypeMismatch);

        /// <summary>
        /// Defines mapping between a draft keyword to its associated draft 2020-12 keyword.
        /// </summary>
        /// <param name="draftName">The draft keyword name.</param>
        /// <param name="type">The draft and target keyword type.</param>
        /// <param name="keywordName">The target keyword name.</param>
        /// <param name="transform">The function to transform the draft keyword value
        /// to the associated target's keyword value.</param>
        /// <param name="raiseExceptionOnTypeMismatch">Raise an exception if source draft's keyword value.
        /// <returns>This mapper.</returns>
        public KeywordMapper Map(string draftName, ValueType type, string keywordName, TransformFunc transform, bool raiseExceptionOnTypeMismatch)
        {
            _typeMapping[(draftName, type)] = new Mapping(keywordName, transform, raiseExceptionOnTypeMismatch);
            return this;
        }

        /// <summary>
        /// Combine the mappings of two <see cref="KeywordMapper"/> instances.
        /// </summary>
        /// <param name="other">The other <see cref="KeywordMapper"/> to combine.</param>
        /// <returns>This mapper.</returns>
        public KeywordMapper CombineWith(KeywordMapper other)
        {
            other._typeMapping.ToList().ForEach(kvp => _typeMapping[kvp.Key] = kvp.Value);
            return this;
        }

        /// <summary>
        /// Transform the source draft's keyword value to its associated target draft's 
        /// value.
        /// </summary>
        /// <param name="draftName">The draft keyword name.</param>
        /// <param name="token">The parsed <see cref="JToken"/> JSON value.</param>
        /// <returns></returns>
        public (string? propertyName, ValueBase? value) Transform(string draftName, ValueBase? token)
        {
            ValueType type = token?.ValueType ?? ValueType.Null;
            if (token != null)
            {
                if (_typeMapping.TryGetValue((draftName, type), out var mapping) ||
                _typeMapping.TryGetValue((draftName, ValueType.Dictionary), out mapping))
                    return (mapping.KeywordName, mapping.Transform(token));
            }
            var expected = _typeMapping.Keys.FirstOrDefault(nameType => nameType.Item1.Equals(draftName));
            if (_typeMapping.TryGetValue(expected, out var expectedMapping) && expectedMapping.RaiseExceptionOnTypeMismatch)
                throw new JsonSchemaBuilderException($"Property '{draftName}' expects a different type from '{token?.ValueType.ToString().ToLower() ?? "null"}'.", token);
            return (null, null);
        }
    }

    private KeywordMapper _mapper;

    public JsonSchemaBuilder() => _mapper = new KeywordMapper();

    KeywordMapper Draft3()
            => new KeywordMapper().Map("type", ValueType.Array, "anyOf", AnyOfType)
                                  .Map("divisibleBy", ValueType.Integer, "multipleOf")
                                  .Map("divisibleBy", ValueType.Number, "multipleOf")
                                  .Map("disallow", ValueType.Array, "not", NotAnyOf)
                                  .Map("disallow", ValueType.String, "not", NotType)
                                  .Map("extends", ValueType.Dictionary, "allOf", Extends)
                                  .Map("extends", ValueType.Array, "allOf", ArrayOfSchemasDraft3And4)
                                  .CombineWith(Drafts3And4())
                                  .CombineWith(AllDraftsPre2020Mapper())
                                  .CombineWith(AllDraftsMapper());

    KeywordMapper Draft4()
        => new KeywordMapper().Map("allOf", ValueType.Array, ArrayOfSchemasPostDraft4)
                              .Map("anyOf", ValueType.Array, ArrayOfSchemasPostDraft4)
                              .Map("oneOf", ValueType.Array, ArrayOfSchemasPostDraft4)
                              .CombineWith(Drafts3And4())
                              .CombineWith(AllDraftsPost3())
                              .CombineWith(AllDraftsPre2020Mapper())
                              .CombineWith(AllDraftsMapper());

    KeywordMapper Draft6()
        => Drafts6And7().CombineWith(AllDraftsPost4Pre2020())
                        .CombineWith(AllDraftsMapper());

    KeywordMapper Draft7()
        => Drafts6And7().CombineWith(AllDraftsPost4Pre2020())
                        .CombineWith(AllDraftsPost6())
                        .CombineWith(AllDraftsMapper());

    KeywordMapper Draft2019()
        => new KeywordMapper().Map("$recursiveAnchor", ValueType.Boolean, "$dynamicAnchor", (t) => new StringValue("\"\""))
                              .Map("$recursiveRef", ValueType.String, "$dynamicRef")
                              .CombineWith(AllDraftsPost4Pre2020())
                              .CombineWith(AllDraftsPost7())
                              .CombineWith(AllDraftsMapper());

    KeywordMapper AllSchemas()
        => new KeywordMapper().Map("$dynamicAnchor", ValueType.String)
                              .Map("$dynamicRef", ValueType.String)
                              .Map("contentSchema", ValueType.Dictionary, MapToSchema)
                              .Map("contentSchema", ValueType.Boolean)
                              .Map("prefixItems", ValueType.Array, ArrayOfSchemasPostDraft4)
                              .CombineWith(Draft3())
                              .CombineWith(Drafts6And7())
                              .CombineWith(Draft2019());

    KeywordMapper AllDraftsPost7()
        => new KeywordMapper().Map("$anchor", ValueType.String)
                              .Map("$defs", ValueType.Dictionary, ObjectOfSchemasPostDraft4)
                              .Map("dependentSchemas", ValueType.Dictionary, ObjectOfSchemasPostDraft4)
                              .Map("dependentRequired", ValueType.Dictionary)
                              .Map("unevaluatedItems", ValueType.Dictionary, MapToSchema)
                              .Map("unevaluatedItems", ValueType.Boolean)
                              .Map("unevaluatedProperties", ValueType.Dictionary, MapToSchema)
                              .Map("unevaluatedProperties", ValueType.Boolean)
                              .Map("minContains", ValueType.Integer)
                              .Map("maxContains", ValueType.Integer)
                              .CombineWith(AllDraftsPost6());

    KeywordMapper AllDraftsPost6()
        => new KeywordMapper().Map("contentEncoding", ValueType.String)
                              .Map("contentMediaType", ValueType.String)
                              .Map("if", ValueType.Dictionary, MapToSchema)
                              .Map("if", ValueType.Boolean)
                              .Map("then", ValueType.Dictionary, MapToSchema)
                              .Map("then", ValueType.Boolean)
                              .Map("else", ValueType.Dictionary, MapToSchema)
                              .Map("else", ValueType.Boolean);

    KeywordMapper Drafts6And7()
        => new KeywordMapper().Map("definitions", ValueType.Dictionary, "$defs", ObjectOfSchemasPostDraft4)
                              .Map("dependencies", ValueType.Dictionary, "dependentSchemas", DependenciesDrafts6And7);

    KeywordMapper AllDraftsPost4Pre2020()
        => new KeywordMapper().Map("items", ValueType.Array, "prefixItems", ArrayOfSchemasPostDraft4)
                              .CombineWith(AllDraftsPost3())
                              .CombineWith(AllDraftsPost4())
                              .CombineWith(AllDraftsPre2020Mapper())
                              .CombineWith(AllDraftsMapper());

    KeywordMapper AllDraftsPost4()
        => new KeywordMapper().Map("$id", ValueType.String)
                              .Map("properties", ValueType.Dictionary, ObjectOfSchemasPostDraft4)
                              .Map("patternProperties", ValueType.Dictionary, ObjectOfSchemasPostDraft4)
                              .Map("propertyNames", ValueType.Dictionary, MapToSchema)
                              .Map("propertyNames", ValueType.Boolean)
                              .Map("items", ValueType.Boolean)
                              .Map("required", ValueType.Array)
                              .Map("contains", ValueType.Dictionary, MapToSchema)
                              .Map("contains", ValueType.Boolean)
                              .Map("exclusiveMaximum", ValueType.Integer)
                              .Map("exclusiveMinimum", ValueType.Number)
                              .Map("exclusiveMinimum", ValueType.Integer)
                              .Map("exclusiveMaximum", ValueType.Number)
                              .Map("minProperties", ValueType.Integer)
                              .Map("maxProperties", ValueType.Integer)
                              .Map("allOf", ValueType.Array, ArrayOfSchemasPostDraft4)
                              .Map("anyOf", ValueType.Array, ArrayOfSchemasPostDraft4)
                              .Map("oneOf", ValueType.Array, ArrayOfSchemasPostDraft4)
                              .Map("not", ValueType.Boolean)
                              .Map("const", ValueType.Raw);
    KeywordMapper AllDraftsPost3()
        => new KeywordMapper().Map("multipleOf", ValueType.Integer)
                              .Map("multipleOf", ValueType.Number)
                              .Map("not", ValueType.Dictionary, MapToSchema)
                              .Map("type", ValueType.Array);

    KeywordMapper Drafts3And4()
        => new KeywordMapper().Map("required", ValueType.Boolean, AddRequiredProperty, false)
                              .Map("properties", ValueType.Dictionary, ObjectOfSchemasDraft3And4)
                              .Map("patternProperties", ValueType.Dictionary, ObjectOfSchemasDraft3And4)
                              .Map("exclusiveMinimum", ValueType.Boolean, GetExclusive)
                              .Map("exclusiveMaximum", ValueType.Boolean, GetExclusive)
                              .Map("id", ValueType.String, "$id")
                              .Map("dependencies", ValueType.Dictionary, "dependentSchemas", DependenciesDraft3And4)
                              .Map("items", ValueType.Array, "prefixItems", ArrayOfSchemasDraft3And4);

    KeywordMapper AllDraftsPre2020Mapper()
        => new KeywordMapper().Map("additionalItems", ValueType.Dictionary, "items", MapToSchema)
                              .Map("additionalItems", ValueType.Boolean, "items");

    KeywordMapper AllDraftsMapper()
        => new KeywordMapper().Map("additionalProperties", ValueType.Dictionary, MapToSchema)
                              .Map("additionalProperties", ValueType.Boolean)
                              .Map("items", ValueType.Dictionary, MapToSchema)
                              .Map("minimum", ValueType.Integer)
                              .Map("minimum", ValueType.Number)
                              .Map("maximum", ValueType.Integer)
                              .Map("maximum", ValueType.Number)
                              .Map("minItems", ValueType.Integer)
                              .Map("maxItems", ValueType.Integer)
                              .Map("uniqueItems", ValueType.Boolean)
                              .Map("pattern", ValueType.String)
                              .Map("format", ValueType.String)
                              .Map("minLength", ValueType.Integer)
                              .Map("maxLength", ValueType.Integer)
                              .Map("enum", ValueType.Array)
                              .Map("$ref", ValueType.String)
                              .Map("$schema", ValueType.String)
                              .Map("type", ValueType.String);

    ValueBase? DependenciesDrafts6And7(ValueBase value)
            => Dependencies(value, false);

    ValueBase? DependenciesDraft3And4(ValueBase value)
        => Dependencies(value, true);

    ValueBase? ObjectOfSchemasPostDraft4(ValueBase value)
    => ObjectOfSchemas(value, true);

    ValueBase? ObjectOfSchemasDraft3And4(ValueBase value)
        => ObjectOfSchemas(value, false);

    ValueBase? ArrayOfSchemasPostDraft4(ValueBase value)
            => ArrayOfSchemas(value, true);

    ValueBase? ArrayOfSchemasDraft3And4(ValueBase value)
        => ArrayOfSchemas(value, false);


    ValueBase? MapToSchema(ValueBase? schemaToken)
    {
        if (schemaToken is JsonObject jSchemaObject && schemaToken.Count > 0)
        {
            var schemaResult = new JsonObject();
            for (var i = 0; i < jSchemaObject.Properties().Count(); i++)
            {
                var prop = jSchemaObject.Properties().ElementAt(i);
                var (propertyName, propertyValue) = _mapper.Transform(prop, jSchemaObject[prop]);
                if (propertyName == null || propertyValue == null)
                    continue;
                schemaResult.Add(propertyName, propertyValue);
            }
            if (jSchemaObject.TryGetValue("required", out var requiredToken) && requiredToken?.ValueType == ValueType.Array)
                schemaResult.Add("required", requiredToken);
            return schemaResult;
        }
        return null;
    }

    ValueBase? Dependencies(ValueBase value, bool postDraft4)
    {
        if (value.IsDefined && value is JsonObject dependentObj)
        {
            var dependentSchemas = new JsonObject();
            foreach (var prop in dependentObj)
            {
                var val = prop.Value;
                ValueBase? dependentSchema = null;
                if ((val?.ValueType == ValueType.String && !postDraft4) || val?.ValueType == ValueType.Array)
                {
                    JsonArray reqArray;
                    if (val.ValueType == ValueType.String)
                    {
                        reqArray = new JsonArray
                        {
                            val
                        };
                    }
                    else
                    {
                        reqArray = new JsonArray((JsonArray)val);
                    }
                    dependentSchema = new JsonObject("required", reqArray);
                }
                else if (val?.ValueType == ValueType.Dictionary)
                {
                    dependentSchema = MapToSchema(val);
                }
                else if (val?.ValueType == ValueType.Boolean && postDraft4)
                {
                    dependentSchema = new BoolValue(val.AsBool());
                }
                else
                {
                    throw new JsonSchemaBuilderException($"Property '{prop}' type is invalid.", val);
                }
                if (dependentSchema != null)
                    dependentSchemas.Add(prop.Key, dependentSchema);
            }
            return dependentSchemas;
        }
        return null;
    }

    ValueBase? ObjectOfSchemas(ValueBase value, bool postDraft4)
    {
        if (value is JsonObject obj && value.Count > 0)
        {
            var objSchemas = new JsonObject();
            foreach (var prop in obj)
            {
                var schemaToken = prop.Value;
                ValueBase? schema = null;
                if (schemaToken?.ValueType == ValueType.Dictionary)
                    schema = MapToSchema(schemaToken);
                else if (schemaToken?.ValueType == ValueType.Boolean && postDraft4)
                    schema = new BoolValue(schemaToken.AsBool());
                else
                    throw new JsonSchemaBuilderException($"Property '{prop}' type is invalid.", schemaToken);
                if (schema != null)
                    objSchemas.Add(prop.Key, schema);
            }
            return objSchemas;
        }
        return null;
    }

    ValueBase? ArrayOfSchemas(ValueBase value, bool postDraft4)
    {
        if (value is JsonArray arr && value.Count > 0)
        {
            var schemaList = new JsonArray();
            for (var i = 0; i < arr.Count; i++)
            {
                if (arr[i]?.ValueType == ValueType.Dictionary)
                    schemaList.Add(MapToSchema(arr[i]));
                else if (arr[i]?.ValueType == ValueType.Boolean && postDraft4)
                    schemaList.Add(new BoolValue(arr[i].AsBool()));
                else
                    throw new JsonSchemaBuilderException($"Schema must be an object.", arr[i]);
            }
            return schemaList;
        }
        return null;
    }

    ValueBase? Extends(ValueBase value)
    {
        JsonArray arr = new()
        {
            value
        };
        return ArrayOfSchemas(arr, false);
    }

    ValueBase? NotAnyOf(ValueBase value)
    {
        if (value is JsonArray arr && value.Count > 0)
        {
            var notSchema = new JsonObject
            {
                { "type", arr }
            };
            return MapToSchema(notSchema);
        }
        return null;
    }

    ValueBase? NotType(ValueBase value)
    {
        JsonObject obj = new()
        {
            { "type", value }
        };
        return obj;
    }


    ValueBase? AnyOfType(ValueBase value)
    {
        if (value is JsonArray arr && value.Count > 0)
        {
            var schemaList = new JsonArray();
            for (var i = 0; i < arr.Count; i++)
            {
                var val = arr[i];
                if (val?.ValueType == ValueType.String)
                {
                    var typeSchema = new JsonObject
                    {
                        { "type", val }
                    };
                    schemaList.Add(typeSchema);
                }
                else if (val?.ValueType == ValueType.Dictionary)
                {
                    var schemaObj = MapToSchema(val);
                    schemaList.Add(schemaObj!);
                }
                else
                    throw new JsonSchemaBuilderException($"Property must be a string or object.", val);
            }
            return schemaList;
        }
        return null;
    }

    ValueBase? GetExclusive(ValueBase value)
    {
        if (value.ValueType == ValueType.Boolean && value.AsBool())
        {
            var properties = value.Parent;
            if (properties != null && properties.ValueType == ValueType.String)
            {
                var parentSchema = properties.Parent;
                if (parentSchema is JsonObject parentObject)
                {

                    var dependentProp = properties.AsString().Replace("exclusive", "").ToLower();
                    if (parentObject.ContainsKey(dependentProp))
                    {
                        var minMax = parentObject[dependentProp]!;
                        if (minMax.ValueType == ValueType.Integer || minMax.ValueType == ValueType.Number)
                            return new NumericValue(minMax.AsInt());
                    }
                }
            }
        }
        return null;
    }

    ValueBase? AddRequiredProperty(ValueBase value)
    {
        if (value.ValueType == ValueType.Boolean && value.AsBool())
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
                ancestor.ValueType == ValueType.Dictionary &&
                property.ValueType == ValueType.String)
            {
                var obj = (JsonObject)ancestor;
                var prop = property.AsString();
                var required = obj["required"];
                if (required == null)
                {
                    JsonArray reqArray = new()
                    {
                        new StringValue(prop)
                    };
                    obj.Add("required", reqArray);
                }
                else
                {
                    var reqArr = (JsonArray)required;
                    reqArr.Add(new StringValue(prop));
                }
            }
        }
        return null;
    }

    /// <summary>
    /// Build a draft 2020-12 <see cref="Schema"/> from a JSON string the JSON schema of
    /// any of the following drafts: draft3, draft4, draft6, draft7, draft 2019-09, and
    /// draft 2012-12.
    /// </summary>
    /// <param name="draftSchemaJson">The draft schema's JSON.</param>
    /// <param name="allowAllIfNotSpecified">Indicate whether to allow all schema draft
    /// keywords if the draft schema has not defined its '$schema' source type.</param>
    /// <returns>A draft 2020-12 <see cref="Schema"/> object ready to validate a JSON instance.</returns>
    /// <exception cref="SchemaBuilderException"></exception>
    public JsonSchema BuildFromJson(string draftSchemaJson, bool allowAllIfNotSpecified)
    {
        JsonDeserializer parser = new();
        var token = parser.Deserialize(draftSchemaJson);
        if (parser.Errors.Count > 0)
        {
            IToken? exc = parser.Errors[0].Token;

            throw new JsonSchemaBuilderException($"Parsing error at {exc?.Line ?? 0}:{exc?.Column ?? 0}.", exc?.Text ?? "#");
        }
        var schemaUri = string.Empty;
        if (token is JsonObject obj)
        {
            var schemaUriToken = obj["$schema"];
            if (schemaUriToken != null && schemaUriToken.ValueType == ValueType.String)
                schemaUri = schemaUriToken.AsString();
        }
        if ((!allowAllIfNotSpecified && string.IsNullOrEmpty(schemaUri)) ||
            schemaUri.Equals("https://json-schema.org/draft/2020-12/schema"))
            return new JsonSchema(token);
        _mapper = schemaUri.TrimEndOnce('#') switch
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
}

