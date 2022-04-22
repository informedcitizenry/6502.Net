//-----------------------------------------------------------------------------
// Copyright (c) 2017-2022 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;

namespace Sixty502DotNet
{
    /// <summary>
    /// An error for an invalid Program Counter assignment.
    /// </summary>
    public sealed class InvalidPCAssignmentException : Exception
    {
        readonly int _pc;

        /// <summary>
        /// Creates a new instance of an invalid PC assignment exception.
        /// </summary>
        /// <param name="value">The Program Counter value.</param>
        public InvalidPCAssignmentException(int value) => _pc = value;

        /// <summary>
        /// Creates a new instance of an invalid PC assignment exception.
        /// </summary>
        /// <param name="value">The Program Counter value.</param>
        /// <param name="sectionNotUsedError">The error was due to a section not being set.</param>
        public InvalidPCAssignmentException(int value, bool sectionNotUsedError)
            => (_pc, SectionNotUsedError) = (value, sectionNotUsedError);

        public override string Message
        {
            get
            {
                if (SectionNotUsedError)
                    return "A section was defined but not set.";
                return _pc.ToString();
            }
        }

        /// <summary>
        /// Gets a flag that determines the cause of the error was due to one or more sections
        /// being defined but never set.
        /// </summary>
        public bool SectionNotUsedError { get; }
    }

    /// <summary>
    /// An error for a Program Counter rollover.
    /// </summary>
    public sealed class ProgramOverflowException : Exception
    {
        /// <summary>
        /// Creates a new instance of a program overflow error.
        /// </summary>
        /// <param name="message">The custom overflow message.</param>
        public ProgramOverflowException(string message)
            : base(message)
        {
        }
    }

    /// <summary>
    /// An error in compilation.
    /// </summary>
    public sealed class CompilationException : Exception
    {
        /// <summary>
        /// Create a new instance of the compilation exception.
        /// </summary>
        /// <param name="message">The error message.</param>
        public CompilationException(string message)
            : base(message) { }
    }

}
