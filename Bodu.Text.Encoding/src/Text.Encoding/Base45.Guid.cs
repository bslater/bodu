// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base45.Guid.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public static partial class Base45
{
    /// <summary>
    /// Decodes a Base45 representation of a <see cref="Guid" />.
    /// </summary>
    /// <param name="source">The Base45 characters.</param>
    /// <param name="styles">
    /// Parsing styles. Only <see cref="BaseFormatStyles.IgnoreWhitespace" /> has effect, and it skips tab, carriage
    /// return, and line feed but never the space symbol.
    /// </param>
    /// <returns>The decoded <see cref="Guid" />.</returns>
    /// <exception cref="FormatException">
    /// Thrown when the input is not valid or does not decode to exactly 16 bytes.
    /// </exception>
    public static Guid DecodeGuid(ReadOnlySpan<char> source, BaseFormatStyles styles = BaseFormatStyles.None)
    {
        Span<byte> bytes = stackalloc byte[16];
        return !TryDecode(source, bytes, out int written, styles) || written != 16
            ? throw new FormatException(EncodingResourceStrings.Format_Invalid_GuidNotSixteenBytes)
            : new Guid(bytes);
    }

    /// <summary>
    /// Encodes the byte representation of <paramref name="value" /> as a Base45 string of 24 characters: eight
    /// three-character groups, one for each pair of the 16 bytes.
    /// </summary>
    /// <param name="value">The <see cref="Guid" /> to encode.</param>
    /// <returns>
    /// A Base45 string of the GUID bytes, in the mixed-endian layout of <see cref="Guid.TryWriteBytes(Span{byte})" />.
    /// </returns>
    public static string Encode(Guid value)
    {
        Span<byte> bytes = stackalloc byte[16];
        _ = value.TryWriteBytes(bytes);
        return Encode(bytes);
    }

    /// <summary>
    /// Attempts to decode a Base45 representation of a <see cref="Guid" />.
    /// </summary>
    /// <param name="source">The Base45 characters.</param>
    /// <param name="value">
    /// When this method returns, contains the decoded <see cref="Guid" />, or <see cref="Guid.Empty" /> on failure.
    /// </param>
    /// <param name="styles">
    /// Parsing styles. Only <see cref="BaseFormatStyles.IgnoreWhitespace" /> has effect, and it skips tab, carriage
    /// return, and line feed but never the space symbol.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when the input decodes to exactly 16 bytes; otherwise <see langword="false" />.
    /// </returns>
    public static bool TryDecodeGuid(ReadOnlySpan<char> source, out Guid value, BaseFormatStyles styles = BaseFormatStyles.None)
    {
        Span<byte> bytes = stackalloc byte[16];
        if (!TryDecode(source, bytes, out int written, styles) || written != 16)
        {
            value = Guid.Empty;
            return false;
        }

        value = new Guid(bytes);
        return true;
    }
}
