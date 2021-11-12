//-----------------------------------------------------------------------------
// Copyright (c) 2017-2021 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Core6502DotNet.Json
{
    /// <summary>
    /// A class that serves as an associative collection of <see cref="Schema"/> objects.
    /// </summary>
    public class SchemaReferenceCollection
    {
        readonly Dictionary<string, Schema> _collection;

        /// <summary>
        /// Create a new instance of a <see cref="SchemaReferenceCollection"/> class.
        /// </summary>
        /// <param name="root">The root <see cref="Schema"/> object.</param>
        public SchemaReferenceCollection(Schema root)
        {
            _collection = new Dictionary<string, Schema>();
            Root = root;
            Add(Root);
        }

        void Add(IReadOnlyDictionary<string, Schema> schemas)
        {
            if (schemas != null)
            {
                foreach (var schema in schemas)
                    Add(schema.Value);
            }
        }

        void Add(IList<Schema> schemas)
        {
            if (schemas != null)
            {
                foreach (var schema in schemas)
                    Add(schema);
            }
        }

        void Add(Schema schema)
        {
            if (schema != null)
            {
                var jsonPointer = schema.GetPath(false);
                _collection.Add(jsonPointer, schema);

                var basePath = schema.GetPath(true);
                var urlPath = basePath ?? schema.Id ?? "#";
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
                        _collection.Add($"{urlPath}#{schema.Anchor.TrimOnce('#')}", schema);
                }
                else if (!string.IsNullOrEmpty(schema.Anchor))
                    _collection.Add($"{urlPath}{schema.Anchor.TrimOnce('#')}", schema);
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

        /// <summary>
        /// Get a <see cref="Schema"/> from the <see cref="SchemaReferenceCollection"/>
        /// identified by its reference.
        /// </summary>
        /// <param name="ref">The reference value, either a URI or JSON pointer.</param>
        /// <returns>The referenced schema.</returns>
        public Schema GetReference(string @ref)
        {
            _ = _collection.TryGetValue(@ref, out var schema);
            return schema;
        }

        /// <summary>
        /// Gets the root <see cref="Json.Schema"/> object.
        /// </summary>
        public Schema Root { get; }
    }
}
