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
///<![CDATA[
/// byte[] data = "hello"u8.ToArray();
///
/// // RFC 4648 Standard Base64 (default).
/// string standard = Base64.Encode(data);                                       // "aGVsbG8="
///
/// // URL-safe alphabet, no padding — the form used by JWT and OAuth tokens.
/// string urlSafe  = Base64.Encode(data, Base64Variant.UrlSafe);                // "aGVsbG8"
///
/// // MIME variant — wraps every 76 characters with CRLF.
/// string mime     = Base64.Encode(longerPayload, Base64Variant.Mime);
///
/// // Round-trip.
/// byte[] roundtrip = Base64.Decode(standard);
///]]>
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
    /// Thrown when the input contains invalid characters or the digit count is not a valid Base64 length.
    /// </exception>
    public static int GetDecodedLength(ReadOnlySpan<char> source, Base64Variant variant = Base64Variant.Standard, BaseFormatStyles styles = BaseFormatStyles.None)
    {
        return !TryCountSymbols(source, variant, styles, out int symbolCount)
            ? throw new FormatException(EncodingResourceStrings.Format_Invalid_Base64VariantAlphabet)
            : (symbolCount * 3) / 4;
    }

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
    /// <see langword="true" /> when the input would decode cleanly; otherwise <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="variant" /> is undefined.</exception>
    public static bool IsValid(ReadOnlySpan<char> source, Base64Variant variant = Base64Variant.Standard, BaseFormatStyles styles = BaseFormatStyles.None) =>
        TryCountSymbols(source, variant, styles, out _);

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
    public static bool TryGetDecodedLength(ReadOnlySpan<char> source, out int byteCount, Base64Variant variant = Base64Variant.Standard, BaseFormatStyles styles = BaseFormatStyles.None)
    {
        if (!TryCountSymbols(source, variant, styles, out int symbolCount))
        {
            byteCount = 0;
            return false;
        }

        byteCount = (symbolCount * 3) / 4;
        return true;
    }

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
    /// Indicates whether <paramref name="value" /> is part of the supplied Base64 variant's alphabet.
    /// </summary>
    /// <param name="value">The character to test.</param>
    /// <param name="variant">The variant.</param>
    /// <returns><see langword="true" /> when the character is a valid data symbol.</returns>
    private static bool IsAlphabetCharacter(char value, Base64Variant variant)
    {
        bool isLetter = value is (>= 'A' and <= 'Z') or (>= 'a' and <= 'z');
        bool isDigit = value is >= '0' and <= '9';
        return isLetter || isDigit || (variant == Base64Variant.UrlSafe
            ? value is '-' or '_'
            : value is '+' or '/');
    }

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
    /// Walks <paramref name="source" />, validating each non-decoration character, and reports the data symbol count.
    /// Also enforces RFC 4648 §4 structural constraints (terminal-quantum length and padding alignment) so that
    /// <see cref="IsValid" /> matches the strict decoder.
    /// </summary>
    /// <param name="source">The input characters.</param>
    /// <param name="variant">The variant.</param>
    /// <param name="styles">Parsing styles.</param>
    /// <param name="symbolCount">
    /// When this method returns, contains the number of data symbols (excluding padding).
    /// </param>
    /// <returns>
    /// <see langword="true" /> when every character is valid AND the data quantum is structurally well-formed;
    /// otherwise <see langword="false" />.
    /// </returns>
    private static bool TryCountSymbols(ReadOnlySpan<char> source, Base64Variant variant, BaseFormatStyles styles, out int symbolCount)
    {
        EnsureValidVariant(variant);

        bool ignoreWhitespace = styles.HasFlag(BaseFormatStyles.IgnoreWhitespace) || variant == Base64Variant.Mime;
        symbolCount = 0;
        int paddingSeen = 0;

        foreach (char c in source)
        {
            if (ignoreWhitespace && c is ' ' or '\t' or '\r' or '\n')
                continue;

            if (c == PaddingChar)
            {
                paddingSeen++;
                continue;
            }

            if (paddingSeen > 0)
            {
                symbolCount = 0;
                return false;
            }

            if (!IsAlphabetCharacter(c, variant))
            {
                symbolCount = 0;
                return false;
            }

            symbolCount++;
        }

        // Terminal-quantum data character count must be in {0, 2, 3, 4} per RFC 4648 §4.
        int dataMod = symbolCount % 4;
        if (dataMod == 1)
        {
            symbolCount = 0;
            return false;
        }

        // Padding alignment: Standard / Mime require canonical padding by default. UrlSafe and AllowMissingPadding
        // bypass the "must have padding" requirement. In all variants, if padding IS present it must match the
        // canonical count for the data — partial or excessive padding ("TQ===") is always rejected.
        int expectedPadding = (4 - dataMod) % 4;
        bool padIsRequired = !styles.HasFlag(BaseFormatStyles.AllowMissingPadding)
            && variant != Base64Variant.UrlSafe;

        if (padIsRequired)
        {
            int totalSymbols = symbolCount + paddingSeen;
            if ((totalSymbols % 4) != 0)
            {
                symbolCount = 0;
                return false;
            }

            if (paddingSeen != expectedPadding)
            {
                symbolCount = 0;
                return false;
            }
        }
        else if (paddingSeen != 0 && paddingSeen != expectedPadding)
        {
            // AllowMissingPadding / UrlSafe mode: padding may be absent OR canonical, but never partial / excessive.
            symbolCount = 0;
            return false;
        }

        return true;
    }
}
