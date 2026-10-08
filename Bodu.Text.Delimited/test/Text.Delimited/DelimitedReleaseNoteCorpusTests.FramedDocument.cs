// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedReleaseNoteCorpusTests.FramedDocument.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Delimited;

public sealed partial class DelimitedReleaseNoteCorpusTests
{
    /// <summary>
    /// Represents a whole document as the reader reported it.
    /// </summary>
    /// <param name="Headers">The header names the reader reports once the input is read; empty with no header row.</param>
    /// <param name="Records">The records, in order.</param>
    private sealed record FramedDocument(IReadOnlyList<string> Headers, IReadOnlyList<FramedRecord> Records);
}
