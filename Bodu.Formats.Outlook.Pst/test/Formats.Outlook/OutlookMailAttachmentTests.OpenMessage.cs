// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OutlookMailAttachmentTests.OpenMessage.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Formats.Outlook.Pst;

namespace Bodu.Formats.Outlook;

public partial class OutlookMailAttachmentTests
{
    /// <summary>
    /// Verifies that an embedded-message attachment opens as a nested message view with its own decoded properties.
    /// </summary>
    [TestMethod]
    public void OpenMessage_WhenEmbeddedMessageAttachment_ShouldReturnNestedMessage()
    {
        using OutlookMailStore store = OutlookMailMessageTests.OpenSynthetic();

        OutlookMailMessage nested = GetAttachments(store)[1].OpenMessage();

        Assert.AreEqual(PstMessagingFixtureBuilder.EmbeddedSubject, nested.Subject);
        Assert.AreEqual(PstMessagingFixtureBuilder.EmbeddedSenderName, nested.SenderName);
        Assert.AreEqual("IPM.Note", nested.MessageClass);
        Assert.AreEqual(0, nested.Recipients.Count, "The nested message carries no recipient table.");
    }

    /// <summary>
    /// Verifies that a nested message's <c>PT_STRING8</c> values decode under the code page inherited from the owning
    /// message, which the nested object does not declare itself.
    /// </summary>
    [TestMethod]
    public void OpenMessage_WhenNestedString8Value_ShouldInheritOwningMessageCodePage()
    {
        using OutlookMailStore store = OutlookMailMessageTests.OpenSynthetic();

        OutlookMailMessage nested = GetAttachments(store)[1].OpenMessage();

        Assert.AreEqual(
            PstMessagingFixtureBuilder.EmbeddedBodyText,
            nested.Properties.GetString(MapiPropertyIds.Body),
            "The nested body must decode under the inherited windows-1251 code page, not the store fallback.");
    }

    /// <summary>
    /// Verifies that opening the message of a by-value attachment throws <see cref="NotSupportedException" />.
    /// </summary>
    [TestMethod]
    public void OpenMessage_WhenByValueAttachment_ShouldThrowNotSupportedException()
    {
        using OutlookMailStore store = OutlookMailMessageTests.OpenSynthetic();

        OutlookMailAttachment attachment = GetAttachments(store)[0];

        _ = Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            _ = attachment.OpenMessage();
        });
    }

    /// <summary>
    /// Verifies that an embedded-message attachment whose nested message subnode is missing throws
    /// <see cref="OutlookPstFormatException" />.
    /// </summary>
    [TestMethod]
    public void OpenMessage_WhenEmbeddedMessageSubnodeMissing_ShouldThrowOutlookPstFormatException()
    {
        using OutlookMailStore store = OutlookMailMessageTests.OpenSynthetic(
            static b => b.IncludeEmbeddedMessageSubnode = false);

        OutlookMailAttachment attachment = GetAttachments(store)[1];

        _ = Assert.ThrowsExactly<OutlookPstFormatException>(() =>
        {
            _ = attachment.OpenMessage();
        });
    }

    /// <summary>
    /// Verifies that opening an embedded message deeper than
    /// <see cref="OutlookMailStoreReaderOptions.MaxEmbeddedMessageDepth" /> throws <see cref="OutlookPstFormatException" />
    /// at the boundary, while every level within the limit opens.
    /// </summary>
    [TestMethod]
    public void OpenMessage_WhenNestingExceedsMaxEmbeddedMessageDepth_ShouldThrowOutlookPstFormatException()
    {
        var builder = new PstMessagingFixtureBuilder { EmbeddedMessageNestingDepth = 3 };
        using OutlookMailStore store = OutlookMailStore.Open(
            builder.BuildStream(),
            new OutlookMailStoreReaderOptions { MaxEmbeddedMessageDepth = 2 });

        OutlookMailMessage first = GetAttachments(store)[1].OpenMessage();
        OutlookMailMessage second = first.Attachments[0].OpenMessage();
        Assert.AreEqual(PstMessagingFixtureBuilder.EmbeddedSubject, second.Subject);

        OutlookMailAttachment third = second.Attachments[0];
        _ = Assert.ThrowsExactly<OutlookPstFormatException>(() =>
        {
            _ = third.OpenMessage();
        });
    }

    /// <summary>
    /// Verifies that the default nesting limit admits a realistically deep chain.
    /// </summary>
    [TestMethod]
    public void OpenMessage_WhenNestingWithinDefaultLimit_ShouldOpenEveryLevel()
    {
        var builder = new PstMessagingFixtureBuilder { EmbeddedMessageNestingDepth = 4 };
        using OutlookMailStore store = OutlookMailStore.Open(builder.BuildStream(), new OutlookMailStoreReaderOptions());

        OutlookMailMessage current = GetAttachments(store)[1].OpenMessage();
        for (int level = 1; level < 4; level++)
        {
            current = current.Attachments[0].OpenMessage();
            Assert.AreEqual(PstMessagingFixtureBuilder.EmbeddedSubject, current.Subject);
        }

        Assert.AreEqual(0, current.Attachments.Count, "The innermost message carries no further attachments.");
    }
}
