// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduConfigurationKatRunnerTests.Key.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Configuration.Test.Infrastructure;

namespace Bodu.Text.Configuration.Kat;

public partial class BoduConfigurationKatRunnerTests
{
    /// <summary>
    /// Drives every <see cref="BoduConfigurationKatKind.KeyMapping" /> KAT in the catalogue.
    /// </summary>
    /// <param name="kat">The KAT case to execute.</param>
    [TestMethod]
    [DynamicData(nameof(BoduConfigurationKnownAnswerData.KeyData),
        typeof(BoduConfigurationKnownAnswerData),
        DynamicDataDisplayName = nameof(GetKatDisplayName))]
    public void Key_Kat(BoduConfigurationKat kat)
    {
        BoduConfigurationKeyOptions options = BuildKeyOptions(kat.Options);

        if (kat.Outcome is BoduConfigurationKatOutcome.Fail)
        {
            ExecuteKeyFail(kat, options);
            return;
        }

        ExecuteKeyPass(kat, options);
    }

    private static void ExecuteKeyPass(BoduConfigurationKat kat, BoduConfigurationKeyOptions options)
    {
        var key = BoduConfigurationKey.Parse(kat.Key!, options);

        Assert.AreEqual(kat.ExpectedConfigurationKey, key.ConfigurationKey, $"{kat.Id}: configuration key");
        CollectionAssert.AreEqual(
            kat.ExpectedSegments.ToArray(),
            key.Segments.ToArray(),
            $"{kat.Id}: segments");
    }

    private static void ExecuteKeyFail(BoduConfigurationKat kat, BoduConfigurationKeyOptions options)
    {
        if (kat.ExpectedException is null)
            Assert.Fail($"{kat.Id} is a fail KAT but has no ExpectedException.");

        AssertThrowsExactlyByName(kat.ExpectedException!, () =>
        {
            _ = BoduConfigurationKey.Parse(kat.Key!, options);
        });
    }

    private static BoduConfigurationKeyOptions BuildKeyOptions(string? raw)
    {
        if (raw is null)
            return BoduConfigurationKeyOptions.Default;

        // Encoded as "Mapping;CaseSensitive=bool" or just "Mapping".
        var parts = raw.Split(';');
        BoduConfigurationKeyMapping mapping = parts[0] switch
        {
            "DotToColon" => BoduConfigurationKeyMapping.DotToColon,
            "Colon" => BoduConfigurationKeyMapping.Colon,
            "Identity" => BoduConfigurationKeyMapping.Identity,
            _ => BoduConfigurationKeyMapping.DotToColon,
        };

        var caseSensitive = false;
        foreach (var part in parts.Skip(1))
        {
            if (part.StartsWith("CaseSensitive=", StringComparison.Ordinal))
                caseSensitive = bool.Parse(part["CaseSensitive=".Length..]);
        }

        return new BoduConfigurationKeyOptions
        {
            Mapping = mapping,
            CaseSensitive = caseSensitive,
        };
    }
}
