// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OutlookMessageTests.Bodies.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Formats.Outlook.Msg;

namespace Bodu.Formats.Outlook;

public partial class OutlookMessageTests
{
    /// <summary>
    /// Verifies that the plain-text body surfaces its property.
    /// </summary>
    [TestMethod]
    public void BodyText_WhenPresent_ShouldReturnValue()
    {
        using MemoryStream container = CreateMinimalContainer();

        using var message = OutlookMessage.OpenRead(container);

        Assert.AreEqual("Test body", message.BodyText);
    }

    /// <summary>
    /// Verifies that a binary HTML body decodes through the declared internet code page.
    /// </summary>
    [TestMethod]
    public void BodyHtml_WhenBinaryWithCodePage_ShouldDecodeThroughDeclaredEncoding()
    {
        const string Html = "<html><body>こんにちは</body></html>";
        byte[] bytes = MapiEncodingResolver.GetEncoding(932, null).GetBytes(Html);
        using MemoryStream container = MsgFixtureBuilder.CreateMinimal()
            .AddBinary(MapiPropertyIds.Html, bytes)
            .AddFixedEntry(0x3FDE0003, 932)
            .Build();

        using var message = OutlookMessage.OpenRead(container);

        Assert.AreEqual(Html, message.BodyHtml);
    }

    /// <summary>
    /// Verifies that the RTF body decompresses the specification example payload, and that clearing
    /// <see cref="OutlookMessageReaderOptions.DecompressRtf" /> suppresses the convenience while the raw payload stays
    /// reachable.
    /// </summary>
    [TestMethod]
    public void BodyRtf_WhenCompressedPayloadPresent_ShouldDecompressPerOptions()
    {
        byte[] payload = Convert.FromHexString(CompressedRtfTests.SpecExample1Hex);
        MemoryStream Create() => MsgFixtureBuilder.CreateMinimal()
            .AddBinary(MapiPropertyIds.RtfCompressed, payload)
            .Build();

        using (MemoryStream container = Create())
        using (var message = OutlookMessage.OpenRead(container))
        {
            Assert.AreEqual(CompressedRtfTests.SpecExample1Text, message.BodyRtf);
        }

        using (MemoryStream container = Create())
        using (var message = OutlookMessage.Open(container, new OutlookMessageReaderOptions { DecompressRtf = false }))
        {
            Assert.IsNull(message.BodyRtf);
            Assert.IsNotNull(message.Properties.GetBinary(MapiPropertyIds.RtfCompressed));
        }
    }

    /// <summary>
    /// Verifies that absent bodies surface as <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Bodies_WhenAbsent_ShouldReturnNull()
    {
        using MemoryStream container = new MsgFixtureBuilder()
            .AddUnicode(MapiPropertyIds.Subject, "No bodies")
            .Build();

        using var message = OutlookMessage.OpenRead(container);

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
        byte[] payload = Convert.FromHexString(CompressedRtfTests.SpecExample1Hex);
        byte[] html = System.Text.Encoding.ASCII.GetBytes("<html><body>cached</body></html>");
        using MemoryStream container = MsgFixtureBuilder.CreateMinimal()
            .AddBinary(MapiPropertyIds.RtfCompressed, payload)
            .AddBinary(MapiPropertyIds.Html, html)
            .Build();

        using var message = OutlookMessage.OpenRead(container);

        Assert.AreSame(message.BodyRtf, message.BodyRtf, "The RTF body must decode once and be cached.");
        Assert.AreSame(message.BodyHtml, message.BodyHtml, "The HTML body must decode once and be cached.");
    }

    /// <summary>
    /// Verifies that a compressed RTF body whose decompressed size exceeds
    /// <see cref="OutlookMessageReaderOptions.MaxDecompressedRtfBytes" /> is rejected with the reader's format
    /// exception rather than decompressed.
    /// </summary>
    [TestMethod]
    public void BodyRtf_WhenDecompressedSizeExceedsOption_ShouldThrowOutlookMsgFormatException()
    {
        byte[] payload = Convert.FromHexString(CompressedRtfTests.SpecExample1Hex);
        using MemoryStream container = MsgFixtureBuilder.CreateMinimal()
            .AddBinary(MapiPropertyIds.RtfCompressed, payload)
            .Build();

        using var message = OutlookMessage.Open(container, new OutlookMessageReaderOptions { MaxDecompressedRtfBytes = 16 });

        _ = Assert.ThrowsExactly<OutlookMsgFormatException>(() =>
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
        using MemoryStream container = MsgFixtureBuilder.CreateMinimal()
            .AddBinary(MapiPropertyIds.RtfCompressed, payload)
            .Build();

        using var message = OutlookMessage.OpenRead(container);

        Assert.AreEqual(CompressedRtfTests.HelloWorldRtfText, message.BodyRtf, testName);
    }

    /// <summary>
    /// Verifies that a binary HTML body stored with a NUL terminator decodes to the text before it, while the property
    /// keeps the bytes as stored.
    /// </summary>
    [TestMethod]
    public void BodyHtml_WhenBinaryPayloadHoldsATerminator_ShouldEndAtTheTerminator()
    {
        byte[] html = [.. "<p>x</p>"u8, 0x00];
        using MemoryStream container = new MsgFixtureBuilder()
            .AddBinary(MapiPropertyIds.Html, html)
            .Build();

        using var message = OutlookMessage.OpenRead(container);

        Assert.AreEqual("<p>x</p>", message.BodyHtml);
        Assert.AreEqual(9, message.Properties.GetBinary(MapiPropertyIds.Html)!.Value.Length);
    }

    /// <summary>
    /// Verifies that a binary HTML body whose message declares internet code page 65000 decodes as UTF-7 (RFC 2152).
    /// </summary>
    [TestMethod]
    public void BodyHtml_WhenInternetCodePageIsUtf7_ShouldDecodeUtf7()
    {
        using MemoryStream container = new MsgFixtureBuilder()
            .AddFixedEntry(0x3FDE0003, 65000)
            .AddBinary(MapiPropertyIds.Html, "<p>Gr+APYA3w-e</p>"u8.ToArray())
            .Build();

        using var message = OutlookMessage.OpenRead(container);

        Assert.AreEqual("<p>Größe</p>", message.BodyHtml);
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
        using MemoryStream container = new MsgFixtureBuilder()
            .AddFixedEntry(0x3FDE0003, (ulong)codePage)
            .AddBinary(MapiPropertyIds.Html, html)
            .Build();

        using var message = OutlookMessage.OpenRead(container);

        Assert.AreEqual("<p>x</p>", message.BodyHtml, testName);
    }
}
