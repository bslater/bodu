// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ReleaseNoteFixTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Test.Corpus;

/// <summary>
/// Verifies <see cref="ReleaseNoteFix" />: the label that names a row in test output, and which rows run as data.
/// </summary>
[TestClass]
public sealed partial class ReleaseNoteFixTests
{
    /// <summary>
    /// Creates a row with the given identifying fields and class and kind, and placeholder values elsewhere.
    /// </summary>
    /// <param name="reference">The reference field.</param>
    /// <param name="caseNumber">The case field.</param>
    /// <param name="rowClass">The class field.</param>
    /// <param name="kind">The kind field.</param>
    /// <returns>The row.</returns>
    private static ReleaseNoteFix Row(string reference = "#12", string caseNumber = "", string rowClass = "applies", string kind = "parse") =>
        new(
            "sample-fixes.csv",
            7,
            "sample",
            "1.2.3",
            reference,
            "Reject a truncated document",
            rowClass,
            caseNumber,
            kind,
            string.Empty,
            "abc",
            "FormatException",
            "upstream",
            "a placeholder reason");
}
