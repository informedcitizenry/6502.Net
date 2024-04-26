//-----------------------------------------------------------------------------
// Copyright (c) 2017-2024 informedcitizenry <informedcitizenry@gmail.com>
//
// Licensed under the MIT license. See LICENSE for full license information.
// 
//-----------------------------------------------------------------------------

using Antlr4.Runtime.Tree;

namespace Sixty502DotNet.Shared;

/// <summary>
/// A class that is responsible for converting a number string to its
/// <see cref="NumericValue"/>.
/// </summary>
public static partial class NumberConverter
{
    /// <summary>
    /// Convert a number string of the given radix (base), removing all
    /// separator characters.
    /// </summary>
    /// <param name="numberString">The number string.</param>
    /// <param name="radix">The number radix (base).</param>
    /// <returns>A <see cref="ValueBase"/> of the converted number.</returns>
    private static ValueBase ConvertNumber(string numberString, int radix, bool isFloat)
    {
        numberString = numberString.Replace("_", "");
        if (radix == 10)
        {
            return new NumericValue(Convert.ToDouble(numberString), isFloat);
        }
        if (isFloat)
        {
            int decIndex = numberString.IndexOf('.');
            int expIndex = radix != 16 ?
                numberString.IndexOfAny(new char[] { 'p', 'P', 'e', 'E' }) :
                numberString.IndexOfAny(new char[] { 'p', 'P' });
            int mantissaLen = decIndex > -1 ? decIndex : expIndex;
            double mantissa = Convert.ToInt64(numberString[0..mantissaLen], radix);
            if (decIndex > -1)
            {
                mantissaLen++;
                int fracEnd = expIndex > -1 ? expIndex : numberString.Length;
                int fracLen = fracEnd - mantissaLen;
                mantissa += Convert.ToInt64(numberString.Substring(mantissaLen, fracLen), radix) * (1.0 / Math.Pow(radix, fracLen));
            }
            if (expIndex > -1)
            {
                int expBase = numberString[expIndex] == 'p' || numberString[expIndex] == 'P' ? 2 : 10;
                expIndex++;
                string expStr = numberString[expIndex..];
                return new NumericValue(mantissa * Math.Pow(expBase, double.Parse(expStr)), true);
            }
            return new NumericValue(mantissa, true);
        }
        return new NumericValue(Convert.ToInt64(numberString, radix) * 1.0, false)
        {
            IsBinary = true
        };
    }

    /// <summary>
    /// Convert an integer literal from a <see cref="ITerminalNode"/> symbol.
    /// </summary>
    /// <param name="terminal">The terminal symbol.</param>
    /// <returns>The integer converted from the terminal symbol text.
    /// </returns>
    /// <exception cref="Error"></exception>
    public static int ConvertIntLiteral(ITerminalNode terminal)
    {
        if (!int.TryParse(terminal.GetText(), out int value))
        {
            throw new Error(terminal.Symbol, "Integer literal value expected");
        }
        return value;
    }

    /// <summary>
    /// Convert a number literal from a <see cref="ITerminalNode"/> symbol.
    /// </summary>
    /// <param name="terminal">The terminal symbol.</param>
    /// <returns>The number converted from the terminal symbol text.</returns>
    /// <exception cref="Error"></exception>
    public static int ConvertLiteral(ITerminalNode terminal)
    {
        string text = terminal.GetText();
        int radix = 10;
        int start = 0;
        if (text.Length > 1)
        {
            if (!char.IsAsciiHexDigit(text[1]))
            {
                radix = text[1] switch
                {
                    'X' or 'x' => 16,
                    'O' or 'o' => 8,
                    _ => 2,
                };
                start = 2;
            }
            else if (!char.IsDigit(text[0]))
            {
                radix = text[0] == '$' ? 16 : 2;
                start = 1;
            }
        }
        try
        {
            return Convert.ToInt32(text[start..], radix);
        }
        catch (FormatException)
        {
            throw new Error(terminal.Symbol, "Integer literal value expected");
        }
        catch
        {
            throw new IllegalQuantityError(terminal.Symbol);
        }
    }

    /// <summary>
    /// Convert the number string decimal to a <see cref="ValueBase"/>.
    /// </summary>
    /// <param name="numberString">The number string.</param>
    /// <param name="isFloat">Indicates that the number string is a float.</param>
    /// <returns>The decimal as a <see cref="ValueBase"/>.</returns>
    public static ValueBase ConvertDecimal(string numberString, bool isFloat)
    {
        return ConvertNumber(numberString, 10, isFloat);
    }

    /// <summary>
    /// Convert a hex string to a <see cref="ValueBase"/>.
    /// </summary>
    /// <param name="numberString">The hex string.</param>
    /// <param name="isFloat">Indicates that the number string is a float.</param>
    /// <returns>The hexadecimal as a <see cref="ValueBase"/>.</returns>
    public static ValueBase ConvertHex(string numberString, bool isFloat)
    {
        if (char.IsDigit(numberString[0]))
        {
            return ConvertNumber(numberString[2..], 16, isFloat);
        }
        return ConvertNumber(numberString[1..], 16, isFloat);
    }

    /// <summary>
    /// Convert an octal string to a <see cref="ValueBase"/>.
    /// </summary>
    /// <param name="numberString">The octal string.</param>
    /// <param name="isFloat">Indicates that the number string is a float.</param>
    /// <returns>The octal as a <see cref="ValueBase"/>.</returns>
    public static ValueBase ConvertOctal(string numberString, bool isFloat)
    {
        if (char.IsDigit(numberString[1]))
        {
            return ConvertNumber(numberString, 8, isFloat);
        }
        return ConvertNumber(numberString[2..], 8, isFloat);
    }

    /// <summary>
    /// Convert a binary string to a <see cref="ValueBase"/>.
    /// </summary>
    /// <param name="numberString">The binary string.</param>
    /// <param name="isFloat">Indicates that the number string is a float.</param>
    /// <returns>The binary as a <see cref="ValueBase"/>.</returns>
    public static ValueBase ConvertBinary(string numberString, bool isFloat)
    {
        if (char.IsDigit(numberString[1]))
        {
            return ConvertNumber(numberString[1..], 2, isFloat);
        }
        if (char.IsDigit(numberString[0]))
        {
            return ConvertNumber(numberString[2..], 2, isFloat);
        }
        return ConvertNumber(numberString[1..].Replace(".", "0").Replace("#", "1"), 2, false);
    }
}

