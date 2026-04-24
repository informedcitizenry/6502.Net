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

namespace Sixty502DotNet.Shared.Json;

public sealed class JsonSchemaReferenceCollection
{
    readonly Dictionary<string, JsonSchema> _collection;
    
    public JsonSchemaReferenceCollection(JsonSchema root)
    {
        _collection = new Dictionary<string, JsonSchema>();
        Root = root;
        Add(Root);
    }

    void Add(IReadOnlyDictionary<string, JsonSchema>? schemas)
    {
        if (schemas != null)
        {
            foreach (var schema in schemas)
                Add(schema.Value);
        }
    }

    void Add(IList<JsonSchema>? schemas)
    {
        if (schemas != null)
        {
            foreach (var schema in schemas)
            {
                Add(schema);
            }
        }
    }

    private void Add(JsonSchema? schema)
    {
        if (schema != null)
        {
            var jsonPointer = schema.GetPath(false) ?? "#";
            _collection.Add(jsonPointer, schema);

            var basePath = schema.GetPath(true);
            var urlPath = basePath ?? schema.Id;
            if (!urlPath.Equals("#"))
            {
                _collection.Add($"{urlPath}#{jsonPointer}", schema);
                if (!string.IsNullOrEmpty(schema.Id))
                {
                    _collection.Add($"{urlPath}", schema);
                    _collection.Add($"{urlPath}#", schema);
                    if (Uri.TryCreate(schema.Id, UriKind.RelativeOrAbsolute, out var idUrl) &&
                        idUrl.IsAbsoluteUri)
                        _collection.Add($"{idUrl.LocalPath}", schema);
                    else
                        _collection.Add($"/{schema.Id}", schema);

                }
                if (!string.IsNullOrEmpty(schema.Anchor))
                    _collection.Add($"{urlPath}#{schema.Anchor.Trim('#')}", schema);
            }
            else if (!string.IsNullOrEmpty(schema.Anchor))
                _collection.Add($"{urlPath}{schema.Anchor.Trim('#')}", schema);
            else _collection.Add(jsonPointer, schema);
            if (!string.IsNullOrEmpty(schema.RootBasePath))
                _collection.Add($"{schema.RootBasePath}#{jsonPointer}", schema);

            Add(schema.AdditionalProperties);
            Add(schema.AllOf);
            Add(schema.AnyOf);
            Add(schema.Contains);
            Add(schema.ContentSchema);
            Add(schema.Defs);
            Add(schema.DependentSchemas);
            Add(schema.Else);
            Add(schema.If);
            Add(schema.Not);
            Add(schema.OneOf);
            Add(schema.PatternProperties);
            Add(schema.PrefixItems);
            Add(schema.Properties);
            Add(schema.PropertyNames);
            Add(schema.Then);
            Add(schema.UnevaluatedItems);
            Add(schema.UnevaluatedProperties);
        }
    }
    
    public JsonSchema? GetReference(string @ref)
    {
        _ = _collection.TryGetValue(@ref, out var schema);
        return schema;
    }

    public JsonSchema Root { get; }
}
