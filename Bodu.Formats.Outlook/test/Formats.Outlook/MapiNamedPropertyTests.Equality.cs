// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MapiNamedPropertyTests.Equality.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Outlook;

public partial class MapiNamedPropertyTests
{
    /// <summary>
    /// Verifies that identities with the same property set and numeric identifier are equal through every equality
    /// surface.
    /// </summary>
    [TestMethod]
    public void Equals_WhenSameNumericIdentity_ShouldBeEqual()
    {
        var left = new MapiNamedProperty(PublicStrings, 0x00008233u);
        var right = new MapiNamedProperty(PublicStrings, 0x00008233u);

        Assert.IsTrue(left.Equals(right));
        Assert.IsTrue(left.Equals((object)right));
        Assert.IsTrue(left == right);
        Assert.IsFalse(left != right);
        Assert.AreEqual(left.GetHashCode(), right.GetHashCode());
    }

    /// <summary>
    /// Verifies that identities with the same property set and name are equal, using ordinal name comparison.
    /// </summary>
    [TestMethod]
    public void Equals_WhenSameStringIdentity_ShouldBeEqual()
    {
        var left = new MapiNamedProperty(PublicStrings, "Keywords");
        var right = new MapiNamedProperty(PublicStrings, "Keywords");

        Assert.IsTrue(left.Equals(right));
        Assert.AreEqual(left.GetHashCode(), right.GetHashCode());
    }

    /// <summary>
    /// Verifies that names differing only by case are unequal under the ordinal comparison.
    /// </summary>
    [TestMethod]
    public void Equals_WhenNameDiffersByCase_ShouldBeUnequal()
    {
        var left = new MapiNamedProperty(PublicStrings, "Keywords");
        var right = new MapiNamedProperty(PublicStrings, "keywords");

        Assert.IsFalse(left.Equals(right));
        Assert.IsTrue(left != right);
    }

    /// <summary>
    /// Verifies that a numeric identity and a string identity are unequal, and that differing property sets make
    /// otherwise-identical identities unequal.
    /// </summary>
    [TestMethod]
    public void Equals_WhenKindOrPropertySetDiffers_ShouldBeUnequal()
    {
        var numeric = new MapiNamedProperty(PublicStrings, 0x00008233u);
        var text = new MapiNamedProperty(PublicStrings, "Keywords");
        var otherSet = new MapiNamedProperty(Guid.NewGuid(), 0x00008233u);

        Assert.IsFalse(numeric.Equals(text));
        Assert.IsFalse(numeric.Equals(otherSet));
    }
}
