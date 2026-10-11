// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstReleaseNoteCorpusTests.MessageModel.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Outlook;

public sealed partial class PstReleaseNoteCorpusTests
{
    /// <summary>
    /// Represents one modelled message: a property context, and the recipient and attachment tables and attachment
    /// objects in its subnode tree.
    /// </summary>
    private sealed class MessageModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MessageModel" /> class.
        /// </summary>
        /// <param name="nodeId">The message's node or subnode identifier.</param>
        public MessageModel(uint nodeId)
        {
            NodeId = nodeId;
        }

        /// <summary>
        /// Gets the message's node or subnode identifier.
        /// </summary>
        public uint NodeId { get; }

        /// <summary>
        /// Gets the message's properties.
        /// </summary>
        public List<PropertyModel> Properties { get; } = [];

        /// <summary>
        /// Gets or sets the recipients, or <see langword="null" /> for a message without a recipient table.
        /// </summary>
        public List<RecipientModel>? Recipients { get; set; }

        /// <summary>
        /// Gets or sets the attachments, or <see langword="null" /> for a message without an attachment table.
        /// </summary>
        public List<AttachmentModel>? Attachments { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the message's subnode tree is an SIBLOCK over one SLBLOCK per entry.
        /// </summary>
        public bool SubnodeIndex { get; set; }
    }
}
