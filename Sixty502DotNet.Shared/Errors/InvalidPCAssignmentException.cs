//-----------------------------------------------------------------------------
// Copyright (c) 2017-2024 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

namespace Sixty502DotNet.Shared;

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
            return $"Cannot set program counter: ${_pc:x} invalid";
        }
    }

    /// <summary>
    /// Gets a flag that determines the cause of the error was due to one or more sections
    /// being defined but never set.
    /// </summary>
    public bool SectionNotUsedError { get; }
}

