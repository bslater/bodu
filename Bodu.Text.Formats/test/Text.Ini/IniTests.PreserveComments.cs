// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniTests.PreserveComments.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Ini;

public sealed partial class IniTests
{
    /// <summary>
    /// Verifies that <see cref="IniParseOptions.PreserveComments" /> defaults to <see langword="true" /> so
    /// callers get comment trivia without opting in.
    /// </summary>
    [TestMethod]
    public void PreserveComments_WhenDefaultOptions_ShouldBeTrue() => Assert.IsTrue(new IniParseOptions().PreserveComments);

    /// <summary>
    /// Verifies that comments authored before an entry are attached to
    /// <see cref="IniEntry.LeadingComments" /> when <see cref="IniParseOptions.PreserveComments" /> is enabled.
    /// </summary>
    [TestMethod]
    public void Parse_WhenPreserveCommentsAndCommentsPrecedeEntry_ShouldAttachToEntry()
    {
        var source = "# leading comment\n; second\nkey = value\n";

        IniDocument doc = Ini.Parse(source);

        Assert.AreEqual(1, doc.GlobalSection.Entries.Count);
        IniEntry entry = doc.GlobalSection.Entries[0];
        Assert.AreEqual(2, entry.LeadingComments.Count);
        Assert.AreEqual('#', entry.LeadingComments[0].Prefix);
        Assert.AreEqual(';', entry.LeadingComments[1].Prefix);
    }

    /// <summary>
    /// Verifies that comments authored before a section header are attached to
    /// <see cref="IniSection.LeadingComments" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenPreserveCommentsAndCommentsPrecedeSection_ShouldAttachToSection()
    {
        var source = "# applies to server\n[server]\nhost = localhost\n";

        IniDocument doc = Ini.Parse(source);

        Assert.AreEqual(1, doc.Sections.Count);
        Assert.AreEqual(1, doc.Sections[0].LeadingComments.Count);
        Assert.AreEqual(" applies to server", doc.Sections[0].LeadingComments[0].Text);
    }

    /// <summary>
    /// Verifies that disabling <see cref="IniParseOptions.PreserveComments" /> reverts the parser to the
    /// classic INI behaviour of silently dropping comments.
    /// </summary>
    [TestMethod]
    public void Parse_WhenPreserveCommentsIsFalse_ShouldDropAllComments()
    {
        var source = "# leading\n[server]\n; before host\nhost = localhost\n";

        IniDocument doc = Ini.Parse(source, new IniParseOptions { PreserveComments = false });

        Assert.AreEqual(0, doc.Sections[0].LeadingComments.Count);
        Assert.AreEqual(0, doc.Sections[0].Entries[0].LeadingComments.Count);
    }

    /// <summary>
    /// Verifies that each <see cref="IniComment" /> retains the 1-based source line number at which it
    /// appeared.
    /// </summary>
    [TestMethod]
    public void Parse_WhenCommentsPresent_ShouldAttachLineNumber()
    {
        var source = "# line one\n# line two\nkey = value\n";

        IniDocument doc = Ini.Parse(source);

        Assert.AreEqual(1, doc.GlobalSection.Entries[0].LeadingComments[0].LineNumber);
        Assert.AreEqual(2, doc.GlobalSection.Entries[0].LeadingComments[1].LineNumber);
    }
}
