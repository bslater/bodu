// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLKemEngine.Reduction.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides the modular reductions the ML-KEM arithmetic uses in place of the remainder operator: Montgomery reduction
/// for the transform's twiddle products, and Barrett reduction for its sums and for the base-case products.
/// </summary>
/// <remarks>
/// Every reduction is a fixed sequence of multiplications, shifts and masks, with no branch and no division, so its
/// timing does not depend on the value. Each states the input range within which its result is exact.
/// </remarks>
internal static partial class MLKemEngine
{
    /// <summary>q⁻¹ modulo 2^16, as a signed 16-bit value: q · QInverse ≡ 1 (mod 2^16).</summary>
    private const int QInverse = -3327;

    /// <summary>The Barrett multiplier ⌊(2^26 + ⌊q / 2⌋) / q⌋ for values below 2^15 in magnitude.</summary>
    private const int BarrettMultiplier = ((1 << 26) + (Q / 2)) / Q;

    /// <summary>The Barrett multiplier ⌊2^39 / q⌋ for non-negative values below 2^36.</summary>
    private const ulong WideBarrettMultiplier = (1UL << 39) / Q;

    /// <summary>2^32 mod q: the factor that <see cref="MontgomeryReduce" /> turns into 2^16, removing the 2^−16 a Montgomery product of two plain values carries.</summary>
    private const int MontgomerySquared = 1353;

    /// <summary>
    /// Returns a value congruent to <paramref name="value" /> · 2^−16 modulo q, in (−q, q).
    /// </summary>
    /// <param name="value">The value to reduce, below q · 2^15 in magnitude.</param>
    /// <returns>The Montgomery reduction of <paramref name="value" />.</returns>
    /// <remarks>
    /// The low 16 bits of <paramref name="value" /> · q⁻¹ give the multiple of q whose subtraction clears the low 16
    /// bits of <paramref name="value" />, so the arithmetic shift that follows divides exactly by 2^16. A twiddle
    /// factor stored multiplied by 2^16 therefore multiplies exactly: the reduction of ζ · 2^16 · x is ζ · x.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int MontgomeryReduce(int value)
    {
        int multiple = (short)unchecked(value * QInverse);
        return (value - (multiple * Q)) >> 16;
    }

    /// <summary>
    /// Returns the representative of <paramref name="value" /> modulo q in [−(q − 1) / 2, (q − 1) / 2].
    /// </summary>
    /// <param name="value">The value to reduce, below 2^15 in magnitude.</param>
    /// <returns>The centered representative.</returns>
    /// <remarks>
    /// The quotient is estimated as <paramref name="value" /> · ⌊(2^26 + ⌊q / 2⌋) / q⌋ / 2^26, rounded to nearest.
    /// Within the input range the product fits in 32 bits and the estimate is the nearest integer to
    /// <paramref name="value" /> / q.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int BarrettReduce(int value)
    {
        int quotient = ((BarrettMultiplier * value) + (1 << 25)) >> 26;
        return value - (quotient * Q);
    }

    /// <summary>
    /// Returns <paramref name="value" /> modulo q, in [0, q).
    /// </summary>
    /// <param name="value">The value to reduce, below 2^36.</param>
    /// <returns>The canonical representative.</returns>
    /// <remarks>
    /// The quotient estimate <paramref name="value" /> · ⌊2^39 / q⌋ / 2^39 falls short of <paramref name="value" /> / q
    /// by less than 1/8 within the input range, so the remainder it leaves is below 2q and one conditional subtraction
    /// finishes the reduction.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int ReduceWide(ulong value)
    {
        ulong quotient = (value * WideBarrettMultiplier) >> 39;
        int remainder = (int)(value - (quotient * Q)) - Q;
        return remainder + ((remainder >> 31) & Q);
    }

    /// <summary>
    /// Maps a value in [−q, q) to its representative in [0, q).
    /// </summary>
    /// <param name="value">The value, at least −q and below q.</param>
    /// <returns>The canonical representative.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int Canonicalize(int value) =>
        value + ((value >> 31) & Q);
}
