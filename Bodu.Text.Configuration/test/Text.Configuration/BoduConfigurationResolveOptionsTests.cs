// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduConfigurationResolveOptionsTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Configuration;

/// <summary>
/// Tests for <see cref="BoduConfigurationResolveOptions" /> defaults and per-profile presets.
/// </summary>
[TestClass]
public partial class BoduConfigurationResolveOptionsTests
{
    /// <summary>
    /// Verifies that the default options apply preamble properties and treat unset as a literal value.
    /// </summary>
    [TestMethod]
    public void Defaults_WhenAccessed_ShouldFollowBoduProfile()
    {
        BoduConfigurationResolveOptions options = new();

        Assert.IsTrue(options.ApplyPreambleProperties);
        Assert.AreEqual(BoduConfigurationMissingPathRootMode.UseEmptyRoot, options.MissingPathRootMode);
        Assert.AreEqual(BoduConfigurationUnsetValueMode.TreatAsLiteral, options.UnsetValueMode);
    }
}
