// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OutlookAttachmentTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Formats.Outlook.Msg;

namespace Bodu.Formats.Outlook;

/// <summary>
/// Verifies the behavior of <see cref="OutlookAttachment" />, the attachment view.
/// </summary>
[TestClass]
public partial class OutlookAttachmentTests
{
    /// <summary>
    /// Builds a message with one by-value attachment carrying the given payload.
    /// </summary>
    /// <param name="payload">The attachment content.</param>
    /// <returns>The container stream.</returns>
    internal static MemoryStream CreateWithByValueAttachment(byte[] payload) =>
        MsgFixtureBuilder.CreateMinimal()
            .AddAttachment(attachment => attachment
                .AddFixedEntry(0x37050003, 1)
                .AddUnicode(MapiPropertyIds.AttachLongFilename, "payload.bin")
                .AddBinary(MapiPropertyIds.AttachData, payload))
            .Build();

    /// <summary>
    /// Verifies that the short file name is used when the long form is absent.
    /// </summary>
    [TestMethod]
    public void FileName_WhenOnlyShortNamePresent_ShouldFallBack()
    {
        using MemoryStream container = MsgFixtureBuilder.CreateMinimal()
            .AddAttachment(attachment => attachment
                .AddUnicode(MapiPropertyIds.AttachFilename, "REPORT~1.PDF"))
            .Build();

        using var message = OutlookMessage.OpenRead(container);

        Assert.AreEqual("REPORT~1.PDF", message.Attachments[0].FileName);
    }

    /// <summary>
    /// Verifies that the MIME tag convenience surfaces <c>PidTagAttachMimeTag</c>, matching the PST reader's view.
    /// </summary>
    [TestMethod]
    public void MimeTag_WhenPresent_ShouldReturnValue()
    {
        using MemoryStream container = MsgFixtureBuilder.CreateMinimal()
            .AddAttachment(attachment => attachment
                .AddUnicode(MapiPropertyIds.AttachMimeTag, "application/pdf"))
            .Build();

        using var message = OutlookMessage.OpenRead(container);

        Assert.AreEqual("application/pdf", message.Attachments[0].MimeTag);
    }
}
