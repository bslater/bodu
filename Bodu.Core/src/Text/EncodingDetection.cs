// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EncodingDetection.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace Bodu.Text;

/// <summary>
/// Provides byte-order-mark (BOM) based heuristics for detecting which <see cref="System.Text.Encoding" /> was used to
/// produce a byte sequence.
/// </summary>
/// <remarks>
/// Detection is intentionally limited to the five canonical Unicode byte-order-marks defined by the Unicode standard:
/// <list type="bullet">
/// <item>
/// <description>UTF-8: <c>EF BB BF</c></description>
/// </item>
/// <item>
/// <description>UTF-16 little endian: <c>FF FE</c></description>
/// </item>
/// <item>
/// <description>UTF-16 big endian: <c>FE FF</c></description>
/// </item>
/// <item>
/// <description>UTF-32 little endian: <c>FF FE 00 00</c></description>
/// </item>
/// <item>
/// <description>UTF-32 big endian: <c>00 00 FE FF</c></description>
/// </item>
/// </list>
/// <para>
/// The UTF-32 little-endian preamble shares its first two bytes with UTF-16 little endian; this implementation
/// disambiguates them by inspecting the third and fourth bytes.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// // Read a file's leading bytes and decode using the detected encoding, falling back to UTF-8.
/// byte[] bytes = File.ReadAllBytes(path);
///
/// System.Text.Encoding encoding =
///     EncodingDetection.TryDetectByPreamble(bytes, out System.Text.Encoding? detected)
///         ? detected
///         : System.Text.Encoding.UTF8;
///
/// string text = encoding.GetStringSkippingPreamble(bytes);
///]]>
/// </code>
/// </example>
public static class EncodingDetection
{
    /// <summary>
    /// Attempts to detect the encoding of <paramref name="bytes" /> by examining its leading byte-order-mark.
    /// </summary>
    /// <param name="bytes">The byte span to inspect.</param>
    /// <param name="encoding">
    /// When this method returns <see langword="true" />, contains the detected <see cref="System.Text.Encoding" />
    /// instance (one of <see cref="System.Text.Encoding.UTF8" />, <see cref="System.Text.Encoding.Unicode" />,
    /// <see cref="System.Text.Encoding.BigEndianUnicode" />, or <see cref="System.Text.Encoding.UTF32" />, or the
    /// UTF-32 big-endian instance returned by <see cref="System.Text.Encoding.GetEncoding(int)" /> for codepage
    /// <c>12001</c>); otherwise <see langword="null" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when a recognized BOM is detected; <see langword="false" /> when no BOM is present or
    /// when the leading bytes do not match a known preamble.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method does not allocate. UTF-32 little-endian detection requires inspecting four bytes; when the span is
    /// shorter than four bytes the UTF-16 little-endian preamble is reported instead, matching
    /// <see cref="System.IO.StreamReader" /> behaviour.
    /// </para>
    /// <para>
    /// The UTF-16 little-endian BOM (<c>FF FE</c>) is a strict prefix of the UTF-32 little-endian BOM (<c>FF FE 00 00</c>),
    /// so the two are inherently ambiguous: a UTF-16 little-endian document whose first character is U+0000 begins with
    /// the same four bytes. This method resolves the ambiguity in favour of UTF-32 little-endian whenever all four
    /// bytes match, again matching <see cref="System.IO.StreamReader" />.
    /// </para>
    /// </remarks>
    public static bool TryDetectByPreamble(
        ReadOnlySpan<byte> bytes,
        [NotNullWhen(true)] out System.Text.Encoding? encoding)
    {
        if (bytes.Length >= 4 && bytes[0] == 0x00 && bytes[1] == 0x00 && bytes[2] == 0xFE && bytes[3] == 0xFF)
        {
            encoding = System.Text.Encoding.GetEncoding(12001);
            return true;
        }

        if (bytes.Length >= 4 && bytes[0] == 0xFF && bytes[1] == 0xFE && bytes[2] == 0x00 && bytes[3] == 0x00)
        {
            encoding = System.Text.Encoding.UTF32;
            return true;
        }

        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
        {
            encoding = System.Text.Encoding.UTF8;
            return true;
        }

        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
        {
            encoding = System.Text.Encoding.Unicode;
            return true;
        }

        if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
        {
            encoding = System.Text.Encoding.BigEndianUnicode;
            return true;
        }

        encoding = null;
        return false;
    }
}
