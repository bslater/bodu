// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduConfigurationWriterTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.IO;
using Bodu.Text.Configuration.Infrastructure;

namespace Bodu.Text.Configuration;

/// <summary>
/// Tests for the internal writer pipeline exercised via
/// <see cref="BoduConfigurationDocument.Save(System.IO.TextWriter, BoduConfigurationWriteOptions?)" />.
/// </summary>
[TestClass]
public partial class BoduConfigurationWriterTests
{
    /// <summary>
    /// Verifies that emitting an empty document produces no output.
    /// </summary>
    [TestMethod]
    public void Save_WhenDocumentIsEmpty_ShouldProduceEmptyOutput()
    {
        BoduConfigurationDocument doc = new();
        using StringWriter sw = new();

        doc.Save(sw);

        Assert.AreEqual(string.Empty, sw.ToString());
    }

    /// <summary>
    /// Verifies that emitting a section header uses the <c>[pattern]</c> bracketed form on its own line.
    /// </summary>
    [TestMethod]
    public void Save_WhenSectionPresent_ShouldEmitBracketedHeader()
    {
        BoduConfigurationDocument doc = new();
        BoduConfigurationSection section = doc.GetOrAddSection("*.cs");
        section.Set("a", "b");

        string text = doc.ToString();

        StringAssert.Contains(text, "[*.cs]");
    }

    /// <summary>
    /// Verifies that emitting a property uses the configured key/value separator.
    /// </summary>
    [TestMethod]
    public void Save_WhenSeparatorIsConfigured_ShouldUseSuppliedSeparator()
    {
        BoduConfigurationDocument doc = BoduConfigurationDocument.Parse(BoduConfigurationFixtures.Minimal);
        BoduConfigurationWriteOptions options = new() { KeyValueSeparator = "=" };

        using StringWriter sw = new();
        doc.Save(sw, options);

        StringAssert.Contains(sw.ToString(), "format.indent.size=4");
    }

    /// <summary>
    /// Verifies that the writer emits the configured newline sequence between lines.
    /// </summary>
    [TestMethod]
    public void Save_WhenNewLineIsCrLf_ShouldUseCrLfBetweenLines()
    {
        BoduConfigurationDocument doc = BoduConfigurationDocument.Parse(BoduConfigurationFixtures.Minimal);
        BoduConfigurationWriteOptions options = new() { NewLine = "\r\n" };

        using StringWriter sw = new();
        doc.Save(sw, options);

        StringAssert.Contains(sw.ToString(), "[*]\r\n");
    }

    /// <summary>
    /// Verifies that <see cref="BoduConfigurationDocument.Save(System.IO.TextWriter, BoduConfigurationWriteOptions?)" />
    /// rejects a <see langword="null" /> writer.
    /// </summary>
    [TestMethod]
    public void Save_WhenTextWriterIsNull_ShouldThrowExactly()
    {
        BoduConfigurationDocument doc = new();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            doc.Save((TextWriter)null!);
        });
    }
}
