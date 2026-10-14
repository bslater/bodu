// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstReleaseNoteCorpusTests.AttachmentModel.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Outlook;

public sealed partial class PstReleaseNoteCorpusTests
{
    /// <summary>
    /// Represents one attachment of a modeled message: a row of its attachment table and, unless left out, an
    /// attachment object subnode.
    /// </summary>
    private sealed class AttachmentModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AttachmentModel" /> class.
        /// </summary>
        /// <param name="nodeId">The attachment object's subnode identifier, which also names its table row.</param>
        public AttachmentModel(uint nodeId)
        {
            NodeId = nodeId;
        }

        /// <summary>
        /// Gets the attachment object's subnode identifier, which also names its table row.
        /// </summary>
        public uint NodeId { get; }

        /// <summary>
        /// Gets the attachment object's properties.
        /// </summary>
        public List<PropertyModel> Properties { get; } = [];

        /// <summary>
        /// Gets the cells of the attachment's table row besides PidTagAttachNumber.
        /// </summary>
        public List<PropertyModel> RowProperties { get; } = [];

        /// <summary>
        /// Gets or sets the embedded message, written as a subnode of the attachment object and referenced by a
        /// PidTagAttachDataObject record.
        /// </summary>
        public MessageModel? Embedded { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the attachment object is written; its table row is written either
        /// way.
        /// </summary>
        public bool WriteNode { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether the embedded message's subnode is written; its reference is written
        /// either way.
        /// </summary>
        public bool WriteEmbeddedNode { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether the embedded message's subnode entry names a data block; when it
        /// does not, the entry records block identifier 0.
        /// </summary>
        public bool EmbeddedHasData { get; set; } = true;

        /// <summary>
        /// Gets or sets a PtypObject payload, written as subnode 0x0001203F and referenced by a PidTagAttachDataObject
        /// record of that identifier and the payload's length.
        /// </summary>
        public byte[]? ObjectData { get; set; }
    }
}
