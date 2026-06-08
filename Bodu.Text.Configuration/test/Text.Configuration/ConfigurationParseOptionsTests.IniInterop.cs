// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationParseOptionsTests.IniInterop.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Ini;

namespace Bodu.Text.Configuration;

public partial class ConfigurationParseOptionsTests
{
    /// <summary>
    /// Verifies that <see cref="ConfigurationParseOptions.ToIniParseOptions" /> projects the duplicate-key
    /// behaviour onto the equivalent <see cref="IniParseOptions" /> field.
    /// </summary>
    [TestMethod]
    public void ToIniParseOptions_WhenProjected_ShouldPreserveDuplicateKeyBehavior()
    {
        ConfigurationParseOptions strict = ConfigurationParseOptions.Strict;

        var ini = strict.ToIniParseOptions();

        Assert.AreEqual(DuplicateKeyPolicy.Disallowed, ini.DuplicateKeyBehavior);
    }

    /// <summary>
    /// Verifies that <see cref="ConfigurationParseOptions.ToIniParseOptions" /> passes the duplicate
    /// section behaviour through unchanged now that the two enums are unified.
    /// </summary>
    [TestMethod]
    public void ToIniParseOptions_WhenDuplicateSectionModeIsMergeAdjacent_ShouldProjectIdentically()
    {
        ConfigurationParseOptions options = new() { DuplicateSectionMode = IniDuplicateSectionBehavior.MergeAdjacent };

        Assert.AreEqual(IniDuplicateSectionBehavior.MergeAdjacent, options.ToIniParseOptions().DuplicateSectionBehavior);
    }

    /// <summary>
    /// Verifies that <see cref="ConfigurationParseOptions.ToIniParseOptions" /> maps
    /// <see cref="IniDuplicateSectionBehavior.Disallowed" /> to
    /// <see cref="IniDuplicateSectionBehavior.Disallowed" />.
    /// </summary>
    [TestMethod]
    public void ToIniParseOptions_WhenDuplicateSectionModeIsReject_ShouldProjectToIniDisallowed()
    {
        ConfigurationParseOptions options = new() { DuplicateSectionMode = IniDuplicateSectionBehavior.Disallowed };

        Assert.AreEqual(IniDuplicateSectionBehavior.Disallowed, options.ToIniParseOptions().DuplicateSectionBehavior);
    }

    /// <summary>
    /// Verifies that <see cref="ConfigurationParseOptions.ToIniParseOptions" /> mirrors
    /// <see cref="ConfigurationKeyOptions.CaseSensitive" /> onto both INI case-sensitivity flags.
    /// </summary>
    [TestMethod]
    public void ToIniParseOptions_WhenKeyOptionsAreCaseSensitive_ShouldEnableIniCaseSensitivity()
    {
        ConfigurationParseOptions options = new()
        {
            KeyOptions = new ConfigurationKeyOptions { CaseSensitive = true },
        };

        var ini = options.ToIniParseOptions();

        Assert.IsTrue(ini.CaseSensitiveKeys);
        Assert.IsTrue(ini.CaseSensitiveSections);
    }
}
