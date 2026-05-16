// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduConfigurationParseOptionsTests.IniInterop.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Formats;

namespace Bodu.Text.Configuration;

public partial class BoduConfigurationParseOptionsTests
{
    /// <summary>
    /// Verifies that <see cref="BoduConfigurationParseOptions.ToIniParseOptions" /> projects the duplicate-key
    /// behaviour onto the equivalent <see cref="IniParseOptions" /> field.
    /// </summary>
    [TestMethod]
    public void ToIniParseOptions_WhenProjected_ShouldPreserveDuplicateKeyBehavior()
    {
        BoduConfigurationParseOptions strict = BoduConfigurationParseOptions.Strict;

        IniParseOptions ini = strict.ToIniParseOptions();

        Assert.AreEqual(IniDuplicateKeyBehavior.Disallowed, ini.DuplicateKeyBehavior);
    }

    /// <summary>
    /// Verifies that <see cref="BoduConfigurationParseOptions.ToIniParseOptions" /> maps
    /// <see cref="BoduConfigurationDuplicateSectionMode.MergeAdjacent" /> and
    /// <see cref="BoduConfigurationDuplicateSectionMode.MergeAll" /> to <see cref="IniDuplicateSectionBehavior.Merge" />.
    /// </summary>
    [TestMethod]
    public void ToIniParseOptions_WhenDuplicateSectionModeIsMerge_ShouldProjectToIniMerge()
    {
        BoduConfigurationParseOptions options = new() { DuplicateSectionMode = BoduConfigurationDuplicateSectionMode.MergeAdjacent };

        Assert.AreEqual(IniDuplicateSectionBehavior.Merge, options.ToIniParseOptions().DuplicateSectionBehavior);
    }

    /// <summary>
    /// Verifies that <see cref="BoduConfigurationParseOptions.ToIniParseOptions" /> maps
    /// <see cref="BoduConfigurationDuplicateSectionMode.Reject" /> to
    /// <see cref="IniDuplicateSectionBehavior.Disallowed" />.
    /// </summary>
    [TestMethod]
    public void ToIniParseOptions_WhenDuplicateSectionModeIsReject_ShouldProjectToIniDisallowed()
    {
        BoduConfigurationParseOptions options = new() { DuplicateSectionMode = BoduConfigurationDuplicateSectionMode.Reject };

        Assert.AreEqual(IniDuplicateSectionBehavior.Disallowed, options.ToIniParseOptions().DuplicateSectionBehavior);
    }

    /// <summary>
    /// Verifies that <see cref="BoduConfigurationParseOptions.ToIniParseOptions" /> mirrors
    /// <see cref="BoduConfigurationKeyOptions.CaseSensitive" /> onto both INI case-sensitivity flags.
    /// </summary>
    [TestMethod]
    public void ToIniParseOptions_WhenKeyOptionsAreCaseSensitive_ShouldEnableIniCaseSensitivity()
    {
        BoduConfigurationParseOptions options = new()
        {
            KeyOptions = new BoduConfigurationKeyOptions { CaseSensitive = true },
        };

        IniParseOptions ini = options.ToIniParseOptions();

        Assert.IsTrue(ini.CaseSensitiveKeys);
        Assert.IsTrue(ini.CaseSensitiveSections);
    }
}
