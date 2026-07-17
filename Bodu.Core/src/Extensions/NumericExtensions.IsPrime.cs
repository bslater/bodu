// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NumericExtensions.IsPrime.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;

namespace Bodu.Extensions;

public static partial class NumericExtensions
{
    /// <summary>
    /// Returns a value indicating whether the specified <see cref="short" /> is a prime number.
    /// </summary>
    /// <param name="value">The value to test.</param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="value" /> is greater than <c>1</c> and has no positive divisors
    /// other than <c>1</c> and itself; otherwise <see langword="false" />. Returns <see langword="false" /> for
    /// negative values and for <c>0</c> and <c>1</c>.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsPrime(this short value) =>
        value >= 2 && IsPrimeCore((ulong)value);

    /// <summary>
    /// Returns a value indicating whether the specified <see cref="int" /> is a prime number.
    /// </summary>
    /// <param name="value">The value to test.</param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="value" /> is greater than <c>1</c> and has no positive divisors
    /// other than <c>1</c> and itself; otherwise <see langword="false" />. Returns <see langword="false" /> for
    /// negative values and for <c>0</c> and <c>1</c>.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsPrime(this int value) =>
        value >= 2 && IsPrimeCore((ulong)value);

    /// <summary>
    /// Returns a value indicating whether the specified <see cref="long" /> is a prime number.
    /// </summary>
    /// <param name="value">The value to test.</param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="value" /> is greater than <c>1</c> and has no positive divisors
    /// other than <c>1</c> and itself; otherwise <see langword="false" />. Returns <see langword="false" /> for
    /// negative values and for <c>0</c> and <c>1</c>.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsPrime(this long value) =>
        value >= 2 && IsPrimeCore((ulong)value);

    /// <summary>
    /// Returns a value indicating whether the specified <see cref="ushort" /> is a prime number.
    /// </summary>
    /// <param name="value">The value to test.</param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="value" /> is greater than <c>1</c> and has no positive divisors
    /// other than <c>1</c> and itself; otherwise <see langword="false" />.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsPrime(this ushort value) =>
        value >= 2 && IsPrimeCore(value);

    /// <summary>
    /// Returns a value indicating whether the specified <see cref="uint" /> is a prime number.
    /// </summary>
    /// <param name="value">The value to test.</param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="value" /> is greater than <c>1</c> and has no positive divisors
    /// other than <c>1</c> and itself; otherwise <see langword="false" />.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsPrime(this uint value) =>
        value >= 2 && IsPrimeCore(value);

    /// <summary>
    /// Returns a value indicating whether the specified <see cref="ulong" /> is a prime number.
    /// </summary>
    /// <param name="value">The value to test.</param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="value" /> is greater than <c>1</c> and has no positive divisors
    /// other than <c>1</c> and itself; otherwise <see langword="false" />.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsPrime(this ulong value) =>
        value >= 2 && IsPrimeCore(value);

    /// <summary>
    /// Tests primality of an unsigned integer that is known to be greater than or equal to two, using a trial-division
    /// loop over odd divisors up to <c>sqrt(value)</c>.
    /// </summary>
    /// <param name="value">A candidate value, pre-validated to be at least <c>2</c>.</param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="value" /> is prime; otherwise <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// Returns <see langword="true" /> immediately for <c>2</c>; rejects all other even values via a single bit-test;
    /// iterates odd candidate divisors from <c>3</c> up to the integer square root of <paramref name="value" />
    /// inclusive.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsPrimeCore(ulong value)
    {
        if (value == 2) return true;
        if ((value & 1UL) == 0) return false;

        // Math.Sqrt is exact only up to 2^53: for larger values the double result can round below the true
        // integer square root, which would skip the sole divisor of a prime's square. Adjust the candidate
        // root to the exact integer square root using overflow-safe division comparisons
        // (limit * limit > value ⟺ limit > value / limit for positive integers).
        ulong limit = (ulong)Math.Sqrt(value);
        while (limit > 0 && limit > value / limit)
            limit--;

        while (limit + 1 <= value / (limit + 1))
            limit++;

        for (ulong divisor = 3; divisor <= limit; divisor += 2)
        {
            if (value % divisor == 0)
                return false;
        }

        return true;
    }
}
