//-----------------------------------------------------------------------------
// Copyright (c) 2017-2023 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System.Text.RegularExpressions;

namespace Sixty502DotNet.Shared;

/// <summary>
/// A class that validates a given JSON object against a valid JSON schema and 
/// deserializes that JSON into a .Net object.
/// </summary>
public sealed partial class JsonValidator
{
    private static readonly Dictionary<string, IFormatValidator> s_validators;
    private readonly JsonSchemaReferenceCollection _refs;


    static JsonValidator()
    {
        s_validators = new Dictionary<string, IFormatValidator>
        {
            { "date",                   new DateTimeValidator() },
            { "date-time",              new DateTimeValidator() },
            { "time",                   new DateTimeValidator() },
            { "duration",               new DateTimeValidator() },
            { "email",                  new EmailValidator() },
            { "idn-email",              new EmailValidator() },
            { "hostname",               new HostNameValidator() },
            { "idn-hostname",           new HostNameValidator() },
            { "ipv4",                   new HostNameValidator() },
            { "ipv6",                   new HostNameValidator() },
            { "uri",                    new UriValidator() },
            { "uri-reference",          new UriValidator() },
            { "uri-template",           new UriValidator() },
            { "iri",                    new UriValidator() },
            { "iri-reference",          new UriValidator() },
            { "uuid",                   new UuidValidator() },
            { "json-pointer",           new JsonPointerValidator() },
            { "relative-json-pointer",  new JsonPointerValidator() },
            { "regex",                  new RegexValidator() }
        };
    }

    /// <summary>
    /// Constructs a new instance of a <see cref="JsonValidator"/> that validates
    /// a JSON string against a defined schema.
    /// </summary>
    /// <param name="schemaJson">The schema JSON.</param>
    public JsonValidator(string schemaJson)
    {
        var schemaBuilder = new JsonSchemaBuilder();
        Schema = schemaBuilder.BuildFromJson(schemaJson, true);
        _refs = new JsonSchemaReferenceCollection(Schema);
    }

    private AnnotationCollection ValidateInstance(JsonSchema schema, ValueBase? token)
    {
        var annotations = new AnnotationCollection();
        if (schema.AutoAssertsToFalse)
        {
            annotations.AddError("Schema will always invalidate.", schema, schema.ToString()!, token);
        }
        else if (!schema.AutoAssertsToTrue)
        {
            if (!schema.MatchesTokenType(token))
                annotations.AddError(
                    $"Expected type '{schema.Type}' but got '{token?.ValueType.ToString().ToLower() ?? "null"}'",
                    schema, "type", token);
            if (schema.Const != null && !token!.Equals(schema.Const))
                annotations.AddError($"Expected constant '{schema.Const}'.", schema, "const", token);
            if (schema.Enum != null && !schema.Enum.Any(o => o.Equals(token)))
                annotations.AddError($"'{token}' is not a valid option.", schema, "enum", token);
            ValueType tokenType = token?.ValueType ?? ValueType.Null;
            switch (tokenType)
            {
                case ValueType.Array:
                    annotations.AddAnnotations(ValidateArray(schema, token), AnnotationAddType.ErrorsAndItems);
                    break;
                case ValueType.Dictionary:
                    annotations.AddAnnotations(ValidateObject(schema, token), AnnotationAddType.ErrorsAndProperties);
                    break;
                default:
                    if (token?.ValueType == ValueType.String)
                        annotations.AddAnnotations(ValidateString(schema, token));
                    else if (token?.IsNumeric == true)
                        annotations.AddAnnotations(ValidateNumber(schema, token));
                    annotations.AddAnnotations(ValidateInSubschemas(schema, token, AnnotationAddType.None));
                    break;
            }
        }
        return annotations;
    }

    private static AnnotationCollection ValidateNumber(JsonSchema schema, ValueBase token)
    {
        var annotations = new AnnotationCollection();
        var value = token.AsDouble();
        if (!(!schema.ExclusiveMinimum.HasValue || value > schema.ExclusiveMinimum))
            annotations.AddError($"{value} is not greater than {schema.ExclusiveMinimum}.",
                schema, "exclusiveMinimum", token);
        if (!(!schema.Minimum.HasValue || value >= schema.Minimum))
            annotations.AddError($"{value} is less than {schema.Minimum}.",
                schema, "minimum", token);
        if (!(!schema.ExclusiveMaximum.HasValue || value < schema.ExclusiveMaximum))
            annotations.AddError($"{value} is not less than {schema.ExclusiveMaximum}.",
                schema, "exclusiveMaximum", token);
        if (!(!schema.Maximum.HasValue || value <= schema.Maximum))
            annotations.AddError($"{value} is greater than {schema.Maximum}.",
                schema, "maximum", token);

        if (schema.MultipleOf.HasValue)
        {
            if (value % schema.MultipleOf != 0)
                annotations.AddError($"{value} is not a multiple of {schema.MultipleOf}.",
                    schema, "multipleOf", token);
        }
        return annotations;
    }

    private static AnnotationCollection ValidateString(JsonSchema schema, ValueBase token)
    {
        var annotations = new AnnotationCollection();
        var tokenVal = token.ToString()!;
        if (!string.IsNullOrEmpty(schema.Pattern) && !Regex.IsMatch(tokenVal, schema.Pattern))
            annotations.AddError($"'{tokenVal}' does not match pattern '{schema.Pattern}'.",
                schema, "pattern", token);
        if (schema.MinLength.GreaterThan(tokenVal.Length))
            annotations.AddError($"'{tokenVal}' has fewer characters than the minimum number of {schema.MinLength}.",
                schema, "minLength", token);
        if (schema.MaxLength.LessThan(tokenVal.Length))
            annotations.AddError($"'{tokenVal}' has more characters than the maximum number of {schema.MaxLength}.",
                schema, "maxLength", token);
        if (!string.IsNullOrEmpty(schema.Format) && s_validators.TryGetValue(schema.Format, out var validator))
        {
            if (!validator.FormatIsValid(schema.Format, tokenVal))
                annotations.AddError($"'{tokenVal}' is not a valid '{schema.Format}' string.",
                    schema, "format", token);
        }
        if (!string.IsNullOrEmpty(schema.ContentEncoding))
        {
            if (schema.ContentEncoding.Equals("base16"))
            {
                if (!HexRegex().IsMatch(tokenVal))
                    annotations.AddError($"Instance is not a valid base16 encoding.",
                        schema, "contentEncoding", token);
            }
            else if (schema.ContentEncoding.Equals("base64"))
            {
                try { _ = Convert.FromBase64String(tokenVal); }
                catch (FormatException)
                {
                    annotations.AddError($"Instance is not a valid base64 encoding.",
                        schema, "contentEncoding", token);
                }
            }
        }
        if (!string.IsNullOrEmpty(schema.ContentMediaType) && schema.ContentSchema != null)
            annotations.AddAnnotations(ValidateString(schema.ContentSchema, token));
        return annotations;
    }

    private AnnotationCollection ValidateObject(JsonSchema schema, ValueBase? token)
    {
        var annotations = new AnnotationCollection();
        var jObject = (JsonObject)token!;
        var jObjProps = jObject.Properties().ToHashSet();

        if (schema.MinProperties.GreaterThan(jObjProps.Count))
            annotations.AddError($"Object has too few properties.", schema, "minProperties", jObject);
        if (schema.MaxProperties.LessThan(jObjProps.Count))
            annotations.AddError($"Object has too many properties.", schema, "maxProperties", jObject);

        var definedProperties = new HashSet<string>();
        foreach (var prop in jObjProps)
        {
            if (schema.DependentSchemas != null && schema.DependentSchemas.TryGetValue(prop, out var dependent))
            {
                var dependencyCollection = ValidateObject(dependent, token);
                if (!dependencyCollection.Valid)
                    annotations.AddError($"Dependent schema failed validation for property '{prop}'.", schema, "dependentSchemas", jObject);
                annotations.AddAnnotations(dependencyCollection, AnnotationAddType.ErrorsAndProperties);
            }
            if (schema.PropertyNames != null)
                annotations.AddAnnotations(ValidateInstance(schema.PropertyNames, jObject[prop]));
            var jProp = jObject[prop];
            if (schema.Properties?.ContainsKey(prop) == true)
            {
                var propertySchema = schema.Properties[prop];
                var propertyCollection = ValidateInstance(propertySchema, jProp!);
                if (propertyCollection.Valid)
                    annotations.AddAnnotation(prop);
                definedProperties.Add(prop);
                annotations.AddAnnotations(propertyCollection);
            }
            if (schema.PatternProperties != null)
            {
                var matches = schema.PatternProperties.Keys.Where(pattern =>
                {
                    try { return Regex.IsMatch(prop, pattern, RegexOptions.ECMAScript); }
                    catch (ArgumentException)
                    {
                        throw new ArgumentException($"Error in schema: Property pattern '{pattern}' in {schema}/patternProperties is not valid regex.");
                    }
                });
                if (matches.Any())
                {
                    var patternPropCollection = new AnnotationCollection();
                    foreach (var match in matches)
                    {
                        var patternProp = schema.PatternProperties[match];
                        patternPropCollection.AddAnnotations(ValidateInstance(patternProp, jProp!));
                    }
                    if (patternPropCollection.Valid)
                        annotations.AddAnnotation(prop);
                    definedProperties.Add(prop);
                    annotations.AddAnnotations(patternPropCollection);
                }
            }
            if (schema.DependentRequired?.TryGetValue(prop, out ISet<string>? dependentsRequired) == true
                    && !dependentsRequired!.IsSubsetOf(jObjProps))
            {
                annotations.AddError($"Property '{prop}' is missing the dependent properties: " +
                                  $"{string.Join(',', dependentsRequired)}.",
                                  schema, "dependentRequired", jObject);
            }
        }
        if (schema.AdditionalProperties != null && !schema.AutoAssertsToTrue)
        {
            var additionalProps = jObjProps.Except(definedProperties);
            if (additionalProps.Any())
            {
                if (schema.AdditionalProperties.AutoAssertsToFalse)
                {
                    annotations.AddError("Additional properties to those defined are not allowed.",
                            schema, "additionalProperties", jObject);
                }
                else
                {
                    foreach (var prop in additionalProps)
                    {
                        var additionalPropCollection = ValidateInstance(schema.AdditionalProperties, jObject[prop]!);
                        if (additionalPropCollection.Valid)
                            annotations.AddAnnotation(prop);
                        annotations.AddAnnotations(additionalPropCollection);
                    }
                }
            }
        }
        annotations.AddAnnotations(ValidateInSubschemas(schema, jObject, AnnotationAddType.Properties),
                                    AnnotationAddType.ErrorsAndProperties);
        if (schema.UnevaluatedProperties != null && !schema.UnevaluatedProperties.AutoAssertsToTrue)
        {
            var unevaluatedProps = jObjProps.Except(annotations.EvaluatedProperties);
            if (unevaluatedProps.Any())
            {
                if (schema.UnevaluatedProperties.AutoAssertsToFalse)
                {
                    annotations.AddError("One or more properties were not successfully evaluated and unevaluated properties are not allowed.",
                        schema, "unevaluatedProperties", jObject);
                }
            }
            else
            {
                foreach (var prop in unevaluatedProps)
                    annotations.AddAnnotations(ValidateInstance(schema.UnevaluatedProperties, jObject[prop]!));
            }
        }
        var requiredMissing = schema.Required?.Except(jObjProps);
        if (requiredMissing?.Any() == true)
            annotations.AddError($"Required property or properties missing: {string.Join(',', requiredMissing)}.",
                schema, "required", jObject);
        return annotations;
    }

    private AnnotationCollection ValidateArray(JsonSchema schema, ValueBase? token)
    {
        var annotations = new AnnotationCollection();
        var array = (JsonArray)token!;
        if (schema.MinItems.GreaterThan(array.Count))
            annotations.AddError($"The minimum items required is {schema.MinItems}.", schema, "minItems", array);
        if (schema.MaxItems.LessThan(array.Count))
            annotations.AddError($"The maximum items allowed is {schema.MaxItems}.", schema, "maxItems", array);
        if (schema.UniqueItems.IsTrue() &&
            array.GroupBy(e => e, EqualityComparer<ValueBase?>.Default)
            .Where(g => g.Count() > 1)
            .Select(e => e.Key).Count() > 1)
            annotations.AddError("Duplicate items not allowed.", schema, "uniqueItems", array);
        int contains = 0;
        var prefixCollection = new AnnotationCollection();
        var itemsCollection = new AnnotationCollection();
        var itemsIndex = 0;
        for (var i = 0; i < array.Count; i++)
        {
            if (schema.PrefixItems != null && i < schema.PrefixItems.Count)
            {
                prefixCollection.AddAnnotations(ValidateInstance(schema.PrefixItems[i], array[i]));
                itemsIndex++;
            }
            else if (schema.Items != null)
            {
                itemsCollection.AddAnnotations(ValidateInstance(schema.Items, array[i]));
            }
            if (schema.Contains != null)
            {
                var containsCollection = ValidateInstance(schema.Contains, array[i]);
                if (containsCollection.Valid)
                {
                    annotations.AddAnnotation(i);
                    contains++;
                }
            }
        }
        // both prefixItems and items are all or nothing with respect to which
        // items in the array are considered "evaluated"
        if (schema.PrefixItems != null && prefixCollection.Valid)
        {
            for (var i = 0; i < itemsIndex; i++)
                annotations.AddAnnotation(i);
        }
        annotations.AddAnnotations(prefixCollection);
        if (schema.Items != null && itemsCollection.Valid)
        {
            for (var i = itemsIndex; i < array.Count; i++)
                annotations.AddAnnotation(i);
        }
        annotations.AddAnnotations(itemsCollection);
        if (schema.Contains != null)
        {
            if (!schema.MinContains.HasValue && contains < 1 || schema.MinContains.GreaterThan(contains))
                annotations.AddError("Instance of array contains fewer valid values than expected.",
                    schema, "minContains", array);
            if (schema.MaxContains.LessThan(contains))
                annotations.AddError("Instance of array contains more valid values than expected.",
                    schema, "maxContains", array);
        }
        annotations.AddAnnotations(ValidateInSubschemas(schema, array, AnnotationAddType.Items), AnnotationAddType.ErrorsAndItems);

        if (schema.UnevaluatedItems != null && !schema.UnevaluatedItems.AutoAssertsToTrue &&
            annotations.EvaluatedItems.Count < array.Count)
        {
            if (schema.UnevaluatedItems.AutoAssertsToFalse)
            {
                annotations.AddError("One or more items were not successfully evaluated, and unevaluated items are not allowed.", schema, "unevaluatedItems", array);
            }
            else
            {
                for (var i = 0; i < array.Count; i++)
                {
                    if (!annotations.EvaluatedItems.Contains(i))
                        annotations.AddAnnotations(ValidateInstance(schema.UnevaluatedItems, array[i]));
                }
            }
        }
        return annotations;
    }

    private AnnotationCollection ValidateInSubschemas(JsonSchema schema, ValueBase? token, AnnotationAddType addType)
    {
        var annotations = new AnnotationCollection();
        if (schema.AllOf != null)
        {
            var allOfAnnotations = schema.AllOf.Select(s => ValidateInstance(s, token));
            var allAdd = addType;
            if (allOfAnnotations.Any(a => !a.Valid))
            {
                allAdd = AnnotationAddType.Errors;
                annotations.AddError("Validation failed for one or more schemas.", schema, "allOf", token);
            }
            annotations.AddAnnotations(allOfAnnotations, allAdd);
        }
        if (schema.AnyOf != null)
        {
            var anyOfAnnotations = schema.AnyOf.Select(s => ValidateInstance(s, token));
            var anyAdd = addType;
            if (!anyOfAnnotations.Any(a => a.Valid))
            {
                anyAdd = AnnotationAddType.Errors;
                annotations.AddError("Validation failed due to input not validating against any schema.", schema, "anyOf", token);
            }
            annotations.AddAnnotations(anyOfAnnotations, anyAdd);
        }
        if (schema.OneOf != null)
        {
            var failMessage = string.Empty;
            var oneOfAnnotations = schema.OneOf.Select(s => ValidateInstance(s, token)).ToList();
            var oneOfAdd = addType;
            if (oneOfAnnotations.Count(a => a.Valid) > 1)
                failMessage = "Validation failed due to input validating against more than one schema.";
            else if (!oneOfAnnotations.Any(a => a.Valid))
                failMessage = "Validation failed due to input not validating against any schema.";
            if (!string.IsNullOrEmpty(failMessage))
            {
                oneOfAdd = AnnotationAddType.Errors;
                annotations.AddError(failMessage, schema, "oneOf", token);
            }
            annotations.AddAnnotations(oneOfAnnotations, oneOfAdd);
        }
        if (schema.Not != null)
        {
            var notCollection = ValidateInstance(schema.Not, token);
            if (notCollection.Valid)
                annotations.AddError($"Validation failed.", schema, "not", token);
        }
        if (schema.If != null)
        {
            var ifCollection = ValidateInstance(schema.If, token);
            if (ifCollection.Valid)
            {
                annotations.AddAnnotations(ifCollection, addType);
                if (schema.Then != null)
                    annotations.AddAnnotations(ValidateInstance(schema.Then, token), addType | AnnotationAddType.Errors);
            }
            else if (schema.Else != null)
            {
                annotations.AddAnnotations(ValidateInstance(schema.Else, token), addType | AnnotationAddType.Errors);
            }
            else
            {
                annotations.AddAnnotations(ifCollection, addType | AnnotationAddType.Errors);
            }
        }
        if (!string.IsNullOrEmpty(schema.Ref))
        {
            var definition = _refs.GetReference(schema.Ref)
                ?? throw new JsonSchemaException($"Error in schema: Unable to resolve reference '{schema.Ref}' in {schema}.", schema);
            annotations.AddAnnotations(ValidateInstance(definition, token), addType | AnnotationAddType.Errors);
        }
        return annotations;
    }

    private IEnumerable<string> ValidateAgainstSchema(ValueBase? token)
    {
        if (Schema != null)
        {
            var annotations = ValidateInstance(Schema, token);
            if (!annotations.Valid)
                return annotations.Errors.Select(e => e.Error);
        }
        return new List<string>();
    }

    public ValueBase? ValidateAndDeserialize(string fileName, out IEnumerable<string> errors)
    {
        using var fs = new FileStream(fileName, FileMode.Open);
        using var sr = new StreamReader(fs);
        return ValidateAndDeserialize(sr, out errors);
    }

    /// <summary>
    /// Validate and deserialize a JSON object against the validator's schema.
    /// </summary>
    /// <param name="reader">The text reader.</param>
    /// <param name="errors">An enumeration of the errors encountered.</param>
    /// <returns>A parsed JSON as a <see cref="ValueBase"/>, which can
    /// be <c>null</c>.</returns>
    public ValueBase? ValidateAndDeserialize(TextReader reader, out IEnumerable<string> errors)
    {
        var json = reader.ReadToEnd();
        var deserializer = new JsonDeserializer();
        var jsonObject = deserializer.Deserialize(json);
        if (deserializer.Errors.Count > 0)
        {
            errors = deserializer.Errors.Select(e => e.ToJson());
            return null;
        }
        errors = ValidateAgainstSchema(jsonObject);
        return jsonObject;
    }

    /// <summary>
    /// Gets the validator's applied <see cref="JsonSchema"/> instance.
    /// </summary>
    public JsonSchema Schema { get; }

    [GeneratedRegex("^[0-9a-fA-F]+$")]
    private static partial Regex HexRegex();
}

