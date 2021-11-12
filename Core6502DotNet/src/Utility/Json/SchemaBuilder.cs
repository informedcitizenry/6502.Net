//-----------------------------------------------------------------------------
// Copyright (c) 2017-2021 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using TransformFunc = System.Func<Newtonsoft.Json.Linq.JToken, Newtonsoft.Json.Linq.JToken>;

namespace Core6502DotNet.Json
{
    /// <summary>
    /// A factory class for building draft 2020-12 JSON schemas from other JSON schema versions.
    /// </summary>
    public class SchemaBuilder
    {
        #region Subclasses

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
                     (keywordName, (JToken jin) => jin, true);

                public Mapping(string propertyName, TransformFunc transform)
                    => (KeywordName, Transform, RaiseExceptionOnTypeMismatch) = (propertyName, transform, true);

                public Mapping(string propertyName, TransformFunc transform, bool raiseExceptionOnTypeMismatch)
                    => (KeywordName, Transform, RaiseExceptionOnTypeMismatch) = (propertyName, transform, raiseExceptionOnTypeMismatch);

                public bool RaiseExceptionOnTypeMismatch { get; }

                public override string ToString() => KeywordName;
            }

            readonly Dictionary<(string, JTokenType), Mapping> _typeMapping;

            /// <summary>
            /// Creates a new instance of a <see cref="KeywordMapper"/> class.
            /// </summary>
            public KeywordMapper()
                => _typeMapping = new Dictionary<(string, JTokenType), Mapping>();

            /// <summary>
            /// Defines mapping between a draft keyword to its associated draft 2020-12 keyword.
            /// </summary>
            /// <param name="name">The draft and target keyword name.</param>
            /// <param name="type">The draft and target keyword type.</param>
            /// <returns>This mapper.</returns>
            public KeywordMapper Map(string name, JTokenType type)
                => Map(name, type, name);

            /// <summary>
            /// Defines mapping between a draft keyword to its associated draft 2020-12 keyword.
            /// </summary>
            /// <param name="draftName">The draft keyword name.</param>
            /// <param name="type">The draft and target keyword type.</param>
            /// <param name="keywordName">The target keyword name.</param>
            /// <returns>This mapper.</returns>
            public KeywordMapper Map(string draftName, JTokenType type, string keywordName)
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
            public KeywordMapper Map(string name, JTokenType type, TransformFunc transform)
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
            public KeywordMapper Map(string draftName, JTokenType type, string keywordName, TransformFunc transform)
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
            public KeywordMapper Map(string name, JTokenType type, TransformFunc transform, bool raiseExceptionOnTypeMismatch)
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
            public KeywordMapper Map(string draftName, JTokenType type, string keywordName, TransformFunc transform, bool raiseExceptionOnTypeMismatch)
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
            public (string propertyName, JToken value) Transform(string draftName, JToken token)
            {
                if (_typeMapping.TryGetValue((draftName, token.Type), out var mapping) ||
                    _typeMapping.TryGetValue((draftName, JTokenType.Raw), out mapping))
                    return (mapping.KeywordName, mapping.Transform(token));
                var expected = _typeMapping.Keys.FirstOrDefault(nameType => nameType.Item1.Equals(draftName));
                if (_typeMapping.TryGetValue(expected, out var expectedMapping) && expectedMapping.RaiseExceptionOnTypeMismatch)
                    throw new SchemaBuilderException($"Property '{draftName}' expects a different type from '{token.Type.SchemaType()}'.", token);
                return (null, null);
            }
        }

        #endregion

        #region Members

        KeywordMapper _mapper;

        #endregion

        #region Methods

        KeywordMapper Draft3()
            => new KeywordMapper().Map("type", JTokenType.Array, "anyOf", AnyOfType)
                                  .Map("divisibleBy", JTokenType.Integer, "multipleOf")
                                  .Map("divisibleBy", JTokenType.Float, "multipleOf")
                                  .Map("disallow", JTokenType.Array, "not", NotAnyOf)
                                  .Map("disallow", JTokenType.String, "not", NotType)
                                  .Map("extends", JTokenType.Object, "allOf", Extends)
                                  .Map("extends", JTokenType.Array, "allOf", ArrayOfSchemasDraft3And4)
                                  .CombineWith(Drafts3And4())
                                  .CombineWith(AllDraftsPre2020Mapper())
                                  .CombineWith(AllDraftsMapper());

        KeywordMapper Draft4()
            => new KeywordMapper().Map("allOf", JTokenType.Array, ArrayOfSchemasPostDraft4)
                                  .Map("anyOf", JTokenType.Array, ArrayOfSchemasPostDraft4)
                                  .Map("oneOf", JTokenType.Array, ArrayOfSchemasPostDraft4)
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
            => new KeywordMapper().Map("$recursiveAnchor", JTokenType.Boolean, "$dynamicAnchor", (t) => string.Empty)
                                  .Map("$recursiveRef", JTokenType.String, "$dynamicRef")
                                  .CombineWith(AllDraftsPost4Pre2020())
                                  .CombineWith(AllDraftsPost7())
                                  .CombineWith(AllDraftsMapper());

        KeywordMapper AllSchemas()
            => new KeywordMapper().Map("$dynamicAnchor", JTokenType.String)
                                  .Map("$dynamicRef", JTokenType.String)
                                  .Map("contentSchema", JTokenType.Object, MapToSchema)
                                  .Map("contentSchema", JTokenType.Boolean)
                                  .Map("prefixItems", JTokenType.Array, ArrayOfSchemasPostDraft4)
                                  .CombineWith(Draft3())
                                  .CombineWith(Drafts6And7())
                                  .CombineWith(Draft2019());

        KeywordMapper AllDraftsPost7()
            => new KeywordMapper().Map("$anchor", JTokenType.String)
                                  .Map("$defs", JTokenType.Object, ObjectOfSchemasPostDraft4)
                                  .Map("dependentSchemas", JTokenType.Object, ObjectOfSchemasPostDraft4)
                                  .Map("dependentRequired", JTokenType.Object)
                                  .Map("unevaluatedItems", JTokenType.Object, MapToSchema)
                                  .Map("unevaluatedItems", JTokenType.Boolean)
                                  .Map("unevaluatedProperties", JTokenType.Object, MapToSchema)
                                  .Map("unevaluatedProperties", JTokenType.Boolean)
                                  .Map("minContains", JTokenType.Integer)
                                  .Map("maxContains", JTokenType.Integer)
                                  .CombineWith(AllDraftsPost6());

        KeywordMapper AllDraftsPost6()
            => new KeywordMapper().Map("contentEncoding", JTokenType.String)
                                  .Map("contentMediaType", JTokenType.String)
                                  .Map("if", JTokenType.Object, MapToSchema)
                                  .Map("if", JTokenType.Boolean)
                                  .Map("then", JTokenType.Object, MapToSchema)
                                  .Map("then", JTokenType.Boolean)
                                  .Map("else", JTokenType.Object, MapToSchema)
                                  .Map("else", JTokenType.Boolean);

        KeywordMapper Drafts6And7()
            => new KeywordMapper().Map("definitions", JTokenType.Object, "$defs", ObjectOfSchemasPostDraft4)
                                  .Map("dependencies", JTokenType.Object, "dependentSchemas", DependenciesDrafts6And7);

        KeywordMapper AllDraftsPost4Pre2020()
            => new KeywordMapper().Map("items", JTokenType.Array, "prefixItems", ArrayOfSchemasPostDraft4)
                                  .CombineWith(AllDraftsPost3())
                                  .CombineWith(AllDraftsPost4())
                                  .CombineWith(AllDraftsPre2020Mapper())
                                  .CombineWith(AllDraftsMapper());

        KeywordMapper AllDraftsPost4()
            => new KeywordMapper().Map("$id", JTokenType.String)
                                  .Map("properties", JTokenType.Object, ObjectOfSchemasPostDraft4)
                                  .Map("patternProperties", JTokenType.Object, ObjectOfSchemasPostDraft4)
                                  .Map("propertyNames", JTokenType.Object, MapToSchema)
                                  .Map("propertyNames", JTokenType.Boolean)
                                  .Map("items", JTokenType.Boolean)
                                  .Map("required", JTokenType.Array)
                                  .Map("contains", JTokenType.Object, MapToSchema)
                                  .Map("contains", JTokenType.Boolean)
                                  .Map("exclusiveMaximum", JTokenType.Integer)
                                  .Map("exclusiveMinimum", JTokenType.Float)
                                  .Map("exclusiveMinimum", JTokenType.Integer)
                                  .Map("exclusiveMaximum", JTokenType.Float)
                                  .Map("minProperties", JTokenType.Integer)
                                  .Map("maxProperties", JTokenType.Integer)
                                  .Map("allOf", JTokenType.Array, ArrayOfSchemasPostDraft4)
                                  .Map("anyOf", JTokenType.Array, ArrayOfSchemasPostDraft4)
                                  .Map("oneOf", JTokenType.Array, ArrayOfSchemasPostDraft4)
                                  .Map("not", JTokenType.Boolean)
                                  .Map("const", JTokenType.Raw);
        KeywordMapper AllDraftsPost3()
            => new KeywordMapper().Map("multipleOf", JTokenType.Integer)
                                  .Map("not", JTokenType.Object, MapToSchema)
                                  .Map("type", JTokenType.Array);

        KeywordMapper Drafts3And4()
            => new KeywordMapper().Map("required", JTokenType.Boolean, AddRequiredProperty, false)
                                  .Map("properties", JTokenType.Object, ObjectOfSchemasDraft3And4)
                                  .Map("patternProperties", JTokenType.Object, ObjectOfSchemasDraft3And4)
                                  .Map("exclusiveMinimum", JTokenType.Boolean, GetExclusive)
                                  .Map("exclusiveMaximum", JTokenType.Boolean, GetExclusive)
                                  .Map("id", JTokenType.String, "$id")
                                  .Map("dependencies", JTokenType.Object, "dependentSchemas", DependenciesDraft3And4)
                                  .Map("items", JTokenType.Array, "prefixItems", ArrayOfSchemasDraft3And4);

        KeywordMapper AllDraftsPre2020Mapper()
            => new KeywordMapper().Map("additionalItems", JTokenType.Object, "items", MapToSchema)
                                  .Map("additionalItems", JTokenType.Boolean, "items");

        KeywordMapper AllDraftsMapper()
            => new KeywordMapper().Map("additionalProperties", JTokenType.Object, MapToSchema)
                                  .Map("additionalProperties", JTokenType.Boolean)
                                  .Map("items", JTokenType.Object, MapToSchema)
                                  .Map("minimum", JTokenType.Integer)
                                  .Map("minimum", JTokenType.Float)
                                  .Map("maximum", JTokenType.Integer)
                                  .Map("maximum", JTokenType.Float)
                                  .Map("minItems", JTokenType.Integer)
                                  .Map("maxItems", JTokenType.Integer)
                                  .Map("uniqueItems", JTokenType.Boolean)
                                  .Map("pattern", JTokenType.String)
                                  .Map("format", JTokenType.String)
                                  .Map("minLength", JTokenType.Integer)
                                  .Map("maxLength", JTokenType.Integer)
                                  .Map("enum", JTokenType.Array)
                                  .Map("$ref", JTokenType.String)
                                  .Map("$schema", JTokenType.String)
                                  .Map("type", JTokenType.String);

        JToken Dependencies(JToken value, bool postDraft4)
        {
            if (value.HasValues && value is JObject dependentObj)
            {
                var dependentSchemas = new JObject();
                var props = dependentObj.Properties();
                foreach(var prop in props)
                {
                    var val = prop.Value;
                    JToken dependentSchema = null;
                    if ((val.Type == JTokenType.String && !postDraft4) || val.Type == JTokenType.Array)
                    {
                        JArray reqArray;
                        if (val.Type == JTokenType.String)
                            reqArray = new JArray { val };
                        else
                            reqArray = new JArray((JArray)val);
                        dependentSchema = new JObject(new JProperty("required"), reqArray);
                    }
                    else if (val.Type == JTokenType.Object)
                    {
                        dependentSchema = MapToSchema(val);
                    }
                    else if (val.Type == JTokenType.Boolean && postDraft4)
                    {
                        dependentSchema = new JValue(val);
                    }
                    else
                    {
                        throw new SchemaBuilderException($"Property '{prop.Name}' type is invalid.", val);
                    }
                    if (dependentSchema != null)
                        dependentSchemas.Add(prop.Name, dependentSchema);
                }
                return dependentSchemas;
            }
            return null;
        }

        JToken DependenciesDrafts6And7(JToken value)
            => Dependencies(value, false);

        JToken DependenciesDraft3And4(JToken value)
            => Dependencies(value, true);

        JToken ObjectOfSchemas(JToken value, bool postDraft4)
        {
            if (value is JObject obj && value.HasValues)
            {
                var objSchemas = new JObject();
                var props = obj.Properties();
                foreach(var prop in props)
                {
                    var schemaToken = prop.Value;
                    JToken schema = null;
                    if (schemaToken.Type == JTokenType.Object)
                        schema = MapToSchema(schemaToken);
                    else if (schemaToken.Type == JTokenType.Boolean && postDraft4)
                        schema = new JValue(schemaToken);
                    else
                        throw new SchemaBuilderException($"Property '{prop.Name}' type is invalid.", schemaToken);
                    if (schema != null)
                        objSchemas.Add(prop.Name, schema);
                }
                return objSchemas;
            }
            return null;
        }

        JToken ObjectOfSchemasPostDraft4(JToken value)
            => ObjectOfSchemas(value, true);

        JToken ObjectOfSchemasDraft3And4(JToken value)
            => ObjectOfSchemas(value, false);


        JToken ArrayOfSchemas(JToken value, bool postDraft4)
        {
            if (value.HasValues && value is JArray arr)
            {
                var schemaList = new JArray();
                for(var i = 0; i < arr.Count; i++)
                {
                    var schemaToken = arr[i];
                    if (schemaToken.Type == JTokenType.Object)
                        schemaList.Add(MapToSchema(schemaToken));
                    else if (schemaToken.Type == JTokenType.Boolean && postDraft4)
                        schemaList.Add(new JValue(schemaToken));
                    else
                        throw new SchemaBuilderException($"Schema must be an object.", schemaToken);
                }
                return schemaList;
            }
            return null;
        }

        JToken ArrayOfSchemasPostDraft4(JToken value)
            => ArrayOfSchemas(value, true);

        JToken ArrayOfSchemasDraft3And4(JToken value)
            => ArrayOfSchemas(value, false);

        JToken Extends(JToken value)
            => ArrayOfSchemas(new JArray { value }, false);

        JToken NotAnyOf(JToken value)
        {
            if (value.HasValues && value is JArray arr)
            {
                var notSchema = new JObject(new JProperty("type", arr));
                return MapToSchema(notSchema);
            }
            return null;
        }

        JToken NotType(JToken value)
            => new JObject(new JProperty("type", value));


        JToken AnyOfType(JToken value)
        {
            if (value.HasValues && value is JArray arr)
            {
                var schemaList = new JArray();
                for(var i = 0; i < arr.Count; i++)
                {
                    var val = arr[i];
                    if (val.Type == JTokenType.String)
                    {
                        var typeSchema = new JObject(new JProperty("type", val));
                        schemaList.Add(typeSchema);
                    }
                    else if (val.Type == JTokenType.Object)
                    {
                        var schemaObj = MapToSchema(val);
                        schemaList.Add(schemaObj);
                    }
                    else
                        throw new SchemaBuilderException($"Property must be a string or object.", val);
                }
                return schemaList;
            }
            return null;
        }

        JToken GetExclusive(JToken value)
        {
            if ((bool)value)
            {
                var properties = value.Parent;
                if (properties != null && properties.Type == JTokenType.Property)
                {
                    var parentSchema = properties.Parent;
                    if (parentSchema != null && parentSchema.Type == JTokenType.Object)
                    {
                        var dependentProp = ((JProperty)properties).Name.Replace("exclusive", "").ToLower();
                        if (((JObject)parentSchema).ContainsKey(dependentProp))
                        {
                            var minMax = parentSchema[dependentProp];
                            if (minMax.Type == JTokenType.Integer || minMax.Type == JTokenType.Float)
                                return new JValue((JValue)minMax);
                        }
                    }
                }
            }
            return null;
        }

        JToken AddRequiredProperty(JToken value)
        {
            if ((bool)value)
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
                    ancestor.Type == JTokenType.Object &&
                    property.Type == JTokenType.Property)
                {
                    var obj = (JObject)ancestor;
                    var prop = (JProperty)property;
                    var required = obj["required"];
                    if (required == null)
                    {
                        var reqArray = new JArray(prop.Name);
                        obj.Add("required", reqArray);
                    }
                    else
                    {
                        var reqArr = (JArray)required;
                        reqArr.Add(prop.Name);
                    }
                }
            }
            return null;
        }

        JToken MapToSchema(JToken schemaToken)
        {
            if (schemaToken.HasValues && schemaToken is JObject jSchemaObject)
            {
                var schemaResult = new JObject();
                for(var i = 0; i < jSchemaObject.Properties().Count(); i++)
                {
                    var prop = jSchemaObject.Properties().ElementAt(i);
                    var (propertyName, propertyValue) = _mapper.Transform(prop.Name, prop.Value);
                    if (propertyName == null || propertyValue == null)
                        continue;
                    schemaResult.Add(propertyName, propertyValue);
                }
                if (jSchemaObject.TryGetValue("required", out var requiredToken) && requiredToken.Type == JTokenType.Array)
                    schemaResult.Add("required", requiredToken);
                return schemaResult;
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
        public Schema BuildFromJson(string draftSchemaJson, bool allowAllIfNotSpecified)
        {
            try
            {
                var token = JToken.Parse(draftSchemaJson);
                var schemaUri = string.Empty;
                if (token.HasValues && token is JObject obj)
                {
                    var schemaUriToken = obj.Property("$schema");
                    if (schemaUriToken != null && schemaUriToken.Value.Type == JTokenType.String)
                        schemaUri = schemaUriToken.Value.ToString();
                }
                if ((!allowAllIfNotSpecified && string.IsNullOrEmpty(schemaUri)) ||
                    schemaUri.Equals("https://json-schema.org/draft/2020-12/schema"))
                    return new Schema(token);
                _mapper = schemaUri.TrimEndOnce('#') switch
                {
                    "http://json-schema.org/draft-03/schema"        => Draft3(),
                    "http://json-schema.org/draft-04/schema"        => Draft4(),
                    "http://json-schema.org/draft-06/schema"        => Draft6(),
                    "http://json-schema.org/draft-07/schema"        => Draft7(),
                    "https://json-schema.org/draft/2019-09/schema"  => Draft2019(),
                    _                                               => AllSchemas()
                };
                return new Schema(MapToSchema(token));
            }
            catch (JsonReaderException exc)
            {
                throw new SchemaBuilderException($"Error encountered parsing schema JSON at {exc.LineNumber}:{exc.LinePosition}.", exc.Path.ToJsonPointer());
            }
        }
        #endregion
    }
}
