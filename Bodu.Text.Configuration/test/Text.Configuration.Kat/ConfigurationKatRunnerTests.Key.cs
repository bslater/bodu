// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationKatRunnerTests.Key.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Configuration.Test.Infrastructure;

namespace Bodu.Text.Configuration.Kat;

public partial class ConfigurationKatRunnerTests
{
    /// <summary>
    /// Drives every <see cref="ConfigurationKatKind.KeyMapping" /> KAT in the catalogue.
    /// </summary>
    /// <param name="kat">The KAT case to execute.</param>
    [TestMethod]
    [DynamicData(nameof(ConfigurationKnownAnswerData.KeyData),
        typeof(ConfigurationKnownAnswerData),
        DynamicDataDisplayName = nameof(GetKatDisplayName))]
    public void Key_Kat(ConfigurationKat kat)
    {
        ConfigurationKeyOptions options = BuildKeyOptions(kat.Options);

        if (kat.Outcome is ConfigurationKatOutcome.Fail)
        {
            ExecuteKeyFail(kat, options);
            return;
        }

        ExecuteKeyPass(kat, options);
    }

    private static void ExecuteKeyPass(ConfigurationKat kat, ConfigurationKeyOptions options)
    {
        var key = ConfigurationKey.Parse(kat.Key!, options);

        Assert.AreEqual(kat.ExpectedConfigurationKey, key.Path, $"{kat.Id}: configuration key");
        CollectionAssert.AreEqual(
            kat.ExpectedSegments.ToArray(),
            key.Segments.ToArray(),
            $"{kat.Id}: segments");
    }

    private static void ExecuteKeyFail(ConfigurationKat kat, ConfigurationKeyOptions options)
    {
        if (kat.ExpectedException is null)
            Assert.Fail($"{kat.Id} is a fail KAT but has no ExpectedException.");

        AssertThrowsExactlyByName(kat.ExpectedException!, () =>
        {
            _ = ConfigurationKey.Parse(kat.Key!, options);
        });
    }

    private static ConfigurationKeyOptions BuildKeyOptions(string? raw)
    {
        if (raw is null)
            return ConfigurationKeyOptions.Default;

        // Encoded as "Mapping;CaseSensitive=bool" or just "Mapping".
        var parts = raw.Split(';');
        ConfigurationKeyMapping mapping = parts[0] switch
        {
            "DotToColon" => ConfigurationKeyMapping.DotToColon,
            "Colon" => ConfigurationKeyMapping.Colon,
            "Identity" => ConfigurationKeyMapping.Identity,
            _ => ConfigurationKeyMapping.DotToColon,
        };

        var caseSensitive = false;
        foreach (var part in parts.Skip(1))
        {
            if (part.StartsWith("CaseSensitive=", StringComparison.Ordinal))
                caseSensitive = bool.Parse(part["CaseSensitive=".Length..]);
        }

        return new ConfigurationKeyOptions
        {
            Mapping = mapping,
            CaseSensitive = caseSensitive,
        };
    }
}
