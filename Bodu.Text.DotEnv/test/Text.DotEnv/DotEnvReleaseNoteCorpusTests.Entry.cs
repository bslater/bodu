// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DotEnvReleaseNoteCorpusTests.Entry.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.DotEnv;

public sealed partial class DotEnvReleaseNoteCorpusTests
{
    /// <summary>
    /// Represents one entry of a DotEnv document, as a read reports it and as the write notation describes it.
    /// </summary>
    /// <param name="Key">The key.</param>
    /// <param name="Value">The value, with its quotes removed and its escapes resolved.</param>
    /// <param name="Export">Whether the entry carries the <c>export</c> prefix.</param>
    private sealed record Entry(string Key, string Value, bool Export);
}
