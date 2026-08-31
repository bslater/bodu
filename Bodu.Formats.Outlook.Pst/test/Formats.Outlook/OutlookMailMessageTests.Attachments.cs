// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OutlookMailMessageTests.Attachments.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Formats.Outlook.Pst;
using Bodu.IO.Pst;

namespace Bodu.Formats.Outlook;

public partial class OutlookMailMessageTests
{
    /// <summary>
    /// Verifies that the attachment-table rows resolve to attachment views in table order, each carrying its decoded
    /// metadata.
    /// </summary>
    [TestMethod]
    [DataRow("Compatible", PstValidationLevel.Compatible)]
    [DataRow("Strict", PstValidationLevel.Strict)]
    public void Attachments_WhenSyntheticStore_ShouldDecodeAttachmentObjects(string testName, PstValidationLevel level)
    {
        _ = testName;

        using OutlookMailStore store = OpenSynthetic(level: level);

        IReadOnlyList<OutlookMailAttachment> attachments = GetFullMessage(store).Attachments;

        Assert.AreEqual(2, attachments.Count);

        Assert.AreEqual(OutlookAttachmentMethod.ByValue, attachments[0].Method);
        Assert.AreEqual(PstMessagingFixtureBuilder.AttachmentLongFileName, attachments[0].FileName);
        Assert.AreEqual(PstMessagingFixtureBuilder.AttachmentContentId, attachments[0].ContentId);
        Assert.AreEqual(PstMessagingFixtureBuilder.AttachmentMimeTag, attachments[0].MimeTag);
        Assert.AreEqual(PstMessagingFixtureBuilder.AttachmentContent.Length, attachments[0].Size);

        Assert.AreEqual(OutlookAttachmentMethod.EmbeddedMessage, attachments[1].Method);
    }

    /// <summary>
    /// Verifies that the file-name convenience falls back to the short (8.3) name when the long form is absent.
    /// </summary>
    [TestMethod]
    public void Attachments_WhenLongFileNamePresent_ShouldPreferItOverShortName()
    {
        using OutlookMailStore store = OpenSynthetic();

        OutlookMailAttachment attachment = GetFullMessage(store).Attachments[0];

        Assert.AreEqual(PstMessagingFixtureBuilder.AttachmentLongFileName, attachment.FileName);
        Assert.AreEqual(
            PstMessagingFixtureBuilder.AttachmentShortFileName,
            attachment.Properties.GetString(MapiPropertyIds.AttachFilename));
    }

    /// <summary>
    /// Verifies that a message without an attachment-table subnode reports an empty attachment list.
    /// </summary>
    [TestMethod]
    public void Attachments_WhenMessageHasNoAttachmentTable_ShouldBeEmpty()
    {
        using OutlookMailStore store = OpenSynthetic();

        Assert.AreEqual(0, GetPlainMessage(store).Attachments.Count);
    }

    /// <summary>
    /// Verifies that an attachment-table row referencing no attachment subnode is skipped under the tolerant levels.
    /// </summary>
    [TestMethod]
    public void Attachments_WhenRowReferencesMissingObject_ShouldSkipRow()
    {
        using OutlookMailStore store = OpenSynthetic(static b => b.IncludeDanglingAttachmentRow = true);

        Assert.AreEqual(2, GetFullMessage(store).Attachments.Count);
    }

    /// <summary>
    /// Verifies that an attachment-table row referencing no attachment subnode throws under strict validation.
    /// </summary>
    [TestMethod]
    public void Attachments_WhenRowReferencesMissingObject_ForStrictValidation_ShouldThrowOutlookPstFormatException()
    {
        using OutlookMailStore store = OpenSynthetic(
            static b => b.IncludeDanglingAttachmentRow = true,
            PstValidationLevel.Strict);

        OutlookMailMessage message = GetFullMessage(store);

        _ = Assert.ThrowsExactly<OutlookPstFormatException>(() =>
        {
            _ = message.Attachments;
        });
    }
}
