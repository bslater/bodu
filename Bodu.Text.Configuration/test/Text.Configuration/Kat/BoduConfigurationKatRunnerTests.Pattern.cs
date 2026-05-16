// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduConfigurationKatRunnerTests.Pattern.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Configuration.Test.Infrastructure;

namespace Bodu.Text.Configuration.Kat;

public partial class BoduConfigurationKatRunnerTests
{
    /// <summary>
    /// Drives every <see cref="BoduConfigurationKatKind.Pattern" /> KAT in the catalogue.
    /// </summary>
    /// <param name="kat">The KAT case to execute.</param>
    [DataTestMethod]
    [DynamicData(nameof(BoduConfigurationKnownAnswerData.PatternData),
        typeof(BoduConfigurationKnownAnswerData),
        DynamicDataSourceType.Property,
        DynamicDataDisplayName = nameof(GetKatDisplayName))]
    public void Pattern_Kat(BoduConfigurationKat kat)
    {
        if (kat.Outcome is BoduConfigurationKatOutcome.Fail)
        {
            if (kat.ExpectedException is null)
                Assert.Fail($"{kat.Id} is a fail KAT but has no ExpectedException.");

            AssertThrowsExactlyByName(kat.ExpectedException!, () =>
            {
                _ = BoduConfigurationPattern.Compile(kat.Pattern!);
            });

            return;
        }

        BoduConfigurationPattern pattern = BoduConfigurationPattern.Compile(kat.Pattern!);
        bool match = pattern.IsMatch(kat.TargetPath!);

        Assert.AreEqual(
            kat.ExpectedMatch ?? false,
            match,
            $"{kat.Id}: pattern '{kat.Pattern}' against '{kat.TargetPath}' expected {kat.ExpectedMatch}.");
    }
}
