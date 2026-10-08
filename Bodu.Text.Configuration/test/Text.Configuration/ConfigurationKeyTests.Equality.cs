// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationKeyTests.Equality.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Configuration;

public partial class ConfigurationKeyTests
{
    /// <summary>
    /// Verifies that two keys with the same segment sequence compare equal across operators, <see cref="object.Equals" />
    /// , and hashing.
    /// </summary>
    [TestMethod]
    public void Equality_WhenSegmentsMatch_ShouldBeEqual()
    {
        var a = new ConfigurationKey("logging:level");
        var b = new ConfigurationKey("logging:level");

        Assert.IsTrue(a == b);
        Assert.IsFalse(a != b);
        Assert.IsTrue(a.Equals((object)b));
        Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
    }

    /// <summary>
    /// Verifies that two keys with differing segments compare unequal across operators and <see cref="object.Equals" />.
    /// </summary>
    [TestMethod]
    public void Equality_WhenSegmentsDiffer_ShouldNotBeEqual()
    {
        var a = new ConfigurationKey("logging:level");
        var c = new ConfigurationKey("logging:other");

        Assert.IsTrue(a != c);
        Assert.IsFalse(a == c);
        Assert.IsFalse(a.Equals((object)c));
    }

    /// <summary>
    /// Verifies that keys with differing segment counts compare unequal.
    /// </summary>
    [TestMethod]
    public void Equals_WhenSegmentCountsDiffer_ShouldReturnFalse()
    {
        var a = new ConfigurationKey("logging:level");
        var shorter = new ConfigurationKey("logging");

        Assert.IsFalse(a.Equals(shorter));
    }

    /// <summary>
    /// Verifies that comparing a key to an instance of a different type returns <see langword="false" />.
    /// </summary>
    [TestMethod]
    public void Equals_WhenOtherIsDifferentType_ShouldReturnFalse()
    {
        Assert.IsFalse(new ConfigurationKey("logging:level").Equals("not a key"));
    }

    /// <summary>
    /// Verifies that under <see cref="ConfigurationKeyMapping.Identity" /> a key with a dot and the same key with a
    /// colon in its place compare unequal, since the mapping keeps each key as written.
    /// </summary>
    [TestMethod]
    public void Equals_WhenMappingIsIdentityAndKeysDifferInSeparator_ShouldReturnFalse()
    {
        ConfigurationKeyOptions options = new() { Mapping = ConfigurationKeyMapping.Identity };

        var dotted = new ConfigurationKey("a.b", options);
        var colon = new ConfigurationKey("a:b", options);

        Assert.IsFalse(dotted.Equals(colon));
    }

    /// <summary>
    /// Verifies that two keys that differ only in the whitespace around their separators compare equal and hash alike,
    /// since each segment is trimmed.
    /// </summary>
    [TestMethod]
    public void Equals_WhenKeysDifferOnlyInWhitespaceAroundSeparators_ShouldReturnTrue()
    {
        var spaced = new ConfigurationKey("a . b");
        var compact = new ConfigurationKey("a.b");

        Assert.IsTrue(spaced.Equals(compact));
        Assert.AreEqual(compact.GetHashCode(), spaced.GetHashCode());
    }

    /// <summary>
    /// Verifies that <see cref="ConfigurationKey.ToString" /> returns the canonical key path.
    /// </summary>
    [TestMethod]
    public void ToString_ShouldReturnPath()
    {
        var key = new ConfigurationKey("logging:level");

        Assert.AreEqual(key.Path, key.ToString());
    }
}
