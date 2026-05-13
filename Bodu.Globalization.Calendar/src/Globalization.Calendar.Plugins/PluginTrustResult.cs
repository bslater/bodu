// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PluginTrustResult.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Plugins;

/// <summary>
/// Represents the outcome of an <see cref="IPluginTrustPolicy" /> evaluation.
/// </summary>
/// <param name="Trusted"><see langword="true" /> when the plugin is trusted and may be loaded; otherwise <see langword="false" />.</param>
/// <param name="Reason">Optional human-readable reason. When <paramref name="Trusted" /> is <see langword="false" /> the loader surfaces this reason on the resulting <see cref="PluginNotTrustedException" />.</param>
public readonly record struct PluginTrustResult(
    bool Trusted,
    string? Reason);
