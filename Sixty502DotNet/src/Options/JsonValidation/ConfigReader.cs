//-----------------------------------------------------------------------------
// Copyright (c) 2017-2022 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;
using System.IO;

namespace Sixty502DotNet
{
    public static class ConfigReader
    {
        private static string ReturnConfig(string json)
        {
            try
            {
                var sep = Path.PathSeparator;
                return File.ReadAllText($"\"JsonValidation{sep}{json}\"");
            }
            catch (IOException)
            {
                Console.Error.WriteLine($"Could not open file '{json}'.");
                return "";
            }
        }

        public static string Min()
            => ReturnConfig("ConfigMin.json");

        public static string Schema()
            => ReturnConfig("ConfigSchema.json");
    }

}
