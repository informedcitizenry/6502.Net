//-----------------------------------------------------------------------------
// Copyright (c) 2017-2022 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

namespace Sixty502DotNet
{
    public class CbmFloatFunction : FunctionDefinitionBase
    {
        const int AdjustedBias = 894;

        private readonly CodeOutput _codeOutput;
        private readonly bool _packed;

        public CbmFloatFunction(CodeOutput output, bool packed)
            : base("cbmfloat", new List<FunctionArg> { new FunctionArg("", TypeCode.Int32) })
        {
            _codeOutput = output;
            _packed = packed;
        }

        protected override Value OnInvoke(ArrayValue args)
        {
            var addr = args[0].ToInt();
            if (addr < short.MinValue || addr > ushort.MaxValue)
            {
                throw new Error($"Address is out of range.");
            }
            var bytes = _codeOutput.GetRange(addr, _packed ? 5 : 6);
            var conv = ToDouble(bytes.ToList(), _packed);
            if (double.IsNaN(conv))
            {
                throw new Error($"Content at address ${addr:x4} is not in the proper format.");
            }
            return new Value(conv);
        }

        public static double ToDouble(IList<byte> cmBytes, bool packed)
        {
            int sign;
            sign = cmBytes[1] & 0x80;
            if (!packed)
            {
                if (cmBytes[1] != 0 && cmBytes[1] != 1)
                    return double.NaN;
                sign = cmBytes[1] * 0x80;
                cmBytes[1] = (byte)((cmBytes[1] << 7) | cmBytes[2]);
                for (var i = 2; i < 5; i++)
                    cmBytes[i] = cmBytes[i + 1];
            }
            ulong ieee = ((((ulong)cmBytes[0] + AdjustedBias) | ((ulong)sign << 4)) << 52) |
                          (((ulong)cmBytes[1] & 0x7f) << 45) |
                           ((ulong)cmBytes[2] << 37) |
                           ((ulong)cmBytes[3] << 29) |
                           ((ulong)cmBytes[4] << 21);

            return new Ieee754Converter(ieee).Double;
        }

        public static IEnumerable<byte> ToBytes(double value, bool packed)
        {
            var bytes = packed ? new byte[5] : new byte[6];
            // Convert float to binary.
            var ieeeConv = new Ieee754Converter(value);
            var ieee = ieeeConv.Binary;

            // Calculate exponent
            var exp = ((ieee >> 52) & 0x7ff) - AdjustedBias;
            bytes[0] = Convert.ToByte(exp);

            // Calculate mantissa
            var mantissa = ieee >> 21;

            var manix = packed ? 1 : 2;
            bytes[manix] = (byte)(mantissa >> 24);
            bytes[manix + 1] = (byte)(mantissa >> 16);
            bytes[manix + 2] = (byte)(mantissa >> 8);
            bytes[manix + 3] = (byte)mantissa;

            if (bytes[manix] >= 0x80 && packed)
                bytes[manix] &= 0x7f;

            // Calculate sign
            if (unchecked((long)ieee) < 0)
            {
                if (packed)
                    bytes[1] |= 0x80;
                else
                    bytes[1] = 1;
            }
            return bytes;
        }
    }
}
