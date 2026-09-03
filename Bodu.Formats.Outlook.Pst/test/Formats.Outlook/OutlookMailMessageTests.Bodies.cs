// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OutlookMailMessageTests.Bodies.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Formats.Outlook.Pst;

namespace Bodu.Formats.Outlook;

public partial class OutlookMailMessageTests
{
    /// <summary>
    /// Verifies that the plain-text body surfaces the code-page-decoded <c>PidTagBody</c> value.
    /// </summary>
    [TestMethod]
    public void BodyText_WhenSyntheticStore_ShouldReturnDecodedBody()
    {
        using OutlookMailStore store = OpenSynthetic();

        Assert.AreEqual(PstMessagingFixtureBuilder.BodyText, GetFullMessage(store).BodyText);
    }

    /// <summary>
    /// Verifies that the HTML body decodes the binary <c>PidTagHtml</c> payload under the message code page.
    /// </summary>
    [TestMethod]
    public void BodyHtml_WhenStoredAsBinary_ShouldDecodeWithMessageCodePage()
    {
        using OutlookMailStore store = OpenSynthetic();

        Assert.AreEqual(PstMessagingFixtureBuilder.HtmlBodyText, GetFullMessage(store).BodyHtml);
    }

    /// <summary>
    /// Verifies that the RTF body decompresses the <c>PidTagRtfCompressed</c> payload per MS-OXRTFCP.
    /// </summary>
    [TestMethod]
    public void BodyRtf_WhenDecompressionEnabled_ShouldReturnRtfText()
    {
        using OutlookMailStore store = OpenSynthetic();

        Assert.AreEqual(PstMessagingFixtureBuilder.RtfBodyText, GetFullMessage(store).BodyRtf);
    }

    /// <summary>
    /// Verifies that disabling decompression suppresses the RTF convenience while the raw payload stays reachable
    /// through the property collection.
    /// </summary>
    [TestMethod]
    public void BodyRtf_WhenDecompressionDisabled_ShouldReturnNull()
    {
        var builder = new PstMessagingFixtureBuilder();
        using OutlookMailStore store = OutlookMailStore.Open(
            builder.BuildStream(),
            new OutlookMailStoreReaderOptions { DecompressRtf = false });

        OutlookMailMessage message = GetFullMessage(store);

        Assert.IsNull(message.BodyRtf);
        Assert.IsNotNull(message.Properties.GetBinary(MapiPropertyIds.RtfCompressed));
    }

    /// <summary>
    /// Verifies that a truncated compressed-RTF payload throws the PST reader's format exception.
    /// </summary>
    [TestMethod]
    public void BodyRtf_WhenPayloadTruncated_ShouldThrowOutlookPstFormatException()
    {
        using OutlookMailStore store = OpenSynthetic(static b => b.TruncateRtfPayload = true);

        OutlookMailMessage message = GetFullMessage(store);

        _ = Assert.ThrowsExactly<OutlookPstFormatException>(() =>
        {
            _ = message.BodyRtf;
        });
    }

    /// <summary>
    /// Verifies that a message without body properties reports every body convenience as absent.
    /// </summary>
    [TestMethod]
    public void BodyText_WhenBodiesAbsent_ShouldReturnNull()
    {
        using OutlookMailStore store = OpenSynthetic();

        OutlookMailMessage message = GetPlainMessage(store);

        Assert.IsNull(message.BodyText);
        Assert.IsNull(message.BodyHtml);
        Assert.IsNull(message.BodyRtf);
    }

    /// <summary>
    /// Verifies that the decoded RTF and HTML bodies are computed once and cached: a second read returns the same
    /// instance rather than re-running the decompression and code-page decode.
    /// </summary>
    [TestMethod]
    public void BodyRtf_WhenReadTwice_ShouldReturnCachedInstance()
    {
        using OutlookMailStore store = OpenSynthetic();

        OutlookMailMessage message = GetFullMessage(store);

        Assert.AreSame(message.BodyRtf, message.BodyRtf, "The RTF body must decode once and be cached.");
        Assert.AreSame(message.BodyHtml, message.BodyHtml, "The HTML body must decode once and be cached.");
    }

    /// <summary>
    /// Verifies that an RTF body whose decompressed size exceeds
    /// <see cref="OutlookMailStoreReaderOptions.MaxDecompressedRtfBytes" /> is rejected with the reader's format
    /// exception rather than decoded.
    /// </summary>
    [TestMethod]
    public void BodyRtf_WhenDecompressedSizeExceedsOption_ShouldThrowOutlookPstFormatException()
    {
        var builder = new PstMessagingFixtureBuilder();
        using OutlookMailStore store = OutlookMailStore.Open(
            builder.BuildStream(),
            new OutlookMailStoreReaderOptions { MaxDecompressedRtfBytes = 16 });

        OutlookMailMessage message = GetFullMessage(store);

        _ = Assert.ThrowsExactly<OutlookPstFormatException>(() =>
        {
            _ = message.BodyRtf;
        });
    }
}
