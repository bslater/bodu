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
}
