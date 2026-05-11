// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CryptoHelpers.ThrowHelper.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
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
    /// Throws an <see cref="ArgumentException"/> if the length of <paramref name="iv"/> does not equal
    /// <paramref name="expectedLength"/>.
    /// </summary>
    /// <param name="iv">The initialisation vector to validate.</param>
    /// <param name="expectedLength">The required length in bytes.</param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <c>iv.Length != expectedLength</c>.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfIvLengthInvalid(
        byte[] iv, int expectedLength,
        [CallerArgumentExpression(nameof(iv))] string? paramName = null)
    {
        if (iv.Length != expectedLength)
            throw new ArgumentException(
                string.Format(CryptoResourceStrings.ArgumentException_InvalidIvLength, iv.Length, expectedLength),
                paramName);
    }
}
