// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduConfigurationPropertyTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Configuration;

/// <summary>
/// Tests for <see cref="BoduConfigurationProperty" /> construction, key/value access, and leading/inline
/// comment storage.
/// </summary>
[TestClass]
public partial class BoduConfigurationPropertyTests
{
    /// <summary>
    /// Verifies that the raw-key/value ctor populates <see cref="BoduConfigurationProperty.Key" /> and the
    /// derived <see cref="BoduConfigurationProperty.ConfigurationKey" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenRawKeyAndValueProvided_ShouldComputeConfigurationKey()
    {
        BoduConfigurationProperty property = new("logging.level.default", "Information");

        Assert.AreEqual("logging.level.default", property.RawKey);
        Assert.AreEqual("logging:level:default", property.ConfigurationKey);
        Assert.AreEqual("Information", property.Value);
    }

    /// <summary>
    /// Verifies that an empty value is permitted and stored as <see cref="string.Empty" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenValueIsEmpty_ShouldStoreEmptyString()
    {
        BoduConfigurationProperty property = new("key", string.Empty);

        Assert.AreEqual(string.Empty, property.Value);
    }

    /// <summary>
    /// Verifies that assigning a new value updates <see cref="BoduConfigurationProperty.Value" />.
    /// </summary>
    [TestMethod]
    public void Value_WhenAssigned_ShouldReflectNewValue()
    {
        BoduConfigurationProperty property = new("key", "old");

        property.Value = "new";

        Assert.AreEqual("new", property.Value);
    }

    /// <summary>
    /// Verifies that assigning <see langword="null" /> to <see cref="BoduConfigurationProperty.Value" />
    /// throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Value_WhenAssignedNull_ShouldThrowExactly()
    {
        BoduConfigurationProperty property = new("key", "value");

        Assert.ThrowsExactly<ArgumentNullException>(() => property.Value = null!);
    }

    /// <summary>
    /// Verifies that the raw-key ctor rejects a <see langword="null" /> key with
    /// <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenRawKeyIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = new BoduConfigurationProperty(null!, "value"));
    }

    /// <summary>
    /// Verifies that the raw-key ctor rejects empty or whitespace keys with
    /// <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenRawKeyIsBlank_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentException>(() => _ = new BoduConfigurationProperty(string.Empty, "value"));
        Assert.ThrowsExactly<ArgumentException>(() => _ = new BoduConfigurationProperty("   ", "value"));
    }

    /// <summary>
    /// Verifies that the raw-key ctor rejects a <see langword="null" /> value.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenValueIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = new BoduConfigurationProperty("key", null!));
    }

    /// <summary>
    /// Verifies that the parsed-key ctor rejects a default <see cref="BoduConfigurationKey" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenKeyIsDefault_ShouldThrowExactly()
    {
        BoduConfigurationKey defaultKey = default;

        Assert.ThrowsExactly<ArgumentException>(() => _ = new BoduConfigurationProperty(defaultKey, "value"));
    }
}
