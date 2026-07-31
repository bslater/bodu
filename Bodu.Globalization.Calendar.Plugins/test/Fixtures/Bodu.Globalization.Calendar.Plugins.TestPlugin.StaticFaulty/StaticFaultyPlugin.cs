// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StaticFaultyPlugin.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

[assembly: Bodu.Globalization.Calendar.Plugins.NotableDatePlugin(typeof(Bodu.Globalization.Calendar.Plugins.TestPlugin.StaticFaulty.StaticFaultyPlugin))]

namespace Bodu.Globalization.Calendar.Plugins.TestPlugin.StaticFaulty;

/// <summary>
/// A fixture plugin whose type initializer throws, so activation surfaces a
/// <see cref="TypeInitializationException" /> rather than a constructor-invocation failure.
/// </summary>
public sealed class StaticFaultyPlugin
    : INotableDatePlugin
{
    /// <summary>
    /// Initializes static members of the <see cref="StaticFaultyPlugin" /> class. Always throws; the explicit static
    /// constructor removes <c>beforefieldinit</c> semantics so the failure surfaces at activation time.
    /// </summary>
    /// <exception cref="InvalidOperationException">Always thrown; the fixture simulates a broken plugin.</exception>
    static StaticFaultyPlugin() =>
        throw new InvalidOperationException("The fixture plugin type initializer always fails.");

    /// <inheritdoc />
    public string Name =>
        "Static Faulty Fixture Plugin";

    /// <inheritdoc />
    public Version Version { get; } = new(1, 0, 0);
}
