// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ReleaseNoteGovernanceTests.FindMissingUnitTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Test.Corpus;

public sealed partial class ReleaseNoteGovernanceTests
{
    /// <summary>
    /// Verifies that nothing is reported when every <c>unit</c> row names an existing test; the embedded sample
    /// catalogue's unit row names this test.
    /// </summary>
    [TestMethod]
    public void FindMissingUnitTests_WhenEveryNamedTestExists_ShouldReportNothing()
    {
        IReadOnlyList<ReleaseNoteFix> rows = ReleaseNoteCatalog.LoadEmbedded(typeof(ReleaseNoteGovernanceTests).Assembly)[0].Rows;

        IReadOnlyList<string> problems = ReleaseNoteGovernance.FindMissingUnitTests(rows, typeof(ReleaseNoteGovernanceTests).Assembly);

        Assert.AreEqual(0, problems.Count, string.Join(Environment.NewLine, problems));
    }

    /// <summary>
    /// Verifies that a <c>unit</c> row naming a test that does not exist, or a method that is not a test, is reported.
    /// </summary>
    /// <param name="input">The row's input.</param>
    [TestMethod]
    [DataRow("ReleaseNoteGovernanceTests.Missing_WhenAbsent_ShouldBeReported")]
    [DataRow("NoSuchTests.FindMissingUnitTests_WhenEveryNamedTestExists_ShouldReportNothing")]
    [DataRow("CorpusEscapesTests.ValidEscapes")]
    public void FindMissingUnitTests_WhenNamedTestDoesNotExist_ShouldReportTheRow(string input)
    {
        ReleaseNoteFix row = Row("applies", "unit", input);

        IReadOnlyList<string> problems = ReleaseNoteGovernance.FindMissingUnitTests([row], typeof(ReleaseNoteGovernanceTests).Assembly);

        Assert.AreEqual(1, problems.Count);
        Assert.IsTrue(problems[0].StartsWith("sample-fixes.csv:9: ", StringComparison.Ordinal), problems[0]);
    }

    /// <summary>
    /// Verifies that rows that name no test (other kinds, and <c>n/a</c> rows) are not checked.
    /// </summary>
    [TestMethod]
    public void FindMissingUnitTests_WhenRowsAreNotUnitRows_ShouldSkipThem()
    {
        ReleaseNoteFix[] rows =
        [
            Row("applies", "parse", "NoSuchTests.Parse_WhenX_ShouldY"),
            Row("n/a", string.Empty, string.Empty),
        ];

        IReadOnlyList<string> problems = ReleaseNoteGovernance.FindMissingUnitTests(rows, typeof(ReleaseNoteGovernanceTests).Assembly);

        Assert.AreEqual(0, problems.Count, string.Join(Environment.NewLine, problems));
    }
}
