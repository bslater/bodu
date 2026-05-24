// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelper.Span.NetStandard.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
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
    /// Throws an <see cref="ArgumentNullException" /> if either array is <see langword="null" />, or an
    /// <see cref="ArgumentException" /> if <paramref name="destination" /> is shorter than
    /// <paramref name="source" />.
    /// </summary>
    /// <typeparam name="TSource">The element type of the source array.</typeparam>
    /// <typeparam name="TDestination">The element type of the destination array.</typeparam>
    /// <param name="source">The source array. Must not be <see langword="null" />.</param>
    /// <param name="destination">The destination array. Must not be <see langword="null" />.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source" /> or <paramref name="destination" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <c>destination.Length &lt; source.Length</c>.
    /// </exception>
    /// <remarks>Useful for validating array-to-array operations such as copying or endian-swapping.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfDestinationTooSmall<TSource, TDestination>(
        TSource[] source,
        TDestination[] destination)
    {
        if (source is null)
            throw new ArgumentNullException(nameof(source));

        if (destination is null)
            throw new ArgumentNullException(nameof(destination));

        if (destination.Length < source.Length)
            throw new ArgumentException(
                string.Format(CultureInfo.CurrentCulture, ResourceStrings.Arg_Invalid_DestinationTooSmall, "array", destination.Length),
                nameof(destination));
    }

#if NETSTANDARD2_1_OR_GREATER

    /// <summary>
    /// Throws an exception if the span does not have exactly <paramref name="expectedLength" /> elements.
    /// </summary>
    /// <typeparam name="T">The element type of the span.</typeparam>
    /// <param name="span">The read-only span to validate.</param>
    /// <param name="expectedLength">The exact number of elements that <paramref name="span" /> must contain.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <c>span.Length</c> does not equal <paramref name="expectedLength" />.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfSpanLengthIsNotEqualTo<T>(ReadOnlySpan<T> span, int expectedLength)
    {
        if (span.Length != expectedLength)
            throw new ArgumentException(
                string.Format(CultureInfo.CurrentCulture, ResourceStrings.Arg_Invalid_SpanLength, expectedLength),
                nameof(span));
    }

    /// <summary>
    /// Throws an exception if the specified <paramref name="span" /> has fewer than
    /// <paramref name="minimumLength" /> elements.
    /// </summary>
    /// <typeparam name="T">The element type of the span.</typeparam>
    /// <param name="span">The read-only span to validate.</param>
    /// <param name="minimumLength">
    /// The minimum number of elements that <paramref name="span" /> must contain.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when <c>span.Length</c> is less than <paramref name="minimumLength" />.
    /// </exception>
    /// <remarks>
    /// <see cref="System.ReadOnlySpan{T}" /> is a value type and cannot be <see langword="null" />; no null
    /// guard is required or possible. Use this overload when the caller may supply a larger span than
    /// required and the excess elements are simply ignored. When the length must be exact, use
    /// <see cref="ThrowIfSpanLengthIsNotEqualTo{T}(ReadOnlySpan{T}, int)" /> instead.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfSpanLengthIsInsufficient<T>(ReadOnlySpan<T> span, int minimumLength)
    {
        if (span.Length < minimumLength)
            throw new ArgumentException(
                string.Format(CultureInfo.CurrentCulture, ResourceStrings.Arg_Invalid_SpanTooShort, minimumLength),
                nameof(span));
    }

    /// <summary>
    /// Throws an exception if the span does not have exactly <paramref name="expectedLength" /> elements.
    /// </summary>
    /// <typeparam name="T">The element type of the span.</typeparam>
    /// <param name="span">The span to validate.</param>
    /// <param name="expectedLength">The exact number of elements that <paramref name="span" /> must contain.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <c>span.Length</c> does not equal <paramref name="expectedLength" />.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfSpanLengthIsNotEqualTo<T>(Span<T> span, int expectedLength)
    {
        if (span.Length != expectedLength)
            throw new ArgumentException(
                string.Format(CultureInfo.CurrentCulture, ResourceStrings.Arg_Invalid_SpanLength, expectedLength),
                nameof(span));
    }

    /// <summary>
    /// Throws an exception if the specified <paramref name="span" /> has fewer than
    /// <paramref name="minimumLength" /> elements.
    /// </summary>
    /// <typeparam name="T">The element type of the span.</typeparam>
    /// <param name="span">The span to validate.</param>
    /// <param name="minimumLength">
    /// The minimum number of elements that <paramref name="span" /> must contain.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when <c>span.Length</c> is less than <paramref name="minimumLength" />.
    /// </exception>
    /// <remarks>
    /// <see cref="System.Span{T}" /> is a value type and cannot be <see langword="null" />; no null guard
    /// is required or possible. Use this overload when the caller may supply a larger span than required
    /// and the excess elements are simply ignored. When the length must be exact, use
    /// <see cref="ThrowIfSpanLengthIsNotEqualTo{T}(Span{T}, int)" /> instead.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfSpanLengthIsInsufficient<T>(Span<T> span, int minimumLength)
    {
        if (span.Length < minimumLength)
            throw new ArgumentException(
                string.Format(CultureInfo.CurrentCulture, ResourceStrings.Arg_Invalid_SpanTooShort, minimumLength),
                nameof(span));
    }

    /// <summary>
    /// Throws an <see cref="ArgumentException" /> if <paramref name="destination" /> is shorter than
    /// <paramref name="source" />.
    /// </summary>
    /// <typeparam name="TSource">The element type of the source span.</typeparam>
    /// <typeparam name="TDestination">The element type of the destination span.</typeparam>
    /// <param name="source">The source span.</param>
    /// <param name="destination">The destination span.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <c>destination.Length &lt; source.Length</c>.
    /// </exception>
    /// <remarks>Useful for validating buffer-to-buffer operations such as copying or endian-swapping.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfDestinationSpanTooSmall<TSource, TDestination>(
        ReadOnlySpan<TSource> source,
        Span<TDestination> destination)
    {
        if (destination.Length < source.Length)
            throw new ArgumentException(
                string.Format(CultureInfo.CurrentCulture, ResourceStrings.Arg_Invalid_DestinationTooSmall, "span", destination.Length),
                nameof(destination));
    }

    /// <summary>
    /// Throws an <see cref="ArgumentException" /> if the remaining elements of <paramref name="span" /> from
    /// <paramref name="index" /> are fewer than <paramref name="requiredLength" />.
    /// </summary>
    /// <typeparam name="T">The element type of the span.</typeparam>
    /// <param name="span">The span to validate.</param>
    /// <param name="index">The position from which remaining length is measured.</param>
    /// <param name="requiredLength">The minimum number of elements required from <paramref name="index" />.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <c>span.Length - index &lt; requiredLength</c>.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfSpanLengthIsInsufficient<T>(ReadOnlySpan<T> span, int index, int requiredLength)
    {
        if (span.Length - index < requiredLength)
            throw new ArgumentException(
                string.Format(CultureInfo.CurrentCulture, ResourceStrings.Arg_Invalid_SpanTooShort, requiredLength),
                nameof(span));
    }

    /// <summary>
    /// Throws an <see cref="ArgumentException" /> if the remaining elements of <paramref name="span" /> from
    /// <paramref name="index" /> are fewer than <paramref name="requiredLength" />.
    /// </summary>
    /// <typeparam name="T">The element type of the span.</typeparam>
    /// <param name="span">The span to validate.</param>
    /// <param name="index">The position from which remaining length is measured.</param>
    /// <param name="requiredLength">The minimum number of elements required from <paramref name="index" />.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <c>span.Length - index &lt; requiredLength</c>.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfSpanLengthIsInsufficient<T>(Span<T> span, int index, int requiredLength)
    {
        if (span.Length - index < requiredLength)
            throw new ArgumentException(
                string.Format(CultureInfo.CurrentCulture, ResourceStrings.Arg_Invalid_SpanTooShort, requiredLength),
                nameof(span));
    }

    /// <summary>
    /// Throws an <see cref="ArgumentException" /> if the span length is not a positive multiple of
    /// <paramref name="divisor" />.
    /// </summary>
    /// <typeparam name="T">The element type of the span.</typeparam>
    /// <param name="span">The span to validate.</param>
    /// <param name="divisor">The required positive divisor.</param>
    /// <param name="throwIfZero">
    /// When <see langword="true" />, an empty span is treated as invalid. When <see langword="false" />, an empty
    /// span passes validation.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="span" /> is empty (and <paramref name="throwIfZero" /> is
    /// <see langword="true" />), or when <c>span.Length % divisor != 0</c>.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfSpanLengthNotPositiveMultipleOf<T>(
        ReadOnlySpan<T> span, int divisor, bool throwIfZero = true)
    {
        ThrowIfZeroOrNegative(divisor);

        if ((throwIfZero && span.Length == 0) || span.Length % divisor != 0)
            throw new ArgumentException(
                string.Format(CultureInfo.CurrentCulture, ResourceStrings.Arg_Invalid_SpanLengthMultipleOf, divisor),
                nameof(span));
    }

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException" /> if the span length is not between
    /// <paramref name="minLength" /> and <paramref name="maxLength" /> (inclusive).
    /// </summary>
    /// <typeparam name="T">The element type of the span.</typeparam>
    /// <param name="span">The span to validate.</param>
    /// <param name="minLength">The minimum permitted length, inclusive. Must be zero or greater.</param>
    /// <param name="maxLength">The maximum permitted length, inclusive. Must be greater than or equal to
    /// <paramref name="minLength" />.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <c>span.Length &lt; minLength</c> or <c>span.Length &gt; maxLength</c>.
    /// </exception>
    /// <remarks>
    /// <see cref="System.Span{T}" /> is a value type and cannot be <see langword="null" />; no null guard is
    /// required or possible.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfSpanLengthOutOfRange<T>(ReadOnlySpan<T> span, int minLength, int maxLength)
    {
        if (span.Length < minLength || span.Length > maxLength)
            throw new ArgumentOutOfRangeException(
                nameof(span),
                string.Format(CultureInfo.CurrentCulture, ResourceStrings.Arg_OutOfRange_RequireBetweenInclusive, minLength, maxLength));
    }

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException" /> if the span length is not between
    /// <paramref name="minLength" /> and <paramref name="maxLength" /> (inclusive).
    /// </summary>
    /// <typeparam name="T">The element type of the span.</typeparam>
    /// <param name="span">The span to validate.</param>
    /// <param name="minLength">The minimum permitted length, inclusive. Must be zero or greater.</param>
    /// <param name="maxLength">The maximum permitted length, inclusive. Must be greater than or equal to
    /// <paramref name="minLength" />.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <c>span.Length &lt; minLength</c> or <c>span.Length &gt; maxLength</c>.
    /// </exception>
    /// <remarks>
    /// <see cref="System.Span{T}" /> is a value type and cannot be <see langword="null" />; no null guard is
    /// required or possible.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfSpanLengthOutOfRange<T>(Span<T> span, int minLength, int maxLength)
    {
        if (span.Length < minLength || span.Length > maxLength)
            throw new ArgumentOutOfRangeException(
                nameof(span),
                string.Format(CultureInfo.CurrentCulture, ResourceStrings.Arg_OutOfRange_RequireBetweenInclusive, minLength, maxLength));
    }

    /// <summary>
    /// Throws an <see cref="ArgumentException" /> if the span length is not a positive multiple of
    /// <paramref name="divisor" />.
    /// </summary>
    /// <typeparam name="T">The element type of the span.</typeparam>
    /// <param name="span">The span to validate.</param>
    /// <param name="divisor">The required positive divisor.</param>
    /// <param name="throwIfZero">
    /// When <see langword="true" />, an empty span is treated as invalid. When <see langword="false" />, an empty
    /// span passes validation.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="span" /> is empty (and <paramref name="throwIfZero" /> is
    /// <see langword="true" />), or when <c>span.Length % divisor != 0</c>.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfSpanLengthNotPositiveMultipleOf<T>(
        Span<T> span, int divisor, bool throwIfZero = true)
    {
        ThrowIfZeroOrNegative(divisor);

        if ((throwIfZero && span.Length == 0) || span.Length % divisor != 0)
            throw new ArgumentException(
                string.Format(CultureInfo.CurrentCulture, ResourceStrings.Arg_Invalid_SpanLengthMultipleOf, divisor),
                nameof(span));
    }

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException" /> if <paramref name="index" /> is outside the valid
    /// element range of <paramref name="span" />.
    /// </summary>
    /// <typeparam name="T">The element type of the span.</typeparam>
    /// <param name="index">The index to validate.</param>
    /// <param name="span">The span against which to validate the index.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="index" /> &lt; 0 or &gt;= <c>span.Length</c>.
    /// </exception>
    /// <remarks>
    /// Uses an unsigned cast to collapse the two-sided bounds check into a single comparison.
    /// <see cref="System.ReadOnlySpan{T}" /> is a value type and cannot be <see langword="null" />; no null guard
    /// is required or possible.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfIndexOutOfRange<T>(int index, ReadOnlySpan<T> span)
    {
        if ((uint)index >= (uint)span.Length)
            throw new ArgumentOutOfRangeException(
                nameof(index),
                string.Format(CultureInfo.CurrentCulture, ResourceStrings.Arg_OutOfRange_IndexValidRange, span.Length));
    }

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException" /> if <paramref name="index" /> is outside the valid
    /// element range of <paramref name="span" />.
    /// </summary>
    /// <typeparam name="T">The element type of the span.</typeparam>
    /// <param name="index">The index to validate.</param>
    /// <param name="span">The span against which to validate the index.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="index" /> &lt; 0 or &gt;= <c>span.Length</c>.
    /// </exception>
    /// <remarks>
    /// Uses an unsigned cast to collapse the two-sided bounds check into a single comparison.
    /// <see cref="System.Span{T}" /> is a value type and cannot be <see langword="null" />; no null guard is
    /// required or possible.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfIndexOutOfRange<T>(int index, Span<T> span)
    {
        if ((uint)index >= (uint)span.Length)
            throw new ArgumentOutOfRangeException(
                nameof(index),
                string.Format(CultureInfo.CurrentCulture, ResourceStrings.Arg_OutOfRange_IndexValidRange, span.Length));
    }
}

#endif
#endif
