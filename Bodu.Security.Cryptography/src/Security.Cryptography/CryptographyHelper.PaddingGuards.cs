// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CryptographyHelper.PaddingGuards.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides padding-related always-throw guards and stream-buffer accessors that complement the <c>ThrowIf*</c>
/// validators living in the <see cref="CryptographyHelper" /> ThrowHelper partials.
/// </summary>
internal static partial class CryptographyHelper
{
    /// <summary>
    /// Throws a <see cref="CryptographicException" /> indicating that the input contains invalid padding for the
    /// specified scheme.
    /// </summary>
    /// <param name="paddingScheme">
    /// The name of the padding scheme reported in the exception message (for example <c>"PKCS#7"</c>).
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="paddingScheme" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="CryptographicException">
    /// Always thrown when invoked; the exception message identifies the failing <paramref name="paddingScheme" />.
    /// </exception>
    /// <remarks>
    /// Intended for use after a constant-time padding validation has determined that the trailing bytes do not match
    /// the expected layout. The exception type matches the framework convention for invalid padding so that consumers
    /// can catch <see cref="CryptographicException" /> uniformly.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [System.Diagnostics.CodeAnalysis.DoesNotReturn]
    public static void ThrowInvalidPadding(string paddingScheme)
    {
        ThrowHelper.ThrowIfNull(paddingScheme);
        throw new CryptographicException(
            string.Format(CultureInfo.CurrentCulture, CryptoResourceStrings.Crypt_Invalid_PaddingScheme, paddingScheme));
    }

    /// <summary>
    /// Throws an <see cref="ArgumentException" /> indicating that the supplied input is not a valid padded block
    /// sequence for the specified scheme.
    /// </summary>
    /// <param name="paddingScheme">
    /// The name of the padding scheme reported in the exception message (for example <c>"PKCS#7"</c>).
    /// </param>
    /// <param name="paramName">The name of the parameter whose value was rejected.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="paddingScheme" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Always thrown when invoked; the exception message identifies the failing <paramref name="paddingScheme" />.
    /// </exception>
    /// <remarks>
    /// Used by <c>Unpad</c> entry points when the input length is not a positive multiple of the block size, signaling
    /// that the caller passed something other than the output of a matching <c>Pad</c> operation.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [System.Diagnostics.CodeAnalysis.DoesNotReturn]
    public static void ThrowInvalidPaddedSequence(string paddingScheme, string? paramName)
    {
        ThrowHelper.ThrowIfNull(paddingScheme);
        throw new ArgumentException(
            string.Format(CultureInfo.CurrentCulture, CryptoResourceStrings.Arg_Invalid_PaddedSequence, paddingScheme),
            paramName);
    }

    /// <summary>
    /// Throws a <see cref="CryptographicException" /> indicating that the specified padding mode is not supported.
    /// </summary>
    /// <typeparam name="T">
    /// The enumeration type representing the padding mode (for example <see cref="PaddingMode" /> or
    /// <see cref="PaddingModeKind" />).
    /// </typeparam>
    /// <param name="mode">The unsupported padding mode value.</param>
    /// <exception cref="CryptographicException">
    /// Always thrown when invoked; the exception message identifies the rejected <paramref name="mode" />.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [System.Diagnostics.CodeAnalysis.DoesNotReturn]
    public static void ThrowUnsupportedPaddingMode<T>(T mode)
        where T : struct, Enum =>
        throw new CryptographicException(
            string.Format(CultureInfo.CurrentCulture, CryptoResourceStrings.Crypt_Invalid_UnsupportedPaddingMode, mode));

    /// <summary>
    /// Retrieves the underlying buffer of <paramref name="stream" /> via
    /// <see cref="MemoryStream.TryGetBuffer(out ArraySegment{byte})" />, or throws an
    /// <see cref="InvalidOperationException" /> if the buffer is not exposed.
    /// </summary>
    /// <param name="stream">The <see cref="MemoryStream" /> whose buffer is required.</param>
    /// <returns>The <see cref="ArraySegment{T}" /> describing the active portion of the stream's buffer.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="stream" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the supplied <see cref="MemoryStream" /> was constructed in a mode that does not expose its
    /// underlying buffer.
    /// </exception>
    /// <remarks>
    /// Used by transform helpers that rely on zero-copy access to the stream's buffer; the failure mode corresponds to
    /// the <see cref="MemoryStream(byte[], bool)" /> overload used to suppress buffer publication.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ArraySegment<byte> GetBufferOrThrowIfInaccessible(MemoryStream stream)
    {
        ThrowHelper.ThrowIfNull(stream);
        return !stream.TryGetBuffer(out ArraySegment<byte> segment)
            ? throw new InvalidOperationException(
                CryptoResourceStrings.Op_Invalid_MemoryStreamBufferInaccessible)
            : segment;
    }
}
