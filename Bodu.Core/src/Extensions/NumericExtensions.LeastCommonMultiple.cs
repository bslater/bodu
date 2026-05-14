// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NumericExtensions.LeastCommonMultiple.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;

namespace Bodu.Extensions;

public static partial class NumericExtensions
{
    /// <summary>
    /// Returns the least common multiple of two non-negative <see cref="short"/> values.
    /// </summary>
    /// <param name="value">The first value.</param>
    /// <param name="other">The second value.</param>
    /// <returns>
    /// The least common multiple of <paramref name="value"/> and <paramref name="other"/>; <c>0</c> when
    /// either argument is <c>0</c>.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="value"/> or <paramref name="other"/> is negative.
    /// </exception>
    /// <exception cref="OverflowException">
    /// The result does not fit in a <see cref="short"/>.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static short LeastCommonMultiple(this short value, short other)
    {
        ThrowHelper.ThrowIfNegative(value);
        ThrowHelper.ThrowIfNegative(other);
        return checked((short)Lcm((ulong)value, (ulong)other));
    }

    /// <summary>
    /// Returns the least common multiple of two non-negative <see cref="int"/> values.
    /// </summary>
    /// <param name="value">The first value.</param>
    /// <param name="other">The second value.</param>
    /// <returns>
    /// The least common multiple of <paramref name="value"/> and <paramref name="other"/>; <c>0</c> when
    /// either argument is <c>0</c>.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="value"/> or <paramref name="other"/> is negative.
    /// </exception>
    /// <exception cref="OverflowException">The result does not fit in an <see cref="int"/>.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int LeastCommonMultiple(this int value, int other)
    {
        ThrowHelper.ThrowIfNegative(value);
        ThrowHelper.ThrowIfNegative(other);
        return checked((int)Lcm((ulong)value, (ulong)other));
    }

    /// <summary>
    /// Returns the least common multiple of two non-negative <see cref="long"/> values.
    /// </summary>
    /// <param name="value">The first value.</param>
    /// <param name="other">The second value.</param>
    /// <returns>
    /// The least common multiple of <paramref name="value"/> and <paramref name="other"/>; <c>0</c> when
    /// either argument is <c>0</c>.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="value"/> or <paramref name="other"/> is negative.
    /// </exception>
    /// <exception cref="OverflowException">The result does not fit in a <see cref="long"/>.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long LeastCommonMultiple(this long value, long other)
    {
        ThrowHelper.ThrowIfNegative(value);
        ThrowHelper.ThrowIfNegative(other);
        return checked((long)Lcm((ulong)value, (ulong)other));
    }

    /// <summary>
    /// Returns the least common multiple of two <see cref="ushort"/> values.
    /// </summary>
    /// <param name="value">The first value.</param>
    /// <param name="other">The second value.</param>
    /// <returns>
    /// The least common multiple of <paramref name="value"/> and <paramref name="other"/>; <c>0</c> when
    /// either argument is <c>0</c>.
    /// </returns>
    /// <exception cref="OverflowException">The result does not fit in a <see cref="ushort"/>.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort LeastCommonMultiple(this ushort value, ushort other) =>
        checked((ushort)Lcm(value, other));

    /// <summary>
    /// Returns the least common multiple of two <see cref="uint"/> values.
    /// </summary>
    /// <param name="value">The first value.</param>
    /// <param name="other">The second value.</param>
    /// <returns>
    /// The least common multiple of <paramref name="value"/> and <paramref name="other"/>; <c>0</c> when
    /// either argument is <c>0</c>.
    /// </returns>
    /// <exception cref="OverflowException">The result does not fit in a <see cref="uint"/>.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint LeastCommonMultiple(this uint value, uint other) =>
        checked((uint)Lcm(value, other));

    /// <summary>
    /// Returns the least common multiple of two <see cref="ulong"/> values.
    /// </summary>
    /// <param name="value">The first value.</param>
    /// <param name="other">The second value.</param>
    /// <returns>
    /// The least common multiple of <paramref name="value"/> and <paramref name="other"/>; <c>0</c> when
    /// either argument is <c>0</c>.
    /// </returns>
    /// <exception cref="OverflowException">
    /// The intermediate product exceeds the range of <see cref="ulong"/>.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong LeastCommonMultiple(this ulong value, ulong other) =>
        Lcm(value, other);

    /// <summary>
    /// Returns the least common multiple of every element in the supplied array of non-negative
    /// <see cref="short"/> values.
    /// </summary>
    /// <param name="values">The values whose common multiple is to be computed.</param>
    /// <returns>The least common multiple of every element in <paramref name="values"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="values"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="values"/> is empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// At least one element in <paramref name="values"/> is negative.
    /// </exception>
    /// <exception cref="OverflowException">
    /// The accumulating product cannot be represented as a <see cref="short"/>.
    /// </exception>
    public static short LeastCommonMultiple(this short[] values)
    {
        ThrowHelper.ThrowIfNull(values);
        ThrowHelper.ThrowIfArrayLengthIsZero(values);

        ThrowHelper.ThrowIfNegative(values[0], nameof(values));
        ulong acc = (ulong)values[0];
        for (int i = 1; i < values.Length; i++)
        {
            short v = values[i];
            ThrowHelper.ThrowIfNegative(v, nameof(values));
            acc = Lcm(acc, (ulong)v);
        }

        return checked((short)acc);
    }

    /// <summary>
    /// Returns the least common multiple of every element in the supplied array of non-negative
    /// <see cref="int"/> values.
    /// </summary>
    /// <param name="values">The values whose common multiple is to be computed.</param>
    /// <returns>The least common multiple of every element in <paramref name="values"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="values"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="values"/> is empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// At least one element in <paramref name="values"/> is negative.
    /// </exception>
    /// <exception cref="OverflowException">
    /// The accumulating product cannot be represented as an <see cref="int"/>.
    /// </exception>
    public static int LeastCommonMultiple(this int[] values)
    {
        ThrowHelper.ThrowIfNull(values);
        ThrowHelper.ThrowIfArrayLengthIsZero(values);

        ThrowHelper.ThrowIfNegative(values[0], nameof(values));
        ulong acc = (ulong)values[0];
        for (int i = 1; i < values.Length; i++)
        {
            int v = values[i];
            ThrowHelper.ThrowIfNegative(v, nameof(values));
            acc = Lcm(acc, (ulong)v);
        }

        return checked((int)acc);
    }

    /// <summary>
    /// Returns the least common multiple of every element in the supplied array of non-negative
    /// <see cref="long"/> values.
    /// </summary>
    /// <param name="values">The values whose common multiple is to be computed.</param>
    /// <returns>The least common multiple of every element in <paramref name="values"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="values"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="values"/> is empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// At least one element in <paramref name="values"/> is negative.
    /// </exception>
    /// <exception cref="OverflowException">
    /// The accumulating product cannot be represented as a <see cref="long"/>.
    /// </exception>
    public static long LeastCommonMultiple(this long[] values)
    {
        ThrowHelper.ThrowIfNull(values);
        ThrowHelper.ThrowIfArrayLengthIsZero(values);

        ThrowHelper.ThrowIfNegative(values[0], nameof(values));
        ulong acc = (ulong)values[0];
        for (int i = 1; i < values.Length; i++)
        {
            long v = values[i];
            ThrowHelper.ThrowIfNegative(v, nameof(values));
            acc = Lcm(acc, (ulong)v);
        }

        return checked((long)acc);
    }

    /// <summary>
    /// Returns the least common multiple of every element in the supplied array of
    /// <see cref="ushort"/> values.
    /// </summary>
    /// <param name="values">The values whose common multiple is to be computed.</param>
    /// <returns>The least common multiple of every element in <paramref name="values"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="values"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="values"/> is empty.</exception>
    /// <exception cref="OverflowException">
    /// The accumulating product cannot be represented as a <see cref="ushort"/>.
    /// </exception>
    public static ushort LeastCommonMultiple(this ushort[] values)
    {
        ThrowHelper.ThrowIfNull(values);
        ThrowHelper.ThrowIfArrayLengthIsZero(values);

        ulong acc = values[0];
        for (int i = 1; i < values.Length; i++)
            acc = Lcm(acc, values[i]);

        return checked((ushort)acc);
    }

    /// <summary>
    /// Returns the least common multiple of every element in the supplied array of <see cref="uint"/> values.
    /// </summary>
    /// <param name="values">The values whose common multiple is to be computed.</param>
    /// <returns>The least common multiple of every element in <paramref name="values"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="values"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="values"/> is empty.</exception>
    /// <exception cref="OverflowException">
    /// The accumulating product cannot be represented as a <see cref="uint"/>.
    /// </exception>
    public static uint LeastCommonMultiple(this uint[] values)
    {
        ThrowHelper.ThrowIfNull(values);
        ThrowHelper.ThrowIfArrayLengthIsZero(values);

        ulong acc = values[0];
        for (int i = 1; i < values.Length; i++)
            acc = Lcm(acc, values[i]);

        return checked((uint)acc);
    }

    /// <summary>
    /// Returns the least common multiple of every element in the supplied array of
    /// <see cref="ulong"/> values.
    /// </summary>
    /// <param name="values">The values whose common multiple is to be computed.</param>
    /// <returns>The least common multiple of every element in <paramref name="values"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="values"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="values"/> is empty.</exception>
    /// <exception cref="OverflowException">
    /// The accumulating product exceeds the range of <see cref="ulong"/>.
    /// </exception>
    public static ulong LeastCommonMultiple(this ulong[] values)
    {
        ThrowHelper.ThrowIfNull(values);
        ThrowHelper.ThrowIfArrayLengthIsZero(values);

        ulong acc = values[0];
        for (int i = 1; i < values.Length; i++)
            acc = Lcm(acc, values[i]);

        return acc;
    }

    /// <summary>
    /// Computes the least common multiple of two unsigned values.
    /// </summary>
    /// <param name="a">The first value.</param>
    /// <param name="b">The second value.</param>
    /// <returns>
    /// <c>lcm(a, b)</c>. Returns <c>0</c> when either argument is <c>0</c>. Performs the GCD division
    /// before multiplication to reduce overflow risk.
    /// </returns>
    /// <exception cref="OverflowException">
    /// The product of <c>a / gcd(a, b)</c> and <c>b</c> exceeds the range of <see cref="ulong"/>.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ulong Lcm(ulong a, ulong b)
    {
        if (a == 0 || b == 0) return 0;
        return checked((a / Gcd(a, b)) * b);
    }
}
