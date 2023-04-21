//-----------------------------------------------------------------------------
// Copyright (c) 2017-2023 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

namespace Sixty502DotNet.Shared;

/// <summary>
/// Represents an implementation of a function that returns a <see cref="double"/>
/// from a CBM-formatted binary floating point format.
/// </summary>
public sealed class CbmFloatFunction : BuiltInFunctionObject
{
    const int AdjustedBias = 894;

    private readonly CodeOutput _codeOutput;
    private readonly bool _packed;

    /// <summary>
    /// Construct a new instance of a <see cref="CbmFloatFunction"/>.
    /// </summary>
    /// <param name="codeOutput">The <see cref="CodeOutput"/> to read the
    /// byte output.</param>
    /// <param name="packed">The flag whether the function should expect the
    /// byte array to be a "packed" (5-byte) format.</param>
	public CbmFloatFunction(CodeOutput codeOutput, bool packed)
        : base(packed ? "cbmfltp" : "cbmflt", 1)
    {
        _codeOutput = codeOutput;
        _packed = packed;
    }

    protected override ValueBase OnInvoke(SyntaxParser.ExpressionCallContext callSite, ArrayValue? parameters)
    {
        int addr = (int)parameters![0].AsDouble();
        if (addr < short.MinValue || addr > ushort.MaxValue)
        {
            throw new Error(callSite.exprList().expr()[0], "Illegal quantity");
        }
        var bytes = _codeOutput.GetRange(addr & 0xffff, _packed ? 5 : 6);
        double conv = ToDouble(bytes.ToList(), _packed);
        if (double.IsNaN(conv))
        {
            throw new Error(callSite.exprList().expr()[0], $"Content at address ${addr:x4} is not in the proper format");
        }
        return new NumericValue(conv, true);
    }

    /// <summary>
    /// Convert a CBM-formatted binary string to a <see cref="double"/>.
    /// </summary>
    /// <param name="cbmBytes">The bytes to read and decode.</param>
    /// <param name="packed">The flag indicating whether the bytes should
    /// be packed.</param>
    /// <returns>A double representation of the binary string.</returns>
    public static double ToDouble(IList<byte> cbmBytes, bool packed)
    {
        int sign;
        sign = cbmBytes[1] & 0x80;
        if (!packed)
        {
            if (cbmBytes[1] != 0 && cbmBytes[1] != 1)
                return double.NaN;
            sign = cbmBytes[1] * 0x80;
            cbmBytes[1] = (byte)((cbmBytes[1] << 7) | cbmBytes[2]);
            for (var i = 2; i < 5; i++)
                cbmBytes[i] = cbmBytes[i + 1];
        }
        ulong ieee = ((((ulong)cbmBytes[0] + AdjustedBias) | ((ulong)sign << 4)) << 52) |
                      (((ulong)cbmBytes[1] & 0x7f) << 45) |
                       ((ulong)cbmBytes[2] << 37) |
                       ((ulong)cbmBytes[3] << 29) |
                       ((ulong)cbmBytes[4] << 21);

        return new Ieee754Converter(ieee).Double;
    }

    /// <summary>
    /// Convert a <see cref="double"/> into a CBM-formatted binary string.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="packed">The flag indicating whether the binary representation
    /// should be packed (5-byte) or not.</param>
    /// <returns>The byte string representation of the double.</returns>
    public static IEnumerable<byte> ToBytes(double value, bool packed)
    {
        var bytes = packed ? new byte[5] : new byte[6];

        // Convert float to binary.
        var ieeeConv = new Ieee754Converter(value);
        var ieee = ieeeConv.Binary;

        if (ieee == 0 || ieee == 0x8000000000000000)
        {
            // zero or negative zero
            return bytes;
        }

        // Calculate exponent
        var exp = (int)((ieee >> 52) & 0x7ff) - AdjustedBias;

        if (exp < 0)
        {
            // very small value
            return bytes;
        }

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

