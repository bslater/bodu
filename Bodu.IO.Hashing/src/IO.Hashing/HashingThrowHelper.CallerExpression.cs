// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HashingThrowHelper.CallerExpression.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Bodu.IO.Hashing;

internal static partial class HashingThrowHelper
{
    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException" /> when <paramref name="value" /> does not fit within
    /// <paramref name="size" /> bits.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="size">The permitted width, in bits.</param>
    /// <param name="widthMask">
    /// The mask representing the largest value permitted for <paramref name="size" /> bits.
    /// </param>
    /// <param name="paramName">The parameter name reported in the exception; inferred from the call site.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="value" /> is greater than <paramref name="widthMask" />.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowIfExceedsBitWidth(
        ulong value,
        int size,
        ulong widthMask,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value > widthMask)
            throw new ArgumentOutOfRangeException(
                paramName,
                value,
                string.Format(
                    CultureInfo.CurrentCulture,
                    HashingResourceStrings.Arg_OutOfRange_ValueExceedsBitWidth,
                    size,
                    widthMask));
    }

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException" /> when <paramref name="hashSize" /> is not present in
    /// <paramref name="validHashSizes" />.
    /// </summary>
    /// <param name="hashSize">The hash size requested by the caller, in bits.</param>
    /// <param name="validHashSizes">The set of supported hash sizes for the algorithm.</param>
    /// <param name="paramName">The parameter name reported in the exception; inferred from the call site.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="hashSize" /> is not contained in <paramref name="validHashSizes" />.
    /// </exception>
    internal static void ThrowIfInvalidHashSize(
        int hashSize,
        int[] validHashSizes,
        [CallerArgumentExpression(nameof(hashSize))] string? paramName = null)
    {
        if (Array.IndexOf(validHashSizes, hashSize) == -1)
            throw new ArgumentOutOfRangeException(
                paramName,
                hashSize,
                string.Format(
                    CultureInfo.CurrentCulture,
                    HashingResourceStrings.Arg_OutOfRange_HashSize,
                    hashSize,
                    string.Join(", ", validHashSizes)));
    }

    /// <summary>
    /// Throws an <see cref="ArgumentException" /> when <paramref name="actualLength" /> is less than
    /// <paramref name="requiredLength" />.
    /// </summary>
    /// <param name="actualLength">The available length of the destination span.</param>
    /// <param name="requiredLength">The minimum length that the destination span must provide.</param>
    /// <param name="paramName">The parameter name reported in the exception; inferred from the call site.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="actualLength" /> is less than <paramref name="requiredLength" />.
    /// </exception>
    internal static void ThrowIfDestinationSpanTooShort(
        int actualLength,
        int requiredLength,
        [CallerArgumentExpression(nameof(actualLength))] string? paramName = null)
    {
        if (actualLength < requiredLength)
            throw new ArgumentException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    HashingResourceStrings.Arg_Invalid_DestinationSpanTooShortChars,
                    requiredLength),
                paramName);
    }

    /// <summary>
    /// Throws a <see cref="CryptographicUnexpectedOperationException" /> when <paramref name="started" /> is
    /// <see langword="true" />, indicating the algorithm has begun consuming input and cannot be reconfigured.
    /// </summary>
    /// <param name="started">A flag indicating whether the algorithm has begun consuming input.</param>
    /// <exception cref="CryptographicUnexpectedOperationException">
    /// Thrown when <paramref name="started" /> is <see langword="true" />.
    /// </exception>
    internal static void ThrowIfAlgorithmAlreadyStarted(bool started)
    {
        if (started)
            throw new CryptographicUnexpectedOperationException(
                HashingResourceStrings.Op_Invalid_AlgorithmAlreadyStarted);
    }
}
