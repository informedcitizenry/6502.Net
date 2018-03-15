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
    ///The base assembler class. Must be inherited.
    /// </summary>
    public abstract class AssemblerBase
    {
        #region Members

        IAssemblyController _controller;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs an instance of the class implementing the base class.
        /// </summary>
        /// <param name="controller">The <see cref="T:DotNetAsm.IAssemblyController"/>.</param>
        protected AssemblerBase(IAssemblyController controller)
        {
            Controller = controller;

            if (controller == null)
                Reserved = new ReservedWords();
            else
                Reserved = new ReservedWords(Controller.Options.StringComparison);
        }

        /// <summary>
        /// Constructs an instance of the class implementing the base class.
        /// </summary>
        protected AssemblerBase() :
            this(null)
        {

        }

        #endregion

        #region Methods

        /// <summary>
        /// Determines whether the token is a reserved word to the assembler object.
        /// </summary>
        /// <param name="token">The token to check if reserved</param>
        /// <returns><c>True</c> if reserved, otherwise <c>false</c>.</returns>
        public virtual bool IsReserved(string token) { return false; }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the reserved keywords of the object.
        /// </summary>
        protected ReservedWords Reserved { get; set; }

        /// <summary>
        /// Gets or sets a <see cref="T:DotNetAsm.IAssemblyController"/>.
        /// </summary>
        protected IAssemblyController Controller
        {
            get { return _controller; }
            set {_controller = value; }
        }

        #endregion
    }
}