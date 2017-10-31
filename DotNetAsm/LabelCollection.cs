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
    /// <summary>
    /// A <see cref="T:DotNetAsm.SymbolCollectionBase"/> implemented as a label collection.
    /// </summary>
    public sealed class LabelCollection : SymbolCollectionBase
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="T:DotNetAsm.LabelCollection"/> class.
        /// </summary>
        /// <param name="comparer">Comparer.</param>
        public LabelCollection(StringComparer comparer)
            : base(comparer)
        {

        }

        #endregion

        #region Methods

        /// <summary>
        /// Sets the label.
        /// </summary>
        /// <param name="labelName">Label name.</param>
        /// <param name="value">Value.</param>
        /// <param name="isStrict">If set to <c>true</c> the label name is strictly enforced.</param>
        /// <param name="isDeclaration">If set to <c>true</c> is a declaration.</param>
        public void SetLabel(string labelName, long value, bool isStrict, bool isDeclaration)
        {
            if (isDeclaration && IsSymbol(labelName))
                throw new SymbolCollectionException(labelName, SymbolCollectionException.ExceptionReason.SymbolExists);
            SetSymbol(labelName, value, isStrict);
        }

        #endregion
    }
}
