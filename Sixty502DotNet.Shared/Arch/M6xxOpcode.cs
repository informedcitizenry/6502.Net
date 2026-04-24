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

namespace Sixty502DotNet.Shared.Arch;

// ReSharper disable once InconsistentNaming
public record struct M6xxOpcode
{
    private const int Bad = -1;
    public readonly int implied;
    public readonly int accumulator;
    public readonly int zeroPage;
    public readonly int relative;
    public readonly int absolute;
    public readonly int immediate;
    public readonly int zeroPageX;
    public readonly int zeroPageY;
    public readonly int absoluteX;
    public readonly int absoluteY;
    public readonly int indirectAbsolute;
    public readonly int indirectIndexed;
    public readonly int indexedZeroPageIndirect;
    public readonly int indirectZeroPage;
    public readonly int longAddress;
    public readonly int longX;
    public readonly int bitTest;
    public readonly int blockMove;
    public readonly int stackRelative;
    public readonly int indirectStackRelativeIndexed;
    public readonly int indexedAbsoluteIndirect;
    public readonly int bitTestBranch;
    public readonly int indirectLongIndexed;
    public readonly int indirectLong;
    public readonly int indirectLongAbsolute;
    public readonly int relativeAbsolute;
    public readonly int immediateAbsoluteIndexed;
    public readonly int threeOperand;
    public readonly int indirectZ;
    public readonly int indirectLongZ;
    public readonly int indirectStackPointerIndexed;
    public readonly int immediate16Bit;
    public readonly int immediateZeroPageIndexed;

    public M6xxOpcode(int implied,
        int zeroPage,
        int relative,
        int absolute,
        int immediate,
        int zeroPageX,
        int relativeAbs,
        int immediate16Bit)
        : this(implied,
            Bad,
            zeroPage,
            relative,
            absolute,
            immediate,
            zeroPageX,
            Bad,
            Bad,
            Bad,
            Bad,
            Bad,
            Bad,
            Bad,
            Bad,
            Bad,
            Bad,
            Bad,
            Bad,
            Bad,
            Bad,
            Bad,
            Bad,
            Bad,
            Bad,
            relativeAbs,
            Bad,
            Bad,
            Bad,
            Bad,
            Bad,
            immediate16Bit)
    {

    }

    public M6xxOpcode(int implied,
                    int accumulator,
                    int zeroPage,
                    int relative,
                    int absolute,
                    int immediate,
                    int zeroPageX,
                    int zeroPageY,
                    int absoluteX,
                    int absoluteY,
                    int indirect,
                    int indirectIndexed,
                    int indexedIndirect,
                    int indirectZeroPage,
                    int indirectIndexedAbs)
        : this(implied,
              accumulator,
              zeroPage,
              relative,
              absolute,
              immediate,
              zeroPageX,
              zeroPageY,
              absoluteX,
              absoluteY,
              indirect,
              indirectIndexed,
              indexedIndirect,
              indirectZeroPage,
              Bad,
              Bad,
              Bad,
              Bad,
              Bad,
              Bad,
              indirectIndexedAbs)
    {

    }

    public M6xxOpcode(int implied,
                    int accumulator,
                    int zeroPage,
                    int relative,
                    int absolute,
                    int immediate,
                    int zeroPageX,
                    int zeroPageY,
                    int absoluteX,
                    int absoluteY,
                    int indirect,
                    int indirectIndexed,
                    int indexedIndirect,
                    int indirectZeroPage,
                    int indirectIndexedAbs,
                    int bitTest,
                    int bitTestAbs)
        : this(implied,
              accumulator,
              zeroPage,
              relative,
              absolute,
              immediate,
              zeroPageX,
              zeroPageY,
              absoluteX,
              absoluteY,
              indirect,
              indirectIndexed,
              indexedIndirect,
              indirectZeroPage,
              Bad,
              Bad,
              bitTest,
              Bad,
              Bad,
              Bad,
              indirectIndexedAbs,
              bitTestAbs)
    {

    }

    
    public M6xxOpcode
    (
        int implied,
        int accumulator,
        int zeroPage,
        int relative,
        int absolute,
        int immediate,
        int zeroPageX,
        int zeroPageY,
        int absoluteX,
        int absoluteY,
        int indirectAbsolute,
        int indirectIndexed,
        int indexedZeroPageIndirect,
        int indirectZeroPage = Bad,
        int longAddress = Bad,
        int longX = Bad,
        int bitTest = Bad,
        int blockMove = Bad,
        int stackRelative = Bad,
        int indirectStackRelativeIndexed = Bad,
        int indexedAbsoluteIndirect = Bad,
        int bitTestAbs = Bad,
        int indirectLongIndexed = Bad,
        int indirectLong = Bad,
        int indirectLongAbsolute = Bad,
        int relativeAbs = Bad,
        int immediateAbsoluteIndexed = Bad,
        int threeOperand = Bad,
        int indirectZ = Bad,
        int indirectLongZ = Bad,
        int indirectStackPointerIndexed = Bad,
        int immediate16Bit = Bad,
        int immediateZpX = Bad)
    {
        this.implied = implied;
        this.accumulator = accumulator;
        this.zeroPage = zeroPage;
        this.relative = relative;
        this.absolute = absolute;
        this.immediate = immediate;
        this.zeroPageX = zeroPageX;
        this.zeroPageY = zeroPageY;
        this.absoluteX = absoluteX;
        this.absoluteY = absoluteY;
        this.indirectAbsolute = indirectAbsolute;
        this.indirectIndexed = indirectIndexed;
        this.indexedZeroPageIndirect = indexedZeroPageIndirect;
        this.indirectZeroPage = indirectZeroPage;
        this.longAddress = longAddress;
        this.longX = longX;
        this.bitTest = bitTest;
        this.blockMove = blockMove;
        this.stackRelative = stackRelative;
        this.indirectStackRelativeIndexed = indirectStackRelativeIndexed;
        this.indirectIndexed = indirectIndexed;
        this.indirectAbsolute = indirectAbsolute;
        this.indirectZ = indirectZ;
        this.indexedAbsoluteIndirect = indexedAbsoluteIndirect;
        this.bitTestBranch = bitTestAbs;
        this.indirectLongIndexed = indirectLongIndexed;
        this.indirectLong = indirectLong;
        this.indirectLongAbsolute = indirectLongAbsolute;
        this.relativeAbsolute = relativeAbs;
        this.immediateAbsoluteIndexed = immediateAbsoluteIndexed;
        this.threeOperand = threeOperand;
        this.indirectLongZ = indirectLongZ;
        this.indirectStackPointerIndexed = indirectStackPointerIndexed;
        this.immediate16Bit = immediate16Bit;
        this.immediateZeroPageIndexed = immediateZpX;
    }
}
