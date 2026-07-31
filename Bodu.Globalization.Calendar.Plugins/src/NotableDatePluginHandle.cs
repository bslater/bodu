// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDatePluginHandle.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.Loader;

namespace Bodu.Globalization.Calendar.Plugins;

/// <summary>
/// Owns an activated plugin together with its dedicated <see cref="AssemblyLoadContext" />, so the plugin's assemblies
/// can be unloaded when the handle is disposed.
/// </summary>
/// <remarks>
/// <para>
/// A handle is produced by <see cref="NotableDatePluginLoader.LoadFromFile" />. Disposing it initiates an unload of the
/// plugin's collectible load context; the runtime reclaims the context only once nothing references the plugin's types
/// any longer. In particular, algorithms the plugin registered into a
/// <see cref="Algorithms.NotableDateAlgorithmRegistry" /> — and any service holding that registry — keep the context
/// alive until they are discarded, so tear down consumers of the plugin before (or promptly after) disposing the
/// handle.
/// </para>
/// <para>
/// Disposal is idempotent. The <see cref="Plugin" /> must not be used after the handle is disposed.
/// </para>
/// </remarks>
/// <seealso cref="NotableDatePluginLoader" />
public sealed class NotableDatePluginHandle
    : IDisposable
{
    /// <summary>The plugin's dedicated load context, or <see langword="null" /> when the handle cannot unload.</summary>
    private AssemblyLoadContext? _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotableDatePluginHandle" /> class.
    /// </summary>
    /// <param name="plugin">The activated plugin.</param>
    /// <param name="context">The plugin's dedicated collectible load context, or <see langword="null" />.</param>
    internal NotableDatePluginHandle(INotableDatePlugin plugin, AssemblyLoadContext? context)
    {
        Plugin = plugin;
        _context = context;
    }

    /// <summary>
    /// Gets the activated plugin.
    /// </summary>
    /// <value>The plugin instance the loader activated.</value>
    public INotableDatePlugin Plugin { get; }

    /// <summary>
    /// Gets a value indicating whether disposing the handle initiates an unload of the plugin's load context.
    /// </summary>
    /// <value>
    /// <see langword="true" /> while the handle owns a collectible context that has not yet been unloaded; otherwise
    /// <see langword="false" />.
    /// </value>
    public bool IsUnloadable =>
        _context is not null;

    /// <summary>
    /// Initiates an unload of the plugin's load context. Safe to call more than once.
    /// </summary>
    public void Dispose()
    {
        AssemblyLoadContext? context = Interlocked.Exchange(ref _context, null);
        context?.Unload();
    }
}
