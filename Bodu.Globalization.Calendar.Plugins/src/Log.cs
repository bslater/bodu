// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Log.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;

namespace Bodu.Globalization.Calendar.Plugins;

/// <summary>
/// Provides the source-generated, allocation-free logging messages emitted while loading and registering notable-date
/// plugins.
/// </summary>
/// <remarks>
/// The messages are produced by the <see cref="LoggerMessageAttribute" /> source generator, so a disabled or
/// <see cref="Microsoft.Extensions.Logging.Abstractions.NullLogger" /> logger short-circuits before any argument is
/// formatted.
/// </remarks>
internal static partial class Log
{
    /// <summary>
    /// Logs that an assembly was rejected by the trust policy and will not be activated.
    /// </summary>
    /// <param name="logger">The logger that receives the message.</param>
    /// <param name="assembly">The display name of the rejected assembly.</param>
    /// <param name="reason">The reason reported by the trust policy.</param>
    [LoggerMessage(EventId = 2001, Level = LogLevel.Warning, Message = "Plugin assembly '{assembly}' rejected by trust policy: {reason}")]
    public static partial void PluginTrustRejected(ILogger logger, string assembly, string reason);

    /// <summary>
    /// Logs that an assembly passed the trust policy and is eligible for activation.
    /// </summary>
    /// <param name="logger">The logger that receives the message.</param>
    /// <param name="assembly">The display name of the trusted assembly.</param>
    [LoggerMessage(EventId = 2002, Level = LogLevel.Debug, Message = "Plugin assembly '{assembly}' trusted; activating its entry point")]
    public static partial void PluginTrusted(ILogger logger, string assembly);

    /// <summary>
    /// Logs that a plugin entry-point type was activated.
    /// </summary>
    /// <param name="logger">The logger that receives the message.</param>
    /// <param name="assembly">The display name of the declaring assembly.</param>
    /// <param name="pluginType">The activated plugin entry-point type.</param>
    [LoggerMessage(EventId = 2003, Level = LogLevel.Information, Message = "Activated notable-date plugin '{pluginType}' from assembly '{assembly}'")]
    public static partial void PluginActivated(ILogger logger, string assembly, string pluginType);

    /// <summary>
    /// Logs the number of algorithms a plugin contributed to a registry.
    /// </summary>
    /// <param name="logger">The logger that receives the message.</param>
    /// <param name="count">The number of algorithms registered.</param>
    /// <param name="pluginType">The plugin that contributed the algorithms.</param>
    [LoggerMessage(EventId = 2004, Level = LogLevel.Information, Message = "Registered {count} algorithm(s) contributed by plugin '{pluginType}'")]
    public static partial void PluginAlgorithmsRegistered(ILogger logger, int count, string pluginType);

    /// <summary>
    /// Logs that a plugin was passed to algorithm registration but does not contribute algorithms, so nothing was
    /// registered.
    /// </summary>
    /// <param name="logger">The logger that receives the message.</param>
    /// <param name="pluginType">The plugin that contributed nothing.</param>
    [LoggerMessage(EventId = 2005, Level = LogLevel.Information, Message = "Plugin '{pluginType}' does not implement INotableDateAlgorithmPlugin; no algorithms registered")]
    public static partial void PluginContributesNoAlgorithms(ILogger logger, string pluginType);

    /// <summary>
    /// Logs that a plugin's contributed key replaced a built-in algorithm or an existing registration because
    /// overriding was explicitly permitted.
    /// </summary>
    /// <param name="logger">The logger that receives the message.</param>
    /// <param name="key">The overridden algorithm key.</param>
    /// <param name="pluginType">The plugin whose contribution replaced the registration.</param>
    [LoggerMessage(EventId = 2006, Level = LogLevel.Warning, Message = "Plugin '{pluginType}' overrides existing algorithm key '{key}'")]
    public static partial void PluginAlgorithmOverridesKey(ILogger logger, string key, string pluginType);
}
