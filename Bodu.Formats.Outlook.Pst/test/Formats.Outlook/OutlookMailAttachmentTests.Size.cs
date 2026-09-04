// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OutlookMailAttachmentTests.Size.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Formats.Outlook.Pst;

namespace Bodu.Formats.Outlook;

public partial class OutlookMailAttachmentTests
{
    /// <summary>
    /// Verifies that <see cref="OutlookMailAttachment.Size" /> returns the declared <c>PidTagAttachSize</c> when present.
    /// </summary>
    [TestMethod]
    public void Size_WhenAttachSizeDeclared_ShouldReturnDeclaredValue()
    {
        using OutlookMailStore store = OutlookMailMessageTests.OpenSynthetic();

        Assert.AreEqual(PstMessagingFixtureBuilder.AttachmentContent.Length, GetAttachments(store)[0].Size);
    }

    /// <summary>
    /// Verifies that <see cref="OutlookMailAttachment.Size" /> falls back to the inline payload length when
    /// <c>PidTagAttachSize</c> is absent.
    /// </summary>
    [TestMethod]
    public void Size_WhenAttachSizeAbsent_ForInlinePayload_ShouldReturnPayloadLength()
    {
        using OutlookMailStore store = OutlookMailMessageTests.OpenSynthetic(static b => b.OmitAttachSize = true);

        Assert.AreEqual(PstMessagingFixtureBuilder.AttachmentContent.Length, GetAttachments(store)[0].Size);
    }

    /// <summary>
    /// Verifies that <see cref="OutlookMailAttachment.Size" /> falls back to the container's payload length for a
    /// deferred payload, without materializing it.
    /// </summary>
    [TestMethod]
    [TestCategory(Bodu.Test.TestCategories.Regression)]
    public void Size_WhenAttachSizeAbsent_ForDeferredPayload_ShouldReturnPayloadLength()
    {
        var builder = new PstMessagingFixtureBuilder { LargeAttachmentXBlocks = 5, OmitAttachSize = true };
        using OutlookMailStore store = OutlookMailStore.Open(builder.BuildStream(), new OutlookMailStoreReaderOptions());

        Assert.AreEqual(builder.LargeAttachmentLength, GetAttachments(store)[0].Size);
    }
}
