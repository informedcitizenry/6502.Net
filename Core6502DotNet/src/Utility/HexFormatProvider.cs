//-----------------------------------------------------------------------------
// Copyright (c) 2017-2020 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Core6502DotNet
{
    public class HexFormatProvider : IBinaryFormatProvider
    {
        public IEnumerable<byte> GetFormat(FormatInfo info)
        {
            var hex = BitConverter.ToString(info.ObjectBytes.ToArray()).Replace("-", string.Empty);
            return Encoding.ASCII.GetBytes(hex);
        }
    }
}
