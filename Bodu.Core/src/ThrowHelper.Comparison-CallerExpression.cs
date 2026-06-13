// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelper.Comparison-CallerExpression.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

#if !NETSTANDARD2_0_OR_GREATER
#pragma warning disable SA1117 // Parameters should be on same line or separate lines
#pragma warning disable IDE0011 // Add braces

using System.Globalization;
using System.Runtime.CompilerServices;

namespace Bodu;

public static partial class ThrowHelper
{
    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException" /> if <paramref name="value" /> is greater than
    /// <paramref name="max" />.
    /// </summary>
    /// <typeparam name="T">A comparable type.</typeparam>
    /// <param name="value">The value to compare.</param>
    /// <param name="max">The inclusive upper bound.</param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="value" /> &gt; <paramref name="max" />.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfGreaterThan<T>(
        T value, T max,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
        where T : IComparable<T>
    {
        if (value.CompareTo(max) > 0)
            throw new ArgumentOutOfRangeException(
                paramName,
                string.Format(CultureInfo.CurrentCulture, ResourceStrings.Arg_OutOfRange_RequireLessThanOrEqual, max));
    }

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException" /> if <paramref name="value" /> is greater than or equal to
    /// <paramref name="max" />.
    /// </summary>
    /// <typeparam name="T">A comparable type.</typeparam>
    /// <param name="value">The value to compare.</param>
    /// <param name="max">The exclusive upper bound.</param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="value" /> &gt;= <paramref name="max" />.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfGreaterThanOrEqual<T>(
        T value, T max,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
        where T : IComparable<T>
    {
        if (value.CompareTo(max) >= 0)
            throw new ArgumentOutOfRangeException(
                paramName,
                string.Format(CultureInfo.CurrentCulture, ResourceStrings.Arg_OutOfRange_RequireLessThan, max));
    }

    /// <summary>
    /// Throws an <see cref="ArgumentException" /> if <paramref name="value" /> is greater than or equal to
    /// <paramref name="other" />.
    /// </summary>
    /// <typeparam name="T">A comparable type.</typeparam>
    /// <param name="value">The value being validated.</param>
    /// <param name="other">The comparison reference value.</param>
    /// <param name="paramName">The name of the value parameter. Supplied automatically by the compiler.</param>
    /// <param name="otherName">The name of the comparison parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="value" /> &gt;= <paramref name="other" />.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfGreaterThanOrEqualOther<T>(
        T value, T other,
        [CallerArgumentExpression(nameof(value))] string? paramName = null,
        [CallerArgumentExpression(nameof(other))] string? otherName = null)
        where T : IComparable<T>
    {
        if (value.CompareTo(other) >= 0)
            throw new ArgumentException(
                string.Format(CultureInfo.CurrentCulture, ResourceStrings.Arg_Invalid_GreaterThanOrEqualOtherParameter, otherName),
                paramName);
    }

    /// <summary>
    /// Throws an <see cref="ArgumentException" /> if <paramref name="value" /> is greater than
    /// <paramref name="other" />.
    /// </summary>
    /// <typeparam name="T">A comparable type.</typeparam>
    /// <param name="value">The value being validated.</param>
    /// <param name="other">The comparison reference value.</param>
    /// <param name="paramName">The name of the value parameter. Supplied automatically by the compiler.</param>
    /// <param name="otherName">The name of the comparison parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="value" /> &gt; <paramref name="other" />.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfGreaterThanOther<T>(
        T value, T other,
        [CallerArgumentExpression(nameof(value))] string? paramName = null,
        [CallerArgumentExpression(nameof(other))] string? otherName = null)
        where T : IComparable<T>
    {
        if (value.CompareTo(other) > 0)
            throw new ArgumentException(
                string.Format(CultureInfo.CurrentCulture, ResourceStrings.Arg_Invalid_GreaterThanOtherParameter, otherName),
                paramName);
    }

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException" /> if <paramref name="value" /> is less than
    /// <paramref name="min" />.
    /// </summary>
    /// <typeparam name="T">A comparable type.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="min">The inclusive lower bound.</param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="value" /> &lt; <paramref name="min" />.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfLessThan<T>(
        T value, T min,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
        where T : IComparable<T>
    {
        if (value.CompareTo(min) < 0)
            throw new ArgumentOutOfRangeException(
                paramName,
                string.Format(CultureInfo.CurrentCulture, ResourceStrings.Arg_OutOfRange_RequireGreaterThanOrEqual, min));
    }

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException" /> if the nullable <paramref name="value" /> is less than
    /// <paramref name="min" />. Optionally throws <see cref="ArgumentNullException" /> if <paramref name="value" /> is
    /// <see langword="null" />.
    /// </summary>
    /// <typeparam name="T">A comparable value type.</typeparam>
    /// <param name="value">The nullable value to validate.</param>
    /// <param name="min">The inclusive lower bound.</param>
    /// <param name="throwIfNull">
    /// When <see langword="true" />, throws <see cref="ArgumentNullException" /> if <paramref name="value" /> is
    /// <see langword="null" />. When <see langword="false" />, a <see langword="null" /> value passes validation.
    /// </param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> is <see langword="null" /> and <paramref name="throwIfNull" /> is
    /// <see langword="true" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="value" /> is non-null and less than <paramref name="min" />.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfLessThan<T>(
        T? value, T min, bool throwIfNull = false,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
        where T : struct, IComparable<T>
    {
        if (value is null)
        {
            if (throwIfNull)
                throw new ArgumentNullException(paramName);
        }
        else if (value.Value.CompareTo(min) < 0)
        {
            throw new ArgumentOutOfRangeException(
                paramName,
                string.Format(CultureInfo.CurrentCulture, ResourceStrings.Arg_OutOfRange_RequireGreaterThanOrEqual, min));
        }
    }

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException" /> if <paramref name="value" /> is less than or equal to
    /// <paramref name="min" />.
    /// </summary>
    /// <typeparam name="T">A comparable type.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="min">The exclusive lower bound.</param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="value" /> &lt;= <paramref name="min" />.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfLessThanOrEqual<T>(
        T value, T min,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
        where T : IComparable<T>
    {
        if (value.CompareTo(min) <= 0)
            throw new ArgumentOutOfRangeException(
                paramName,
                string.Format(CultureInfo.CurrentCulture, ResourceStrings.Arg_OutOfRange_RequireGreaterThan, min));
    }

    /// <summary>
    /// Throws an <see cref="ArgumentException" /> if <paramref name="value" /> is less than or equal to
    /// <paramref name="other" />.
    /// </summary>
    /// <typeparam name="T">A comparable type.</typeparam>
    /// <param name="value">The value being validated.</param>
    /// <param name="other">The comparison reference value.</param>
    /// <param name="paramName">The name of the value parameter. Supplied automatically by the compiler.</param>
    /// <param name="otherName">The name of the comparison parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="value" /> &lt;= <paramref name="other" />.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfLessThanOrEqualOther<T>(
        T value, T other,
        [CallerArgumentExpression(nameof(value))] string? paramName = null,
        [CallerArgumentExpression(nameof(other))] string? otherName = null)
        where T : IComparable<T>
    {
        if (value.CompareTo(other) <= 0)
            throw new ArgumentException(
                string.Format(CultureInfo.CurrentCulture, ResourceStrings.Arg_Invalid_LessThanOrEqualOtherParameter, otherName),
                paramName);
    }

    /// <summary>
    /// Throws an <see cref="ArgumentException" /> if <paramref name="value" /> is less than <paramref name="other" />.
    /// </summary>
    /// <typeparam name="T">A comparable type.</typeparam>
    /// <param name="value">The value being validated.</param>
    /// <param name="other">The comparison reference value.</param>
    /// <param name="paramName">The name of the value parameter. Supplied automatically by the compiler.</param>
    /// <param name="otherName">The name of the comparison parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="value" /> &lt; <paramref name="other" />.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfLessThanOther<T>(
        T value, T other,
        [CallerArgumentExpression(nameof(value))] string? paramName = null,
        [CallerArgumentExpression(nameof(other))] string? otherName = null)
        where T : IComparable<T>
    {
        if (value.CompareTo(other) < 0)
            throw new ArgumentException(
                string.Format(CultureInfo.CurrentCulture, ResourceStrings.Arg_Invalid_LessThanOtherParameter, otherName),
                paramName);
    }

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException" /> if <paramref name="value" /> is not within the specified
    /// range.
    /// </summary>
    /// <typeparam name="T">A type that implements <see cref="IComparable{T}" />.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="min">The lower bound of the range.</param>
    /// <param name="max">The upper bound of the range.</param>
    /// <param name="inclusive">
    /// When <see langword="true" />, the bounds are inclusive; when <see langword="false" />, they are exclusive.
    /// </param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="value" /> falls outside the range defined by <paramref name="min" /> and
    /// <paramref name="max" />.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfOutOfRange<T>(
        T value, T min, T max, bool inclusive = true,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
        where T : IComparable<T>
    {
        if (inclusive)
        {
            if (value.CompareTo(min) < 0 || value.CompareTo(max) > 0)
                throw new ArgumentOutOfRangeException(
                    paramName,
                    string.Format(CultureInfo.CurrentCulture, ResourceStrings.Arg_OutOfRange_RequireBetweenInclusive, min, max));
        }
        else
        {
            if (value.CompareTo(min) <= 0 || value.CompareTo(max) >= 0)
                throw new ArgumentOutOfRangeException(
                    paramName,
                    string.Format(CultureInfo.CurrentCulture, ResourceStrings.Arg_OutOfRange_RequireBetweenExclusive, min, max));
        }
    }
}

#endif
