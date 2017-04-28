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

namespace Asm6502.Net
{
    /// <summary>
    /// A class that encapsulates a label definition.
    /// </summary>
    public class Label
    {
        #region Constructors

        /// <summary>
        /// Constructs a new label object.
        /// </summary>
        public Label()
        {
            PreDefined = false;
            Size = 1;
            Value = 0;
            Expression = string.Empty;
        }

        #endregion

        #region Methods
        
        /// <summary>
        /// Returns a string representation of the label object.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (Convert.ToInt64(Value).Size() > 1)
                return string.Format("${0:x4}", Value);
            return string.Format("${0:x2}", Value);
        }
        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the underlying size in bytes of the label's value.
        /// </summary>
        public int Size { get; set; }

        /// <summary>
        /// Gets or sets the label's value.
        /// </summary>
        public ushort Value { get; set; }

        /// <summary>
        /// Gets or sets the string expression representing the label's value.
        /// Useful when being defined in a command line.
        /// </summary>
        public string Expression { get; set; }

        /// <summary>
        /// Gets or sets the flag indicating this label is predefined in the
        /// command-line.
        /// </summary>
        public bool PreDefined { get; set; }

        #endregion

    }

}
