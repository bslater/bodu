// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DependentPlugin.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar.Algorithms;
using Bodu.Globalization.Calendar.Plugins.TestPlugin.Dependency;

[assembly: Bodu.Globalization.Calendar.Plugins.NotableDatePlugin(typeof(Bodu.Globalization.Calendar.Plugins.TestPlugin.WithDependency.DependentPlugin))]

namespace Bodu.Globalization.Calendar.Plugins.TestPlugin.WithDependency;

/// <summary>
/// A fixture plugin whose contributed algorithm depends on a sibling assembly, so loading it exercises the plugin
/// load context's dependency resolution.
/// </summary>
public sealed class DependentPlugin
    : INotableDatePlugin, INotableDateAlgorithmPlugin
{
    /// <inheritdoc />
    public string Name =>
        "Dependent Fixture Plugin";

    /// <inheritdoc />
    public Version Version { get; } = new(1, 0, 0);

    /// <inheritdoc />
    public IEnumerable<KeyValuePair<string, INotableDateAlgorithm>> GetAlgorithms()
    {
        yield return new KeyValuePair<string, INotableDateAlgorithm>("dependent-day", new DependentAlgorithm());
    }

    /// <summary>The contributed algorithm, delegating to the dependency assembly.</summary>
    private sealed class DependentAlgorithm
        : INotableDateAlgorithm
    {
        /// <inheritdoc />
        public DateOnly? Calculate(int year) =>
            FixtureDateSource.For(year);
    }
}
