// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelper.CallerExpression.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

#if !NETSTANDARD2_0_OR_GREATER
#pragma warning disable SA1117 // Parameters should be on same line or separate lines
#pragma warning disable IDE0011 // Add braces

using Bodu.Extensions;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Bodu;

public static partial class ThrowHelper
{
    /// <summary>Cached parsed format for <see cref="ResourceStrings.Arg_Invalid_ArrayLength"/>.</summary>
    private static readonly CompositeFormat s_argInvalidArrayLength =
        CompositeFormat.Parse(ResourceStrings.Arg_Invalid_ArrayLength);

    /// <summary>Cached parsed format for <see cref="ResourceStrings.Arg_Invalid_ArrayLengthMultipleOf"/>.</summary>
    private static readonly CompositeFormat s_argInvalidArrayLengthMultipleOf =
        CompositeFormat.Parse(ResourceStrings.Arg_Invalid_ArrayLengthMultipleOf);

    /// <summary>Cached parsed format for <see cref="ResourceStrings.Arg_Invalid_ArrayOffset"/>.</summary>
    private static readonly CompositeFormat s_argInvalidArrayOffset =
        CompositeFormat.Parse(ResourceStrings.Arg_Invalid_ArrayOffset);

    /// <summary>Cached parsed format for <see cref="ResourceStrings.Arg_Invalid_ArrayOffsetOrLength"/>.</summary>
    private static readonly CompositeFormat s_argInvalidArrayOffsetOrLength =
        CompositeFormat.Parse(ResourceStrings.Arg_Invalid_ArrayOffsetOrLength);

    /// <summary>Cached parsed format for <see cref="ResourceStrings.Arg_Invalid_ArrayTooShort"/>.</summary>
    private static readonly CompositeFormat s_argInvalidArrayTooShort =
        CompositeFormat.Parse(ResourceStrings.Arg_Invalid_ArrayTooShort);

    /// <summary>Cached parsed format for <see cref="ResourceStrings.Arg_Invalid_CollectionTooSmall"/>.</summary>
    private static readonly CompositeFormat s_argInvalidCollectionTooSmall =
        CompositeFormat.Parse(ResourceStrings.Arg_Invalid_CollectionTooSmall);

    /// <summary>Cached parsed format for <see cref="ResourceStrings.Arg_Invalid_DestinationTooSmall"/>.</summary>
    private static readonly CompositeFormat s_argInvalidDestinationTooSmall =
        CompositeFormat.Parse(ResourceStrings.Arg_Invalid_DestinationTooSmall);

    /// <summary>Cached parsed format for <see cref="ResourceStrings.Arg_Invalid_GreaterThanOrEqualOtherParameter"/>.</summary>
    private static readonly CompositeFormat s_argInvalidGreaterThanOrEqualOtherParameter =
        CompositeFormat.Parse(ResourceStrings.Arg_Invalid_GreaterThanOrEqualOtherParameter);

    /// <summary>Cached parsed format for <see cref="ResourceStrings.Arg_Invalid_GreaterThanOtherParameter"/>.</summary>
    private static readonly CompositeFormat s_argInvalidGreaterThanOtherParameter =
        CompositeFormat.Parse(ResourceStrings.Arg_Invalid_GreaterThanOtherParameter);

    /// <summary>Cached parsed format for <see cref="ResourceStrings.Arg_Invalid_LessThanOrEqualOtherParameter"/>.</summary>
    private static readonly CompositeFormat s_argInvalidLessThanOrEqualOtherParameter =
        CompositeFormat.Parse(ResourceStrings.Arg_Invalid_LessThanOrEqualOtherParameter);

    /// <summary>Cached parsed format for <see cref="ResourceStrings.Arg_Invalid_LessThanOtherParameter"/>.</summary>
    private static readonly CompositeFormat s_argInvalidLessThanOtherParameter =
        CompositeFormat.Parse(ResourceStrings.Arg_Invalid_LessThanOtherParameter);

    /// <summary>Cached parsed format for <see cref="ResourceStrings.Arg_Invalid_MustBeOfType"/>.</summary>
    private static readonly CompositeFormat s_argInvalidMustBeOfType =
        CompositeFormat.Parse(ResourceStrings.Arg_Invalid_MustBeOfType);

    /// <summary>Cached parsed format for <see cref="ResourceStrings.Arg_Invalid_PositiveMultipleOf"/>.</summary>
    private static readonly CompositeFormat s_argInvalidPositiveMultipleOf =
        CompositeFormat.Parse(ResourceStrings.Arg_Invalid_PositiveMultipleOf);

    /// <summary>Cached parsed format for <see cref="ResourceStrings.Arg_Invalid_SpanLength"/>.</summary>
    private static readonly CompositeFormat s_argInvalidSpanLength =
        CompositeFormat.Parse(ResourceStrings.Arg_Invalid_SpanLength);

    /// <summary>Cached parsed format for <see cref="ResourceStrings.Arg_Invalid_SpanLengthMultipleOf"/>.</summary>
    private static readonly CompositeFormat s_argInvalidSpanLengthMultipleOf =
        CompositeFormat.Parse(ResourceStrings.Arg_Invalid_SpanLengthMultipleOf);

    /// <summary>Cached parsed format for <see cref="ResourceStrings.Arg_Invalid_SpanTooShort"/>.</summary>
    private static readonly CompositeFormat s_argInvalidSpanTooShort =
        CompositeFormat.Parse(ResourceStrings.Arg_Invalid_SpanTooShort);

    /// <summary>Cached parsed format for <see cref="ResourceStrings.Arg_Invalid_StringLength"/>.</summary>
    private static readonly CompositeFormat s_argInvalidStringLength =
        CompositeFormat.Parse(ResourceStrings.Arg_Invalid_StringLength);

    /// <summary>Cached parsed format for <see cref="ResourceStrings.Arg_Invalid_StringLengthRange"/>.</summary>
    private static readonly CompositeFormat s_argInvalidStringLengthRange =
        CompositeFormat.Parse(ResourceStrings.Arg_Invalid_StringLengthRange);

    /// <summary>Cached parsed format for <see cref="ResourceStrings.Arg_Invalid_StringTooLong"/>.</summary>
    private static readonly CompositeFormat s_argInvalidStringTooLong =
        CompositeFormat.Parse(ResourceStrings.Arg_Invalid_StringTooLong);

    /// <summary>Cached parsed format for <see cref="ResourceStrings.Arg_Invalid_ValuesNotEqual"/>.</summary>
    private static readonly CompositeFormat s_argInvalidValuesNotEqual =
        CompositeFormat.Parse(ResourceStrings.Arg_Invalid_ValuesNotEqual);

    /// <summary>Cached parsed format for <see cref="ResourceStrings.Arg_OutOfRangeException_EnumValue"/>.</summary>
    private static readonly CompositeFormat s_argOutOfRangeExceptionEnumValue =
        CompositeFormat.Parse(ResourceStrings.Arg_OutOfRangeException_EnumValue);

    /// <summary>Cached parsed format for <see cref="ResourceStrings.Arg_OutOfRange_CountExceedsAvailable"/>.</summary>
    private static readonly CompositeFormat s_argOutOfRangeCountExceedsAvailable =
        CompositeFormat.Parse(ResourceStrings.Arg_OutOfRange_CountExceedsAvailable);

    /// <summary>Cached parsed format for <see cref="ResourceStrings.Arg_OutOfRange_IndexValidRange"/>.</summary>
    private static readonly CompositeFormat s_argOutOfRangeIndexValidRange =
        CompositeFormat.Parse(ResourceStrings.Arg_OutOfRange_IndexValidRange);

    /// <summary>Cached parsed format for <see cref="ResourceStrings.Arg_OutOfRange_RequireBetweenExclusive"/>.</summary>
    private static readonly CompositeFormat s_argOutOfRangeRequireBetweenExclusive =
        CompositeFormat.Parse(ResourceStrings.Arg_OutOfRange_RequireBetweenExclusive);

    /// <summary>Cached parsed format for <see cref="ResourceStrings.Arg_OutOfRange_RequireBetweenInclusive"/>.</summary>
    private static readonly CompositeFormat s_argOutOfRangeRequireBetweenInclusive =
        CompositeFormat.Parse(ResourceStrings.Arg_OutOfRange_RequireBetweenInclusive);

    /// <summary>Cached parsed format for <see cref="ResourceStrings.Arg_OutOfRange_RequireGreaterThan"/>.</summary>
    private static readonly CompositeFormat s_argOutOfRangeRequireGreaterThan =
        CompositeFormat.Parse(ResourceStrings.Arg_OutOfRange_RequireGreaterThan);

    /// <summary>Cached parsed format for <see cref="ResourceStrings.Arg_OutOfRange_RequireGreaterThanOrEqual"/>.</summary>
    private static readonly CompositeFormat s_argOutOfRangeRequireGreaterThanOrEqual =
        CompositeFormat.Parse(ResourceStrings.Arg_OutOfRange_RequireGreaterThanOrEqual);

    /// <summary>Cached parsed format for <see cref="ResourceStrings.Arg_OutOfRange_RequireLessThan"/>.</summary>
    private static readonly CompositeFormat s_argOutOfRangeRequireLessThan =
        CompositeFormat.Parse(ResourceStrings.Arg_OutOfRange_RequireLessThan);

    /// <summary>Cached parsed format for <see cref="ResourceStrings.Arg_OutOfRange_RequireLessThanOrEqual"/>.</summary>
    private static readonly CompositeFormat s_argOutOfRangeRequireLessThanOrEqual =
        CompositeFormat.Parse(ResourceStrings.Arg_OutOfRange_RequireLessThanOrEqual);

    /// <summary>Cached parsed format for <see cref="ResourceStrings.Arg_OutOfRange_SequenceRangeOverflow"/>.</summary>
    private static readonly CompositeFormat s_argOutOfRangeSequenceRangeOverflow =
        CompositeFormat.Parse(ResourceStrings.Arg_OutOfRange_SequenceRangeOverflow);

    /// <summary>Cached parsed format for <see cref="ResourceStrings.Arg_OutOfRange_ValuesEqual"/>.</summary>
    private static readonly CompositeFormat s_argOutOfRangeValuesEqual =
        CompositeFormat.Parse(ResourceStrings.Arg_OutOfRange_ValuesEqual);

    /// <summary>Cached parsed format for <see cref="ResourceStrings.Arg_Required_ParameterRequiredIf"/>.</summary>
    private static readonly CompositeFormat s_argRequiredParameterRequiredIf =
        CompositeFormat.Parse(ResourceStrings.Arg_Required_ParameterRequiredIf);

    /// <summary>
    /// Throws an <see cref="ArgumentNullException"/> if the array is <see langword="null"/>, or an
    /// <see cref="ArgumentException"/> if it contains any non-numeric element.
    /// </summary>
    /// <param name="array">The array to validate. Must not be <see langword="null"/>.</param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="array"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when any element in <paramref name="array"/> is not a recognized numeric type.
    /// </exception>
    /// <remarks>
    /// Validates that each non-null element is one of the primitive numeric types: <see cref="byte"/>,
    /// <see cref="sbyte"/>, <see cref="short"/>, <see cref="ushort"/>, <see cref="int"/>, <see cref="uint"/>,
    /// <see cref="long"/>, <see cref="ulong"/>, <see cref="float"/>, <see cref="double"/>, or
    /// <see cref="decimal"/>. Nullable wrappers are unwrapped before the type check is applied.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfArrayContainsNonNumeric(
        Array? array,
        [CallerArgumentExpression(nameof(array))] string? paramName = null)
    {
        if (array is null)
            throw new ArgumentNullException(paramName);

        foreach (var item in array)
        {
            if (item is null) continue;

            Type type = item.GetType();

            // Unbox nullable to underlying type if needed.
            if (Nullable.GetUnderlyingType(type) is Type underlying)
                type = underlying;

            if (type != typeof(byte) && type != typeof(sbyte) &&
                type != typeof(short) && type != typeof(ushort) &&
                type != typeof(int) && type != typeof(uint) &&
                type != typeof(long) && type != typeof(ulong) &&
                type != typeof(float) && type != typeof(double) &&
                type != typeof(decimal))
            {
                throw new ArgumentException(ResourceStrings.Arg_Invalid_Array_NumericOnly, paramName);
            }
        }
    }

    /// <summary>
    /// Throws an <see cref="ArgumentNullException"/> if the array is <see langword="null"/>, or an
    /// <see cref="ArgumentException"/> if it is not single-dimensional.
    /// </summary>
    /// <param name="array">The array to validate. Must not be <see langword="null"/>.</param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="array"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="array"/> has a rank other than 1.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfArrayMultidimensional(
        Array? array,
        [CallerArgumentExpression(nameof(array))] string? paramName = null)
    {
        if (array is null)
            throw new ArgumentNullException(paramName);

        if (array.Rank != 1)
            throw new ArgumentException(ResourceStrings.Rank_MultiDimensionArrayNotSupported, paramName);
    }

    /// <summary>
    /// Throws an <see cref="ArgumentNullException"/> if the array is <see langword="null"/>, or an
    /// <see cref="ArgumentException"/> if it does not have a zero lower bound.
    /// </summary>
    /// <param name="array">The array to validate. Must not be <see langword="null"/>.</param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="array"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <c>array.GetLowerBound(0) != 0</c>.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfArrayIsNotZeroBased(
        Array? array,
        [CallerArgumentExpression(nameof(array))] string? paramName = null)
    {
        if (array is null)
            throw new ArgumentNullException(paramName);

        if (array.GetLowerBound(0) != 0)
            throw new ArgumentException(ResourceStrings.Arg_Invalid_ArrayNonZeroLowerBound, paramName);
    }

    /// <summary>
    /// Throws an exception if the specified <paramref name="array"/> does not have exactly
    /// <paramref name="expectedLength"/> elements.
    /// </summary>
    /// <param name="array">The array to validate. Must not be <see langword="null"/>.</param>
    /// <param name="expectedLength">The exact number of elements that <paramref name="array"/> must contain.</param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="array"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <c>array.Length</c> does not equal <paramref name="expectedLength"/>.
    /// </exception>
    /// <remarks>
    /// Commonly used in cryptographic APIs or buffer transformations where a fixed-size input is mandatory
    /// (e.g. 16 bytes for a cipher block, 32 bytes for a key).
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfArrayLengthIsNotEqualTo(
        Array? array, int expectedLength,
        [CallerArgumentExpression(nameof(array))] string? paramName = null)
    {
        if (array is null)
            throw new ArgumentNullException(paramName);

        if (array.Length != expectedLength)
            throw new ArgumentException(
                string.Format(CultureInfo.CurrentCulture, s_argInvalidArrayLength, expectedLength),
                paramName);
    }

    /// <summary>
    /// Throws an exception if the specified <paramref name="array"/> has fewer than
    /// <paramref name="minimumLength"/> elements.
    /// </summary>
    /// <param name="array">The array to validate. Must not be <see langword="null"/>.</param>
    /// <param name="minimumLength">
    /// The minimum number of elements that <paramref name="array"/> must contain.
    /// </param>
    /// <param name="paramName">
    /// The name of the array parameter. Supplied automatically by the compiler via
    /// <see cref="CallerArgumentExpressionAttribute"/>.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="array"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <c>array.Length</c> is less than <paramref name="minimumLength"/>.
    /// </exception>
    /// <remarks>
    /// Use this overload when the caller may supply a larger array than required and the
    /// excess elements are simply ignored — for example, a buffer that must hold at least
    /// a full cipher block but may be larger. When the length must be exact, use
    /// <see cref="ThrowIfArrayLengthIsNotEqualTo"/> instead.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfArrayLengthIsInsufficient(
        Array? array, int minimumLength,
        [CallerArgumentExpression(nameof(array))] string? paramName = null)
    {
        if (array is null)
            throw new ArgumentNullException(paramName);
        if (array.Length < minimumLength)
            throw new ArgumentException(
                string.Format(CultureInfo.CurrentCulture, s_argInvalidArrayTooShort, minimumLength),
                paramName);
    }

    /// <summary>
    /// Throws an exception if the span does not have exactly <paramref name="expectedLength"/> elements.
    /// </summary>
    /// <typeparam name="T">The element type of the span.</typeparam>
    /// <param name="span">The span to validate.</param>
    /// <param name="expectedLength">The exact number of elements that <paramref name="span"/> must contain.</param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <c>span.Length</c> does not equal <paramref name="expectedLength"/>.
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
    /// Throws an exception if the specified <paramref name="span"/> has fewer than
    /// <paramref name="minimumLength"/> elements.
    /// </summary>
    /// <typeparam name="T">The element type of the span.</typeparam>
    /// <param name="span">The span to validate.</param>
    /// <param name="minimumLength">
    /// The minimum number of elements that <paramref name="span"/> must contain.
    /// </param>
    /// <param name="paramName">
    /// The name of the span parameter. Supplied automatically by the compiler via
    /// <see cref="CallerArgumentExpressionAttribute"/>.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when <c>span.Length</c> is less than <paramref name="minimumLength"/>.
    /// </exception>
    /// <remarks>
    /// <see cref="System.Span{T}"/> is a value type and cannot be <see langword="null"/>; no null guard
    /// is required or possible. Use this overload when the caller may supply a larger span than required
    /// and the excess elements are simply ignored — for example, a buffer that must hold at least a full
    /// cipher block but may be larger. When the length must be exact, use
    /// <see cref="ThrowIfSpanLengthIsNotEqualTo{T}(Span{T}, int, string)"/> instead.
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
    /// Throws an exception if the span does not have exactly <paramref name="expectedLength"/> elements.
    /// </summary>
    /// <typeparam name="T">The element type of the span.</typeparam>
    /// <param name="span">The span to validate.</param>
    /// <param name="expectedLength">The exact number of elements that <paramref name="span"/> must contain.</param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <c>span.Length</c> does not equal <paramref name="expectedLength"/>.
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
    /// Throws an exception if the specified <paramref name="span"/> has fewer than
    /// <paramref name="minimumLength"/> elements.
    /// </summary>
    /// <typeparam name="T">The element type of the span.</typeparam>
    /// <param name="span">The read-only span to validate.</param>
    /// <param name="minimumLength">
    /// The minimum number of elements that <paramref name="span"/> must contain.
    /// </param>
    /// <param name="paramName">
    /// The name of the span parameter. Supplied automatically by the compiler via
    /// <see cref="CallerArgumentExpressionAttribute"/>.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when <c>span.Length</c> is less than <paramref name="minimumLength"/>.
    /// </exception>
    /// <remarks>
    /// <see cref="System.ReadOnlySpan{T}"/> is a value type and cannot be <see langword="null"/>; no null
    /// guard is required or possible. Use this overload when the caller may supply a larger span than
    /// required and the excess elements are simply ignored — for example, a buffer that must hold at least
    /// a full cipher block but may be larger. When the length must be exact, use
    /// <see cref="ThrowIfSpanLengthIsNotEqualTo{T}(ReadOnlySpan{T}, int, string)"/> instead.
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
    /// Throws an <see cref="ArgumentNullException"/> if the array is <see langword="null"/>, or an
    /// <see cref="ArgumentException"/> if it has zero length.
    /// </summary>
    /// <param name="array">The array to validate. Must not be <see langword="null"/>.</param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="array"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <c>array.Length == 0</c>.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfArrayLengthIsZero(
        Array array,
        [CallerArgumentExpression(nameof(array))] string? paramName = null)
    {
        if (array is null)
            throw new ArgumentNullException(paramName);

        if (array.Length == 0)
            throw new ArgumentException(ResourceStrings.Arg_Invalid_ArrayIsZeroLength, paramName);
    }

    /// <summary>
    /// Throws an <see cref="ArgumentNullException"/> if the array is <see langword="null"/>, or an
    /// <see cref="ArgumentOutOfRangeException"/> if its length is not between <paramref name="minLength"/> and
    /// <paramref name="maxLength"/> (inclusive).
    /// </summary>
    /// <param name="array">The array to validate. Must not be <see langword="null"/>.</param>
    /// <param name="minLength">The minimum permitted length, inclusive. Must be zero or greater.</param>
    /// <param name="maxLength">The maximum permitted length, inclusive. Must be greater than or equal to
    /// <paramref name="minLength"/>.</param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="array"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <c>array.Length &lt; minLength</c> or <c>array.Length &gt; maxLength</c>.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfArrayLengthOutOfRange(
        Array array, int minLength, int maxLength,
        [CallerArgumentExpression(nameof(array))] string? paramName = null)
    {
        if (array is null)
            throw new ArgumentNullException(paramName);

        if (array.Length < minLength || array.Length > maxLength)
            throw new ArgumentOutOfRangeException(
                paramName,
                string.Format(CultureInfo.CurrentCulture, s_argOutOfRangeRequireBetweenInclusive, minLength, maxLength));
    }

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException"/> if the span length is not between
    /// <paramref name="minLength"/> and <paramref name="maxLength"/> (inclusive).
    /// </summary>
    /// <typeparam name="T">The element type of the span.</typeparam>
    /// <param name="span">The span to validate.</param>
    /// <param name="minLength">The minimum permitted length, inclusive. Must be zero or greater.</param>
    /// <param name="maxLength">The maximum permitted length, inclusive. Must be greater than or equal to
    /// <paramref name="minLength"/>.</param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <c>span.Length &lt; minLength</c> or <c>span.Length &gt; maxLength</c>.
    /// </exception>
    /// <remarks>
    /// <see cref="System.ReadOnlySpan{T}"/> is a value type and cannot be <see langword="null"/>; no null guard
    /// is required or possible.
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
    /// Throws an <see cref="ArgumentOutOfRangeException"/> if the span length is not between
    /// <paramref name="minLength"/> and <paramref name="maxLength"/> (inclusive).
    /// </summary>
    /// <typeparam name="T">The element type of the span.</typeparam>
    /// <param name="span">The span to validate.</param>
    /// <param name="minLength">The minimum permitted length, inclusive. Must be zero or greater.</param>
    /// <param name="maxLength">The maximum permitted length, inclusive. Must be greater than or equal to
    /// <paramref name="minLength"/>.</param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <c>span.Length &lt; minLength</c> or <c>span.Length &gt; maxLength</c>.
    /// </exception>
    /// <remarks>
    /// <see cref="System.Span{T}"/> is a value type and cannot be <see langword="null"/>; no null guard is
    /// required or possible.
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
    /// Throws an <see cref="ArgumentNullException"/> if the array is <see langword="null"/>, or an
    /// <see cref="ArgumentException"/> if its length is not a positive multiple of <paramref name="divisor"/>.
    /// </summary>
    /// <param name="array">The array to validate. Must not be <see langword="null"/>.</param>
    /// <param name="divisor">The required positive divisor.</param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="array"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <c>array.Length == 0</c> or <c>array.Length % divisor != 0</c>.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfArrayLengthNotPositiveMultipleOf(
        Array array, int divisor,
        [CallerArgumentExpression(nameof(array))] string? paramName = null)
    {
        if (array is null)
            throw new ArgumentNullException(paramName);

        if (array.Length == 0 || array.Length % divisor != 0)
            throw new ArgumentException(
                string.Format(CultureInfo.CurrentCulture, s_argInvalidArrayLengthMultipleOf, divisor),
                paramName);
    }

    /// <summary>
    /// Throws an <see cref="ArgumentNullException"/> if the array is <see langword="null"/>, an
    /// <see cref="ArgumentOutOfRangeException"/> if <paramref name="offset"/> or <paramref name="count"/> is out of
    /// range, or an <see cref="ArgumentException"/> if the segment they define exceeds the bounds of
    /// <paramref name="array"/>.
    /// </summary>
    /// <param name="array">The array to validate. Must not be <see langword="null"/>.</param>
    /// <param name="offset">The zero-based starting index within the array.</param>
    /// <param name="count">The number of elements to access from <paramref name="offset"/>.</param>
    /// <param name="paramArrayName">The name of the array parameter. Supplied automatically by the compiler.</param>
    /// <param name="paramOffsetName">The name of the index parameter. Supplied automatically by the compiler.</param>
    /// <param name="paramCountName">The name of the count parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="array"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="offset"/> or <paramref name="count"/> is negative or exceeds
    /// <c>array.Length</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <c>index + count</c> exceeds <c>array.Length</c>.
    /// </exception>
    public static void ThrowIfArrayOffsetOrCountInvalid(
        Array array, int offset, int count,
        [CallerArgumentExpression(nameof(array))] string? paramArrayName = null,
        [CallerArgumentExpression(nameof(offset))] string? paramOffsetName = null,
        [CallerArgumentExpression(nameof(count))] string? paramCountName = null)
    {
        if (array is null)
            throw new ArgumentNullException(paramArrayName);

        if (offset < 0 || offset > array.Length)
            throw new ArgumentOutOfRangeException(
                paramOffsetName,
                string.Format(CultureInfo.CurrentCulture, s_argInvalidArrayOffset, paramOffsetName));

        if (count < 0 || count > array.Length)
            throw new ArgumentOutOfRangeException(
                paramCountName,
                string.Format(CultureInfo.CurrentCulture, s_argInvalidArrayOffset, paramCountName));

        if (count > array.Length - offset)
            throw new ArgumentException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    s_argInvalidArrayOffsetOrLength,
                    paramOffsetName,
                    paramCountName,
                    paramArrayName));
    }

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException"/> if <paramref name="offset"/> or
    /// <paramref name="count"/> is out of range, or an <see cref="ArgumentException"/> if the
    /// segment they define exceeds the bounds of <paramref name="span"/>.
    /// </summary>
    /// <typeparam name="T">The type of elements in the span.</typeparam>
    /// <param name="span">The span to validate.</param>
    /// <param name="offset">The zero-based starting index within the span.</param>
    /// <param name="count">The number of elements to access from <paramref name="offset"/>.</param>
    /// <param name="paramSpanName">
    /// The name of the span parameter. Supplied automatically by the compiler via
    /// <see cref="CallerArgumentExpressionAttribute"/>.
    /// </param>
    /// <param name="paramIndexName">
    /// The name of the index parameter. Supplied automatically by the compiler via
    /// <see cref="CallerArgumentExpressionAttribute"/>.
    /// </param>
    /// <param name="paramCountName">
    /// The name of the count parameter. Supplied automatically by the compiler via
    /// <see cref="CallerArgumentExpressionAttribute"/>.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="offset"/> or <paramref name="count"/> is negative or
    /// exceeds <see cref="ReadOnlySpan{T}.Length"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <c>index + count</c> exceeds <see cref="ReadOnlySpan{T}.Length"/>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Unlike the array equivalent (<c>ThrowIfArrayOffsetOrCountInvalid</c>), this overload
    /// does not check for <see langword="null"/> and does not throw
    /// <see cref="ArgumentNullException"/>. <see cref="Span{T}"/> is a value type and can
    /// never be <see langword="null"/>; a default <see cref="Span{T}"/> is equivalent to an
    /// empty span with <see cref="ReadOnlySpan{T}.Length"/> of zero.
    /// </para>
    /// <para>
    /// Implicitly converts <paramref name="span"/> to <see cref="ReadOnlySpan{T}"/> and
    /// delegates to
    /// <see cref="ThrowIfSpanOffsetOrCountInvalid{T}(ReadOnlySpan{T}, int, int, string, string, string)"/>,
    /// which is the canonical implementation. <paramref name="paramSpanName"/>,
    /// <paramref name="paramIndexName"/>, and <paramref name="paramCountName"/> are forwarded
    /// explicitly to preserve the call-site argument expressions in any exception messages.
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
    /// Throws an <see cref="ArgumentOutOfRangeException"/> if <paramref name="offset"/> or
    /// <paramref name="count"/> is out of range, or an <see cref="ArgumentException"/> if the
    /// segment they define exceeds the bounds of <paramref name="span"/>.
    /// </summary>
    /// <typeparam name="T">The type of elements in the span.</typeparam>
    /// <param name="span">The read-only span to validate.</param>
    /// <param name="offset">The zero-based starting index within the span.</param>
    /// <param name="count">The number of elements to access from <paramref name="offset"/>.</param>
    /// <param name="paramSpanName">
    /// The name of the span parameter. Supplied automatically by the compiler via
    /// <see cref="CallerArgumentExpressionAttribute"/>.
    /// </param>
    /// <param name="paramOffsetName">
    /// The name of the index parameter. Supplied automatically by the compiler via
    /// <see cref="CallerArgumentExpressionAttribute"/>.
    /// </param>
    /// <param name="paramCountName">
    /// The name of the count parameter. Supplied automatically by the compiler via
    /// <see cref="CallerArgumentExpressionAttribute"/>.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="offset"/> or <paramref name="count"/> is negative or
    /// exceeds <see cref="ReadOnlySpan{T}.Length"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <c>index + count</c> exceeds <see cref="ReadOnlySpan{T}.Length"/>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This is the canonical implementation. The <see cref="Span{T}"/> overload
    /// (<see cref="ThrowIfSpanOffsetOrCountInvalid{T}(Span{T}, int, int, string, string, string)"/>)
    /// converts its argument and delegates here, forwarding all
    /// <see cref="CallerArgumentExpressionAttribute"/> values explicitly so that exception
    /// messages always reflect the original call-site expressions.
    /// </para>
    /// <para>
    /// Unlike the array equivalent (<c>ThrowIfArrayOffsetOrCountInvalid</c>), this overload
    /// does not check for <see langword="null"/> and does not throw
    /// <see cref="ArgumentNullException"/>. <see cref="ReadOnlySpan{T}"/> is a value type and
    /// can never be <see langword="null"/>; a default <see cref="ReadOnlySpan{T}"/> is
    /// equivalent to an empty span with <see cref="ReadOnlySpan{T}.Length"/> of zero.
    /// </para>
    /// <para>
    /// Although the index and count validations are expressed as two-part conditions
    /// (<c>&lt; 0 || &gt; span.Length</c>) rather than the single unsigned-cast trick used
    /// in <see cref="SpanExtensions"/>, the explicit form is preferred here because this is a
    /// guard method whose primary purpose is clarity of intent. The unsigned-cast form trades
    /// readability for a marginal branch reduction that is irrelevant on an error path.
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
    /// Throws an <see cref="ArgumentNullException"/> if the array is <see langword="null"/>, or an
    /// <see cref="ArgumentException"/> if it is not assignable to <typeparamref name="TExpected"/>[].
    /// </summary>
    /// <typeparam name="TExpected">The expected element type.</typeparam>
    /// <param name="array">The array to validate. Must not be <see langword="null"/>.</param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="array"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="array"/> is not of type <typeparamref name="TExpected"/>[].
    /// </exception>
    /// <remarks>
    /// The null guard is applied before the pattern match because a <see langword="null"/> reference satisfies
    /// <c>is not TExpected[]</c>, which would otherwise produce an <see cref="ArgumentException"/> rather than
    /// an <see cref="ArgumentNullException"/>.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfArrayTypeIsNotCompatible<TExpected>(
        Array array,
        [CallerArgumentExpression(nameof(array))] string? paramName = null)
    {
        if (array is null)
            throw new ArgumentNullException(paramName);

        if (array is not TExpected[])
            throw new ArgumentException(ResourceStrings.Arg_Invalid_ArrayType, paramName);
    }

    /// <summary>
    /// Throws an <see cref="ArgumentException"/> if the collection has fewer than
    /// <paramref name="minCount"/> elements.
    /// </summary>
    /// <typeparam name="T">The element type of the collection.</typeparam>
    /// <param name="collection">The collection to validate. Must not be <see langword="null"/>.</param>
    /// <param name="minCount">The minimum number of required elements.</param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="collection"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <c>collection.Count &lt; minCount</c>.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfCollectionTooSmall<T>(
        ICollection<T> collection, int minCount,
        [CallerArgumentExpression(nameof(collection))] string? paramName = null)
    {
        ThrowIfNull(collection, paramName);
        if (collection.Count < minCount)
            throw new ArgumentException(
                string.Format(CultureInfo.CurrentCulture, s_argInvalidCollectionTooSmall, minCount),
                paramName);
    }

    /// <summary>
    /// Throws an <see cref="ArgumentException"/> if the collection is empty.
    /// </summary>
    /// <typeparam name="T">The element type of the collection.</typeparam>
    /// <param name="collection">The collection to validate. Must not be <see langword="null"/>.</param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="collection"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <c>collection.Count == 0</c>.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfCollectionIsEmpty<T>(
        ICollection<T> collection,
        [CallerArgumentExpression(nameof(collection))] string? paramName = null)
    {
        ThrowIfNull(collection, paramName);
        if (collection.Count == 0)
            throw new ArgumentException(ResourceStrings.Arg_Invalid_CollectionIsEmpty, paramName);
    }

    /// <summary>
    /// Throws an <see cref="ArgumentException"/> if <paramref name="value"/> is <see langword="null"/> when
    /// <paramref name="conditionalParam"/> equals <paramref name="conditionalValue"/>.
    /// </summary>
    /// <typeparam name="TValue">The type of the parameter being validated.</typeparam>
    /// <typeparam name="TCondition">The type of the conditional parameter.</typeparam>
    /// <param name="value">The parameter value to validate for null.</param>
    /// <param name="conditionalParam">The current value of the conditional parameter.</param>
    /// <param name="conditionalValue">The value of <paramref name="conditionalParam"/> that makes
    /// <paramref name="value"/> mandatory.</param>
    /// <param name="paramName">The name of the value parameter. Supplied automatically by the compiler.</param>
    /// <param name="conditionalParamName">The name of the conditional parameter. Supplied automatically by the
    /// compiler.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="conditionalParam"/> equals <paramref name="conditionalValue"/> and
    /// <paramref name="value"/> is <see langword="null"/>.
    /// </exception>
    /// <remarks>
    /// Use this method when a parameter becomes mandatory depending on the value of another parameter.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfConditionallyRequiredParameterIsNull<TValue, TCondition>(
        TValue? value,
        TCondition conditionalParam,
        TCondition conditionalValue,
        [CallerArgumentExpression(nameof(value))] string? paramName = null,
        [CallerArgumentExpression(nameof(conditionalParam))] string? conditionalParamName = null)
    {
        if (EqualityComparer<TCondition>.Default.Equals(conditionalParam, conditionalValue) && value is null)
        {
            throw new ArgumentException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    s_argRequiredParameterRequiredIf,
                    paramName,
                    conditionalParamName,
                    conditionalValue),
                paramName);
        }
    }

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException"/> if <paramref name="count"/> is negative or exceeds
    /// <paramref name="available"/>.
    /// </summary>
    /// <param name="count">The count value to validate.</param>
    /// <param name="available">The number of available items.</param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="count"/> &lt; 0 or <paramref name="count"/> &gt;
    /// <paramref name="available"/>.
    /// </exception>
    /// <remarks>Use this method when validating that a subset operation will not exceed the size of the source.</remarks>
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
    /// Throws an <see cref="ArgumentNullException"/> if either array is <see langword="null"/>, or an
    /// <see cref="ArgumentException"/> if <paramref name="destination"/> is shorter than
    /// <paramref name="source"/>.
    /// </summary>
    /// <typeparam name="TSource">The element type of the source array.</typeparam>
    /// <typeparam name="TDestination">The element type of the destination array.</typeparam>
    /// <param name="source">The source array. Must not be <see langword="null"/>.</param>
    /// <param name="destination">The destination array. Must not be <see langword="null"/>.</param>
    /// <param name="paramDestinationName">The name of the destination parameter. Supplied automatically by the
    /// compiler.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source"/> or <paramref name="destination"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <c>destination.Length &lt; source.Length</c>.
    /// </exception>
    /// <remarks>Useful for validating array-to-array operations such as copying or endian-swapping.</remarks>
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
                string.Format(CultureInfo.CurrentCulture, s_argInvalidDestinationTooSmall, "array", destination.Length),
                paramDestinationName);
    }

    /// <summary>
    /// Throws an <see cref="ArgumentException"/> if <paramref name="destination"/> is shorter than
    /// <paramref name="source"/>.
    /// </summary>
    /// <typeparam name="TSource">The element type of the source span.</typeparam>
    /// <typeparam name="TDestination">The element type of the destination span.</typeparam>
    /// <param name="source">The source span.</param>
    /// <param name="destination">The destination span.</param>
    /// <param name="paramDestinationName">The name of the destination parameter. Supplied automatically by the
    /// compiler.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <c>destination.Length &lt; source.Length</c>.
    /// </exception>
    /// <remarks>Useful for validating buffer-to-buffer operations such as copying or endian-swapping.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfDestinationSpanTooSmall<TSource, TDestination>(
        ReadOnlySpan<TSource> source,
        Span<TDestination> destination,
        [CallerArgumentExpression(nameof(destination))] string? paramDestinationName = null)
    {
        if (destination.Length < source.Length)
            throw new ArgumentException(
                string.Format(CultureInfo.CurrentCulture, s_argInvalidDestinationTooSmall, "span", destination.Length),
                paramDestinationName);
    }

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException"/> if <paramref name="value"/> is not a defined member of
    /// <typeparamref name="TEnum"/>.
    /// </summary>
    /// <typeparam name="TEnum">The enum type.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="value"/> is not a defined member of <typeparamref name="TEnum"/>.
    /// </exception>
    public static void ThrowIfEnumValueIsUndefined<TEnum>(
        TEnum value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(typeof(TEnum), value))
            throw new ArgumentOutOfRangeException(
                paramName,
                string.Format(CultureInfo.CurrentCulture, s_argOutOfRangeExceptionEnumValue, typeof(TEnum).Name, value));
    }

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException"/> if <paramref name="value"/> is greater than
    /// <paramref name="max"/>.
    /// </summary>
    /// <typeparam name="T">A comparable type.</typeparam>
    /// <param name="value">The value to compare.</param>
    /// <param name="max">The inclusive upper bound.</param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="value"/> &gt; <paramref name="max"/>.
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
                string.Format(CultureInfo.CurrentCulture, s_argOutOfRangeRequireLessThanOrEqual, max));
    }

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException"/> if <paramref name="value"/> is greater than or equal to
    /// <paramref name="max"/>.
    /// </summary>
    /// <typeparam name="T">A comparable type.</typeparam>
    /// <param name="value">The value to compare.</param>
    /// <param name="max">The exclusive upper bound.</param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="value"/> &gt;= <paramref name="max"/>.
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
                string.Format(CultureInfo.CurrentCulture, s_argOutOfRangeRequireLessThan, max));
    }

    /// <summary>
    /// Throws an <see cref="ArgumentException"/> if <paramref name="value"/> is greater than or equal to
    /// <paramref name="other"/>.
    /// </summary>
    /// <typeparam name="T">A comparable type.</typeparam>
    /// <param name="value">The value being validated.</param>
    /// <param name="other">The comparison reference value.</param>
    /// <param name="paramName">The name of the value parameter. Supplied automatically by the compiler.</param>
    /// <param name="otherName">The name of the comparison parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="value"/> &gt;= <paramref name="other"/>.
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
                string.Format(CultureInfo.CurrentCulture, s_argInvalidGreaterThanOrEqualOtherParameter, otherName),
                paramName);
    }

    /// <summary>
    /// Throws an <see cref="ArgumentException"/> if <paramref name="value"/> is greater than
    /// <paramref name="other"/>.
    /// </summary>
    /// <typeparam name="T">A comparable type.</typeparam>
    /// <param name="value">The value being validated.</param>
    /// <param name="other">The comparison reference value.</param>
    /// <param name="paramName">The name of the value parameter. Supplied automatically by the compiler.</param>
    /// <param name="otherName">The name of the comparison parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="value"/> &gt; <paramref name="other"/>.
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
                string.Format(CultureInfo.CurrentCulture, s_argInvalidGreaterThanOtherParameter, otherName),
                paramName);
    }

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException"/> if <paramref name="index"/> is outside the valid range
    /// of <paramref name="array"/>.
    /// </summary>
    /// <param name="index">The index to validate.</param>
    /// <param name="array">The array against which to validate the index.</param>
    /// <param name="paramName">The name of the index parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="array"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="index"/> &lt; 0 or &gt;= <c>array.LongLength</c>.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfIndexOutOfRange(
        long index, Array array,
        [CallerArgumentExpression(nameof(index))] string? paramName = null)
    {
        if (array is null)
            throw new ArgumentNullException(nameof(array));

        if (index < 0 || index >= array.LongLength)
            throw new ArgumentOutOfRangeException(
                paramName,
                string.Format(CultureInfo.CurrentCulture, s_argOutOfRangeIndexValidRange, array.LongLength));
    }

    /// <summary>
    /// Throws an <see cref="ArgumentException"/> if <paramref name="comparison"/> is not a valid
    /// <see cref="StringComparison"/> value.
    /// </summary>
    /// <param name="comparison">The <see cref="StringComparison"/> value to validate.</param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="comparison"/> is not a defined enum member.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfInvalidStringComparison(
        StringComparison comparison,
        [CallerArgumentExpression(nameof(comparison))] string? paramName = null)
    {
        if (!Enum.IsDefined(typeof(StringComparison), comparison))
            throw new ArgumentException(ResourceStrings.Arg_Invalid_StringComparison, paramName);
    }

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException"/> if <paramref name="value"/> is less than
    /// <paramref name="min"/>.
    /// </summary>
    /// <typeparam name="T">A comparable type.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="min">The inclusive lower bound.</param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="value"/> &lt; <paramref name="min"/>.
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
                string.Format(CultureInfo.CurrentCulture, s_argOutOfRangeRequireGreaterThanOrEqual, min));
    }

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException"/> if the nullable <paramref name="value"/> is less than
    /// <paramref name="min"/>. Optionally throws <see cref="ArgumentNullException"/> if
    /// <paramref name="value"/> is <see langword="null"/>.
    /// </summary>
    /// <typeparam name="T">A comparable value type.</typeparam>
    /// <param name="value">The nullable value to validate.</param>
    /// <param name="min">The inclusive lower bound.</param>
    /// <param name="throwIfNull">
    /// When <see langword="true"/>, throws <see cref="ArgumentNullException"/> if <paramref name="value"/> is
    /// <see langword="null"/>. When <see langword="false"/>, a <see langword="null"/> value passes validation.
    /// </param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value"/> is <see langword="null"/> and <paramref name="throwIfNull"/> is
    /// <see langword="true"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="value"/> is non-null and less than <paramref name="min"/>.
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
                string.Format(CultureInfo.CurrentCulture, s_argOutOfRangeRequireGreaterThanOrEqual, min));
        }
    }

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException"/> if <paramref name="value"/> is less than or equal to
    /// <paramref name="min"/>.
    /// </summary>
    /// <typeparam name="T">A comparable type.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="min">The exclusive lower bound.</param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="value"/> &lt;= <paramref name="min"/>.
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
                string.Format(CultureInfo.CurrentCulture, s_argOutOfRangeRequireGreaterThan, min));
    }

    /// <summary>
    /// Throws an <see cref="ArgumentException"/> if <paramref name="value"/> is less than or equal to
    /// <paramref name="other"/>.
    /// </summary>
    /// <typeparam name="T">A comparable type.</typeparam>
    /// <param name="value">The value being validated.</param>
    /// <param name="other">The comparison reference value.</param>
    /// <param name="paramName">The name of the value parameter. Supplied automatically by the compiler.</param>
    /// <param name="otherName">The name of the comparison parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="value"/> &lt;= <paramref name="other"/>.
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
                string.Format(CultureInfo.CurrentCulture, s_argInvalidLessThanOrEqualOtherParameter, otherName),
                paramName);
    }

    /// <summary>
    /// Throws an <see cref="ArgumentException"/> if <paramref name="value"/> is less than
    /// <paramref name="other"/>.
    /// </summary>
    /// <typeparam name="T">A comparable type.</typeparam>
    /// <param name="value">The value being validated.</param>
    /// <param name="other">The comparison reference value.</param>
    /// <param name="paramName">The name of the value parameter. Supplied automatically by the compiler.</param>
    /// <param name="otherName">The name of the comparison parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="value"/> &lt; <paramref name="other"/>.
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
                string.Format(CultureInfo.CurrentCulture, s_argInvalidLessThanOtherParameter, otherName),
                paramName);
    }

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException"/> if <paramref name="value"/> is negative.
    /// </summary>
    /// <typeparam name="T">A comparable numeric type.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="value"/> &lt; 0.
    /// </exception>
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
    /// Throws an <see cref="ArgumentException"/> if <paramref name="value"/> is not assignable to
    /// <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The target type to validate against.</typeparam>
    /// <param name="value">The object to check.</param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="value"/> is not <see langword="null"/> and not of type
    /// <typeparamref name="T"/>, or when <paramref name="value"/> is <see langword="null"/> and
    /// <typeparamref name="T"/> is a non-nullable value type.
    /// </exception>
    /// <remarks>
    /// A <see langword="null"/> value passes validation only when <typeparamref name="T"/> is a reference or
    /// nullable type.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNotOfType<T>(
        object? value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value is null)
        {
            if (default(T) is not null)
                throw new ArgumentException(
                    string.Format(CultureInfo.CurrentCulture, s_argInvalidMustBeOfType, typeof(T)),
                    paramName);
        }
        else if (value is not T)
        {
            throw new ArgumentException(
                string.Format(CultureInfo.CurrentCulture, s_argInvalidMustBeOfType, typeof(T)),
                paramName);
        }
    }

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException"/> if <paramref name="value"/> is not a positive multiple
    /// of <paramref name="divisor"/>.
    /// </summary>
    /// <typeparam name="T">A binary integer type.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="divisor">The required positive divisor. Must itself be positive.</param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="value"/> &lt;= 0 or <c>value % divisor != 0</c>.
    /// </exception>
    /// <remarks>
    /// Useful for validating aligned buffer sizes, memory boundaries, or block-aligned lengths. When the
    /// required constraint is specifically a power of two, prefer <see cref="ThrowIfNotPowerOfTwo{T}"/>,
    /// which uses a single bitwise operation.
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
    /// Throws an <see cref="ArgumentOutOfRangeException"/> if <paramref name="value"/> is not a positive power of
    /// two.
    /// </summary>
    /// <typeparam name="T">A binary integer type.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="value"/> is not a positive integer whose binary representation contains
    /// exactly one set bit (i.e. <c>value &lt;= 0</c> or <c>(value &amp; (value - 1)) != 0</c>).
    /// </exception>
    /// <remarks>
    /// A specialization of the positive-multiple-of family. When the divisor is itself an arbitrary positive
    /// integer rather than a power of two, use <see cref="ThrowIfNotPositiveMultipleOf{T}"/> instead.
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
    /// Throws an <see cref="ArgumentOutOfRangeException"/> if <paramref name="value"/> is not zero.
    /// </summary>
    /// <typeparam name="T">A type that implements <see cref="IEquatable{T}"/>.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="value"/> is not equal to the default value of <typeparamref name="T"/>.
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
    /// Throws an <see cref="ArgumentOutOfRangeException"/> if <paramref name="value"/> is not an ASCII decimal
    /// digit character, that is, a character in the range <c>'0'</c> (U+0030) to <c>'9'</c> (U+0039) inclusive.
    /// </summary>
    /// <param name="value">The character to validate.</param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="value"/> is outside the inclusive range <c>'0'</c> to <c>'9'</c>.
    /// </exception>
    /// <remarks>Useful for validating inputs to algorithms that operate on decimal digit strings, such as check-digit algorithms.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNotAsciiDecimalDigit(
        char value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if ((uint)(value - '0') > 9u)
            throw new ArgumentOutOfRangeException(
                paramName,
                value,
                $"Character '{value}' (U+{(int)value:X4}) is not an ASCII decimal digit ('0' to '9').");
    }

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException"/> if <paramref name="value"/> is not an ASCII uppercase
    /// alphanumeric character, that is, a character in the range <c>'0'</c> (U+0030) to <c>'9'</c> (U+0039) or
    /// <c>'A'</c> (U+0041) to <c>'Z'</c> (U+005A) inclusive.
    /// </summary>
    /// <param name="value">The character to validate.</param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="value"/> is outside the inclusive ranges <c>'0'</c> to <c>'9'</c> and
    /// <c>'A'</c> to <c>'Z'</c>.
    /// </exception>
    /// <remarks>
    /// Useful for validating inputs to algorithms that operate on uppercase alphanumeric identifiers, such as
    /// ISO 7064 MOD 97-10 (IBAN / LEI), ISIN, SEDOL, and CUSIP. Lowercase letters are <b>not</b> accepted — the
    /// caller is expected to normalize before validation.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNotAsciiAlphanumericUppercase(
        char value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if ((uint)(value - '0') > 9u && (uint)(value - 'A') > 25u)
            throw new ArgumentOutOfRangeException(
                paramName,
                value,
                $"Character '{value}' (U+{(int)value:X4}) is not an ASCII uppercase alphanumeric character ('0' to '9' or 'A' to 'Z').");
    }

    /// <summary>
    /// Throws an <see cref="ArgumentNullException"/> if <paramref name="value"/> is <see langword="null"/>.
    /// </summary>
    /// <typeparam name="T">The type of the object.</typeparam>
    /// <param name="value">The value to check. Must not be <see langword="null"/>.</param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value"/> is <see langword="null"/>.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNull<T>(
        [NotNull] T value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value is null)
            throw new ArgumentNullException(paramName);
    }

    /// <summary>
    /// Throws an <see cref="ArgumentNullException"/> if <paramref name="value"/> is <see langword="null"/>,
    /// using <paramref name="message"/> as the exception message.
    /// </summary>
    /// <typeparam name="T">The type of the object.</typeparam>
    /// <param name="value">The value to check. Must not be <see langword="null"/>.</param>
    /// <param name="message">The message to include in the exception.</param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value"/> is <see langword="null"/>.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNull<T>(
        [NotNull] T value, string message,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value is null)
            throw new ArgumentNullException(paramName, message);
    }

    /// <summary>
    /// Throws an <see cref="ArgumentNullException"/> if <paramref name="value"/> is <see langword="null"/>, or
    /// an <see cref="ArgumentException"/> if it is an empty string.
    /// </summary>
    /// <param name="value">The string value to validate.</param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is an empty string.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNullOrEmpty(
        string value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value is null)
            throw new ArgumentNullException(paramName);

        if (value.Length == 0)
            throw new ArgumentException(ResourceStrings.Arg_Invalid_StringNullOrEmpty, paramName);
    }

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException"/> if <paramref name="value"/> is not within the
    /// specified range.
    /// </summary>
    /// <typeparam name="T">A type that implements <see cref="IComparable{T}"/>.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="min">The lower bound of the range.</param>
    /// <param name="max">The upper bound of the range.</param>
    /// <param name="inclusive">
    /// When <see langword="true"/>, the bounds are inclusive; when <see langword="false"/>, they are exclusive.
    /// </param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="value"/> falls outside the range defined by <paramref name="min"/> and
    /// <paramref name="max"/>.
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
                    string.Format(CultureInfo.CurrentCulture, s_argOutOfRangeRequireBetweenInclusive, min, max));
        }
        else
        {
            if (value.CompareTo(min) <= 0 || value.CompareTo(max) >= 0)
                throw new ArgumentOutOfRangeException(
                    paramName,
                    string.Format(CultureInfo.CurrentCulture, s_argOutOfRangeRequireBetweenExclusive, min, max));
        }
    }

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException"/> if <paramref name="value"/> is positive.
    /// </summary>
    /// <typeparam name="T">A comparable numeric type.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="value"/> &gt; 0.
    /// </exception>
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
    /// Throws an <see cref="ArgumentOutOfRangeException"/> if the sequence starting at <paramref name="start"/>
    /// with <paramref name="count"/> elements would overflow <see cref="int.MaxValue"/>.
    /// </summary>
    /// <param name="start">The starting value of the sequence.</param>
    /// <param name="count">The number of values in the sequence.</param>
    /// <param name="paramName">The name of the count parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <c>start + count - 1</c> would exceed <see cref="int.MaxValue"/>.
    /// </exception>
    /// <remarks>Prevents arithmetic overflow when generating <see cref="int"/>-based numeric sequences.</remarks>
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
    /// Throws an <see cref="ArgumentOutOfRangeException"/> if the sequence starting at <paramref name="start"/>
    /// with <paramref name="count"/> elements would overflow <see cref="long.MaxValue"/>.
    /// </summary>
    /// <param name="start">The starting value of the sequence.</param>
    /// <param name="count">The number of values in the sequence.</param>
    /// <param name="paramName">The name of the count parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <c>start + count - 1</c> would exceed <see cref="long.MaxValue"/>.
    /// </exception>
    /// <remarks>Prevents arithmetic overflow when generating <see cref="long"/>-based numeric sequences.</remarks>
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
    /// Throws an <see cref="ArgumentException"/> if the remaining elements of <paramref name="span"/> from
    /// <paramref name="index"/> are fewer than <paramref name="requiredLength"/>.
    /// </summary>
    /// <typeparam name="T">The element type of the span.</typeparam>
    /// <param name="span">The span to validate.</param>
    /// <param name="index">The position from which remaining length is measured.</param>
    /// <param name="requiredLength">The minimum number of elements required from <paramref name="index"/>.</param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <c>span.Length - index &lt; requiredLength</c>.
    /// </exception>
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
    /// Throws an <see cref="ArgumentException"/> if the remaining elements of <paramref name="span"/> from
    /// <paramref name="index"/> are fewer than <paramref name="requiredLength"/>.
    /// </summary>
    /// <typeparam name="T">The element type of the span.</typeparam>
    /// <param name="span">The span to validate.</param>
    /// <param name="index">The position from which remaining length is measured.</param>
    /// <param name="requiredLength">The minimum number of elements required from <paramref name="index"/>.</param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <c>span.Length - index &lt; requiredLength</c>.
    /// </exception>
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
    /// Throws an <see cref="ArgumentException"/> if the span length is not a positive multiple of
    /// <paramref name="divisor"/>.
    /// </summary>
    /// <typeparam name="T">The element type of the span.</typeparam>
    /// <param name="span">The span to validate.</param>
    /// <param name="divisor">The required positive divisor.</param>
    /// <param name="throwIfZero">
    /// When <see langword="true"/>, an empty span is treated as invalid. When <see langword="false"/>, an empty
    /// span passes validation.
    /// </param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="span"/> is empty (and <paramref name="throwIfZero"/> is
    /// <see langword="true"/>), or when <c>span.Length % divisor != 0</c>.
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
    /// Throws an <see cref="ArgumentException"/> if the span length is not a positive multiple of
    /// <paramref name="divisor"/>.
    /// </summary>
    /// <typeparam name="T">The element type of the span.</typeparam>
    /// <param name="span">The span to validate.</param>
    /// <param name="divisor">The required positive divisor.</param>
    /// <param name="throwIfZero">
    /// When <see langword="true"/>, an empty span is treated as invalid. When <see langword="false"/>, an empty
    /// span passes validation.
    /// </param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="span"/> is empty (and <paramref name="throwIfZero"/> is
    /// <see langword="true"/>), or when <c>span.Length % divisor != 0</c>.
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
    /// Throws an <see cref="ArgumentOutOfRangeException"/> if <paramref name="value"/> equals zero.
    /// </summary>
    /// <typeparam name="T">A type that implements <see cref="IEquatable{T}"/>.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="value"/> equals the default value of <typeparamref name="T"/>.
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
    /// Throws an <see cref="ArgumentOutOfRangeException"/> if <paramref name="value"/> is zero or negative.
    /// </summary>
    /// <typeparam name="T">A comparable numeric type.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="value"/> &lt;= 0.
    /// </exception>
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
    /// Throws an <see cref="ArgumentOutOfRangeException"/> if <paramref name="value"/> is zero or positive.
    /// </summary>
    /// <typeparam name="T">A comparable numeric type.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="value"/> &gt;= 0.
    /// </exception>
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
    /// Throws an <see cref="ArgumentNullException"/> if <paramref name="value"/> is <see langword="null"/>, or
    /// an <see cref="ArgumentException"/> if it is empty or contains only whitespace.
    /// </summary>
    /// <param name="value">The string value to validate.</param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="value"/> is empty or contains only whitespace characters.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNullOrWhiteSpace(
        string value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value is null)
            throw new ArgumentNullException(paramName);

        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException(ResourceStrings.Arg_Invalid_StringEmptyOrWhitespace, paramName);
    }

    /// <summary>
    /// Throws an <see cref="ObjectDisposedException"/> if <paramref name="disposed"/> is
    /// <see langword="true"/>.
    /// </summary>
    /// <param name="disposed">The disposal flag to evaluate.</param>
    /// <param name="objectName">
    /// The name of the disposed object included in the exception message. Supplied automatically by the
    /// compiler via <see cref="CallerArgumentExpressionAttribute"/>.
    /// </param>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when <paramref name="disposed"/> is <see langword="true"/>.
    /// </exception>
    [SuppressMessage(
        "Microsoft.CodeAnalysis.NetAnalyzers",
        "CA1513:Use ObjectDisposedException throw helper",
        Justification = "ObjectDisposedException.ThrowIf requires an object or Type instance; this helper accepts a string objectName captured via CallerArgumentExpressionAttribute, which the BCL helper does not support.")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfDisposed(
        bool disposed,
        [CallerArgumentExpression(nameof(disposed))] string? objectName = null)
    {
        if (disposed)
            throw new ObjectDisposedException(objectName);
    }

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException"/> if <paramref name="value"/> is
    /// <see cref="double.NaN"/>, <see cref="double.PositiveInfinity"/>, or
    /// <see cref="double.NegativeInfinity"/>.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="value"/> is not a finite number.
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
    /// Throws an <see cref="ArgumentOutOfRangeException"/> if <paramref name="value"/> is
    /// <see cref="float.NaN"/>, <see cref="float.PositiveInfinity"/>, or
    /// <see cref="float.NegativeInfinity"/>.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="value"/> is not a finite number.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNotFinite(
        float value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (!float.IsFinite(value))
            throw new ArgumentOutOfRangeException(paramName, value, ResourceStrings.Arg_OutOfRange_NotFinite);
    }

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException"/> if <paramref name="value"/> equals
    /// <paramref name="other"/>.
    /// </summary>
    /// <typeparam name="T">A type that implements <see cref="IEquatable{T}"/>.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="other">The value that <paramref name="value"/> must not equal.</param>
    /// <param name="paramName">The name of the value parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="value"/> equals <paramref name="other"/>.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfEqual<T>(
        T value, T other,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
        where T : IEquatable<T>
    {
        if (value.Equals(other))
            throw new ArgumentOutOfRangeException(
                paramName,
                string.Format(CultureInfo.CurrentCulture, s_argOutOfRangeValuesEqual, other));
    }

    /// <summary>
    /// Throws an <see cref="ArgumentException"/> if <paramref name="value"/> does not equal
    /// <paramref name="other"/>.
    /// </summary>
    /// <typeparam name="T">A type that implements <see cref="IEquatable{T}"/>.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="other">The value that <paramref name="value"/> must equal.</param>
    /// <param name="paramName">The name of the value parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="value"/> does not equal <paramref name="other"/>.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNotEqual<T>(
        T value, T other,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
        where T : IEquatable<T>
    {
        if (!value.Equals(other))
            throw new ArgumentException(
                string.Format(CultureInfo.CurrentCulture, s_argInvalidValuesNotEqual, other),
                paramName);
    }

    /// <summary>
    /// Throws an <see cref="ArgumentNullException"/> if <paramref name="value"/> is <see langword="null"/>, or an
    /// <see cref="ArgumentOutOfRangeException"/> if its length exceeds <paramref name="maxLength"/> characters.
    /// </summary>
    /// <param name="value">The string to validate. Must not be <see langword="null"/>.</param>
    /// <param name="maxLength">The maximum permitted length, inclusive.</param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <c>value.Length &gt; maxLength</c>.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfStringTooLong(
        string value, int maxLength,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value is null)
            throw new ArgumentNullException(paramName);

        if (value.Length > maxLength)
            throw new ArgumentOutOfRangeException(
                paramName,
                string.Format(CultureInfo.CurrentCulture, s_argInvalidStringTooLong, maxLength));
    }

    /// <summary>
    /// Throws an <see cref="ArgumentNullException"/> if <paramref name="value"/> is <see langword="null"/>, or an
    /// <see cref="ArgumentException"/> if its length does not equal <paramref name="expectedLength"/> characters.
    /// </summary>
    /// <param name="value">The string to validate. Must not be <see langword="null"/>.</param>
    /// <param name="expectedLength">The exact required length in characters.</param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <c>value.Length != expectedLength</c>.
    /// </exception>
    /// <remarks>
    /// Useful for validating fixed-length identifiers such as ISIN (12 characters), CUSIP (9 characters),
    /// or IBAN country codes (2 characters).
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfStringLengthIsNotEqualTo(
        string value, int expectedLength,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value is null)
            throw new ArgumentNullException(paramName);

        if (value.Length != expectedLength)
            throw new ArgumentException(
                string.Format(CultureInfo.CurrentCulture, s_argInvalidStringLength, expectedLength),
                paramName);
    }

    /// <summary>
    /// Throws an <see cref="ArgumentNullException"/> if <paramref name="value"/> is <see langword="null"/>, or an
    /// <see cref="ArgumentOutOfRangeException"/> if its length is not between <paramref name="minLength"/> and
    /// <paramref name="maxLength"/> (inclusive).
    /// </summary>
    /// <param name="value">The string to validate. Must not be <see langword="null"/>.</param>
    /// <param name="minLength">The minimum permitted length, inclusive.</param>
    /// <param name="maxLength">The maximum permitted length, inclusive. Must be &gt;= <paramref name="minLength"/>.</param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <c>value.Length &lt; minLength</c> or <c>value.Length &gt; maxLength</c>.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfStringLengthOutOfRange(
        string value, int minLength, int maxLength,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value is null)
            throw new ArgumentNullException(paramName);

        if (value.Length < minLength || value.Length > maxLength)
            throw new ArgumentOutOfRangeException(
                paramName,
                string.Format(CultureInfo.CurrentCulture, s_argInvalidStringLengthRange, minLength, maxLength));
    }

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException"/> if <paramref name="value"/> is not an ASCII
    /// hexadecimal digit character — that is, a character in the ranges <c>'0'</c>–<c>'9'</c> (U+0030–U+0039),
    /// <c>'A'</c>–<c>'F'</c> (U+0041–U+0046), or <c>'a'</c>–<c>'f'</c> (U+0061–U+0066) inclusive.
    /// </summary>
    /// <param name="value">The character to validate.</param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="value"/> is not a decimal digit or a letter in <c>'A'</c>–<c>'F'</c> /
    /// <c>'a'</c>–<c>'f'</c>.
    /// </exception>
    /// <remarks>
    /// Both upper- and lowercase hex letters are accepted. Useful for validating hex-encoded cryptographic
    /// outputs such as hash digests, key material, or initialization vectors prior to parsing.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNotAsciiHexDigit(
        char value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if ((uint)(value - '0') > 9u &&
            (uint)(value - 'A') > 5u &&
            (uint)(value - 'a') > 5u)
            throw new ArgumentOutOfRangeException(
                paramName,
                value,
                $"Character '{value}' (U+{(int)value:X4}) is not a hex digit ('0'–'9', 'A'–'F', or 'a'–'f').");
    }

    /// <summary>
    /// Throws an <see cref="ArgumentNullException"/> if <paramref name="collection"/> is <see langword="null"/>,
    /// or an <see cref="ArgumentException"/> if it is read-only.
    /// </summary>
    /// <typeparam name="T">The element type of the collection.</typeparam>
    /// <param name="collection">The collection to validate. Must not be <see langword="null"/>.</param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="collection"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <c>collection.IsReadOnly</c> is <see langword="true"/>.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfReadOnly<T>(
        ICollection<T> collection,
        [CallerArgumentExpression(nameof(collection))] string? paramName = null)
    {
        ThrowIfNull(collection, paramName);
        if (collection.IsReadOnly)
            throw new ArgumentException(ResourceStrings.Arg_Invalid_CollectionReadOnly, paramName);
    }

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException"/> if <paramref name="index"/> is outside the valid
    /// element range of <paramref name="span"/>.
    /// </summary>
    /// <typeparam name="T">The element type of the span.</typeparam>
    /// <param name="index">The index to validate.</param>
    /// <param name="span">The span against which to validate the index.</param>
    /// <param name="paramName">The name of the index parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="index"/> &lt; 0 or &gt;= <c>span.Length</c>.
    /// </exception>
    /// <remarks>
    /// Uses an unsigned cast to collapse the two-sided bounds check into a single comparison.
    /// <see cref="System.ReadOnlySpan{T}"/> is a value type and cannot be <see langword="null"/>; no null guard
    /// is required or possible.
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
    /// Throws an <see cref="ArgumentOutOfRangeException"/> if <paramref name="index"/> is outside the valid
    /// element range of <paramref name="span"/>.
    /// </summary>
    /// <typeparam name="T">The element type of the span.</typeparam>
    /// <param name="index">The index to validate.</param>
    /// <param name="span">The span against which to validate the index.</param>
    /// <param name="paramName">The name of the index parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="index"/> &lt; 0 or &gt;= <c>span.Length</c>.
    /// </exception>
    /// <remarks>
    /// Uses an unsigned cast to collapse the two-sided bounds check into a single comparison.
    /// <see cref="System.Span{T}"/> is a value type and cannot be <see langword="null"/>; no null guard is
    /// required or possible.
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

#pragma warning restore IDE0011 // Add braces
#pragma warning restore SA1117
#endif
