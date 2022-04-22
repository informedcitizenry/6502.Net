//-----------------------------------------------------------------------------
// Copyright (c) 2017-2022 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

namespace Sixty502DotNet
{
    /// <summary>
    /// A class that is responsible for converting a hexadecimal string to its
    /// actual numeric value.
    /// </summary>
    public class HexConverter : ICustomConverter
    {
        public Value Convert(string str)
        {
            str = str.Replace("_", "");
            Value hexVal = str[0] == '$' ?
                new Value(System.Convert.ToInt64(str[1..], 16)) :
                new Value(System.Convert.ToInt64(str[2..], 16));
            return Evaluator.ConvertToIntegral(hexVal);
        }
    }

    /// <summary>
    /// A class that is responsible for converting a hexadecimal double string
    /// to its actual numeric value.
    /// </summary>
    public class HexDoubleConverter : ICustomConverter
    {
        public Value Convert(string str)
        {
            var hString = str[0] == '$' ? str[1..] : str[2..];
            return new Value(NumberConverter.GetDoubleAtBase(hString, 16));
        }
    }
}
