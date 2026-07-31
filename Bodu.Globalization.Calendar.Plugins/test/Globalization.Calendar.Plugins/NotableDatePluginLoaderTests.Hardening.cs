// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDatePluginLoaderTests.Hardening.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar.Algorithms;

namespace Bodu.Globalization.Calendar.Plugins;

public sealed partial class NotableDatePluginLoaderTests
{
    /// <summary>A configurable in-test plugin whose contributed algorithm sequence is supplied by the test.</summary>
    private sealed class StubAlgorithmPlugin
        : INotableDatePlugin, INotableDateAlgorithmPlugin
    {
        /// <summary>The factory producing the contributed sequence.</summary>
        private readonly Func<IEnumerable<KeyValuePair<string, INotableDateAlgorithm>>> _algorithms;

        /// <summary>
        /// Initializes a new instance of the <see cref="StubAlgorithmPlugin" /> class.
        /// </summary>
        /// <param name="algorithms">The factory producing the contributed sequence.</param>
        public StubAlgorithmPlugin(Func<IEnumerable<KeyValuePair<string, INotableDateAlgorithm>>> algorithms) =>
            _algorithms = algorithms;

        /// <inheritdoc />
        public string Name =>
            "Stub Plugin";

        /// <inheritdoc />
        public Version Version { get; } = new(1, 0, 0);

        /// <inheritdoc />
        public IEnumerable<KeyValuePair<string, INotableDateAlgorithm>> GetAlgorithms() =>
            _algorithms();
    }

    /// <summary>A trivial algorithm returning a fixed date.</summary>
    private sealed class StubAlgorithm
        : INotableDateAlgorithm
    {
        /// <inheritdoc />
        public DateOnly? Calculate(int year) =>
            new DateOnly(year, 6, 1);
    }

    /// <summary>
    /// Verifies that a plugin whose contributed algorithm calls into a sibling assembly shipped alongside it resolves
    /// that dependency from the plugin's own directory rather than failing because the dependency is not deployed with
    /// the host application.
    /// </summary>
    [TestMethod]
    public void LoadFrom_WhenFixturePluginDependsOnSiblingAssembly_ShouldResolveDependencyFromPluginDirectory()
    {
        INotableDatePlugin plugin = NotableDatePluginLoader.LoadFrom(
            FixturePath("Bodu.Globalization.Calendar.Plugins.TestPlugin.WithDependency"),
            new AllowAllPluginTrustPolicy());
        NotableDateAlgorithmRegistry registry = new();

        int registered = NotableDatePluginLoader.RegisterAlgorithms(plugin, registry);

        Assert.AreEqual(1, registered);
        Assert.IsTrue(registry.TryGet("dependent-day", out INotableDateAlgorithm? algorithm));
        Assert.AreEqual(new DateOnly(2026, 7, 4), algorithm!.Calculate(2026));
    }

    /// <summary>
    /// Verifies that a plugin whose type initializer throws surfaces <see cref="PluginActivationException" /> carrying
    /// the <see cref="TypeInitializationException" />, rather than letting the raw failure escape.
    /// </summary>
    [TestMethod]
    public void LoadFrom_WhenFixturePluginTypeInitializerThrows_ShouldThrowPluginActivationException()
    {
        var ex = Assert.ThrowsExactly<PluginActivationException>(() =>
        {
            _ = NotableDatePluginLoader.LoadFrom(
                FixturePath("Bodu.Globalization.Calendar.Plugins.TestPlugin.StaticFaulty"),
                new AllowAllPluginTrustPolicy());
        });

        Assert.IsNotNull(ex.InnerException);
        Assert.IsInstanceOfType<TypeInitializationException>(ex.InnerException);
    }

    /// <summary>
    /// Verifies that a file that is not a managed assembly is rejected with <see cref="NotableDatePluginException" />
    /// carrying the <see cref="BadImageFormatException" />, rather than letting the raw failure escape.
    /// </summary>
    [TestMethod]
    public void LoadFrom_WhenFileIsNotAManagedAssembly_ShouldThrowNotableDatePluginException()
    {
        string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".dll");
        File.WriteAllBytes(path, [0x4D, 0x5A, 0x00, 0x01, 0x02, 0x03, 0x04, 0x05]);

        try
        {
            var ex = Assert.ThrowsExactly<NotableDatePluginException>(() =>
            {
                _ = NotableDatePluginLoader.LoadFrom(path, new AllowAllPluginTrustPolicy());
            });

            Assert.IsNotNull(ex.InnerException);
            Assert.IsInstanceOfType<BadImageFormatException>(ex.InnerException);
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>
    /// Verifies that an empty assembly path is rejected up front with an <see cref="ArgumentException" /> naming the
    /// parameter, rather than surfacing a path-API failure with a foreign parameter name.
    /// </summary>
    [TestMethod]
    public void LoadFrom_WhenAssemblyPathIsEmpty_ShouldThrowArgumentException()
    {
        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = NotableDatePluginLoader.LoadFrom(string.Empty, new AllowAllPluginTrustPolicy());
        });

        Assert.AreEqual("assemblyPath", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a plugin returning a <see langword="null" /> algorithm sequence is reported as a broken plugin via
    /// <see cref="NotableDatePluginException" /> rather than a raw <see cref="NullReferenceException" />.
    /// </summary>
    [TestMethod]
    public void RegisterAlgorithms_WhenPluginReturnsNullSequence_ShouldThrowNotableDatePluginException()
    {
        StubAlgorithmPlugin plugin = new(() => null!);
        NotableDateAlgorithmRegistry registry = new();

        _ = Assert.ThrowsExactly<NotableDatePluginException>(() =>
        {
            _ = NotableDatePluginLoader.RegisterAlgorithms(plugin, registry);
        });
    }

    /// <summary>
    /// Verifies that a plugin faulting mid-enumeration leaves the registry unchanged — registration is atomic — and
    /// surfaces the failure as <see cref="NotableDatePluginException" />.
    /// </summary>
    [TestMethod]
    public void RegisterAlgorithms_WhenPluginThrowsMidEnumeration_ShouldLeaveRegistryUnchanged()
    {
        static IEnumerable<KeyValuePair<string, INotableDateAlgorithm>> Faulting()
        {
            yield return new KeyValuePair<string, INotableDateAlgorithm>("stub-day", new StubAlgorithm());
            throw new InvalidOperationException("The plugin faults after its first algorithm.");
        }

        StubAlgorithmPlugin plugin = new(Faulting);
        NotableDateAlgorithmRegistry registry = new();

        var ex = Assert.ThrowsExactly<NotableDatePluginException>(() =>
        {
            _ = NotableDatePluginLoader.RegisterAlgorithms(plugin, registry);
        });

        Assert.IsNotNull(ex.InnerException);
        Assert.IsInstanceOfType<InvalidOperationException>(ex.InnerException);
        Assert.IsFalse(registry.Contains("stub-day"), "A faulting registration must not partially mutate the registry.");
    }

    /// <summary>
    /// Verifies that a plugin contributing a <see langword="null" /> algorithm is reported as a broken plugin and
    /// leaves the registry unchanged.
    /// </summary>
    [TestMethod]
    public void RegisterAlgorithms_WhenPluginContributesNullAlgorithm_ShouldThrowNotableDatePluginException()
    {
        StubAlgorithmPlugin plugin = new(() =>
        [
            new KeyValuePair<string, INotableDateAlgorithm>("stub-day", new StubAlgorithm()),
            new KeyValuePair<string, INotableDateAlgorithm>("null-day", null!),
        ]);
        NotableDateAlgorithmRegistry registry = new();

        _ = Assert.ThrowsExactly<NotableDatePluginException>(() =>
        {
            _ = NotableDatePluginLoader.RegisterAlgorithms(plugin, registry);
        });

        Assert.IsFalse(registry.Contains("stub-day"), "A faulting registration must not partially mutate the registry.");
    }

    /// <summary>
    /// Verifies that a plugin key colliding with a built-in algorithm key is rejected by default with
    /// <see cref="NotableDatePluginException" />, so a plugin cannot silently take over a built-in resolution path.
    /// </summary>
    [TestMethod]
    public void RegisterAlgorithms_WhenKeyCollidesWithBuiltInAlgorithm_ShouldThrowNotableDatePluginException()
    {
        StubAlgorithmPlugin plugin = new(() =>
        [
            new KeyValuePair<string, INotableDateAlgorithm>("western-easter", new StubAlgorithm()),
        ]);
        NotableDateAlgorithmRegistry registry = new();

        _ = Assert.ThrowsExactly<NotableDatePluginException>(() =>
        {
            _ = NotableDatePluginLoader.RegisterAlgorithms(plugin, registry);
        });

        Assert.IsFalse(registry.Contains("western-easter"), "The colliding key must not be registered.");
    }

    /// <summary>
    /// Verifies that a plugin key colliding with a key already present in the target registry — for example one
    /// contributed by another plugin — is rejected by default.
    /// </summary>
    [TestMethod]
    public void RegisterAlgorithms_WhenKeyCollidesWithExistingRegistration_ShouldThrowNotableDatePluginException()
    {
        NotableDateAlgorithmRegistry registry = new();
        registry.Register("stub-day", new StubAlgorithm());
        StubAlgorithmPlugin plugin = new(() =>
        [
            new KeyValuePair<string, INotableDateAlgorithm>("stub-day", new StubAlgorithm()),
        ]);

        _ = Assert.ThrowsExactly<NotableDatePluginException>(() =>
        {
            _ = NotableDatePluginLoader.RegisterAlgorithms(plugin, registry);
        });
    }
}
