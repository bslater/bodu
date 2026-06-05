// ---------------------------------------------------------------------------------------------------------------
// <copyright file="INotableDatePlugin.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Plugins;

/// <summary>
/// The base contract implemented by every notable-date plugin, exposing identity used by hosts for diagnostics and
/// logging.
/// </summary>
public interface INotableDatePlugin
{
    /// <summary>
    /// Gets the human-readable name of the plugin.
    /// </summary>
    /// <returns>The plugin name.</returns>
    string Name { get; }

    /// <summary>
    /// Gets the version of the plugin.
    /// </summary>
    /// <returns>The plugin version.</returns>
    Version Version { get; }
}
