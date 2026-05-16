// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduConfigurationParseOptions.Presets.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Formats;

namespace Bodu.Text.Configuration;

public sealed partial class BoduConfigurationParseOptions
{
    /// <summary>
    /// Gets the canonical option set for the default Bodu profile: <see cref="BoduConfigurationInlineCommentMode.WhitespaceIntroduced" />
    /// inline comments, <see cref="IniDuplicateKeyBehavior.LastWins" /> duplicates,
    /// <see cref="IniDuplicateSectionBehavior.Preserve" /> sections, and throw-on-error diagnostics.
    /// </summary>
    /// <returns>A cached default options instance.</returns>
    public static BoduConfigurationParseOptions Bodu { get; } = For(BoduConfigurationProfile.Bodu);

    /// <summary>
    /// Gets the canonical option set for the EditorConfig-compatible profile: inline comments disabled,
    /// duplicates last-wins, preserve duplicate sections, and throw-on-error diagnostics.
    /// </summary>
    /// <returns>A cached EditorConfig-compatible options instance.</returns>
    public static BoduConfigurationParseOptions EditorConfigCompatible { get; } =
        For(BoduConfigurationProfile.EditorConfigCompatible);

    /// <summary>
    /// Gets the canonical option set for strict, deterministic parsing intended for generated files: inline
    /// comments disabled, duplicate keys rejected, duplicate sections rejected, throw-on-error.
    /// </summary>
    /// <returns>A cached strict options instance.</returns>
    public static BoduConfigurationParseOptions Strict { get; } = For(BoduConfigurationProfile.Strict);

    /// <summary>
    /// Gets the canonical option set for permissive parsing of user-authored files: inline comments enabled,
    /// duplicates last-wins, preserve duplicate sections, and collect-on-error diagnostics.
    /// </summary>
    /// <returns>A cached relaxed options instance.</returns>
    public static BoduConfigurationParseOptions Relaxed { get; } = For(BoduConfigurationProfile.Relaxed);

    /// <summary>
    /// Returns the canonical option set for the specified profile.
    /// </summary>
    /// <param name="profile">The profile to materialize.</param>
    /// <returns>An options instance configured for <paramref name="profile" />.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="profile" /> is not a defined value.</exception>
    public static BoduConfigurationParseOptions For(BoduConfigurationProfile profile)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(profile);

        return profile switch
        {
            BoduConfigurationProfile.Bodu => new BoduConfigurationParseOptions
            {
                Profile = BoduConfigurationProfile.Bodu,
                InlineCommentMode = BoduConfigurationInlineCommentMode.WhitespaceIntroduced,
                DuplicateKeyMode = IniDuplicateKeyBehavior.LastWins,
                DuplicateSectionMode = IniDuplicateSectionBehavior.Preserve,
                DiagnosticMode = BoduConfigurationDiagnosticMode.Throw,
                TrimKeysAndValues = true,
                AllowKeyOnlyProperties = false,
            },
            BoduConfigurationProfile.EditorConfigCompatible => new BoduConfigurationParseOptions
            {
                Profile = BoduConfigurationProfile.EditorConfigCompatible,
                InlineCommentMode = BoduConfigurationInlineCommentMode.Disabled,
                DuplicateKeyMode = IniDuplicateKeyBehavior.LastWins,
                DuplicateSectionMode = IniDuplicateSectionBehavior.Preserve,
                DiagnosticMode = BoduConfigurationDiagnosticMode.Throw,
                TrimKeysAndValues = true,
                AllowKeyOnlyProperties = false,
            },
            BoduConfigurationProfile.Strict => new BoduConfigurationParseOptions
            {
                Profile = BoduConfigurationProfile.Strict,
                InlineCommentMode = BoduConfigurationInlineCommentMode.Disabled,
                DuplicateKeyMode = IniDuplicateKeyBehavior.Disallowed,
                DuplicateSectionMode = IniDuplicateSectionBehavior.Disallowed,
                DiagnosticMode = BoduConfigurationDiagnosticMode.Throw,
                TrimKeysAndValues = true,
                AllowKeyOnlyProperties = false,
            },
            BoduConfigurationProfile.Relaxed => new BoduConfigurationParseOptions
            {
                Profile = BoduConfigurationProfile.Relaxed,
                InlineCommentMode = BoduConfigurationInlineCommentMode.WhitespaceIntroduced,
                DuplicateKeyMode = IniDuplicateKeyBehavior.LastWins,
                DuplicateSectionMode = IniDuplicateSectionBehavior.Preserve,
                DiagnosticMode = BoduConfigurationDiagnosticMode.Collect,
                TrimKeysAndValues = true,
                AllowKeyOnlyProperties = false,
            },
            _ => throw new ArgumentOutOfRangeException(nameof(profile)),
        };
    }
}
