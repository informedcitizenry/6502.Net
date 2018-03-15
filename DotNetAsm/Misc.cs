//-----------------------------------------------------------------------------
// Copyright (c) 2017, 2018 informedcitizenry <informedcitizenry@gmail.com>
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to 
// deal in the Software without restriction, including without limitation the 
// rights to use, copy, modify, merge, publish, distribute, sublicense, and/or 
// sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in 
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS 
// IN THE SOFTWARE.
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
        public const int MinValue = (0 - 8388608);
        /// <summary>
        /// Represents the largest possible value of an Int24. This field is constant.
        /// </summary>
        public const int MaxValue = 8388607;
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
