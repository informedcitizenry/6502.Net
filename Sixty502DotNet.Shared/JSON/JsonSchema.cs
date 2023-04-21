//-----------------------------------------------------------------------------
// Copyright (c) 2017-2023 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System.Text.RegularExpressions;

namespace Sixty502DotNet.Shared;

/// <summary>
/// A class representing a draft 2020-12 JSON schema. 
/// </summary>
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

    private static readonly Dictionary<ValueType, JsonSchemaType> s_valueToSchemaTypes = new()
    {
        { ValueType.Array, JsonSchemaType.Array },
        { ValueType.Boolean, JsonSchemaType.Boolean },
        { ValueType.Dictionary, JsonSchemaType.Object },
        { ValueType.Integer, JsonSchemaType.Integer },
        { ValueType.Null, JsonSchemaType.Null },
        { ValueType.Number, JsonSchemaType.Number },
        { ValueType.String, JsonSchemaType.String }
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
        DynamicRef =
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

    private JsonSchema(ValueBase? value, JsonSchema? parent)
        : this()
    {
        if (value == null)
        {
            return;
        }
        _root = parent?._root ?? parent;
        _jsonPointer = value.JsonPath;
        if (value is BoolValue)
        {
            _autoAssertion = value.AsBool();
            _basePath = GetBasePath();
            RootBasePath = GetParentBasePath(parent)?.ToString() ?? "#";
            _basePathIsAbsoluteUri = Uri.TryCreate(_basePath, UriKind.Absolute, out _);
            if (_autoAssertion == false)
                Not = new JsonSchema(true, this);
        }
        else
        {
            if (value is not JsonObject obj)
                throw new ArgumentException("Not a valid schema object");
            if (obj.Count > 0)
            {
                Id = GetStringProperty(obj, "$id") ?? "";
                _basePath = GetBasePath();
                RootBasePath = GetParentBasePath(parent)?.ToString() ?? "#";
                _basePathIsAbsoluteUri = _ = Uri.TryCreate(_basePath, UriKind.Absolute, out _);
                SchemaUri = GetStringProperty(obj, "$schema") ?? "#";
                Anchor = GetStringProperty(obj, "$anchor") ?? "";
                Ref = GetStringProperty(obj, "$ref") ?? "";
                DynamicAnchor = GetStringProperty(obj, "$dynamicAnchor") ?? "";
                DynamicRef = GetStringProperty(obj, "$dynamicRef") ?? "";
                Format = GetStringProperty(obj, "format") ?? "";
                Pattern = GetStringProperty(obj, "pattern") ?? "";
                ContentEncoding = GetStringProperty(obj, "contentEncoding") ?? "";
                ContentMediaType = GetStringProperty(obj, "contentMediaType") ?? "";
                Minimum = GetProperty<double>(obj, "minimum");
                Maximum = GetProperty<double>(obj, "maximum");
                ExclusiveMaximum = GetProperty<double>(obj, "exclusiveMaximum");
                ExclusiveMinimum = GetProperty<double>(obj, "exclusiveMinimum");
                MultipleOf = GetProperty<double>(obj, "multipleOf");
                MinLength = GetProperty<int>(obj, "minLength");
                MaxLength = GetProperty<int>(obj, "maxLength");
                MinItems = GetProperty<int>(obj, "minItems");
                MaxItems = GetProperty<int>(obj, "maxItems");
                MinContains = GetProperty<int>(obj, "minContains");
                MaxContains = GetProperty<int>(obj, "maxContains");
                MinProperties = GetProperty<int>(obj, "MinProperties");
                MaxProperties = GetProperty<int>(obj, "MaxProperties");
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

    /// <summary>
    /// Constructs a new instance of a schema 
    /// from parsed JSON in the <see cref="ValueBase"/> object.
    /// </summary>
    /// <param name="value">The value representing the schema JSON.</param>
    public JsonSchema(ValueBase? value)
        : this(value, null)
    {

    }

    private void Validate()
    {
        ValidateProperty("multipleOf", "must be greater than 0", !MultipleOf.LessThanOrEqual(0));
        ValidateProperty("$anchor", "is not a valid anchor name", string.IsNullOrEmpty(Anchor) || AnchorRegex().IsMatch(Anchor));
        ValidateProperty("$dynamicAnchor", "is not a valid anchor name", string.IsNullOrEmpty(DynamicAnchor) || AnchorRegex().IsMatch(DynamicAnchor));
        ValidateProperty("required", "must be a non-empty array of strings", !(Required?.Count == 0));
        ValidateProperty("dependentRequired", "contains one or more properties whose array is empty.", !(DependentRequired?.Values.Any(arr => arr.Count == 0) == true));
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

    private static void ValidateProperty(string propertyName, string error, bool isValid)
    {
        if (!isValid)
            throw new Exception($"Error in schema: Property '{propertyName}' {error}.");
    }

    private JsonSchemaType ConvertType()
    {
        JsonSchemaType convertedType = JsonSchemaType.None;
        try
        {
            if (Type != null)
            {
                if (Type.ValueType == ValueType.Dictionary)
                {
                    return JsonSchemaType.Object;
                }
                if (Type is ArrayValue types)
                {
                    foreach (var type in types)
                    {
                        convertedType |= s_schemaTypes[type.AsString()];
                    }
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
        if (parent != null && parent._basePathIsAbsoluteUri &&
            Uri.TryCreate(parent._basePath, UriKind.Absolute, out Uri? parentUri))
        {
            return parentUri.ToString();
        }
        return null;
    }

    /// <summary>
    /// Gets the path (JSON pointer) of the schema.
    /// </summary>
    /// <param name="absolute">A flag indicating whether the path to return is 
    /// the schema's absolute path. If no absolute path is defined, this will return
    /// a null value.</param>
    /// <returns>The schema's path, or null.</returns>
    public string? GetPath(bool absolute)
    {
        if (absolute)
            return _basePathIsAbsoluteUri ? _basePath ?? "/" : null;
        return _jsonPointer;
    }

    /// <summary>
    /// Get the base path of the schema.
    /// </summary>
    /// <returns>The schema base class.</returns>
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
            if (RootBasePath != null)
            {
                return new Uri(new Uri(RootBasePath), idUri).ToString();
            }
            return idUri.ToString();
        }
        return null;
    }

    private IList<JsonSchema>? GetSchemaList(JsonObject obj, string property)
    {
        ValueBase? arrayProp = obj.GetValue(property);
        if (arrayProp is JsonArray tokenList)
        {
            List<JsonSchema> schemas = new();
            for (int i = 0; i < tokenList.Count; i++)
            {
                schemas.Add(new JsonSchema(tokenList[i], this));
            }
            return schemas;
        }
        return null;
    }

    private IReadOnlyDictionary<string, JsonSchema>? GetSchemaDictionary(JsonObject obj, string property)
    {
        ValueBase? prop = obj.GetValue(property);
        if (prop is JsonObject objProp)
        {
            Dictionary<string, JsonSchema> schemaDict = new();
            foreach (KeyValuePair<string, ValueBase?> p in objProp)
            {
                schemaDict[p.Key] = new JsonSchema(p.Value, this);
            }
            return schemaDict;
        }
        return null;
    }

    private JsonSchema? GetSchemaProperty(JsonObject obj, string property)
    {
        ValueBase? value = obj.GetValue(property);
        if (value != null)
        {
            return new JsonSchema(value, this);
        }
        return null;
    }

    private IReadOnlyDictionary<string, ISet<string>>? GetDependentProperties(JsonObject obj)
    {
        if (obj.TryGetValue("dependentRequired", out ValueBase? dependent))
        {
            Dictionary<string, ISet<string>> result = new();
            if (dependent is JsonObject dependentRequired)
            {
                foreach (var kvp in dependentRequired)
                {
                    ISet<string> required = GetStringPropertyList(dependentRequired, kvp.Key)!.ToHashSet();
                    result[kvp.Key] = required;
                }
                return result.AsReadOnly();
            }
            throw new JsonSchemaException($"Property 'dependentRequired' is not in the correct form", this);
        }
        return null;
    }

    private IList<string>? GetStringPropertyList(JsonObject obj, string property)
    {
        if (GetPolymorphicProperty(obj, property) is JsonArray array)
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

    private static ValueBase? GetPolymorphicProperty(JsonObject obj, string property)
    {
        _ = obj.TryGetValue(property, out ValueBase? value);
        return value;
    }

    private static ISet<ValueBase>? GetEnumProperty(JsonObject obj)
    {
        if (obj.TryGetValue("enum", out ValueBase? enumArray))
        {
            if (enumArray is JsonArray array)
            {
                return new HashSet<ValueBase>(array.ToList());
            }
            return new HashSet<ValueBase>
            {
                enumArray!
            };
        }
        return null;
    }

    private static string? GetStringProperty(JsonObject obj, string property)
    {
        if (obj.TryGetValue(property, out ValueBase? value))
        {
            return value!.AsString();
        }
        return null;
    }

    private static Nullable<T> GetProperty<T>(JsonObject obj, string property) where T : struct
    {
        if (obj.TryGetValue(property, out ValueBase? value))
        {
            return value!.ToObject<T>();
        }
        return default;
    }

    /// Gets whether the instance token's type matches the schema's type.
    /// </summary>
    /// <param name="token">The parsed token of the instance property.</param>
    /// <returns><c>true</c> if the schema's type is valid for the instance property type
    /// or if the schema's type is not defined, <c>false</c> otherwise.</returns>
    public bool MatchesTokenType(ValueBase? token)
    {
        if (_type == JsonSchemaType.None) return true;
        if (token == null)
        {
            return _type == JsonSchemaType.Null;
        }
        JsonSchemaType tokenSchemaType = s_valueToSchemaTypes[token.ValueType];
        if ((_type & tokenSchemaType) == tokenSchemaType) return true;
        return token is NumericValue num &&
            ((num.ValueType == ValueType.Integer && _type == JsonSchemaType.Number) ||
            (num.ValueType == ValueType.Number && _type == JsonSchemaType.Float));
    }

    public override string ToString()
    {
        return GetPath(false) ?? _jsonPointer;
    }

    /// <summary>
    /// Sets the given schema to a default evaluation behavior.
    /// </summary>
    /// <param name="autoEvaluate">The auto-evaluation.</param>
    public static implicit operator JsonSchema(bool autoEvaluate) => new(autoEvaluate);

    /// <summary>
    /// Resolves the given schema's default evaluation behavior against a boolean value.
    /// </summary>
    /// <param name="s">The schema.</param>
    public static implicit operator bool(JsonSchema s) => s?._autoAssertion != null && s?._autoAssertion.Value == true;

    /// <summary>
    /// The schema "$id" property.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// The meta-schema "$schema" URI of the schema.
    /// </summary>
    public string SchemaUri { get; }

    /// <summary>
    /// The schema "$ref", which is a URI or JSON pointer to another 
    /// schema resource.
    /// </summary>
    public string Ref { get; }

    /// <summary>
    /// The schema "$defs", which are schema definitions that can be re-used
    /// by other schemas.
    /// </summary>
    public IReadOnlyDictionary<string, JsonSchema>? Defs { get; }

    /// <summary>
    /// The schema "$anchor", its reference alias to other schemas.
    /// </summary>
    public string Anchor { get; }

    /// <summary>
    /// The schema "$dynamicRef". NOTE: This is not currently implemented.
    /// </summary>
    public string DynamicRef { get; }

    /// <summary>
    /// The schema "$dynamicAnchor". NOTE: This is not currently implemented.
    /// </summary>
    public string DynamicAnchor { get; }

    /// <summary>
    /// Validations against the named properties of the instance.
    /// </summary>
    public IReadOnlyDictionary<string, JsonSchema>? Properties { get; }

    /// <summary>
    /// Validations against any properties not evaluated by Properties and PatternProperties.
    /// </summary>
    public JsonSchema? AdditionalProperties { get; }

    /// <summary>
    /// Validations against any properties not successfully evaluated by Properties,
    /// PatternProperties, AdditionalProperties, as well as by AllOf, AnyOf, OneOf, 
    /// If, Then, Else, Not, DependentSchemas, and Ref.
    /// </summary>
    public JsonSchema? UnevaluatedProperties { get; }

    /// <summary>
    /// Validates against the instance property names.
    /// </summary>
    public JsonSchema? PropertyNames { get; }

    /// <summary>
    /// Validations against the instance properties whose names match the 
    /// patterns.
    /// </summary>
    public IReadOnlyDictionary<string, JsonSchema>? PatternProperties { get; }

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
    public IReadOnlyList<string>? Required { get; }

    /// <summary>
    /// Gets the properties required to be defined in the instance if 
    /// given properties are defined.
    /// </summary>
    public IReadOnlyDictionary<string, ISet<string>>? DependentRequired { get; }

    /// <summary>
    /// Additional validation against the instance if the given properties
    /// are defined.
    /// </summary>
    public IReadOnlyDictionary<string, JsonSchema>? DependentSchemas { get; }

    /// <summary>
    /// Validates the instance against all of the given schemas. Successful 
    /// validation is required against all schemas. If any one of the schema does not
    /// validate, then validation fails.
    /// </summary>
    public IList<JsonSchema>? AllOf { get; }

    /// <summary>
    /// Validates the instance against the given schemas. Successful 
    /// validation is only required for one of the schemas. If any one of 
    /// the schemas validates, then validation passes.
    /// </summary>
    public IList<JsonSchema>? AnyOf { get; }

    /// <summary>
    /// Validates the instance against the given schemas. Only one
    /// schema can validate against the instance. If no schemas 
    /// validate, then validation fails.
    /// </summary>
    public IList<JsonSchema>? OneOf { get; }

    /// <summary>
    /// Validates the instance does not validate against the schema. 
    /// </summary>
    public JsonSchema? Not { get; }

    /// <summary>
    /// Conditionally validates the instance against the schema.
    /// If the schema validates successfully, the instance will validate against
    /// Then, or validation is successful if Then is not present. If the schema
    /// fails validation, the instance will validate against Else, or 
    /// validation fails if Else is not present.
    /// </summary>
    public JsonSchema? If { get; }

    /// <summary>
    /// Conditionally evaluates the instance against the schema if the If 
    /// schema has validated successfully. If Then is not defined, then either 
    /// Else is evaluated, or the result of If is the validation.
    /// </summary>
    public JsonSchema? Then { get; }

    /// <summary>
    /// Conditionally evaluates the instance against the schema if the If
    /// schema fails validation. If Else is not defined, the result of the If
    /// validation is the result of the If validation.
    /// </summary>
    public JsonSchema? Else { get; }

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
    public ValueBase? Type { get; }

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
    public JsonSchema? ContentSchema { get; }

    /// <summary>
    /// Validates the instance value, object, or array is equal to that
    /// specified in Const.
    /// </summary>
    public ValueBase? Const { get; }

    /// <summary>
    /// Validates the instance value, object, or array matches one or 
    /// more elements in the Enum.
    /// </summary>
    public ISet<ValueBase>? Enum { get; }

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
    public JsonSchema? Contains { get; }

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
    public JsonSchema? Items { get; }

    /// <summary>
    /// Validates against members of the instance array, each element in PrefixItem 
    /// validating against the index of the corresponding array element.
    /// </summary>
    public IList<JsonSchema>? PrefixItems { get; }

    /// <summary>
    /// Validates against members of the instance array that were not successfully validated
    /// by PrefixItems, Items, and Contains, as well as AllOf, AnyOf, OneOf, If, Then, Else,
    /// Not, and Ref. If any member fails validation against PrefixItems or Items, all 
    /// array elements are considered unevaluated, excepting those successfully validated by
    /// Contains.
    /// </summary>
    public JsonSchema? UnevaluatedItems { get; }

    /// <summary>
    /// Gets whether the schema will pass validation by default.
    /// </summary>

    public bool AutoAssertsToTrue => _autoAssertion == true;

    /// <summary>
    /// Gets whether the schema will fail validation by default.
    /// </summary>

    public bool AutoAssertsToFalse => _autoAssertion == false;

    /// <summary>
    /// Gets the schema's root base URI.
    /// </summary>

    public string RootBasePath { get; }

    /// <summary>
    /// Gets whether the schema is a meta-schema. NOTE: This is experimental and not fully implemented.
    /// </summary>


    public bool IsMeta => !string.IsNullOrEmpty(SchemaUri) && !string.IsNullOrEmpty(Id) && Id.Equals(SchemaUri);

    /// <summary>
    /// Gets whether the schema is a dynamic schema. NOTE: This is experimental and not fully implemented.
    /// </summary>

    public bool IsDynamic => _root?.IsMeta == true && !string.IsNullOrEmpty(DynamicAnchor);

    [GeneratedRegex("^#?[a-z_][0-9a-z\\-_.]*$", RegexOptions.Compiled)]
    private static partial Regex AnchorRegex();
}

