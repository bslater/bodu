// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduConfigurationKatRunnerTests.Resolve.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Configuration.Test.Infrastructure;

namespace Bodu.Text.Configuration.Kat;

public partial class BoduConfigurationKatRunnerTests
{
    /// <summary>
    /// Drives every <see cref="BoduConfigurationKatKind.Resolve" /> KAT in the catalogue.
    /// </summary>
    /// <param name="kat">The KAT case to execute.</param>
    [DataTestMethod]
    [DynamicData(nameof(BoduConfigurationKnownAnswerData.ResolutionData),
        typeof(BoduConfigurationKnownAnswerData),
        DynamicDataSourceType.Property,
        DynamicDataDisplayName = nameof(GetKatDisplayName))]
    public void Resolve_Kat(BoduConfigurationKat kat)
    {
        BoduConfigurationProfile profile = MapProfile(kat.Profile);
        BoduConfigurationDocument doc = BoduConfigurationDocument.Parse(kat.Source!, BoduConfigurationParseOptions.For(profile));
        BoduConfigurationResolveOptions resolveOptions = BuildResolveOptions(kat, profile);

        if (kat.Outcome is BoduConfigurationKatOutcome.Fail)
        {
            if (kat.ExpectedException is null)
                Assert.Fail($"{kat.Id} is a fail KAT but has no ExpectedException.");

            AssertThrowsExactlyByName(kat.ExpectedException!, () =>
            {
                _ = doc.Resolve(kat.TargetPath, resolveOptions);
            });

            return;
        }

        BoduConfigurationView view = doc.Resolve(kat.TargetPath, resolveOptions);

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
            BoduConfigurationSection section = doc.GetOrAddSection(mutation.Section);
            section.Set(mutation.Key, mutation.Value);

            // The view is a snapshot, so it must remain unchanged.
            ExpectedValue before = kat.ExpectedValues[0];
            Assert.AreEqual(before.Value, view[before.Key], $"{kat.Id}: post-mutation view should be a snapshot.");
        }
    }

    private static BoduConfigurationResolveOptions BuildResolveOptions(BoduConfigurationKat kat, BoduConfigurationProfile profile)
    {
        BoduConfigurationResolveOptions baseline = BoduConfigurationResolveOptions.For(profile);

        BoduConfigurationUnsetValueMode unsetMode = kat.Options switch
        {
            "UnsetRemovesEffectiveValue" => BoduConfigurationUnsetValueMode.RemoveEffectiveValue,
            "UnsetTreatAsLiteral" => BoduConfigurationUnsetValueMode.TreatAsLiteral,
            _ => baseline.UnsetValueMode,
        };

        BoduConfigurationMissingPathRootMode missingPathRootMode = kat.Options switch
        {
            "RequirePathRoot" => BoduConfigurationMissingPathRootMode.Throw,
            _ => baseline.MissingPathRootMode,
        };

        return new BoduConfigurationResolveOptions
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
