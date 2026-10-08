// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedReleaseNoteCorpusTests.FramedRecord.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Delimited;

public sealed partial class DelimitedReleaseNoteCorpusTests
{
    /// <summary>
    /// Represents one record as the reader framed it, or as a write row describes it.
    /// </summary>
    /// <param name="IsObject">
    /// Whether the record is an object record (header mode) rather than a positional one.
    /// </param>
    /// <param name="Fields">
    /// The fields in order: a name and a value in an object record, a value alone, with a <see langword="null" /> name,
    /// in a positional one. A write row may give a <see langword="null" /> name or value, to pass to the writer.
    /// </param>
    private sealed record FramedRecord(bool IsObject, List<(string? Name, string? Value)> Fields);
}
