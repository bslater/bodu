// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelper.Numeric.NetStandard.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

#if NETSTANDARD2_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Bodu;

public static partial class ThrowHelper
{
    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException" /> if <paramref name="count" /> is negative or exceeds
    /// <paramref name="available" />.
    /// </summary>
    /// <param name="count">The count value to validate.</param>
    /// <param name="available">The number of available items.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="count" /> &lt; 0 or <paramref name="count" /> &gt;
    /// <paramref name="available" />.
    /// </exception>
    /// <remarks>Use this method when validating that a subset operation will not exceed the size of the source.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfCountExceedsAvailable(int count, int available)
    {
        if (count < 0 || count > available)
            throw new ArgumentOutOfRangeException(
                nameof(count),
                string.Format(CultureInfo.CurrentCulture, ResourceStrings.Arg_OutOfRange_CountExceedsAvailable, available));
    }

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException" /> if <paramref name="value" /> is negative.
    /// </summary>
    /// <typeparam name="T">A comparable numeric type.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="value" /> &lt; 0.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNegative<T>(T value)
        where T : struct, IComparable<T>
    {
        if (value.CompareTo(default) < 0)
            throw new ArgumentOutOfRangeException(nameof(value), ResourceStrings.Arg_OutOfRange_RequireNonNegative);
    }

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException" /> if <paramref name="value" /> is not a positive multiple
    /// of <paramref name="divisor" />.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="divisor">The required positive divisor. Must itself be positive.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="value" /> &lt;= 0 or <c>value % divisor != 0</c>.
    /// </exception>
    /// <remarks>
    /// Useful for validating aligned buffer sizes, memory boundaries, or block-aligned lengths. When the
    /// required constraint is specifically a power of two, prefer <see cref="ThrowIfNotPowerOfTwo(int)" />,
    /// which uses a single bitwise operation.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNotPositiveMultipleOf(int value, int divisor)
    {
        if (value <= 0 || value % divisor != 0)
            throw new ArgumentOutOfRangeException(
                nameof(value),
                string.Format(CultureInfo.CurrentCulture, ResourceStrings.Arg_Invalid_PositiveMultipleOf, divisor));
    }

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException" /> if <paramref name="value" /> is not a positive power
    /// of two.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="value" /> is not a positive integer whose binary representation contains
    /// exactly one set bit (i.e. <c>value &lt;= 0</c> or <c>(value &amp; (value - 1)) != 0</c>).
    /// </exception>
    /// <remarks>
    /// A specialization of the positive-multiple-of family. When the divisor is itself an arbitrary positive
    /// integer rather than a power of two, use <see cref="ThrowIfNotPositiveMultipleOf(int, int)" /> instead.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNotPowerOfTwo(int value)
    {
        if (value <= 0 || (value & (value - 1)) != 0)
            throw new ArgumentOutOfRangeException(nameof(value), ResourceStrings.Arg_Invalid_PowerOfTwo);
    }

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException" /> if <paramref name="value" /> is not zero.
    /// </summary>
    /// <typeparam name="T">A type that implements <see cref="IEquatable{T}" />.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="value" /> is not equal to the default value of <typeparamref name="T" />.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNotZero<T>(T value)
        where T : struct, IEquatable<T>
    {
        if (!value.Equals(default))
            throw new ArgumentOutOfRangeException(nameof(value), ResourceStrings.Arg_OutOfRange_RequireZero);
    }

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException" /> if <paramref name="value" /> is positive.
    /// </summary>
    /// <typeparam name="T">A comparable numeric type.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="value" /> &gt; 0.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfPositive<T>(T value)
        where T : struct, IComparable<T>
    {
        if (value.CompareTo(default) > 0)
            throw new ArgumentOutOfRangeException(nameof(value), ResourceStrings.Arg_OutOfRange_RequireNonPositive);
    }

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException" /> if the sequence starting at <paramref name="start" />
    /// with <paramref name="count" /> elements would overflow <see cref="int.MaxValue" />.
    /// </summary>
    /// <param name="start">The starting value of the sequence.</param>
    /// <param name="count">The number of values in the sequence.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <c>start + count - 1</c> would exceed <see cref="int.MaxValue" />.
    /// </exception>
    /// <remarks>Prevents arithmetic overflow when generating <see cref="int" />-based numeric sequences.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfSequenceRangeOverflows(int start, int count)
    {
        if (count > 0 && start > int.MaxValue - (count - 1))
            throw new ArgumentOutOfRangeException(
                nameof(start),
                string.Format(CultureInfo.CurrentCulture, ResourceStrings.Arg_OutOfRange_SequenceRangeOverflow, nameof(Int32)));
    }

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException" /> if the sequence starting at <paramref name="start" />
    /// with <paramref name="count" /> elements would overflow <see cref="long.MaxValue" />.
    /// </summary>
    /// <param name="start">The starting value of the sequence.</param>
    /// <param name="count">The number of values in the sequence.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <c>start + count - 1</c> would exceed <see cref="long.MaxValue" />.
    /// </exception>
    /// <remarks>Prevents arithmetic overflow when generating <see cref="long" />-based numeric sequences.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfSequenceRangeOverflows(long start, int count)
    {
        if (count > 0 && start > long.MaxValue - (count - 1))
            throw new ArgumentOutOfRangeException(
                nameof(start),
                string.Format(CultureInfo.CurrentCulture, ResourceStrings.Arg_OutOfRange_SequenceRangeOverflow, nameof(Int64)));
    }

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException" /> if <paramref name="value" /> equals zero.
    /// </summary>
    /// <typeparam name="T">A type that implements <see cref="IEquatable{T}" />.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="value" /> equals the default value of <typeparamref name="T" />.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfZero<T>(T value)
        where T : struct, IEquatable<T>
    {
        if (value.Equals(default))
            throw new ArgumentOutOfRangeException(nameof(value), ResourceStrings.Arg_OutOfRange_RequireNonZero);
    }

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException" /> if <paramref name="value" /> is zero or negative.
    /// </summary>
    /// <typeparam name="T">A comparable numeric type.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="value" /> &lt;= 0.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfZeroOrNegative<T>(T value)
        where T : struct, IComparable<T>
    {
        if (value.CompareTo(default) <= 0)
            throw new ArgumentOutOfRangeException(nameof(value), ResourceStrings.Arg_OutOfRange_RequirePositive);
    }

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException" /> if <paramref name="value" /> is zero or positive.
    /// </summary>
    /// <typeparam name="T">A comparable numeric type.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="value" /> &gt;= 0.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfZeroOrPositive<T>(T value)
        where T : struct, IComparable<T>
    {
        if (value.CompareTo(default) >= 0)
            throw new ArgumentOutOfRangeException(nameof(value), ResourceStrings.Arg_OutOfRange_RequireNegative);
    }

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException" /> if <paramref name="value" /> is
    /// <see cref="double.NaN" />, <see cref="double.PositiveInfinity" />, or
    /// <see cref="double.NegativeInfinity" />.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="value" /> is not a finite number.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNotFinite(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
            throw new ArgumentOutOfRangeException(nameof(value), value, ResourceStrings.Arg_OutOfRange_NotFinite);
    }

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException" /> if <paramref name="value" /> is
    /// <see cref="float.NaN" />, <see cref="float.PositiveInfinity" />, or
    /// <see cref="float.NegativeInfinity" />.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="value" /> is not a finite number.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNotFinite(float value)
    {
        if (float.IsNaN(value) || float.IsInfinity(value))
            throw new ArgumentOutOfRangeException(nameof(value), value, ResourceStrings.Arg_OutOfRange_NotFinite);
    }
}

#endif
