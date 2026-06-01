// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DotEnvTests.PreserveComments.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.DotEnv;

public sealed partial class DotEnvTests
{
    /// <summary>
    /// Verifies that <see cref="DotEnv.Parse(ReadOnlySpan{char})" /> retains comment lines that precede an entry
    /// as <see cref="DotEnvEntry.LeadingComments" />, so that downstream callers can inspect human-authored
    /// annotations attached to specific keys.
    /// </summary>
    [TestMethod]
    public void Parse_WhenEntryHasLeadingComment_ShouldRetainCommentOnEntry()
    {
        DotEnvDocument doc = DotEnv.Parse(
            "# API key for production\n" +
            "API_KEY=secret\n");

        DotEnvEntry entry = doc.Entries[0];
        Assert.AreEqual(1, entry.LeadingComments.Count);
        Assert.AreEqual(" API key for production", entry.LeadingComments[0].Text);
        Assert.AreEqual('#', entry.LeadingComments[0].Prefix);
    }

    /// <summary>
    /// Verifies that <see cref="DotEnv.Format(DotEnvDocument)" /> emits an entry's leading comments before the
    /// <c>KEY=VALUE</c> line they belong to.
    /// </summary>
    [TestMethod]
    public void Format_WhenEntryHasLeadingComments_ShouldEmitCommentsBeforeAssignment()
    {
        const string source =
            "# Section header\n" +
            "# Authentication\n" +
            "API_KEY=secret\n";

        DotEnvDocument doc = DotEnv.Parse(source);
        var formatted = DotEnv.Format(doc);

        Assert.AreEqual(source.Replace("\n", Environment.NewLine, StringComparison.Ordinal), formatted);
    }

    /// <summary>
    /// Verifies that <see cref="DotEnvParseOptions.PreserveComments" /> set to <see langword="false" /> discards
    /// comment lines so the parsed document is purely a data model.
    /// </summary>
    [TestMethod]
    public void Parse_WhenPreserveCommentsDisabled_ShouldDropComments()
    {
        DotEnvParseOptions options = new() { PreserveComments = false };

        DotEnvDocument doc = DotEnv.Parse(
            "# Should be dropped\n" +
            "API_KEY=secret\n",
            options);

        Assert.AreEqual(0, doc.Entries[0].LeadingComments.Count);
    }

    /// <summary>
    /// Verifies that <see cref="DotEnvParseOptions.PreserveComments" /> defaults to <see langword="true" />.
    /// </summary>
    [TestMethod]
    public void PreserveComments_WhenOptionsAreDefault_ShouldBeTrue()
    {
        DotEnvParseOptions options = DotEnvParseOptions.Default;

        Assert.IsTrue(options.PreserveComments);
    }
}
