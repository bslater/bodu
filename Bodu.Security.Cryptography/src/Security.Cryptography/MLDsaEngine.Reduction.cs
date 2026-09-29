// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLDsaEngine.Reduction.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides the modular reductions the ML-DSA arithmetic uses in place of the remainder operator: Montgomery reduction
/// with R = 2^32 for the transforms' twiddle products and the coefficient-wise products, and a Barrett-style reduction
/// for sums.
/// </summary>
/// <remarks>
/// <para>
/// Every reduction is a fixed sequence of multiplications, shifts and masks, with no branch and no division, so its
/// timing does not depend on the value. Each states the input range within which its result is exact.
/// </para>
/// <para>
/// A value in Montgomery form is stored multiplied by 2^32. <see cref="MontgomeryReduce" /> of the product of a value
/// in Montgomery form and a plain value is their plain product, so each coefficient-wise product needs one reduction as
/// long as one of its factors was converted beforehand with <see cref="ToMontgomery(int)" />.
/// </para>
/// </remarks>
internal static partial class MLDsaEngine
{
    /// <summary>q⁻¹ modulo 2^32: q · QInverse ≡ 1 (mod 2^32).</summary>
    private const int QInverse = 58728449;

    /// <summary>2^64 mod q: the factor that <see cref="MontgomeryReduce" /> turns into 2^32, converting to Montgomery form.</summary>
    private const long MontgomerySquared = 2365951;

    /// <summary>
    /// Returns a value congruent to <paramref name="value" /> · 2^−32 modulo q, in (−q, q).
    /// </summary>
    /// <param name="value">The value to reduce, below 2^31 · q in magnitude.</param>
    /// <returns>The Montgomery reduction of <paramref name="value" />.</returns>
    /// <remarks>
    /// The low 32 bits of <paramref name="value" /> · q⁻¹ give the multiple of q whose subtraction clears the low 32
    /// bits of <paramref name="value" />, so the arithmetic shift that follows divides exactly by 2^32.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int MontgomeryReduce(long value)
    {
        int multiple = unchecked((int)value * QInverse);
        return (int)((value - ((long)multiple * Q)) >> 32);
    }

    /// <summary>
    /// Returns a representative of <paramref name="value" /> modulo q within [−6283008, 6283008], inside (−q, q).
    /// </summary>
    /// <param name="value">The value to reduce, from −2^31 to 2^31 − 2^22 − 1.</param>
    /// <returns>The reduced representative.</returns>
    /// <remarks>
    /// q is just below 2^23, so rounding <paramref name="value" /> / 2^23 to nearest estimates the multiple of q to
    /// subtract.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int Reduce32(int value)
    {
        int quotient = (value + (1 << 22)) >> 23;
        return value - (quotient * Q);
    }

    /// <summary>
    /// Maps a value in [−q, q) to its representative in [0, q).
    /// </summary>
    /// <param name="value">The value, at least −q and below q.</param>
    /// <returns>The canonical representative.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int Canonicalize(int value) =>
        value + ((value >> 31) & Q);

    /// <summary>
    /// Returns the representative of <paramref name="value" /> modulo q in [0, q).
    /// </summary>
    /// <param name="value">The value, from −2^31 to 2^31 − 2^22 − 1.</param>
    /// <returns>The canonical representative.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int Freeze(int value) =>
        Canonicalize(Reduce32(value));

    /// <summary>
    /// Returns the Montgomery form of a coefficient: <paramref name="value" /> · 2^32 modulo q, in (−q, q).
    /// </summary>
    /// <param name="value">The coefficient.</param>
    /// <returns>The coefficient in Montgomery form.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int ToMontgomery(int value) =>
        MontgomeryReduce(value * MontgomerySquared);

    /// <summary>
    /// Converts every coefficient of a polynomial to Montgomery form, in place, with the kernel dispatch selects.
    /// </summary>
    /// <param name="poly">The 256 coefficients, replaced by their Montgomery form.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="poly" /> holds fewer than 256 coefficients.
    /// </exception>
    internal static void ToMontgomery(Span<int> poly) =>
        ToMontgomery(KernelKind.Auto, poly);

    /// <summary>
    /// Converts every coefficient of a polynomial to Montgomery form, in place, with the specified kernel.
    /// </summary>
    /// <param name="kernel">
    /// The kernel; <see cref="KernelKind.Auto" /> for the one dispatch selects. Any other kind must be one the
    /// processor supports.
    /// </param>
    /// <param name="poly">The 256 coefficients, replaced by their Montgomery form.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="poly" /> holds fewer than 256 coefficients.
    /// </exception>
    /// <remarks>
    /// Every kernel produces the same coefficients as <see cref="ToMontgomery(int)" />.
    /// </remarks>
    internal static void ToMontgomery(KernelKind kernel, Span<int> poly)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(poly.Length, N, nameof(poly));

        if (Resolve(kernel) == KernelKind.Avx2)
        {
            Vector256Kernel.ToMontgomery(ref MemoryMarshal.GetReference(poly));
            return;
        }

        for (int i = 0; i < N; i++)
            poly[i] = ToMontgomery(poly[i]);
    }

    /// <summary>
    /// Reduces every coefficient of a polynomial with <see cref="Reduce32(int)" />, in place, with the kernel dispatch
    /// selects.
    /// </summary>
    /// <param name="poly">
    /// The 256 coefficients, each from −2^31 to 2^31 − 2^22 − 1, replaced by their reductions.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="poly" /> holds fewer than 256 coefficients.
    /// </exception>
    internal static void Reduce32(Span<int> poly) =>
        Reduce32(KernelKind.Auto, poly);

    /// <summary>
    /// Reduces every coefficient of a polynomial with <see cref="Reduce32(int)" />, in place, with the specified
    /// kernel.
    /// </summary>
    /// <param name="kernel">
    /// The kernel; <see cref="KernelKind.Auto" /> for the one dispatch selects. Any other kind must be one the
    /// processor supports.
    /// </param>
    /// <param name="poly">
    /// The 256 coefficients, each from −2^31 to 2^31 − 2^22 − 1, replaced by their reductions.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="poly" /> holds fewer than 256 coefficients.
    /// </exception>
    /// <remarks>
    /// Every kernel produces the same coefficients as <see cref="Reduce32(int)" />.
    /// </remarks>
    internal static void Reduce32(KernelKind kernel, Span<int> poly)
    {
        ThrowHelper.ThrowIfLessThan(poly.Length, N, nameof(poly));

        if (Resolve(kernel) == KernelKind.Avx2)
        {
            Vector256Kernel.Reduce32(ref MemoryMarshal.GetReference(poly));
            return;
        }

        for (int i = 0; i < N; i++)
            poly[i] = Reduce32(poly[i]);
    }

    /// <summary>
    /// Adds two polynomials coefficient-wise modulo q with the kernel dispatch selects.
    /// </summary>
    /// <param name="left">The first polynomial. Coefficients in [0, q).</param>
    /// <param name="right">The second polynomial. Coefficients in [0, q).</param>
    /// <param name="sum">The span receiving the sum, coefficients in [0, q). May be either operand.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="left" />, <paramref name="right" /> or <paramref name="sum" /> holds fewer than 256
    /// coefficients.
    /// </exception>
    internal static void AddModQ(ReadOnlySpan<int> left, ReadOnlySpan<int> right, Span<int> sum) =>
        AddModQ(KernelKind.Auto, left, right, sum);

    /// <summary>
    /// Adds two polynomials coefficient-wise modulo q with the specified kernel.
    /// </summary>
    /// <param name="kernel">
    /// The kernel; <see cref="KernelKind.Auto" /> for the one dispatch selects. Any other kind must be one the
    /// processor supports.
    /// </param>
    /// <param name="left">The first polynomial. Coefficients in [0, q).</param>
    /// <param name="right">The second polynomial. Coefficients in [0, q).</param>
    /// <param name="sum">The span receiving the sum, coefficients in [0, q). May be either operand.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="left" />, <paramref name="right" /> or <paramref name="sum" /> holds fewer than 256
    /// coefficients.
    /// </exception>
    /// <remarks>
    /// Every kernel produces the same coefficients.
    /// </remarks>
    internal static void AddModQ(KernelKind kernel, ReadOnlySpan<int> left, ReadOnlySpan<int> right, Span<int> sum)
    {
        ThrowHelper.ThrowIfLessThan(left.Length, N, nameof(left));
        ThrowHelper.ThrowIfLessThan(right.Length, N, nameof(right));
        ThrowHelper.ThrowIfLessThan(sum.Length, N, nameof(sum));

        if (Resolve(kernel) == KernelKind.Avx2)
        {
            Vector256Kernel.AddModQ(ref MemoryMarshal.GetReference(left), ref MemoryMarshal.GetReference(right), ref MemoryMarshal.GetReference(sum));
            return;
        }

        for (int i = 0; i < N; i++)
            sum[i] = Canonicalize(left[i] + right[i] - Q);
    }

    /// <summary>
    /// Subtracts one polynomial from another coefficient-wise modulo q with the kernel dispatch selects.
    /// </summary>
    /// <param name="left">The minuend. Coefficients in [0, q).</param>
    /// <param name="right">The subtrahend. Coefficients in [0, q).</param>
    /// <param name="difference">
    /// The span receiving the difference, coefficients in [0, q). May be either operand.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="left" />, <paramref name="right" /> or <paramref name="difference" /> holds fewer than 256
    /// coefficients.
    /// </exception>
    internal static void SubtractModQ(ReadOnlySpan<int> left, ReadOnlySpan<int> right, Span<int> difference) =>
        SubtractModQ(KernelKind.Auto, left, right, difference);

    /// <summary>
    /// Subtracts one polynomial from another coefficient-wise modulo q with the specified kernel.
    /// </summary>
    /// <param name="kernel">
    /// The kernel; <see cref="KernelKind.Auto" /> for the one dispatch selects. Any other kind must be one the
    /// processor supports.
    /// </param>
    /// <param name="left">The minuend. Coefficients in [0, q).</param>
    /// <param name="right">The subtrahend. Coefficients in [0, q).</param>
    /// <param name="difference">
    /// The span receiving the difference, coefficients in [0, q). May be either operand.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="left" />, <paramref name="right" /> or <paramref name="difference" /> holds fewer than 256
    /// coefficients.
    /// </exception>
    /// <remarks>
    /// Every kernel produces the same coefficients.
    /// </remarks>
    internal static void SubtractModQ(KernelKind kernel, ReadOnlySpan<int> left, ReadOnlySpan<int> right, Span<int> difference)
    {
        ThrowHelper.ThrowIfLessThan(left.Length, N, nameof(left));
        ThrowHelper.ThrowIfLessThan(right.Length, N, nameof(right));
        ThrowHelper.ThrowIfLessThan(difference.Length, N, nameof(difference));

        if (Resolve(kernel) == KernelKind.Avx2)
        {
            Vector256Kernel.SubtractModQ(ref MemoryMarshal.GetReference(left), ref MemoryMarshal.GetReference(right), ref MemoryMarshal.GetReference(difference));
            return;
        }

        for (int i = 0; i < N; i++)
            difference[i] = Canonicalize(left[i] - right[i]);
    }
}
