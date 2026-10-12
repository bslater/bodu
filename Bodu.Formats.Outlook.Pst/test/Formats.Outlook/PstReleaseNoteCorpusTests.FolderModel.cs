// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstReleaseNoteCorpusTests.FolderModel.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Outlook;

public sealed partial class PstReleaseNoteCorpusTests
{
    /// <summary>
    /// Represents one modelled folder: a property context, a hierarchy table naming its child folders, a contents table
    /// naming its messages, and the messages themselves.
    /// </summary>
    /// <remarks>
    /// A folder with index x (its node identifier shifted right by 5) has hierarchy table <c>(x &lt;&lt; 5) | 0x0D</c>
    /// and contents table <c>(x &lt;&lt; 5) | 0x0E</c>.
    /// </remarks>
    private sealed class FolderModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FolderModel" /> class.
        /// </summary>
        /// <param name="nodeId">The folder's node identifier.</param>
        public FolderModel(uint nodeId)
        {
            NodeId = nodeId;
        }

        /// <summary>
        /// Gets the folder's node identifier.
        /// </summary>
        public uint NodeId { get; }

        /// <summary>
        /// Gets or sets the parent the folder's node records instead of its parent folder, or <see langword="null" />.
        /// </summary>
        public uint? ParentOverride { get; set; }

        /// <summary>
        /// Gets the folder's properties.
        /// </summary>
        public List<PropertyModel> Properties { get; } = [];

        /// <summary>
        /// Gets the child folders, in the order the hierarchy table lists them.
        /// </summary>
        public List<FolderModel> Children { get; } = [];

        /// <summary>
        /// Gets the messages, in the order the contents table lists them.
        /// </summary>
        public List<MessageModel> Messages { get; } = [];

        /// <summary>
        /// Gets the identifiers of nodes that are not written, listed in the contents table before the messages' rows.
        /// </summary>
        public List<uint> DanglingContentsRows { get; } = [];

        /// <summary>
        /// Gets or sets a value indicating whether the hierarchy table is written.
        /// </summary>
        public bool WriteHierarchyTable { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether the contents table is written.
        /// </summary>
        public bool WriteContentsTable { get; set; } = true;
    }
}
