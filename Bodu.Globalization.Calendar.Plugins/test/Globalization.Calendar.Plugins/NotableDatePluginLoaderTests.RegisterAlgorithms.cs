// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDatePluginLoaderTests.RegisterAlgorithms.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

using Microsoft.Extensions.Logging;

namespace Bodu.Globalization.Calendar.Plugins;

public sealed partial class NotableDatePluginLoaderTests
{
    /// <summary>A plugin that implements only <see cref="INotableDatePlugin" />, so it contributes no algorithms.</summary>
    private sealed class NoAlgorithmsPlugin
        : INotableDatePlugin
    {
        /// <inheritdoc />
        public string Name =>
            "No Algorithms Plugin";

        /// <inheritdoc />
        public Version Version { get; } = new(1, 0, 0);
    }

    /// <summary>
    /// Verifies that registering a plugin contributing a single algorithm reports one registered algorithm.
    /// </summary>
    [TestMethod]
    public void RegisterAlgorithms_WhenPluginHasOneAlgorithm_ReturnsRegisteredCount()
    {
        INotableDatePlugin plugin = NotableDatePluginLoader.LoadFrom(TestAssembly, new AllowAllPluginTrustPolicy());
        NotableDateAlgorithmRegistry registry = new();

        int count = NotableDatePluginLoader.RegisterAlgorithms(plugin, registry);

        Assert.AreEqual(1, count);
    }

    /// <summary>
    /// Verifies that, once the plugin's algorithms are registered, the engine resolves a notable date that references
    /// the plugin's key to the algorithm-computed occurrence (1 July).
    /// </summary>
    [TestMethod]
    public void RegisterAlgorithms_WhenKeyRegistered_ResolvesPluginAlgorithmOccurrence()
    {
        INotableDatePlugin plugin = NotableDatePluginLoader.LoadFrom(TestAssembly, new AllowAllPluginTrustPolicy());
        NotableDateAlgorithmRegistry registry = new();
        _ = NotableDatePluginLoader.RegisterAlgorithms(plugin, registry);

        const string Xml = """
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="data.plugin">
          <ResolutionPolicy duplicatePolicy="Error" priorityDirection="HigherWins" />
          <NotableDates>
            <NotableDate id="test-day" displayName="Test Day" category="Observance" defaultNonWorkingDay="false">
              <Rules><Rule id="x"><Strategy><Algorithm key="test-day" /></Strategy></Rule></Rules>
            </NotableDate>
          </NotableDates>
        </NotableDateResource>
        """;

        NotableDateService service = new(NotableDateResourceLoader.Load(Xml, _ => null, registry), new NotableDateServiceOptions { Algorithms = registry });
        NotableDate match = service
            .Resolve(new DateRange(new DateOnly(2024, 1, 1), new DateOnly(2024, 12, 31)), "XX")
            .Single(r => r.NotableDateId == "test-day");

        Assert.AreEqual(new DateOnly(2024, 7, 1), match.Date);
    }

    /// <summary>
    /// Verifies that a plugin that does not implement <see cref="INotableDateAlgorithmPlugin" /> registers nothing,
    /// reports zero, and is logged as contributing no algorithms rather than treated as a fault.
    /// </summary>
    [TestMethod]
    public void RegisterAlgorithms_WhenPluginContributesNoAlgorithms_ShouldReturnZeroAndLogInformation()
    {
        CapturingLogger logger = new();
        NoAlgorithmsPlugin plugin = new();
        NotableDateAlgorithmRegistry registry = new();

        int count = NotableDatePluginLoader.RegisterAlgorithms(plugin, registry, logger);

        Assert.AreEqual(0, count);
        Assert.Contains(e => e.Level == LogLevel.Information && e.EventId.Id == 2005, logger.Entries);
    }

    /// <summary>
    /// Verifies that a plugin contributing the same key twice is reported as a broken plugin with
    /// <see cref="NotableDatePluginException" /> naming the key, and leaves the registry unchanged.
    /// </summary>
    [TestMethod]
    public void RegisterAlgorithms_WhenPluginContributesTheSameKeyTwice_ShouldThrowNotableDatePluginException()
    {
        StubAlgorithmPlugin plugin = new(() =>
        [
            new KeyValuePair<string, INotableDateAlgorithm>("stub-day", new StubAlgorithm()),
            new KeyValuePair<string, INotableDateAlgorithm>("stub-day", new StubAlgorithm()),
        ]);
        NotableDateAlgorithmRegistry registry = new();

        var ex = Assert.ThrowsExactly<NotableDatePluginException>(() =>
        {
            _ = NotableDatePluginLoader.RegisterAlgorithms(plugin, registry);
        });

        Assert.AreEqual(
            string.Format(CultureInfo.CurrentCulture, PluginsResourceStrings.Op_Invalid_PluginAlgorithmKeyCollision, typeof(StubAlgorithmPlugin).FullName, "stub-day"),
            ex.Message);
        Assert.IsNull(ex.InnerException);
        Assert.IsFalse(registry.Contains("stub-day"), "A faulting registration must not partially mutate the registry.");
    }

    /// <summary>
    /// Verifies that a plugin contributing a <see langword="null" /> key is reported as a broken plugin, as a
    /// <see langword="null" /> algorithm is, and leaves the registry unchanged.
    /// </summary>
    [TestMethod]
    public void RegisterAlgorithms_WhenPluginContributesNullKey_ShouldThrowNotableDatePluginException()
    {
        StubAlgorithmPlugin plugin = new(() =>
        [
            new KeyValuePair<string, INotableDateAlgorithm>("stub-day", new StubAlgorithm()),
            new KeyValuePair<string, INotableDateAlgorithm>(null!, new StubAlgorithm()),
        ]);
        NotableDateAlgorithmRegistry registry = new();

        var ex = Assert.ThrowsExactly<NotableDatePluginException>(() =>
        {
            _ = NotableDatePluginLoader.RegisterAlgorithms(plugin, registry);
        });

        Assert.AreEqual(
            string.Format(CultureInfo.CurrentCulture, PluginsResourceStrings.Op_Invalid_PluginAlgorithmEntry, typeof(StubAlgorithmPlugin).FullName),
            ex.Message);
        Assert.IsFalse(registry.Contains("stub-day"), "A faulting registration must not partially mutate the registry.");
    }
}
