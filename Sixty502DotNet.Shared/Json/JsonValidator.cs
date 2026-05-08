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
    
    public JsonValidator(string schemaJson)
    {
        var schemaBuilder = new JsonSchemaBuilder();
        Schema = schemaBuilder.BuildFromJson(schemaJson, true);
        _refs = new JsonSchemaReferenceCollection(Schema);
    }

    public IEnumerable<string> Validate(Value token)
    {
        return ValidateAgainstSchema(token);
    }
    
    private AnnotationCollection ValidateInstance(JsonSchema schema, Value? token)
    {
        var annotations = new AnnotationCollection();
        if (schema.AutoAssertsToFalse)
        {
            annotations.AddError("Schema will always invalidate.", schema, schema.ToString(), token);
        }
        else if (!schema.AutoAssertsToTrue)
        {
            if (token == null || !schema.MatchesTokenType(token))
                annotations.AddError(
                    $"Expected type '{schema.Type}' but got '{token?.TypeTag.ToString().ToLower() ?? "null"}'",
                    schema, "type", token);
            if (token != null && schema.Const != null && !token.Equals(schema.Const))
                annotations.AddError($"Expected constant '{schema.Const}'.", schema, "const", token);
            if (schema.Enum != null && !schema.Enum.Any(o => o.Equals(token)))
                annotations.AddError($"'{token}' is not a valid option.", schema, "enum", token);
            var tokenType = token?.TypeTag ?? TypeTag.Undefined;
            switch (tokenType)
            {
                case TypeTag.Array:
                    annotations.AddAnnotations(ValidateArray(schema, token), AnnotationAddType.ErrorsAndItems);
                    break;
                case TypeTag.Dictionary:
                    annotations.AddAnnotations(ValidateObject(schema, token), AnnotationAddType.ErrorsAndProperties);
                    break;
                default:
                    if (token?.TypeTag == TypeTag.String)
                        annotations.AddAnnotations(ValidateString(schema, token));
                    else if (token?.IsNumber() == true)
                        annotations.AddAnnotations(ValidateNumber(schema, token));
                    annotations.AddAnnotations(ValidateInSubschemas(schema, token, AnnotationAddType.None));
                    break;
            }
        }
        return annotations;
    }
    
    private static AnnotationCollection ValidateNumber(JsonSchema schema, Value token)
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
    
    private static AnnotationCollection ValidateString(JsonSchema schema, Value token)
    {
        var annotations = new AnnotationCollection();
        var tokenVal = token.ToString();
        if (!string.IsNullOrEmpty(schema.Pattern) && !Regex.IsMatch(tokenVal, schema.Pattern))
            annotations.AddError($"'{tokenVal}' does not match pattern '{schema.Pattern}'.",
                schema, "pattern", token);
        if (schema.MinLength > tokenVal.Length)
            annotations.AddError($"'{tokenVal}' has fewer characters than the minimum number of {schema.MinLength}.",
                schema, "minLength", token);
        if (schema.MaxLength < tokenVal.Length)
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

    private AnnotationCollection ValidateObject(JsonSchema schema, Value? token)
    {
        var annotations = new AnnotationCollection();
        var jObject = token?.AsDictionary() ?? new Dictionary();
        var jObjProps = jObject.Keys.Select(k => k.AsString()).ToHashSet();

        if (schema.MinProperties > jObjProps.Count)
            annotations.AddError($"Object has too few properties.", schema, "minProperties", new Value(jObject));
        if (schema.MaxProperties < jObjProps.Count)
            annotations.AddError($"Object has too many properties.", schema, "maxProperties", new Value(jObject));

        var definedProperties = new HashSet<string>();
        foreach (var prop in jObjProps)
        {
            if (schema.DependentSchemas != null && schema.DependentSchemas.TryGetValue(prop, out var dependent))
            {
                var dependencyCollection = ValidateObject(dependent, token);
                if (!dependencyCollection.Valid)
                    annotations.AddError($"Dependent schema failed validation for property '{prop}'.", schema, "dependentSchemas", new Value(jObject));
                annotations.AddAnnotations(dependencyCollection, AnnotationAddType.ErrorsAndProperties);
            }
            if (schema.PropertyNames != null)
                annotations.AddAnnotations(ValidateInstance(schema.PropertyNames, jObject[prop]));
            var jProp = jObject[prop];
            if (schema.Properties?.TryGetValue(prop, out var propertySchema) is true)
            {
                var propertyCollection = ValidateInstance(propertySchema, jProp);
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
                }).ToArray();
                if (matches.Length > 0)
                {
                    var patternPropCollection = new AnnotationCollection();
                    foreach (var match in matches)
                    {
                        var patternProp = schema.PatternProperties[match];
                        patternPropCollection.AddAnnotations(ValidateInstance(patternProp, jProp));
                    }
                    if (patternPropCollection.Valid)
                        annotations.AddAnnotation(prop);
                    definedProperties.Add(prop);
                    annotations.AddAnnotations(patternPropCollection);
                }
            }
            if (schema.DependentRequired?.TryGetValue(prop, out var dependentsRequired) == true
                    && !dependentsRequired.IsSubsetOf(jObjProps))
            {
                annotations.AddError($"Property '{prop}' is missing the dependent properties: " +
                                  $"{string.Join(',', dependentsRequired)}.",
                                  schema, "dependentRequired", new Value(jObject));
            }
        }
        if (schema is { AdditionalProperties: not null, AutoAssertsToTrue: false })
        {
            var additionalProps = jObjProps.Except(definedProperties).ToArray();
            if (additionalProps.Any())
            {
                if (schema.AdditionalProperties.AutoAssertsToFalse)
                {
                    annotations.AddError("Additional properties to those defined are not allowed.",
                            schema, "additionalProperties", new Value(jObject));
                }
                else
                {
                    foreach (var prop in additionalProps)
                    {
                        var additionalPropCollection = ValidateInstance(schema.AdditionalProperties, jObject[prop]);
                        if (additionalPropCollection.Valid)
                            annotations.AddAnnotation(prop);
                        annotations.AddAnnotations(additionalPropCollection);
                    }
                }
            }
        }
        annotations.AddAnnotations(ValidateInSubschemas(schema, new Value(jObject), AnnotationAddType.Properties),
                                    AnnotationAddType.ErrorsAndProperties);
        if (schema.UnevaluatedProperties is { AutoAssertsToTrue: false })
        {
            var unevaluatedProps = jObjProps.Except(annotations.EvaluatedProperties).ToArray();
            if (unevaluatedProps.Any())
            {
                if (schema.UnevaluatedProperties.AutoAssertsToFalse)
                {
                    annotations.AddError("One or more properties were not successfully evaluated and unevaluated properties are not allowed.",
                        schema, "unevaluatedProperties", new Value(jObject));
                }
            }
            else
            {
                foreach (var prop in unevaluatedProps)
                    annotations.AddAnnotations(ValidateInstance(schema.UnevaluatedProperties, jObject[prop]));
            }
        }
        var requiredMissing = schema.Required?.Except(jObjProps).ToArray() ?? [];
        if (requiredMissing.Length != 0)
            annotations.AddError($"Required property or properties missing: {string.Join(',', requiredMissing)}.",
                schema, "required", new Value(jObject));
        return annotations;
    }

    private AnnotationCollection ValidateArray(JsonSchema schema, Value? token)
    {
        var annotations = new AnnotationCollection();
        var array = token?.AsArray()?.ToList() ?? [];
        if (schema.MinItems >array.Count)
            annotations.AddError($"The minimum items required is {schema.MinItems}.", schema, "minItems", new Value(array, TypeTag.Array));
        if (schema.MaxItems < array.Count)
            annotations.AddError($"The maximum items allowed is {schema.MaxItems}.", schema, "maxItems",  new Value(array, TypeTag.Array));
        if (schema.UniqueItems.IsTrue() &&
            array.GroupBy(e => e, EqualityComparer<Value?>.Default)
            .Where(g => g.Count() > 1)
            .Select(e => e.Key).Count() > 1)
            annotations.AddError("Duplicate items not allowed.", schema, "uniqueItems",  new Value(array, TypeTag.Array));
        var contains = 0;
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
            if (!schema.MinContains.HasValue && contains < 1 || schema.MinContains > contains)
                annotations.AddError("Instance of array contains fewer valid values than expected.",
                    schema, "minContains", new Value(array, TypeTag.Array));
            if (schema.MaxContains < contains)
                annotations.AddError("Instance of array contains more valid values than expected.",
                    schema, "maxContains", new Value(array, TypeTag.Array));
        }
        annotations.AddAnnotations(ValidateInSubschemas(schema, new Value(array, TypeTag.Array), AnnotationAddType.Items), AnnotationAddType.ErrorsAndItems);

        if (schema.UnevaluatedItems is { AutoAssertsToTrue: false } &&
            annotations.EvaluatedItems.Count < array.Count)
        {
            if (schema.UnevaluatedItems.AutoAssertsToFalse)
            {
                annotations.AddError("One or more items were not successfully evaluated, and unevaluated items are not allowed.", schema, "unevaluatedItems",  new Value(array, TypeTag.Array));
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

    private AnnotationCollection ValidateInSubschemas(JsonSchema schema, Value? token, AnnotationAddType addType)
    {
        var annotations = new AnnotationCollection();
        if (schema.AllOf != null)
        {
            var allOfAnnotations = schema.AllOf.Select(s => ValidateInstance(s, token));
            var allAdd = addType;
            var annotationCollections = allOfAnnotations as AnnotationCollection[] ?? allOfAnnotations.ToArray();
            if (annotationCollections.Any(a => !a.Valid))
            {
                allAdd = AnnotationAddType.Errors;
                annotations.AddError("Validation failed for one or more schemas.", schema, "allOf", token);
            }
            annotations.AddAnnotations(annotationCollections, allAdd);
        }
        if (schema.AnyOf != null)
        {
            var anyOfAnnotations = schema.AnyOf.Select(s => ValidateInstance(s, token));
            var anyAdd = addType;
            var annotationCollections = anyOfAnnotations as AnnotationCollection[] ?? anyOfAnnotations.ToArray();
            if (!annotationCollections.Any(a => a.Valid))
            {
                anyAdd = AnnotationAddType.Errors;
                annotations.AddError("Validation failed due to input not validating against any schema.", schema, "anyOf", token);
            }
            annotations.AddAnnotations(annotationCollections, anyAdd);
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
                ?? throw new JsonSchemaException($"Error in schema: Unable to resolve reference '{schema.Ref}' in {schema}.");
            annotations.AddAnnotations(ValidateInstance(definition, token), addType | AnnotationAddType.Errors);
        }
        return annotations;
    }

    private IEnumerable<string> ValidateAgainstSchema(Value? token)
    {
        if (Schema != null)
        {
            var annotations = ValidateInstance(Schema, token);
            if (!annotations.Valid)
                return annotations.Errors.Select(e => e.Error);
        }
        return new List<string>();
    }
    
    public JsonSchema? Schema { get; }

    [GeneratedRegex("^[0-9a-fA-F]+$")]
    private static partial Regex HexRegex();
}