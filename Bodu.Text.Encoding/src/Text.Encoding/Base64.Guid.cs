// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base64.Guid.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public static partial class Base64
{

    /// <summary>
    /// Decodes a Base64 representation of a <see cref="Guid" />.
    /// </summary>
    /// <param name="source">The Base64 characters.</param>
    /// <param name="variant">The Base64 variant.</param>
    /// <param name="styles">Parsing styles.</param>
    /// <returns>The decoded <see cref="Guid" />.</returns>
    /// <exception cref="FormatException">Thrown when the input does not decode to exactly 16 bytes.</exception>
    public static Guid DecodeGuid(ReadOnlySpan<char> source, Base64Variant variant = Base64Variant.Standard, BaseFormatStyles styles = BaseFormatStyles.None)
    {
        Span<byte> bytes = stackalloc byte[16];
        return !TryDecode(source, bytes, out var written, variant, styles) || written != 16
            ? throw new FormatException(EncodingResourceStrings.Format_Invalid_GuidNotSixteenBytes)
            : new Guid(bytes);
    }
    /// <summary>
    /// Encodes the byte representation of <paramref name="value" /> as a Base64 string. With RFC 4648 padding the
    /// result is 24 characters; without padding it is 22 characters.
    /// </summary>
    /// <param name="value">The <see cref="Guid" /> to encode.</param>
    /// <param name="variant">The Base64 variant.</param>
    /// <param name="options">Formatting options.</param>
    /// <returns>
    /// A Base64 string of the GUID bytes (mixed-endian, matching <see cref="Guid.TryWriteBytes(Span{byte})" />).
    /// </returns>
    public static string Encode(Guid value, Base64Variant variant = Base64Variant.Standard, BaseFormattingOptions options = BaseFormattingOptions.None)
    {
        Span<byte> bytes = stackalloc byte[16];
        value.TryWriteBytes(bytes);
        return Encode(bytes, variant, options);
    }

    /// <summary>
    /// Attempts to decode a Base64 representation of a <see cref="Guid" />.
    /// </summary>
    /// <param name="source">The Base64 characters.</param>
    /// <param name="value">
    /// When this method returns, contains the decoded <see cref="Guid" /> or <see cref="Guid.Empty" />.
    /// </param>
    /// <param name="variant">The Base64 variant.</param>
    /// <param name="styles">Parsing styles.</param>
    /// <returns><see langword="true" /> when decoding succeeds; otherwise <see langword="false" />.</returns>
    public static bool TryDecodeGuid(ReadOnlySpan<char> source, out Guid value, Base64Variant variant = Base64Variant.Standard, BaseFormatStyles styles = BaseFormatStyles.None)
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
