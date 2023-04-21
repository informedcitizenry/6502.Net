//-----------------------------------------------------------------------------
// Copyright (c) 2017-2023 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using Antlr4.Runtime;

namespace Sixty502DotNet.Shared;

//// <summary>
/// Provides a custom implementation of the the <see cref="ICharStreamFactory"/>
/// interface. When requests are made to the <see cref="CustomCharStreamFactory"/>
/// to provide a <see cref="ICharStream"/>, the <see cref="Handler"/> provided
/// will be called, as it is assumed the custom implementation will know how
/// to interpret the source resource.
/// </summary>
public class CustomCharStreamFactory : ICharStreamFactory
{
    /// <summary>
    /// Construct a new instance of the <see cref="CustomCharStreamFactory"/>
    /// with a given handler method that is invoked when a request is made for
    /// a source (e.g. an <c>.include</c> directive).
    /// </summary>
    /// <param name="hander">The handler method.</param>
    public CustomCharStreamFactory(Func<string, ICharStream>? hander)
    {
        Handler = hander;
    }

    public virtual ICharStream GetStream(string source)
    {
        if (Handler == null)
        {
            throw new IOException("Handler not set to handle source");
        }
        return Handler(source);
    }

    /// <summary>
    /// Get or set the function that will handle the source and provide
    /// the <see cref="ICharStream"/>.
    /// </summary>
    public Func<string, ICharStream>? Handler { get; set; }
}

