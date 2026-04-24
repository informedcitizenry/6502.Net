// Copyright (c) 2026 informedcitizenry
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System.Collections.Frozen;

namespace Sixty502DotNet.Shared.Arch.Formats;

public sealed class D64FormatProvider : IOutputFormatProvider
{
    public IReadOnlyCollection<byte> GetFormat
    (
        string fileName, 
        int startAddress, 
        ReadOnlySpan<byte> objectBytes
    )
    {
        var diskImage = new byte[DiskSize];
        var availTrackSectors = s_trackSectorTable.ToDictionary(k => k.Key, k => ToBitMap(k.Value));

        fileName = fileName.ToUpper();
        if (fileName.Length > 16)
            fileName = fileName[..16];
        else if (fileName.Length > 4 && fileName.EndsWith(".D64"))
            fileName = fileName[0..^4];

        var fileBytes = new List<byte>(objectBytes.Length + 2)
        {
            // write load address
            Convert.ToByte(startAddress % 256),
            Convert.ToByte(startAddress / 256)
        };

        // write file data
        fileBytes.AddRange(objectBytes);

        // write directory header
        diskImage[DirectoryOffset] = DirectorySector;
        diskImage[DirectoryOffset + 1] = 1;
        diskImage[DirectoryOffset + 2] = Convert.ToByte('A');
        diskImage[DirectoryOffset + 3] = 0;

        // hold off on BAM for now, but save offset value
        var bamOffs = DirectoryOffset + 4;
        var offset = bamOffs + BamSize + 1;

        // write file name
        for (var i = 0; i < fileName.Length; i++)
            diskImage[offset++] = Convert.ToByte(fileName[i]);

        // write shifted spaces up to 18 places after BAM
        for (var i = fileName.Length; i < 18; i++)
            diskImage[offset++] = 0xA0;

        // disk ID
        diskImage[offset++] = 0x41;
        diskImage[offset++] = 0x42;
        diskImage[offset++] = 0xA0;

        // DOS version
        diskImage[offset++] = Convert.ToByte('2');
        diskImage[offset++] = Convert.ToByte('A');

        // more shifted spaces
        for (var i = 0; i < 4; i++)
            diskImage[offset++] = 0xA0;

        // write next track/sector (which is none)
        offset = 0x16601;
        diskImage[offset++] = 0xFF; // signal end of directory

        // write file type(program)
        diskImage[offset++] = 0x82;

        // first track/sector of program file proper (17,0)
        diskImage[offset++] = DirectorySector - 1;
        diskImage[offset++] = 0;

        // write filename again
        for (var i = 0; i < fileName.Length; i++)
            diskImage[offset++] = Convert.ToByte(fileName[i]);

        // write shifted spaces up to 16 places 
        for (var i = fileName.Length; i < 16; i++)
            diskImage[offset++] = 0xA0;

        // write block size =
        var blockSize = fileBytes.Count / 256;
        if ((fileBytes.Count % 256) != 0)
            blockSize++;

        diskImage[offset + 9] = Convert.ToByte(blockSize % 256);
        diskImage[offset + 10] = Convert.ToByte(blockSize / 256);

        // adjust available track sectors on track 18
        availTrackSectors[18] = ResetBit(availTrackSectors[18], 0) &
                                 ResetBit(availTrackSectors[18], 1);


        var currentTrack = 17;
        var currentSector = 0;
        var currentStrideDir = 1;
        var stride = 10;
        var finx = 0;
        for (; finx < fileBytes.Count - 253; finx += 254)
        {
            availTrackSectors[currentTrack] = ResetBit(availTrackSectors[currentTrack], currentSector);
            offset = s_trackOffsets[currentTrack] + 256 * currentSector;
            currentSector += 10;
            if (currentSector < 0 || currentSector >= s_trackSectorTable[currentTrack])
            {
                stride += -2 * currentStrideDir;
                if (stride == 0)
                {
                    currentStrideDir = -1;
                    stride = 1;
                    currentSector = stride;
                }
                else if (stride > 10)
                {
                    currentSector = 0;
                    stride = 10;
                    currentStrideDir = 1;
                    --currentTrack;

                    if (currentTrack < 0)
                        currentTrack = 35;
                    else if (currentTrack == 18)
                        throw new FormatException("fatal error: Unable to write to D64. Capacity exceeded.");
                }
                else
                {
                    currentSector = stride;
                }
            }
            diskImage[offset++] = Convert.ToByte(currentTrack);
            diskImage[offset++] = Convert.ToByte(currentSector);
            for (var j = 0; j < 254; j++)
                diskImage[offset++] = fileBytes[finx + j];
        }
        if (finx < fileBytes.Count)
        {
            offset = s_trackOffsets[currentTrack] + 256 * currentSector;
            availTrackSectors[currentTrack] = ResetBit(availTrackSectors[currentTrack], currentSector);
            diskImage[offset++] = 0;
            diskImage[offset++] = Convert.ToByte((fileBytes.Count % 254) + 1);
            for (; finx < fileBytes.Count; finx++)
                diskImage[offset++] = fileBytes[finx];
        }
        for (var i = 1; i <= availTrackSectors.Count; i++)
        {
            var bitmap = availTrackSectors[i];
            var blocksFree = GetFreeBlockCount(bitmap);
            diskImage[bamOffs++] = Convert.ToByte(blocksFree);
            diskImage[bamOffs++] = Convert.ToByte(bitmap & 0b1111_1111);
            diskImage[bamOffs++] = Convert.ToByte((bitmap >> 8) & 0b1111_1111);
            diskImage[bamOffs++] = Convert.ToByte((bitmap >> 16) & 0b1111_1111);
        }
        return diskImage;
    }

    public FormatDescriptor Describe(ReadOnlySpan<byte> codeBytes) => new();

    private static readonly FrozenDictionary<int, int> s_trackSectorTable 
        = new Dictionary<int, int>
    {
        { 1, 21 },
        { 2, 21 },
        { 3, 21 },
        { 4, 21 },
        { 5, 21 },
        { 6, 21 },
        { 7, 21 },
        { 8, 21 },
        { 9, 21 },
        { 10, 21 },
        { 11, 21 },
        { 12, 21 },
        { 13, 21 },
        { 14, 21 },
        { 15, 21 },
        { 16, 21 },
        { 17, 21 },
        { 18, 19 },
        { 19, 19 },
        { 20, 19 },
        { 21, 19 },
        { 22, 19 },
        { 23, 19 },
        { 24, 19 },
        { 25, 18 },
        { 26, 18 },
        { 27, 18 },
        { 28, 18 },
        { 29, 18 },
        { 30, 18 },
        { 31, 17 },
        { 32, 17 },
        { 33, 17 },
        { 34, 17 },
        { 35, 17 }
    }.ToFrozenDictionary();

    private static readonly int[] s_trackOffsets =
    [
        0x00000, // extra dummy track
        0x00000,
        0x01500,
        0x02A00,
        0x03F00,
        0x05400,
        0x06900,
        0x07E00,
        0x09300,
        0x0A800,
        0x0BD00,
        0x0D200,
        0x0E700,
        0x0FC00,
        0x11100,
        0x12600,
        0x13B00,
        0x15000,
        0x16500,
        0x17800,
        0x18B00,
        0x19E00,
        0x1B100,
        0x1C400,
        0x1D700,
        0x1EA00,
        0x1FC00,
        0x20E00,
        0x22000,
        0x23200,
        0x24400,
        0x25600,
        0x26700,
        0x27800,
        0x28900,
        0x29A00,
        0x2AB00,
        0x2BC00,
        0x2CD00,
        0x2DE00,
        0x2EF00,
        0x30000,
        0x31100
    ];
    
    private const int DirectorySector = 18;
    
    private const int DirectoryOffset = 0x16500;

    private const int BamSize = 139;
    
    private const int DiskSize = 683 * 256;

    private static int ToBitMap(int sector)
        => (int)Math.Pow(2, sector) - 1;

    private static int ResetBit(int bitField, int flag)
        => bitField & (~(int)Math.Pow(2, flag));

    private static int GetFreeBlockCount(int i)
    {
        i -= (i >> 1) & 0x55555555;
        i = (i & 0x33333333) + ((i >> 2) & 0x33333333);
        return (((i + (i >> 4)) & 0x0F0F0F0F) * 0x01010101) >> 24;
    }
}