//-----------------------------------------------------------------------------
// Copyright (c) 2017-2021 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

namespace Sixty502DotNet
{
    /// <summary>
    /// Represents a signed 24-bit integer.
    /// </summary>
    public struct Int24
    {
        /// <summary>
        /// Represents the smallest possible value of an <see cref="Int24"/>. This field is constant.
        /// </summary>
        public const int MinValue = -8388608;
        /// <summary>
        /// Represents the largest possible value of an <see cref="Int24"/>. This field is constant.
        /// </summary>
        public const int MaxValue = 8388607;
    }

    /// <summary>
    /// Represents an unsigned 24-bit integer.
    /// </summary>
    public struct UInt24
    {
        /// <summary>
        /// Represents the smallest possible value of a <see cref="UInt24"/>. This field is constant.
        /// </summary>
        public const int MinValue = 0;
        /// <summary>
        /// Represents the largest possible value of a <see cref="UInt24"/>. This field is constant.
        /// </summary>
        public const int MaxValue = 16777215;
    }
}