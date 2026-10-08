// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ReleaseNoteGovernanceTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Test.Corpus;

/// <summary>
/// Verifies <see cref="ReleaseNoteGovernance" />: finding <c>unit</c> rows whose test does not exist, and options an
/// area does not recognize.
/// </summary>
[TestClass]
public sealed partial class ReleaseNoteGovernanceTests
{
    /// <summary>
    /// Creates a row of the given class and kind, naming the given options and input.
    /// </summary>
    /// <param name="rowClass">The class field.</param>
    /// <param name="kind">The kind field.</param>
    /// <param name="input">The input field.</param>
    /// <param name="options">The options field.</param>
    /// <returns>The row.</returns>
    private static ReleaseNoteFix Row(string rowClass, string kind, string input, string options = "") =>
        new("sample-fixes.csv", 9, "sample", "1.0.0", "#1", "A fix", rowClass, string.Empty, kind, options, input, string.Empty, "upstream", "a reason");
}
