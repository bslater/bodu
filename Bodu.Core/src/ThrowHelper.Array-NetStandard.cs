// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelper.Array.NetStandard.cs" company="Bodu Pty. Ltd.">
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
    /// Throws an <see cref="ArgumentNullException" /> if the array is <see langword="null" />, or an
    /// <see cref="ArgumentException" /> if it contains any non-numeric element.
    /// </summary>
    /// <param name="array">The array to validate. Must not be <see langword="null" />.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="array" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when any element in <paramref name="array" /> is not a recognized numeric type.
    /// </exception>
    /// <remarks>
    /// Validates that each non-null element is one of the primitive numeric types: <see cref="byte" />,
    /// <see cref="sbyte" />, <see cref="short" />, <see cref="ushort" />, <see cref="int" />, <see cref="uint" />,
    /// <see cref="long" />, <see cref="ulong" />, <see cref="float" />, <see cref="double" />, or
    /// <see cref="decimal" />. Nullable wrappers are unwrapped before the type check is applied.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfArrayContainsNonNumeric(Array array)
    {
        if (array is null)
            throw new ArgumentNullException(nameof(array));

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
                throw new ArgumentException(ResourceStrings.Arg_Invalid_ArrayNumericOnly, nameof(array));
            }
        }
    }

    /// <summary>
    /// Throws an <see cref="ArgumentNullException" /> if the array is <see langword="null" />, or an
    /// <see cref="ArgumentException" /> if it is not single-dimensional.
    /// </summary>
    /// <param name="array">The array to validate. Must not be <see langword="null" />.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="array" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="array" /> has a rank other than 1.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfArrayMultidimensional(Array array)
    {
        if (array is null)
            throw new ArgumentNullException(nameof(array));

        if (array.Rank != 1)
            throw new ArgumentException(ResourceStrings.Arg_Invalid_RankMultiDimensionArray, nameof(array));
    }

    /// <summary>
    /// Throws an <see cref="ArgumentNullException" /> if the array is <see langword="null" />, or an
    /// <see cref="ArgumentException" /> if it does not have a zero lower bound.
    /// </summary>
    /// <param name="array">The array to validate. Must not be <see langword="null" />.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="array" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <c>array.GetLowerBound(0) != 0</c>.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfArrayIsNotZeroBased(Array array)
    {
        if (array is null)
            throw new ArgumentNullException(nameof(array));

        if (array.GetLowerBound(0) != 0)
            throw new ArgumentException(ResourceStrings.Arg_Invalid_ArrayNonZeroLowerBound, nameof(array));
    }

    /// <summary>
    /// Throws an exception if the specified <paramref name="array" /> does not have exactly
    /// <paramref name="expectedLength" /> elements.
    /// </summary>
    /// <param name="array">The array to validate. Must not be <see langword="null" />.</param>
    /// <param name="expectedLength">The exact number of elements that <paramref name="array" /> must contain.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="array" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <c>array.Length</c> does not equal <paramref name="expectedLength" />.
    /// </exception>
    /// <remarks>
    /// Commonly used in cryptographic APIs or buffer transformations where a fixed-size input is mandatory
    /// (e.g. 16 bytes for a cipher block, 32 bytes for a key).
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfArrayLengthIsNotEqualTo(Array array, int expectedLength)
    {
        if (array is null)
            throw new ArgumentNullException(nameof(array));

        if (array.Length != expectedLength)
            throw new ArgumentException(
                string.Format(CultureInfo.CurrentCulture, ResourceStrings.Arg_Invalid_ArrayLength, expectedLength),
                nameof(array));
    }

    /// <summary>
    /// Throws an exception if the specified <paramref name="array" /> has fewer than
    /// <paramref name="minimumLength" /> elements.
    /// </summary>
    /// <param name="array">The array to validate. Must not be <see langword="null" />.</param>
    /// <param name="minimumLength">
    /// The minimum number of elements that <paramref name="array" /> must contain.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="array" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <c>array.Length</c> is less than <paramref name="minimumLength" />.
    /// </exception>
    /// <remarks>
    /// Use this overload when the caller may supply a larger array than required and the excess elements
    /// are simply ignored — for example, a buffer that must hold at least a full cipher block but may be
    /// larger. When the length must be exact, use <see cref="ThrowIfArrayLengthIsNotEqualTo" /> instead.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfArrayLengthIsInsufficient(Array array, int minimumLength)
    {
        if (array is null)
            throw new ArgumentNullException(nameof(array));

        if (array.Length < minimumLength)
            throw new ArgumentException(
                string.Format(CultureInfo.CurrentCulture, ResourceStrings.Arg_Invalid_ArrayTooShort, minimumLength),
                nameof(array));
    }

    /// <summary>
    /// Throws an <see cref="ArgumentNullException" /> if the array is <see langword="null" />, an
    /// <see cref="ArgumentOutOfRangeException" /> if <paramref name="index" /> is negative, or an
    /// <see cref="ArgumentException" /> if the array does not have enough elements from <paramref name="index" /> to
    /// accommodate <paramref name="requiredLength" /> elements.
    /// </summary>
    /// <param name="array">The array to validate. Must not be <see langword="null" />.</param>
    /// <param name="index">The zero-based starting index within the array. Must be zero or greater.</param>
    /// <param name="requiredLength">The number of elements required starting from <paramref name="index" />.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="array" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="index" /> is negative.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <c>array.Length - index &lt; requiredLength</c>.
    /// </exception>
    /// <remarks>
    /// Ensures that a caller can safely access a contiguous block of <paramref name="requiredLength" /> elements
    /// starting at <paramref name="index" /> without exceeding array bounds.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfArrayLengthIsInsufficient(Array array, int index, int requiredLength)
    {
        if (array is null)
            throw new ArgumentNullException(nameof(array));

        if (index < 0)
            throw new ArgumentOutOfRangeException(
                nameof(index),
                string.Format(CultureInfo.CurrentCulture, ResourceStrings.Arg_OutOfRange_IndexValidRange, nameof(array)));

        if (array.Length - index < requiredLength)
            throw new ArgumentException(
                string.Format(CultureInfo.CurrentCulture, ResourceStrings.Arg_Invalid_ArrayTooShort, requiredLength),
                nameof(array));
    }

    /// <summary>
    /// Throws an <see cref="ArgumentNullException" /> if the array is <see langword="null" />, or an
    /// <see cref="ArgumentException" /> if it has zero length.
    /// </summary>
    /// <param name="array">The array to validate. Must not be <see langword="null" />.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="array" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <c>array.Length == 0</c>.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfArrayLengthIsZero(Array array)
    {
        if (array is null)
            throw new ArgumentNullException(nameof(array));

        if (array.Length == 0)
            throw new ArgumentException(ResourceStrings.Arg_Invalid_ArrayIsZeroLength, nameof(array));
    }

    /// <summary>
    /// Throws an <see cref="ArgumentNullException" /> if the array is <see langword="null" />, or an
    /// <see cref="ArgumentException" /> if its length is not a positive multiple of <paramref name="divisor" />.
    /// </summary>
    /// <param name="array">The array to validate. Must not be <see langword="null" />.</param>
    /// <param name="divisor">The required positive divisor.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="array" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <c>array.Length == 0</c> or <c>array.Length % divisor != 0</c>.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfArrayLengthNotPositiveMultipleOf(Array array, int divisor)
    {
        if (array is null)
            throw new ArgumentNullException(nameof(array));

        if (array.Length == 0 || array.Length % divisor != 0)
            throw new ArgumentException(
                string.Format(CultureInfo.CurrentCulture, ResourceStrings.Arg_Invalid_ArrayLengthMultipleOf, divisor),
                nameof(array));
    }

    /// <summary>
    /// Throws an <see cref="ArgumentNullException" /> if the array is <see langword="null" />, an
    /// <see cref="ArgumentOutOfRangeException" /> if <paramref name="offset" /> or <paramref name="count" /> is out of
    /// range, or an <see cref="ArgumentException" /> if the segment they define exceeds the bounds of
    /// <paramref name="array" />.
    /// </summary>
    /// <param name="array">The array to validate. Must not be <see langword="null" />.</param>
    /// <param name="offset">The zero-based starting index within the array.</param>
    /// <param name="count">The number of elements to access from <paramref name="offset" />.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="array" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="offset" /> or <paramref name="count" /> is negative or exceeds
    /// <c>array.Length</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <c>offset + count</c> exceeds <c>array.Length</c>.
    /// </exception>
    public static void ThrowIfArrayOffsetOrCountInvalid(Array array, int offset, int count)
    {
        if (array is null)
            throw new ArgumentNullException(nameof(array));

        if (offset < 0 || offset > array.Length)
            throw new ArgumentOutOfRangeException(
                nameof(offset),
                string.Format(CultureInfo.CurrentCulture, ResourceStrings.Arg_Invalid_ArrayOffset, nameof(offset)));

        if (count < 0 || count > array.Length)
            throw new ArgumentOutOfRangeException(
                nameof(count),
                string.Format(CultureInfo.CurrentCulture, ResourceStrings.Arg_Invalid_ArrayOffset, nameof(count)));

        if (count > array.Length - offset)
            throw new ArgumentException(
                string.Format(CultureInfo.CurrentCulture, ResourceStrings.Arg_Invalid_ArrayOffsetOrLength,
                    nameof(offset), nameof(count), nameof(array)));
    }

    /// <summary>
    /// Throws an <see cref="ArgumentNullException" /> if the array is <see langword="null" />, or an
    /// <see cref="ArgumentException" /> if it is not assignable to <typeparamref name="TExpected" />[].
    /// </summary>
    /// <typeparam name="TExpected">The expected element type.</typeparam>
    /// <param name="array">The array to validate. Must not be <see langword="null" />.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="array" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="array" /> is not of type <typeparamref name="TExpected" />[].
    /// </exception>
    /// <remarks>
    /// The null guard is applied before the pattern match because a <see langword="null" /> reference satisfies
    /// <c>is not TExpected[]</c>, which would otherwise produce an <see cref="ArgumentException" /> rather than
    /// an <see cref="ArgumentNullException" />.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfArrayTypeIsNotCompatible<TExpected>(Array array)
    {
        if (array is null)
            throw new ArgumentNullException(nameof(array));

        if (array is not TExpected[])
            throw new ArgumentException(ResourceStrings.Arg_Invalid_ArrayType, nameof(array));
    }

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException" /> if <paramref name="index" /> is outside the valid range
    /// of <paramref name="array" />.
    /// </summary>
    /// <param name="index">The index to validate.</param>
    /// <param name="array">The array against which to validate the index.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="array" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="index" /> &lt; 0 or &gt;= <c>array.LongLength</c>.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfIndexOutOfRange(long index, Array array)
    {
        if (array is null)
            throw new ArgumentNullException(nameof(array));

        if (index < 0 || index >= array.LongLength)
            throw new ArgumentOutOfRangeException(
                nameof(index),
                string.Format(CultureInfo.CurrentCulture, ResourceStrings.Arg_OutOfRange_IndexValidRange, array.LongLength));
    }

    /// <summary>
    /// Throws an <see cref="ArgumentNullException" /> if the array is <see langword="null" />, or an
    /// <see cref="ArgumentOutOfRangeException" /> if its length is not between <paramref name="minLength" /> and
    /// <paramref name="maxLength" /> (inclusive).
    /// </summary>
    /// <param name="array">The array to validate. Must not be <see langword="null" />.</param>
    /// <param name="minLength">The minimum permitted length, inclusive. Must be zero or greater.</param>
    /// <param name="maxLength">The maximum permitted length, inclusive. Must be greater than or equal to
    /// <paramref name="minLength" />.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="array" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <c>array.Length &lt; minLength</c> or <c>array.Length &gt; maxLength</c>.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfArrayLengthOutOfRange(Array array, int minLength, int maxLength)
    {
        if (array is null)
            throw new ArgumentNullException(nameof(array));
 
        if (array.Length < minLength || array.Length > maxLength)
            throw new ArgumentOutOfRangeException(
                nameof(array),
                string.Format(CultureInfo.CurrentCulture, ResourceStrings.Arg_OutOfRange_RequireBetweenInclusive, minLength, maxLength));
    }
}

#endif
