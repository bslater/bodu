// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PercentEncoding.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

/// <summary>
/// Provides span-first percent-encoding and decoding of URI components and form-style values (RFC 3986 §2.1 with the
/// WHATWG <c>application/x-www-form-urlencoded</c> rules), selecting the unescaped character set by component mode.
/// </summary>
/// <remarks>
/// <para>
/// Percent-encoding represents an octet as <c>%HH</c> using uppercase hexadecimal digits. Each
/// <see cref="PercentEncodingMode" /> selects which bytes pass through unescaped — from the conservative
/// unreserved-only <see cref="PercentEncodingMode.UriComponent" /> set through to the WHATWG
/// <see cref="PercentEncodingMode.FormUrlEncoded" /> set, where space becomes <c>+</c>. Encoding always emits uppercase
/// hex; decoding accepts both uppercase and lowercase, which RFC 3986 defines as equivalent.
/// </para>
/// <para>
/// The byte-oriented surfaces operate on ASCII percent-encoded text; the <see cref="EncodeString" /> and
/// <see cref="DecodeString" /> helpers bridge .NET strings through a configurable text encoding (UTF-8 by default).
/// This type is not a URL parser: it does not resolve relative URLs, normalise hosts, paths, dot-segments, or scheme
/// casing, implement IDNA or IRI processing, accept the obsolete <c>%uXXXX</c> syntax, or depend on <c>System.Web</c>.
/// </para>
/// <para>
/// Because correct use depends on the component mode and decoding options, this is a static type and is intentionally
/// not registered in <see cref="BinaryEncodings" /> — the parameterless <see cref="IBinaryEncoding" /> contract cannot
/// carry that information.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// // URI component (default) — only unreserved characters pass through.
/// string component = PercentEncoding.EncodeString("a/b?c=d");   // a%2Fb%3Fc%3Dd
///
/// // Form field — space becomes '+'.
/// string field = PercentEncoding.EncodeString("a b+c", mode: PercentEncodingMode.FormUrlEncoded); // a+b%2Bc
///
/// // Round-trip.
/// string value = PercentEncoding.DecodeString("a%2Fb", mode: PercentEncodingMode.UriComponent);   // a/b
///]]>
/// </code>
/// </example>
public static partial class PercentEncoding
{
    /// <summary>The pass-through table for each <see cref="PercentEncodingMode" />, indexed by mode then byte value.</summary>
    private static readonly bool[][] s_passThrough =
    [
        BuildTable(static b => IsUnreserved(b)),
        BuildTable(static b => IsUnreserved(b) || IsSubDelimiter(b) || b == ':' || b == '@'),
        BuildTable(static b => IsUnreserved(b) || IsSubDelimiter(b) || b == ':' || b == '@' || b == '/' || b == '?'),
        BuildTable(static b => IsAlphaNumeric(b) || b == '*' || b == '-' || b == '.' || b == '_'),
    ];

    /// <summary>
    /// Returns the maximum number of characters that encoding <paramref name="byteCount" /> bytes could produce.
    /// </summary>
    /// <param name="byteCount">The input byte count. Must be non-negative.</param>
    /// <returns>The worst-case encoded character count, <c>byteCount * 3</c>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="byteCount" /> is negative.</exception>
    public static int GetMaxEncodedLength(int byteCount)
    {
        ThrowHelper.ThrowIfNegative(byteCount);
        checked
        {
            return byteCount * 3;
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
    /// Indicates whether <paramref name="source" /> is the <em>canonical</em> percent-encoded form for the supplied
    /// mode — every literal character is one the mode leaves unescaped, and every percent sequence is well-formed.
    /// </summary>
    /// <param name="source">The input characters.</param>
    /// <param name="mode">The component mode.</param>
    /// <param name="options">The decoding options.</param>
    /// <returns>
    /// <see langword="true" /> when the input is canonical for the mode; otherwise <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// This is stricter than <see cref="Decode(ReadOnlySpan{char}, PercentEncodingMode, PercentDecodingOptions)" />,
    /// which performs lenient recovery: a literal character the mode would have percent-encoded (for example <c>#</c>
    /// in <see cref="PercentEncodingMode.UriComponent" />, or a literal space) makes <c>IsValid</c> return
    /// <see langword="false" /> even though <c>Decode</c> would still accept it. A percent-escaped octet such as
    /// <c>%2F</c> is always canonical, so <c>IsValid</c> accepts it even when a literal <c>/</c> would not be allowed.
    /// <see cref="IsValid(ReadOnlySpan{char}, PercentEncodingMode, PercentDecodingOptions)" /> returning
    /// <see langword="true" /> implies <c>Decode</c> succeeds, but not the converse.
    /// </remarks>
    public static bool IsValid(ReadOnlySpan<char> source, PercentEncodingMode mode = PercentEncodingMode.UriComponent, PercentDecodingOptions options = PercentDecodingOptions.None)
    {
        if (!IsDefinedMode(mode))
            return false;

        return TryDecodeCore(source, mode, options, Span<byte>.Empty, measureOnly: true, requireCanonical: true, out _);
    }

    /// <summary>
    /// Attempts to determine the exact number of bytes that
    /// <see cref="Decode(ReadOnlySpan{char}, PercentEncodingMode, PercentDecodingOptions)" /> will write for the
    /// supplied input.
    /// </summary>
    /// <param name="source">The input characters.</param>
    /// <param name="byteCount">
    /// When this method returns, contains the decoded byte count, or <c>0</c> on failure.
    /// </param>
    /// <param name="mode">The component mode.</param>
    /// <param name="options">The decoding options.</param>
    /// <returns><see langword="true" /> when the input is decodable; otherwise <see langword="false" />.</returns>
    /// <remarks>
    /// This mirrors <see cref="Decode(ReadOnlySpan{char}, PercentEncodingMode, PercentDecodingOptions)" /> (lenient
    /// recovery), not <see cref="IsValid(ReadOnlySpan{char}, PercentEncodingMode, PercentDecodingOptions)" />
    /// (canonical conformance): it returns the exact length <c>Decode</c> would write, so the result can size a decode
    /// buffer even for decodable-but-non-canonical input.
    /// </remarks>
    public static bool TryGetDecodedLength(ReadOnlySpan<char> source, out int byteCount, PercentEncodingMode mode = PercentEncodingMode.UriComponent, PercentDecodingOptions options = PercentDecodingOptions.None)
    {
        if (!IsDefinedMode(mode))
        {
            byteCount = 0;
            return false;
        }

        return TryDecodeCore(source, mode, options, Span<byte>.Empty, measureOnly: true, requireCanonical: false, out byteCount);
    }

    /// <summary>
    /// Builds a 256-entry pass-through table from <paramref name="predicate" />.
    /// </summary>
    /// <param name="predicate">A predicate selecting the bytes that pass through unescaped.</param>
    /// <returns>The pass-through table.</returns>
    private static bool[] BuildTable(Func<byte, bool> predicate)
    {
        bool[] table = new bool[256];
        for (int b = 0; b < 256; b++)
            table[b] = predicate((byte)b);

        return table;
    }

    /// <summary>
    /// Indicates whether <paramref name="b" /> is an RFC 3986 unreserved character.
    /// </summary>
    /// <param name="b">The byte to test.</param>
    /// <returns><see langword="true" /> when the byte is unreserved.</returns>
    private static bool IsUnreserved(byte b) =>
        IsAlphaNumeric(b) || b == '-' || b == '.' || b == '_' || b == '~';

    /// <summary>
    /// Indicates whether <paramref name="b" /> is an ASCII letter or digit.
    /// </summary>
    /// <param name="b">The byte to test.</param>
    /// <returns><see langword="true" /> when the byte is alphanumeric.</returns>
    private static bool IsAlphaNumeric(byte b) =>
        (b >= 'A' && b <= 'Z') || (b >= 'a' && b <= 'z') || (b >= '0' && b <= '9');

    /// <summary>
    /// Indicates whether <paramref name="b" /> is an RFC 3986 sub-delimiter.
    /// </summary>
    /// <param name="b">The byte to test.</param>
    /// <returns><see langword="true" /> when the byte is a sub-delimiter.</returns>
    private static bool IsSubDelimiter(byte b) =>
        b is (byte)'!' or (byte)'$' or (byte)'&' or (byte)'\'' or (byte)'(' or (byte)')'
            or (byte)'*' or (byte)'+' or (byte)',' or (byte)';' or (byte)'=';

    /// <summary>
    /// Indicates whether <paramref name="mode" /> is a defined enum value.
    /// </summary>
    /// <param name="mode">The mode to test.</param>
    /// <returns><see langword="true" /> when the mode is defined.</returns>
    private static bool IsDefinedMode(PercentEncodingMode mode) =>
        mode is PercentEncodingMode.UriComponent or PercentEncodingMode.PathSegment
            or PercentEncodingMode.Query or PercentEncodingMode.FormUrlEncoded;

    /// <summary>
    /// Validates that <paramref name="mode" /> is a defined enum value.
    /// </summary>
    /// <param name="mode">The mode to test.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="mode" /> is undefined.</exception>
    private static void EnsureValidMode(PercentEncodingMode mode)
    {
        if (!IsDefinedMode(mode))
            throw new ArgumentOutOfRangeException(nameof(mode), mode, EncodingResourceStrings.Arg_OutOfRange_PercentEncodingMode);
    }

    /// <summary>
    /// Returns the upper-case hexadecimal digit for <paramref name="nibble" /> (0–15).
    /// </summary>
    /// <param name="nibble">The nibble value.</param>
    /// <returns>The hexadecimal digit.</returns>
    private static char ToHexDigit(int nibble) => (char)(nibble < 10 ? '0' + nibble : 'A' + (nibble - 10));

    /// <summary>
    /// Attempts to map a hexadecimal digit (upper or lower case) to its value.
    /// </summary>
    /// <param name="c">The candidate digit.</param>
    /// <param name="value">When this method returns, contains the digit value.</param>
    /// <returns><see langword="true" /> when <paramref name="c" /> is a hexadecimal digit.</returns>
    private static bool TryHexValue(char c, out int value)
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

        if (c >= 'a' && c <= 'f')
        {
            value = (c - 'a') + 10;
            return true;
        }

        value = 0;
        return false;
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
}
