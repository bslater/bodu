// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduConfigurationResolveOptions.Presets.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Configuration;

public sealed partial class BoduConfigurationResolveOptions
{
    /// <summary>
    /// Gets the canonical option set for the default Bodu profile.
    /// </summary>
    /// <returns>A cached default options instance.</returns>
    public static BoduConfigurationResolveOptions Bodu { get; } = For(BoduConfigurationProfile.Bodu);

    /// <summary>
    /// Gets the canonical option set for the EditorConfig-compatible profile.
    /// </summary>
    /// <returns>A cached EditorConfig-compatible options instance.</returns>
    public static BoduConfigurationResolveOptions EditorConfigCompatible { get; } =
        For(BoduConfigurationProfile.EditorConfigCompatible);

    /// <summary>
    /// Returns the canonical option set for the specified profile.
    /// </summary>
    /// <param name="profile">The profile to materialize.</param>
    /// <returns>An options instance configured for <paramref name="profile" />.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="profile" /> is not a defined value.</exception>
    public static BoduConfigurationResolveOptions For(BoduConfigurationProfile profile)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(profile);

        return profile switch
        {
            BoduConfigurationProfile.Bodu => new BoduConfigurationResolveOptions
            {
                Profile = BoduConfigurationProfile.Bodu,
                ApplyPreambleProperties = true,
                MissingPathRootMode = BoduConfigurationMissingPathRootMode.UseEmptyRoot,
                UnsetValueMode = BoduConfigurationUnsetValueMode.TreatAsLiteral,
            },
            BoduConfigurationProfile.EditorConfigCompatible => new BoduConfigurationResolveOptions
            {
                Profile = BoduConfigurationProfile.EditorConfigCompatible,
                ApplyPreambleProperties = false,
                MissingPathRootMode = BoduConfigurationMissingPathRootMode.Throw,
                UnsetValueMode = BoduConfigurationUnsetValueMode.RemoveEffectiveValue,
            },
            BoduConfigurationProfile.Strict => new BoduConfigurationResolveOptions
            {
                Profile = BoduConfigurationProfile.Strict,
                ApplyPreambleProperties = true,
                MissingPathRootMode = BoduConfigurationMissingPathRootMode.Throw,
                UnsetValueMode = BoduConfigurationUnsetValueMode.RemoveEffectiveValue,
            },
            BoduConfigurationProfile.Relaxed => new BoduConfigurationResolveOptions
            {
                Profile = BoduConfigurationProfile.Relaxed,
                ApplyPreambleProperties = true,
                MissingPathRootMode = BoduConfigurationMissingPathRootMode.UseEmptyRoot,
                UnsetValueMode = BoduConfigurationUnsetValueMode.TreatAsLiteral,
            },
            _ => throw new ArgumentOutOfRangeException(nameof(profile)),
        };
    }
}
