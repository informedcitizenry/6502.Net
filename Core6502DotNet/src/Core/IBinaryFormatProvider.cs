//-----------------------------------------------------------------------------
// Copyright (c) 2017-2020 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System.Collections.Generic;

namespace Core6502DotNet
{
    public interface IBinaryFormatProvider
    {
        /// <summary>
        /// Converts the assembly output to a custom binary format.
        /// </summary>
        /// <param name="objectBytes">The object bytes to format.</param>
        /// <returns>A custom-formatted byte collection.</returns>
        IEnumerable<byte> GetFormat(IEnumerable<byte> objectBytes);
    }
}