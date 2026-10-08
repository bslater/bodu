// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedReleaseNoteCorpusTests.RowView.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Delimited.Reader;

namespace Bodu.Text.Delimited;

public sealed partial class DelimitedReleaseNoteCorpusTests
{
    /// <summary>
    /// Identifies what a <c>parse</c> row renders, as its <c>View</c> option selects.
    /// </summary>
    private enum RowView
    {
        /// <summary>
        /// The records, as a JSON array of records; the default.
        /// </summary>
        Records = 0,

        /// <summary>
        /// The reader's <see cref="Utf8DelimitedReader.Headers" /> list once the input is read, as a JSON array of
        /// strings.
        /// </summary>
        Headers,
    }
}
