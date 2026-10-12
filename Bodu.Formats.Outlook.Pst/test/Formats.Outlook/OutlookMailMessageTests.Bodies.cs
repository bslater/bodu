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

    /// <summary>
    /// Verifies that an uncompressed (<c>MELA</c>) RTF body returns every byte after its 16-byte header whatever its CRC
    /// and declared sizes say, as MS-OXRTFCP 2.2.3.1 requires.
    /// </summary>
    /// <param name="testName">The fault in the header.</param>
    /// <param name="headerHex">The 16-byte header, in hexadecimal, that precedes the 20 bytes of RTF text.</param>
    [TestMethod]
    [DataRow("NonZeroCrc", "20000000140000004d454c41f1c5c7a7")]
    [DataRow("RawSizeAboveData", "20000000200000004d454c4100000000")]
    [DataRow("CompressedSizeBelowData", "14000000140000004d454c4100000000")]
    public void BodyRtf_WhenUncompressedHeaderDisagreesWithData_ShouldReturnEveryByteAfterTheHeader(string testName, string headerHex)
    {
        byte[] payload = Convert.FromHexString(headerHex + CompressedRtfTests.HelloWorldRtfHex);
        using OutlookMailStore store = OpenSynthetic(b => b.RtfPayloadOverride = payload);

        Assert.AreEqual(CompressedRtfTests.HelloWorldRtfText, GetFullMessage(store).BodyRtf, testName);
    }

    /// <summary>
    /// Verifies that a binary HTML body stored with a NUL terminator decodes to the text before it, while the property
    /// keeps the bytes as stored.
    /// </summary>
    [TestMethod]
    public void BodyHtml_WhenBinaryPayloadHoldsATerminator_ShouldEndAtTheTerminator()
    {
        using OutlookMailStore store = OpenSynthetic(static b => b.HtmlPayloadOverride = [.. "<p>x</p>"u8, 0x00]);

        OutlookMailMessage message = GetFullMessage(store);

        Assert.AreEqual("<p>x</p>", message.BodyHtml);
        Assert.AreEqual(9, message.Properties.GetBinary(MapiPropertyIds.Html)!.Value.Length);
    }

    /// <summary>
    /// Verifies that a message declaring code page 65000 decodes its <c>PT_STRING8</c> body as UTF-7 (RFC 2152):
    /// <c>Caf+AOk-</c> reads as <c>Café</c> rather than as its Windows-1252 characters.
    /// </summary>
    [TestMethod]
    public void BodyText_WhenMessageCodePageIsUtf7_ShouldDecodeUtf7()
    {
        using OutlookMailStore store = OpenSynthetic(static b =>
        {
            b.MessageCodePageOverride = 65000;
            b.BodyPayloadOverride = "Caf+AOk-"u8.ToArray();
        });

        Assert.AreEqual("Café", GetFullMessage(store).BodyText);
    }

    /// <summary>
    /// Verifies that a binary HTML body whose internet code page is a UTF-16 or UTF-32 code page decodes through that
    /// encoding, with or without a terminating NUL code unit, rather than through the message code page or
    /// Windows-1252.
    /// </summary>
    /// <param name="testName">The body's encoding and termination, by name.</param>
    /// <param name="codePage">The declared internet code page.</param>
    /// <param name="terminated">Whether the body ends with a NUL code unit.</param>
    [TestMethod]
    [DataRow("Utf16LE", 1200, false)]
    [DataRow("Utf16LETerminated", 1200, true)]
    [DataRow("Utf16BE", 1201, false)]
    [DataRow("Utf16BETerminated", 1201, true)]
    [DataRow("Utf32LE", 12000, false)]
    [DataRow("Utf32LETerminated", 12000, true)]
    [DataRow("Utf32BE", 12001, false)]
    [DataRow("Utf32BETerminated", 12001, true)]
    public void BodyHtml_WhenInternetCodePageIsUnicode_ShouldDecodeTheBody(
        string testName,
        int codePage,
        bool terminated)
    {
        byte[] html = System.Text.Encoding.GetEncoding(codePage).GetBytes(terminated ? "<p>x</p>\0" : "<p>x</p>");
        using OutlookMailStore store = OpenSynthetic(b =>
        {
            b.InternetCodePage = codePage;
            b.HtmlPayloadOverride = html;
        });

        Assert.AreEqual("<p>x</p>", GetFullMessage(store).BodyHtml, testName);
    }

    /// <summary>
    /// Verifies that the <c>PT_STRING8</c> body of a message declaring a UTF-32 code page, which cannot describe a
    /// code-page string, decodes through the Windows-1252 fallback: the bytes <c>48 69</c> read as <c>Hi</c> rather
    /// than as a replacement character.
    /// </summary>
    /// <param name="testName">The declared code page, by name.</param>
    /// <param name="codePage">The declared message code page.</param>
    [TestMethod]
    [DataRow("Utf32LE", 12000)]
    [DataRow("Utf32BE", 12001)]
    public void BodyText_WhenMessageCodePageIsUtf32_ShouldDecodeThroughTheFallback(string testName, int codePage)
    {
        using OutlookMailStore store = OpenSynthetic(b =>
        {
            b.MessageCodePageOverride = codePage;
            b.BodyPayloadOverride = "Hi"u8.ToArray();
        });

        Assert.AreEqual("Hi", GetFullMessage(store).BodyText, testName);
    }
}
