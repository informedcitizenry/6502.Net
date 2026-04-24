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

using System.Text.Json.Serialization;
using Sixty502DotNet.Shared.Eval;

namespace Sixty502DotNet.Shared.Json;

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

public sealed class ValidationError
{
    public ValidationError(string message, JsonSchema schema, string keyword, string instanceLocation)
    {
        Error = message;
        KeywordLocation = $"{schema.GetBasePath()}/{keyword}";
        var absoluteUri = schema.GetBasePath();
        if (!string.IsNullOrEmpty(absoluteUri))
        {
        }

        InstanceLocation = instanceLocation;
    }

    public override string ToString()
    {
        var messageFrament = Error.Length > 39 ? Error[..40] : Error;
        return $"@{InstanceLocation}:{KeywordLocation}=>'{messageFrament}'";
    }
    
    public string Error { get; }

    public string KeywordLocation { get; }
    
    public string InstanceLocation { get; }
}

public class AnnotationCollection
{
    public List<ValidationError> Errors { get; } = [];

    [JsonIgnore]
    public ISet<string> EvaluatedProperties { get; } = new HashSet<string>();

    [JsonIgnore]
    public ISet<int> EvaluatedItems { get; } = new HashSet<int>();

    public bool Valid => Errors.Count == 0;

    public void AddAnnotation(string property)
        => EvaluatedProperties.Add(property);

    public void AddAnnotation(int index)
        => EvaluatedItems.Add(index);
    
    public void AddError(string message, JsonSchema schema, string keywordLocation, Value? token)
        => Errors.Add(new ValidationError(message, schema, keywordLocation, token?.JsonPath ?? "#"));

    
    public void AddAnnotations(AnnotationCollection other, AnnotationAddType addType = AnnotationAddType.Errors)
    {
        if (addType.HasFlag(AnnotationAddType.Errors))
            Errors.AddRange(other.Errors);
        if (addType.HasFlag(AnnotationAddType.Items))
            EvaluatedItems.UnionWith(other.EvaluatedItems);
        if (addType.HasFlag(AnnotationAddType.Properties))
            EvaluatedProperties.UnionWith(other.EvaluatedProperties);
    }
    
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
