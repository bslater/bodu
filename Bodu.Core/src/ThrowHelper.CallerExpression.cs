// --------------------------------------------------------------------------------------------------------------- //
// <copyright file="ThrowHelper.CallerExpression.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

#if !NETSTANDARD2_0_OR_GREATER
#pragma warning disable SA1117 // Parameters should be on same line or separate lines

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Bodu;

public static partial class ThrowHelper
{
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
    /// Thrown when any element in <paramref name="array"/> is not a recognised numeric type.
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

        foreach (object? item in array)
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
    public static void ThrowIfArrayIsNotSingleDimension(
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
    public static void ThrowIfArrayLengthIsInsufficient(
        Array? array, int expectedLength,
        [CallerArgumentExpression(nameof(array))] string? paramName = null)
    {
        if (array is null)
            throw new ArgumentNullException(paramName);

        if (array.Length != expectedLength)
            throw new ArgumentException(
                string.Format(ResourceStrings.Arg_Invalid_ArrayLength, expectedLength),
                paramName);
    }

    /// <summary>
    /// Throws an <see cref="ArgumentNullException"/> if the array is <see langword="null"/>, an
    /// <see cref="ArgumentOutOfRangeException"/> if <paramref name="index"/> is negative, or an
    /// <see cref="ArgumentException"/> if the array does not have enough elements from <paramref name="index"/> to
    /// accommodate <paramref name="requiredLength"/> elements.
    /// </summary>
    /// <param name="array">The array to validate. Must not be <see langword="null"/>.</param>
    /// <param name="index">The zero-based starting index within the array. Must be zero or greater.</param>
    /// <param name="requiredLength">The number of elements required starting from <paramref name="index"/>.</param>
    /// <param name="paramArrayName">The name of the array parameter. Supplied automatically by the compiler.</param>
    /// <param name="paramIndexName">The name of the index parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="array"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="index"/> is negative.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <c>array.Length - index &lt; requiredLength</c>.
    /// </exception>
    /// <remarks>
    /// Ensures that a caller can safely access a contiguous block of <paramref name="requiredLength"/> elements
    /// starting at <paramref name="index"/> without exceeding array bounds.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfArrayLengthIsInsufficient(
        Array? array, int index, int requiredLength,
        [CallerArgumentExpression(nameof(array))] string? paramArrayName = null,
        [CallerArgumentExpression(nameof(index))] string? paramIndexName = null)
    {
        if (array is null)
            throw new ArgumentNullException(paramArrayName);

        if (index < 0)
            throw new ArgumentOutOfRangeException(
                paramIndexName,
                string.Format(ResourceStrings.Arg_OutOfRange_IndexValidRange, paramArrayName));

        if (array.Length - index < requiredLength)
            throw new ArgumentException(
                string.Format(ResourceStrings.Arg_Invalid_ArrayTooShort, requiredLength),
                paramArrayName);
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
    public static void ThrowIfSpanLengthIsInsufficient<T>(
        Span<T> span, int expectedLength,
        [CallerArgumentExpression(nameof(span))] string? paramName = null)
    {
        if (span.Length != expectedLength)
            throw new ArgumentException(
                string.Format(ResourceStrings.Arg_Invalid_ArrayLength, expectedLength),
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
    public static void ThrowIfSpanLengthIsInsufficient<T>(
        ReadOnlySpan<T> span, int expectedLength,
        [CallerArgumentExpression(nameof(span))] string? paramName = null)
    {
        if (span.Length != expectedLength)
            throw new ArgumentException(
                string.Format(ResourceStrings.Arg_Invalid_ArrayLength, expectedLength),
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
                string.Format(ResourceStrings.Arg_OutOfRange_RequireBetweenInclusive, minLength, maxLength));
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
                string.Format(ResourceStrings.Arg_OutOfRange_RequireBetweenInclusive, minLength, maxLength));
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
                string.Format(ResourceStrings.Arg_OutOfRange_RequireBetweenInclusive, minLength, maxLength));
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
                string.Format(ResourceStrings.Arg_Invalid_ArrayLengthMultipleOf, divisor),
                paramName);
    }

    /// <summary>
    /// Throws an <see cref="ArgumentNullException"/> if the array is <see langword="null"/>, an
    /// <see cref="ArgumentOutOfRangeException"/> if <paramref name="index"/> or <paramref name="count"/> is out of
    /// range, or an <see cref="ArgumentException"/> if the segment they define exceeds the bounds of
    /// <paramref name="array"/>.
    /// </summary>
    /// <param name="array">The array to validate. Must not be <see langword="null"/>.</param>
    /// <param name="index">The zero-based starting index within the array.</param>
    /// <param name="count">The number of elements to access from <paramref name="index"/>.</param>
    /// <param name="paramArrayName">The name of the array parameter. Supplied automatically by the compiler.</param>
    /// <param name="paramIndexName">The name of the index parameter. Supplied automatically by the compiler.</param>
    /// <param name="paramCountName">The name of the count parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="array"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="index"/> or <paramref name="count"/> is negative or exceeds
    /// <c>array.Length</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <c>index + count</c> exceeds <c>array.Length</c>.
    /// </exception>
    public static void ThrowIfArrayOffsetOrCountInvalid(
        Array array, int index, int count,
        [CallerArgumentExpression(nameof(array))] string? paramArrayName = null,
        [CallerArgumentExpression(nameof(index))] string? paramIndexName = null,
        [CallerArgumentExpression(nameof(count))] string? paramCountName = null)
    {
        if (array is null)
            throw new ArgumentNullException(paramArrayName);

        if (index < 0 || index > array.Length)
            throw new ArgumentOutOfRangeException(
                paramIndexName,
                string.Format(ResourceStrings.Arg_Invalid_ArrayOffset, paramArrayName));

        if (count < 0 || count > array.Length)
            throw new ArgumentOutOfRangeException(
                paramCountName,
                string.Format(ResourceStrings.Arg_Invalid_ArrayOffset, paramArrayName));

        if (count > array.Length - index)
            throw new ArgumentException(
                string.Format(ResourceStrings.Arg_Invalid_ArrayOffsetOrLength,
                    paramIndexName, paramCountName, paramArrayName));
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
    /// <param name="collection">The collection to validate.</param>
    /// <param name="minCount">The minimum number of required elements.</param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
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
            throw new ArgumentException(ResourceStrings.Arg_Invalid_CollectionTooSmall, paramName);
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
                string.Format(ResourceStrings.Arg_Required_ParameterRequiredIf,
                    paramName, conditionalParamName, conditionalValue),
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
                string.Format(ResourceStrings.Arg_OutOfRange_CountExceedsAvailable, available));
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
                string.Format(ResourceStrings.Arg_Invalid_DestinationTooSmall, "array", destination.Length),
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
                string.Format(ResourceStrings.Arg_Invalid_DestinationTooSmall, "span", destination.Length),
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
                string.Format(ResourceStrings.Arg_OutOfRangeException_EnumValue, value, typeof(TEnum).Name));
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
                string.Format(ResourceStrings.Arg_OutOfRange_RequireLessThanOrEqual, max));
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
                string.Format(ResourceStrings.Arg_OutOfRange_RequireLessThan, max));
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
                string.Format(ResourceStrings.Arg_Invalid_GreaterThanOrEqualOtherParameter, otherName),
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
                string.Format(ResourceStrings.Arg_Invalid_GreaterThanOtherParameter, otherName),
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
                string.Format(ResourceStrings.Arg_OutOfRange_IndexValidRange, array.LongLength));
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
                string.Format(ResourceStrings.Arg_OutOfRange_RequireGreaterThanOrEqual, min));
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
                string.Format(ResourceStrings.Arg_OutOfRange_RequireGreaterThanOrEqual, min));
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
                string.Format(ResourceStrings.Arg_OutOfRange_RequireGreaterThan, min));
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
                string.Format(ResourceStrings.Arg_Invalid_LessThanOrEqualOtherParameter, otherName),
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
                string.Format(ResourceStrings.Arg_Invalid_LessThanOtherParameter, otherName),
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
        where T : IComparable<T>
    {
        if (value.CompareTo(default!) < 0)
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
                    string.Format(ResourceStrings.Arg_Invalid_MustBeOfType, typeof(T)),
                    paramName);
        }
        else if (value is not T)
        {
            throw new ArgumentException(
                string.Format(ResourceStrings.Arg_Invalid_MustBeOfType, typeof(T)),
                paramName);
        }
    }

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException"/> if <paramref name="value"/> is not a positive multiple
    /// of <paramref name="divisor"/>.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="divisor">The required positive divisor.</param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="value"/> &lt;= 0 or <c>value % divisor != 0</c>.
    /// </exception>
    /// <remarks>Useful for validating aligned buffer sizes, memory boundaries, or block-aligned lengths.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNotPositiveMultipleOf(
        int value, int divisor,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value <= 0 || value % divisor != 0)
            throw new ArgumentOutOfRangeException(
                paramName,
                string.Format(ResourceStrings.Arg_Invalid_PositiveMultipleOf, divisor));
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
        where T : IEquatable<T>
    {
        if (!value.Equals(default!))
            throw new ArgumentOutOfRangeException(paramName, ResourceStrings.Arg_OutOfRange_RequireZero);
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
        T value,
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
        T value, string message,
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
                    string.Format(ResourceStrings.Arg_OutOfRange_RequireBetweenInclusive, min, max));
        }
        else
        {
            if (value.CompareTo(min) <= 0 || value.CompareTo(max) >= 0)
                throw new ArgumentOutOfRangeException(
                    paramName,
                    string.Format(ResourceStrings.Arg_OutOfRange_RequireBetweenExclusive, min, max));
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
        where T : IComparable<T>
    {
        if (value.CompareTo(default!) > 0)
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
                string.Format(ResourceStrings.Arg_OutOfRange_SequenceRangeOverflow, nameof(Int32)));
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
                string.Format(ResourceStrings.Arg_OutOfRange_SequenceRangeOverflow, nameof(Int64)));
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
                string.Format(ResourceStrings.Arg_Invalid_SpanTooShort, requiredLength),
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
                string.Format(ResourceStrings.Arg_Invalid_SpanTooShort, requiredLength),
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
        if ((throwIfZero && span.Length == 0) || span.Length % divisor != 0)
            throw new ArgumentException(
                string.Format(ResourceStrings.Arg_Invalid_SpanLengthMultipleOf, divisor),
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
                string.Format(ResourceStrings.Arg_Invalid_SpanLengthMultipleOf, divisor),
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
        where T : IEquatable<T>
    {
        if (value.Equals(default!))
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
        where T : IComparable<T>
    {
        if (value.CompareTo(default!) <= 0)
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
        where T : IComparable<T>
    {
        if (value.CompareTo(default!) >= 0)
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
    public static void ThrowIsNullOrWhiteSpace(
        string value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value is null)
            throw new ArgumentNullException(paramName);

        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException(ResourceStrings.Arg_Invalid_StringEmptyOrWhitespace, paramName);
    }
}

#pragma warning restore SA1117
#endif
