//-----------------------------------------------------------------------------
// Copyright (c) 2017-2021 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace Core6502DotNet.Json
{

    public static class JTokenExtension
    {
        /// <summary>
        /// Represent the <see cref="JToken"/> path as a JSON pointer.
        /// </summary>
        /// <param name="token">This token.</param>
        /// <returns>The JSON pointer representing the token's path.</returns>
        public static string FullPath(this JToken token)
        {
            var paths = new Stack<string>();
            for (var current = token; current != token.Root; current = current.Parent)
            {
                if (current.Parent?.Type == JTokenType.Property)
                    continue;
                var path = current.Path.Split('.')[^1];
                paths.Push(path);
                if (current.Parent?.Type == JTokenType.Array)
                {
                    while (current.Parent != token.Root && current.Type != JTokenType.Property)
                        current = current.Parent;
                }
            }
            return $"/{string.Join('/', paths)}";
        }

        /// <summary>
        /// Represents the <see cref="JToken"/> Path property as a JSON pointer.
        /// </summary>
        /// <param name="token">The token.</param>
        /// <returns>The JSON pointer representation of the token's Path property.</returns>
        public static string ToJsonPointer(this JToken token)
            => "#/" + token.Path.Replace('.', '/').Replace('[', '/').Replace("]", "");

        /// <summary>
        /// Represents the JSON path as a JSON pointer.
        /// </summary>
        /// <param name="path">The path string.</param>
        /// <returns>The JSON pointer representation of the string path.</returns>
        public static string ToJsonPointer(this string path)
            => "#" + path.Replace('.', '/').Replace('[', '/').Replace("]", "");

        /// <summary>
        /// Get the JSON schema type of the <see cref="JTokenType"/>.
        /// </summary>
        /// <param name="type">This type.</param>
        /// <returns>The JSON schema type as a string.</returns>
        public static string SchemaType(this JTokenType type)
            => type.ToString().ToLower().Replace("JTokenType.", "");
    }
}
