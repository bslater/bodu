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
public sealed class TestDayAlgorithm : INotableDateAlgorithm
{
    /// <inheritdoc />
    public DateOnly? Calculate(int year) =>
        new DateOnly(year, 7, 1);
}

/// <summary>
/// A test plugin contributing a single custom algorithm under the key <c>test-day</c>.
/// </summary>
public sealed class TestPlugin : INotableDateAlgorithmPlugin
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
public sealed class NotableDatePluginLoaderTests
{
    /// <summary>
    /// Gets the test assembly, which declares the plugin via an assembly attribute.
    /// </summary>
    private static Assembly TestAssembly =>
        typeof(TestPlugin).Assembly;

    /// <summary>
    /// Verifies that a trusted, attributed assembly yields its plugin with the expected identity.
    /// </summary>
    [TestMethod]
    public void LoadFrom_WhenTrustedAndAttributed_ActivatesThePlugin()
    {
        INotableDatePlugin plugin = NotableDatePluginLoader.LoadFrom(TestAssembly, new AllowAllPluginTrustPolicy());

        Assert.IsInstanceOfType<TestPlugin>(plugin);
        Assert.AreEqual(
            ("Test Plugin", new Version(1, 2, 3)),
            (plugin.Name, plugin.Version));
    }

    /// <summary>
    /// Verifies that registering a plugin contributing a single algorithm reports one registered algorithm.
    /// </summary>
    [TestMethod]
    public void RegisterAlgorithms_WhenPluginHasOneAlgorithm_ReturnsRegisteredCount()
    {
        INotableDatePlugin plugin = NotableDatePluginLoader.LoadFrom(TestAssembly, new AllowAllPluginTrustPolicy());
        NotableDateAlgorithmRegistry registry = new();

        var count = NotableDatePluginLoader.RegisterAlgorithms(plugin, registry);

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
    /// Verifies that a rejecting trust policy prevents activation.
    /// </summary>
    [TestMethod]
    public void LoadFrom_WhenTrustRejected_Throws()
    {
        IPluginTrustPolicy policy = new DelegatingPluginTrustPolicy(_ => PluginTrustResult.Rejected("blocked"));

        PluginNotTrustedException ex = Assert.ThrowsExactly<PluginNotTrustedException>(() =>
        {
            _ = NotableDatePluginLoader.LoadFrom(TestAssembly, policy);
        });

        Assert.AreEqual("blocked", ex.Reason);
    }

    /// <summary>
    /// Verifies that a composite policy rejects when any member rejects.
    /// </summary>
    [TestMethod]
    public void LoadFrom_WhenCompositePolicyHasRejection_Throws()
    {
        IPluginTrustPolicy policy = new CompositePluginTrustPolicy(
            new AllowAllPluginTrustPolicy(),
            new DelegatingPluginTrustPolicy(_ => PluginTrustResult.Rejected("second policy")));

        PluginNotTrustedException ex = Assert.ThrowsExactly<PluginNotTrustedException>(() =>
        {
            _ = NotableDatePluginLoader.LoadFrom(TestAssembly, policy);
        });

        Assert.AreEqual("second policy", ex.Reason);
    }

    /// <summary>
    /// Verifies that an assembly without the plugin attribute is rejected.
    /// </summary>
    [TestMethod]
    public void LoadFrom_WhenAssemblyHasNoAttribute_Throws()
    {
        Assert.ThrowsExactly<PluginMissingAttributeException>(() =>
        {
            _ = NotableDatePluginLoader.LoadFrom(typeof(string).Assembly, new AllowAllPluginTrustPolicy());
        });
    }

    /// <summary>
    /// Verifies that the loader populates the trust context with the assembly name and the strong-name public-key token
    /// (lowercase hexadecimal, or <see langword="null" /> when the assembly is not signed).
    /// </summary>
    [TestMethod]
    public void LoadFrom_ShouldPopulateTrustContextWithNameAndToken()
    {
        PluginTrustContext? captured = null;
        IPluginTrustPolicy policy = new DelegatingPluginTrustPolicy(context =>
        {
            captured = context;
            return PluginTrustResult.Trusted();
        });

        _ = NotableDatePluginLoader.LoadFrom(TestAssembly, policy);

        var tokenBytes = TestAssembly.GetName().GetPublicKeyToken();
        var expectedToken = tokenBytes is null || tokenBytes.Length == 0
            ? null
            : Convert.ToHexString(tokenBytes).ToLowerInvariant();

        Assert.IsNotNull(captured);
        Assert.AreEqual(
            (TestAssembly.GetName().Name, (string?)expectedToken),
            ((string?)captured!.AssemblyName, captured.PublicKeyToken));
    }
}
