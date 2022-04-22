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
        public static double GetDoubleAtBase(string str, int atBase)
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
                var expBase = mantissaExponent.Groups[2].Value.ToLower()[0] == 'e' ? 10 : 2;
                var pow = double.Parse(mantissaExponent.Groups[3].Value);
                return mantissa * Math.Pow(expBase, pow);
            }
            return mantissa;
        }

        public Value Convert(string str)
        {
            str = str.Replace("_", "");
            Value numVal;
            bool isDouble = str.IndexOf('.') > -1 || str.IndexOf('e') > -1 || str.IndexOf('E') > -1;
            if (str[0] == '0' && str.Length > 1)
            {
                if (!char.IsDigit(str[1]))
                {
                    numVal = new Value(System.Convert.ToInt64(str[2..], 8));
                }
                else
                {
                    numVal = new Value(System.Convert.ToInt64(str, 8));
                }
            }
            else
            {
                numVal = new Value(System.Convert.ToDouble(str));
            }
            if (!isDouble)
            {
                return Evaluator.ConvertToIntegral(numVal);
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
            string oString = !char.IsDigit(str[1]) ? str[2..] : str[1..];
            return new Value(NumberConverter.GetDoubleAtBase(oString, 8));
        }
    }
}
