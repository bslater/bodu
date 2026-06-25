// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDatePluginLoaderTests.Logging.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;

namespace Bodu.Globalization.Calendar.Plugins;

/// <summary>
/// Verifies that <see cref="NotableDatePluginLoader" /> emits diagnostics through a supplied logger and remains silent
/// and side-effect-free when no logger is supplied.
/// </summary>
public sealed partial class NotableDatePluginLoaderTests
{
    /// <summary>
    /// Verifies that activating a trusted plugin emits an informational activation record through the supplied logger.
    /// </summary>
    [TestMethod]
    public void LoadFrom_WhenLoggerSupplied_ShouldLogActivation()
    {
        CapturingLogger logger = new();

        _ = NotableDatePluginLoader.LoadFrom(TestAssembly, new AllowAllPluginTrustPolicy(), logger);

        Assert.Contains(e => e.Level == LogLevel.Information && e.EventId.Id == 2003, logger.Entries);
    }

    /// <summary>
    /// Verifies that a trust-policy rejection emits a warning record through the supplied logger before the loader
    /// throws.
    /// </summary>
    [TestMethod]
    public void LoadFrom_WhenTrustRejectedAndLoggerSupplied_ShouldLogWarning()
    {
        CapturingLogger logger = new();
        IPluginTrustPolicy policy = new DelegatingPluginTrustPolicy(_ => PluginTrustResult.Rejected("blocked"));

        _ = Assert.ThrowsExactly<PluginNotTrustedException>(() =>
        {
            _ = NotableDatePluginLoader.LoadFrom(TestAssembly, policy, logger);
        });

        Assert.Contains(e => e.Level == LogLevel.Warning && e.EventId.Id == 2001, logger.Entries);
    }

    /// <summary>
    /// Verifies that registering a plugin's algorithms emits an informational record reporting the registered count.
    /// </summary>
    [TestMethod]
    public void RegisterAlgorithms_WhenLoggerSupplied_ShouldLogRegisteredCount()
    {
        CapturingLogger logger = new();
        INotableDatePlugin plugin = NotableDatePluginLoader.LoadFrom(TestAssembly, new AllowAllPluginTrustPolicy());
        NotableDateAlgorithmRegistry registry = new();

        int count = NotableDatePluginLoader.RegisterAlgorithms(plugin, registry, logger);

        Assert.AreEqual(1, count);
        Assert.Contains(e => e.Level == LogLevel.Information && e.EventId.Id == 2004, logger.Entries);
    }

    /// <summary>
    /// Verifies that loading without a logger activates the plugin without throwing, confirming the default
    /// <c>null</c> logger path is a no-op.
    /// </summary>
    [TestMethod]
    public void LoadFrom_WhenNoLoggerSupplied_ShouldActivateWithoutLogging()
    {
        INotableDatePlugin plugin = NotableDatePluginLoader.LoadFrom(TestAssembly, new AllowAllPluginTrustPolicy());

        Assert.IsInstanceOfType<TestPlugin>(plugin);
    }
}
