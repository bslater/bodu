// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationKatRunnerTests.Pattern.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;
using Bodu.Text.Configuration.Test.Infrastructure;

namespace Bodu.Text.Configuration.Kat;

public partial class ConfigurationKatRunnerTests
{
    /// <summary>
    /// Verifies that a valid <see cref="ConfigurationKatKind.Pattern" /> KAT compiles and matches the target path
    /// with the expected result.
    /// </summary>
    /// <param name="kat">The KAT case to execute.</param>
    [TestMethod]
    [DynamicData(nameof(ConfigurationKnownAnswerData.PatternDataPass),
        typeof(ConfigurationKnownAnswerData),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Pattern_WhenValid_ShouldReturnExpectedMatch(ConfigurationKat kat)
    {
        var pattern = ConfigurationPattern.Compile(kat.Pattern!);
        bool match = pattern.IsMatch(kat.TargetPath!);

        Assert.AreEqual(
            kat.ExpectedMatch ?? false,
            match,
            $"{kat.Id}: pattern '{kat.Pattern}' against '{kat.TargetPath}' expected {kat.ExpectedMatch}.");
    }

    /// <summary>
    /// Verifies that an invalid <see cref="ConfigurationKatKind.Pattern" /> KAT throws the expected exception when
    /// compiled.
    /// </summary>
    /// <param name="kat">The KAT case to execute.</param>
    [TestMethod]
    [DynamicData(nameof(ConfigurationKnownAnswerData.PatternDataFail),
        typeof(ConfigurationKnownAnswerData),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Pattern_WhenInvalid_ShouldThrowExpectedException(ConfigurationKat kat)
    {
        if (kat.ExpectedException is null)
            Assert.Fail($"{kat.Id} is a fail KAT but has no ExpectedException.");

        AssertThrowsExactlyByName(kat.ExpectedException!, () =>
        {
            _ = ConfigurationPattern.Compile(kat.Pattern!);
        });
    }
}
