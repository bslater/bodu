// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationParseOptions.Presets.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Configuration;

public sealed partial class ConfigurationParseOptions
{
    /// <summary>
    /// Gets the canonical option set for the default Bodu profile:
    /// <see cref="ConfigurationInlineCommentMode.WhitespaceIntroduced" /> inline comments,
    /// <see cref="DuplicateKeyPolicy.LastWins" /> duplicates, <see cref="IniDuplicateSectionBehavior.Preserve" />
    /// sections, and throw-on-error diagnostics.
    /// </summary>
    /// <value>A cached default options instance.</value>
    public static ConfigurationParseOptions Bodu { get; } = For(ConfigurationProfile.Bodu);

    /// <summary>
    /// Gets the canonical option set for the EditorConfig-compatible profile: inline comments disabled, duplicates
    /// last-wins, preserve duplicate sections, and throw-on-error diagnostics.
    /// </summary>
    /// <value>A cached EditorConfig-compatible options instance.</value>
    public static ConfigurationParseOptions EditorConfigCompatible { get; } =
        For(ConfigurationProfile.EditorConfigCompatible);

    /// <summary>
    /// Gets the canonical option set for strict, deterministic parsing intended for generated files: inline comments
    /// disabled, duplicate keys rejected, duplicate sections rejected, throw-on-error.
    /// </summary>
    /// <value>A cached strict options instance.</value>
    public static ConfigurationParseOptions Strict { get; } = For(ConfigurationProfile.Strict);

    /// <summary>
    /// Gets the canonical option set for permissive parsing of user-authored files: inline comments enabled, duplicates
    /// last-wins, preserve duplicate sections, and collect-on-error diagnostics.
    /// </summary>
    /// <value>A cached relaxed options instance.</value>
    public static ConfigurationParseOptions Relaxed { get; } = For(ConfigurationProfile.Relaxed);

    /// <summary>
    /// Returns the canonical option set for the specified profile.
    /// </summary>
    /// <param name="profile">The profile to materialize.</param>
    /// <returns>An options instance configured for <paramref name="profile" />.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="profile" /> is not a defined value.</exception>
    public static ConfigurationParseOptions For(ConfigurationProfile profile)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(profile);

        return profile switch
        {
            ConfigurationProfile.Bodu => new ConfigurationParseOptions
            {
                Profile = ConfigurationProfile.Bodu,
                InlineCommentMode = ConfigurationInlineCommentMode.WhitespaceIntroduced,
                DuplicateKeyMode = DuplicateKeyPolicy.LastWins,
                DuplicateSectionMode = IniDuplicateSectionBehavior.Preserve,
                SectionHeaderMode = ConfigurationSectionHeaderMode.Lenient,
                DiagnosticMode = ConfigurationDiagnosticMode.Throw,
                TrimKeysAndValues = true,
                AllowKeyOnlyProperties = false,
            },
            ConfigurationProfile.EditorConfigCompatible => new ConfigurationParseOptions
            {
                Profile = ConfigurationProfile.EditorConfigCompatible,
                InlineCommentMode = ConfigurationInlineCommentMode.Disabled,
                DuplicateKeyMode = DuplicateKeyPolicy.LastWins,
                DuplicateSectionMode = IniDuplicateSectionBehavior.Preserve,
                SectionHeaderMode = ConfigurationSectionHeaderMode.Strict,
                DiagnosticMode = ConfigurationDiagnosticMode.Throw,
                TrimKeysAndValues = true,
                AllowKeyOnlyProperties = false,
            },
            ConfigurationProfile.Strict => new ConfigurationParseOptions
            {
                Profile = ConfigurationProfile.Strict,
                InlineCommentMode = ConfigurationInlineCommentMode.Disabled,
                DuplicateKeyMode = DuplicateKeyPolicy.Disallowed,
                DuplicateSectionMode = IniDuplicateSectionBehavior.Disallowed,
                SectionHeaderMode = ConfigurationSectionHeaderMode.Strict,
                DiagnosticMode = ConfigurationDiagnosticMode.Throw,
                TrimKeysAndValues = true,
                AllowKeyOnlyProperties = false,
            },
            ConfigurationProfile.Relaxed => new ConfigurationParseOptions
            {
                Profile = ConfigurationProfile.Relaxed,
                InlineCommentMode = ConfigurationInlineCommentMode.WhitespaceIntroduced,
                DuplicateKeyMode = DuplicateKeyPolicy.LastWins,
                DuplicateSectionMode = IniDuplicateSectionBehavior.Preserve,
                SectionHeaderMode = ConfigurationSectionHeaderMode.Lenient,
                DiagnosticMode = ConfigurationDiagnosticMode.Collect,
                TrimKeysAndValues = true,
                AllowKeyOnlyProperties = false,
            },
            _ => throw new ArgumentOutOfRangeException(nameof(profile)),
        };
    }
}
