// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PluginAlgorithmRegistrationOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Plugins;

/// <summary>
/// Controls how
/// <see cref="NotableDatePluginLoader.RegisterAlgorithms(INotableDatePlugin, Algorithms.NotableDateAlgorithmRegistry, PluginAlgorithmRegistrationOptions, Microsoft.Extensions.Logging.ILogger?)" />
/// treats a plugin's contributed algorithm keys.
/// </summary>
/// <remarks>
/// <para>
/// By default a contributed key that collides with a built-in algorithm key or with a key already present in the target
/// registry is rejected, so a plugin cannot silently take over an existing resolution path. Opting in to
/// <see cref="AllowOverride" /> permits the replacement; each override is logged at warning level so the takeover is
/// observable.
/// </para>
/// </remarks>
/// <seealso cref="NotableDatePluginLoader" />
public sealed class PluginAlgorithmRegistrationOptions
{
    /// <summary>
    /// Gets the default options: colliding keys are rejected.
    /// </summary>
    /// <value>The shared default options instance.</value>
    public static PluginAlgorithmRegistrationOptions Default { get; } = new();

    /// <summary>
    /// Gets a value indicating whether a contributed key may replace a built-in algorithm or an existing registration.
    /// </summary>
    /// <value>
    /// <see langword="true" /> to permit replacement (each override is logged at warning level); otherwise
    /// <see langword="false" /> to reject the collision. The default is <see langword="false" />.
    /// </value>
    public bool AllowOverride { get; init; }
}
