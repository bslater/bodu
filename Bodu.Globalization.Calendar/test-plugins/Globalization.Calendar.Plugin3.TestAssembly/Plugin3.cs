// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Plugin3.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar.Plugins;

[assembly: NotableDatePlugin(typeof(Bodu.Globalization.Calendar.Plugin3.TestAssembly.ThrowingConstructorPlugin))]

namespace Bodu.Globalization.Calendar.Plugin3.TestAssembly;

/// <summary>
/// Test-only plugin whose constructor throws. Exercises the
/// <see cref="PluginActivationException" /> path in <see cref="ExternalPluginLoader" /> where
/// <c>Activator.CreateInstance</c> fails after the plugin passes the trust and attribute
/// checks.
/// </summary>
public sealed class ThrowingConstructorPlugin
    : INotableDatePlugin
{
    /// <summary>
    /// Always throws <see cref="InvalidOperationException" /> on construction so the loader
    /// surfaces a <see cref="PluginActivationException" /> wrapping the cause.
    /// </summary>
    public ThrowingConstructorPlugin()
    {
        throw new InvalidOperationException("intentional failure from Plugin3 test fixture");
    }

    public string Name => nameof(ThrowingConstructorPlugin);

    public Version Version { get; } = new(1, 0, 0);
}
