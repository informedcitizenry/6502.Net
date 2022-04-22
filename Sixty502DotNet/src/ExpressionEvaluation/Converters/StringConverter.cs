//-----------------------------------------------------------------------------
// Copyright (c) 2017-2022 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System.Globalization;
using System.Text.RegularExpressions;

namespace Sixty502DotNet
{
    /// <summary>
    /// The base string converter class that processes a string to its unescapes
    /// form. This class must be inherited.
    /// </summary>
    public abstract class BaseStringConverter : ICustomConverter
    {
        /* for surrogate pairs */
        private static readonly Regex s_surrogates = new(@"\\u([dD][8-9a-bA-B][0-9a-fA-F]{2})\\u([dD][c-fC-F][0-9a-fA-F]{2})", RegexOptions.Compiled);

        /* for all other \uhhhh+ */
        private static readonly Regex s_unicodepoint = new(@"\\[uU]([0-9a-fA-F]{4,8})", RegexOptions.Compiled);

        public virtual Value Convert(string str)
        {
            var unic = s_surrogates.Replace(str, u =>
               char.ToString((char)ushort.Parse(u.Groups[1].Value, NumberStyles.AllowHexSpecifier)) +
               char.ToString((char)ushort.Parse(u.Groups[2].Value, NumberStyles.AllowHexSpecifier)));
            unic = s_unicodepoint.Replace(unic, u =>
                char.ConvertFromUtf32(int.Parse(u.Groups[1].Value, NumberStyles.AllowHexSpecifier)));
            var unesc = Regex.Unescape(unic.Replace("\\x0", "\\x").Replace("\\0", "\\"));
            return new Value(unesc);
        }
    }

    /// <summary>
    /// Unescapes a string.
    /// </summary>
    public class StringConverter : BaseStringConverter
    {
        public override Value Convert(string str)
        {
            if (str.StartsWith("\"\"\""))
            {
                return new Value(str[3..^3]);
            }
            return base.Convert(str);
        }
    }

    /// <summary>
    /// Unescapes a char and validates its form.
    /// </summary>
    public class CharConverter : BaseStringConverter
    {
        public override Value Convert(string str)
        {
            var charValue = base.Convert(str);
            var chrString = charValue.ToString(true);
            if (chrString.Length > 1 ||
                char.IsSurrogatePair(chrString, 0))
            {
                throw new Error($"{charValue} is not a valid character literal.");
            }
            return charValue;
        }
    }
}
