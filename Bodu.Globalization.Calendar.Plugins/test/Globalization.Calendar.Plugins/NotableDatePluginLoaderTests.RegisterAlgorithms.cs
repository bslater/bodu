// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDatePluginLoaderTests.RegisterAlgorithms.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;
using Bodu.Globalization.Calendar.Plugins;

namespace Bodu.Globalization.Calendar.Plugins;

public sealed partial class NotableDatePluginLoaderTests
{
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
}
