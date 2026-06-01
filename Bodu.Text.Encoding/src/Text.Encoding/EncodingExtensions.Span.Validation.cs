// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EncodingExtensions.Span.Validation.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;

namespace Bodu.Text.Encoding;

public static partial class EncodingExtensions
{
    /// <summary>
    /// Throws an <see cref="ArgumentException" /> when <paramref name="destination" /> is too small to receive the
    /// bytes produced by encoding <paramref name="chars" /> with <paramref name="encoding" />.
    /// </summary>
    /// <param name="destination">The destination span to validate.</param>
    /// <param name="chars">The character span whose encoded length is computed.</param>
    /// <param name="encoding">The encoding used to compute the required size.</param>
    /// <param name="paramName">
    /// The parameter name reported in the exception; inferred from the call site when not specified.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="encoding" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="destination" />.Length is less than
    /// <see cref="System.Text.Encoding.GetByteCount(ReadOnlySpan{char})" /> for <paramref name="chars" />.
    /// </exception>
    public static void ThrowIfTooSmallForEncoding(
        this Span<byte> destination,
        ReadOnlySpan<char> chars,
        System.Text.Encoding encoding,
        [CallerArgumentExpression(nameof(destination))] string? paramName = null)
    {
        ThrowHelper.ThrowIfNull(encoding);

        var required = encoding.GetByteCount(chars);
        if (destination.Length < required)
            throw new ArgumentException(
                EncodingResourceStrings.Arg_Invalid_DestinationTooSmallForEncoded,
                paramName);
    }

    /// <summary>
    /// Throws an <see cref="ArgumentException" /> when <paramref name="destination" /> is too small to receive the
    /// characters produced by decoding <paramref name="bytes" /> with <paramref name="encoding" />.
    /// </summary>
    /// <param name="destination">The destination span to validate.</param>
    /// <param name="bytes">The byte span whose decoded length is computed.</param>
    /// <param name="encoding">The encoding used to compute the required size.</param>
    /// <param name="paramName">
    /// The parameter name reported in the exception; inferred from the call site when not specified.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="encoding" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="destination" />.Length is less than
    /// <see cref="System.Text.Encoding.GetCharCount(ReadOnlySpan{byte})" /> for <paramref name="bytes" />.
    /// </exception>
    public static void ThrowIfTooSmallForDecoding(
        this Span<char> destination,
        ReadOnlySpan<byte> bytes,
        System.Text.Encoding encoding,
        [CallerArgumentExpression(nameof(destination))] string? paramName = null)
    {
        ThrowHelper.ThrowIfNull(encoding);

        var required = encoding.GetCharCount(bytes);
        if (destination.Length < required)
            throw new ArgumentException(
                EncodingResourceStrings.Arg_Invalid_DestinationTooSmallForDecoded,
                paramName);
    }
}
