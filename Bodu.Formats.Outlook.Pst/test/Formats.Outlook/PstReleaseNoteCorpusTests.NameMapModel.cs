// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstReleaseNoteCorpusTests.NameMapModel.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Outlook;

public sealed partial class PstReleaseNoteCorpusTests
{
    /// <summary>
    /// Represents the modelled name-to-id map, node 0x61: its GUID, entry and string streams.
    /// </summary>
    private sealed class NameMapModel
    {
        /// <summary>
        /// Gets or sets the GUID stream (property 0x0002).
        /// </summary>
        public byte[] Guids { get; set; } = [];

        /// <summary>
        /// Gets or sets the entry stream (property 0x0003): one 8-byte NAMEID per entry.
        /// </summary>
        public byte[] Entries { get; set; } = [];

        /// <summary>
        /// Gets or sets the string stream (property 0x0004).
        /// </summary>
        public byte[] Strings { get; set; } = [];
    }
}
