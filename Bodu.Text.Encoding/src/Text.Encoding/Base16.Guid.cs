// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base16.Guid.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public static partial class Base16
{

    /// <summary>
    /// Decodes a hexadecimal representation of a <see cref="Guid" />.
    /// </summary>
    /// <param name="source">The hexadecimal characters (32 hex digits after any decoration is stripped).</param>
    /// <param name="styles">Parsing styles to apply when interpreting decorations.</param>
    /// <returns>The decoded <see cref="Guid" />.</returns>
    /// <exception cref="FormatException">Thrown when the input does not decode to exactly 16 bytes.</exception>
    public static Guid DecodeGuid(ReadOnlySpan<char> source, BaseFormatStyles styles = BaseFormatStyles.None)
    {
        Span<byte> bytes = stackalloc byte[16];
        return !TryDecode(source, bytes, out var written, styles) || written != 16
            ? throw new FormatException(EncodingResourceStrings.Format_Invalid_GuidNotSixteenBytes)
            : new Guid(bytes);
    }
    /// <summary>
    /// Encodes the byte representation of <paramref name="value" /> as a hexadecimal string.
    /// </summary>
    /// <param name="value">The <see cref="Guid" /> to encode.</param>
    /// <param name="options">Formatting options applied to the hex output (case, prefix, spacing, line breaks).</param>
    /// <returns>A Base16 string of the <see cref="Guid" />'s 16 underlying bytes (mixed-endian, matching
    /// <see cref="Guid.TryWriteBytes(Span{byte})" />).</returns>
    /// <remarks>
    /// The encoding uses the GUID's native byte layout — the first three fields are little-endian, the fourth is
    /// big-endian. This matches <see cref="Guid.ToByteArray()" /> and <see cref="Guid.TryWriteBytes(Span{byte})" />, so
    /// the result of <see cref="DecodeGuid(ReadOnlySpan{char}, BaseFormatStyles)" /> reconstructs the original
    /// <see cref="Guid" /> exactly.
    /// </remarks>
    public static string Encode(Guid value, BaseFormattingOptions options = BaseFormattingOptions.None)
    {
        Span<byte> bytes = stackalloc byte[16];
        value.TryWriteBytes(bytes);
        return Encode(bytes, options);
    }

    /// <summary>
    /// Attempts to decode a hexadecimal representation of a <see cref="Guid" />.
    /// </summary>
    /// <param name="source">The hexadecimal characters.</param>
    /// <param name="value">
    /// When this method returns, contains the decoded <see cref="Guid" /> or <see cref="Guid.Empty" /> when the input
    /// is invalid.
    /// </param>
    /// <param name="styles">Parsing styles to apply when interpreting decorations.</param>
    /// <returns><see langword="true" /> when decoding succeeds; otherwise <see langword="false" />.</returns>
    public static bool TryDecodeGuid(ReadOnlySpan<char> source, out Guid value, BaseFormatStyles styles = BaseFormatStyles.None)
    {
        Span<byte> bytes = stackalloc byte[16];
        if (!TryDecode(source, bytes, out var written, styles) || written != 16)
        {
            value = Guid.Empty;
            return false;
        }

        value = new Guid(bytes);
        return true;
    }
}
