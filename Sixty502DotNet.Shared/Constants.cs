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

namespace Sixty502DotNet.Shared;

public static class Unicode
{
    public const int MaxCodepoint = 0x10ffff;

    public const int SurrogateMin = 0xd800;

    public const int SurrogateMax = 0xdfff;
    
    public const int HighSurrogate = 0xd800;

    public const int LowSurrogate = 0xdc00;
}

public static class UInt24
{
    public const int MinValue = 0;

    public const int MaxValue = 16777215;
}

public static class Int24
{
    public const int MinValue = -8388608;

    public const int MaxValue = 8388607;
}

public static class CbmFloat
{
    public const double MinValue = -2.93783588E+39;

    public const double MaxValue = 1.70141183E+38;
}

public static class Address
{
    public const int BadAddress = 16777216;

    public const int BankSize = 65536;

    public const int MaxAddress = 16777215;
}

public static class Epsilon
{
    public const double Value = 0.0000001;
}