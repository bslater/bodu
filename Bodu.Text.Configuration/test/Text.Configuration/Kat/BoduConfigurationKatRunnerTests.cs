// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduConfigurationKatRunnerTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Configuration.Test.Infrastructure;

using Bodu.Text.Ini;

namespace Bodu.Text.Configuration.Kat;

/// <summary>
/// Drives the data-driven known-answer-test (KAT) catalogue defined in
/// <see cref="BoduConfigurationKnownAnswerData" />. Each <c>[DataTestMethod]</c> consumes one
/// <see cref="BoduConfigurationKatKind" />'s data source and dispatches to a kind-specific executor.
/// </summary>
[TestClass]
public partial class BoduConfigurationKatRunnerTests
{
    /// <summary>
    /// Translates the KAT profile string into a <see cref="BoduConfigurationProfile" />.
    /// </summary>
    private static BoduConfigurationProfile MapProfile(string profile) =>
        profile switch
        {
            "Bodu" => BoduConfigurationProfile.Bodu,
            "EditorConfigCompatible" => BoduConfigurationProfile.EditorConfigCompatible,
            "Strict" => BoduConfigurationProfile.Strict,
            "Relaxed" => BoduConfigurationProfile.Relaxed,
            _ => throw new ArgumentOutOfRangeException(nameof(profile), profile, "Unknown KAT profile."),
        };

    /// <summary>
    /// Translates the KAT duplicate-key-mode string into a <see cref="IniDuplicateKeyBehavior" />.
    /// </summary>
    private static IniDuplicateKeyBehavior MapDuplicateKeyMode(string? mode) =>
        mode switch
        {
            null => IniDuplicateKeyBehavior.LastWins,
            "LastWins" => IniDuplicateKeyBehavior.LastWins,
            "FirstWins" => IniDuplicateKeyBehavior.FirstWins,
            "Disallowed" => IniDuplicateKeyBehavior.Disallowed,
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown KAT duplicate-key mode."),
        };

    /// <summary>
    /// Translates the KAT diagnostic-mode string into a <see cref="BoduConfigurationDiagnosticMode" />.
    /// </summary>
    private static BoduConfigurationDiagnosticMode MapDiagnosticMode(string? mode) =>
        mode switch
        {
            null => BoduConfigurationDiagnosticMode.Throw,
            "Throw" => BoduConfigurationDiagnosticMode.Throw,
            "Collect" => BoduConfigurationDiagnosticMode.Collect,
            "Ignore" => BoduConfigurationDiagnosticMode.Ignore,
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown KAT diagnostic mode."),
        };

    /// <summary>
    /// Builds a <see cref="BoduConfigurationParseOptions" /> instance from the KAT's profile, duplicate-key
    /// mode, diagnostic mode, and free-form option string.
    /// </summary>
    private static BoduConfigurationParseOptions BuildParseOptions(BoduConfigurationKat kat)
    {
        BoduConfigurationProfile profile = MapProfile(kat.Profile);
        var baseline = BoduConfigurationParseOptions.For(profile);

        var allowKeyOnly = kat.Options is "AllowKeyOnlyProperties" || baseline.AllowKeyOnlyProperties;

        return new BoduConfigurationParseOptions
        {
            Profile = profile,
            InlineCommentMode = baseline.InlineCommentMode,
            DuplicateKeyMode = kat.DuplicateKeyMode is null ? baseline.DuplicateKeyMode : MapDuplicateKeyMode(kat.DuplicateKeyMode),
            DuplicateSectionMode = baseline.DuplicateSectionMode,
            DiagnosticMode = kat.DiagnosticMode is null ? baseline.DiagnosticMode : MapDiagnosticMode(kat.DiagnosticMode),
            MaxLineLength = baseline.MaxLineLength,
            MaxKeyLength = baseline.MaxKeyLength,
            KeyOptions = baseline.KeyOptions,
            TrimKeysAndValues = baseline.TrimKeysAndValues,
            AllowKeyOnlyProperties = allowKeyOnly,
            DefaultEncoding = baseline.DefaultEncoding,
        };
    }

    /// <summary>
    /// Asserts that <paramref name="action" /> throws an exception whose simple type name matches
    /// <paramref name="expectedException" /> exactly.
    /// </summary>
    private static Exception AssertThrowsExactlyByName(string expectedException, Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            Assert.AreEqual(
                expectedException,
                ex.GetType().Name,
                $"Expected an exception of type '{expectedException}' but got '{ex.GetType().Name}': {ex.Message}");
            return ex;
        }

        Assert.Fail($"Expected an exception of type '{expectedException}' but none was thrown.");
        return null!;
    }
}
