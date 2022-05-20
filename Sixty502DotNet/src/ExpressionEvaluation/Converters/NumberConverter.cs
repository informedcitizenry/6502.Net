//-----------------------------------------------------------------------------
// Copyright (c) 2017-2022 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;
using System.Text.RegularExpressions;

namespace Sixty502DotNet
{
    /// <summary>
    /// A class that is responsible for converting a number string to its
    /// actual numeric value.
    /// </summary>
    public class NumberConverter : ICustomConverter
    {
        private static readonly Regex s_hexDoubleParserRegex =
            new(@"([0-9a-f]+(?:\.[0-9a-f]+)?)(p((\+|-)?\d+))?", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex s_nonHexDoubleParserRegex =
            new(@"(\d+(?:\.\d+)?)([ep]((\+|-)?\d+))?", RegexOptions.Compiled);

        /// <summary>
        /// Converts a non-decimal double string according to its base. This
        /// function also supports exponential notation for non-base 10 values.
        /// </summary>
        /// <param name="str">The non-decimal string.</param>
        /// <param name="atBase">The number base.</param>
        /// <returns>The converted string as a double.</returns>
        public static Value GetDoubleAtBase(string str, int atBase)
        {
            var regex = atBase == 16 ? s_hexDoubleParserRegex : s_nonHexDoubleParserRegex;
            var mantissaExponent = regex.Match(str.Replace("_", ""));
            var mantissaParts = mantissaExponent.Groups[1].Value.Split('.');
            var mantissa = System.Convert.ToInt64(mantissaParts[0], atBase) * 1.0;
            if (mantissaParts.Length > 1)
                mantissa +=
                System.Convert.ToInt64(mantissaParts[1], atBase) * 1.0 / Math.Pow(atBase, mantissaParts[1].Length);
            if (mantissaExponent.Groups.Count > 2 && mantissaExponent.Groups[3].Success)
            {
                var base_ = mantissaExponent.Groups[2].Value.ToLower()[0] == 'e' ? 10 : 2;
                var exponent = double.Parse(mantissaExponent.Groups[3].Value);
                return new Value(mantissa * Math.Pow(base_, exponent));
            }
            return new Value(mantissa);
        }

        /// <summary>
        /// Convert a <see cref="double"/> to an <see cref="int"/> or
        /// <see cref="uint"/> if the converted value is able to be converted.
        /// Otherwise, the returned value is the original value itself.
        /// </summary>
        /// <param name="value">The <see cref="double"/> as an <see cref="IValue"/>.
        /// </param>
        /// <returns>The converted <see cref="int"/> or <see cref="uint"/> as an
        /// <see cref="Value"/> if conversion was successful, otherwise the
        /// original value itself.</returns>
        public static Value ConvertToIntegral(Value value)
        {
            if (value.ToDouble() >= int.MinValue && value.ToDouble() <= uint.MaxValue)
            {
                if (value.ToDouble() <= int.MaxValue)
                {
                    return new Value(unchecked((int)(value.ToLong() & 0xFFFF_FFFF)));
                }
                return new Value((uint)(value.ToLong() & 0xFFFF_FFFF));
            }
            return value;
        }

        public Value Convert(string str)
        {
            str = str.Replace("_", "");
            if (str[0] == '0' && str.Length > 1)
            {
                if (!char.IsDigit(str[1]))
                {
                    return ConvertToIntegral(new Value(System.Convert.ToInt64(str[2..], 8)));
                }
                return ConvertToIntegral(new Value(System.Convert.ToInt64(str, 8)));
            }
            var numVal = new Value(System.Convert.ToDouble(str));
            bool isDouble = str.IndexOf('.') > -1 || str.IndexOf('e') > -1 || str.IndexOf('E') > -1;
            if (!isDouble)
            {
                return ConvertToIntegral(numVal);
            }
            return numVal;
        }
    }

    /// <summary>
    /// A class that is responsible for converting a double floating point
    /// number in octal base to its actual numeric value.
    /// </summary>
    public class OctalDoubleConverter : ICustomConverter
    {
        public Value Convert(string str)
        {
            string octalStr = !char.IsDigit(str[1]) ? str[2..] : str[1..];
            return NumberConverter.GetDoubleAtBase(octalStr, 8);
        }
    }
}
