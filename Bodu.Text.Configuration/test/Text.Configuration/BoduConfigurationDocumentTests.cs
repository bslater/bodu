// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduConfigurationDocumentTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Configuration;

/// <summary>
/// Tests for the top-level <see cref="BoduConfigurationDocument" /> shape: preamble, sections collection,
/// the <c>root</c> property, the <see cref="BoduConfigurationDocument.Global" /> alias, and mutation helpers.
/// </summary>
[TestClass]
public partial class BoduConfigurationDocumentTests
{
    /// <summary>
    /// Verifies that a freshly constructed document exposes a preamble flagged
    /// <see cref="BoduConfigurationSection.IsPreamble" /> and an empty sections collection.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenDefault_ShouldExposeEmptyPreambleAndNoSections()
    {
        BoduConfigurationDocument doc = new();

        Assert.IsTrue(doc.Preamble.IsPreamble);
        Assert.AreEqual(0, doc.Preamble.Properties.Count);
        Assert.AreEqual(0, doc.Sections.Count);
        Assert.IsNull(doc.Root);
    }

    /// <summary>
    /// Verifies that <see cref="BoduConfigurationDocument.Global" /> returns the same instance as
    /// <see cref="BoduConfigurationDocument.Preamble" />.
    /// </summary>
    [TestMethod]
    public void Global_WhenAccessed_ShouldReturnSameInstanceAsPreamble()
    {
        BoduConfigurationDocument doc = new();

        Assert.AreSame(doc.Preamble, doc.Global);
    }

    /// <summary>
    /// Verifies that the <c>root</c> property in the preamble is reflected on
    /// <see cref="BoduConfigurationDocument.Root" />.
    /// </summary>
    [TestMethod]
    public void Root_WhenPreambleContainsRootTrue_ShouldBeTrue()
    {
        BoduConfigurationDocument doc = BoduConfigurationDocument.Parse("root = true\n");

        Assert.AreEqual(true, doc.Root);
    }

    /// <summary>
    /// Verifies that <see cref="BoduConfigurationDocument.Root" /> reports <see langword="false" /> when the
    /// preamble contains <c>root = false</c>.
    /// </summary>
    [TestMethod]
    public void Root_WhenPreambleContainsRootFalse_ShouldBeFalse()
    {
        BoduConfigurationDocument doc = BoduConfigurationDocument.Parse("root = false\n");

        Assert.AreEqual(false, doc.Root);
    }

    /// <summary>
    /// Verifies that <see cref="BoduConfigurationDocument.Root" /> is <see langword="null" /> when the
    /// preamble omits the <c>root</c> property.
    /// </summary>
    [TestMethod]
    public void Root_WhenPreambleHasNoRootProperty_ShouldBeNull()
    {
        BoduConfigurationDocument doc = BoduConfigurationDocument.Parse("application.name = Bodu\n");

        Assert.IsNull(doc.Root);
    }

    /// <summary>
    /// Verifies that <see cref="BoduConfigurationDocument.Root" /> is <see langword="null" /> when the
    /// <c>root</c> value is not a valid boolean literal.
    /// </summary>
    [TestMethod]
    public void Root_WhenRootValueIsNotBoolean_ShouldBeNull()
    {
        BoduConfigurationDocument doc = BoduConfigurationDocument.Parse("root = sometimes\n");

        Assert.IsNull(doc.Root);
    }

    /// <summary>
    /// Verifies that <see cref="BoduConfigurationDocument.GetOrAddSection(string)" /> appends a new section
    /// when no section with the supplied pattern exists.
    /// </summary>
    [TestMethod]
    public void GetOrAddSection_WhenPatternIsNew_ShouldAppendSection()
    {
        BoduConfigurationDocument doc = new();

        BoduConfigurationSection section = doc.GetOrAddSection("*.cs");

        Assert.AreEqual(1, doc.Sections.Count);
        Assert.AreSame(section, doc.Sections[0]);
        Assert.AreEqual("*.cs", section.Pattern);
    }

    /// <summary>
    /// Verifies that <see cref="BoduConfigurationDocument.GetOrAddSection(string)" /> returns the existing
    /// section when one with the same pattern already exists.
    /// </summary>
    [TestMethod]
    public void GetOrAddSection_WhenPatternExists_ShouldReturnExistingSection()
    {
        BoduConfigurationDocument doc = new();
        BoduConfigurationSection first = doc.GetOrAddSection("*.cs");

        BoduConfigurationSection second = doc.GetOrAddSection("*.cs");

        Assert.AreSame(first, second);
        Assert.AreEqual(1, doc.Sections.Count);
    }

    /// <summary>
    /// Verifies that <see cref="BoduConfigurationDocument.AddSection(BoduConfigurationSection)" /> rejects
    /// the preamble (which has no pattern).
    /// </summary>
    [TestMethod]
    public void AddSection_WhenSectionIsPreamble_ShouldThrowExactly()
    {
        BoduConfigurationDocument doc = new();

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            doc.AddSection(doc.Preamble);
        });
    }
}
