// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OutlookMailMessage.Bodies.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Formats.Outlook.Pst;

namespace Bodu.Formats.Outlook;

public sealed partial class OutlookMailMessage
{
    /// <summary>
    /// Gets the plain-text body.
    /// </summary>
    /// <value>The <c>PidTagBody</c> value, or <see langword="null" /> when absent.</value>
    public string? BodyText =>
        Properties.GetString(MapiPropertyIds.Body);

    /// <summary>
    /// Gets the HTML body.
    /// </summary>
    /// <value>
    /// The <c>PidTagHtml</c> payload decoded through the message's internet code page (falling back to the message
    /// code page), or the value verbatim when the writer stored it as a string; <see langword="null" /> when absent.
    /// </value>
    public string? BodyHtml
    {
        get
        {
            ReadOnlyMemory<byte>? bytes = Properties.GetBinary(MapiPropertyIds.Html);
            if (bytes is ReadOnlyMemory<byte> payload)
            {
                System.Text.Encoding encoding = MapiEncodingResolver.GetEncoding(
                    Properties.GetInt32(MapiPropertyIds.InternetCodepage),
                    Properties.GetInt32(MapiPropertyIds.MessageCodepage));
                return encoding.GetString(payload.Span);
            }

            return Properties.GetString(MapiPropertyIds.Html);
        }
    }

    /// <summary>
    /// Gets the RTF body, decompressed from <c>PidTagRtfCompressed</c> per MS-OXRTFCP.
    /// </summary>
    /// <value>
    /// The RTF text, or <see langword="null" /> when the property is absent or
    /// <see cref="OutlookMailStoreReaderOptions.DecompressRtf" /> is disabled (the raw payload stays available
    /// through <see cref="Properties" />).
    /// </value>
    /// <exception cref="OutlookPstFormatException">
    /// The compressed payload is malformed or fails its checksum.
    /// </exception>
    public string? BodyRtf
    {
        get
        {
            if (!_store.DecompressRtf)
                return null;

            ReadOnlyMemory<byte>? payload = Properties.GetBinary(MapiPropertyIds.RtfCompressed);
            if (payload is not ReadOnlyMemory<byte> bytes)
                return null;

            byte[] rtf = CompressedRtf.Decompress(bytes.Span);
            return System.Text.Encoding.Latin1.GetString(rtf);
        }
    }
}
