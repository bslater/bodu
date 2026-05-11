// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CryptoHelpers.ThrowHelper.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public static partial class CryptoHelpers
{
    /// <summary>
    /// Throws an <see cref="InvalidOperationException"/> if associated data has already been processed.
    /// </summary>
    /// <param name="alreadyProcessed">
    /// <see langword="true"/> if associated data was already supplied; <see langword="false"/> otherwise.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="alreadyProcessed"/> is <see langword="true"/>.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfAssociatedDataAlreadyProcessed(bool alreadyProcessed)
    {
        if (alreadyProcessed)
            throw new InvalidOperationException(CryptoResourceStrings.CryptographicException_AssociatedDataAlreadyProcessed);
    }

    /// <summary>
    /// Throws a <see cref="CryptographicException"/> if associated data has not yet been processed.
    /// </summary>
    /// <param name="alreadyProcessed">
    /// <see langword="true"/> if associated data has been supplied; <see langword="false"/> if it has not.
    /// </param>
    /// <exception cref="CryptographicException">
    /// Thrown when <paramref name="alreadyProcessed"/> is <see langword="false"/>.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfAssociatedDataNotProcessed(bool alreadyProcessed)
    {
        if (!alreadyProcessed)
            throw new CryptographicException(CryptoResourceStrings.CryptographicException_AssociatedDataNotProcessed);
    }

    /// <summary>
    /// Throws an <see cref="InvalidOperationException"/> if the transform has already completed.
    /// </summary>
    /// <param name="completed">
    /// <see langword="true"/> if the transform has already produced its output; <see langword="false"/> otherwise.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="completed"/> is <see langword="true"/>.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfAlreadyCompleted(bool completed)
    {
        if (completed)
            throw new InvalidOperationException(CryptoResourceStrings.InvalidOperationException_TransformAlreadyFinalized);
    }

    /// <summary>
    /// Throws an <see cref="ArgumentException"/> if <paramref name="output"/> is smaller than
    /// <paramref name="required"/> bytes.
    /// </summary>
    /// <param name="output">The output buffer to validate.</param>
    /// <param name="required">The minimum number of bytes the buffer must hold.</param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <c>output.Length &lt; required</c>.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfOutputBufferTooSmall(
        Span<byte> output, int required,
        [CallerArgumentExpression(nameof(output))] string? paramName = null)
    {
        if (output.Length < required)
            throw new ArgumentException(
                string.Format(CryptoResourceStrings.CryptographicException_OutputBufferTooSmall, required),
                paramName);
    }

    /// <summary>
    /// Throws an <see cref="ArgumentException"/> if <paramref name="input"/> is shorter than
    /// <paramref name="tagSize"/> bytes, meaning it cannot contain a complete authentication tag.
    /// </summary>
    /// <param name="input">The ciphertext-with-tag buffer to validate.</param>
    /// <param name="tagSize">The required tag size in bytes.</param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <c>input.Length &lt; tagSize</c>.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfCiphertextTooShort(
        ReadOnlySpan<byte> input, int tagSize,
        [CallerArgumentExpression(nameof(input))] string? paramName = null)
    {
        if (input.Length < tagSize)
            throw new ArgumentException(
                string.Format(CryptoResourceStrings.CryptographicException_CiphertextTooShort, tagSize),
                paramName);
    }

    /// <summary>
    /// Throws a <see cref="CryptographicException" /> if the span length is not a valid multiple of
    /// <paramref name="divisor" />.
    /// </summary>
    /// <typeparam name="T">The element type of the span.</typeparam>
    /// <param name="span">The span whose length is validated.</param>
    /// <param name="divisor">The required positive divisor.</param>
    /// <param name="throwIfZero"><see langword="true" /> to treat an empty span as invalid; <see langword="false" /> to allow an empty span.</param>
    /// <param name="paramName">The name of the span parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="divisor" /> is zero or negative.
    /// </exception>
    /// <exception cref="CryptographicException">
    /// Thrown when <paramref name="span" /> is empty and <paramref name="throwIfZero" /> is <see langword="true" />,
    /// or when the span length is not evenly divisible by <paramref name="divisor" />.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfSpanLengthNotPositiveMultipleOf<T>(
        ReadOnlySpan<T> span, int divisor, bool throwIfZero = true,
        [CallerArgumentExpression(nameof(span))] string? paramName = null)
    {
        ThrowHelper.ThrowIfZeroOrNegative(divisor);

        if ((throwIfZero && span.Length == 0) || span.Length % divisor != 0)
            throw new CryptographicException(
                string.Format(CryptoResourceStrings.CryptographicException_Invalid_BlockLengthMultipleOf, divisor),
                paramName);
    }

    /// <summary>
    /// Throws a <see cref="CryptographicException" /> if the specified value is not a positive multiple of
    /// <paramref name="divisor" />.
    /// </summary>
    /// <typeparam name="T">A binary integer type.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="divisor">The required positive divisor.</param>
    /// <param name="paramName">The name of the value parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="divisor" /> is zero or negative.
    /// </exception>
    /// <exception cref="CryptographicException">
    /// Thrown when <paramref name="value" /> is less than or equal to zero, or when
    /// <paramref name="value" /> is not evenly divisible by <paramref name="divisor" />.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNotPositiveMultipleOf<T>(
        T value, T divisor,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
        where T : IBinaryInteger<T>
    {
        if (divisor <= T.Zero)
            throw new ArgumentOutOfRangeException(nameof(divisor));

        if (value <= T.Zero || value % divisor != T.Zero)
            throw new CryptographicException(
                paramName,
                string.Format(CryptoResourceStrings.CryptographicException_HashSize_PositiveMultipleOf, divisor));
    }

    /// <summary>
    /// Throws an <see cref="ArgumentNullException"/> if the array is <see langword="null"/>, an
    /// <see cref="ArgumentException"/> if <paramref name="offset"/> or <paramref name="count"/> is out of
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
    /// <exception cref="ArgumentException">
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
                string.Format(ResourceStrings.Arg_Invalid_ArrayOffset, paramOffsetName));

        if (count < 0 || count > array.Length)
            throw new ArgumentException(
                string.Format(ResourceStrings.Arg_Invalid_ArrayOffset, paramCountName),
                paramCountName);

        if (count > array.Length - offset)
            throw new ArgumentException(
                string.Format(
                    ResourceStrings.Arg_Invalid_ArrayOffsetOrLength,
                    paramOffsetName,
                    paramCountName,
                    paramArrayName));
    }

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException" /> if the specified hash size is not one of the permitted hash sizes.
    /// </summary>
    /// <param name="hashSize">
    /// The hash size, in bits, to validate.
    /// </param>
    /// <param name="permittedHashSizes">
    /// The set of valid hash sizes, in bits.
    /// </param>
    /// <param name="paramHashSizeName">
    /// The name of the hash-size parameter. Supplied automatically by the compiler.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="permittedHashSizes" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="hashSize" /> is not present in <paramref name="permittedHashSizes" />.
    /// </exception>
    public static void ThrowIfInvalidHashSize(
        int hashSize,
        int[] permittedHashSizes,
        [CallerArgumentExpression(nameof(hashSize))] string? paramHashSizeName = null)
    {
        ThrowHelper.ThrowIfNull(permittedHashSizes);

        if (Array.IndexOf(permittedHashSizes, hashSize) == -1)
            throw new ArgumentOutOfRangeException(
                paramHashSizeName,
                string.Format(
                    CryptoResourceStrings.CryptographicException_InvalidHashSize,
                    hashSize,
                    string.Join(", ", permittedHashSizes)));
    }

    /// <summary>
    /// Throws an <see cref="ArgumentException"/> if the length of <paramref name="iv"/> does not equal
    /// <paramref name="expectedLength"/>.
    /// </summary>
    /// <param name="iv">The initialisation vector to validate.</param>
    /// <param name="expectedLength">The required length in bytes.</param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="iv"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <c>iv.Length != expectedLength</c>.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfIvLengthInvalid(
        byte[] iv, int expectedLength,
        [CallerArgumentExpression(nameof(iv))] string? paramName = null)
    {
        ThrowHelper.ThrowIfNull(iv, paramName);
        if (iv.Length != expectedLength)
            throw new ArgumentException(
                string.Format(CryptoResourceStrings.ArgumentException_InvalidIvLength, iv.Length, expectedLength),
                paramName);
    }
}
