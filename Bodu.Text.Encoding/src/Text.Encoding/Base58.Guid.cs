// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base58.Guid.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public static partial class Base58
{
    /// <summary>
    /// Decodes a Base58 representation of a <see cref="Guid" />.
    /// </summary>
    /// <param name="source">The Base58 characters.</param>
    /// <param name="variant">The Base58 variant.</param>
    /// <param name="styles">Parsing styles.</param>
    /// <returns>The decoded <see cref="Guid" />.</returns>
    /// <exception cref="FormatException">Thrown when the input does not decode to exactly 16 bytes.</exception>
    public static Guid DecodeGuid(ReadOnlySpan<char> source, Base58Variant variant = Base58Variant.BitcoinFlickr, BaseFormatStyles styles = BaseFormatStyles.None)
    {
        Span<byte> bytes = stackalloc byte[16];
        return !TryDecode(source, bytes, out var written, variant, styles) || written != 16
            ? throw new FormatException(EncodingResourceStrings.Format_Invalid_GuidNotSixteenBytes)
            : new Guid(bytes);
    }

    /// <summary>
    /// Encodes the byte representation of <paramref name="value" /> as a Base58 string. Typical output is 22 characters
    /// (radix-58 representation of 16 bytes), though leading zero bytes inflate the count slightly.
    /// </summary>
    /// <param name="value">The <see cref="Guid" /> to encode.</param>
    /// <param name="variant">The Base58 variant.</param>
    /// <returns>
    /// A Base58 string of the GUID bytes (mixed-endian, matching <see cref="Guid.TryWriteBytes(Span{byte})" />).
    /// </returns>
    public static string Encode(Guid value, Base58Variant variant = Base58Variant.BitcoinFlickr)
    {
        Span<byte> bytes = stackalloc byte[16];
        value.TryWriteBytes(bytes);
        return Encode(bytes, variant);
    }

    /// <summary>
    /// Attempts to decode a Base58 representation of a <see cref="Guid" />.
    /// </summary>
    /// <param name="source">The Base58 characters.</param>
    /// <param name="value">
    /// When this method returns, contains the decoded <see cref="Guid" /> or <see cref="Guid.Empty" />.
    /// </param>
    /// <param name="variant">The Base58 variant.</param>
    /// <param name="styles">Parsing styles.</param>
    /// <returns><see langword="true" /> when decoding succeeds; otherwise <see langword="false" />.</returns>
    public static bool TryDecodeGuid(ReadOnlySpan<char> source, out Guid value, Base58Variant variant = Base58Variant.BitcoinFlickr, BaseFormatStyles styles = BaseFormatStyles.None)
    {
        Span<byte> bytes = stackalloc byte[16];
        if (!TryDecode(source, bytes, out var written, variant, styles) || written != 16)
        {
            value = Guid.Empty;
            return false;
        }

        value = new Guid(bytes);
        return true;
    }
}
