// ---------------------------------------------------------------------------------------------------------------
// <copyright file="INotableDatePlugin.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Plugins;

/// <summary>
/// The base contract implemented by every notable-date plugin, exposing the identity a host uses for diagnostics,
/// logging, and version reporting.
/// </summary>
/// <remarks>
/// <para>
/// This is the marker every plugin shares; capability-specific contracts derive from it — for example
/// <see cref="INotableDateAlgorithmPlugin" />, which contributes custom algorithms. A host activates the plugin through
/// <see cref="NotableDatePluginLoader" /> and reads <see cref="Name" /> and <see cref="Version" /> purely for reporting;
/// neither participates in trust evaluation, which is governed entirely by the <see cref="IPluginTrustPolicy" />.
/// </para>
/// </remarks>
/// <seealso cref="INotableDateAlgorithmPlugin" />
/// <seealso cref="NotableDatePluginLoader" />
public interface INotableDatePlugin
{
    /// <summary>
    /// Gets the human-readable name of the plugin, used by hosts for diagnostics and logging.
    /// </summary>
    /// <returns>The plugin name.</returns>
    string Name { get; }

    /// <summary>
    /// Gets the version of the plugin, used by hosts for diagnostics and compatibility reporting.
    /// </summary>
    /// <returns>The plugin version.</returns>
    Version Version { get; }
}
