// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base16.Encode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Text.Encoding;

public static partial class Base16
{
    /// <summary>
    /// Encodes the entire byte array into a hexadecimal string.
    /// </summary>
    /// <param name="bytes">The byte array to encode.</param>
    /// <param name="options">Formatting options to apply, such as upper case, spacing, prefix, or line breaks.</param>
    /// <returns>A Base16 encoded string representing the input bytes.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="bytes" /> is <see langword="null" />.
    /// </exception>
    public static string Encode(byte[] bytes, BaseFormattingOptions options = BaseFormattingOptions.None)
    {
        ThrowHelper.ThrowIfNull(bytes);
        return Encode(bytes.AsSpan(), options);
    }

    /// <summary>
    /// Encodes a read-only span of bytes into a formatted hexadecimal string.
    /// </summary>
    /// <param name="bytes">The bytes to encode.</param>
    /// <param name="options">Formatting options to apply, such as upper case, spacing, prefix, or line breaks.</param>
    /// <returns>
    /// A Base16 encoded string. When <paramref name="bytes" /> is empty and
    /// <see cref="BaseFormattingOptions.IncludePrefix" /> is set, the result is the prefix string alone; otherwise the
    /// result is <see cref="string.Empty" />.
    /// </returns>
    /// <remarks>
    /// Provides the optimal allocation profile when no formatting flags beyond
    /// <see cref="BaseFormattingOptions.UpperCase" /> are specified. When spacing, prefix, or line break flags are
    /// requested, the implementation falls back to a <see cref="StringBuilder" />-backed writer.
    /// </remarks>
    public static string Encode(ReadOnlySpan<byte> bytes, BaseFormattingOptions options = BaseFormattingOptions.None)
    {
        if (bytes.IsEmpty)
            return options.HasFlag(BaseFormattingOptions.IncludePrefix) ? Prefix : string.Empty;

        var simpleUpper = (options & ~BaseFormattingOptions.UpperCase) == 0;
        return simpleUpper
            ? EncodeFast(bytes, options.HasFlag(BaseFormattingOptions.UpperCase))
            : EncodeWithFormatting(bytes, options);
    }

    /// <summary>
    /// Encodes a read-only span of bytes directly into a pre-allocated character destination.
    /// </summary>
    /// <param name="bytes">The bytes to encode.</param>
    /// <param name="destination">The span that receives the encoded characters.</param>
    /// <param name="options">
    /// Formatting options. Only <see cref="BaseFormattingOptions.UpperCase" /> is supported on this overload.
    /// </param>
    /// <returns>The number of characters written to <paramref name="destination" />.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="options" /> contains any flag other than
    /// <see cref="BaseFormattingOptions.UpperCase" />, or when <paramref name="destination" /> is too small to receive
    /// the encoded output.
    /// </exception>
    public static int Encode(ReadOnlySpan<byte> bytes, Span<char> destination, BaseFormattingOptions options = BaseFormattingOptions.None)
    {
        EnsureSpanOutputOptionsSupported(options);

        var required = bytes.Length * 2;
        if (destination.Length < required)
            throw new ArgumentException(EncodingResourceStrings.Arg_Invalid_DestinationTooSmallForEncoded, nameof(destination));

        if (bytes.IsEmpty)
            return 0;

        EncodeToHexCore(bytes, destination, options.HasFlag(BaseFormattingOptions.UpperCase));
        return required;
    }

    /// <summary>
    /// Encodes a portion of a byte array into a formatted hexadecimal string.
    /// </summary>
    /// <param name="bytes">The byte array to encode.</param>
    /// <param name="offset">The zero-based offset in <paramref name="bytes" /> at which to begin encoding.</param>
    /// <param name="count">The number of bytes to encode.</param>
    /// <param name="options">Formatting options to apply, such as upper case, spacing, prefix, or line breaks.</param>
    /// <returns>A Base16 encoded string representing the selected slice of <paramref name="bytes" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="bytes" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="offset" /> or <paramref name="count" /> is negative, or either exceeds the bounds of
    /// <paramref name="bytes" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the segment defined by <paramref name="offset" /> and <paramref name="count" /> exceeds the
    /// available range of <paramref name="bytes" />.
    /// </exception>
    public static string Encode(byte[] bytes, int offset, int count, BaseFormattingOptions options = BaseFormattingOptions.None)
    {
        ThrowHelper.ThrowIfArrayOffsetOrCountInvalid(bytes, offset, count);
        return Encode(bytes.AsSpan(offset, count), options);
    }

    /// <summary>
    /// Attempts to encode binary bytes into hexadecimal characters using the provided destination span.
    /// </summary>
    /// <param name="bytes">The bytes to encode.</param>
    /// <param name="destination">The span that receives the encoded characters.</param>
    /// <param name="charsWritten">
    /// When this method returns, contains the number of characters written, or <c>0</c> when
    /// <paramref name="destination" /> is too small.
    /// </param>
    /// <param name="options">
    /// Formatting options. Only <see cref="BaseFormattingOptions.UpperCase" /> is supported on this overload.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when the destination is large enough and the encoding succeeded; otherwise
    /// <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="options" /> contains any flag other than
    /// <see cref="BaseFormattingOptions.UpperCase" />.
    /// </exception>
    public static bool TryEncode(ReadOnlySpan<byte> bytes, Span<char> destination, out int charsWritten, BaseFormattingOptions options = BaseFormattingOptions.None)
    {
        EnsureSpanOutputOptionsSupported(options);

        var required = bytes.Length * 2;
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

        EncodeToHexCore(bytes, destination, options.HasFlag(BaseFormattingOptions.UpperCase));
        charsWritten = required;
        return true;
    }

    /// <summary>
    /// Encodes the entire byte array into a hexadecimal string using the supplied variant.
    /// </summary>
    /// <param name="bytes">The byte array to encode.</param>
    /// <param name="variant">The Base16 alphabet case.</param>
    /// <param name="options">Additional formatting options to apply, such as spacing, prefix, or line breaks.</param>
    /// <returns>A Base16 encoded string representing the input bytes.</returns>
    /// <remarks>
    /// This is the variant-based form of <see cref="Encode(byte[], BaseFormattingOptions)" />, sharing the uniform
    /// <c>(bytes, variant, options)</c> shape used by the other Base-N families. The case selected by
    /// <paramref name="variant" /> takes precedence over <see cref="BaseFormattingOptions.UpperCase" />.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="bytes" /> is <see langword="null" />.
    /// </exception>
    public static string Encode(byte[] bytes, Base16Variant variant, BaseFormattingOptions options = BaseFormattingOptions.None)
    {
        ThrowHelper.ThrowIfNull(bytes);
        return Encode(bytes.AsSpan(), ApplyVariant(variant, options));
    }

    /// <summary>
    /// Encodes a read-only span of bytes into a formatted hexadecimal string using the supplied variant.
    /// </summary>
    /// <param name="bytes">The bytes to encode.</param>
    /// <param name="variant">The Base16 alphabet case.</param>
    /// <param name="options">Additional formatting options to apply, such as spacing, prefix, or line breaks.</param>
    /// <returns>A Base16 encoded string.</returns>
    /// <remarks>
    /// The case selected by <paramref name="variant" /> takes precedence over
    /// <see cref="BaseFormattingOptions.UpperCase" />.
    /// </remarks>
    public static string Encode(ReadOnlySpan<byte> bytes, Base16Variant variant, BaseFormattingOptions options = BaseFormattingOptions.None) =>
        Encode(bytes, ApplyVariant(variant, options));

    /// <summary>
    /// Encodes a read-only span of bytes directly into a pre-allocated character destination using the supplied variant.
    /// </summary>
    /// <param name="bytes">The bytes to encode.</param>
    /// <param name="destination">The span that receives the encoded characters.</param>
    /// <param name="variant">The Base16 alphabet case.</param>
    /// <param name="options">
    /// Additional formatting options. Only <see cref="BaseFormattingOptions.UpperCase" /> is supported on this overload;
    /// <paramref name="variant" /> takes precedence over it for the alphabet case.
    /// </param>
    /// <returns>The number of characters written to <paramref name="destination" />.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="options" /> contains any flag other than
    /// <see cref="BaseFormattingOptions.UpperCase" />, or when <paramref name="destination" /> is too small to receive
    /// the encoded output.
    /// </exception>
    public static int Encode(ReadOnlySpan<byte> bytes, Span<char> destination, Base16Variant variant, BaseFormattingOptions options = BaseFormattingOptions.None) =>
        Encode(bytes, destination, ApplyVariant(variant, options));

    /// <summary>
    /// Encodes a portion of a byte array into a formatted hexadecimal string using the supplied variant.
    /// </summary>
    /// <param name="bytes">The byte array to encode.</param>
    /// <param name="offset">The zero-based offset in <paramref name="bytes" /> at which to begin encoding.</param>
    /// <param name="count">The number of bytes to encode.</param>
    /// <param name="variant">The Base16 alphabet case.</param>
    /// <param name="options">Additional formatting options to apply, such as spacing, prefix, or line breaks.</param>
    /// <returns>A Base16 encoded string representing the selected slice of <paramref name="bytes" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="bytes" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="offset" /> or <paramref name="count" /> is negative, or either exceeds the bounds of
    /// <paramref name="bytes" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the segment defined by <paramref name="offset" /> and <paramref name="count" /> exceeds the
    /// available range of <paramref name="bytes" />.
    /// </exception>
    public static string Encode(byte[] bytes, int offset, int count, Base16Variant variant, BaseFormattingOptions options = BaseFormattingOptions.None)
    {
        ThrowHelper.ThrowIfArrayOffsetOrCountInvalid(bytes, offset, count);
        return Encode(bytes.AsSpan(offset, count), ApplyVariant(variant, options));
    }

    /// <summary>
    /// Attempts to encode binary bytes into hexadecimal characters using the supplied variant and destination span.
    /// </summary>
    /// <param name="bytes">The bytes to encode.</param>
    /// <param name="destination">The span that receives the encoded characters.</param>
    /// <param name="charsWritten">
    /// When this method returns, contains the number of characters written, or <c>0</c> when
    /// <paramref name="destination" /> is too small.
    /// </param>
    /// <param name="variant">The Base16 alphabet case.</param>
    /// <param name="options">
    /// Additional formatting options. Only <see cref="BaseFormattingOptions.UpperCase" /> is supported on this overload;
    /// <paramref name="variant" /> takes precedence over it for the alphabet case.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when the destination is large enough and the encoding succeeded; otherwise
    /// <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="options" /> contains any flag other than
    /// <see cref="BaseFormattingOptions.UpperCase" />.
    /// </exception>
    public static bool TryEncode(ReadOnlySpan<byte> bytes, Span<char> destination, out int charsWritten, Base16Variant variant, BaseFormattingOptions options = BaseFormattingOptions.None) =>
        TryEncode(bytes, destination, out charsWritten, ApplyVariant(variant, options));

    /// <summary>
    /// Merges a <see cref="Base16Variant" /> into a set of formatting options so that the variant determines the
    /// alphabet case, overriding any existing <see cref="BaseFormattingOptions.UpperCase" /> flag.
    /// </summary>
    /// <param name="variant">The Base16 alphabet case.</param>
    /// <param name="options">The caller-supplied formatting options.</param>
    /// <returns>
    /// <paramref name="options" /> with the <see cref="BaseFormattingOptions.UpperCase" /> flag set when
    /// <paramref name="variant" /> is <see cref="Base16Variant.Upper" /> and cleared otherwise.
    /// </returns>
    private static BaseFormattingOptions ApplyVariant(Base16Variant variant, BaseFormattingOptions options) =>
        variant == Base16Variant.Upper
            ? options | BaseFormattingOptions.UpperCase
            : options & ~BaseFormattingOptions.UpperCase;

    /// <summary>
    /// Encodes a byte span into a hexadecimal string using the allocation-minimal fast path.
    /// </summary>
    /// <param name="bytes">The byte span to encode.</param>
    /// <param name="upper">Specifies whether to use upper case characters.</param>
    /// <returns>A hexadecimal string representing the input bytes.</returns>
    /// <remarks>
    /// Delegates the upper case path to <see cref="Convert.ToHexString(ReadOnlySpan{byte})" /> to inherit the BCL's
    /// SIMD-accelerated hex conversion. On .NET 9 or later the lower case path delegates to
    /// <c>Convert.ToHexStringLower</c>; on earlier frameworks it uses the local hand-rolled lookup path.
    /// </remarks>
    private static string EncodeFast(ReadOnlySpan<byte> bytes, bool upper)
    {
        if (upper)
            return Convert.ToHexString(bytes);

#if NET9_0_OR_GREATER
        return Convert.ToHexStringLower(bytes);
#else
        var buffer = new char[bytes.Length * 2];
        EncodeToHexCore(bytes, buffer, upperCase: false);
        return new string(buffer);
#endif
    }

    /// <summary>
    /// Writes the hexadecimal representation of <paramref name="bytes" /> directly into <paramref name="chars" />.
    /// </summary>
    /// <param name="bytes">The input byte span.</param>
    /// <param name="chars">
    /// The destination character span. The caller must ensure it is at least <c>bytes.Length * 2</c> characters in
    /// size.
    /// </param>
    /// <param name="upperCase">Whether to emit upper case characters.</param>
    private static void EncodeToHexCore(ReadOnlySpan<byte> bytes, Span<char> chars, bool upperCase)
    {
        ReadOnlySpan<char> map = (upperCase ? HexUpperAlphabet : HexLowerAlphabet).AsSpan();
        for (var i = 0; i < bytes.Length; i++)
        {
            var b = bytes[i];
            chars[i * 2] = map[b >> 4];
            chars[(i * 2) + 1] = map[b & 0x0F];
        }
    }

    /// <summary>
    /// Encodes a byte span into a hexadecimal string with optional decoration (spacing, prefix, line breaks).
    /// </summary>
    /// <param name="bytes">The byte span to encode.</param>
    /// <param name="options">The formatting options to apply.</param>
    /// <returns>A formatted hexadecimal string.</returns>
    /// <remarks>
    /// This is the slow path; it is invoked only when at least one decoration flag is set.
    /// </remarks>
    private static string EncodeWithFormatting(ReadOnlySpan<byte> bytes, BaseFormattingOptions options)
    {
        var upper = options.HasFlag(BaseFormattingOptions.UpperCase);
        var spacing = options.HasFlag(BaseFormattingOptions.InsertSpacing);
        var lineBreaks = options.HasFlag(BaseFormattingOptions.InsertLineBreaks);
        var prefix = options.HasFlag(BaseFormattingOptions.IncludePrefix);

        StringBuilder sb = new(GetEncodedLength(bytes.Length, options));
        if (prefix)
            sb.Append(Prefix);

        ReadOnlySpan<char> map = (upper ? HexUpperAlphabet : HexLowerAlphabet).AsSpan();
        var encodedCharsInLine = prefix ? Prefix.Length : 0;

        for (var i = 0; i < bytes.Length; i++)
        {
            if (spacing && i > 0)
            {
                sb.Append(' ');
                encodedCharsInLine++;
            }

            if (lineBreaks && encodedCharsInLine >= LineBreakInterval)
            {
                sb.Append("\r\n");
                encodedCharsInLine = 0;
            }

            var b = bytes[i];
            sb.Append(map[b >> 4]);
            sb.Append(map[b & 0x0F]);
            encodedCharsInLine += 2;
        }

        return sb.ToString();
    }

    /// <summary>
    /// Validates that <paramref name="options" /> contains no flags incompatible with span-based output.
    /// </summary>
    /// <param name="options">The encode options requested by the caller.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when any flag other than <see cref="BaseFormattingOptions.UpperCase" /> is set.
    /// </exception>
    private static void EnsureSpanOutputOptionsSupported(BaseFormattingOptions options)
    {
        if ((options & ~BaseFormattingOptions.UpperCase) != 0)
        {
            throw new ArgumentException(
                EncodingResourceStrings.Arg_Invalid_Base16UnsupportedEncodeOptions,
                nameof(options));
        }
    }
}
