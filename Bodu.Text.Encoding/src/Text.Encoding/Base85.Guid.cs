// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base85.Guid.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public static partial class Base85
{

    /// <summary>
    /// Decodes a Base85 representation of a <see cref="Guid" />.
    /// </summary>
    /// <param name="source">The Base85 characters (20 chars for Ascii85 or Z85; additional 4 with delimiters).</param>
    /// <param name="variant">The Base85 variant.</param>
    /// <param name="styles">Parsing styles.</param>
    /// <returns>The decoded <see cref="Guid" />.</returns>
    /// <exception cref="FormatException">Thrown when the input does not decode to exactly 16 bytes.</exception>
    public static Guid DecodeGuid(ReadOnlySpan<char> source, Base85Variant variant = Base85Variant.Ascii85, BaseFormatStyles styles = BaseFormatStyles.None)
    {
        Span<byte> bytes = stackalloc byte[16];
        if (!TryDecode(source, bytes, out int written, variant, styles) || written != 16)
            throw new FormatException("Input does not decode to a 16-byte GUID.");

        return new Guid(bytes);
    }
    /// <summary>
    /// Encodes the byte representation of <paramref name="value" /> as a Base85 string. 16 bytes encode to exactly
    /// 20 characters (Ascii85 or Z85).
    /// </summary>
    /// <param name="value">The <see cref="Guid" /> to encode.</param>
    /// <param name="variant">The Base85 variant.</param>
    /// <param name="options">Formatting options.</param>
    /// <returns>A Base85 string of the GUID bytes (mixed-endian, matching <see cref="Guid.TryWriteBytes(Span{byte})" />).</returns>
    public static string Encode(Guid value, Base85Variant variant = Base85Variant.Ascii85, BaseFormattingOptions options = BaseFormattingOptions.None)
    {
        Span<byte> bytes = stackalloc byte[16];
        value.TryWriteBytes(bytes);
        return Encode(bytes, variant, options);
    }

    /// <summary>
    /// Attempts to decode a Base85 representation of a <see cref="Guid" />.
    /// </summary>
    /// <param name="source">The Base85 characters.</param>
    /// <param name="value">When this method returns, contains the decoded <see cref="Guid" /> or
    /// <see cref="Guid.Empty" />.</param>
    /// <param name="variant">The Base85 variant.</param>
    /// <param name="styles">Parsing styles.</param>
    /// <returns><see langword="true" /> when decoding succeeds; otherwise <see langword="false" />.</returns>
    public static bool TryDecodeGuid(ReadOnlySpan<char> source, out Guid value, Base85Variant variant = Base85Variant.Ascii85, BaseFormatStyles styles = BaseFormatStyles.None)
    {
        Span<byte> bytes = stackalloc byte[16];
        if (!TryDecode(source, bytes, out int written, variant, styles) || written != 16)
        {
            value = Guid.Empty;
            return false;
        }

        value = new Guid(bytes);
        return true;
    }

}
