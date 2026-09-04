// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OutlookMailAttachmentTests.Properties.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Formats.Outlook.Pst;

namespace Bodu.Formats.Outlook;

public partial class OutlookMailAttachmentTests
{
    /// <summary>The tag of the by-value payload property.</summary>
    private static readonly MapiPropertyTag AttachDataTag = new(MapiPropertyIds.AttachData, MapiPropertyType.Binary);

    /// <summary>
    /// Verifies that a by-value payload larger than <see cref="OutlookMailStoreReaderOptions.MaxInlineAttachmentBytes" />
    /// is not decoded into the property collection: the property stays present with a null value, the conveniences
    /// still work, and decoding the attachment stays under a memory ceiling far below the payload size.
    /// </summary>
    [TestMethod]
    [TestCategory(Bodu.Test.TestCategories.Regression)]
    public void Properties_WhenPayloadExceedsInlineLimit_ShouldDeferPayload()
    {
        const long CeilingBytes = 8L * 1024 * 1024;
        var builder = new PstMessagingFixtureBuilder { LargeAttachmentXBlocks = 5 };
        using OutlookMailStore store = OutlookMailStore.Open(builder.BuildStream(), new OutlookMailStoreReaderOptions());

        GC.Collect();
        GC.WaitForPendingFinalizers();
        long baseline = GC.GetTotalMemory(forceFullCollection: true);

        OutlookMailAttachment attachment = GetAttachments(store)[0];
        MapiPropertyCollection properties = attachment.Properties;
        OutlookAttachmentMethod method = attachment.Method;
        string? fileName = attachment.FileName;

        long delta = GC.GetTotalMemory(forceFullCollection: false) - baseline;
        Assert.IsTrue(delta < CeilingBytes, $"Decoding the attachment allocated {delta / (1024 * 1024)} MB — the payload is being materialized.");
        Assert.IsTrue(properties.Contains(AttachDataTag), "The payload property must remain present.");
        Assert.IsNull(properties.GetBinary(MapiPropertyIds.AttachData), "A deferred payload must not be decoded inline.");
        Assert.AreEqual(OutlookAttachmentMethod.ByValue, method);
        Assert.AreEqual(PstMessagingFixtureBuilder.AttachmentLongFileName, fileName);
    }

    /// <summary>
    /// Verifies that a by-value payload within the inline limit is decoded into the property collection as before.
    /// </summary>
    [TestMethod]
    public void Properties_WhenPayloadWithinInlineLimit_ShouldKeepPayloadInline()
    {
        using OutlookMailStore store = OutlookMailMessageTests.OpenSynthetic();

        MapiPropertyCollection properties = GetAttachments(store)[0].Properties;

        CollectionAssert.AreEqual(PstMessagingFixtureBuilder.AttachmentContent, properties.GetBinary(MapiPropertyIds.AttachData)!.Value.ToArray());
    }

    /// <summary>
    /// Verifies that raising the inline limit above a large payload restores inline decoding.
    /// </summary>
    [TestMethod]
    [TestCategory(Bodu.Test.TestCategories.Regression)]
    public void Properties_WhenInlineLimitRaisedAbovePayload_ShouldKeepPayloadInline()
    {
        var builder = new PstMessagingFixtureBuilder { LargeAttachmentXBlocks = 1 };
        using OutlookMailStore store = OutlookMailStore.Open(
            builder.BuildStream(),
            new OutlookMailStoreReaderOptions { MaxInlineAttachmentBytes = 16 * 1024 * 1024 });

        ReadOnlyMemory<byte>? payload = GetAttachments(store)[0].Properties.GetBinary(MapiPropertyIds.AttachData);

        Assert.IsNotNull(payload);
        Assert.AreEqual(builder.LargeAttachmentLength, payload.Value.Length);
    }
}
