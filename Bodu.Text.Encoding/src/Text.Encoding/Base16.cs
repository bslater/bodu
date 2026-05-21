// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base16.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

/// <summary>
/// Provides Base16 (hexadecimal) encoding and decoding of binary data, with support for flexible output formatting
/// (case selection, byte spacing, line wrapping, <c>0x</c> prefix) and lenient input parsing (prefix tolerance,
/// whitespace stripping).
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Encode(ReadOnlySpan{byte}, BaseFormattingOptions)" /> emits two hexadecimal characters per input byte
/// using the configured <see cref="BaseFormattingOptions" /> flags.
/// <see cref="Decode(ReadOnlySpan{char}, BaseFormatStyles)" /> reverses the operation and accepts decoration tolerance
/// via <see cref="BaseFormatStyles" />.
/// </para>
/// <para>
/// All public methods are thread-safe — the type is stateless. The implementation provides allocation-minimal fast
/// paths when no decoration flags are set, and falls back to a <see cref="System.Text.StringBuilder" />-based writer
/// when spacing, prefix, or line breaks are requested.
/// </para>
/// </remarks>
/// <example>
///<![CDATA[
/// byte[] data = { 0xDE, 0xAD, 0xBE, 0xEF };
///
/// // Canonical lower-case hex.
/// string lower = Base16.Encode(data);                                           // "deadbeef"
///
/// // Upper-case with the "0x" prefix and byte spacing — useful for diagnostic output.
/// string pretty = Base16.Encode(data, BaseFormattingOptions.UpperCase
///                                    | BaseFormattingOptions.IncludePrefix
///                                    | BaseFormattingOptions.InsertSpacing);    // "0xDE AD BE EF"
///
/// // Lenient decoding — accepts the "0x" prefix and embedded whitespace.
/// byte[] roundtrip = Base16.Decode(pretty,
///     BaseFormatStyles.AllowPrefix | BaseFormatStyles.IgnoreWhitespace);
///]]>
/// </example>
public static partial class Base16
{
    /// <summary>
    /// The Base16 alphabet using lower case letters.
    /// </summary>
    private const string HexLowerAlphabet = "0123456789abcdef";

    /// <summary>
    /// The Base16 alphabet using upper case letters.
    /// </summary>
    private const string HexUpperAlphabet = "0123456789ABCDEF";

    /// <summary>
    /// The number of encoded hexadecimal characters per output line when
    /// <see cref="BaseFormattingOptions.InsertLineBreaks" /> is requested.
    /// </summary>
    private const int LineBreakInterval = 64;

    /// <summary>
    /// The decorative prefix emitted when <see cref="BaseFormattingOptions.IncludePrefix" /> is requested, and the
    /// prefix accepted when <see cref="BaseFormatStyles.AllowPrefix" /> is set.
    /// </summary>
    private const string Prefix = "0x";

    /// <summary>
    /// Computes the exact number of bytes that decoding <paramref name="source" /> would produce after stripping
    /// decorations permitted by <paramref name="styles" />.
    /// </summary>
    /// <param name="source">The hexadecimal input.</param>
    /// <param name="styles">The parsing styles.</param>
    /// <returns>
    /// The exact byte count that <see cref="Decode(ReadOnlySpan{char}, BaseFormatStyles)" /> would return.
    /// </returns>
    /// <exception cref="FormatException">
    /// Thrown when the post-decoration digit count is odd, which indicates the input cannot decode cleanly.
    /// </exception>
    public static int GetDecodedLength(ReadOnlySpan<char> source, BaseFormatStyles styles = BaseFormatStyles.None)
    {
        return TryCountHexDigits(source, styles, out var digits)
            ? (digits & 1) == 0
                ? digits / 2
                : throw new FormatException(EncodingResourceStrings.Format_Invalid_HexDigitCountOddAfterStyles)
            : throw new FormatException(EncodingResourceStrings.Format_Invalid_HexCharactersAfterStyles);
    }

    /// <summary>
    /// Returns the number of characters produced by encoding <paramref name="byteCount" /> bytes with strict formatting
    /// (no decorations).
    /// </summary>
    /// <param name="byteCount">The input byte count. Must be non-negative.</param>
    /// <returns><c>byteCount * 2</c>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="byteCount" /> is negative.</exception>
    public static int GetEncodedLength(int byteCount) =>
        GetEncodedLength(byteCount, BaseFormattingOptions.None);

    /// <summary>
    /// Computes the number of characters required to encode <paramref name="byteCount" /> bytes with the supplied
    /// formatting options.
    /// </summary>
    /// <param name="byteCount">The number of input bytes. Must be non-negative.</param>
    /// <param name="options">The formatting options that influence the output length.</param>
    /// <returns>
    /// The exact number of characters that <see cref="Encode(ReadOnlySpan{byte}, BaseFormattingOptions)" /> will
    /// produce.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="byteCount" /> is negative.</exception>
    /// <exception cref="OverflowException">
    /// Thrown when <paramref name="byteCount" /> is large enough that the resulting character count would overflow
    /// <see cref="int" />.
    /// </exception>
    public static int GetEncodedLength(int byteCount, BaseFormattingOptions options = BaseFormattingOptions.None)
    {
        ThrowHelper.ThrowIfNegative(byteCount);

        if (byteCount == 0)
            return options.HasFlag(BaseFormattingOptions.IncludePrefix) ? Prefix.Length : 0;

        var spacing = options.HasFlag(BaseFormattingOptions.InsertSpacing);
        var lineBreaks = options.HasFlag(BaseFormattingOptions.InsertLineBreaks);
        var prefix = options.HasFlag(BaseFormattingOptions.IncludePrefix);

        checked
        {
            var chars = spacing ? (byteCount * 3) - 1 : byteCount * 2;
            if (prefix)
                chars += Prefix.Length;

            if (lineBreaks)
            {
                var encodedCharsInLine = prefix ? Prefix.Length : 0;
                var breaks = 0;

                var column = encodedCharsInLine;
                for (var i = 0; i < byteCount; i++)
                {
                    if (spacing && i > 0)
                        column++;

                    if (column >= LineBreakInterval)
                    {
                        breaks++;
                        column = 0;
                    }

                    column += 2;
                }

                chars += breaks * 2; // "\r\n" per break
            }

            return chars;
        }
    }

    /// <summary>
    /// Computes the maximum number of bytes that can result from decoding <paramref name="charCount" /> characters.
    /// </summary>
    /// <param name="charCount">The number of input characters. Must be non-negative.</param>
    /// <returns>
    /// The upper bound on the number of decoded bytes. The actual byte count will be lower when the input contains
    /// decorations that are stripped during parsing.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="charCount" /> is negative.</exception>
    public static int GetMaxDecodedLength(int charCount)
    {
        ThrowHelper.ThrowIfNegative(charCount);
        return charCount / 2;
    }

    /// <summary>
    /// Indicates whether <paramref name="value" /> is one of the 22 hexadecimal digit characters.
    /// </summary>
    /// <param name="value">The character to test.</param>
    /// <returns>
    /// <see langword="true" /> for <c>'0'</c>-<c>'9'</c>, <c>'A'</c>-<c>'F'</c>, and <c>'a'</c>-<c>'f'</c>; otherwise
    /// <see langword="false" />.
    /// </returns>
    public static bool IsHexDigit(char value) => value switch
    {
        >= '0' and <= '9' => true,
        >= 'A' and <= 'F' => true,
        >= 'a' and <= 'f' => true,
        _ => false,
    };

    /// <summary>
    /// Indicates whether <paramref name="source" /> is a valid hexadecimal input under the supplied parsing styles.
    /// </summary>
    /// <param name="source">The input characters.</param>
    /// <param name="styles">The parsing styles.</param>
    /// <returns>
    /// <see langword="true" /> when every retained character is a hex digit and the retained count is even; otherwise
    /// <see langword="false" />.
    /// </returns>
    public static bool IsValid(ReadOnlySpan<char> source, BaseFormatStyles styles = BaseFormatStyles.None)
    {
        var start = 0;
        var ignoreWhitespace = styles.HasFlag(BaseFormatStyles.IgnoreWhitespace);

        if (styles.HasFlag(BaseFormatStyles.AllowPrefix) &&
            source.Length >= Prefix.Length &&
            source.StartsWith(Prefix.AsSpan(), StringComparison.OrdinalIgnoreCase))
        {
            start = Prefix.Length;
        }

        var digits = 0;
        for (var i = start; i < source.Length; i++)
        {
            var c = source[i];
            if (ignoreWhitespace && c is ' ' or '\t' or '\r' or '\n')
                continue;

            if (!IsHexDigit(c))
                return false;

            digits++;
        }

        return (digits & 1) == 0;
    }

    /// <summary>
    /// Attempts to compute the exact number of bytes that decoding <paramref name="source" /> would produce.
    /// </summary>
    /// <param name="source">The hexadecimal input.</param>
    /// <param name="byteCount">
    /// When this method returns, contains the byte count, or <c>0</c> when the input is malformed.
    /// </param>
    /// <param name="styles">The parsing styles.</param>
    /// <returns>
    /// <see langword="true" /> when the input would decode cleanly; otherwise <see langword="false" />.
    /// </returns>
    public static bool TryGetDecodedLength(ReadOnlySpan<char> source, out int byteCount, BaseFormatStyles styles = BaseFormatStyles.None)
    {
        if (!TryCountHexDigits(source, styles, out var digits) || (digits & 1) != 0)
        {
            byteCount = 0;
            return false;
        }

        byteCount = digits / 2;
        return true;
    }

    /// <summary>
    /// Counts the hex digits remaining in <paramref name="source" /> after applying the decoration leniency flags,
    /// validating each retained character against the hexadecimal alphabet so that the result agrees with what the
    /// decoder would accept.
    /// </summary>
    /// <param name="source">The hexadecimal input.</param>
    /// <param name="styles">The parsing styles.</param>
    /// <param name="digitCount">
    /// When this method returns, contains the retained digit count, or <c>0</c> when an invalid character was
    /// encountered.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when every retained character is a hex digit; <see langword="false" /> when any retained
    /// character is outside the hex alphabet.
    /// </returns>
    private static bool TryCountHexDigits(ReadOnlySpan<char> source, BaseFormatStyles styles, out int digitCount)
    {
        digitCount = 0;
        var ignoreWhitespace = styles.HasFlag(BaseFormatStyles.IgnoreWhitespace);
        var start = 0;

        if (styles.HasFlag(BaseFormatStyles.AllowPrefix) &&
            source.Length >= Prefix.Length &&
            source.StartsWith(Prefix.AsSpan(), StringComparison.OrdinalIgnoreCase))
        {
            start = Prefix.Length;
        }

        var digits = 0;
        for (var i = start; i < source.Length; i++)
        {
            var c = source[i];
            if (ignoreWhitespace && c is ' ' or '\t' or '\r' or '\n')
                continue;

            if (!IsHexDigit(c))
                return false;

            digits++;
        }

        digitCount = digits;
        return true;
    }
}
