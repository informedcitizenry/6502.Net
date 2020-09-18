//-----------------------------------------------------------------------------
// Copyright (c) 2017-2020 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Core6502DotNet.m680x
{
    /// <summary>
    /// A class responsible for formatting binary output for several M6800/M6809-
    /// based systems.
    /// </summary>
    public class MotorolaFormatProvider : Core6502Base, IBinaryFormatProvider
    {
        /// <summary>
        /// Creates a new instance of the Motorola binary format provider class.
        /// </summary>
        /// <param name="services">The shared <see cref="AssemblyServices"/> object.</param>
        public MotorolaFormatProvider(AssemblyServices services)
            : base(services)
        {
        }

        public IEnumerable<byte> GetFormat()
        {
            if (!string.IsNullOrEmpty(Services.OutputFormat) &&
                !Services.OutputFormat.Equals("flat"))
                throw new Exception($"Unrecognized file format: \"{Services.OutputFormat}\".");
            return Services.Output.GetCompilation();
        }
    }
}
