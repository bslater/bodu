// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MapiPropertyIdsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;

namespace Bodu.Formats.Outlook;

/// <summary>
/// Verifies that the curated <see cref="MapiPropertyIds" /> constants are pinned to their MS-OXPROPS values.
/// </summary>
[TestClass]
public class MapiPropertyIdsTests
{
    /// <summary>
    /// Verifies that each curated constant carries its published property identifier.
    /// </summary>
    /// <param name="testName">The <c>PidTag</c> name of the constant.</param>
    /// <param name="actual">The constant under test.</param>
    /// <param name="expected">The published identifier.</param>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    [DataRow("PidTagImportance", MapiPropertyIds.Importance, (ushort)0x0017)]
    [DataRow("PidTagMessageClass", MapiPropertyIds.MessageClass, (ushort)0x001A)]
    [DataRow("PidTagSensitivity", MapiPropertyIds.Sensitivity, (ushort)0x0036)]
    [DataRow("PidTagSubject", MapiPropertyIds.Subject, (ushort)0x0037)]
    [DataRow("PidTagClientSubmitTime", MapiPropertyIds.ClientSubmitTime, (ushort)0x0039)]
    [DataRow("PidTagTransportMessageHeaders", MapiPropertyIds.TransportMessageHeaders, (ushort)0x007D)]
    [DataRow("PidTagReceivedByName", MapiPropertyIds.ReceivedByName, (ushort)0x0040)]
    [DataRow("PidTagSentRepresentingName", MapiPropertyIds.SentRepresentingName, (ushort)0x0042)]
    [DataRow("PidTagSentRepresentingEmailAddress", MapiPropertyIds.SentRepresentingEmailAddress, (ushort)0x0065)]
    [DataRow("PidTagConversationTopic", MapiPropertyIds.ConversationTopic, (ushort)0x0070)]
    [DataRow("PidTagRecipientType", MapiPropertyIds.RecipientType, (ushort)0x0C15)]
    [DataRow("PidTagSenderName", MapiPropertyIds.SenderName, (ushort)0x0C1A)]
    [DataRow("PidTagSenderEmailAddress", MapiPropertyIds.SenderEmailAddress, (ushort)0x0C1F)]
    [DataRow("PidTagDisplayBcc", MapiPropertyIds.DisplayBcc, (ushort)0x0E02)]
    [DataRow("PidTagDisplayCc", MapiPropertyIds.DisplayCc, (ushort)0x0E03)]
    [DataRow("PidTagDisplayTo", MapiPropertyIds.DisplayTo, (ushort)0x0E04)]
    [DataRow("PidTagMessageDeliveryTime", MapiPropertyIds.MessageDeliveryTime, (ushort)0x0E06)]
    [DataRow("PidTagMessageFlags", MapiPropertyIds.MessageFlags, (ushort)0x0E07)]
    [DataRow("PidTagMessageSize", MapiPropertyIds.MessageSize, (ushort)0x0E08)]
    [DataRow("PidTagHasAttachments", MapiPropertyIds.HasAttachments, (ushort)0x0E1B)]
    [DataRow("PidTagNormalizedSubject", MapiPropertyIds.NormalizedSubject, (ushort)0x0E1D)]
    [DataRow("PidTagAttachSize", MapiPropertyIds.AttachSize, (ushort)0x0E20)]
    [DataRow("PidTagAttachNumber", MapiPropertyIds.AttachNumber, (ushort)0x0E21)]
    [DataRow("PidTagRecordKey", MapiPropertyIds.RecordKey, (ushort)0x0FF9)]
    [DataRow("PidTagEntryId", MapiPropertyIds.EntryId, (ushort)0x0FFF)]
    [DataRow("PidTagBody", MapiPropertyIds.Body, (ushort)0x1000)]
    [DataRow("PidTagRtfCompressed", MapiPropertyIds.RtfCompressed, (ushort)0x1009)]
    [DataRow("PidTagHtml", MapiPropertyIds.Html, (ushort)0x1013)]
    [DataRow("PidTagInternetMessageId", MapiPropertyIds.InternetMessageId, (ushort)0x1035)]
    [DataRow("PidTagDisplayName", MapiPropertyIds.DisplayName, (ushort)0x3001)]
    [DataRow("PidTagAddressType", MapiPropertyIds.AddressType, (ushort)0x3002)]
    [DataRow("PidTagEmailAddress", MapiPropertyIds.EmailAddress, (ushort)0x3003)]
    [DataRow("PidTagCreationTime", MapiPropertyIds.CreationTime, (ushort)0x3007)]
    [DataRow("PidTagLastModificationTime", MapiPropertyIds.LastModificationTime, (ushort)0x3008)]
    [DataRow("PidTagStoreSupportMask", MapiPropertyIds.StoreSupportMask, (ushort)0x340D)]
    [DataRow("PidTagIpmSubTreeEntryId", MapiPropertyIds.IpmSubTreeEntryId, (ushort)0x35E0)]
    [DataRow("PidTagIpmWastebasketEntryId", MapiPropertyIds.IpmWastebasketEntryId, (ushort)0x35E3)]
    [DataRow("PidTagFinderEntryId", MapiPropertyIds.FinderEntryId, (ushort)0x35E7)]
    [DataRow("PidTagContentCount", MapiPropertyIds.ContentCount, (ushort)0x3602)]
    [DataRow("PidTagContentUnreadCount", MapiPropertyIds.ContentUnreadCount, (ushort)0x3603)]
    [DataRow("PidTagSubfolders", MapiPropertyIds.Subfolders, (ushort)0x360A)]
    [DataRow("PidTagContainerClass", MapiPropertyIds.ContainerClass, (ushort)0x3613)]
    [DataRow("PidTagAttachData", MapiPropertyIds.AttachData, (ushort)0x3701)]
    [DataRow("PidTagAttachExtension", MapiPropertyIds.AttachExtension, (ushort)0x3703)]
    [DataRow("PidTagAttachFilename", MapiPropertyIds.AttachFilename, (ushort)0x3704)]
    [DataRow("PidTagAttachMethod", MapiPropertyIds.AttachMethod, (ushort)0x3705)]
    [DataRow("PidTagAttachLongFilename", MapiPropertyIds.AttachLongFilename, (ushort)0x3707)]
    [DataRow("PidTagAttachMimeTag", MapiPropertyIds.AttachMimeTag, (ushort)0x370E)]
    [DataRow("PidTagAttachContentId", MapiPropertyIds.AttachContentId, (ushort)0x3712)]
    [DataRow("PidTagInternetCodepage", MapiPropertyIds.InternetCodepage, (ushort)0x3FDE)]
    [DataRow("PidTagMessageCodepage", MapiPropertyIds.MessageCodepage, (ushort)0x3FFD)]
    [DataRow("PidTagLtpRowId", MapiPropertyIds.LtpRowId, (ushort)0x67F2)]
    [DataRow("PidTagLtpRowVer", MapiPropertyIds.LtpRowVer, (ushort)0x67F3)]
    public void Constants_WhenComparedToPublishedId_ShouldMatch(string testName, ushort actual, ushort expected)
    {
        _ = testName;

        Assert.AreEqual(expected, actual);
    }
}
