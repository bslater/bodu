// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlSerializerOptionsTests.Ctors.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Serialization;

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies the <see cref="YamlSerializerOptions" /> constructors, including the
/// <see cref="YamlSerializerDefaults" /> presets.
/// </summary>
public partial class YamlSerializerOptionsTests
{
    /// <summary>
    /// Verifies that the parameterless constructor applies the general defaults: no naming policy, case-sensitive
    /// matching, and <see cref="IgnoreCondition.Never" /> as the default ignore condition.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenParameterless_ShouldApplyGeneralDefaults()
    {
        var options = new YamlSerializerOptions();

        Assert.IsNull(options.PropertyNamingPolicy);
        Assert.IsFalse(options.PropertyNameCaseInsensitive);
        Assert.AreEqual(IgnoreCondition.Never, options.DefaultIgnoreCondition);
    }

    /// <summary>
    /// Verifies that the <see cref="YamlSerializerDefaults.General" /> preset leaves the defaults unchanged, matching
    /// the parameterless constructor.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenGeneralDefaults_ShouldMatchParameterless()
    {
        var options = new YamlSerializerOptions(YamlSerializerDefaults.General);

        Assert.IsNull(options.PropertyNamingPolicy);
        Assert.IsFalse(options.PropertyNameCaseInsensitive);
    }

    /// <summary>
    /// Verifies that the <see cref="YamlSerializerDefaults.Web" /> preset selects camel-case property naming and
    /// case-insensitive property-name matching.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenWebDefaults_ShouldApplyCamelCaseAndCaseInsensitiveMatching()
    {
        var options = new YamlSerializerOptions(YamlSerializerDefaults.Web);

        Assert.AreSame(NamingPolicy.CamelCase, options.PropertyNamingPolicy);
        Assert.IsTrue(options.PropertyNameCaseInsensitive);
    }

    /// <summary>
    /// Verifies that the <see cref="YamlSerializerDefaults.Web" /> preset produces camel-cased keys and binds them back
    /// case-insensitively end to end.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenWebDefaults_ShouldSerializeCamelCaseAndRoundTrip()
    {
        var options = new YamlSerializerOptions(YamlSerializerDefaults.Web);

        string text = YamlSerializer.Serialize(new Point { X = 7 }, options);

        Assert.AreEqual("x: 7\n", text);

        Point roundTripped = YamlSerializer.Deserialize<Point>("X: 7\n", options);
        Assert.AreEqual(7, roundTripped.X);
    }

    /// <summary>
    /// Verifies that constructing options with an undefined <see cref="YamlSerializerDefaults" /> value throws
    /// <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenDefaultsUndefined_ShouldThrowArgumentOutOfRangeException()
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new YamlSerializerOptions((YamlSerializerDefaults)99);
        });

        Assert.AreEqual("defaults", ex.ParamName);
    }
}
