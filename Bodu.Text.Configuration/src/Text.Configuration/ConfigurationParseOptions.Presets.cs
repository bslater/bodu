// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationParseOptions.Presets.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Ini;

namespace Bodu.Text.Configuration;

public sealed partial class ConfigurationParseOptions
{
    /// <summary>
    /// Gets the canonical option set for the default Bodu profile:
    /// <see cref="ConfigurationInlineCommentMode.WhitespaceIntroduced" /> inline comments,
    /// <see cref="IniDuplicateKeyBehavior.LastWins" /> duplicates, <see cref="IniDuplicateSectionBehavior.Preserve" />
    /// sections, and throw-on-error diagnostics.
    /// </summary>
    /// <returns>A cached default options instance.</returns>
    public static ConfigurationParseOptions Bodu { get; } = For(ConfigurationProfile.Bodu);

    /// <summary>
    /// Gets the canonical option set for the EditorConfig-compatible profile: inline comments disabled, duplicates
    /// last-wins, preserve duplicate sections, and throw-on-error diagnostics.
    /// </summary>
    /// <returns>A cached EditorConfig-compatible options instance.</returns>
    public static ConfigurationParseOptions EditorConfigCompatible { get; } =
        For(ConfigurationProfile.EditorConfigCompatible);

    /// <summary>
    /// Gets the canonical option set for strict, deterministic parsing intended for generated files: inline comments
    /// disabled, duplicate keys rejected, duplicate sections rejected, throw-on-error.
    /// </summary>
    /// <returns>A cached strict options instance.</returns>
    public static ConfigurationParseOptions Strict { get; } = For(ConfigurationProfile.Strict);

    /// <summary>
    /// Gets the canonical option set for permissive parsing of user-authored files: inline comments enabled, duplicates
    /// last-wins, preserve duplicate sections, and collect-on-error diagnostics.
    /// </summary>
    /// <returns>A cached relaxed options instance.</returns>
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
                DuplicateKeyMode = IniDuplicateKeyBehavior.LastWins,
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
                DuplicateKeyMode = IniDuplicateKeyBehavior.LastWins,
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
                DuplicateKeyMode = IniDuplicateKeyBehavior.Disallowed,
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
                DuplicateKeyMode = IniDuplicateKeyBehavior.LastWins,
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
