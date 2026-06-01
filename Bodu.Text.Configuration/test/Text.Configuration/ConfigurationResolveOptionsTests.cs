// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationResolveOptionsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Configuration;

/// <summary>
/// Tests for <see cref="ConfigurationResolveOptions" /> defaults and per-profile presets.
/// </summary>
[TestClass]
public partial class ConfigurationResolveOptionsTests
{
    /// <summary>
    /// Verifies that the default options apply preamble properties and treat unset as a literal value.
    /// </summary>
    [TestMethod]
    public void Defaults_WhenAccessed_ShouldFollowBoduProfile()
    {
        ConfigurationResolveOptions options = new();

        Assert.IsTrue(options.ApplyPreambleProperties);
        Assert.AreEqual(ConfigurationMissingPathRootMode.UseEmptyRoot, options.MissingPathRootMode);
        Assert.AreEqual(ConfigurationUnsetValueMode.TreatAsLiteral, options.UnsetValueMode);
    }
}
