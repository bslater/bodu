// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelper.Numeric-CallerExpression.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

#if !NETSTANDARD2_0_OR_GREATER
#pragma warning disable SA1117 // Parameters should be on same line or separate lines
#pragma warning disable IDE0011 // Add braces

using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Bodu;

public static partial class ThrowHelper
{
    /// <summary>
    /// Cached parsed format for <see cref="ResourceStrings.Arg_Invalid_PositiveMultipleOf" />.
    /// </summary>
    private static readonly CompositeFormat s_argInvalidPositiveMultipleOf =
        CompositeFormat.Parse(ResourceStrings.Arg_Invalid_PositiveMultipleOf);

    /// <summary>
    /// Cached parsed format for <see cref="ResourceStrings.Arg_OutOfRange_CountExceedsAvailable" />.
    /// </summary>
    private static readonly CompositeFormat s_argOutOfRangeCountExceedsAvailable =
        CompositeFormat.Parse(ResourceStrings.Arg_OutOfRange_CountExceedsAvailable);

    /// <summary>
    /// Cached parsed format for <see cref="ResourceStrings.Arg_OutOfRange_SequenceRangeOverflow" />.
    /// </summary>
    private static readonly CompositeFormat s_argOutOfRangeSequenceRangeOverflow =
        CompositeFormat.Parse(ResourceStrings.Arg_OutOfRange_SequenceRangeOverflow);

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException" /> if <paramref name="count" /> is negative or exceeds
    /// <paramref name="available" />.
    /// </summary>
    /// <param name="count">The count value to validate.</param>
    /// <param name="available">The number of available items.</param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="count" /> &lt; 0 or <paramref name="count" /> &gt; <paramref name="available" />.
    /// </exception>
    /// <remarks>
    /// Use this method when validating that a subset operation will not exceed the size of the source.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfCountExceedsAvailable(
        int count, int available,
        [CallerArgumentExpression(nameof(count))] string? paramName = null)
    {
        if (count < 0 || count > available)
            throw new ArgumentOutOfRangeException(
                paramName,
                string.Format(CultureInfo.CurrentCulture, s_argOutOfRangeCountExceedsAvailable, available));
    }

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException" /> if <paramref name="value" /> is negative.
    /// </summary>
    /// <typeparam name="T">A comparable numeric type.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="value" /> &lt; 0.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNegative<T>(
        T value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
        where T : struct, IComparable<T>
    {
        if (value.CompareTo(default) < 0)
            throw new ArgumentOutOfRangeException(paramName, ResourceStrings.Arg_OutOfRange_RequireNonNegative);
    }

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException" /> if <paramref name="value" /> is not a positive multiple of
    /// <paramref name="divisor" />.
    /// </summary>
    /// <typeparam name="T">A binary integer type.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="divisor">The required positive divisor. Must itself be positive.</param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="value" /> &lt;= 0 or <c>value % divisor != 0</c>.
    /// </exception>
    /// <remarks>
    /// Useful for validating aligned buffer sizes, memory boundaries, or block-aligned lengths. When the required
    /// constraint is specifically a power of two, prefer <see cref="ThrowIfNotPowerOfTwo{T}" />, which uses a single
    /// bitwise operation.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNotPositiveMultipleOf<T>(
        T value, T divisor,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
        where T : IBinaryInteger<T>
    {
        if (value <= T.Zero || value % divisor != T.Zero)
            throw new ArgumentOutOfRangeException(
                paramName,
                string.Format(CultureInfo.CurrentCulture, s_argInvalidPositiveMultipleOf, divisor));
    }

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException" /> if <paramref name="value" /> is not a positive power of
    /// two.
    /// </summary>
    /// <typeparam name="T">A binary integer type.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="value" /> is not a positive integer whose binary representation contains exactly one
    /// set bit (i.e. <c>value &lt;= 0</c> or <c>(value &amp; (value - 1)) != 0</c>).
    /// </exception>
    /// <remarks>
    /// A specialization of the positive-multiple-of family. When the divisor is itself an arbitrary positive integer
    /// rather than a power of two, use <see cref="ThrowIfNotPositiveMultipleOf{T}" /> instead.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNotPowerOfTwo<T>(
        T value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
        where T : IBinaryInteger<T>
    {
        if (!T.IsPow2(value))
            throw new ArgumentOutOfRangeException(paramName, ResourceStrings.Arg_Invalid_PowerOfTwo);
    }

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException" /> if <paramref name="value" /> is not zero.
    /// </summary>
    /// <typeparam name="T">A type that implements <see cref="IEquatable{T}" />.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="value" /> is not equal to the default value of <typeparamref name="T" />.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNotZero<T>(
        T value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
        where T : struct, IEquatable<T>
    {
        if (!value.Equals(default))
            throw new ArgumentOutOfRangeException(paramName, ResourceStrings.Arg_OutOfRange_RequireZero);
    }

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException" /> if <paramref name="value" /> is positive.
    /// </summary>
    /// <typeparam name="T">A comparable numeric type.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="value" /> &gt; 0.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfPositive<T>(
        T value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
        where T : struct, IComparable<T>
    {
        if (value.CompareTo(default) > 0)
            throw new ArgumentOutOfRangeException(paramName, ResourceStrings.Arg_OutOfRange_RequireNonPositive);
    }

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException" /> if the sequence starting at <paramref name="start" /> with
    /// <paramref name="count" /> elements would overflow <see cref="int.MaxValue" />.
    /// </summary>
    /// <param name="start">The starting value of the sequence.</param>
    /// <param name="count">The number of values in the sequence.</param>
    /// <param name="paramName">The name of the count parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <c>start + count - 1</c> would exceed <see cref="int.MaxValue" />.
    /// </exception>
    /// <remarks>
    /// Prevents arithmetic overflow when generating <see cref="int" />-based numeric sequences.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfSequenceRangeOverflows(
        int start, int count,
        [CallerArgumentExpression(nameof(count))] string? paramName = null)
    {
        if (count > 0 && start > int.MaxValue - (count - 1))
            throw new ArgumentOutOfRangeException(
                paramName,
                string.Format(CultureInfo.CurrentCulture, s_argOutOfRangeSequenceRangeOverflow, nameof(Int32)));
    }

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException" /> if the sequence starting at <paramref name="start" /> with
    /// <paramref name="count" /> elements would overflow <see cref="long.MaxValue" />.
    /// </summary>
    /// <param name="start">The starting value of the sequence.</param>
    /// <param name="count">The number of values in the sequence.</param>
    /// <param name="paramName">The name of the count parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <c>start + count - 1</c> would exceed <see cref="long.MaxValue" />.
    /// </exception>
    /// <remarks>
    /// Prevents arithmetic overflow when generating <see cref="long" />-based numeric sequences.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfSequenceRangeOverflows(
        long start, int count,
        [CallerArgumentExpression(nameof(count))] string? paramName = null)
    {
        if (count > 0 && start > long.MaxValue - (count - 1))
            throw new ArgumentOutOfRangeException(
                paramName,
                string.Format(CultureInfo.CurrentCulture, s_argOutOfRangeSequenceRangeOverflow, nameof(Int64)));
    }

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException" /> if <paramref name="value" /> equals zero.
    /// </summary>
    /// <typeparam name="T">A type that implements <see cref="IEquatable{T}" />.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="value" /> equals the default value of <typeparamref name="T" />.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfZero<T>(
        T value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
        where T : struct, IEquatable<T>
    {
        if (value.Equals(default))
            throw new ArgumentOutOfRangeException(paramName, ResourceStrings.Arg_OutOfRange_RequireNonZero);
    }

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException" /> if <paramref name="value" /> is zero or negative.
    /// </summary>
    /// <typeparam name="T">A comparable numeric type.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="value" /> &lt;= 0.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfZeroOrNegative<T>(
        T value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
        where T : struct, IComparable<T>
    {
        if (value.CompareTo(default) <= 0)
            throw new ArgumentOutOfRangeException(paramName, ResourceStrings.Arg_OutOfRange_RequirePositive);
    }

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException" /> if <paramref name="value" /> is zero or positive.
    /// </summary>
    /// <typeparam name="T">A comparable numeric type.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="value" /> &gt;= 0.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfZeroOrPositive<T>(
        T value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
        where T : struct, IComparable<T>
    {
        if (value.CompareTo(default) >= 0)
            throw new ArgumentOutOfRangeException(paramName, ResourceStrings.Arg_OutOfRange_RequireNegative);
    }

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException" /> if <paramref name="value" /> is <see cref="double.NaN" />,
    /// <see cref="double.PositiveInfinity" />, or <see cref="double.NegativeInfinity" />.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="value" /> is not a finite number.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNotFinite(
        double value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (!double.IsFinite(value))
            throw new ArgumentOutOfRangeException(paramName, value, ResourceStrings.Arg_OutOfRange_NotFinite);
    }

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException" /> if <paramref name="value" /> is <see cref="float.NaN" />,
    /// <see cref="float.PositiveInfinity" />, or <see cref="float.NegativeInfinity" />.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="value" /> is not a finite number.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNotFinite(
        float value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (!float.IsFinite(value))
            throw new ArgumentOutOfRangeException(paramName, value, ResourceStrings.Arg_OutOfRange_NotFinite);
    }
}

#endif
