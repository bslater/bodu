// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FixturePlugin.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar.Algorithms;

[assembly: Bodu.Globalization.Calendar.Plugins.NotableDatePlugin(typeof(Bodu.Globalization.Calendar.Plugins.TestPlugin.FixturePlugin))]

namespace Bodu.Globalization.Calendar.Plugins.TestPlugin;

/// <summary>
/// A well-formed fixture plugin contributing a single algorithm, loaded by the plugin-loader tests as a genuinely
/// foreign assembly.
/// </summary>
public sealed class FixturePlugin
    : INotableDatePlugin, INotableDateAlgorithmPlugin
{
    /// <inheritdoc />
    public string Name =>
        "Fixture Plugin";

    /// <inheritdoc />
    public Version Version { get; } = new(1, 0, 0);

    /// <inheritdoc />
    public IEnumerable<KeyValuePair<string, INotableDateAlgorithm>> GetAlgorithms()
    {
        yield return new KeyValuePair<string, INotableDateAlgorithm>("fixture-day", new FixtureDayAlgorithm());
    }

    /// <summary>The contributed algorithm, resolving 1 July of the requested year.</summary>
    private sealed class FixtureDayAlgorithm
        : INotableDateAlgorithm
    {
        /// <inheritdoc />
        public DateOnly? Calculate(int year) =>
            new DateOnly(year, 7, 1);
    }
}
