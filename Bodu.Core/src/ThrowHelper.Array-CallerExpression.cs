// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelper.Array.CallerExpression.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

#if !NETSTANDARD2_0_OR_GREATER
#pragma warning disable SA1117 // Parameters should be on same line or separate lines
#pragma warning disable IDE0011 // Add braces

using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace Bodu;

public static partial class ThrowHelper
{
    /// <summary>
    /// Cached parsed format for <see cref="ResourceStrings.Arg_Invalid_ArrayLength" />.
    /// </summary>
    private static readonly CompositeFormat s_argInvalidArrayLength =
        CompositeFormat.Parse(ResourceStrings.Arg_Invalid_ArrayLength);

    /// <summary>
    /// Cached parsed format for <see cref="ResourceStrings.Arg_Invalid_ArrayLengthMultipleOf" />.
    /// </summary>
    private static readonly CompositeFormat s_argInvalidArrayLengthMultipleOf =
        CompositeFormat.Parse(ResourceStrings.Arg_Invalid_ArrayLengthMultipleOf);

    /// <summary>
    /// Cached parsed format for <see cref="ResourceStrings.Arg_Invalid_ArrayOffset" />.
    /// </summary>
    private static readonly CompositeFormat s_argInvalidArrayOffset =
        CompositeFormat.Parse(ResourceStrings.Arg_Invalid_ArrayOffset);

    /// <summary>
    /// Cached parsed format for <see cref="ResourceStrings.Arg_Invalid_ArrayOffsetOrLength" />.
    /// </summary>
    private static readonly CompositeFormat s_argInvalidArrayOffsetOrLength =
        CompositeFormat.Parse(ResourceStrings.Arg_Invalid_ArrayOffsetOrLength);

    /// <summary>
    /// Cached parsed format for <see cref="ResourceStrings.Arg_Invalid_ArrayTooShort" />.
    /// </summary>
    private static readonly CompositeFormat s_argInvalidArrayTooShort =
        CompositeFormat.Parse(ResourceStrings.Arg_Invalid_ArrayTooShort);

    /// <summary>
    /// Cached parsed format for <see cref="ResourceStrings.Arg_OutOfRange_IndexValidRange" />.
    /// </summary>
    private static readonly CompositeFormat s_argOutOfRangeIndexValidRange =
        CompositeFormat.Parse(ResourceStrings.Arg_OutOfRange_IndexValidRange);

    /// <summary>
    /// Throws an <see cref="ArgumentNullException" /> if the array is <see langword="null" />, or an
    /// <see cref="ArgumentException" /> if it contains any non-numeric element.
    /// </summary>
    /// <param name="array">The array to validate. Must not be <see langword="null" />.</param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
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
                throw new ArgumentException(ResourceStrings.Arg_Invalid_ArrayNumericOnly, paramName);
            }
        }
    }

    /// <summary>
    /// Throws an <see cref="ArgumentNullException" /> if the array is <see langword="null" />, or an
    /// <see cref="ArgumentException" /> if it is not single-dimensional.
    /// </summary>
    /// <param name="array">The array to validate. Must not be <see langword="null" />.</param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="array" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="array" /> has a rank other than 1.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfArrayMultidimensional(
        Array? array,
        [CallerArgumentExpression(nameof(array))] string? paramName = null)
    {
        if (array is null)
            throw new ArgumentNullException(paramName);

        if (array.Rank != 1)
            throw new ArgumentException(ResourceStrings.Arg_Invalid_RankMultiDimensionArray, paramName);
    }

    /// <summary>
    /// Throws an <see cref="ArgumentNullException" /> if the array is <see langword="null" />, or an
    /// <see cref="ArgumentException" /> if it does not have a zero lower bound.
    /// </summary>
    /// <param name="array">The array to validate. Must not be <see langword="null" />.</param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="array" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <c>array.GetLowerBound(0) != 0</c>.</exception>
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
    /// Throws an exception if the specified <paramref name="array" /> does not have exactly
    /// <paramref name="expectedLength" /> elements.
    /// </summary>
    /// <param name="array">The array to validate. Must not be <see langword="null" />.</param>
    /// <param name="expectedLength">The exact number of elements that <paramref name="array" /> must contain.</param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="array" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <c>array.Length</c> does not equal <paramref name="expectedLength" />.
    /// </exception>
    /// <remarks>
    /// Commonly used in cryptographic APIs or buffer transformations where a fixed-size input is mandatory (e.g. 16
    /// bytes for a cipher block, 32 bytes for a key).
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
    /// Throws an exception if the specified <paramref name="array" /> has fewer than <paramref name="minimumLength" />
    /// elements.
    /// </summary>
    /// <param name="array">The array to validate. Must not be <see langword="null" />.</param>
    /// <param name="minimumLength">The minimum number of elements that <paramref name="array" /> must contain.</param>
    /// <param name="paramName">
    /// The name of the array parameter. Supplied automatically by the compiler via
    /// <see cref="CallerArgumentExpressionAttribute" />.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="array" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <c>array.Length</c> is less than <paramref name="minimumLength" />.
    /// </exception>
    /// <remarks>
    /// Use this overload when the caller may supply a larger array than required and the excess elements are simply
    /// ignored — for example, a buffer that must hold at least a full cipher block but may be larger. When the length
    /// must be exact, use <see cref="ThrowIfArrayLengthIsNotEqualTo" /> instead.
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
    /// Throws an <see cref="ArgumentNullException" /> if the array is <see langword="null" />, or an
    /// <see cref="ArgumentException" /> if it has zero length.
    /// </summary>
    /// <param name="array">The array to validate. Must not be <see langword="null" />.</param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="array" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <c>array.Length == 0</c>.</exception>
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
    /// Throws an <see cref="ArgumentNullException" /> if the array is <see langword="null" />, or an
    /// <see cref="ArgumentOutOfRangeException" /> if its length is not between <paramref name="minLength" /> and
    /// <paramref name="maxLength" /> (inclusive).
    /// </summary>
    /// <param name="array">The array to validate. Must not be <see langword="null" />.</param>
    /// <param name="minLength">The minimum permitted length, inclusive. Must be zero or greater.</param>
    /// <param name="maxLength">
    /// The maximum permitted length, inclusive. Must be greater than or equal to <paramref name="minLength" />.
    /// </param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="array" /> is <see langword="null" />.
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
    /// Throws an <see cref="ArgumentNullException" /> if the array is <see langword="null" />, or an
    /// <see cref="ArgumentException" /> if its length is not a positive multiple of <paramref name="divisor" />.
    /// </summary>
    /// <param name="array">The array to validate. Must not be <see langword="null" />.</param>
    /// <param name="divisor">The required positive divisor.</param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="array" /> is <see langword="null" />.
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
    /// Throws an <see cref="ArgumentNullException" /> if the array is <see langword="null" />, an
    /// <see cref="ArgumentOutOfRangeException" /> if <paramref name="offset" /> or <paramref name="count" /> is out of
    /// range, or an <see cref="ArgumentException" /> if the segment they define exceeds the bounds of
    /// <paramref name="array" /> .
    /// </summary>
    /// <param name="array">The array to validate. Must not be <see langword="null" />.</param>
    /// <param name="offset">The zero-based starting index within the array.</param>
    /// <param name="count">The number of elements to access from <paramref name="offset" />.</param>
    /// <param name="paramArrayName">The name of the array parameter. Supplied automatically by the compiler.</param>
    /// <param name="paramOffsetName">The name of the index parameter. Supplied automatically by the compiler.</param>
    /// <param name="paramCountName">The name of the count parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="array" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="offset" /> or <paramref name="count" /> is negative or exceeds <c>array.Length</c>.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <c>index + count</c> exceeds <c>array.Length</c>.</exception>
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
    /// Throws an <see cref="ArgumentNullException" /> if the array is <see langword="null" />, or an
    /// <see cref="ArgumentException" /> if it is not assignable to <typeparamref name="TExpected" />[].
    /// </summary>
    /// <typeparam name="TExpected">The expected element type.</typeparam>
    /// <param name="array">The array to validate. Must not be <see langword="null" />.</param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="array" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="array" /> is not of type <typeparamref name="TExpected" />[].
    /// </exception>
    /// <remarks>
    /// The null guard is applied before the pattern match because a <see langword="null" /> reference satisfies
    /// <c>is not TExpected[]</c>, which would otherwise produce an <see cref="ArgumentException" /> rather than an
    /// <see cref="ArgumentNullException" /> .
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
    /// Throws an <see cref="ArgumentOutOfRangeException" /> if <paramref name="index" /> is outside the valid range of
    /// <paramref name="array" />.
    /// </summary>
    /// <param name="index">The index to validate.</param>
    /// <param name="array">The array against which to validate the index.</param>
    /// <param name="paramName">The name of the index parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="array" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="index" /> &lt; 0 or &gt;= <c>array.LongLength</c>.
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
}

#endif
