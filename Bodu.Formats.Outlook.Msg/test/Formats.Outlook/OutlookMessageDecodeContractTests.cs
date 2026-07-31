// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OutlookMessageDecodeContractTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Formats.Outlook.Msg;
using Bodu.IO.Compound;
using Bodu.Test;

namespace Bodu.Formats.Outlook;

/// <summary>
/// Verifies, over one comprehensive synthetic message exercising every feature the reader ships, that the full decode
/// contract holds — and holds identically under strict validation, since a well-formed message must never trip the
/// strict checks.
/// </summary>
/// <remarks>
/// The corpus is synthetic (authored through <c>Bodu.IO.Compound</c>) because no licensed real-world corpus is
/// available in this environment; assembling one, with a provenance <c>NOTICE.md</c>, is a recorded roadmap follow-up.
/// </remarks>
[TestClass]
public class OutlookMessageDecodeContractTests
{
    /// <summary>The PS_PUBLIC_STRINGS property-set GUID.</summary>
    private static readonly Guid s_publicStrings = new("00020329-0000-0000-C000-000000000046");

    /// <summary>
    /// Verifies the full decode contract under both validation levels.
    /// </summary>
    /// <param name="testName">The scenario label.</param>
    /// <param name="validationLevel">The validation level to open under.</param>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    [DataRow("Compatible", CompoundValidationLevel.Compatible)]
    [DataRow("Strict", CompoundValidationLevel.Strict)]
    public void OpenRead_WhenComprehensiveMessage_ShouldDecodeEveryFeature(string testName, CompoundValidationLevel validationLevel)
    {
        _ = testName;

        var sent = new DateTimeOffset(2026, 7, 30, 8, 0, 0, TimeSpan.Zero);
        byte[] attachmentPayload = { 0xCA, 0xFE };
        using MemoryStream container = MsgFixtureBuilder.CreateMinimal()
            .AddUnicode(MapiPropertyIds.SenderName, "Alex Doe")
            .AddString8(MapiPropertyIds.SenderEmailAddress, "alex@example.com", 1252)
            .AddFixedEntry(0x3FFD0003, 1252)
            .AddFixedEntry(0x00390040, (ulong)sent.ToFileTime())
            .AddMultiValueUnicode(0x8000, "urgent", "finance")
            .AddBinary(MapiPropertyIds.RtfCompressed, Convert.FromHexString(CompressedRtfTests.SpecExample1Hex))
            .WithNameId(
                Array.Empty<byte>(),
                MsgFixtureBuilder.NameIdEntry(0, 2, isString: true, 0),
                MsgFixtureBuilder.NameIdString("Keywords"))
            .AddRecipient(recipient => recipient
                .AddUnicode(MapiPropertyIds.DisplayName, "To Person")
                .AddFixedEntry(0x0C150003, 1))
            .AddRecipient(recipient => recipient
                .AddUnicode(MapiPropertyIds.DisplayName, "Cc Person")
                .AddFixedEntry(0x0C150003, 2))
            .AddAttachment(attachment => attachment
                .AddFixedEntry(0x37050003, 1)
                .AddUnicode(MapiPropertyIds.AttachLongFilename, "data.bin")
                .AddBinary(MapiPropertyIds.AttachData, attachmentPayload))
            .AddAttachment(attachment => attachment
                .AddEmbeddedMessage(embedded => embedded
                    .AddUnicode(MapiPropertyIds.Subject, "Inner subject")
                    .AddRecipient(recipient => recipient.AddUnicode(MapiPropertyIds.DisplayName, "Nested Recipient"))))
            .Build();

        using var message = OutlookMessage.Open(container, new OutlookMessageReaderOptions { ValidationLevel = validationLevel });

        // Scalar conveniences and both string wire types.
        Assert.AreEqual("Test subject", message.Subject);
        Assert.AreEqual("Alex Doe", message.SenderName);
        Assert.AreEqual("alex@example.com", message.SenderEmailAddress);
        Assert.AreEqual(sent, message.SentTime);
        Assert.AreEqual("Test body", message.BodyText);
        Assert.AreEqual(CompressedRtfTests.SpecExample1Text, message.BodyRtf);

        // Multi-valued and named properties.
        CollectionAssert.AreEqual(new[] { "urgent", "finance" }, message.Properties.GetStringArray(0x8000));
        Assert.IsTrue(message.TryGetNamedPropertyId(new MapiNamedProperty(s_publicStrings, "Keywords"), out ushort namedId));
        Assert.AreEqual((ushort)0x8000, namedId);

        // Recipients in index order.
        Assert.AreEqual(2, message.Recipients.Count);
        Assert.AreEqual(OutlookRecipientType.To, message.Recipients[0].RecipientType);
        Assert.AreEqual(OutlookRecipientType.Cc, message.Recipients[1].RecipientType);

        // Attachments: by-value payload and the nested message.
        Assert.AreEqual(2, message.Attachments.Count);
        using (Stream content = message.Attachments[0].OpenContentStream())
        using (var copy = new MemoryStream())
        {
            content.CopyTo(copy);
            CollectionAssert.AreEqual(attachmentPayload, copy.ToArray());
        }

        OutlookMessage nested = message.Attachments[1].OpenMessage();
        Assert.AreEqual("Inner subject", nested.Subject);
        Assert.AreEqual(1, nested.Recipients.Count);
    }
}
