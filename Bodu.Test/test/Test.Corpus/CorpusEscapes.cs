// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CorpusEscapes.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace Bodu.Test.Corpus;

/// <summary>
/// Decodes and encodes the escapes that the release-note fix catalogues under <c>corpus/</c> use in their <c>input</c>,
/// <c>expected</c> and <c>options</c> fields, which keep every catalogue printable ASCII.
/// </summary>
/// <remarks>
/// <para>
/// <c>\n</c> is a line feed, <c>\r</c> a carriage return, <c>\t</c> a tab, <c>\0</c> a NUL, <c>\\</c> a backslash,
/// <c>\xHH</c> the single byte 0xHH, and <c>\u{H...}</c> the UTF-8 encoding of the Unicode scalar value U+H..., written
/// with one to six hexadecimal digits. Every other printable ASCII character stands for its own byte.
/// </para>
/// <para>
/// Any other backslash sequence, and any character outside printable ASCII, is an error. Raw bytes, byte order marks
/// and non-ASCII text therefore reach a catalogue only through the escapes.
/// </para>
/// </remarks>
public static class CorpusEscapes
{
    /// <summary>The UTF-8 encoding that reads decoded fields as text, rejecting invalid sequences.</summary>
    private static readonly UTF8Encoding s_strictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    /// <summary>
    /// Attempts to decode an escaped field into the bytes it stands for.
    /// </summary>
    /// <param name="field">The escaped field, as it appears in the catalogue.</param>
    /// <param name="bytes">When the method returns <see langword="true" />, the decoded bytes; otherwise empty.</param>
    /// <param name="error">
    /// When the method returns <see langword="false" />, a description of the first problem.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when every character and escape in <paramref name="field" /> is valid.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="field" /> is <see langword="null" />.</exception>
    public static bool TryDecode(string field, out byte[] bytes, [NotNullWhen(false)] out string? error)
    {
        ArgumentNullException.ThrowIfNull(field);

        var output = new List<byte>(field.Length);
        Span<byte> utf8 = stackalloc byte[4];
        bytes = [];
        error = null;

        for (int i = 0; i < field.Length; i++)
        {
            char c = field[i];
            if (c is < ' ' or > '~')
            {
                error = $"the character U+{(int)c:X4} at position {i} is not printable ASCII";
                return false;
            }

            if (c != '\\')
            {
                output.Add((byte)c);
                continue;
            }

            if (i + 1 >= field.Length)
            {
                error = $"the backslash at position {i} ends the field";
                return false;
            }

            char next = field[++i];
            switch (next)
            {
                case 'n':
                    output.Add((byte)'\n');
                    break;

                case 'r':
                    output.Add((byte)'\r');
                    break;

                case 't':
                    output.Add((byte)'\t');
                    break;

                case '0':
                    output.Add(0);
                    break;

                case '\\':
                    output.Add((byte)'\\');
                    break;

                case 'x':
                    if (i + 2 >= field.Length || !char.IsAsciiHexDigit(field[i + 1]) || !char.IsAsciiHexDigit(field[i + 2]))
                    {
                        error = $"the \\x at position {i - 1} is not followed by two hexadecimal digits";
                        return false;
                    }

                    output.Add(byte.Parse(field.AsSpan(i + 1, 2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture));
                    i += 2;
                    break;

                case 'u':
                    if (!TryDecodeScalar(field, i, out Rune scalar, out int close))
                    {
                        error = $"the \\u at position {i - 1} is not written \\u{{H...}} around a Unicode scalar value";
                        return false;
                    }

                    int written = scalar.EncodeToUtf8(utf8);
                    for (int k = 0; k < written; k++)
                        output.Add(utf8[k]);

                    i = close;
                    break;

                default:
                    error = $"the escape \\{next} at position {i - 1} is not recognized";
                    return false;
            }
        }

        bytes = [.. output];
        return true;
    }

    /// <summary>
    /// Decodes an escaped field into the bytes it stands for.
    /// </summary>
    /// <param name="field">The escaped field, as it appears in the catalogue.</param>
    /// <returns>The decoded bytes.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="field" /> is <see langword="null" />.</exception>
    /// <exception cref="FormatException"><paramref name="field" /> holds a character or escape that is not valid.</exception>
    public static byte[] Decode(string field) =>
        TryDecode(field, out byte[] bytes, out string? error) ? bytes : throw new FormatException(error);

    /// <summary>
    /// Decodes an escaped field and reads the bytes it stands for as UTF-8 text.
    /// </summary>
    /// <param name="field">The escaped field, as it appears in the catalogue.</param>
    /// <returns>The decoded text.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="field" /> is <see langword="null" />.</exception>
    /// <exception cref="FormatException">
    /// <paramref name="field" /> holds a character or escape that is not valid, or the bytes it stands for are not valid
    /// UTF-8.
    /// </exception>
    public static string DecodeText(string field)
    {
        byte[] bytes = Decode(field);

        try
        {
            return s_strictUtf8.GetString(bytes);
        }
        catch (DecoderFallbackException ex)
        {
            throw new FormatException("The decoded field is not valid UTF-8.", ex);
        }
    }

    /// <summary>
    /// Writes bytes in the catalogue's escaped notation.
    /// </summary>
    /// <param name="bytes">The bytes to escape.</param>
    /// <returns>
    /// The escaped text: printable ASCII stays as it is, a backslash is doubled, a line feed, carriage return, tab and
    /// NUL use their short escapes, and every other byte is written <c>\xHH</c>.
    /// </returns>
    /// <remarks>
    /// The result decodes back to <paramref name="bytes" />, which makes it suitable for failure messages and for
    /// writing new rows. It never uses <c>\u{H...}</c>, so non-ASCII text appears byte by byte.
    /// </remarks>
    public static string Encode(ReadOnlySpan<byte> bytes)
    {
        var builder = new StringBuilder(bytes.Length);
        foreach (byte b in bytes)
        {
            switch (b)
            {
                case (byte)'\\':
                    builder.Append(@"\\");
                    break;

                case (byte)'\n':
                    builder.Append(@"\n");
                    break;

                case (byte)'\r':
                    builder.Append(@"\r");
                    break;

                case (byte)'\t':
                    builder.Append(@"\t");
                    break;

                case 0:
                    builder.Append(@"\0");
                    break;

                default:
                    if (b is >= (byte)' ' and <= (byte)'~')
                        builder.Append((char)b);
                    else
                        builder.Append(@"\x").Append(b.ToString("X2", CultureInfo.InvariantCulture));

                    break;
            }
        }

        return builder.ToString();
    }

    /// <summary>
    /// Attempts to read the <c>{H...}</c> part of a <c>\u</c> escape.
    /// </summary>
    /// <param name="field">The escaped field.</param>
    /// <param name="u">The index of the <c>u</c> that follows the backslash.</param>
    /// <param name="scalar">When the method returns <see langword="true" />, the Unicode scalar value.</param>
    /// <param name="close">When the method returns <see langword="true" />, the index of the closing brace.</param>
    /// <returns>
    /// <see langword="true" /> when the braces hold one to six hexadecimal digits naming a scalar value.
    /// </returns>
    private static bool TryDecodeScalar(string field, int u, out Rune scalar, out int close)
    {
        scalar = default;
        close = -1;

        if (u + 1 >= field.Length || field[u + 1] != '{')
            return false;

        close = field.IndexOf('}', u + 2);
        if (close < 0)
            return false;

        ReadOnlySpan<char> hex = field.AsSpan(u + 2, close - u - 2);
        if (hex.Length is < 1 or > 6)
            return false;

        foreach (char c in hex)
        {
            if (!char.IsAsciiHexDigit(c))
                return false;
        }

        int value = int.Parse(hex, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture);
        return Rune.TryCreate(value, out scalar);
    }
}
