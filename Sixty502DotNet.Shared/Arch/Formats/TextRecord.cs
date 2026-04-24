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

using System.Globalization;

namespace Sixty502DotNet.Shared.Arch.Formats;

/*
 SRecMos
  H  size  addr   data       chk
+---+----+------+----------+----+
| ; | 00 | 0000 | ...      | 00 |
+---+----+------+----------+----+
0   1    3      7
 
 SRecord
  H  typ  siz  addr   data       chk
+---+---+----+------+----------+----+
| S | 0 | 00 | 0000 | ...      | 00 |
+---+---+----+------+----------+----+
0   1   2    4      8
 
 Hex86
  H  size  addr   typ  data       chk
+---+----+------+----+----------+----+
| : | 00 | 0000 | 00 | ...      | 00 |
+---+----+------+----+----------+----+
0   1    3      7    9     
 
 */


public class TextRecord
{
    private const int HeaderSize = 1;

    private const int SizeSize = 2;

    private const int AddressSize = 4;

    private const int ChecksumSize = 2;

    private const int NonDataSize = HeaderSize + SizeSize + AddressSize + ChecksumSize;
    
    public static bool TryParse(string line, char headerChar, out TextRecord? record)
    {
        var (sizeIndex, addrIndex, typeIndex, typeSize, dataIndex) = headerChar switch
        {
            ':' => (1, 3, 7, 2, 9),
            ';' => (1, 3, 0, 0, 7),
            _   => (2, 4, 1, 1, 8)
        };

        var addrSlice = line.AsSpan(addrIndex, AddressSize);
        var sizeSlice = line.AsSpan(sizeIndex, SizeSize);
        
        record = null;
        var recordType = 0;
        if (line.Length < 10 || line[0] != headerChar) return false;
        if  (!int.TryParse
             (
                 sizeSlice, 
                 NumberStyles.HexNumber, 
                 CultureInfo.InvariantCulture, 
                 out var size) || 
             !int.TryParse
             (
                 addrSlice, 
                 NumberStyles.HexNumber, 
                 CultureInfo.InvariantCulture, 
                 out var address
             ) ||
             !int.TryParse
             (
                 line[^2..], 
                 NumberStyles.HexNumber, 
                 CultureInfo.InvariantCulture, 
                 out var checksum
             ) || 
             (typeSize > 0 && !int.TryParse
                 (
                     line.AsSpan(typeIndex, typeSize), 
                     NumberStyles.HexNumber, 
                     CultureInfo.InvariantCulture, 
                     out recordType
                 )
             )) 
        {
            return false;
        }
        if (size > 0)
        {
            var data = line.Substring(dataIndex, line.Length - (typeSize + NonDataSize));
            record = new TextRecord(recordType, size, address, data, checksum);
        }
        else
        {
            record = new TextRecord(recordType, size, address, null, checksum);
        }
        return true;
    }

    private TextRecord(int type, int dataSize, int address, string? data, int checksum)
    {
        Type = type;
        DataSize = dataSize;
        Address = address;
        Data = data;
        Checksum = checksum;
    }

    public int DataSize { get; }
    
    public int Type { get; }
    
    public int Checksum { get; }
    
    public int Address { get; }
    
    public string? Data { get; }
    
}