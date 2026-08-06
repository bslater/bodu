// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniSerializerOptionsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Serialization;

namespace Bodu.Text.Ini;

/// <summary>
/// Verifies the option surface of <see cref="IniSerializerOptions" />: its defaults, that every setting round trips,
/// and that the shared <see cref="IniSerializerOptions.Default" /> instance is frozen.
/// </summary>
[TestClass]
public sealed class IniSerializerOptionsTests
{
    /// <summary>
    /// Verifies that a new instance carries the general-purpose defaults, which permit duplicate sections and keys.
    /// Nulls are skipped on write by default because a line-based INI file has no representation for one.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenDefaultConstructed_ShouldCarryGeneralDefaults()
    {
        var options = new IniSerializerOptions();

        Assert.IsFalse(options.PropertyNameCaseInsensitive);
        Assert.IsFalse(options.IncludeFields);
        Assert.AreEqual(IgnoreCondition.WhenWritingNull, options.DefaultIgnoreCondition);
        Assert.IsNull(options.PropertyNamingPolicy);
        Assert.IsNull(options.GlobalSectionName);
        Assert.AreNotEqual(IniDuplicateSectionBehavior.Disallowed, options.DuplicateSectionBehavior);
        Assert.AreNotEqual(IniDuplicateKeyBehavior.Disallowed, options.DuplicateKeyBehavior);
        Assert.IsFalse(options.IsReadOnly);
    }

    /// <summary>
    /// Verifies that the <see cref="IniSerializerDefaults.Strict" /> preset disallows both duplicate sections and
    /// duplicate keys, matching configparser's strict mode.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenStrictDefaults_ShouldDisallowDuplicates()
    {
        var options = new IniSerializerOptions(IniSerializerDefaults.Strict);

        Assert.AreEqual(IniDuplicateSectionBehavior.Disallowed, options.DuplicateSectionBehavior);
        Assert.AreEqual(IniDuplicateKeyBehavior.Disallowed, options.DuplicateKeyBehavior);
    }

    /// <summary>
    /// Verifies that the <see cref="IniSerializerDefaults.General" /> preset leaves the duplicate policies permissive.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenGeneralDefaults_ShouldLeaveDuplicatesPermitted()
    {
        var options = new IniSerializerOptions(IniSerializerDefaults.General);

        Assert.AreNotEqual(IniDuplicateSectionBehavior.Disallowed, options.DuplicateSectionBehavior);
        Assert.AreNotEqual(IniDuplicateKeyBehavior.Disallowed, options.DuplicateKeyBehavior);
    }

    /// <summary>
    /// Verifies that every mutable setting round trips through its property.
    /// </summary>
    [TestMethod]
    public void Properties_WhenSetOnMutableInstance_ShouldRoundTrip()
    {
        var options = new IniSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            IncludeFields = true,
            DefaultIgnoreCondition = IgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = NamingPolicy.CamelCase,
            GlobalSectionName = "DEFAULT",
            DuplicateSectionBehavior = IniDuplicateSectionBehavior.Disallowed,
            DuplicateKeyBehavior = IniDuplicateKeyBehavior.Disallowed,
        };

        Assert.IsTrue(options.PropertyNameCaseInsensitive);
        Assert.IsTrue(options.IncludeFields);
        Assert.AreEqual(IgnoreCondition.WhenWritingNull, options.DefaultIgnoreCondition);
        Assert.AreSame(NamingPolicy.CamelCase, options.PropertyNamingPolicy);
        Assert.AreEqual("DEFAULT", options.GlobalSectionName);
        Assert.AreEqual(IniDuplicateSectionBehavior.Disallowed, options.DuplicateSectionBehavior);
        Assert.AreEqual(IniDuplicateKeyBehavior.Disallowed, options.DuplicateKeyBehavior);
    }

    /// <summary>
    /// Verifies that the shared default instance is read-only, so callers cannot mutate the options every
    /// serializer call falls back to.
    /// </summary>
    [TestMethod]
    public void Default_ShouldBeReadOnly()
    {
        Assert.IsTrue(IniSerializerOptions.Default.IsReadOnly);
    }

    /// <summary>
    /// Verifies that every setter on the read-only default instance throws
    /// <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(ReadOnlyMutations))]
    public void Setters_WhenInstanceIsReadOnly_ShouldThrowExactly(string testName, Action<IniSerializerOptions> mutate)
    {
        Assert.IsNotNull(testName);

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            mutate(IniSerializerOptions.Default);
        });
    }

    /// <summary>
    /// Gets the per-setter mutations applied against the read-only default instance.
    /// </summary>
    /// <value>The mutation cases.</value>
    public static IEnumerable<object[]> ReadOnlyMutations =>
    [
        ["PropertyNameCaseInsensitive", (Action<IniSerializerOptions>)(o => o.PropertyNameCaseInsensitive = true)],
        ["IncludeFields", (Action<IniSerializerOptions>)(o => o.IncludeFields = true)],
        ["DefaultIgnoreCondition", (Action<IniSerializerOptions>)(o => o.DefaultIgnoreCondition = IgnoreCondition.Always)],
        ["PropertyNamingPolicy", (Action<IniSerializerOptions>)(o => o.PropertyNamingPolicy = NamingPolicy.CamelCase)],
        ["GlobalSectionName", (Action<IniSerializerOptions>)(o => o.GlobalSectionName = "DEFAULT")],
        ["DuplicateSectionBehavior", (Action<IniSerializerOptions>)(o => o.DuplicateSectionBehavior = IniDuplicateSectionBehavior.Disallowed)],
        ["DuplicateKeyBehavior", (Action<IniSerializerOptions>)(o => o.DuplicateKeyBehavior = IniDuplicateKeyBehavior.Disallowed)],
    ];
}
