// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstNodeTypeTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Pst;

/// <summary>
/// Verifies that <see cref="PstNodeType" /> pins the <c>NID_TYPE_*</c> values of MS-PST §2.2.2.1.
/// </summary>
[TestClass]
public class PstNodeTypeTests
{
    /// <summary>
    /// Verifies the numeric value of each member against the specification.
    /// </summary>
    /// <param name="testName">The specification constant the row pins.</param>
    /// <param name="type">The enum member.</param>
    /// <param name="expected">The specification value.</param>
    [TestMethod]
    [DataRow("NID_TYPE_HID", PstNodeType.HeapId, 0x00)]
    [DataRow("NID_TYPE_INTERNAL", PstNodeType.Internal, 0x01)]
    [DataRow("NID_TYPE_NORMAL_FOLDER", PstNodeType.NormalFolder, 0x02)]
    [DataRow("NID_TYPE_SEARCH_FOLDER", PstNodeType.SearchFolder, 0x03)]
    [DataRow("NID_TYPE_NORMAL_MESSAGE", PstNodeType.NormalMessage, 0x04)]
    [DataRow("NID_TYPE_ATTACHMENT", PstNodeType.Attachment, 0x05)]
    [DataRow("NID_TYPE_SEARCH_UPDATE_QUEUE", PstNodeType.SearchUpdateQueue, 0x06)]
    [DataRow("NID_TYPE_SEARCH_CRITERIA_OBJECT", PstNodeType.SearchCriteria, 0x07)]
    [DataRow("NID_TYPE_ASSOC_MESSAGE", PstNodeType.AssociatedMessage, 0x08)]
    [DataRow("NID_TYPE_CONTENTS_TABLE_INDEX", PstNodeType.ContentsTableIndex, 0x0A)]
    [DataRow("NID_TYPE_RECEIVE_FOLDER_TABLE", PstNodeType.ReceiveFolderTable, 0x0B)]
    [DataRow("NID_TYPE_OUTGOING_QUEUE_TABLE", PstNodeType.OutgoingQueueTable, 0x0C)]
    [DataRow("NID_TYPE_HIERARCHY_TABLE", PstNodeType.HierarchyTable, 0x0D)]
    [DataRow("NID_TYPE_CONTENTS_TABLE", PstNodeType.ContentsTable, 0x0E)]
    [DataRow("NID_TYPE_ASSOC_CONTENTS_TABLE", PstNodeType.AssociatedContentsTable, 0x0F)]
    [DataRow("NID_TYPE_SEARCH_CONTENTS_TABLE", PstNodeType.SearchContentsTable, 0x10)]
    [DataRow("NID_TYPE_ATTACHMENT_TABLE", PstNodeType.AttachmentTable, 0x11)]
    [DataRow("NID_TYPE_RECIPIENT_TABLE", PstNodeType.RecipientTable, 0x12)]
    [DataRow("NID_TYPE_SEARCH_TABLE_INDEX", PstNodeType.SearchTableIndex, 0x13)]
    [DataRow("NID_TYPE_LTP", PstNodeType.Ltp, 0x1F)]
    public void Values_WhenComparedToSpecification_ShouldMatch(string testName, PstNodeType type, int expected)
    {
        Assert.AreEqual(expected, (int)type, testName);
    }
}
