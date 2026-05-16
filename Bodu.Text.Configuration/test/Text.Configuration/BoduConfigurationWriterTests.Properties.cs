// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduConfigurationWriterTests.Properties.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.IO;
using Bodu.Text.Configuration.Infrastructure;

namespace Bodu.Text.Configuration;

public partial class BoduConfigurationWriterTests
{
    /// <summary>
    /// Verifies that property author order within a section is preserved in the emitted text.
    /// </summary>
    [TestMethod]
    public void Save_WhenPropertiesAppendedInOrder_ShouldEmitInSameOrder()
    {
        BoduConfigurationDocument doc = new();
        BoduConfigurationSection section = doc.GetOrAddSection("*");
        section.Set("a", "1");
        section.Set("b", "2");
        section.Set("c", "3");

        string text = doc.ToString();

        int idxA = text.IndexOf("a =", StringComparison.Ordinal);
        int idxB = text.IndexOf("b =", StringComparison.Ordinal);
        int idxC = text.IndexOf("c =", StringComparison.Ordinal);

        Assert.IsTrue(idxA < idxB && idxB < idxC);
    }

    /// <summary>
    /// Verifies that leading property comments are emitted before the property line when
    /// <see cref="BoduConfigurationWriteOptions.PreserveComments" /> is enabled.
    /// </summary>
    [TestMethod]
    public void Save_WhenPreserveCommentsAndPropertyHasLeading_ShouldEmitLeadingThenProperty()
    {
        BoduConfigurationDocument doc = BoduConfigurationDocument.Parse(BoduConfigurationFixtures.CommentsAndProperties);

        string text = doc.ToString();

        Assert.IsTrue(text.IndexOf("# leading property comment", StringComparison.Ordinal)
            < text.IndexOf("format.indent.size", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that inline comments are emitted when
    /// <see cref="BoduConfigurationWriteOptions.WriteInlineComments" /> is enabled.
    /// </summary>
    [TestMethod]
    public void Save_WhenWriteInlineCommentsIsTrue_ShouldEmitInlineCommentOnPropertyLine()
    {
        BoduConfigurationDocument doc = BoduConfigurationDocument.Parse(BoduConfigurationFixtures.InlineComments);

        BoduConfigurationWriteOptions options = new() { WriteInlineComments = true };
        using StringWriter sw = new();
        doc.Save(sw, options);

        StringAssert.Contains(sw.ToString(), "# standard Bodu indent");
    }

    /// <summary>
    /// Verifies that inline comments are omitted when
    /// <see cref="BoduConfigurationWriteOptions.WriteInlineComments" /> is disabled (the
    /// EditorConfig-compatible preset).
    /// </summary>
    [TestMethod]
    public void Save_WhenWriteInlineCommentsIsFalse_ShouldOmitInlineComments()
    {
        BoduConfigurationDocument doc = BoduConfigurationDocument.Parse(BoduConfigurationFixtures.InlineComments);

        using StringWriter sw = new();
        doc.Save(sw, BoduConfigurationWriteOptions.EditorConfigCompatible);

        Assert.IsFalse(sw.ToString().Contains("# standard Bodu indent"));
    }
}
