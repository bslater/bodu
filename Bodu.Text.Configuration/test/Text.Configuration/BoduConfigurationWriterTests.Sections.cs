// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduConfigurationWriterTests.Sections.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.IO;

namespace Bodu.Text.Configuration;

public partial class BoduConfigurationWriterTests
{
    /// <summary>
    /// Verifies that section author order is preserved in the emitted text.
    /// </summary>
    [TestMethod]
    public void Save_WhenMultipleSectionsAppended_ShouldEmitInAuthorOrder()
    {
        BoduConfigurationDocument doc = new();
        doc.GetOrAddSection("first").Set("a", "1");
        doc.GetOrAddSection("second").Set("a", "2");

        string text = doc.ToString();

        int firstIdx = text.IndexOf("[first]", StringComparison.Ordinal);
        int secondIdx = text.IndexOf("[second]", StringComparison.Ordinal);

        Assert.IsTrue(firstIdx >= 0 && secondIdx > firstIdx);
    }

    /// <summary>
    /// Verifies that a blank line is inserted between sections when
    /// <see cref="BoduConfigurationWriteOptions.InsertBlankLineBetweenSections" /> is true.
    /// </summary>
    [TestMethod]
    public void Save_WhenInsertBlankLineBetweenSectionsIsTrue_ShouldEmitBlankLineBetweenSections()
    {
        BoduConfigurationDocument doc = new();
        doc.GetOrAddSection("first").Set("a", "1");
        doc.GetOrAddSection("second").Set("a", "2");

        BoduConfigurationWriteOptions options = new() { NewLine = "\n", InsertBlankLineBetweenSections = true };
        using StringWriter sw = new();
        doc.Save(sw, options);

        StringAssert.Contains(sw.ToString(), "\n\n[second]");
    }

    /// <summary>
    /// Verifies that when <see cref="BoduConfigurationWriteOptions.InsertBlankLineBetweenSections" /> is
    /// <see langword="false" /> the output does not contain a blank line between sections.
    /// </summary>
    [TestMethod]
    public void Save_WhenInsertBlankLineBetweenSectionsIsFalse_ShouldNotEmitBlankLineBetweenSections()
    {
        BoduConfigurationDocument doc = new();
        doc.GetOrAddSection("first").Set("a", "1");
        doc.GetOrAddSection("second").Set("a", "2");

        BoduConfigurationWriteOptions options = new() { NewLine = "\n", InsertBlankLineBetweenSections = false };
        using StringWriter sw = new();
        doc.Save(sw, options);

        Assert.IsFalse(sw.ToString().Contains("\n\n[second]"));
    }

    /// <summary>
    /// Verifies that leading section comments are emitted before the section header when
    /// <see cref="BoduConfigurationWriteOptions.PreserveComments" /> is enabled.
    /// </summary>
    [TestMethod]
    public void Save_WhenPreserveCommentsIsTrue_ShouldEmitLeadingSectionComments()
    {
        BoduConfigurationDocument doc = new();
        BoduConfigurationSection section = doc.GetOrAddSection("*.cs");
        section.AddLeadingComment(new BoduConfigurationComment('#', " leading note", default));
        section.Set("a", "1");

        string text = doc.ToString();

        StringAssert.Contains(text, "# leading note");
        Assert.IsTrue(text.IndexOf("# leading note", StringComparison.Ordinal)
            < text.IndexOf("[*.cs]", StringComparison.Ordinal));
    }
}
