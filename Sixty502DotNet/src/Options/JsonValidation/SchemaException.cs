//-----------------------------------------------------------------------------
// Copyright (c) 2017-2021 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;

namespace Sixty502DotNet
{
    /// <summary>
    /// Represents an error encountered during the processing and 
    /// validation of a schema object, for instance if a schema is
    /// malformed or contains invalid property values.
    /// </summary>
    public sealed class SchemaException : Exception
    {
        /// <summary>
        /// Create a new instance of a <see cref="SchemaException"/>.
        /// </summary>
        /// <param name="message">The exception's error message.</param>
        /// <param name="schema">The <see cref="Json.Schema"/> of the error.</param>
        public SchemaException(string message, Schema schema)
            : base(message)
        {
            Schema = schema;
        }

        /// <summary>
        /// Get the <see cref="Json.Schema"/> causing the error.
        /// </summary>
        public Schema Schema { get; }
    }
}
