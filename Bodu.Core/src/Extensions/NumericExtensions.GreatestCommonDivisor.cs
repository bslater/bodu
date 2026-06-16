// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NumericExtensions.GreatestCommonDivisor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;

namespace Bodu.Extensions;

public static partial class NumericExtensions
{
    /// <summary>
    /// Returns the greatest common divisor of two non-negative <see cref="short" /> values.
    /// </summary>
    /// <param name="value">The first value.</param>
    /// <param name="other">The second value.</param>
    /// <returns>The greatest common divisor of <paramref name="value" /> and <paramref name="other" />.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="value" /> or <paramref name="other" /> is negative.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static short GreatestCommonDivisor(this short value, short other)
    {
        ThrowHelper.ThrowIfNegative(value);
        ThrowHelper.ThrowIfNegative(other);
        return (short)Gcd((ulong)value, (ulong)other);
    }

    /// <summary>
    /// Returns the greatest common divisor of two non-negative <see cref="int" /> values.
    /// </summary>
    /// <param name="value">The first value.</param>
    /// <param name="other">The second value.</param>
    /// <returns>The greatest common divisor of <paramref name="value" /> and <paramref name="other" />.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="value" /> or <paramref name="other" /> is negative.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GreatestCommonDivisor(this int value, int other)
    {
        ThrowHelper.ThrowIfNegative(value);
        ThrowHelper.ThrowIfNegative(other);
        return (int)Gcd((ulong)value, (ulong)other);
    }

    /// <summary>
    /// Returns the greatest common divisor of two non-negative <see cref="long" /> values.
    /// </summary>
    /// <param name="value">The first value.</param>
    /// <param name="other">The second value.</param>
    /// <returns>The greatest common divisor of <paramref name="value" /> and <paramref name="other" />.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="value" /> or <paramref name="other" /> is negative.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long GreatestCommonDivisor(this long value, long other)
    {
        ThrowHelper.ThrowIfNegative(value);
        ThrowHelper.ThrowIfNegative(other);
        return (long)Gcd((ulong)value, (ulong)other);
    }

    /// <summary>
    /// Returns the greatest common divisor of two <see cref="ushort" /> values.
    /// </summary>
    /// <param name="value">The first value.</param>
    /// <param name="other">The second value.</param>
    /// <returns>The greatest common divisor of <paramref name="value" /> and <paramref name="other" />.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort GreatestCommonDivisor(this ushort value, ushort other) =>
        (ushort)Gcd(value, other);

    /// <summary>
    /// Returns the greatest common divisor of two <see cref="uint" /> values.
    /// </summary>
    /// <param name="value">The first value.</param>
    /// <param name="other">The second value.</param>
    /// <returns>The greatest common divisor of <paramref name="value" /> and <paramref name="other" />.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint GreatestCommonDivisor(this uint value, uint other) =>
        (uint)Gcd(value, other);

    /// <summary>
    /// Returns the greatest common divisor of two <see cref="ulong" /> values.
    /// </summary>
    /// <param name="value">The first value.</param>
    /// <param name="other">The second value.</param>
    /// <returns>The greatest common divisor of <paramref name="value" /> and <paramref name="other" />.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong GreatestCommonDivisor(this ulong value, ulong other) =>
        Gcd(value, other);

    /// <summary>
    /// Returns the greatest common divisor of every element in the supplied array of non-negative <see cref="short" />
    /// values.
    /// </summary>
    /// <param name="values">The values whose common divisor is to be computed.</param>
    /// <returns>The greatest common divisor of every element in <paramref name="values" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="values" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException"><paramref name="values" /> is empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// At least one element in <paramref name="values" /> is negative.
    /// </exception>
    public static short GreatestCommonDivisor(this short[] values)
    {
        ThrowHelper.ThrowIfNull(values);
        ThrowHelper.ThrowIfArrayLengthIsZero(values);

        ulong acc = 0;
        for (int i = 0; i < values.Length; i++)
        {
            short v = values[i];
            ThrowHelper.ThrowIfNegative(v, nameof(values));
            acc = Gcd(acc, (ulong)v);
        }

        return (short)acc;
    }

    /// <summary>
    /// Returns the greatest common divisor of every element in the supplied array of non-negative <see cref="int" />
    /// values.
    /// </summary>
    /// <param name="values">The values whose common divisor is to be computed.</param>
    /// <returns>The greatest common divisor of every element in <paramref name="values" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="values" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException"><paramref name="values" /> is empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// At least one element in <paramref name="values" /> is negative.
    /// </exception>
    public static int GreatestCommonDivisor(this int[] values)
    {
        ThrowHelper.ThrowIfNull(values);
        ThrowHelper.ThrowIfArrayLengthIsZero(values);

        ulong acc = 0;
        for (int i = 0; i < values.Length; i++)
        {
            int v = values[i];
            ThrowHelper.ThrowIfNegative(v, nameof(values));
            acc = Gcd(acc, (ulong)v);
        }

        return (int)acc;
    }

    /// <summary>
    /// Returns the greatest common divisor of every element in the supplied array of non-negative <see cref="long" />
    /// values.
    /// </summary>
    /// <param name="values">The values whose common divisor is to be computed.</param>
    /// <returns>The greatest common divisor of every element in <paramref name="values" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="values" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException"><paramref name="values" /> is empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// At least one element in <paramref name="values" /> is negative.
    /// </exception>
    public static long GreatestCommonDivisor(this long[] values)
    {
        ThrowHelper.ThrowIfNull(values);
        ThrowHelper.ThrowIfArrayLengthIsZero(values);

        ulong acc = 0;
        for (int i = 0; i < values.Length; i++)
        {
            long v = values[i];
            ThrowHelper.ThrowIfNegative(v, nameof(values));
            acc = Gcd(acc, (ulong)v);
        }

        return (long)acc;
    }

    /// <summary>
    /// Returns the greatest common divisor of every element in the supplied array of <see cref="ushort" /> values.
    /// </summary>
    /// <param name="values">The values whose common divisor is to be computed.</param>
    /// <returns>The greatest common divisor of every element in <paramref name="values" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="values" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException"><paramref name="values" /> is empty.</exception>
    public static ushort GreatestCommonDivisor(this ushort[] values)
    {
        ThrowHelper.ThrowIfNull(values);
        ThrowHelper.ThrowIfArrayLengthIsZero(values);

        ulong acc = 0;
        for (int i = 0; i < values.Length; i++)
            acc = Gcd(acc, values[i]);

        return (ushort)acc;
    }

    /// <summary>
    /// Returns the greatest common divisor of every element in the supplied array of <see cref="uint" /> values.
    /// </summary>
    /// <param name="values">The values whose common divisor is to be computed.</param>
    /// <returns>The greatest common divisor of every element in <paramref name="values" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="values" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException"><paramref name="values" /> is empty.</exception>
    public static uint GreatestCommonDivisor(this uint[] values)
    {
        ThrowHelper.ThrowIfNull(values);
        ThrowHelper.ThrowIfArrayLengthIsZero(values);

        ulong acc = 0;
        for (int i = 0; i < values.Length; i++)
            acc = Gcd(acc, values[i]);

        return (uint)acc;
    }

    /// <summary>
    /// Returns the greatest common divisor of every element in the supplied array of <see cref="ulong" /> values.
    /// </summary>
    /// <param name="values">The values whose common divisor is to be computed.</param>
    /// <returns>The greatest common divisor of every element in <paramref name="values" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="values" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException"><paramref name="values" /> is empty.</exception>
    public static ulong GreatestCommonDivisor(this ulong[] values)
    {
        ThrowHelper.ThrowIfNull(values);
        ThrowHelper.ThrowIfArrayLengthIsZero(values);

        ulong acc = 0;
        for (int i = 0; i < values.Length; i++)
            acc = Gcd(acc, values[i]);

        return acc;
    }

    /// <summary>
    /// Computes the greatest common divisor of two unsigned values using the iterative Euclidean algorithm.
    /// </summary>
    /// <param name="a">The first value.</param>
    /// <param name="b">The second value.</param>
    /// <returns>
    /// <c>gcd(a, b)</c>. When both arguments are zero the result is zero; otherwise the larger non-zero argument is
    /// returned when the other is zero.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ulong Gcd(ulong a, ulong b)
    {
        while (b != 0)
        {
            ulong t = a % b;
            a = b;
            b = t;
        }

        return a;
    }
}
