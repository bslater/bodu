// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduConfigurationSectionTests.Set.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Configuration;

public partial class BoduConfigurationSectionTests
{
    /// <summary>
    /// Verifies that <see cref="BoduConfigurationSection.Set(string, string, BoduConfigurationKeyOptions?)" />
    /// appends a new property when the key is not yet present.
    /// </summary>
    [TestMethod]
    public void Set_WhenKeyIsNew_ShouldAppendProperty()
    {
        BoduConfigurationSection section = new("*.cs");

        section.Set("format.indent.size", "4");

        Assert.AreEqual(1, section.Properties.Count);
        Assert.AreEqual("4", section.Properties[0].Value);
    }

    /// <summary>
    /// Verifies that <see cref="BoduConfigurationSection.Set(string, string, BoduConfigurationKeyOptions?)" />
    /// updates the existing property's value when the key is already present.
    /// </summary>
    [TestMethod]
    public void Set_WhenKeyExists_ShouldUpdateValueWithoutAppending()
    {
        BoduConfigurationSection section = new("*.cs");
        section.Set("format.indent.size", "4");

        section.Set("format.indent.size", "2");

        Assert.AreEqual(1, section.Properties.Count);
        Assert.AreEqual("2", section.Properties[0].Value);
    }

    /// <summary>
    /// Verifies that <see cref="BoduConfigurationSection.Set(string, bool, BoduConfigurationKeyOptions?)" />
    /// emits the EditorConfig boolean form.
    /// </summary>
    [TestMethod]
    public void Set_WhenValueIsBoolean_ShouldEmitTrueOrFalseLiteral()
    {
        BoduConfigurationSection section = new("*");
        section.Set("a", true);
        section.Set("b", false);

        Assert.AreEqual("true", section.Properties[0].Value);
        Assert.AreEqual("false", section.Properties[1].Value);
    }

    /// <summary>
    /// Verifies that <see cref="BoduConfigurationSection.Set(string, int, BoduConfigurationKeyOptions?)" />
    /// emits invariant-culture formatted integers regardless of the current culture.
    /// </summary>
    [TestMethod]
    public void Set_WhenValueIsInt32_ShouldEmitInvariantFormatting()
    {
        BoduConfigurationSection section = new("*");
        section.Set("a", 1234);

        Assert.AreEqual("1234", section.Properties[0].Value);
    }

    /// <summary>
    /// Verifies that <see cref="BoduConfigurationSection.Set(string, long, BoduConfigurationKeyOptions?)" />
    /// emits invariant-culture formatted 64-bit integers.
    /// </summary>
    [TestMethod]
    public void Set_WhenValueIsInt64_ShouldEmitInvariantFormatting()
    {
        BoduConfigurationSection section = new("*");
        section.Set("a", 9_000_000_000L);

        Assert.AreEqual("9000000000", section.Properties[0].Value);
    }

    /// <summary>
    /// Verifies that <see cref="BoduConfigurationSection.Set(string, string, BoduConfigurationKeyOptions?)" />
    /// rejects a <see langword="null" /> key.
    /// </summary>
    [TestMethod]
    public void Set_WhenKeyIsNull_ShouldThrowExactly()
    {
        BoduConfigurationSection section = new("*");

        Assert.ThrowsExactly<ArgumentNullException>(() => section.Set(null!, "value"));
    }

    /// <summary>
    /// Verifies that <see cref="BoduConfigurationSection.Set(string, string, BoduConfigurationKeyOptions?)" />
    /// rejects a <see langword="null" /> value.
    /// </summary>
    [TestMethod]
    public void Set_WhenValueIsNull_ShouldThrowExactly()
    {
        BoduConfigurationSection section = new("*");

        Assert.ThrowsExactly<ArgumentNullException>(() => section.Set("key", (string)null!));
    }

    /// <summary>
    /// Verifies that <see cref="BoduConfigurationSection.TryGetProperty(string, out BoduConfigurationProperty?, BoduConfigurationKeyOptions?)" />
    /// returns the existing property on a hit.
    /// </summary>
    [TestMethod]
    public void TryGetProperty_WhenKeyExists_ShouldReturnTrueAndProperty()
    {
        BoduConfigurationSection section = new("*");
        section.Set("a.b", "value");

        bool found = section.TryGetProperty("a.b", out BoduConfigurationProperty? property);

        Assert.IsTrue(found);
        Assert.IsNotNull(property);
        Assert.AreEqual("value", property!.Value);
    }

    /// <summary>
    /// Verifies that <see cref="BoduConfigurationSection.TryGetProperty(string, out BoduConfigurationProperty?, BoduConfigurationKeyOptions?)" />
    /// returns <see langword="false" /> and <see langword="null" /> on a miss.
    /// </summary>
    [TestMethod]
    public void TryGetProperty_WhenKeyIsMissing_ShouldReturnFalseAndNull()
    {
        BoduConfigurationSection section = new("*");

        bool found = section.TryGetProperty("a.b", out BoduConfigurationProperty? property);

        Assert.IsFalse(found);
        Assert.IsNull(property);
    }

    /// <summary>
    /// Verifies that <see cref="BoduConfigurationSection.RemoveProperty(string, BoduConfigurationKeyOptions?)" />
    /// removes the property and returns <see langword="true" /> on a hit.
    /// </summary>
    [TestMethod]
    public void RemoveProperty_WhenKeyExists_ShouldRemoveAndReturnTrue()
    {
        BoduConfigurationSection section = new("*");
        section.Set("a.b", "value");

        bool removed = section.RemoveProperty("a.b");

        Assert.IsTrue(removed);
        Assert.AreEqual(0, section.Properties.Count);
    }

    /// <summary>
    /// Verifies that <see cref="BoduConfigurationSection.RemoveProperty(string, BoduConfigurationKeyOptions?)" />
    /// returns <see langword="false" /> when the key is absent.
    /// </summary>
    [TestMethod]
    public void RemoveProperty_WhenKeyIsMissing_ShouldReturnFalse()
    {
        BoduConfigurationSection section = new("*");

        bool removed = section.RemoveProperty("a.b");

        Assert.IsFalse(removed);
    }
}
