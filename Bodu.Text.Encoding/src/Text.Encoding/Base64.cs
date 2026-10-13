// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base64.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

/// <summary>
/// Provides Base64 encoding and decoding of binary data across the RFC 4648 standard, URL-safe, and MIME variants, with
/// optional padding control and whitespace tolerance during parsing.
/// </summary>
/// <remarks>
/// <para>
/// The implementation delegates the inner radix conversion to <see cref="Convert" />'s
/// <see cref="Convert.ToBase64String(byte[], Base64FormattingOptions)" />,
/// <see cref="Convert.TryToBase64Chars(ReadOnlySpan{byte}, Span{char}, out int, Base64FormattingOptions)" />, and
/// <see cref="Convert.TryFromBase64Chars(ReadOnlySpan{char}, Span{byte}, out int)" /> to inherit their
/// hardware-accelerated paths. The wrapper supplies the alphabet swapping for <see cref="Base64Variant.UrlSafe" />, the
/// line-break convention for <see cref="Base64Variant.Mime" />, and the padding / leniency flag handling that the BCL
/// does not expose directly.
/// </para>
/// <para>
/// MIME line breaks are inserted every 76 characters (RFC 2045). The <see cref="BaseFormattingOptions.UpperCase" />,
/// <see cref="BaseFormattingOptions.IncludePrefix" />, and <see cref="BaseFormattingOptions.InsertSpacing" /> flags
/// have no effect on Base64.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// byte[] data = "hello"u8.ToArray();
///
/// // RFC 4648 Standard Base64 (default).
/// string standard = Base64.Encode(data);                                       // "aGVsbG8="
///
/// // URL-safe alphabet, no padding - the form used by JWT and OAuth tokens.
/// string urlSafe  = Base64.Encode(data, Base64Variant.UrlSafe);                // "aGVsbG8"
///
/// // MIME variant - wraps every 76 characters with CRLF.
/// string mime     = Base64.Encode(longerPayload, Base64Variant.Mime);
///
/// // Round-trip.
/// byte[] roundtrip = Base64.Decode(standard);
///]]>
/// </code>
/// </example>
public static partial class Base64
{
    /// <summary>The number of encoded characters per MIME / line-break line.</summary>
    private const int MimeLineLength = 76;

    /// <summary>The padding character used by Base64 per RFC 4648.</summary>
    private const char PaddingChar = '=';

    /// <summary>
    /// Computes the exact number of bytes that decoding <paramref name="source" /> would produce after applying
    /// <paramref name="styles" /> and stripping padding for the supplied <paramref name="variant" />.
    /// </summary>
    /// <param name="source">The Base64 character span.</param>
    /// <param name="variant">The Base64 variant.</param>
    /// <param name="styles">Parsing styles.</param>
    /// <returns>The exact decoded byte count.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="variant" /> is undefined.</exception>
    /// <exception cref="FormatException">
    /// Thrown when the input is not valid Base64 for the variant and styles, that is, when
    /// <see cref="Decode(ReadOnlySpan{char}, Base64Variant, BaseFormatStyles)" /> would reject it.
    /// </exception>
    public static int GetDecodedLength(ReadOnlySpan<char> source, Base64Variant variant = Base64Variant.Standard, BaseFormatStyles styles = BaseFormatStyles.None) =>
        TryMeasure(source, variant, styles, out int byteCount, out string? error)
            ? byteCount
            : throw new FormatException(error);

    /// <summary>
    /// Returns the number of characters produced by encoding <paramref name="byteCount" /> bytes using the Standard
    /// variant with default formatting.
    /// </summary>
    /// <param name="byteCount">The input byte count. Must be non-negative.</param>
    /// <returns>The number of characters the encoder will produce.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="byteCount" /> is negative.</exception>
    public static int GetEncodedLength(int byteCount) =>
        GetEncodedLength(byteCount, Base64Variant.Standard, BaseFormattingOptions.None);

    /// <summary>
    /// Returns the number of characters produced by encoding <paramref name="byteCount" /> bytes using
    /// <paramref name="variant" /> with default formatting.
    /// </summary>
    /// <param name="byteCount">The input byte count.</param>
    /// <param name="variant">The Base64 variant.</param>
    /// <returns>The number of characters the encoder will produce.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="byteCount" /> is negative or <paramref name="variant" /> is undefined.
    /// </exception>
    public static int GetEncodedLength(int byteCount, Base64Variant variant) =>
        GetEncodedLength(byteCount, variant, BaseFormattingOptions.None);

    /// <summary>
    /// Computes the number of characters required to encode <paramref name="byteCount" /> bytes with the given variant
    /// and options.
    /// </summary>
    /// <param name="byteCount">The number of input bytes. Must be non-negative.</param>
    /// <param name="variant">The Base64 variant.</param>
    /// <param name="options">The formatting options.</param>
    /// <returns>The number of characters the matching encode overload will produce.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="byteCount" /> is negative.</exception>
    public static int GetEncodedLength(int byteCount, Base64Variant variant = Base64Variant.Standard, BaseFormattingOptions options = BaseFormattingOptions.None)
    {
        ThrowHelper.ThrowIfNegative(byteCount);

        if (byteCount == 0)
            return 0;

        bool emitPadding = ShouldEmitPadding(variant, options);
        bool insertLineBreaks = ShouldInsertLineBreaks(variant, options);

        checked
        {
            int dataChars = emitPadding
                ? ((byteCount + 2) / 3) * 4
                : ((byteCount * 4) + 2) / 3;

            if (!insertLineBreaks || dataChars <= MimeLineLength)
                return dataChars;

            int breaks = (dataChars - 1) / MimeLineLength;
            return dataChars + (breaks * 2);
        }
    }

    /// <summary>
    /// Computes the maximum number of bytes that can result from decoding <paramref name="charCount" /> characters.
    /// </summary>
    /// <param name="charCount">The number of input characters. Must be non-negative.</param>
    /// <returns>The upper bound on the decoded byte count.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="charCount" /> is negative.</exception>
    public static int GetMaxDecodedLength(int charCount)
    {
        ThrowHelper.ThrowIfNegative(charCount);
        checked
        {
            return (charCount * 3) / 4;
        }
    }

    /// <summary>
    /// Indicates whether <paramref name="value" /> is a valid Base64 alphabet character for the supplied variant.
    /// Padding (<c>=</c>) is not considered a digit.
    /// </summary>
    /// <param name="value">The character to test.</param>
    /// <param name="variant">The variant.</param>
    /// <returns><see langword="true" /> when the character belongs to the variant alphabet.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="variant" /> is undefined.</exception>
    public static bool IsBase64Digit(char value, Base64Variant variant = Base64Variant.Standard)
    {
        EnsureValidVariant(variant);
        return IsAlphabetCharacter(value, variant);
    }

    /// <summary>
    /// Indicates whether <paramref name="source" /> is a valid Base64 input under the supplied variant and styles.
    /// </summary>
    /// <param name="source">The character span.</param>
    /// <param name="variant">The variant.</param>
    /// <param name="styles">Parsing styles.</param>
    /// <returns>
    /// <see langword="true" /> when <see cref="Decode(ReadOnlySpan{char}, Base64Variant, BaseFormatStyles)" /> with the
    /// same variant and styles would succeed; otherwise <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="variant" /> is undefined.</exception>
    public static bool IsValid(ReadOnlySpan<char> source, Base64Variant variant = Base64Variant.Standard, BaseFormatStyles styles = BaseFormatStyles.None) =>
        TryMeasure(source, variant, styles, out _, out _);

    /// <summary>
    /// Attempts to compute the exact decoded byte count for <paramref name="source" />.
    /// </summary>
    /// <param name="source">The Base64 character span.</param>
    /// <param name="byteCount">
    /// When this method returns, contains the decoded byte count, or <c>0</c> on failure.
    /// </param>
    /// <param name="variant">The Base64 variant.</param>
    /// <param name="styles">Parsing styles.</param>
    /// <returns>
    /// <see langword="true" /> when the input would decode cleanly; otherwise <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="variant" /> is undefined.</exception>
    public static bool TryGetDecodedLength(ReadOnlySpan<char> source, out int byteCount, Base64Variant variant = Base64Variant.Standard, BaseFormatStyles styles = BaseFormatStyles.None) =>
        TryMeasure(source, variant, styles, out byteCount, out _);

    /// <summary>
    /// Validates that <paramref name="variant" /> is a defined enum value.
    /// </summary>
    /// <param name="variant">The variant to validate.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the variant is undefined.</exception>
    private static void EnsureValidVariant(Base64Variant variant)
    {
        if (variant is not (Base64Variant.Standard or Base64Variant.UrlSafe or Base64Variant.Mime))
            throw new ArgumentOutOfRangeException(nameof(variant), variant, EncodingResourceStrings.Arg_OutOfRange_Base64Variant);
    }

    /// <summary>
    /// Returns the six-bit value of <paramref name="value" /> in the supplied Base64 variant's alphabet.
    /// </summary>
    /// <param name="value">The character to look up.</param>
    /// <param name="variant">The variant.</param>
    /// <returns>
    /// The symbol value, from <c>0</c> to <c>63</c>, or <c>-1</c> when the character is not a data symbol.
    /// </returns>
    private static int GetSymbolValue(char value, Base64Variant variant) => value switch
    {
        >= 'A' and <= 'Z' => value - 'A',
        >= 'a' and <= 'z' => value - 'a' + 26,
        >= '0' and <= '9' => value - '0' + 52,
        '+' when variant != Base64Variant.UrlSafe => 62,
        '/' when variant != Base64Variant.UrlSafe => 63,
        '-' when variant == Base64Variant.UrlSafe => 62,
        '_' when variant == Base64Variant.UrlSafe => 63,
        _ => -1,
    };

    /// <summary>
    /// Indicates whether <paramref name="value" /> is part of the supplied Base64 variant's alphabet.
    /// </summary>
    /// <param name="value">The character to test.</param>
    /// <param name="variant">The variant.</param>
    /// <returns><see langword="true" /> when the character is a valid data symbol.</returns>
    private static bool IsAlphabetCharacter(char value, Base64Variant variant) =>
        GetSymbolValue(value, variant) >= 0;

    /// <summary>
    /// Determines whether the encoder should emit <c>=</c> padding for the supplied variant and options.
    /// </summary>
    /// <param name="variant">The variant.</param>
    /// <param name="options">The encode options.</param>
    /// <returns><see langword="true" /> when padding should be emitted.</returns>
    private static bool ShouldEmitPadding(Base64Variant variant, BaseFormattingOptions options)
    {
        return !options.HasFlag(BaseFormattingOptions.OmitPadding) && variant switch
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

    /// <summary>
    /// Validates <paramref name="source" /> by the rules
    /// <see cref="Decode(ReadOnlySpan{char}, Base64Variant, BaseFormatStyles)" /> applies, and reports the decoded byte
    /// count without decoding, so that the validation members accept exactly the input the decoder accepts.
    /// </summary>
    /// <param name="source">The input characters.</param>
    /// <param name="variant">The variant.</param>
    /// <param name="styles">The parsing styles.</param>
    /// <param name="byteCount">
    /// When this method returns, contains the number of bytes the input decodes to, or <c>0</c> on failure.
    /// </param>
    /// <param name="error">
    /// When this method returns <see langword="false" />, contains the reason the decoder would reject the input.
    /// </param>
    /// <returns><see langword="true" /> when the input would decode; otherwise <see langword="false" />.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="variant" /> is undefined.</exception>
    /// <remarks>
    /// The decoder strips whitespace when permitted, rejects any character outside the variant alphabet and any data
    /// after padding, completes a partial final quantum with padding when padding may be missing, and then requires
    /// whole four-character quanta whose padding exactly fills the final one (RFC 4648 §4). Under
    /// <see cref="BaseFormatStyles.RequireCanonicalEncoding" /> it also requires the unused low bits of the last symbol
    /// to be zero.
    /// </remarks>
    private static bool TryMeasure(ReadOnlySpan<char> source, Base64Variant variant, BaseFormatStyles styles, out int byteCount, out string? error)
    {
        EnsureValidVariant(variant);

        bool ignoreWhitespace = styles.HasFlag(BaseFormatStyles.IgnoreWhitespace) || variant == Base64Variant.Mime;
        bool allowMissingPadding = styles.HasFlag(BaseFormatStyles.AllowMissingPadding) || variant == Base64Variant.UrlSafe;

        byteCount = 0;
        int symbolCount = 0;
        int paddingCount = 0;
        int lastSymbolValue = 0;

        foreach (char c in source)
        {
            if (ignoreWhitespace && c is ' ' or '\t' or '\r' or '\n')
                continue;

            if (c == PaddingChar)
            {
                paddingCount++;
                continue;
            }

            int value = GetSymbolValue(c, variant);
            if (value < 0 || paddingCount > 0)
            {
                error = EncodingResourceStrings.Format_Invalid_Base64Alphabet;
                return false;
            }

            lastSymbolValue = value;
            symbolCount++;
        }

        // The decoder completes a partial final quantum of two or three characters with padding; a lone character
        // past the last whole quantum cannot be completed.
        int retained = symbolCount + paddingCount;
        if (allowMissingPadding && (retained % 4) is 2 or 3)
            paddingCount += 4 - (retained % 4);

        int dataRemainder = symbolCount % 4;
        if (dataRemainder == 1 || ((symbolCount + paddingCount) % 4) != 0 || paddingCount != (4 - dataRemainder) % 4)
        {
            error = EncodingResourceStrings.Format_Invalid_Base64;
            return false;
        }

        // A final quantum of two characters leaves four unused bits in its last symbol, one of three leaves two.
        int unusedBitsMask = dataRemainder switch
        {
            2 => 0x0F,
            3 => 0x03,
            _ => 0,
        };

        if (styles.HasFlag(BaseFormatStyles.RequireCanonicalEncoding) && (lastSymbolValue & unusedBitsMask) != 0)
        {
            error = EncodingResourceStrings.Format_Invalid_Base64NonCanonical;
            return false;
        }

        byteCount = (symbolCount * 3) / 4;
        error = null;
        return true;
    }
}
