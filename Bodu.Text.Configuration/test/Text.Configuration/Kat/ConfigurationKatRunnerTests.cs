// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationKatRunnerTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Configuration.Test.Infrastructure;

using Bodu.Text.Ini;

namespace Bodu.Text.Configuration.Kat;

/// <summary>
/// Drives the data-driven known-answer-test (KAT) catalogue defined in
/// <see cref="ConfigurationKnownAnswerData" />. Each <c>[DataTestMethod]</c> consumes one
/// <see cref="ConfigurationKatKind" />'s data source and dispatches to a kind-specific executor.
/// </summary>
[TestClass]
public partial class ConfigurationKatRunnerTests
{
    /// <summary>
    /// Translates the KAT profile string into a <see cref="ConfigurationProfile" />.
    /// </summary>
    private static ConfigurationProfile MapProfile(string profile) =>
        profile switch
        {
            "Bodu" => ConfigurationProfile.Bodu,
            "EditorConfigCompatible" => ConfigurationProfile.EditorConfigCompatible,
            "Strict" => ConfigurationProfile.Strict,
            "Relaxed" => ConfigurationProfile.Relaxed,
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
    /// Translates the KAT diagnostic-mode string into a <see cref="ConfigurationDiagnosticMode" />.
    /// </summary>
    private static ConfigurationDiagnosticMode MapDiagnosticMode(string? mode) =>
        mode switch
        {
            null => ConfigurationDiagnosticMode.Throw,
            "Throw" => ConfigurationDiagnosticMode.Throw,
            "Collect" => ConfigurationDiagnosticMode.Collect,
            "Ignore" => ConfigurationDiagnosticMode.Ignore,
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown KAT diagnostic mode."),
        };

    /// <summary>
    /// Builds a <see cref="ConfigurationParseOptions" /> instance from the KAT's profile, duplicate-key
    /// mode, diagnostic mode, and free-form option string.
    /// </summary>
    private static ConfigurationParseOptions BuildParseOptions(ConfigurationKat kat)
    {
        ConfigurationProfile profile = MapProfile(kat.Profile);
        var baseline = ConfigurationParseOptions.For(profile);

        var allowKeyOnly = kat.Options is "AllowKeyOnlyProperties" || baseline.AllowKeyOnlyProperties;

        return new ConfigurationParseOptions
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
