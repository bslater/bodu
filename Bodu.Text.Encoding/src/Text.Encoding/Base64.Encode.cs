// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base64.Encode.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
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
    /// <param name="options">Formatting options. Only <see cref="BaseFormattingOptions.InsertLineBreaks" /> and
    /// <see cref="BaseFormattingOptions.OmitPadding" /> have an effect on Base64.</param>
    /// <returns>The Base64 encoded string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="bytes" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="variant" /> is undefined.</exception>
    public static string Encode(byte[] bytes, Base64Variant variant = Base64Variant.Standard, BaseFormattingOptions options = BaseFormattingOptions.None)
    {
        ThrowHelper.ThrowIfNull(bytes);
        return Encode(bytes.AsSpan(), variant, options);
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
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="bytes" /> is <see langword="null" />.</exception>
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

        int required = GetEncodedLength(bytes.Length, variant, options);
        char[] buffer = ArrayPool<char>.Shared.Rent(required);
        try
        {
            int written = EncodeIntoSpan(bytes, buffer, variant, options);
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
    /// <param name="destination">The destination span. Must be at least
    /// <see cref="GetEncodedLength(int, Base64Variant, BaseFormattingOptions)" /> characters in size.</param>
    /// <param name="variant">The Base64 variant.</param>
    /// <param name="options">Formatting options.</param>
    /// <returns>The number of characters written.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="destination" /> is too small.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="variant" /> is undefined.</exception>
    public static int Encode(ReadOnlySpan<byte> bytes, Span<char> destination, Base64Variant variant = Base64Variant.Standard, BaseFormattingOptions options = BaseFormattingOptions.None)
    {
        EnsureValidVariant(variant);

        int required = GetEncodedLength(bytes.Length, variant, options);
        if (destination.Length < required)
            throw new ArgumentException("Destination is too small to receive the encoded characters.", nameof(destination));

        if (bytes.IsEmpty)
            return 0;

        return EncodeIntoSpan(bytes, destination, variant, options);
    }

    /// <summary>
    /// Attempts to encode a span of bytes into a destination character span.
    /// </summary>
    /// <param name="bytes">The bytes to encode.</param>
    /// <param name="destination">The destination span.</param>
    /// <param name="charsWritten">When this method returns, contains the number of characters written, or <c>0</c>
    /// when the destination is too small.</param>
    /// <param name="variant">The Base64 variant.</param>
    /// <param name="options">Formatting options.</param>
    /// <returns><see langword="true" /> when the destination is large enough; otherwise <see langword="false" />.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="variant" /> is undefined.</exception>
    public static bool TryEncode(ReadOnlySpan<byte> bytes, Span<char> destination, out int charsWritten, Base64Variant variant = Base64Variant.Standard, BaseFormattingOptions options = BaseFormattingOptions.None)
    {
        EnsureValidVariant(variant);

        int required = GetEncodedLength(bytes.Length, variant, options);
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
        bool insertLineBreaks = ShouldInsertLineBreaks(variant, options);
        Base64FormattingOptions bclOpts = insertLineBreaks ? Base64FormattingOptions.InsertLineBreaks : Base64FormattingOptions.None;

        if (!Convert.TryToBase64Chars(bytes, destination, out int rawWritten, bclOpts))
            throw new InvalidOperationException("Unexpected failure while encoding Base64 characters.");

        if (variant == Base64Variant.UrlSafe)
        {
            for (int i = 0; i < rawWritten; i++)
            {
                char c = destination[i];
                if (c == '+')
                    destination[i] = '-';
                else if (c == '/')
                    destination[i] = '_';
            }
        }

        if (!ShouldEmitPadding(variant, options))
        {
            while (rawWritten > 0 && destination[rawWritten - 1] == PaddingChar)
                rawWritten--;
        }

        return rawWritten;
    }
}
