// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base64.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

/// <summary>
/// Provides Base64 encoding and decoding of binary data across the RFC 4648 standard, URL-safe, and MIME variants,
/// with optional padding control and whitespace tolerance during parsing.
/// </summary>
/// <remarks>
/// <para>
/// The implementation delegates the inner radix conversion to <see cref="Convert" />'s
/// <see cref="Convert.ToBase64String(byte[], Base64FormattingOptions)" />,
/// <see cref="Convert.TryToBase64Chars(ReadOnlySpan{byte}, Span{char}, out int, Base64FormattingOptions)" />, and
/// <see cref="Convert.TryFromBase64Chars(ReadOnlySpan{char}, Span{byte}, out int)" /> to inherit their
/// hardware-accelerated paths. The wrapper supplies the alphabet swapping for <see cref="Base64Variant.UrlSafe" />,
/// the line-break convention for <see cref="Base64Variant.Mime" />, and the padding / leniency flag handling that the
/// BCL does not expose directly.
/// </para>
/// <para>
/// MIME line breaks are inserted every 76 characters (RFC 2045). The <see cref="BaseFormattingOptions.UpperCase" />,
/// <see cref="BaseFormattingOptions.IncludePrefix" />, and <see cref="BaseFormattingOptions.InsertSpacing" /> flags
/// have no effect on Base64.
/// </para>
/// </remarks>
public static partial class Base64
{
    /// <summary>
    /// The padding character used by Base64 per RFC 4648.
    /// </summary>
    private const char PaddingChar = '=';

    /// <summary>
    /// The number of encoded characters per MIME / line-break line.
    /// </summary>
    private const int MimeLineLength = 76;

    /// <summary>
    /// Computes the number of characters required to encode <paramref name="byteCount" /> bytes with the given
    /// variant and options.
    /// </summary>
    /// <param name="byteCount">The number of input bytes. Must be non-negative.</param>
    /// <param name="variant">The Base64 variant.</param>
    /// <param name="options">The formatting options.</param>
    /// <returns>The number of characters the matching encode overload will produce.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="byteCount" /> is negative.
    /// </exception>
    public static int GetEncodedLength(int byteCount, Base64Variant variant = Base64Variant.Standard, BaseFormattingOptions options = BaseFormattingOptions.None)
    {
        ThrowHelper.ThrowIfNegative(byteCount);

        if (byteCount == 0)
            return 0;

        bool emitPadding = ShouldEmitPadding(variant, options);
        bool insertLineBreaks = ShouldInsertLineBreaks(variant, options);

        int dataChars = emitPadding
            ? ((byteCount + 2) / 3) * 4
            : ((byteCount * 4) + 2) / 3;

        if (!insertLineBreaks || dataChars <= MimeLineLength)
            return dataChars;

        int breaks = (dataChars - 1) / MimeLineLength;
        return dataChars + (breaks * 2);
    }

    /// <summary>
    /// Computes the maximum number of bytes that can result from decoding <paramref name="charCount" /> characters.
    /// </summary>
    /// <param name="charCount">The number of input characters. Must be non-negative.</param>
    /// <returns>The upper bound on the decoded byte count.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="charCount" /> is negative.
    /// </exception>
    public static int GetMaxDecodedLength(int charCount)
    {
        ThrowHelper.ThrowIfNegative(charCount);
        return (charCount * 3) / 4;
    }

    /// <summary>
    /// Validates that <paramref name="variant" /> is a defined enum value.
    /// </summary>
    /// <param name="variant">The variant to validate.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the variant is undefined.</exception>
    private static void EnsureValidVariant(Base64Variant variant)
    {
        if (variant is not (Base64Variant.Standard or Base64Variant.UrlSafe or Base64Variant.Mime))
            throw new ArgumentOutOfRangeException(nameof(variant), variant, "Unknown Base64 variant.");
    }

    /// <summary>
    /// Determines whether the encoder should emit <c>=</c> padding for the supplied variant and options.
    /// </summary>
    /// <param name="variant">The variant.</param>
    /// <param name="options">The encode options.</param>
    /// <returns><see langword="true" /> when padding should be emitted.</returns>
    private static bool ShouldEmitPadding(Base64Variant variant, BaseFormattingOptions options)
    {
        if (options.HasFlag(BaseFormattingOptions.OmitPadding))
            return false;

        return variant switch
        {
            Base64Variant.Standard => true,
            Base64Variant.Mime => true,
            Base64Variant.UrlSafe => false,
            _ => true,
        };
    }

    /// <summary>
    /// Determines whether the encoder should insert line breaks every <see cref="MimeLineLength" /> characters.
    /// </summary>
    /// <param name="variant">The variant.</param>
    /// <param name="options">The encode options.</param>
    /// <returns><see langword="true" /> when line breaks should be inserted.</returns>
    private static bool ShouldInsertLineBreaks(Base64Variant variant, BaseFormattingOptions options) =>
        variant == Base64Variant.Mime || options.HasFlag(BaseFormattingOptions.InsertLineBreaks);
}
