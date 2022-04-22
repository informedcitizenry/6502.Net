//-----------------------------------------------------------------------------
// Copyright (c) 2017-2022 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.IO;

namespace Sixty502DotNet
{
    /// <summary>
    /// Represents a class that can be represented as a JSON string. This class must
    /// be inherited.
    /// </summary>
    public abstract class JsonSerializable
    {
        /// <summary>
        /// Gets the JSON-representation of the <see cref="JsonSerializable"/> object.
        /// </summary>
        /// <returns>The JSON string representation of the class object.</returns>
        public string ToJson()
        {
            using var strWriter = new StringWriter();
            using var jsWriter = new JsonTextWriter(strWriter);
            var serializer = new JsonSerializer
            {
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore,
                Formatting = Formatting.Indented,
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };
            serializer.Serialize(jsWriter, this, typeof(Schema));
            return strWriter.ToString();
        }

        /// <summary>
        /// Gets the JSON-representation of the <see cref="JsonSerializable"/> object, including
        /// all default and null options.
        /// </summary>
        /// <returns>The JSON string representation of the class object.</returns>
        public string ToFullJson()
        {
            using var strWriter = new StringWriter();
            using var jsWriter = new JsonTextWriter(strWriter);
            var serializer = new JsonSerializer
            {
                Formatting = Formatting.Indented,
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };
            serializer.Serialize(jsWriter, this, typeof(Schema));
            return strWriter.ToString();//s_nullRegex.Replace(strWriter.ToString(), "$1\"\"");
        }

        /// <summary>
        /// Outputs a given JSON string to a formatted version.
        /// </summary>
        /// <param name="json">The JSON string.</param>
        /// <returns>A formatted version of the JSON.</returns>
        public static string ToFormattedJson(string json)
        {
            using var stringReader = new StringReader(json);
            using var stringWriter = new StringWriter();
            var jsonReader = new JsonTextReader(stringReader);
            var jsonWriter = new JsonTextWriter(stringWriter) { Formatting = Formatting.Indented };
            jsonWriter.WriteToken(jsonReader);
            return stringWriter.ToString();
        }
    }
}
