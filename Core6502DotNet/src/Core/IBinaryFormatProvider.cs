//-----------------------------------------------------------------------------
// Copyright (c) 2017-2021 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System.Collections.Generic;

namespace Core6502DotNet
{
    /// <summary>
    /// Information related to a format to assist the <see cref="IBinaryFormatProvider"/>
    /// in formatting assembly.
    /// </summary>
    public sealed class FormatInfo
    {

        /// <summary>
        /// Creates a new instance of a <see cref="FormatInfo"/> class.
        /// </summary>
        /// <param name="fileName">The output file name.</param>
        /// <param name="formatName">The format name.</param>
        /// <param name="startAddress">The output start address.</param>
        /// <param name="objectBytes">The output object bytes.</param>
        public FormatInfo(string fileName, string formatName, int startAddress, IEnumerable<byte> objectBytes)
        {
            FileName = fileName;
            FormatName = formatName;
            StartAddress = startAddress;
            ObjectBytes = objectBytes;
        }

        /// <summary>
        /// The output's file name.
        /// </summary>
        public string FileName { get; }

        /// <summary>
        /// The output format name.
        /// </summary>
        public string FormatName { get; }

        /// <summary>
        /// The output start address.
        /// </summary>
        public int StartAddress { get; }

        /// <summary>
        /// The output object bytes.
        /// </summary>
        public IEnumerable<byte> ObjectBytes { get; }
    }

    public interface IBinaryFormatProvider
    {
        /// <summary>
        /// Converts the assembly output to a custom binary format.
        /// </summary>
        /// <param name="info">The format info.</param>
        /// <returns>A custom-formatted byte collection.</returns>
        IEnumerable<byte> GetFormat(FormatInfo info);
    }
}