// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OutlookMailMessage.Bodies.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Formats.Outlook.Pst;

namespace Bodu.Formats.Outlook;

public sealed partial class OutlookMailMessage
{
    /// <summary>The decoded HTML body, valid once <see cref="_bodyHtmlDecoded" /> is set.</summary>
    private string? _bodyHtml;

    /// <summary>Whether <see cref="_bodyHtml" /> has been decoded.</summary>
    private bool _bodyHtmlDecoded;

    /// <summary>The decoded RTF body, valid once <see cref="_bodyRtfDecoded" /> is set.</summary>
    private string? _bodyRtf;

    /// <summary>Whether <see cref="_bodyRtf" /> has been decoded.</summary>
    private bool _bodyRtfDecoded;

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
    /// The <c>PidTagHtml</c> payload decoded through the message's internet code page (falling back to the message code
    /// page), or the value verbatim when the writer stored it as a string; <see langword="null" /> when absent. The
    /// body is decoded once and the same instance returned thereafter.
    /// </value>
    /// <exception cref="ObjectDisposedException">The owning session has been disposed.</exception>
    public string? BodyHtml
    {
        get
        {
            MapiPropertyCollection properties = Properties;
            if (!_bodyHtmlDecoded)
            {
                _bodyHtml = MapiBodies.DecodeHtml(properties);
                _bodyHtmlDecoded = true;
            }

            return _bodyHtml;
        }
    }

    /// <summary>
    /// Gets the RTF body, decompressed from <c>PidTagRtfCompressed</c> per MS-OXRTFCP.
    /// </summary>
    /// <value>
    /// The RTF text, or <see langword="null" /> when the property is absent or
    /// <see cref="OutlookMailStoreReaderOptions.DecompressRtf" /> is disabled (the raw payload stays available through
    /// <see cref="Properties" />). The body is decompressed once and the same instance returned thereafter.
    /// </value>
    /// <exception cref="ObjectDisposedException">The owning session has been disposed.</exception>
    /// <exception cref="OutlookPstFormatException">
    /// The compressed payload is malformed, fails its checksum, or decompresses beyond
    /// <see cref="OutlookMailStoreReaderOptions.MaxDecompressedRtfBytes" />.
    /// </exception>
    public string? BodyRtf
    {
        get
        {
            MapiPropertyCollection properties = Properties;
            if (!_store.DecompressRtf)
                return null;

            if (!_bodyRtfDecoded)
            {
                _bodyRtf = MapiBodies.DecodeRtf(properties, _store.MaxDecompressedRtfBytes);
                _bodyRtfDecoded = true;
            }

            return _bodyRtf;
        }
    }
}
