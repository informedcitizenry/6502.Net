//-----------------------------------------------------------------------------
// Copyright (c) 2017-2022 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System.Runtime.InteropServices;

namespace Sixty502DotNet
{
    /// <summary>
    /// A union of a <see cref="double"/> and a <see cref="long"/> holding the
    /// double's binary representation in memory.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 8)]
    public struct Ieee754Converter
    {
        /// <summary>
        /// Get the binary field.
        /// </summary>
        [FieldOffset(0)]
        public readonly ulong Binary;

        /// <summary>
        /// Get the float field.
        /// </summary>
        [FieldOffset(0)]
        public readonly double Double;

        /// <summary>
        /// Construct a new <see cref="Ieee754Converter"/> struct from the
        /// double floating point value.
        /// </summary>
        /// <param name="floatVal">The floating point value.</param>
        public Ieee754Converter(double floatVal)
        {
            Binary = 0;
            Double = floatVal;
        }

        /// <summary>
        /// Construct a new <see cref="Ieee754Converter"/> struct from the
        /// binary value.
        /// </summary>
        /// <param name="ulongVal">The binary value.</param>
        public Ieee754Converter(ulong ulongVal)
        {
            Double = 0;
            Binary = ulongVal;
        }

        // Comparisons were made between the output of the ".cbmflt"/".cbmfltp" 
        // doubles to CBM/MBF binary and what the Commodore 64 BASIC ROM would
        // produce in VICE. In some cases, the final bit--or couple of bits--
        // would not exactly match.
        //
        // Since the method the assembler uses leverages the raw 
        // IEEE-754 binary, the thinking was to improve results instead to
        // "hand-convert" the double from a string. 
        //
        // Testing this method, the results did mostly align to the
        // Commodore output but not always. More surpising, though, was the 
        // mismatching cases still matched the IEEE-754-based method, suggesting 
        // there is something fundamental to the way the C64 BASIC converts
        // float strings to binary that requires a more complete understanding
        // of the ROM. Given the values returned are close enough, the level of
        // effort necessary to improve the existing algorithm is not worth it
        // (for now).
        //
        // TL;DR version: Pointless to hand-convert a string to MBF, since a
        // more accurate technique is non-trivial. Instead keep starting from
        // IEEE-754.
        private static long Convert(string str)
        {
            var parts = str.Split('.');
            long fractBinary = System.Convert.ToInt64(parts[0]), integral = fractBinary;
            var integerBits = 0;
            while (integral > 0)
            {
                integral >>= 1;
                integerBits++;
            }
            var exponent = 128 + integerBits;
            if (exponent > 255)
            {
                return 0xFF_FFFF_FFFF;
            }
            if (parts.Length > 1)
            {
                var fractStr = parts[1].Substring(0, parts[1].Length > 15 ? 15 : parts[1].Length);
                var fract = System.Convert.ToInt64(fractStr);
                if (fractBinary == 0 && fract == 0)
                {
                    return 0;
                }
                var fractBitSet = integerBits > 0;
                var remainingBits = 32 - integerBits;
                var threshold = (long)System.Math.Pow(10, fractStr.Length);
                while (remainingBits > 0)
                {
                    fract *= 2;
                    long binDig = fract / threshold;
                    fractBitSet |= binDig == 1;
                    if (!fractBitSet)
                    {
                        --exponent;
                    }
                    else
                    {
                        fractBinary = (fractBinary << 1) | binDig;
                        if (binDig == 1)
                        {
                            fract -= threshold;
                        }
                        remainingBits--;
                    }
                }
            }
            else if (fractBinary == 0)
            {
                return 0;
            }
            else
            {
                while (fractBinary <= 0x7fff_ffff)
                {
                    fractBinary <<= 1;
                }
            }
            if (str[0] != '-')
                fractBinary &= 0x7fff_ffff;
            return exponent * 0x10000_0000 | fractBinary;
        }
    }
}
