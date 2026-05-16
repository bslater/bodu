// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduConfigurationSectionTests.IsPreamble.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Configuration;

public partial class BoduConfigurationSectionTests
{
    /// <summary>
    /// Verifies that a regular section has <see cref="BoduConfigurationSection.IsPreamble" /> equal to
    /// <see langword="false" /> and a non-null pattern.
    /// </summary>
    [TestMethod]
    public void IsPreamble_WhenSectionIsRegular_ShouldBeFalse()
    {
        BoduConfigurationSection section = new("*.cs");

        Assert.IsFalse(section.IsPreamble);
        Assert.IsNotNull(section.Pattern);
    }

    /// <summary>
    /// Verifies that the synthetic preamble has <see cref="BoduConfigurationSection.IsPreamble" /> equal to
    /// <see langword="true" /> and <see cref="BoduConfigurationSection.Pattern" /> equal to
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void IsPreamble_WhenSectionIsPreamble_ShouldBeTrueWithNullPattern()
    {
        BoduConfigurationDocument doc = new();

        Assert.IsTrue(doc.Preamble.IsPreamble);
        Assert.IsNull(doc.Preamble.Pattern);
    }

    /// <summary>
    /// Verifies that <see cref="BoduConfigurationSection.IsMatch(string)" /> on the preamble always returns
    /// <see langword="false" />; preamble layering is performed separately by the resolver.
    /// </summary>
    [TestMethod]
    public void IsMatch_WhenSectionIsPreamble_ShouldReturnFalse()
    {
        BoduConfigurationDocument doc = new();

        Assert.IsFalse(doc.Preamble.IsMatch("anything"));
    }

    /// <summary>
    /// Verifies that <see cref="BoduConfigurationSection.IsMatch(string)" /> rejects a
    /// <see langword="null" /> path.
    /// </summary>
    [TestMethod]
    public void IsMatch_WhenPathIsNull_ShouldThrowExactly()
    {
        BoduConfigurationSection section = new("*.cs");

        Assert.ThrowsExactly<ArgumentNullException>(() => section.IsMatch(null!));
    }
}
