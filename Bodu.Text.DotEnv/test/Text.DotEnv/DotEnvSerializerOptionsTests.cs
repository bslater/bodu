// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DotEnvSerializerOptionsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Serialization;

namespace Bodu.Text.DotEnv;

/// <summary>
/// Verifies the option surface of <see cref="DotEnvSerializerOptions" />: its defaults, that every setting round
/// trips, and that the shared <see cref="DotEnvSerializerOptions.Default" /> instance is frozen.
/// </summary>
[TestClass]
public sealed class DotEnvSerializerOptionsTests
{
    /// <summary>
    /// Verifies that a new instance carries the general-purpose defaults. Nulls are skipped on write by default
    /// because a line-based environment file has no representation for one.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenDefaultConstructed_ShouldCarryGeneralDefaults()
    {
        var options = new DotEnvSerializerOptions();

        Assert.IsFalse(options.PropertyNameCaseInsensitive);
        Assert.IsFalse(options.IncludeFields);
        Assert.IsFalse(options.WriteExportPrefix);
        Assert.AreEqual(IgnoreCondition.WhenWritingNull, options.DefaultIgnoreCondition);
        Assert.IsNull(options.PropertyNamingPolicy);
        Assert.IsFalse(options.IsReadOnly);
    }

    /// <summary>
    /// Verifies that the <see cref="DotEnvSerializerDefaults.Web" /> preset applies SCREAMING_SNAKE_CASE naming and
    /// case-insensitive property matching, which is the convention environment files use.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenWebDefaults_ShouldApplyUpperSnakeCaseAndCaseInsensitiveMatching()
    {
        var options = new DotEnvSerializerOptions(DotEnvSerializerDefaults.Web);

        Assert.AreSame(NamingPolicy.SnakeCaseUpper, options.PropertyNamingPolicy);
        Assert.IsTrue(options.PropertyNameCaseInsensitive);
    }

    /// <summary>
    /// Verifies that the <see cref="DotEnvSerializerDefaults.General" /> preset leaves naming untouched.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenGeneralDefaults_ShouldLeaveNamingUnset()
    {
        var options = new DotEnvSerializerOptions(DotEnvSerializerDefaults.General);

        Assert.IsNull(options.PropertyNamingPolicy);
        Assert.IsFalse(options.PropertyNameCaseInsensitive);
    }

    /// <summary>
    /// Verifies that every mutable setting round trips through its property.
    /// </summary>
    [TestMethod]
    public void Properties_WhenSetOnMutableInstance_ShouldRoundTrip()
    {
        var options = new DotEnvSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            IncludeFields = true,
            WriteExportPrefix = true,
            DefaultIgnoreCondition = IgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = NamingPolicy.CamelCase,
        };

        Assert.IsTrue(options.PropertyNameCaseInsensitive);
        Assert.IsTrue(options.IncludeFields);
        Assert.IsTrue(options.WriteExportPrefix);
        Assert.AreEqual(IgnoreCondition.WhenWritingNull, options.DefaultIgnoreCondition);
        Assert.AreSame(NamingPolicy.CamelCase, options.PropertyNamingPolicy);
    }

    /// <summary>
    /// Verifies that the shared default instance is read-only, so callers cannot mutate the options every
    /// serializer call falls back to.
    /// </summary>
    [TestMethod]
    public void Default_ShouldBeReadOnly()
    {
        Assert.IsTrue(DotEnvSerializerOptions.Default.IsReadOnly);
    }

    /// <summary>
    /// Verifies that every setter on the read-only default instance throws
    /// <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(ReadOnlyMutations))]
    public void Setters_WhenInstanceIsReadOnly_ShouldThrowExactly(string testName, Action<DotEnvSerializerOptions> mutate)
    {
        Assert.IsNotNull(testName);

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            mutate(DotEnvSerializerOptions.Default);
        });
    }

    /// <summary>
    /// Gets the per-setter mutations applied against the read-only default instance.
    /// </summary>
    /// <value>The mutation cases.</value>
    public static IEnumerable<object[]> ReadOnlyMutations =>
    [
        ["PropertyNameCaseInsensitive", (Action<DotEnvSerializerOptions>)(o => o.PropertyNameCaseInsensitive = true)],
        ["IncludeFields", (Action<DotEnvSerializerOptions>)(o => o.IncludeFields = true)],
        ["WriteExportPrefix", (Action<DotEnvSerializerOptions>)(o => o.WriteExportPrefix = true)],
        ["DefaultIgnoreCondition", (Action<DotEnvSerializerOptions>)(o => o.DefaultIgnoreCondition = IgnoreCondition.Always)],
        ["PropertyNamingPolicy", (Action<DotEnvSerializerOptions>)(o => o.PropertyNamingPolicy = NamingPolicy.CamelCase)],
    ];
}
