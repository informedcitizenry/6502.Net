//-----------------------------------------------------------------------------
// Copyright (c) 2017-2022 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Sixty502DotNet
{
    /// <summary>
    /// A class that implements an output format provider that refactors 
    /// generated code as a long hexadecimal string.
    /// </summary>
    public class HexFormatProvider : IOutputFormatProvider
    {
        public ReadOnlyCollection<byte> GetFormat(OutputFormatInfo info)
        {
            var hex = BitConverter.ToString(info.ObjectBytes.ToArray()).Replace("-", string.Empty);
            return new ReadOnlyCollection<byte>(Encoding.ASCII.GetBytes(hex));
        }

        public string FormatName => "hex";
    }
}
