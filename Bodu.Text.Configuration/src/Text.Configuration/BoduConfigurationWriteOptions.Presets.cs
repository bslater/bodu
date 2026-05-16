// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduConfigurationWriteOptions.Presets.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Configuration;

public sealed partial class BoduConfigurationWriteOptions
{
    /// <summary>
    /// Gets the canonical option set for the default Bodu profile.
    /// </summary>
    /// <returns>A cached default options instance.</returns>
    public static BoduConfigurationWriteOptions Bodu { get; } = For(BoduConfigurationProfile.Bodu);

    /// <summary>
    /// Gets the canonical option set for the EditorConfig-compatible profile: inline comments suppressed.
    /// </summary>
    /// <returns>A cached EditorConfig-compatible options instance.</returns>
    public static BoduConfigurationWriteOptions EditorConfigCompatible { get; } =
        For(BoduConfigurationProfile.EditorConfigCompatible);

    /// <summary>
    /// Gets the canonical option set for normalized output: sorted properties, deterministic spacing.
    /// </summary>
    /// <returns>A cached normalized options instance.</returns>
    public static BoduConfigurationWriteOptions Normalized { get; } = For(BoduConfigurationProfile.Strict);

    /// <summary>
    /// Returns the canonical option set for the specified profile.
    /// </summary>
    /// <param name="profile">The profile to materialize.</param>
    /// <returns>An options instance configured for <paramref name="profile" />.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="profile" /> is not a defined value.</exception>
    public static BoduConfigurationWriteOptions For(BoduConfigurationProfile profile)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(profile);

        return profile switch
        {
            BoduConfigurationProfile.Bodu => new BoduConfigurationWriteOptions
            {
                Profile = BoduConfigurationProfile.Bodu,
                WriteInlineComments = true,
                PreserveComments = true,
                InsertBlankLineBetweenSections = true,
            },
            BoduConfigurationProfile.EditorConfigCompatible => new BoduConfigurationWriteOptions
            {
                Profile = BoduConfigurationProfile.EditorConfigCompatible,
                WriteInlineComments = false,
                PreserveComments = true,
                InsertBlankLineBetweenSections = true,
            },
            BoduConfigurationProfile.Strict => new BoduConfigurationWriteOptions
            {
                Profile = BoduConfigurationProfile.Strict,
                WriteInlineComments = false,
                PreserveComments = false,
                InsertBlankLineBetweenSections = true,
            },
            BoduConfigurationProfile.Relaxed => new BoduConfigurationWriteOptions
            {
                Profile = BoduConfigurationProfile.Relaxed,
                WriteInlineComments = true,
                PreserveComments = true,
                InsertBlankLineBetweenSections = true,
            },
            _ => throw new ArgumentOutOfRangeException(nameof(profile)),
        };
    }
}
