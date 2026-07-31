// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstNodeType.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Pst;

/// <summary>
/// Identifies the kind of object a node holds — the five type bits of a <see cref="PstNodeId" /> (the <c>NID_TYPE_*</c>
/// values of MS-PST §2.2.2.1).
/// </summary>
public enum PstNodeType
{
    /// <summary>
    /// A heap identifier rather than a node (<c>NID_TYPE_HID</c>).
    /// </summary>
    HeapId = 0x00,

    /// <summary>
    /// An internal bookkeeping node (<c>NID_TYPE_INTERNAL</c>).
    /// </summary>
    Internal = 0x01,

    /// <summary>
    /// A normal folder object (<c>NID_TYPE_NORMAL_FOLDER</c>).
    /// </summary>
    NormalFolder = 0x02,

    /// <summary>
    /// A search folder object (<c>NID_TYPE_SEARCH_FOLDER</c>).
    /// </summary>
    SearchFolder = 0x03,

    /// <summary>
    /// A normal message object (<c>NID_TYPE_NORMAL_MESSAGE</c>).
    /// </summary>
    NormalMessage = 0x04,

    /// <summary>
    /// An attachment object (<c>NID_TYPE_ATTACHMENT</c>).
    /// </summary>
    Attachment = 0x05,

    /// <summary>
    /// A search-update queue (<c>NID_TYPE_SEARCH_UPDATE_QUEUE</c>).
    /// </summary>
    SearchUpdateQueue = 0x06,

    /// <summary>
    /// A search-criteria object (<c>NID_TYPE_SEARCH_CRITERIA_OBJECT</c>).
    /// </summary>
    SearchCriteria = 0x07,

    /// <summary>
    /// A folder-associated (FAI) message object (<c>NID_TYPE_ASSOC_MESSAGE</c>).
    /// </summary>
    AssociatedMessage = 0x08,

    /// <summary>
    /// An internal contents-table index (<c>NID_TYPE_CONTENTS_TABLE_INDEX</c>).
    /// </summary>
    ContentsTableIndex = 0x0A,

    /// <summary>
    /// The receive-folder table (<c>NID_TYPE_RECEIVE_FOLDER_TABLE</c>).
    /// </summary>
    ReceiveFolderTable = 0x0B,

    /// <summary>
    /// The outgoing-queue table (<c>NID_TYPE_OUTGOING_QUEUE_TABLE</c>).
    /// </summary>
    OutgoingQueueTable = 0x0C,

    /// <summary>
    /// A folder's hierarchy table (<c>NID_TYPE_HIERARCHY_TABLE</c>).
    /// </summary>
    HierarchyTable = 0x0D,

    /// <summary>
    /// A folder's contents table (<c>NID_TYPE_CONTENTS_TABLE</c>).
    /// </summary>
    ContentsTable = 0x0E,

    /// <summary>
    /// A folder's FAI contents table (<c>NID_TYPE_ASSOC_CONTENTS_TABLE</c>).
    /// </summary>
    AssociatedContentsTable = 0x0F,

    /// <summary>
    /// A search folder's contents table (<c>NID_TYPE_SEARCH_CONTENTS_TABLE</c>).
    /// </summary>
    SearchContentsTable = 0x10,

    /// <summary>
    /// A message's attachment table (<c>NID_TYPE_ATTACHMENT_TABLE</c>).
    /// </summary>
    AttachmentTable = 0x11,

    /// <summary>
    /// A message's recipient table (<c>NID_TYPE_RECIPIENT_TABLE</c>).
    /// </summary>
    RecipientTable = 0x12,

    /// <summary>
    /// An internal search-table index (<c>NID_TYPE_SEARCH_TABLE_INDEX</c>).
    /// </summary>
    SearchTableIndex = 0x13,

    /// <summary>
    /// An LTP-internal node (<c>NID_TYPE_LTP</c>).
    /// </summary>
    Ltp = 0x1F,
}
