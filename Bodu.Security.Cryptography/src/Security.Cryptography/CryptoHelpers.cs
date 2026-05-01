// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CryptoHelpers.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides general-purpose utility methods used by cryptographic components and implementations within
/// <c>Bodu.Security.Cryptography</c>, including secure memory clearing, block padding and depadding, cryptographically
/// secure random byte generation, and argument validation helpers.
/// </summary>
/// <remarks>
/// <para>
/// This class is implemented as a partial type, with each file documenting a distinct facet of the surface: memory
/// clearing (<c>CryptoHelpers.DisposeHelpers.cs</c>), block padding (<c>CryptoHelpers.Padding.cs</c>), secure random byte
/// generation (<c>CryptoHelpers.RandomNumberGenerator.cs</c>), and internal argument validation helpers
/// (<c>CryptoHelpers.ThrowHelper.cs</c>).
/// </para>
/// <para>
/// Random byte generation methods use <see cref="RandomNumberGenerator" /> and operate on <see cref="Span{T}" /> where
/// possible to minimise allocations in performance-sensitive paths.
/// </para>
/// </remarks>
public static partial class CryptoHelpers
{
    /// <summary>
    /// Returns a comma-separated string of valid key sizes (in bits) from the provided <see cref="KeySizes" /> array.
    /// </summary>
    /// <param name="keySizes">An array of <see cref="KeySizes" /> objects specifying allowed key sizes.</param>
    /// <returns>A string containing all supported key sizes in ascending order, or an empty string if none.</returns>
    internal static string FormatLegalSizes(KeySizes[]? keySizes) =>
        keySizes is null || keySizes.Length == 0
            ? string.Empty
            : string.Join(", ",
                keySizes
                    .SelectMany(ks => ks.SkipSize == 0
                        ? new[] { ks.MinSize }
                        : Enumerable.Range(0, ((ks.MaxSize - ks.MinSize) / ks.SkipSize) + 1)
                            .Select(i => ks.MinSize + i * ks.SkipSize))
                    .Distinct()
                    .OrderBy(size => size));


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
            throw new CryptographicException(
                string.Format(CryptoResourceStrings.CryptographicException_Invalid_BlockLengthMultipleOf, divisor),
                paramName);
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
}
