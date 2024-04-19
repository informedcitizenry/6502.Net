//-----------------------------------------------------------------------------
// Copyright (c) 2017-2024 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System.Collections.ObjectModel;

namespace Sixty502DotNet.Shared;

/// <summary>
/// A class that implements the default format provider for m65xx CPUs.
/// </summary>
public sealed class M65xxFormatProvider : IOutputFormatProvider
{
    public M65xxFormatProvider(string formatName)
        => FormatName = formatName;

    public ReadOnlyCollection<byte> GetFormat(OutputFormatInfo info)
    {
        var size = info.ObjectBytes.Count();
        var end = info.StartAddress + size;
        byte startL = (byte)(info.StartAddress & 0xFF);
        var startH = (byte)(info.StartAddress / 256);
        byte endL = (byte)(end & 0xFF);
        byte endH = (byte)(end / 256);
        byte sizeL = (byte)(size & 0xFF);
        byte sizeH = (byte)(size / 256);

        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        if (string.IsNullOrEmpty(FormatName) || FormatName.Equals("cbm"))
        {
            writer.Write(startL);
            writer.Write(startH);
        }
        else if (FormatName.Equals("atari-xex"))
        {
            writer.Write(new byte[] { 0xff, 0xff }); // FF FF
            writer.Write(startL); writer.Write(startH);
            writer.Write(endL); writer.Write(endH);
        }
        else if (FormatName.Equals("apple2"))
        {
            writer.Write(startL); writer.Write(startH);
            writer.Write(sizeL); writer.Write(sizeH);
        }
        else
        {
            throw new Error($"Format '{FormatName}' not supported with targeted CPU.");
        }
        writer.Write(info.ObjectBytes.ToArray());
        return new ReadOnlyCollection<byte>(ms.ToArray());
    }

    public string FormatName { get; }
}

