// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NumericExtensions.Digits.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;

namespace Bodu.Extensions;

public static partial class NumericExtensions
{
    /// <summary>
    /// Reverses the decimal digits of a <see cref="ushort" /> value.
    /// </summary>
    /// <param name="value">The 16-bit unsigned integer whose digits are to be reversed.</param>
    /// <returns>
    /// A <see cref="ushort" /> whose decimal digits appear in the reverse order of those in <paramref name="value" />.
    /// Leading zeros that emerge in the reversed value are not preserved.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort ReverseDigits(this ushort value) =>
        (ushort)ReverseDigitsCore(value);

    /// <summary>
    /// Reverses the decimal digits of a <see cref="uint" /> value.
    /// </summary>
    /// <param name="value">The 32-bit unsigned integer whose digits are to be reversed.</param>
    /// <returns>
    /// A <see cref="uint" /> whose decimal digits appear in the reverse order of those in <paramref name="value" />.
    /// Leading zeros that emerge in the reversed value are not preserved.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint ReverseDigits(this uint value) =>
        (uint)ReverseDigitsCore(value);

    /// <summary>
    /// Reverses the decimal digits of a <see cref="ulong" /> value.
    /// </summary>
    /// <param name="value">The 64-bit unsigned integer whose digits are to be reversed.</param>
    /// <returns>
    /// A <see cref="ulong" /> whose decimal digits appear in the reverse order of those in <paramref name="value" />.
    /// Leading zeros that emerge in the reversed value are not preserved.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong ReverseDigits(this ulong value) =>
        ReverseDigitsCore(value);

    /// <summary>
    /// Rotates the decimal digits of a <see cref="ushort" /> value one place to the left.
    /// </summary>
    /// <param name="value">The value whose digits are to be rotated.</param>
    /// <returns>The rotated value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort RotateDigitsLeft(this ushort value) =>
        (ushort)RotateDigitsCore(value, -1);

    /// <summary>
    /// Rotates the decimal digits of a <see cref="ushort" /> value the specified number of places to the left.
    /// </summary>
    /// <param name="value">The value whose digits are to be rotated.</param>
    /// <param name="count">The non-negative number of positions to rotate.</param>
    /// <returns>The rotated value.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count" /> is negative.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort RotateDigitsLeft(this ushort value, int count)
    {
        ThrowHelper.ThrowIfNegative(count);
        return (ushort)RotateDigitsCore(value, -count);
    }

    /// <summary>
    /// Rotates the decimal digits of a <see cref="uint" /> value one place to the left.
    /// </summary>
    /// <param name="value">The value whose digits are to be rotated.</param>
    /// <returns>The rotated value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint RotateDigitsLeft(this uint value) =>
        (uint)RotateDigitsCore(value, -1);

    /// <summary>
    /// Rotates the decimal digits of a <see cref="uint" /> value the specified number of places to the left.
    /// </summary>
    /// <param name="value">The value whose digits are to be rotated.</param>
    /// <param name="count">The non-negative number of positions to rotate.</param>
    /// <returns>The rotated value.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count" /> is negative.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint RotateDigitsLeft(this uint value, int count)
    {
        ThrowHelper.ThrowIfNegative(count);
        return (uint)RotateDigitsCore(value, -count);
    }

    /// <summary>
    /// Rotates the decimal digits of a <see cref="ulong" /> value one place to the left.
    /// </summary>
    /// <param name="value">The value whose digits are to be rotated.</param>
    /// <returns>The rotated value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong RotateDigitsLeft(this ulong value) =>
        RotateDigitsCore(value, -1);

    /// <summary>
    /// Rotates the decimal digits of a <see cref="ulong" /> value the specified number of places to the left.
    /// </summary>
    /// <param name="value">The value whose digits are to be rotated.</param>
    /// <param name="count">The non-negative number of positions to rotate.</param>
    /// <returns>The rotated value.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count" /> is negative.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong RotateDigitsLeft(this ulong value, int count)
    {
        ThrowHelper.ThrowIfNegative(count);
        return RotateDigitsCore(value, -count);
    }

    /// <summary>
    /// Rotates the decimal digits of a <see cref="ushort" /> value one place to the right.
    /// </summary>
    /// <param name="value">The value whose digits are to be rotated.</param>
    /// <returns>The rotated value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort RotateDigitsRight(this ushort value) =>
        (ushort)RotateDigitsCore(value, 1);

    /// <summary>
    /// Rotates the decimal digits of a <see cref="ushort" /> value the specified number of places to the right.
    /// </summary>
    /// <param name="value">The value whose digits are to be rotated.</param>
    /// <param name="count">The non-negative number of positions to rotate.</param>
    /// <returns>The rotated value.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count" /> is negative.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort RotateDigitsRight(this ushort value, int count)
    {
        ThrowHelper.ThrowIfNegative(count);
        return (ushort)RotateDigitsCore(value, count);
    }

    /// <summary>
    /// Rotates the decimal digits of a <see cref="uint" /> value one place to the right.
    /// </summary>
    /// <param name="value">The value whose digits are to be rotated.</param>
    /// <returns>The rotated value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint RotateDigitsRight(this uint value) =>
        (uint)RotateDigitsCore(value, 1);

    /// <summary>
    /// Rotates the decimal digits of a <see cref="uint" /> value the specified number of places to the right.
    /// </summary>
    /// <param name="value">The value whose digits are to be rotated.</param>
    /// <param name="count">The non-negative number of positions to rotate.</param>
    /// <returns>The rotated value.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count" /> is negative.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint RotateDigitsRight(this uint value, int count)
    {
        ThrowHelper.ThrowIfNegative(count);
        return (uint)RotateDigitsCore(value, count);
    }

    /// <summary>
    /// Rotates the decimal digits of a <see cref="ulong" /> value one place to the right.
    /// </summary>
    /// <param name="value">The value whose digits are to be rotated.</param>
    /// <returns>The rotated value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong RotateDigitsRight(this ulong value) =>
        RotateDigitsCore(value, 1);

    /// <summary>
    /// Rotates the decimal digits of a <see cref="ulong" /> value the specified number of places to the right.
    /// </summary>
    /// <param name="value">The value whose digits are to be rotated.</param>
    /// <param name="count">The non-negative number of positions to rotate.</param>
    /// <returns>The rotated value.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count" /> is negative.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong RotateDigitsRight(this ulong value, int count)
    {
        ThrowHelper.ThrowIfNegative(count);
        return RotateDigitsCore(value, count);
    }

    /// <summary>
    /// Returns the decimal digits of a <see cref="ushort" /> value in most-significant-first order.
    /// </summary>
    /// <param name="value">The value whose digits are to be enumerated.</param>
    /// <returns>
    /// A new array containing the digits of <paramref name="value" />; for <c>0</c> the result is a single-element
    /// array containing <c>0</c>.
    /// </returns>
    public static byte[] ToDigitArray(this ushort value) =>
        ToDigitArrayCore(value);

    /// <summary>
    /// Returns the decimal digits of a <see cref="uint" /> value in most-significant-first order.
    /// </summary>
    /// <param name="value">The value whose digits are to be enumerated.</param>
    /// <returns>
    /// A new array containing the digits of <paramref name="value" />; for <c>0</c> the result is a single-element
    /// array containing <c>0</c>.
    /// </returns>
    public static byte[] ToDigitArray(this uint value) =>
        ToDigitArrayCore(value);

    /// <summary>
    /// Returns the decimal digits of a <see cref="ulong" /> value in most-significant-first order.
    /// </summary>
    /// <param name="value">The value whose digits are to be enumerated.</param>
    /// <returns>
    /// A new array containing the digits of <paramref name="value" />; for <c>0</c> the result is a single-element
    /// array containing <c>0</c>.
    /// </returns>
    public static byte[] ToDigitArray(this ulong value) =>
        ToDigitArrayCore(value);

    /// <summary>
    /// Counts the number of decimal digits required to represent the supplied unsigned value.
    /// </summary>
    /// <param name="value">The value to measure.</param>
    /// <returns>The decimal digit count; <c>1</c> for <c>0</c>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int DigitLength(ulong value)
    {
        if (value == 0) return 1;

        var length = 0;
        while (value != 0)
        {
            length++;
            value /= 10;
        }

        return length;
    }

    /// <summary>
    /// Returns ten raised to the specified non-negative integer power.
    /// </summary>
    /// <param name="exponent">A non-negative exponent.</param>
    /// <returns>
    /// <c>10^<paramref name="exponent" /></c>. The caller must ensure the result fits in a <see cref="ulong" />; values
    /// up to <c>10^19</c> are representable.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ulong Pow10(int exponent)
    {
        ulong result = 1;
        for (var i = 0; i < exponent; i++)
            result *= 10;
        return result;
    }

    /// <summary>
    /// Reverses the decimal digits of an unsigned value.
    /// </summary>
    /// <param name="value">The value to reverse.</param>
    /// <returns>The reversed value; <c>0</c> when <paramref name="value" /> is <c>0</c>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ulong ReverseDigitsCore(ulong value)
    {
        ulong result = 0;
        while (value > 0)
        {
            result = (result * 10) + (value % 10);
            value /= 10;
        }

        return result;
    }

    /// <summary>
    /// Rotates the decimal digits of an unsigned value by the signed number of positions.
    /// </summary>
    /// <param name="value">The value whose digits are to be rotated.</param>
    /// <param name="count">
    /// The signed number of positions to rotate. Positive values rotate to the right (least significant digit becomes
    /// most significant); negative values rotate to the left.
    /// </param>
    /// <returns>
    /// The rotated value. Returns <paramref name="value" /> unchanged when it has fewer than two digits or when
    /// <paramref name="count" /> is congruent to zero modulo the digit length.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ulong RotateDigitsCore(ulong value, int count)
    {
        if (value == 0) return 0;

        var length = DigitLength(value);
        if (length < 2) return value;

        count %= length;
        if (count == 0) return value;
        if (count < 0) count += length;

        var divisor = Pow10(count);
        var multiplier = Pow10(length - count);
        return (value % divisor * multiplier) + (value / divisor);
    }

    /// <summary>
    /// Allocates and populates a most-significant-first digit array for the supplied value.
    /// </summary>
    /// <param name="value">The value to decompose.</param>
    /// <returns>The decimal digits of <paramref name="value" />.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static byte[] ToDigitArrayCore(ulong value)
    {
        var length = DigitLength(value);
        var digits = new byte[length];
        for (var i = length - 1; i >= 0; i--)
        {
            digits[i] = (byte)(value % 10);
            value /= 10;
        }

        return digits;
    }
}
