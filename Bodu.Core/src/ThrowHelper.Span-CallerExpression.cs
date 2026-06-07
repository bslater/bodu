// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelper.Span-CallerExpression.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

#if !NETSTANDARD2_0_OR_GREATER
#pragma warning disable SA1117 // Parameters should be on same line or separate lines
#pragma warning disable IDE0011 // Add braces

using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using Bodu.Extensions;

namespace Bodu;

public static partial class ThrowHelper
{
    /// <summary>
    /// Cached parsed format for <see cref="ResourceStrings.Arg_Invalid_DestinationTooSmall" />.
    /// </summary>
    private static readonly CompositeFormat s_argInvalidDestinationTooSmall =
        CompositeFormat.Parse(ResourceStrings.Arg_Invalid_DestinationTooSmall);

    /// <summary>
    /// Cached parsed format for <see cref="ResourceStrings.Arg_Invalid_SpanLength" />.
    /// </summary>
    private static readonly CompositeFormat s_argInvalidSpanLength =
        CompositeFormat.Parse(ResourceStrings.Arg_Invalid_SpanLength);

    /// <summary>
    /// Cached parsed format for <see cref="ResourceStrings.Arg_Invalid_SpanLengthMultipleOf" />.
    /// </summary>
    private static readonly CompositeFormat s_argInvalidSpanLengthMultipleOf =
        CompositeFormat.Parse(ResourceStrings.Arg_Invalid_SpanLengthMultipleOf);

    /// <summary>
    /// Cached parsed format for <see cref="ResourceStrings.Arg_Invalid_SpanTooShort" />.
    /// </summary>
    private static readonly CompositeFormat s_argInvalidSpanTooShort =
        CompositeFormat.Parse(ResourceStrings.Arg_Invalid_SpanTooShort);

    /// <summary>
    /// Throws an exception if the span does not have exactly <paramref name="expectedLength" /> elements.
    /// </summary>
    /// <typeparam name="T">The element type of the span.</typeparam>
    /// <param name="span">The span to validate.</param>
    /// <param name="expectedLength">The exact number of elements that <paramref name="span" /> must contain.</param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <c>span.Length</c> does not equal <paramref name="expectedLength" />.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfSpanLengthIsNotEqualTo<T>(
        Span<T> span, int expectedLength,
        [CallerArgumentExpression(nameof(span))] string? paramName = null)
    {
        if (span.Length != expectedLength)
            throw new ArgumentException(
                string.Format(CultureInfo.CurrentCulture, s_argInvalidSpanLength, expectedLength),
                paramName);
    }

    /// <summary>
    /// Throws an exception if the specified <paramref name="span" /> has fewer than <paramref name="minimumLength" />
    /// elements.
    /// </summary>
    /// <typeparam name="T">The element type of the span.</typeparam>
    /// <param name="span">The span to validate.</param>
    /// <param name="minimumLength">The minimum number of elements that <paramref name="span" /> must contain.</param>
    /// <param name="paramName">
    /// The name of the span parameter. Supplied automatically by the compiler via
    /// <see cref="CallerArgumentExpressionAttribute" />.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when <c>span.Length</c> is less than <paramref name="minimumLength" />.
    /// </exception>
    /// <remarks>
    /// <see cref="System.Span{T}" /> is a value type and cannot be <see langword="null" />; no null guard is required
    /// or possible. Use this overload when the caller may supply a larger span than required and the excess elements
    /// are simply ignored — for example, a buffer that must hold at least a full cipher block but may be larger. When
    /// the length must be exact, use <see cref="ThrowIfSpanLengthIsNotEqualTo{T}(Span{T}, int, string)" /> instead.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfSpanLengthIsInsufficient<T>(
        Span<T> span, int minimumLength,
        [CallerArgumentExpression(nameof(span))] string? paramName = null)
    {
        if (span.Length < minimumLength)
            throw new ArgumentException(
                string.Format(CultureInfo.CurrentCulture, s_argInvalidSpanTooShort, minimumLength),
                paramName);
    }

    /// <summary>
    /// Throws an exception if the span does not have exactly <paramref name="expectedLength" /> elements.
    /// </summary>
    /// <typeparam name="T">The element type of the span.</typeparam>
    /// <param name="span">The span to validate.</param>
    /// <param name="expectedLength">The exact number of elements that <paramref name="span" /> must contain.</param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <c>span.Length</c> does not equal <paramref name="expectedLength" />.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfSpanLengthIsNotEqualTo<T>(
        ReadOnlySpan<T> span, int expectedLength,
        [CallerArgumentExpression(nameof(span))] string? paramName = null)
    {
        if (span.Length != expectedLength)
            throw new ArgumentException(
                string.Format(CultureInfo.CurrentCulture, s_argInvalidSpanLength, expectedLength),
                paramName);
    }

    /// <summary>
    /// Throws an exception if the specified <paramref name="span" /> has fewer than <paramref name="minimumLength" />
    /// elements.
    /// </summary>
    /// <typeparam name="T">The element type of the span.</typeparam>
    /// <param name="span">The read-only span to validate.</param>
    /// <param name="minimumLength">The minimum number of elements that <paramref name="span" /> must contain.</param>
    /// <param name="paramName">
    /// The name of the span parameter. Supplied automatically by the compiler via
    /// <see cref="CallerArgumentExpressionAttribute" />.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when <c>span.Length</c> is less than <paramref name="minimumLength" />.
    /// </exception>
    /// <remarks>
    /// <see cref="System.ReadOnlySpan{T}" /> is a value type and cannot be <see langword="null" />; no null guard is
    /// required or possible. Use this overload when the caller may supply a larger span than required and the excess
    /// elements are simply ignored — for example, a buffer that must hold at least a full cipher block but may be
    /// larger. When the length must be exact, use
    /// <see cref="ThrowIfSpanLengthIsNotEqualTo{T}(ReadOnlySpan{T}, int, string)" /> instead.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfSpanLengthIsInsufficient<T>(
        ReadOnlySpan<T> span, int minimumLength,
        [CallerArgumentExpression(nameof(span))] string? paramName = null)
    {
        if (span.Length < minimumLength)
            throw new ArgumentException(
                string.Format(CultureInfo.CurrentCulture, s_argInvalidSpanTooShort, minimumLength),
                paramName);
    }

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException" /> if the span length is not between
    /// <paramref name="minLength" /> and <paramref name="maxLength" /> (inclusive).
    /// </summary>
    /// <typeparam name="T">The element type of the span.</typeparam>
    /// <param name="span">The span to validate.</param>
    /// <param name="minLength">The minimum permitted length, inclusive. Must be zero or greater.</param>
    /// <param name="maxLength">
    /// The maximum permitted length, inclusive. Must be greater than or equal to <paramref name="minLength" />.
    /// </param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <c>span.Length &lt; minLength</c> or <c>span.Length &gt; maxLength</c>.
    /// </exception>
    /// <remarks>
    /// <see cref="System.ReadOnlySpan{T}" /> is a value type and cannot be <see langword="null" />; no null guard is
    /// required or possible.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfSpanLengthOutOfRange<T>(
        ReadOnlySpan<T> span, int minLength, int maxLength,
        [CallerArgumentExpression(nameof(span))] string? paramName = null)
    {
        if (span.Length < minLength || span.Length > maxLength)
            throw new ArgumentOutOfRangeException(
                paramName,
                string.Format(CultureInfo.CurrentCulture, s_argOutOfRangeRequireBetweenInclusive, minLength, maxLength));
    }

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException" /> if the span length is not between
    /// <paramref name="minLength" /> and <paramref name="maxLength" /> (inclusive).
    /// </summary>
    /// <typeparam name="T">The element type of the span.</typeparam>
    /// <param name="span">The span to validate.</param>
    /// <param name="minLength">The minimum permitted length, inclusive. Must be zero or greater.</param>
    /// <param name="maxLength">
    /// The maximum permitted length, inclusive. Must be greater than or equal to <paramref name="minLength" />.
    /// </param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <c>span.Length &lt; minLength</c> or <c>span.Length &gt; maxLength</c>.
    /// </exception>
    /// <remarks>
    /// <see cref="System.Span{T}" /> is a value type and cannot be <see langword="null" />; no null guard is required
    /// or possible.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfSpanLengthOutOfRange<T>(
        Span<T> span, int minLength, int maxLength,
        [CallerArgumentExpression(nameof(span))] string? paramName = null)
    {
        if (span.Length < minLength || span.Length > maxLength)
            throw new ArgumentOutOfRangeException(
                paramName,
                string.Format(CultureInfo.CurrentCulture, s_argOutOfRangeRequireBetweenInclusive, minLength, maxLength));
    }

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException" /> if <paramref name="offset" /> or <paramref name="count" />
    /// is out of range, or an <see cref="ArgumentException" /> if the segment they define exceeds the bounds of
    /// <paramref name="span" /> .
    /// </summary>
    /// <typeparam name="T">The type of elements in the span.</typeparam>
    /// <param name="span">The span to validate.</param>
    /// <param name="offset">The zero-based starting index within the span.</param>
    /// <param name="count">The number of elements to access from <paramref name="offset" />.</param>
    /// <param name="paramSpanName">
    /// The name of the span parameter. Supplied automatically by the compiler via
    /// <see cref="CallerArgumentExpressionAttribute" />.
    /// </param>
    /// <param name="paramIndexName">
    /// The name of the index parameter. Supplied automatically by the compiler via
    /// <see cref="CallerArgumentExpressionAttribute" />.
    /// </param>
    /// <param name="paramCountName">
    /// The name of the count parameter. Supplied automatically by the compiler via
    /// <see cref="CallerArgumentExpressionAttribute" />.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="offset" /> or <paramref name="count" /> is negative or exceeds
    /// <see cref="ReadOnlySpan{T}.Length" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <c>index + count</c> exceeds <see cref="ReadOnlySpan{T}.Length" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Unlike the array equivalent (<c>ThrowIfArrayOffsetOrCountInvalid</c>), this overload does not check for
    /// <see langword="null" /> and does not throw <see cref="ArgumentNullException" /> . <see cref="Span{T}" /> is a
    /// value type and can never be <see langword="null" />; a default <see cref="Span{T}" /> is equivalent to an empty
    /// span with <see cref="ReadOnlySpan{T}.Length" /> of zero.
    /// </para>
    /// <para>
    /// Implicitly converts <paramref name="span" /> to <see cref="ReadOnlySpan{T}" /> and delegates to
    /// <see cref="ThrowIfSpanOffsetOrCountInvalid{T}(ReadOnlySpan{T}, int, int, string, string, string)" />, which is
    /// the canonical implementation. <paramref name="paramSpanName" />, <paramref name="paramIndexName" />, and
    /// <paramref name="paramCountName" /> are forwarded explicitly to preserve the call-site argument expressions in
    /// any exception messages.
    /// </para>
    /// </remarks>
    public static void ThrowIfSpanOffsetOrCountInvalid<T>(
        Span<T> span, int offset, int count,
        [CallerArgumentExpression(nameof(span))] string? paramSpanName = null,
        [CallerArgumentExpression(nameof(offset))] string? paramIndexName = null,
        [CallerArgumentExpression(nameof(count))] string? paramCountName = null)
        => ThrowIfSpanOffsetOrCountInvalid(
            (ReadOnlySpan<T>)span, offset, count,
            paramSpanName, paramIndexName, paramCountName);

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException" /> if <paramref name="offset" /> or <paramref name="count" />
    /// is out of range, or an <see cref="ArgumentException" /> if the segment they define exceeds the bounds of
    /// <paramref name="span" /> .
    /// </summary>
    /// <typeparam name="T">The type of elements in the span.</typeparam>
    /// <param name="span">The read-only span to validate.</param>
    /// <param name="offset">The zero-based starting index within the span.</param>
    /// <param name="count">The number of elements to access from <paramref name="offset" />.</param>
    /// <param name="paramSpanName">
    /// The name of the span parameter. Supplied automatically by the compiler via
    /// <see cref="CallerArgumentExpressionAttribute" />.
    /// </param>
    /// <param name="paramOffsetName">
    /// The name of the index parameter. Supplied automatically by the compiler via
    /// <see cref="CallerArgumentExpressionAttribute" />.
    /// </param>
    /// <param name="paramCountName">
    /// The name of the count parameter. Supplied automatically by the compiler via
    /// <see cref="CallerArgumentExpressionAttribute" />.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="offset" /> or <paramref name="count" /> is negative or exceeds
    /// <see cref="ReadOnlySpan{T}.Length" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <c>index + count</c> exceeds <see cref="ReadOnlySpan{T}.Length" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This is the canonical implementation. The <see cref="Span{T}" /> overload (
    /// <see cref="ThrowIfSpanOffsetOrCountInvalid{T}(Span{T}, int, int, string, string, string)" /> ) converts its
    /// argument and delegates here, forwarding all <see cref="CallerArgumentExpressionAttribute" /> values explicitly
    /// so that exception messages always reflect the original call-site expressions.
    /// </para>
    /// <para>
    /// Unlike the array equivalent (<c>ThrowIfArrayOffsetOrCountInvalid</c>), this overload does not check for
    /// <see langword="null" /> and does not throw <see cref="ArgumentNullException" /> . <see cref="ReadOnlySpan{T}" />
    /// is a value type and can never be <see langword="null" />; a default <see cref="ReadOnlySpan{T}" /> is equivalent
    /// to an empty span with <see cref="ReadOnlySpan{T}.Length" /> of zero.
    /// </para>
    /// <para>
    /// Although the index and count validations are expressed as two-part conditions (<c>&lt; 0 || &gt; span.Length</c>
    /// ) rather than the single unsigned-cast trick used in <see cref="SpanExtensions" />, the explicit form is
    /// preferred here because this is a guard method whose primary purpose is clarity of intent. The unsigned-cast form
    /// trades readability for a marginal branch reduction that is irrelevant on an error path.
    /// </para>
    /// </remarks>
    public static void ThrowIfSpanOffsetOrCountInvalid<T>(
        ReadOnlySpan<T> span, int offset, int count,
        [CallerArgumentExpression(nameof(span))] string? paramSpanName = null,
        [CallerArgumentExpression(nameof(offset))] string? paramOffsetName = null,
        [CallerArgumentExpression(nameof(count))] string? paramCountName = null)
    {
        if (offset < 0 || offset > span.Length)
            throw new ArgumentOutOfRangeException(
                paramOffsetName,
                string.Format(CultureInfo.CurrentCulture, s_argInvalidArrayOffset, paramOffsetName));

        if (count < 0 || count > span.Length)
            throw new ArgumentOutOfRangeException(
                paramCountName,
                string.Format(CultureInfo.CurrentCulture, s_argInvalidArrayOffset, paramCountName));

        if (count > span.Length - offset)
            throw new ArgumentException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    s_argInvalidArrayOffsetOrLength,
                    paramOffsetName,
                    paramCountName,
                    paramSpanName));
    }

    /// <summary>
    /// Throws an <see cref="ArgumentNullException" /> if either array is <see langword="null" />, or an
    /// <see cref="ArgumentException" /> if <paramref name="destination" /> is shorter than <paramref name="source" />.
    /// </summary>
    /// <typeparam name="TSource">The element type of the source array.</typeparam>
    /// <typeparam name="TDestination">The element type of the destination array.</typeparam>
    /// <param name="source">The source array. Must not be <see langword="null" />.</param>
    /// <param name="destination">The destination array. Must not be <see langword="null" />.</param>
    /// <param name="paramDestinationName">
    /// The name of the destination parameter. Supplied automatically by the compiler.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source" /> or <paramref name="destination" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <c>destination.Length &lt; source.Length</c>.</exception>
    /// <remarks>
    /// Useful for validating array-to-array operations such as copying or endian-swapping.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfDestinationTooSmall<TSource, TDestination>(
        TSource[] source,
        TDestination[] destination,
        [CallerArgumentExpression(nameof(destination))] string? paramDestinationName = null)
    {
        if (source is null)
            throw new ArgumentNullException(nameof(source));

        if (destination is null)
            throw new ArgumentNullException(paramDestinationName);

        if (destination.Length < source.Length)
            throw new ArgumentException(
                string.Format(CultureInfo.CurrentCulture, s_argInvalidDestinationTooSmall, ResourceStrings.BufferKind_Array, destination.Length),
                paramDestinationName);
    }

    /// <summary>
    /// Throws an <see cref="ArgumentException" /> if <paramref name="destination" /> is shorter than
    /// <paramref name="source" />.
    /// </summary>
    /// <typeparam name="TSource">The element type of the source span.</typeparam>
    /// <typeparam name="TDestination">The element type of the destination span.</typeparam>
    /// <param name="source">The source span.</param>
    /// <param name="destination">The destination span.</param>
    /// <param name="paramDestinationName">
    /// The name of the destination parameter. Supplied automatically by the compiler.
    /// </param>
    /// <exception cref="ArgumentException">Thrown when <c>destination.Length &lt; source.Length</c>.</exception>
    /// <remarks>
    /// Useful for validating buffer-to-buffer operations such as copying or endian-swapping.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfDestinationSpanTooSmall<TSource, TDestination>(
        ReadOnlySpan<TSource> source,
        Span<TDestination> destination,
        [CallerArgumentExpression(nameof(destination))] string? paramDestinationName = null)
    {
        if (destination.Length < source.Length)
            throw new ArgumentException(
                string.Format(CultureInfo.CurrentCulture, s_argInvalidDestinationTooSmall, ResourceStrings.BufferKind_Span, destination.Length),
                paramDestinationName);
    }

    /// <summary>
    /// Throws an <see cref="ArgumentException" /> if the remaining elements of <paramref name="span" /> from
    /// <paramref name="index" /> are fewer than <paramref name="requiredLength" />.
    /// </summary>
    /// <typeparam name="T">The element type of the span.</typeparam>
    /// <param name="span">The span to validate.</param>
    /// <param name="index">The position from which remaining length is measured.</param>
    /// <param name="requiredLength">The minimum number of elements required from <paramref name="index" />.</param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentException">Thrown when <c>span.Length - index &lt; requiredLength</c>.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfSpanLengthIsInsufficient<T>(
        ReadOnlySpan<T> span, int index, int requiredLength,
        [CallerArgumentExpression(nameof(span))] string? paramName = null)
    {
        if (span.Length - index < requiredLength)
            throw new ArgumentException(
                string.Format(CultureInfo.CurrentCulture, s_argInvalidSpanTooShort, requiredLength),
                paramName);
    }

    /// <summary>
    /// Throws an <see cref="ArgumentException" /> if the remaining elements of <paramref name="span" /> from
    /// <paramref name="index" /> are fewer than <paramref name="requiredLength" />.
    /// </summary>
    /// <typeparam name="T">The element type of the span.</typeparam>
    /// <param name="span">The span to validate.</param>
    /// <param name="index">The position from which remaining length is measured.</param>
    /// <param name="requiredLength">The minimum number of elements required from <paramref name="index" />.</param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentException">Thrown when <c>span.Length - index &lt; requiredLength</c>.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfSpanLengthIsInsufficient<T>(
        Span<T> span, int index, int requiredLength,
        [CallerArgumentExpression(nameof(span))] string? paramName = null)
    {
        if (span.Length - index < requiredLength)
            throw new ArgumentException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    s_argInvalidSpanTooShort,
                    requiredLength),
                paramName);
    }

    /// <summary>
    /// Throws an <see cref="ArgumentException" /> if the span length is not a positive multiple of
    /// <paramref name="divisor" />.
    /// </summary>
    /// <typeparam name="T">The element type of the span.</typeparam>
    /// <param name="span">The span to validate.</param>
    /// <param name="divisor">The required positive divisor.</param>
    /// <param name="throwIfZero">
    /// When <see langword="true" />, an empty span is treated as invalid. When <see langword="false" />, an empty span
    /// passes validation.
    /// </param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="span" /> is empty (and <paramref name="throwIfZero" /> is <see langword="true" />),
    /// or when <c>span.Length % divisor != 0</c>.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfSpanLengthNotPositiveMultipleOf<T>(
        ReadOnlySpan<T> span, int divisor, bool throwIfZero = true,
        [CallerArgumentExpression(nameof(span))] string? paramName = null)
    {
        ThrowHelper.ThrowIfZeroOrNegative(divisor);

        if ((throwIfZero && span.Length == 0) || span.Length % divisor != 0)
            throw new ArgumentException(
                string.Format(CultureInfo.CurrentCulture, s_argInvalidSpanLengthMultipleOf, divisor),
                paramName);
    }

    /// <summary>
    /// Throws an <see cref="ArgumentException" /> if the span length is not a positive multiple of
    /// <paramref name="divisor" />.
    /// </summary>
    /// <typeparam name="T">The element type of the span.</typeparam>
    /// <param name="span">The span to validate.</param>
    /// <param name="divisor">The required positive divisor.</param>
    /// <param name="throwIfZero">
    /// When <see langword="true" />, an empty span is treated as invalid. When <see langword="false" />, an empty span
    /// passes validation.
    /// </param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="span" /> is empty (and <paramref name="throwIfZero" /> is <see langword="true" />),
    /// or when <c>span.Length % divisor != 0</c>.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfSpanLengthNotPositiveMultipleOf<T>(
        Span<T> span, int divisor, bool throwIfZero = true,
        [CallerArgumentExpression(nameof(span))] string? paramName = null)
    {
        if ((throwIfZero && span.Length == 0) || span.Length % divisor != 0)
            throw new ArgumentException(
                string.Format(CultureInfo.CurrentCulture, s_argInvalidSpanLengthMultipleOf, divisor),
                paramName);
    }

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException" /> if <paramref name="index" /> is outside the valid element
    /// range of <paramref name="span" />.
    /// </summary>
    /// <typeparam name="T">The element type of the span.</typeparam>
    /// <param name="index">The index to validate.</param>
    /// <param name="span">The span against which to validate the index.</param>
    /// <param name="paramName">The name of the index parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="index" /> &lt; 0 or &gt;= <c>span.Length</c>.
    /// </exception>
    /// <remarks>
    /// Uses an unsigned cast to collapse the two-sided bounds check into a single comparison.
    /// <see cref="System.ReadOnlySpan{T}" /> is a value type and cannot be <see langword="null" />; no null guard is
    /// required or possible.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfIndexOutOfRange<T>(
        int index, ReadOnlySpan<T> span,
        [CallerArgumentExpression(nameof(index))] string? paramName = null)
    {
        if ((uint)index >= (uint)span.Length)
            throw new ArgumentOutOfRangeException(
                paramName,
                string.Format(CultureInfo.CurrentCulture, s_argOutOfRangeIndexValidRange, span.Length));
    }

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException" /> if <paramref name="index" /> is outside the valid element
    /// range of <paramref name="span" />.
    /// </summary>
    /// <typeparam name="T">The element type of the span.</typeparam>
    /// <param name="index">The index to validate.</param>
    /// <param name="span">The span against which to validate the index.</param>
    /// <param name="paramName">The name of the index parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="index" /> &lt; 0 or &gt;= <c>span.Length</c>.
    /// </exception>
    /// <remarks>
    /// Uses an unsigned cast to collapse the two-sided bounds check into a single comparison.
    /// <see cref="System.Span{T}" /> is a value type and cannot be <see langword="null" />; no null guard is required
    /// or possible.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfIndexOutOfRange<T>(
        int index, Span<T> span,
        [CallerArgumentExpression(nameof(index))] string? paramName = null)
    {
        if ((uint)index >= (uint)span.Length)
            throw new ArgumentOutOfRangeException(
                paramName,
                string.Format(CultureInfo.CurrentCulture, s_argOutOfRangeIndexValidRange, span.Length));
    }
}

#endif
