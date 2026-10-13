// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstReleaseNoteCorpusTests.RecipientModel.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Outlook;

public sealed partial class PstReleaseNoteCorpusTests
{
    /// <summary>
    /// Represents one recipient of a modeled message: a row of its recipient table.
    /// </summary>
    private sealed class RecipientModel
    {
        /// <summary>
        /// Gets the recipient's properties, written as the row's cells.
        /// </summary>
        public List<PropertyModel> Properties { get; } = [];
    }
}
