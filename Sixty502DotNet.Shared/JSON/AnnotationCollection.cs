//-----------------------------------------------------------------------------
// Copyright (c) 2017-2023 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System.Text.Json.Serialization;

namespace Sixty502DotNet.Shared;

/// <summary>
/// The flag representing the type of annotations to add from 
/// one <see cref="AnnotationCollection"/> to another.
/// </summary>
[Flags]
public enum AnnotationAddType
{
    None = 0,
    Errors = 1,
    Items = 2,
    Properties = 4,
    ErrorsAndItems = Errors | Items,
    ErrorsAndProperties = Errors | Properties,
    All = Errors | Items | Properties
};

/// <summary>
/// Represents details of an error when an instance fails validation
/// against a schema, including the schema keyword and the instance location.
/// </summary>
public sealed class ValidationError
{
    /// <summary>
    /// Constructs a new instance of the <see cref="ValidationError"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="schema">The <see cref="Json.Schema"/> object from which the 
    /// error originates.</param>
    /// <param name="keyword">The keyword of the schema from which the 
    /// error originates.</param>
    /// <param name="instanceLocation">The location in the JSON instance.</param>
    public ValidationError(string message, JsonSchema schema, string keyword, string instanceLocation)
    {
        Error = message;
        Schema = schema;
        KeywordLocation = $"{schema.GetBasePath()}/{keyword}";
        var absoluteUri = schema.GetBasePath();
        if (!string.IsNullOrEmpty(absoluteUri))
            AbsoluteKeywordLocation = $"{absoluteUri}/{KeywordLocation}";
        else
            AbsoluteKeywordLocation = "";
        InstanceLocation = instanceLocation;
    }

    public override string ToString()
    {
        var messageFrament = Error.Length > 39 ? Error[..40] : Error;
        return $"@{InstanceLocation}:{KeywordLocation}=>'{messageFrament}'";
    }

    /// <summary>
    /// Gets the error's message.
    /// </summary>
    public string Error { get; }

    /// <summary>
    /// Gets the <see cref="Json.Schema"/> object from which the error originates.
    /// </summary>
    public JsonSchema Schema { get; }

    /// <summary>
    /// Gets the absolute URI of the location of the keyword in the schema from 
    /// which the error originates.
    /// </summary>
    public string AbsoluteKeywordLocation { get; }

    /// <summary>
    /// Gets the JSON pointer of the keyword in the schema from 
    /// which the error originates.
    /// </summary>
    public string KeywordLocation { get; }

    /// <summary>
    /// Gets the isntance location failing validation.
    /// </summary>
    public string InstanceLocation { get; }
}

/// <summary>
/// A class representing the annotations and errors collected
/// during schema validation against an instance.
/// </summary>
public class AnnotationCollection
{
    /// <summary>
    /// Gets the list of errors encounterd during validation.
    /// </summary>
    public List<ValidationError> Errors { get; }

    /// <summary>
    /// Gets the collection of properties successfully evaluated
    /// during validation.
    /// </summary>
    [JsonIgnore]
    public ISet<string> EvaluatedProperties { get; }

    /// <summary>
    /// Gets the collection of array members (array indeces) evaluated
    /// during validation.
    /// </summary>
    [JsonIgnore]
    public ISet<int> EvaluatedItems { get; }

    /// <summary>
    /// Gets whether the validation is valid.
    /// </summary>
    public bool Valid => Errors.Count == 0;

    /// <summary>
    /// Constructs a new instance of an <see cref="AnnotationCollection"/>.
    /// </summary>
    public AnnotationCollection()
    {
        Errors = new List<ValidationError>();
        EvaluatedProperties = new HashSet<string>();
        EvaluatedItems = new HashSet<int>();
    }

    /// <summary>
    /// Adds a property successfully validated to the collection of
    /// evaluated properties.
    /// </summary>
    /// <param name="property">The property name.</param>
    public void AddAnnotation(string property)
        => EvaluatedProperties.Add(property);

    /// <summary>
    /// Adds the array index successfully validated to the collection of 
    /// evaluated items.
    /// </summary>
    /// <param name="index">The array's element index.</param>
    public void AddAnnotation(int index)
        => EvaluatedItems.Add(index);

    /// <summary>
    /// Adds an error to the collection to capture a failed validation.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="schema">The <see cref="Json.Schema"/> object from which the 
    /// error originates.</param>
    /// <param name="keywordLocation">The JSON pointer of the keyword in the schema 
    /// from which the error originates.</param>
    /// <param name="token">The parsed instance.</param>
    public void AddError(string message, JsonSchema schema, string keywordLocation, ValueBase? token)
        => Errors.Add(new ValidationError(message, schema, keywordLocation, token?.JsonPath ?? "#"));

    /// <summary>
    /// Collect annotations from another <see cref="AnnotationCollection"/>. 
    /// </summary>
    /// <param name="other">The other collection.</param>
    /// <param name="addType">The types of annotations to collect.</param>
    public void AddAnnotations(AnnotationCollection other, AnnotationAddType addType = AnnotationAddType.Errors)
    {
        if (addType.HasFlag(AnnotationAddType.Errors))
            Errors.AddRange(other.Errors);
        if (addType.HasFlag(AnnotationAddType.Items))
            EvaluatedItems.UnionWith(other.EvaluatedItems);
        if (addType.HasFlag(AnnotationAddType.Properties))
            EvaluatedProperties.UnionWith(other.EvaluatedProperties);
    }

    /// <summary>
    /// Collection annotations from a collection of other <see cref="AnnotationCollection"/> items.
    /// </summary>
    /// <param name="collections">The collection of <see cref="AnnotationCollection"/> items.</param>
    /// <param name="addType">The types of annotations to collect.</param>
    public void AddAnnotations(IEnumerable<AnnotationCollection> collections, AnnotationAddType addType = AnnotationAddType.Errors)
    {
        if (addType.HasFlag(AnnotationAddType.Errors))
            Errors.AddRange(collections.SelectMany(a => a.Errors));
        if (addType.HasFlag(AnnotationAddType.Items))
            EvaluatedItems.UnionWith(collections.SelectMany(a => a.EvaluatedItems));
        if (addType.HasFlag(AnnotationAddType.Properties))
            EvaluatedProperties.UnionWith(collections.SelectMany(a => a.EvaluatedProperties));
    }
}

