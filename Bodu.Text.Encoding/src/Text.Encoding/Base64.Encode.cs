// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base64.Encode.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;

namespace Bodu.Text.Encoding;

public static partial class Base64
{
    /// <summary>
    /// Encodes the entire byte array into a Base64 string.
    /// </summary>
    /// <param name="bytes">The byte array to encode.</param>
    /// <param name="variant">The Base64 variant.</param>
    /// <param name="options">
    /// Formatting options. Only <see cref="BaseFormattingOptions.InsertLineBreaks" /> and
    /// <see cref="BaseFormattingOptions.OmitPadding" /> have an effect on Base64.
    /// </param>
    /// <returns>The Base64 encoded string.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="bytes" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="variant" /> is undefined.</exception>
    public static string Encode(byte[] bytes, Base64Variant variant = Base64Variant.Standard, BaseFormattingOptions options = BaseFormattingOptions.None)
    {
        ThrowHelper.ThrowIfNull(bytes);
        return Encode(bytes.AsSpan(), variant, options);
    }

    /// <summary>
    /// Encodes a span of bytes into a Base64 string.
    /// </summary>
    /// <param name="bytes">The bytes to encode.</param>
    /// <param name="variant">The Base64 variant.</param>
    /// <param name="options">Formatting options.</param>
    /// <returns>The Base64 encoded string.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="variant" /> is undefined.</exception>
    public static string Encode(ReadOnlySpan<byte> bytes, Base64Variant variant = Base64Variant.Standard, BaseFormattingOptions options = BaseFormattingOptions.None)
    {
        EnsureValidVariant(variant);

        if (bytes.IsEmpty)
            return string.Empty;

        var required = GetEncodedLength(bytes.Length, variant, options);
        var buffer = ArrayPool<char>.Shared.Rent(required);
        try
        {
            var written = EncodeIntoSpan(bytes, buffer, variant, options);
            return new string(buffer, 0, written);
        }
        finally
        {
            ArrayPool<char>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// Encodes a span of bytes directly into a destination character span.
    /// </summary>
    /// <param name="bytes">The bytes to encode.</param>
    /// <param name="destination">
    /// The destination span. Must be at least
    /// <see cref="GetEncodedLength(int, Base64Variant, BaseFormattingOptions)" /> characters in size.
    /// </param>
    /// <param name="variant">The Base64 variant.</param>
    /// <param name="options">Formatting options.</param>
    /// <returns>The number of characters written.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="destination" /> is too small.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="variant" /> is undefined.</exception>
    public static int Encode(ReadOnlySpan<byte> bytes, Span<char> destination, Base64Variant variant = Base64Variant.Standard, BaseFormattingOptions options = BaseFormattingOptions.None)
    {
        EnsureValidVariant(variant);

        var required = GetEncodedLength(bytes.Length, variant, options);
        return destination.Length >= required
            ? bytes.IsEmpty
                ? 0
                : EncodeIntoSpan(bytes, destination, variant, options)
            : throw new ArgumentException(EncodingResourceStrings.Arg_Invalid_DestinationTooSmallForEncoded, nameof(destination));
    }

    /// <summary>
    /// Encodes a portion of a byte array into a Base64 string.
    /// </summary>
    /// <param name="bytes">The byte array to encode.</param>
    /// <param name="offset">The starting offset.</param>
    /// <param name="count">The number of bytes to encode.</param>
    /// <param name="variant">The Base64 variant.</param>
    /// <param name="options">Formatting options.</param>
    /// <returns>The Base64 encoded string.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="bytes" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="offset" /> or <paramref name="count" /> is out of range, or when
    /// <paramref name="variant" /> is undefined.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the segment defined by <paramref name="offset" /> and <paramref name="count" /> exceeds the
    /// available range of <paramref name="bytes" />.
    /// </exception>
    public static string Encode(byte[] bytes, int offset, int count, Base64Variant variant = Base64Variant.Standard, BaseFormattingOptions options = BaseFormattingOptions.None)
    {
        ThrowHelper.ThrowIfArrayOffsetOrCountInvalid(bytes, offset, count);
        return Encode(bytes.AsSpan(offset, count), variant, options);
    }

    /// <summary>
    /// Attempts to encode a span of bytes into a destination character span.
    /// </summary>
    /// <param name="bytes">The bytes to encode.</param>
    /// <param name="destination">The destination span.</param>
    /// <param name="charsWritten">
    /// When this method returns, contains the number of characters written, or <c>0</c> when the destination is too
    /// small.
    /// </param>
    /// <param name="variant">The Base64 variant.</param>
    /// <param name="options">Formatting options.</param>
    /// <returns>
    /// <see langword="true" /> when the destination is large enough; otherwise <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="variant" /> is undefined.</exception>
    public static bool TryEncode(ReadOnlySpan<byte> bytes, Span<char> destination, out int charsWritten, Base64Variant variant = Base64Variant.Standard, BaseFormattingOptions options = BaseFormattingOptions.None)
    {
        EnsureValidVariant(variant);

        var required = GetEncodedLength(bytes.Length, variant, options);
        if (destination.Length < required)
        {
            charsWritten = 0;
            return false;
        }

        if (bytes.IsEmpty)
        {
            charsWritten = 0;
            return true;
        }

        charsWritten = EncodeIntoSpan(bytes, destination, variant, options);
        return true;
    }

    /// <summary>
    /// Applies the URL-safe alphabet swap and the padding-strip step to <paramref name="buffer" /> in place. Updates
    /// <paramref name="writtenLength" /> to reflect the post-strip length.
    /// </summary>
    /// <param name="buffer">The buffer holding the BCL-encoded characters.</param>
    /// <param name="writtenLength">The number of characters written by the BCL call. Updated on padding strip.</param>
    /// <param name="variant">The Base64 variant.</param>
    /// <param name="emitPadding">Whether trailing <c>=</c> padding should be retained.</param>
    private static void ApplyVariantTransforms(Span<char> buffer, ref int writtenLength, Base64Variant variant, bool emitPadding)
    {
        if (variant == Base64Variant.UrlSafe)
        {
            for (var i = 0; i < writtenLength; i++)
            {
                var c = buffer[i];
                if (c == '+')
                    buffer[i] = '-';
                else if (c == '/')
                    buffer[i] = '_';
            }
        }

        if (!emitPadding)
        {
            while (writtenLength > 0 && buffer[writtenLength - 1] == PaddingChar)
                writtenLength--;
        }
    }

    /// <summary>
    /// Performs the BCL-backed Base64 encode and applies variant transformations (alphabet swap and padding strip)
    /// directly into <paramref name="destination" />.
    /// </summary>
    /// <param name="bytes">The input bytes.</param>
    /// <param name="destination">The destination span.</param>
    /// <param name="variant">The Base64 variant.</param>
    /// <param name="options">The encode options.</param>
    /// <returns>The number of characters written after transformations.</returns>
    private static int EncodeIntoSpan(ReadOnlySpan<byte> bytes, Span<char> destination, Base64Variant variant, BaseFormattingOptions options)
    {
        var insertLineBreaks = ShouldInsertLineBreaks(variant, options);
        Base64FormattingOptions bclOpts = insertLineBreaks ? Base64FormattingOptions.InsertLineBreaks : Base64FormattingOptions.None;
        var emitPadding = ShouldEmitPadding(variant, options);

        // Convert.TryToBase64Chars always writes the canonical padded form. When the caller's destination is sized
        // for the unpadded URL-safe output (which is the typical case for JWT-style consumers) the BCL call would
        // fail with a too-small destination. Detect that case and route through a rented scratch buffer.
        var bclRequired = ((bytes.Length + 2) / 3) * 4;
        if (insertLineBreaks)
        {
            var breaks = (bclRequired - 1) / MimeLineLength;
            bclRequired += breaks * 2;
        }

        if (destination.Length >= bclRequired)
        {
            if (!Convert.TryToBase64Chars(bytes, destination, out var rawWritten, bclOpts))
                throw new InvalidOperationException(EncodingResourceStrings.Op_Invalid_Base64EncodeUnexpectedFailure);

            ApplyVariantTransforms(destination, ref rawWritten, variant, emitPadding);
            return rawWritten;
        }

        var scratch = System.Buffers.ArrayPool<char>.Shared.Rent(bclRequired);
        try
        {
            Span<char> scratchSpan = scratch.AsSpan(0, bclRequired);
            if (!Convert.TryToBase64Chars(bytes, scratchSpan, out var rawWritten, bclOpts))
                throw new InvalidOperationException(EncodingResourceStrings.Op_Invalid_Base64EncodeUnexpectedFailure);

            ApplyVariantTransforms(scratchSpan, ref rawWritten, variant, emitPadding);
            scratchSpan[..rawWritten].CopyTo(destination);
            return rawWritten;
        }
        finally
        {
            System.Buffers.ArrayPool<char>.Shared.Return(scratch);
        }
    }
}
