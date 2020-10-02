//-----------------------------------------------------------------------------
// Copyright (c) 2017-2020 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Core6502DotNet.m680x
{
    /// <summary>
    /// A class responsible for formatting binary output for several M6800/M6809-
    /// based systems.
    /// </summary>
    public class MotorolaFormatProvider : IBinaryFormatProvider
    {
        public IEnumerable<byte> GetFormat(FormatInfo info)
        {
            if (!string.IsNullOrEmpty(info.FormatName) &&
                !info.FormatName.Equals("flat"))
                throw new Exception($"Unrecognized file format: \"{info.FormatName}\".");
            return info.ObjectBytes;
        }
    }
}
