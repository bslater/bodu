// ---------------------------------------------------------------------------------------------------------------
// <copyright file="QuotedPrintable.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

/// <summary>
/// Provides MIME Quoted-Printable body encoding and decoding (RFC 2045 §6.7) of binary data, with selectable binary or
/// canonical-text line handling, configurable soft line wrapping, and strict-by-default decoding.
/// </summary>
/// <remarks>
/// <para>
/// Quoted-Printable represents mostly-7-bit-safe octets as readable text: printable ASCII passes through literally,
/// while other octets are escaped as <c>=HH</c> with uppercase hexadecimal digits. Encoded lines are kept within a
/// configurable limit (76 characters by default) by inserting soft line breaks — a trailing <c>=</c> followed by the
/// newline — which the decoder removes.
/// </para>
/// <para>
/// <see cref="QuotedPrintableEncodingMode.Binary" /> treats the whole input as arbitrary octets and escapes CR and LF;
/// <see cref="QuotedPrintableEncodingMode.Text" /> recognises canonical CRLF pairs as hard line breaks. Encoding always
/// emits uppercase hex; decoding is strict by default but accepts lowercase hex, bare line feeds, and
/// transport-inserted trailing whitespace under the corresponding <see cref="QuotedPrintableDecodingOptions" /> flags.
/// </para>
/// <para>
/// This type encodes and decodes only the Quoted-Printable body transform. It does not implement the RFC 2047
/// encoded-word <c>Q</c> header encoding (which has different syntax and underscore-for-space handling), and it does
/// not parse MIME messages, headers, multiparts, charsets, or content-transfer-encoding declarations.
/// </para>
/// <para>
/// Because correct use depends on the mode and decoding options, this is a static type and is intentionally not
/// registered in <see cref="BinaryEncodings" /> — the parameterless <see cref="IBinaryEncoding" /> contract cannot
/// carry that information.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// byte[] data = "café = møney"u8.ToArray();
///
/// // Binary mode (default) — arbitrary octets, 76-column soft wrapping, CRLF.
/// string encoded = QuotedPrintable.Encode(data);
///
/// // Round-trip.
/// byte[] roundtrip = QuotedPrintable.Decode(encoded);
///]]>
/// </code>
/// </example>
public static partial class QuotedPrintable
{
    /// <summary>The RFC 2045 default maximum encoded line length.</summary>
    private const int DefaultMaxLineLength = 76;

    /// <summary>The RFC 2045 canonical encoded-line limit enforced by <see cref="IsValid" /> regardless of encode width.</summary>
    private const int MaxCanonicalLineLength = 76;

    /// <summary>The smallest permitted line length: one escape plus a soft-break marker.</summary>
    private const int MinMaxLineLength = 4;

    /// <summary>The canonical CRLF newline.</summary>
    private const string CrLf = "\r\n";

    /// <summary>The bare LF newline.</summary>
    private const string Lf = "\n";

    /// <summary>
    /// Returns the exact number of characters that
    /// <see cref="Encode(ReadOnlySpan{byte}, QuotedPrintableEncodingOptions)" /> produces for the supplied data and
    /// options.
    /// </summary>
    /// <param name="source">The input bytes.</param>
    /// <param name="options">The encoding options.</param>
    /// <returns>The exact encoded character count.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <see cref="QuotedPrintableEncodingOptions.Mode" /> is undefined or the normalized
    /// <see cref="QuotedPrintableEncodingOptions.MaxLineLength" /> is less than four.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <see cref="QuotedPrintableEncodingOptions.NewLine" /> is not <c>"\r\n"</c> or <c>"\n"</c>.
    /// </exception>
    public static int GetEncodedLength(ReadOnlySpan<byte> source, QuotedPrintableEncodingOptions options = default)
    {
        QuotedPrintableEncodingOptions normalized = NormalizeAndValidate(options);

        EncodeCore(source, normalized, Span<char>.Empty, measureOnly: true, out int charCount);
        return charCount;
    }

    /// <summary>
    /// Returns an upper bound on the number of characters that encoding <paramref name="byteCount" /> bytes could
    /// produce under the supplied options.
    /// </summary>
    /// <param name="byteCount">The input byte count. Must be non-negative.</param>
    /// <param name="options">The encoding options.</param>
    /// <returns>The worst-case encoded character count.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="byteCount" /> is negative, <see cref="QuotedPrintableEncodingOptions.Mode" /> is
    /// undefined, or the normalized <see cref="QuotedPrintableEncodingOptions.MaxLineLength" /> is less than four.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <see cref="QuotedPrintableEncodingOptions.NewLine" /> is not <c>"\r\n"</c> or <c>"\n"</c>.
    /// </exception>
    public static int GetMaxEncodedLength(int byteCount, QuotedPrintableEncodingOptions options = default)
    {
        ThrowHelper.ThrowIfNegative(byteCount);
        QuotedPrintableEncodingOptions normalized = NormalizeAndValidate(options);

        if (byteCount == 0)
            return 0;

        int maxContent = normalized.MaxLineLength - 1;
        int newLineLength = normalized.NewLine!.Length;

        checked
        {
            // Worst case: every byte escapes to three characters; soft breaks add '=' plus a newline per filled line.
            int contentMax = byteCount * 3;
            int softBreaks = (contentMax / maxContent) + 1;
            return contentMax + (softBreaks * (1 + newLineLength));
        }
    }

    /// <summary>
    /// Returns the maximum number of bytes that decoding <paramref name="charCount" /> characters could produce.
    /// </summary>
    /// <param name="charCount">The input character count. Must be non-negative.</param>
    /// <returns>The worst-case decoded byte count, equal to <paramref name="charCount" />.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="charCount" /> is negative.</exception>
    public static int GetMaxDecodedLength(int charCount)
    {
        ThrowHelper.ThrowIfNegative(charCount);
        return charCount;
    }

    /// <summary>
    /// Indicates whether <paramref name="source" /> is <em>canonical</em> RFC 2045 Quoted-Printable under the supplied
    /// options, including the 76-character encoded-line limit.
    /// </summary>
    /// <param name="source">The input characters.</param>
    /// <param name="options">The decoding options.</param>
    /// <returns><see langword="true" /> when the input is canonical; otherwise <see langword="false" />.</returns>
    /// <remarks>
    /// This is stricter than <see cref="Decode(ReadOnlySpan{char}, QuotedPrintableDecodingOptions)" />, which performs
    /// lenient recovery and accepts encoded lines longer than 76 characters. <c>IsValid</c> enforces the fixed RFC 2045
    /// limit (the soft-break <c>=</c> counted within it, the CRLF terminator not) regardless of any custom encode
    /// <see cref="QuotedPrintableEncodingOptions.MaxLineLength" />. <c>IsValid</c> returning <see langword="true" />
    /// implies <c>Decode</c> succeeds, but not the converse.
    /// </remarks>
    public static bool IsValid(ReadOnlySpan<char> source, QuotedPrintableDecodingOptions options = QuotedPrintableDecodingOptions.None) =>
        TryDecodeCore(source, options, Span<byte>.Empty, measureOnly: true, requireCanonical: true, out _);

    /// <summary>
    /// Attempts to determine the exact number of bytes that
    /// <see cref="Decode(ReadOnlySpan{char}, QuotedPrintableDecodingOptions)" /> will write for the supplied input.
    /// </summary>
    /// <param name="source">The input characters.</param>
    /// <param name="byteCount">
    /// When this method returns, contains the decoded byte count, or <c>0</c> on failure.
    /// </param>
    /// <param name="options">The decoding options.</param>
    /// <returns><see langword="true" /> when the input is decodable; otherwise <see langword="false" />.</returns>
    /// <remarks>
    /// This mirrors <see cref="Decode(ReadOnlySpan{char}, QuotedPrintableDecodingOptions)" /> (lenient recovery), not
    /// <see cref="IsValid(ReadOnlySpan{char}, QuotedPrintableDecodingOptions)" /> (canonical conformance): it does not
    /// enforce the 76-character line limit, so the result can size a decode buffer even for overlong input.
    /// </remarks>
    public static bool TryGetDecodedLength(ReadOnlySpan<char> source, out int byteCount, QuotedPrintableDecodingOptions options = QuotedPrintableDecodingOptions.None) =>
        TryDecodeCore(source, options, Span<byte>.Empty, measureOnly: true, requireCanonical: false, out byteCount);

    /// <summary>
    /// Normalizes <paramref name="options" /> (filling defaults for a zero line length or null newline) and validates
    /// the result.
    /// </summary>
    /// <param name="options">The options to normalize and validate.</param>
    /// <returns>The normalized options.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <see cref="QuotedPrintableEncodingOptions.Mode" /> is undefined or the normalized line length is
    /// less than four.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <see cref="QuotedPrintableEncodingOptions.NewLine" /> is not <c>"\r\n"</c> or <c>"\n"</c>.
    /// </exception>
    private static QuotedPrintableEncodingOptions NormalizeAndValidate(QuotedPrintableEncodingOptions options)
    {
        if (!TryNormalize(options, out QuotedPrintableEncodingOptions normalized, out int failure))
        {
            switch (failure)
            {
                case FailureMode:
                    throw new ArgumentOutOfRangeException(nameof(options), options.Mode, EncodingResourceStrings.Arg_OutOfRange_QuotedPrintableEncodingMode);
                case FailureMaxLineLength:
                    throw new ArgumentOutOfRangeException(nameof(options), options.MaxLineLength, EncodingResourceStrings.Arg_OutOfRange_QuotedPrintableMaxLineLength);
                default:
                    throw new ArgumentException(EncodingResourceStrings.Arg_Invalid_QuotedPrintableNewLine, nameof(options));
            }
        }

        return normalized;
    }

    /// <summary>Sentinel indicating the options validated successfully.</summary>
    private const int FailureNone = 0;

    /// <summary>Sentinel indicating an undefined <see cref="QuotedPrintableEncodingMode" />.</summary>
    private const int FailureMode = 1;

    /// <summary>Sentinel indicating a normalized line length below the minimum.</summary>
    private const int FailureMaxLineLength = 2;

    /// <summary>Sentinel indicating an unsupported newline string.</summary>
    private const int FailureNewLine = 3;

    /// <summary>
    /// Normalizes and validates <paramref name="options" /> without throwing.
    /// </summary>
    /// <param name="options">The options to normalize.</param>
    /// <param name="normalized">When this method returns, contains the normalized options.</param>
    /// <param name="failure">When this method returns <see langword="false" />, contains the failure sentinel.</param>
    /// <returns><see langword="true" /> when the options are valid; otherwise <see langword="false" />.</returns>
    private static bool TryNormalize(QuotedPrintableEncodingOptions options, out QuotedPrintableEncodingOptions normalized, out int failure)
    {
        int maxLineLength = options.MaxLineLength == 0 ? DefaultMaxLineLength : options.MaxLineLength;
        string newLine = options.NewLine ?? CrLf;
        normalized = new QuotedPrintableEncodingOptions(options.Mode, maxLineLength, newLine);

        if (options.Mode is not (QuotedPrintableEncodingMode.Binary or QuotedPrintableEncodingMode.Text))
        {
            failure = FailureMode;
            return false;
        }

        if (maxLineLength < MinMaxLineLength)
        {
            failure = FailureMaxLineLength;
            return false;
        }

        if (newLine is not (CrLf or Lf))
        {
            failure = FailureNewLine;
            return false;
        }

        failure = FailureNone;
        return true;
    }

    /// <summary>
    /// Writes <paramref name="value" /> into <paramref name="destination" /> at <paramref name="pos" /> (unless
    /// measuring or the destination is full) and advances the position.
    /// </summary>
    /// <param name="destination">The destination span.</param>
    /// <param name="measureOnly">When <see langword="true" />, the character is counted but not written.</param>
    /// <param name="value">The character to emit.</param>
    /// <param name="pos">The current write position, advanced by one.</param>
    /// <param name="overflow">Set to <see langword="true" /> when the destination has no room.</param>
    private static void EmitChar(Span<char> destination, bool measureOnly, char value, ref int pos, ref bool overflow)
    {
        if (!measureOnly)
        {
            if (pos < destination.Length)
                destination[pos] = value;
            else
                overflow = true;
        }

        pos++;
    }

    /// <summary>
    /// Writes <paramref name="value" /> into <paramref name="destination" /> at <paramref name="pos" /> (unless
    /// measuring or the destination is full) and advances the position.
    /// </summary>
    /// <param name="destination">The destination span.</param>
    /// <param name="measureOnly">When <see langword="true" />, the byte is counted but not written.</param>
    /// <param name="value">The byte to emit.</param>
    /// <param name="pos">The current write position, advanced by one.</param>
    /// <param name="overflow">Set to <see langword="true" /> when the destination has no room.</param>
    private static void EmitByte(Span<byte> destination, bool measureOnly, byte value, ref int pos, ref bool overflow)
    {
        if (!measureOnly)
        {
            if (pos < destination.Length)
                destination[pos] = value;
            else
                overflow = true;
        }

        pos++;
    }

    /// <summary>
    /// Returns the upper-case hexadecimal digit for the high nibble of <paramref name="value" />.
    /// </summary>
    /// <param name="value">The byte.</param>
    /// <returns>The high-nibble hex digit.</returns>
    private static char HexHigh(byte value) => ToHexDigit(value >> 4);

    /// <summary>
    /// Returns the upper-case hexadecimal digit for the low nibble of <paramref name="value" />.
    /// </summary>
    /// <param name="value">The byte.</param>
    /// <returns>The low-nibble hex digit.</returns>
    private static char HexLow(byte value) => ToHexDigit(value & 0x0F);

    /// <summary>
    /// Maps a nibble (0–15) to its upper-case hexadecimal digit.
    /// </summary>
    /// <param name="nibble">The nibble value.</param>
    /// <returns>The hexadecimal digit.</returns>
    private static char ToHexDigit(int nibble) => (char)(nibble < 10 ? '0' + nibble : 'A' + (nibble - 10));

    /// <summary>
    /// Attempts to map a hexadecimal digit to its value, honouring the lowercase relaxation.
    /// </summary>
    /// <param name="c">The candidate digit.</param>
    /// <param name="allowLowercase">Whether lowercase <c>a</c>–<c>f</c> are accepted.</param>
    /// <param name="value">When this method returns, contains the digit value.</param>
    /// <returns><see langword="true" /> when <paramref name="c" /> is an accepted hex digit.</returns>
    private static bool TryHexValue(char c, bool allowLowercase, out int value)
    {
        if (c >= '0' && c <= '9')
        {
            value = c - '0';
            return true;
        }

        if (c >= 'A' && c <= 'F')
        {
            value = (c - 'A') + 10;
            return true;
        }

        if (allowLowercase && c >= 'a' && c <= 'f')
        {
            value = (c - 'a') + 10;
            return true;
        }

        value = 0;
        return false;
    }
}
