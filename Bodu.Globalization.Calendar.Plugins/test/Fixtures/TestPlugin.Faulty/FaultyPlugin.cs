// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FaultyPlugin.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

[assembly: Bodu.Globalization.Calendar.Plugins.NotableDatePlugin(typeof(Bodu.Globalization.Calendar.Plugins.TestPlugin.Faulty.FaultyPlugin))]

namespace Bodu.Globalization.Calendar.Plugins.TestPlugin.Faulty;

/// <summary>
/// A fixture plugin whose instance constructor throws, so activation fails after the trust check passes.
/// </summary>
public sealed class FaultyPlugin
    : INotableDatePlugin
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FaultyPlugin" /> class. Always throws.
    /// </summary>
    /// <exception cref="InvalidOperationException">Always thrown; the fixture simulates a broken plugin.</exception>
    public FaultyPlugin() =>
        throw new InvalidOperationException("The fixture plugin constructor always fails.");

    /// <inheritdoc />
    public string Name =>
        "Faulty Fixture Plugin";

    /// <inheritdoc />
    public Version Version { get; } = new(1, 0, 0);
}
