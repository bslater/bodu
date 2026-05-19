// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationKatRunnerTests.Pattern.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Configuration.Test.Infrastructure;

namespace Bodu.Text.Configuration.Kat;

public partial class ConfigurationKatRunnerTests
{
    /// <summary>
    /// Drives every <see cref="ConfigurationKatKind.Pattern" /> KAT in the catalogue.
    /// </summary>
    /// <param name="kat">The KAT case to execute.</param>
    [TestMethod]
    [DynamicData(nameof(ConfigurationKnownAnswerData.PatternData),
        typeof(ConfigurationKnownAnswerData),
        DynamicDataDisplayName = nameof(GetKatDisplayName))]
    public void Pattern_Kat(ConfigurationKat kat)
    {
        if (kat.Outcome is ConfigurationKatOutcome.Fail)
        {
            if (kat.ExpectedException is null)
                Assert.Fail($"{kat.Id} is a fail KAT but has no ExpectedException.");

            AssertThrowsExactlyByName(kat.ExpectedException!, () =>
            {
                _ = ConfigurationPattern.Compile(kat.Pattern!);
            });

            return;
        }

        var pattern = ConfigurationPattern.Compile(kat.Pattern!);
        var match = pattern.IsMatch(kat.TargetPath!);

        Assert.AreEqual(
            kat.ExpectedMatch ?? false,
            match,
            $"{kat.Id}: pattern '{kat.Pattern}' against '{kat.TargetPath}' expected {kat.ExpectedMatch}.");
    }
}
