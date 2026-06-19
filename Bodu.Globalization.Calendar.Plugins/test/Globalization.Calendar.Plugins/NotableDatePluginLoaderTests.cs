// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDatePluginLoaderTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;
using Bodu.Globalization.Calendar.Plugins;

[assembly: NotableDatePlugin(typeof(Bodu.Globalization.Calendar.Plugins.TestPlugin))]

namespace Bodu.Globalization.Calendar.Plugins;

/// <summary>
/// A test algorithm placing the occurrence on 1 July.
/// </summary>
public sealed class TestDayAlgorithm
    : INotableDateAlgorithm
{
    /// <inheritdoc />
    public DateOnly? Calculate(int year) =>
        new DateOnly(year, 7, 1);
}

/// <summary>
/// A test plugin contributing a single custom algorithm under the key <c>test-day</c>.
/// </summary>
public sealed class TestPlugin
    : INotableDateAlgorithmPlugin
{
    /// <inheritdoc />
    public string Name => "Test Plugin";

    /// <inheritdoc />
    public Version Version => new(1, 2, 3);

    /// <inheritdoc />
    public IEnumerable<KeyValuePair<string, INotableDateAlgorithm>> GetAlgorithms() =>
        [new KeyValuePair<string, INotableDateAlgorithm>("test-day", new TestDayAlgorithm())];
}

/// <summary>
/// Verifies that the plugin loader activates a trusted, attributed plugin, rejects untrusted or unattributed
/// assemblies, and registers the plugin's algorithms for use by the resolver.
/// </summary>
[TestClass]
public sealed partial class NotableDatePluginLoaderTests
{
    /// <summary>
    /// Gets the test assembly, which declares the plugin via an assembly attribute.
    /// </summary>
    private static Assembly TestAssembly =>
        typeof(TestPlugin).Assembly;
}
