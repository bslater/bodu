// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduConfigurationSectionTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Configuration;

/// <summary>
/// Tests for <see cref="BoduConfigurationSection" /> construction, preamble vs. regular discrimination,
/// property mutation, leading-comment storage, and glob matching.
/// </summary>
[TestClass]
public partial class BoduConfigurationSectionTests
{
    /// <summary>
    /// Verifies that the ctor stores the supplied pattern and reports the section as not-preamble.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenPatternProvided_ShouldExposePatternAndNotBePreamble()
    {
        BoduConfigurationSection section = new("*.cs");

        Assert.AreEqual("*.cs", section.Pattern);
        Assert.IsFalse(section.IsPreamble);
    }

    /// <summary>
    /// Verifies that the ctor rejects a <see langword="null" /> pattern with
    /// <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenPatternIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = new BoduConfigurationSection(null!));
    }

    /// <summary>
    /// Verifies that the ctor rejects empty or whitespace patterns with <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenPatternIsBlank_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentException>(() => _ = new BoduConfigurationSection(string.Empty));
        Assert.ThrowsExactly<ArgumentException>(() => _ = new BoduConfigurationSection("   "));
    }

    /// <summary>
    /// Verifies that an empty section exposes no properties or comments.
    /// </summary>
    [TestMethod]
    public void Properties_WhenNewlyCreated_ShouldBeEmpty()
    {
        BoduConfigurationSection section = new("*.cs");

        Assert.AreEqual(0, section.Properties.Count);
        Assert.AreEqual(0, section.LeadingComments.Count);
    }

    /// <summary>
    /// Verifies that <see cref="BoduConfigurationSection.AddProperty(BoduConfigurationProperty)" /> rejects
    /// a <see langword="null" /> property.
    /// </summary>
    [TestMethod]
    public void AddProperty_WhenPropertyIsNull_ShouldThrowExactly()
    {
        BoduConfigurationSection section = new("*.cs");

        Assert.ThrowsExactly<ArgumentNullException>(() => section.AddProperty(null!));
    }

    /// <summary>
    /// Verifies that the synthetic preamble has <see cref="BoduConfigurationSection.Pattern" /> equal to
    /// <see langword="null" /> and <see cref="BoduConfigurationSection.IsPreamble" /> equal to
    /// <see langword="true" />.
    /// </summary>
    [TestMethod]
    public void Preamble_WhenAccessed_ShouldHaveNullPatternAndIsPreambleTrue()
    {
        BoduConfigurationDocument doc = new();

        Assert.IsNull(doc.Preamble.Pattern);
        Assert.IsTrue(doc.Preamble.IsPreamble);
    }
}
