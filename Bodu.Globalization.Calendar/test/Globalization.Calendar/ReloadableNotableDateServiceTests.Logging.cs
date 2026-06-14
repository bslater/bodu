// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ReloadableNotableDateServiceTests.Logging.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies that <see cref="MutableNotableDateResourceProvider" /> and <see cref="ReloadableNotableDateService" /> emit
/// diagnostics through a supplied logger and remain silent when no logger is supplied.
/// </summary>
public sealed partial class ReloadableNotableDateServiceTests
{
    /// <summary>
    /// Verifies that reloading the provider with a logger emits an informational reload record.
    /// </summary>
    [TestMethod]
    public void Reload_WhenLoggerSupplied_ShouldLogReload()
    {
        CapturingLogger logger = new();
        MutableNotableDateResourceProvider provider = new(NotableDateResourceLoader.Load(JanuaryXml), logger);

        provider.Reload(NotableDateResourceLoader.Load(FebruaryXml));

        Assert.IsTrue(logger.Entries.Any(e => e.Level == LogLevel.Information && e.EventId.Id == 3003));
    }

    /// <summary>
    /// Verifies that the service logs a resolution-state rebuild the first time it observes a reloaded resource.
    /// </summary>
    [TestMethod]
    public void Resolve_AfterReloadWithLogger_ShouldLogResolutionRebuild()
    {
        CapturingLogger logger = new();
        MutableNotableDateResourceProvider provider = new(NotableDateResourceLoader.Load(JanuaryXml));
        ReloadableNotableDateService service = new(provider, logger);

        _ = service.Resolve(Year2025, "XX");
        provider.Reload(NotableDateResourceLoader.Load(FebruaryXml));
        _ = service.Resolve(Year2025, "XX");

        Assert.IsTrue(logger.Entries.Any(e => e.Level == LogLevel.Debug && e.EventId.Id == 3004));
    }

    /// <summary>
    /// Verifies that a reloadable service constructed without a logger reflects a reload without throwing, confirming
    /// the default <c>null</c> logger path is a no-op.
    /// </summary>
    [TestMethod]
    public void Resolve_AfterReloadWithoutLogger_ShouldReflectReloadWithoutLogging()
    {
        MutableNotableDateResourceProvider provider = new(NotableDateResourceLoader.Load(JanuaryXml));
        ReloadableNotableDateService service = new(provider);

        provider.Reload(NotableDateResourceLoader.Load(FebruaryXml));
        NotableDate match = service.Resolve(Year2025, "XX").Single(r => r.NotableDateId == "special-day");

        Assert.AreEqual(new DateOnly(2025, 2, 1), match.Date);
    }
}
