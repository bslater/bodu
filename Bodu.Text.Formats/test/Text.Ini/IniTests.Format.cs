// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniTests.Format.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Ini;

public sealed partial class IniTests
{

    /// <summary>
    /// Verifies that <see cref="Ini.Format(IniDocument)" /> throws <see cref="ArgumentNullException" /> when the
    /// document is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Format_WhenDocumentIsNull_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Ini.Format(null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Ini.Format(IniDocument)" /> produces an empty string for an empty document.
    /// </summary>
    [TestMethod]
    public void Format_WhenDocumentIsEmpty_ShouldReturnEmptyString()
    {
        IniDocument doc = Ini.Parse(string.Empty);

        var formatted = Ini.Format(doc);

        Assert.AreEqual(string.Empty, formatted);
    }

    /// <summary>
    /// Verifies that <see cref="Ini.Format(IniDocument)" /> writes global entries without a section header.
    /// </summary>
    [TestMethod]
    public void Format_WhenGlobalEntriesOnly_ShouldWriteWithoutSectionHeader()
    {
        IniDocument doc = Ini.Parse("key=value");

        var formatted = Ini.Format(doc);

        StringAssert.Contains(formatted, "key = value");
        Assert.IsFalse(formatted.Contains('['), "Formatted output should not contain a section header.");
    }

    /// <summary>
    /// Verifies that <see cref="Ini.Format(IniDocument)" /> writes section entries with the section header.
    /// </summary>
    [TestMethod]
    public void Format_WhenNamedSection_ShouldWriteSectionHeader()
    {
        IniDocument doc = Ini.Parse("[server]\nhost=localhost");

        var formatted = Ini.Format(doc);

        StringAssert.Contains(formatted, "[server]");
        StringAssert.Contains(formatted, "host = localhost");
    }

    /// <summary>
    /// Verifies that <see cref="Ini.Format(IniDocument)" /> separates named sections with a blank line.
    /// </summary>
    [TestMethod]
    public void Format_WhenMultipleSections_ShouldSeparateWithBlankLine()
    {
        IniDocument doc = Ini.Parse("[a]\nk=1\n[b]\nk=2");

        var formatted = Ini.Format(doc);

        // Both section headers must be present and a blank line must appear between them.
        StringAssert.Contains(formatted, "[a]");
        StringAssert.Contains(formatted, "[b]");
        Assert.IsTrue(formatted.Contains("\n\n") || formatted.Contains("\r\n\r\n"),
            "Expected a blank line between sections.");
    }

    /// <summary>
    /// Verifies that a round-trip through <see cref="Ini.Parse(ReadOnlySpan{char})" /> and
    /// <see cref="Ini.Format(IniDocument)" /> preserves all section names, keys, and values.
    /// </summary>
    [TestMethod]
    public void Format_WhenRoundTripped_ShouldPreserveDocumentContent()
    {
        const string source = "global=g\n[s1]\na=1\nb=2\n[s2]\nc=3";

        IniDocument original = Ini.Parse(source);
        var formatted = Ini.Format(original);
        IniDocument roundTripped = Ini.Parse(formatted);

        Assert.AreEqual("g", roundTripped.GlobalSection["global"]);
        Assert.IsNotNull(roundTripped.GetSection("s1"));
        Assert.AreEqual("1", roundTripped.GetSection("s1")!["a"]);
        Assert.AreEqual("2", roundTripped.GetSection("s1")!["b"]);
        Assert.IsNotNull(roundTripped.GetSection("s2"));
        Assert.AreEqual("3", roundTripped.GetSection("s2")!["c"]);
    }

    /// <summary>
    /// Verifies that <see cref="Ini.Format(IniDocument)" /> uses <c> = </c> (space-equals-space) as the
    /// key/value separator.
    /// </summary>
    [TestMethod]
    public void Format_ShouldUsePaddedEqualsAsSeparator()
    {
        IniDocument doc = Ini.Parse("[s]\nkey=value");

        var formatted = Ini.Format(doc);

        StringAssert.Contains(formatted, "key = value");
    }

}
