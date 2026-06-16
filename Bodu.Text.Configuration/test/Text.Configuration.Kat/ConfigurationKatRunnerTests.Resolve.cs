// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationKatRunnerTests.Resolve.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Configuration.Test.Infrastructure;
using Bodu.Text.Ini;

namespace Bodu.Text.Configuration.Kat;

public partial class ConfigurationKatRunnerTests
{
    /// <summary>
    /// Drives every <see cref="ConfigurationKatKind.Resolve" /> KAT in the catalogue.
    /// </summary>
    /// <param name="kat">The KAT case to execute.</param>
    [TestMethod]
    [DynamicData(nameof(ConfigurationKnownAnswerData.ResolutionData),
        typeof(ConfigurationKnownAnswerData),
        DynamicDataDisplayName = nameof(GetKatDisplayName))]
    public void Resolve_Kat(ConfigurationKat kat)
    {
        ConfigurationProfile profile = MapProfile(kat.Profile);
        var doc = ConfigurationDocument.Parse(kat.Source!, ConfigurationParseOptions.For(profile));
        ConfigurationResolveOptions resolveOptions = BuildResolveOptions(kat, profile);

        if (kat.Outcome is ConfigurationKatOutcome.Fail)
        {
            if (kat.ExpectedException is null)
                Assert.Fail($"{kat.Id} is a fail KAT but has no ExpectedException.");

            AssertThrowsExactlyByName(kat.ExpectedException!, () =>
            {
                _ = doc.Resolve(kat.TargetPath, resolveOptions);
            });

            return;
        }

        ConfigurationView view = doc.Resolve(kat.TargetPath, resolveOptions);

        foreach (ExpectedValue ev in kat.ExpectedValues)
        {
            Assert.AreEqual(ev.Value, view[ev.Key], $"{kat.Id}: expected value for '{ev.Key}'.");
        }

        foreach (string absent in kat.UnexpectedKeys)
        {
            Assert.IsNull(view[absent], $"{kat.Id}: '{absent}' should be absent from the resolved view.");
        }

        if (kat.Mutation is { } mutation)
        {
            IniSection section = doc.GetOrAddSection(mutation.Section);
            section.SetEntry(mutation.Key, mutation.Value);

            // The view is a snapshot, so it must remain unchanged.
            ExpectedValue before = kat.ExpectedValues[0];
            Assert.AreEqual(before.Value, view[before.Key], $"{kat.Id}: post-mutation view should be a snapshot.");
        }
    }

    private static ConfigurationResolveOptions BuildResolveOptions(ConfigurationKat kat, ConfigurationProfile profile)
    {
        var baseline = ConfigurationResolveOptions.For(profile);

        ConfigurationUnsetValueMode unsetMode = kat.Options switch
        {
            "UnsetRemovesEffectiveValue" => ConfigurationUnsetValueMode.RemoveEffectiveValue,
            "UnsetTreatAsLiteral" => ConfigurationUnsetValueMode.TreatAsLiteral,
            _ => baseline.UnsetValueMode,
        };

        ConfigurationMissingPathRootMode missingPathRootMode = kat.Options switch
        {
            "RequirePathRoot" => ConfigurationMissingPathRootMode.Throw,
            _ => baseline.MissingPathRootMode,
        };

        return new ConfigurationResolveOptions
        {
            Profile = profile,
            PathRoot = baseline.PathRoot,
            MissingPathRootMode = missingPathRootMode,
            ApplyPreambleProperties = baseline.ApplyPreambleProperties,
            PathComparison = baseline.PathComparison,
            UnsetValueMode = unsetMode,
            KeyOptions = baseline.KeyOptions,
        };
    }
}
