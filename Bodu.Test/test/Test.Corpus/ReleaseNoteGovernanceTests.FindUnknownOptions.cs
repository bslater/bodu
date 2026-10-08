// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ReleaseNoteGovernanceTests.FindUnknownOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Test.Corpus;

public sealed partial class ReleaseNoteGovernanceTests
{
    /// <summary>
    /// Verifies that nothing is reported when every row's options are names the area recognizes.
    /// </summary>
    [TestMethod]
    public void FindUnknownOptions_WhenEveryOptionIsKnown_ShouldReportNothing()
    {
        ReleaseNoteFix[] rows = [Row("applies", "parse", "a", "MaxDepth=4;Strict=true"), Row("applies", "parse", "b")];

        IReadOnlyList<string> problems = ReleaseNoteGovernance.FindUnknownOptions(rows, ["MaxDepth", "Strict"]);

        Assert.AreEqual(0, problems.Count, string.Join(Environment.NewLine, problems));
    }

    /// <summary>
    /// Verifies that an option name the area does not recognize is reported.
    /// </summary>
    [TestMethod]
    public void FindUnknownOptions_WhenAnOptionIsUnknown_ShouldReportIt()
    {
        ReleaseNoteFix[] rows = [Row("applies", "parse", "a", "MaxDepth=4;Lenient=true")];

        IReadOnlyList<string> problems = ReleaseNoteGovernance.FindUnknownOptions(rows, ["MaxDepth"]);

        Assert.AreEqual(1, problems.Count);
        Assert.IsTrue(problems[0].Contains("'Lenient'", StringComparison.Ordinal), problems[0]);
    }

    /// <summary>
    /// Verifies that a malformed options field is reported.
    /// </summary>
    [TestMethod]
    public void FindUnknownOptions_WhenOptionsAreMalformed_ShouldReportThem()
    {
        ReleaseNoteFix[] rows = [Row("applies", "parse", "a", "MaxDepth")];

        IReadOnlyList<string> problems = ReleaseNoteGovernance.FindUnknownOptions(rows, ["MaxDepth"]);

        Assert.AreEqual(1, problems.Count);
        Assert.IsTrue(problems[0].Contains("options", StringComparison.Ordinal), problems[0]);
    }
}
