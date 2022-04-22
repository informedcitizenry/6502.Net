//-----------------------------------------------------------------------------
// Copyright (c) 2017-2022 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

namespace Sixty502DotNet
{
    /// <summary>
    /// An interface for an object that converts a string representation of a
    /// value into the value.
    /// </summary>
    public interface ICustomConverter
    {
        /// <summary>
        /// Parse a string as its actual value.
        /// </summary>
        /// <param name="str">The string to convert.</param>
        /// <returns>The converted object as an <see cref="IValue"/>.</returns>
        Value Convert(string str);
    }
}
