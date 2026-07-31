// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDatePluginLoaderTests.FixtureAssemblies.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;
using System.Runtime.Loader;
using Bodu.Globalization.Calendar.Algorithms;

namespace Bodu.Globalization.Calendar.Plugins;

public sealed partial class NotableDatePluginLoaderTests
{
    /// <summary>
    /// Returns the on-disk path of a built fixture plugin assembly, copied by the test project into an isolated
    /// per-fixture folder so its dependencies are not resolvable from the test application base.
    /// </summary>
    /// <param name="fixtureName">The fixture project/assembly name.</param>
    /// <returns>The fixture assembly path.</returns>
    private static string FixturePath(string fixtureName)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "PluginFixtures", fixtureName, fixtureName + ".dll");
        Assert.IsTrue(File.Exists(path), $"Fixture assembly not found: {path}");

        return path;
    }

    /// <summary>
    /// Verifies that the file-path overload activates a genuinely foreign fixture plugin — an assembly that is not the
    /// test assembly — into a dedicated collectible load context.
    /// </summary>
    [TestMethod]
    public void LoadFrom_WhenGivenFixtureAssemblyPath_ShouldActivateForeignPlugin()
    {
        INotableDatePlugin plugin = NotableDatePluginLoader.LoadFrom(
            FixturePath("Bodu.Globalization.Calendar.Plugins.TestPlugin"),
            new AllowAllPluginTrustPolicy());

        Assembly pluginAssembly = plugin.GetType().Assembly;
        AssemblyLoadContext? context = AssemblyLoadContext.GetLoadContext(pluginAssembly);

        Assert.AreEqual("Fixture Plugin", plugin.Name);
        Assert.AreEqual(new Version(1, 0, 0), plugin.Version);
        Assert.AreNotEqual(typeof(NotableDatePluginLoaderTests).Assembly, pluginAssembly, "The fixture must be a foreign assembly.");
        Assert.IsNotNull(context);
        Assert.AreNotEqual(AssemblyLoadContext.Default, context, "The fixture must load outside the default context.");
        Assert.IsTrue(context!.IsCollectible);
    }

    /// <summary>
    /// Verifies that a foreign fixture plugin's contributed algorithm registers and calculates through the registry.
    /// </summary>
    [TestMethod]
    public void RegisterAlgorithms_WhenGivenFixturePlugin_ShouldRegisterContributedAlgorithm()
    {
        INotableDatePlugin plugin = NotableDatePluginLoader.LoadFrom(
            FixturePath("Bodu.Globalization.Calendar.Plugins.TestPlugin"),
            new AllowAllPluginTrustPolicy());
        NotableDateAlgorithmRegistry registry = new();

        int registered = NotableDatePluginLoader.RegisterAlgorithms(plugin, registry);

        Assert.AreEqual(1, registered);
        Assert.IsTrue(registry.TryGet("fixture-day", out INotableDateAlgorithm? algorithm));
        Assert.AreEqual(new DateOnly(2026, 7, 1), algorithm!.Calculate(2026));
    }

    /// <summary>
    /// Verifies that a foreign fixture plugin's algorithm resolves end to end through a document rule and the service.
    /// </summary>
    [TestMethod]
    public void RegisterAlgorithms_WhenGivenFixturePlugin_ShouldResolveThroughService()
    {
        const string xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.fixture-plugin">
              <NotableDates>
                <NotableDate id="fixture-day" displayName="Fixture Day" category="Observance">
                  <Rules><Rule id="x"><Strategy><Algorithm key="fixture-day" /></Strategy></Rule></Rules>
                </NotableDate>
              </NotableDates>
            </NotableDateResource>
            """;

        INotableDatePlugin plugin = NotableDatePluginLoader.LoadFrom(
            FixturePath("Bodu.Globalization.Calendar.Plugins.TestPlugin"),
            new AllowAllPluginTrustPolicy());
        NotableDateAlgorithmRegistry registry = new();
        _ = NotableDatePluginLoader.RegisterAlgorithms(plugin, registry);

        NotableDateResource resource = NotableDateResourceLoader.Load(xml, _ => null, registry);
        NotableDateService service = new(resource, new NotableDateServiceOptions { Algorithms = registry });

        var matches = service
            .Resolve(new DateRange(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)), "XX")
            .Where(r => r.NotableDateId == "fixture-day")
            .ToList();

        Assert.HasCount(1, matches);
        Assert.AreEqual(new DateOnly(2026, 7, 1), matches[0].Date);
    }

    /// <summary>
    /// Verifies that a foreign assembly declaring no plugin attribute is rejected with
    /// <see cref="PluginMissingAttributeException" />.
    /// </summary>
    [TestMethod]
    public void LoadFrom_WhenFixtureAssemblyLacksPluginAttribute_ShouldThrowPluginMissingAttributeException()
    {
        _ = Assert.ThrowsExactly<PluginMissingAttributeException>(() =>
        {
            _ = NotableDatePluginLoader.LoadFrom(
                FixturePath("Bodu.Globalization.Calendar.Plugins.TestPlugin.Dependency"),
                new AllowAllPluginTrustPolicy());
        });
    }

    /// <summary>
    /// Verifies that a fixture plugin whose instance constructor throws surfaces
    /// <see cref="PluginActivationException" /> carrying the constructor failure as its inner exception chain.
    /// </summary>
    [TestMethod]
    public void LoadFrom_WhenFixturePluginConstructorThrows_ShouldThrowPluginActivationException()
    {
        var ex = Assert.ThrowsExactly<PluginActivationException>(() =>
        {
            _ = NotableDatePluginLoader.LoadFrom(
                FixturePath("Bodu.Globalization.Calendar.Plugins.TestPlugin.Faulty"),
                new AllowAllPluginTrustPolicy());
        });

        Assert.IsNotNull(ex.InnerException);
        Assert.IsInstanceOfType<TargetInvocationException>(ex.InnerException);
        Assert.IsInstanceOfType<InvalidOperationException>(ex.InnerException!.InnerException);
    }

    /// <summary>
    /// Verifies that a fixture assembly whose declared entry-point type does not implement
    /// <see cref="INotableDatePlugin" /> is rejected with <see cref="PluginActivationException" />.
    /// </summary>
    [TestMethod]
    public void LoadFrom_WhenFixtureEntryPointIsNotAPlugin_ShouldThrowPluginActivationException()
    {
        _ = Assert.ThrowsExactly<PluginActivationException>(() =>
        {
            _ = NotableDatePluginLoader.LoadFrom(
                FixturePath("Bodu.Globalization.Calendar.Plugins.TestPlugin.NotPlugin"),
                new AllowAllPluginTrustPolicy());
        });
    }
}
