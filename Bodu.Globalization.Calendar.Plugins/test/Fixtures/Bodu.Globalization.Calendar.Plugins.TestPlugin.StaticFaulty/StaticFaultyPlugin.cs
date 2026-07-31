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
    /// <summary>The value whose initializer faults the type.</summary>
    private static readonly int s_faulted = Fault();

    /// <inheritdoc />
    public string Name =>
        $"Static Faulty Fixture Plugin {s_faulted}";

    /// <inheritdoc />
    public Version Version { get; } = new(1, 0, 0);

    /// <summary>
    /// Always throws, faulting the type initializer.
    /// </summary>
    /// <returns>Never returns.</returns>
    /// <exception cref="InvalidOperationException">Always thrown; the fixture simulates a broken plugin.</exception>
    private static int Fault() =>
        throw new InvalidOperationException("The fixture plugin type initializer always fails.");
}
