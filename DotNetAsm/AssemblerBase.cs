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
using System.Collections.Generic;

namespace DotNetAsm
{
    /// <summary>
    ///The base assembler class. Must be inherited.
    /// </summary>
    public abstract class AssemblerBase
    {
        #region Members

        private IAssemblyController _controller;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs an instance of the class implementing the base class.
        /// </summary>
        /// <param name="controller">The assembly controller.</param>
        public AssemblerBase(IAssemblyController controller)
        {
            Controller = controller;

            if (controller == null)
            {
                Reserved = new ReservedWords();
            }
            else
            {
                Reserved = new ReservedWords(Controller.Options.StringComparison);
            }
        }

        protected abstract bool IsReserved(string token);

        /// <summary>
        /// Constructs an instance of the class implementing the base class.
        /// </summary>
        /// <param name="comparer">The string comparision.</param>
        public AssemblerBase() :
            this(null)
        {

        }

        #endregion

        #region Properties

        /// <summary>
        /// The reserved keywords of the object.
        /// </summary>
        protected ReservedWords Reserved { get; set; }

        /// <summary>
        /// An Assembly controller
        /// </summary>
        protected IAssemblyController Controller
        {
            get { return _controller; }
            set
            {
                _controller = value;
            }
        }

        #endregion
    }
}