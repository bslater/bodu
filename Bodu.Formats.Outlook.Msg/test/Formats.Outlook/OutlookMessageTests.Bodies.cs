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
        byte[] bytes = MsgEncodingResolver.GetEncoding(932, null).GetBytes(Html);
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
}
