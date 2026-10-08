// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationResolveOptions.Presets.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Configuration;

public sealed partial class ConfigurationResolveOptions
{
    /// <summary>
    /// Gets the canonical option set for the default Bodu profile.
    /// </summary>
    /// <value>A cached default options instance.</value>
    public static ConfigurationResolveOptions Bodu { get; } = For(ConfigurationProfile.Bodu);

    /// <summary>
    /// Gets the canonical option set for the EditorConfig-compatible profile: preamble properties left out, the
    /// <c>unset</c> sentinel removing a value, a path root or target path required, and the
    /// <see cref="ConfigurationKeyMapping.Identity" /> key mapping with keys lowercased, so a key keeps its dots and
    /// reads as EditorConfig lowercases it.
    /// </summary>
    /// <value>A cached EditorConfig-compatible options instance.</value>
    public static ConfigurationResolveOptions EditorConfigCompatible { get; } =
        For(ConfigurationProfile.EditorConfigCompatible);

    /// <summary>
    /// Returns the canonical option set for the specified profile.
    /// </summary>
    /// <param name="profile">The profile to materialize.</param>
    /// <returns>An options instance configured for <paramref name="profile" />.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="profile" /> is not a defined value.</exception>
    public static ConfigurationResolveOptions For(ConfigurationProfile profile)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(profile);

        return profile switch
        {
            ConfigurationProfile.Bodu => new ConfigurationResolveOptions
            {
                Profile = ConfigurationProfile.Bodu,
                ApplyPreambleProperties = true,
                MissingPathRootMode = ConfigurationMissingPathRootMode.UseEmptyRoot,
                UnsetValueMode = ConfigurationUnsetValueMode.TreatAsLiteral,
            },
            ConfigurationProfile.EditorConfigCompatible => new ConfigurationResolveOptions
            {
                Profile = ConfigurationProfile.EditorConfigCompatible,
                KeyOptions = ConfigurationKeyOptions.EditorConfigCompatible,
                ApplyPreambleProperties = false,
                MissingPathRootMode = ConfigurationMissingPathRootMode.Throw,
                UnsetValueMode = ConfigurationUnsetValueMode.RemoveEffectiveValue,
            },
            ConfigurationProfile.Strict => new ConfigurationResolveOptions
            {
                Profile = ConfigurationProfile.Strict,
                ApplyPreambleProperties = true,
                MissingPathRootMode = ConfigurationMissingPathRootMode.Throw,
                UnsetValueMode = ConfigurationUnsetValueMode.RemoveEffectiveValue,
            },
            ConfigurationProfile.Relaxed => new ConfigurationResolveOptions
            {
                Profile = ConfigurationProfile.Relaxed,
                ApplyPreambleProperties = true,
                MissingPathRootMode = ConfigurationMissingPathRootMode.UseEmptyRoot,
                UnsetValueMode = ConfigurationUnsetValueMode.TreatAsLiteral,
            },
            _ => throw new ArgumentOutOfRangeException(nameof(profile)),
        };
    }
}
