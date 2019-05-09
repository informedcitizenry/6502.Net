//-----------------------------------------------------------------------------
// Copyright (c) 2017-2019 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

namespace DotNetAsm
{
    /// <summary>
    /// Represents a signed 24-bit integer.
    /// </summary>
    public struct Int24
    {
        /// <summary>
        /// Represents the smallest possible value of an Int24. This field is constant.
        /// </summary>
        public const int MinValue = (0 - 8_388_608);
        /// <summary>
        /// Represents the largest possible value of an Int24. This field is constant.
        /// </summary>
        public const int MaxValue = 8_388_607;
    }

    /// <summary>
    /// Represents an unsigned 24-bit integer.
    /// </summary>
    public struct UInt24
    {
        /// <summary>
        /// Represents the smallest possible value of a UInt24. This field is constant.
        /// </summary>
        public const int MinValue = 0;
        /// <summary>
        /// Represents the largest possible value of a UInt24. This field is constant.
        /// </summary>
        public const int MaxValue = 0xFFFFFF;
    }
}
