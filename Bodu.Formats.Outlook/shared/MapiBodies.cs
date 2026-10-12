// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MapiBodies.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

#if MSG
namespace Bodu.Formats.Outlook.Msg;
#elif OUTLOOK_PST
namespace Bodu.Formats.Outlook.Pst;
#endif

/// <summary>
/// Decodes the HTML and RTF message bodies from a decoded property collection, independent of the container the
/// properties came from.
/// </summary>
/// <remarks>
/// The HTML body is <c>PidTagHtml</c>: a byte payload decoded through the internet code page (falling back to the
/// message code page), which may name any code page .NET serves, UTF-16 and UTF-32 included, or the string value when
/// the writer stored it as a string; either way the text ends at its first NUL. The RTF body is
/// <c>PidTagRtfCompressed</c>, decompressed per MS-OXRTFCP under the caller's output ceiling. This file lives in
/// <c>Bodu.Formats.Outlook/shared/</c> and is source-compiled into each Outlook format reader; the consuming project
/// selects the namespace via its <c>DefineConstants</c>.
/// </remarks>
internal static class MapiBodies
{
    /// <summary>
    /// Decodes the HTML body.
    /// </summary>
    /// <param name="properties">The message's decoded properties.</param>
    /// <returns>
    /// The HTML text, which ends at its first NUL, or <see langword="null" /> when the property is absent.
    /// </returns>
    internal static string? DecodeHtml(MapiPropertyCollection properties)
    {
        if (properties.GetBinary(MapiPropertyIds.Html) is ReadOnlyMemory<byte> payload)
        {
            Encoding encoding = MapiEncodingResolver.GetHtmlEncoding(
                properties.GetInt32(MapiPropertyIds.InternetCodepage),
                properties.GetInt32(MapiPropertyIds.MessageCodepage));
            string text = encoding.GetString(payload.Span);

            // A writer may store the body with a terminating NUL. The text is cut at its first NUL character rather
            // than at a zero byte, because whether a zero byte is a NUL depends on the code page.
            int terminator = text.AsSpan().IndexOf('\0');
            return terminator < 0 ? text : text[..terminator];
        }

        return properties.GetString(MapiPropertyIds.Html);
    }

    /// <summary>
    /// Decodes the RTF body from its compressed form.
    /// </summary>
    /// <param name="properties">The message's decoded properties.</param>
    /// <param name="maxDecompressedBytes">The largest decompressed size the caller accepts.</param>
    /// <returns>The RTF text, or <see langword="null" /> when the property is absent.</returns>
    /// <exception cref="OutlookFormatException">
    /// The compressed payload is malformed, fails its checksum, or exceeds <paramref name="maxDecompressedBytes" />.
    /// The concrete type is the consuming format's exception.
    /// </exception>
    internal static string? DecodeRtf(MapiPropertyCollection properties, int maxDecompressedBytes)
    {
        if (properties.GetBinary(MapiPropertyIds.RtfCompressed) is not ReadOnlyMemory<byte> payload)
            return null;

        return Encoding.Latin1.GetString(CompressedRtf.Decompress(payload.Span, maxDecompressedBytes));
    }
}
