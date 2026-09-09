// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffRk.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff;

/// <summary>
/// Encodes and decodes the RK number representation: a 32-bit value whose two low bits select between a signed 30-bit
/// integer and the high 30 bits of an IEEE 754 double, optionally divided by 100.
/// </summary>
/// <remarks>
/// Bit 0 set means the decoded value is divided by 100; bit 1 set means the high 30 bits are a signed integer, clear
/// means they are the most significant 30 bits of a double whose remaining 34 bits are zero. Not every double has an RK
/// form — <see cref="TryEncode" /> reports when a value must be written as a <c>NUMBER</c> record instead.
/// </remarks>
public static class BiffRk
{
    /// <summary>The flag selecting division by 100.</summary>
    private const uint DividedByHundredFlag = 0x01;

    /// <summary>The flag selecting the signed-integer form.</summary>
    private const uint IntegerFlag = 0x02;

    /// <summary>The largest magnitude a signed 30-bit integer can hold.</summary>
    private const int MaxInteger = (1 << 29) - 1;

    /// <summary>The smallest value a signed 30-bit integer can hold.</summary>
    private const int MinInteger = -(1 << 29);

    /// <summary>
    /// Decodes an RK value.
    /// </summary>
    /// <param name="rk">The 32-bit RK value.</param>
    /// <returns>The number the value represents.</returns>
    public static double Decode(uint rk)
    {
        bool dividedByHundred = (rk & DividedByHundredFlag) != 0;
        bool isInteger = (rk & IntegerFlag) != 0;

        double value;
        if (isInteger)
        {
            // Arithmetic shift sign-extends the signed 30-bit integer stored in the high bits.
            value = ((int)rk) >> 2;
        }
        else
        {
            ulong bits = (ulong)(rk & 0xFFFFFFFC) << 32;
            value = BitConverter.Int64BitsToDouble((long)bits);
        }

        return dividedByHundred ? value / 100.0 : value;
    }

    /// <summary>
    /// Attempts to encode a number in RK form, preferring the integer forms and falling back to the truncated-double
    /// forms when they represent the value exactly.
    /// </summary>
    /// <param name="value">The number to encode.</param>
    /// <param name="rk">
    /// When this method returns, the RK value when one represents <paramref name="value" /> exactly.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="value" /> has an exact RK form; otherwise <see langword="false" />,
    /// in which case the value must be written as a <c>NUMBER</c> record.
    /// </returns>
    public static bool TryEncode(double value, out uint rk)
    {
        if (TryEncodeInteger(value, out rk))
            return true;

        double scaled = value * 100.0;
        if (TryEncodeInteger(scaled, out rk) && scaled / 100.0 == value)
        {
            rk |= DividedByHundredFlag;
            return true;
        }

        if (TryEncodeDouble(value, out rk))
            return true;

        if (TryEncodeDouble(scaled, out rk) && scaled / 100.0 == value)
        {
            rk |= DividedByHundredFlag;
            return true;
        }

        rk = 0;
        return false;
    }

    /// <summary>
    /// Attempts the signed 30-bit integer form.
    /// </summary>
    /// <param name="value">The number to encode.</param>
    /// <param name="rk">When this method returns, the RK value when the form applies.</param>
    /// <returns><see langword="true" /> when <paramref name="value" /> is an integer within the 30-bit range.</returns>
    private static bool TryEncodeInteger(double value, out uint rk)
    {
        if (double.IsFinite(value) && value == Math.Floor(value) && value >= MinInteger && value <= MaxInteger
            && !(value == 0 && double.IsNegative(value)))
        {
            rk = ((uint)(int)value << 2) | IntegerFlag;
            return true;
        }

        rk = 0;
        return false;
    }

    /// <summary>
    /// Attempts the truncated-double form, which applies only when the low 34 bits of the value are zero.
    /// </summary>
    /// <param name="value">The number to encode.</param>
    /// <param name="rk">When this method returns, the RK value when the form applies.</param>
    /// <returns><see langword="true" /> when the form represents <paramref name="value" /> exactly.</returns>
    private static bool TryEncodeDouble(double value, out uint rk)
    {
        ulong bits = (ulong)BitConverter.DoubleToInt64Bits(value);
        if ((bits & 0x3_FFFF_FFFFUL) != 0)
        {
            rk = 0;
            return false;
        }

        rk = (uint)(bits >> 32) & 0xFFFFFFFC;
        return true;
    }
}
