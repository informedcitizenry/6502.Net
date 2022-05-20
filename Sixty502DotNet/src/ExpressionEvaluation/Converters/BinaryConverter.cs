//-----------------------------------------------------------------------------
// Copyright (c) 2017-2022 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

namespace Sixty502DotNet
{
    /// <summary>
    /// A class that is responsible for converting a binary number string to its
    /// actual numeric value.
    /// </summary>
    public class BinaryConverter : ICustomConverter
    {
        public Value Convert(string str)
        {
            str = str.Replace("_", "");
            Value binVal;
            if (str[0] == '%')
            {
                if (str.IndexOf('.') > -1)
                {
                    binVal = new Value(System.Convert.ToInt64(str[1..].Replace(".", "0").Replace("#", "1"), 2));
                }
                else
                {
                    binVal = new Value(System.Convert.ToInt64(str[1..], 2));
                }
            }
            else
            {
                binVal = new Value(System.Convert.ToInt64(str[2..], 2));
            }
            if (binVal.ToDouble() >= int.MinValue && binVal.ToDouble() <= int.MaxValue)
            {
                return new Value(binVal.ToInt());
            }
            return binVal;
        }
    }

    /// <summary>
    /// A class that is responsible for converting a binary double string to its
    /// actual numeric value.
    /// </summary>
    public class BinaryDoubleConverter : ICustomConverter
    {
        public Value Convert(string str)
        {
            string bString = str[0] == '%' ? str[1..] : str[2..];
            return NumberConverter.GetDoubleAtBase(bString, 2);
        }
    }
}
