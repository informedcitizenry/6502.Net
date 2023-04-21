//-----------------------------------------------------------------------------
// Copyright (c) 2017-2023 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using Antlr4.Runtime;

namespace Sixty502DotNet.Shared;

/// <summary>
/// An error that signals a break from a block.
/// </summary>
public sealed class Break : Error
{
    /// <summary>
    /// Construct a new instance of a <see cref="Break"/> exception.
    /// </summary>
    /// <param name="token">The token the break is associated with.</param>
    public Break(IToken token)
        : base(token, "Nothing to break from")
    {
        IsControlFlow = true;
    }
}

/// <summary>
/// An error that signals a block to continue its next iteration.
/// </summary>
public sealed class Continue : Error
{
    /// <summary>
    /// Construct a new instance of a <see cref="Continue"/> exception.
    /// </summary>
    /// <param name="token">The token the continue is associate with.</param>
    public Continue(IToken token)
        : base(token, "Nothing to continue from")
    {
        IsControlFlow = true;
    }
}

/// <summary>
/// An error that signals there is a <c>.goto</c> instruction.
/// </summary>
public sealed class Goto : Error
{
    /// <summary>
    /// Construct a new instance of a <see cref="Goto"/> exception. 
    /// </summary>
    /// <param name="destination">The destination expression. This should be an
    /// identifier to a named label.</param>
    public Goto(SyntaxParser.ExprListContext destination)
        : base(destination.Start, "Cannot go to here")
    {
        IsControlFlow = true;
        Destination = destination;
    }

    public SyntaxParser.ExprListContext Destination { get; init; }
}

/// <summary>
/// An error that signals a function is being returned, optionally with a
/// value.
/// </summary>
public sealed class Return : Error
{
    /// <summary>
    /// Construct a new instance of a <see cref="Return"/> exception.
    /// </summary>
    /// <param name="token">The token associated to the directive.</param>
    /// <param name="returnValue">The return value, if any.</param>
    public Return(IToken token, ValueBase? returnValue)
        : base(token, "Cannot return from here")
    {
        IsControlFlow = true;
        ReturnValue = returnValue;
    }

    /// <summary>
    /// Get the return value if any.
    /// </summary>
    public ValueBase? ReturnValue { get; }
}
