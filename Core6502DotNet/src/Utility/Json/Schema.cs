//-----------------------------------------------------------------------------
// Copyright (c) 2017-2021 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Core6502DotNet.Json
{
    /// <summary>
    /// A class representing a draft 2020-12 JSON schema. 
    /// </summary>
    public sealed class Schema : JsonSerializable
    {
        #region Members

        static readonly Dictionary<string, JTokenType> s_schemaTypes;
        static readonly Regex s_validAnchorRegex;

        readonly Schema _root;
        readonly JTokenType _type;
        readonly string _jsonPointer;
        readonly string _basePath;
        readonly bool _basePathIsAbsoluteUri;
        readonly bool? _autoAssertion;

        #endregion

        #region Constructors
        static Schema()
        {
            s_validAnchorRegex = new Regex(@"^#?[a-z_][0-9a-z\-_.]*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            s_schemaTypes = new Dictionary<string, JTokenType>
            {
                { "array",      JTokenType.Array },
                { "boolean",    JTokenType.Boolean },
                { "integer",    JTokenType.Integer },
                { "number",     JTokenType.Integer | JTokenType.Float },
                { "null",       JTokenType.Null },
                { "object",     JTokenType.Object },
                { "string",     JTokenType.String }
            };
        }

        Schema(bool autoEvaluation)
        {
            _jsonPointer = "#";
            _autoAssertion = autoEvaluation;
            if (!autoEvaluation)
                Not = true;
        }

        Schema(bool autoEvaluation, Schema parent)
        {
            _jsonPointer = $"{parent._jsonPointer}/not";
            _basePath = parent._basePath;
            _basePathIsAbsoluteUri = parent._basePathIsAbsoluteUri;
            RootBasePath = parent.RootBasePath;
            _autoAssertion = autoEvaluation;
        }

        Schema(JToken token, Schema parent)
        {
            _root = parent?._root ?? parent;
            if (!string.IsNullOrEmpty(token.Path))
                _jsonPointer = token.ToJsonPointer();
            else
                _jsonPointer = "#";
            if (token.Type == JTokenType.Boolean)
            {
                _autoAssertion = (bool)token;
                _basePath = GetBasePath();
                RootBasePath = GetParentBasePath(parent)?.ToString();
                _basePathIsAbsoluteUri = _ = Uri.TryCreate(_basePath, UriKind.Absolute, out _);
                if (_autoAssertion == false)
                    Not = new Schema(true, this);
            }
            else
            {
                if (token.Type != JTokenType.Object)
                    throw new ArgumentException("Not a valid schema object.");

                if (token.HasValues)
                {
                    var obj = (JObject)token;
                    Id = GetProperty<string>(obj, "$id");
                    _basePath = GetBasePath();
                    RootBasePath = GetParentBasePath(parent)?.ToString();
                    _basePathIsAbsoluteUri = _ = Uri.TryCreate(_basePath, UriKind.Absolute, out _);
                    SchemaUri = GetProperty<string>(obj, "$schema");
                    Anchor = GetProperty<string>(obj, "$anchor");
                    Ref = GetProperty<string>(obj, "$ref");
                    DynamicAnchor = GetProperty<string>(obj, "$dynamicAnchor");
                    DynamicRef = GetProperty<string>(obj, "$dynamicRef");
                    Format = GetProperty<string>(obj, "format");
                    Pattern = GetProperty<string>(obj, "pattern");
                    ContentEncoding = GetProperty<string>(obj, "contentEncoding");
                    ContentMediaType = GetProperty<string>(obj, "contentMediaType");
                    Minimum = GetProperty<double?>(obj, "minimum");
                    Maximum = GetProperty<double?>(obj, "maximum");
                    ExclusiveMaximum = GetProperty<double?>(obj, "exclusiveMaximum");
                    ExclusiveMinimum = GetProperty<double?>(obj, "exclusiveMinimum");
                    MultipleOf = GetProperty<double?>(obj, "multipleOf");
                    MinLength = GetProperty<int?>(obj, "minLength");
                    MaxLength = GetProperty<int?>(obj, "maxLength");
                    MinItems = GetProperty<int?>(obj, "minItems");
                    MaxItems = GetProperty<int?>(obj, "maxItems");
                    MinContains = GetProperty<int?>(obj, "minContains");
                    MaxContains = GetProperty<int?>(obj, "maxContains");
                    MinProperties = GetProperty<int?>(obj, "MinProperties");
                    MaxProperties = GetProperty<int?>(obj, "MaxProperties");
                    UniqueItems = GetProperty<bool?>(obj, "uniqueItems");
                    Const = GetProperty<JToken>(obj, "const");
                    Required = GetProperty<IReadOnlyList<string>>(obj, "required");
                    Enum = GetProperty<ISet<JToken>>(obj, "enum");
                    DependentRequired = GetProperty<IReadOnlyDictionary<string, ISet<string>>>(obj, "dependentRequired");
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
                    Type = GetProperty<JToken>(obj, "type");
                    _type = ConvertType();
                    Validate();
                }    
            }
        }

        /// <summary>
        /// Constructs a new instance of a schema 
        /// from parsed JSON in the <see cref="JToken"/> object.
        /// </summary>
        /// <param name="token">The token representing the schema JSON.</param>
        public Schema(JToken token)
            : this(token, null)
        {

        }

        #endregion

        #region Methods

        JTokenType ConvertType()
        {
            JTokenType convertedType = JTokenType.None;
            try
            {
                if (Type != null)
                {
                    if (Type is JArray types)
                    {
                        foreach (var type in types)
                            convertedType |= s_schemaTypes[type.ToString()];
                    }
                    else
                    {
                        convertedType = s_schemaTypes[Type.ToString()];
                    }
                }
                return convertedType;
            }
            catch (KeyNotFoundException)
            {
                throw new SchemaException($"Error in schema: Property 'type' contains one or more invalid type names.", this);
            }
        }

        void Validate()
        {
            ValidateProperty("multipleOf", "must be greater than 0", !MultipleOf.LessThanOrEqual(0));
            ValidateProperty("$anchor", "is not a valid anchor name", string.IsNullOrEmpty(Anchor) || s_validAnchorRegex.IsMatch(Anchor));
            ValidateProperty("$dynamicAnchor", "is not a valid anchor name", string.IsNullOrEmpty(DynamicAnchor) || s_validAnchorRegex.IsMatch(DynamicAnchor));
            ValidateProperty("required", "must be a non-empty array of strings", !(Required?.Count == 0));
            ValidateProperty("dependentRequired", "contains one or more properties whose array is empty.", !(DependentRequired?.Values.Any(arr => arr.Count == 0)==true));
            ValidateProperty("allOf", "cannot be empty", !(AllOf?.Count == 0));
            ValidateProperty("anyOf", "cannot be empty", !(AnyOf?.Count == 0));
            ValidateProperty("oneOf", "cannot be empty", !(OneOf?.Count == 0));
            ValidateProperty("prefixItems", "cannot be empty", !(PrefixItems?.Count == 0));
            ValidateProperty("minContains", "must be greater than or equal to zero", !MinContains.LessThan(0));
            ValidateProperty("maxContains", "must be greater than or equal to zero", !MaxContains.LessThan(0));
            ValidateProperty("minLength", "must be greater than or equal to zero", !MinLength.LessThan(0));
            ValidateProperty("maxLength", "must be greater than or equal to zero", !MaxLength.LessThan(0));
            ValidateProperty("minItems", "must be greater than or equal to zero", !MinItems.LessThan(0));
            ValidateProperty("maxItems", "must be greater than or equal to zero", !MaxItems.LessThan(0));
            ValidateProperty("minProperties", "must be greater than or equal to zero", !MinProperties.LessThan(0));
            ValidateProperty("maxProperties", "must be greater than or equal to zero", !MaxProperties.LessThan(0));
        }

        void ValidateProperty(string propertyName, string error, bool isValid)
        {
            if (!isValid)
                throw new SchemaException($"Error in schema: Property '{propertyName}' {error}.", this);
        }

        static Uri GetParentBasePath(Schema parent)
        {
            if (parent != null && parent._basePathIsAbsoluteUri &&
                    Uri.TryCreate(parent._basePath, UriKind.Absolute, out var parentUri))
                return parentUri;
            return null;
        }

        string GetBasePath()
        {
            Uri idUri = null;
            if (!string.IsNullOrEmpty(Id) && Uri.TryCreate(Id, UriKind.RelativeOrAbsolute, out idUri) &&
                idUri.IsAbsoluteUri)
                return idUri.ToString();
            if (idUri != null)
            {
                if (RootBasePath != null)
                    return new Uri(new Uri(RootBasePath), idUri).ToString();
                return idUri.ToString();
            }
            return null;
        }

        IList<Schema> GetSchemaList(JObject obj, string property)
        {

            var arrayProp = obj[property];
            if (arrayProp != null)
            {
                var tokenList = (JArray)arrayProp;
                var schemas = new List<Schema>();
                for (var i = 0; i < tokenList.Count; i++)
                    schemas.Add(new Schema(tokenList[i], this));
                return schemas;
            }
            return null;
        }

        IReadOnlyDictionary<string, Schema> GetSchemaDictionary(JObject obj, string property)
        {
            
            var prop = obj[property];
            if (prop?.Type == JTokenType.Object)
            {
                var objProp = (JObject)prop;
                var schemaDict = new Dictionary<string, Schema>();
                foreach (var p in objProp)
                    schemaDict[p.Key] = new Schema(p.Value, this); ;
                return schemaDict;
            }
            return null;
        }

        Schema GetSchemaProperty(JObject obj, string property)
        {
            var token = obj[property];
            if (token != null)
                return new Schema(token, this);
            return null;
        }

        static T GetProperty<T>(JObject obj, string property)
        {
            if (obj.TryGetValue(property, out var token))
                return token.ToObject<T>();
            return default;
        }

        /// <summary>
        /// Gets the path (JSON pointer) of the schema.
        /// </summary>
        /// <param name="absolute">A flag indicating whether the path to return is 
        /// the schema's absolute path. If no absolute path is defined, this will return
        /// a null value.</param>
        /// <returns>The schema's path, or null.</returns>
        public string GetPath(bool absolute)
        {
            if (absolute)
                return _basePathIsAbsoluteUri ? _basePath : null;
            return _jsonPointer;
        }


        /// <summary>
        /// Gets whether the instance token's type matches the schema's type.
        /// </summary>
        /// <param name="token">The parsed token of the instance property.</param>
        /// <returns><c>true</c> if the schema's type is valid for the instance property type
        /// or if the schema's type is not defined, <c>false</c> otherwise.</returns>
        public bool MatchesTokenType(JToken token) =>
            _type == JTokenType.None || (_type & token.Type) == token.Type;

        public override string ToString() => GetPath(false);

        /// <summary>
        /// Sets the given schema to a default evaluation behavior.
        /// </summary>
        /// <param name="autoEvaluate">The auto-evaluation.</param>
        public static implicit operator Schema(bool autoEvaluate) => new Schema(autoEvaluate);

        /// <summary>
        /// Resolves the given schema's default evaluation behavior against a boolean value.
        /// </summary>
        /// <param name="s">The schema.</param>
        public static implicit operator bool(Schema s) => s?._autoAssertion != null && s?._autoAssertion.Value == true;

        #endregion

        #region Properties

        /// <summary>
        /// The schema "$id" property.
        /// </summary>
        [JsonProperty(PropertyName = "$id")]
        public string Id { get; }

        /// <summary>
        /// The meta-schema "$schema" URI of the schema.
        /// </summary>
        [JsonProperty(PropertyName = "$schema")]
        public string SchemaUri { get; }

        /// <summary>
        /// The schema "$ref", which is a URI or JSON pointer to another 
        /// schema resource.
        /// </summary>
        [JsonProperty(PropertyName = "$ref")]
        public string Ref { get; }

        /// <summary>
        /// The schema "$defs", which are schema definitions that can be re-used
        /// by other schemas.
        /// </summary>
        [JsonProperty(PropertyName = "$defs")]
        public IReadOnlyDictionary<string, Schema> Defs { get; }

        /// <summary>
        /// The schema "$anchor", its reference alias to other schemas.
        /// </summary>
        [JsonProperty(PropertyName = "$anchor")]
        public string Anchor { get; }

        /// <summary>
        /// The schema "$dynamicRef". NOTE: This is not currently implemented.
        /// </summary>
        [JsonProperty(PropertyName = "$dynamicRef")]
        public string DynamicRef { get; }

        /// <summary>
        /// The schema "$dynamicAnchor". NOTE: This is not currently implemented.
        /// </summary>
        [JsonProperty(PropertyName = "$dynamicAnchor")]
        public string DynamicAnchor { get; }

        /// <summary>
        /// Validations against the named properties of the instance.
        /// </summary>
        public IReadOnlyDictionary<string, Schema> Properties { get; }

        /// <summary>
        /// Validations against any properties not evaluated by Properties and PatternProperties.
        /// </summary>
        public Schema AdditionalProperties { get; }

        /// <summary>
        /// Validations against any properties not successfully evaluated by Properties,
        /// PatternProperties, AdditionalProperties, as well as by AllOf, AnyOf, OneOf, 
        /// If, Then, Else, Not, DependentSchemas, and Ref.
        /// </summary>
        public Schema UnevaluatedProperties { get; }

        /// <summary>
        /// Validates against the instance property names.
        /// </summary>
        public Schema PropertyNames { get; }

        /// <summary>
        /// Validations against the instance properties whose names match the 
        /// patterns.
        /// </summary>
        public IReadOnlyDictionary<string, Schema> PatternProperties { get; }

        /// <summary>
        /// Validates the number of instance properties is greater than or 
        /// equal to the given value.
        /// </summary>
        public int? MinProperties { get; }

        /// <summary>
        /// Validates the number of instance properties is less than or 
        /// equal to the given value.
        /// </summary>
        public int? MaxProperties { get; }

        /// <summary>
        /// Gets the properties required to be defined in the instance.
        /// </summary>
        public IReadOnlyList<string> Required { get; }

        /// <summary>
        /// Gets the properties required to be defined in the instance if 
        /// given properties are defined.
        /// </summary>
        public IReadOnlyDictionary<string, ISet<string>> DependentRequired { get; }

        /// <summary>
        /// Additional validation against the instance if the given properties
        /// are defined.
        /// </summary>
        public IReadOnlyDictionary<string, Schema> DependentSchemas { get; }

        /// <summary>
        /// Validates the instance against all of the given schemas. Successful 
        /// validation is required against all schemas. If any one of the schema does not
        /// validate, then validation fails.
        /// </summary>
        public IList<Schema> AllOf { get; }

        /// <summary>
        /// Validates the instance against the given schemas. Successful 
        /// validation is only required for one of the schemas. If any one of 
        /// the schemas validates, then validation passes.
        /// </summary>
        public IList<Schema> AnyOf { get; }

        /// <summary>
        /// Validates the instance against the given schemas. Only one
        /// schema can validate against the instance. If no schemas 
        /// validate, then validation fails.
        /// </summary>
        public IList<Schema> OneOf { get; }

        /// <summary>
        /// Validates the instance does not validate against the schema. 
        /// </summary>
        public Schema Not { get; }

        /// <summary>
        /// Conditionally validates the instance against the schema.
        /// If the schema validates successfully, the instance will validate against
        /// Then, or validation is successful if Then is not present. If the schema
        /// fails validation, the instance will validate against Else, or 
        /// validation fails if Else is not present.
        /// </summary>
        public Schema If { get; }

        /// <summary>
        /// Conditionally evaluates the instance against the schema if the If 
        /// schema has validated successfully. If Then is not defined, then either 
        /// Else is evaluated, or the result of If is the validation.
        /// </summary>
        public Schema Then { get; }

        /// <summary>
        /// Conditionally evaluates the instance against the schema if the If
        /// schema fails validation. If Else is not defined, the result of the If
        /// validation is the result of the If validation.
        /// </summary>
        public Schema Else { get; }

        /// <summary>
        /// The format of the string instance against which to validate.
        /// </summary>
        public string Format { get; }

        /// <summary>
        /// The regex pattern of the string instance.
        /// </summary>
        public string Pattern { get; }

        /// <summary>
        /// The instance's expected JSON type or range of types.
        /// </summary>
        public JToken Type { get; }

        /// <summary>
        /// The encoding the instance string is expected to be.
        /// </summary>
        public string ContentEncoding { get; }

        /// <summary>
        /// The media type the instance string is expected to represent.
        /// </summary>
        public string ContentMediaType { get; }

        /// <summary>
        /// Validates the string instance if the ContentMediaType is defined.
        /// </summary>
        public Schema ContentSchema { get; }

        /// <summary>
        /// Validates the instance value, object, or array is equal to that
        /// specified in Const.
        /// </summary>
        public JToken Const { get; }

        /// <summary>
        /// Validates the instance value, object, or array matches one or 
        /// more elements in the Enum.
        /// </summary>
        public ISet<JToken> Enum { get; }

        /// <summary>
        /// Validates the instance number is a multiple of the given value.
        /// </summary>
        public double? MultipleOf { get; }

        /// <summary>
        /// Validates the instance number is greater than or equal to the given value.
        /// </summary>
        public double? Minimum { get; }

        /// <summary>
        /// Validates the instance number is less than or equal to the given value.
        /// </summary>
        public double? Maximum { get; }

        /// <summary>
        /// Validates the instance number is greater than the given value.
        /// </summary>
        public double? ExclusiveMinimum { get; }

        /// <summary>
        /// Validates the instance number i sless than the given value.
        /// </summary>
        public double? ExclusiveMaximum { get; }

        /// <summary>
        /// Validates the instance string length is greater than or equal to 
        /// the given value.
        /// </summary>
        public int? MinLength { get; }

        /// <summary>
        /// Validates the instance string length is less than or equal to 
        /// the given value.
        /// </summary>
        public int? MaxLength { get; }

        /// <summary>
        /// Validates the instance array length is greater than or equal to
        /// the given value.
        /// </summary>
        public int? MinItems { get; }

        /// <summary>
        /// Validates the instance array length is less than or equal to 
        /// the given value.
        /// </summary>
        public int? MaxItems { get; }

        /// <summary>
        /// Validates the instance array contains only unique elements.
        /// </summary>
        public bool? UniqueItems { get; }

        /// <summary>
        /// Validates against members of the instance array. Validation against the array
        /// itself is successful if the number of members validations is greater than or 
        /// equal to MinContains (or 1, if MinContains is not present), and less than
        /// or equal to MaxContains.
        /// </summary>
        public Schema Contains { get; }

        /// <summary>
        /// Validates that if Contains is present, validation against the instance array
        /// passes or fails if the number of member validations is greater than or
        /// equal to MinContains. 
        /// </summary>
        public int? MinContains { get; }

        /// <summary>
        /// Validates that if Contains is present, validation against the instance array
        /// passes or fails if the number of member validations is less than or 
        /// equal to MaxContains.
        /// </summary>
        public int? MaxContains { get; }

        /// <summary>
        /// Validates against members of the instance array, from element subsequent to the
        /// lsat validated by PrefixItems, or against all members of the array if PrefixItems
        /// is not present.
        /// </summary>
        public Schema Items { get; }

        /// <summary>
        /// Validates against members of the instance array, each element in PrefixItem 
        /// validating against the index of the corresponding array element.
        /// </summary>
        public IList<Schema> PrefixItems { get; }

        /// <summary>
        /// Validates against members of the instance array that were not successfully validated
        /// by PrefixItems, Items, and Contains, as well as AllOf, AnyOf, OneOf, If, Then, Else,
        /// Not, and Ref. If any member fails validation against PrefixItems or Items, all 
        /// array elements are considered unevaluated, excepting those successfully validated by
        /// Contains.
        /// </summary>
        public Schema UnevaluatedItems { get; }

        /// <summary>
        /// Gets whether the schema will pass validation by default.
        /// </summary>
        [JsonIgnore]
        public bool AutoAssertsToTrue => _autoAssertion == true;

        /// <summary>
        /// Gets whether the schema will fail validation by default.
        /// </summary>
        [JsonIgnore]
        public bool AutoAssertsToFalse => _autoAssertion == false;

        /// <summary>
        /// Gets the schema's root base URI.
        /// </summary>
        [JsonIgnore]
        public string RootBasePath { get; }

        /// <summary>
        /// Gets whether the schema is a meta-schema. NOTE: This is experimental and not fully implemented.
        /// </summary>

        [JsonIgnore]
        public bool IsMeta => !string.IsNullOrEmpty(SchemaUri) && !string.IsNullOrEmpty(Id) && Id.Equals(SchemaUri);

        /// <summary>
        /// Gets whether the schema is a dynamic schema. NOTE: This is experimental and not fully implemented.
        /// </summary>
        [JsonIgnore]
        public bool IsDynamic => _root?.IsMeta == true && !string.IsNullOrEmpty(DynamicAnchor);

        #endregion
    }
}
