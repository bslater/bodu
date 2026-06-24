// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationWriteOptions.Presets.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Configuration;

public sealed partial class ConfigurationWriteOptions
{
    /// <summary>
    /// Gets the canonical option set for the default Bodu profile.
    /// </summary>
    /// <value>A cached default options instance.</value>
    public static ConfigurationWriteOptions Bodu { get; } = For(ConfigurationProfile.Bodu);

    /// <summary>
    /// Gets the canonical option set for the EditorConfig-compatible profile: inline comments suppressed.
    /// </summary>
    /// <value>A cached EditorConfig-compatible options instance.</value>
    public static ConfigurationWriteOptions EditorConfigCompatible { get; } =
        For(ConfigurationProfile.EditorConfigCompatible);

    /// <summary>
    /// Gets the canonical option set for normalized output: sorted properties, deterministic spacing.
    /// </summary>
    /// <value>A cached normalized options instance.</value>
    public static ConfigurationWriteOptions Normalized { get; } = For(ConfigurationProfile.Strict);

    /// <summary>
    /// Returns the canonical option set for the specified profile.
    /// </summary>
    /// <param name="profile">The profile to materialize.</param>
    /// <returns>An options instance configured for <paramref name="profile" />.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="profile" /> is not a defined value.</exception>
    public static ConfigurationWriteOptions For(ConfigurationProfile profile)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(profile);

        return profile switch
        {
            ConfigurationProfile.Bodu => new ConfigurationWriteOptions
            {
                Profile = ConfigurationProfile.Bodu,
                WriteInlineComments = true,
                PreserveComments = true,
                InsertBlankLineBetweenSections = true,
            },
            ConfigurationProfile.EditorConfigCompatible => new ConfigurationWriteOptions
            {
                Profile = ConfigurationProfile.EditorConfigCompatible,
                WriteInlineComments = false,
                PreserveComments = true,
                InsertBlankLineBetweenSections = true,
            },
            ConfigurationProfile.Strict => new ConfigurationWriteOptions
            {
                Profile = ConfigurationProfile.Strict,
                WriteInlineComments = false,
                PreserveComments = false,
                InsertBlankLineBetweenSections = true,
            },
            ConfigurationProfile.Relaxed => new ConfigurationWriteOptions
            {
                Profile = ConfigurationProfile.Relaxed,
                WriteInlineComments = true,
                PreserveComments = true,
                InsertBlankLineBetweenSections = true,
            },
            _ => throw new ArgumentOutOfRangeException(nameof(profile)),
        };
    }
}
