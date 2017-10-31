//-----------------------------------------------------------------------------
// Copyright (c) 2017 informedcitizenry <informedcitizenry@gmail.com>
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

using System;

namespace DotNetAsm
{
    static class ConstStrings
    {
        /// <summary>
        /// The default token indicating a scope block opening. This field is constant.
        /// </summary>
        public const string OPEN_SCOPE = ".block";

        /// <summary>
        /// The default token indicating a scope block closure. This field is constant.
        /// </summary>
        public const string CLOSE_SCOPE = ".endblock";

        /// <summary>
        /// A constant string expression to use for a DotNetAsm.SourceLine SourceString 
        /// to indicate the line itself is "shadow source," i.e. injected programmatically
        /// and not present in the original source file.
        /// </summary>
        public const string SHADOW_SOURCE = "@@__SHADOW__@@";

        /// <summary>
        /// The directive used to declare and assign variables.
        /// </summary>
        public const string VAR_DIRECTIVE = ".let";
    }
}
