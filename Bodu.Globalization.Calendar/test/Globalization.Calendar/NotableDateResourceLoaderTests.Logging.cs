// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateResourceLoaderTests.Logging.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies that <see cref="NotableDateResourceLoader" /> emits load and validation diagnostics through a supplied
/// logger and remains silent when no logger is supplied.
/// </summary>
public sealed partial class NotableDateResourceLoaderTests
{
    /// <summary>
    /// A self-contained, valid notable-date document with a single fixed-date concept.
    /// </summary>
    private const string ValidLoggingXml = """
    <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="data.log">
      <ResolutionPolicy duplicatePolicy="Error" priorityDirection="HigherWins" />
      <NotableDates>
        <NotableDate id="x" displayName="X" category="PublicHoliday">
          <Rules><Rule id="r"><Strategy><Fixed month="January" day="1" /></Strategy></Rule></Rules>
        </NotableDate>
      </NotableDates>
    </NotableDateResource>
    """;

    /// <summary>
    /// A well-formed document whose import cannot be resolved by the default resolver, producing an error diagnostic.
    /// </summary>
    private const string UnresolvableImportXml = """
    <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="data.log.bad">
      <ResolutionPolicy duplicatePolicy="Error" priorityDirection="HigherWins" />
      <Imports><Import resource="missing-resource" /></Imports>
      <NotableDates>
        <NotableDate id="x" displayName="X" category="PublicHoliday">
          <Rules><Rule id="r"><Strategy><Fixed month="January" day="1" /></Strategy></Rule></Rules>
        </NotableDate>
      </NotableDates>
    </NotableDateResource>
    """;

    /// <summary>
    /// Verifies that a successful load emits a debug record reporting the loaded resource.
    /// </summary>
    [TestMethod]
    public void Load_WhenLoggerSupplied_ShouldLogResourceLoaded()
    {
        CapturingLogger logger = new();

        _ = NotableDateResourceLoader.Load(ValidLoggingXml, logger);

        Assert.IsTrue(logger.Entries.Any(e => e.Level == LogLevel.Debug && e.EventId.Id == 3001));
    }

    /// <summary>
    /// Verifies that a validation failure emits a warning record before the loader throws.
    /// </summary>
    [TestMethod]
    public void Load_WhenValidationFailsAndLoggerSupplied_ShouldLogWarning()
    {
        CapturingLogger logger = new();

        _ = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = NotableDateResourceLoader.Load(UnresolvableImportXml, logger);
        });

        Assert.IsTrue(logger.Entries.Any(e => e.Level == LogLevel.Warning && e.EventId.Id == 3002));
    }

    /// <summary>
    /// Verifies that loading without a logger succeeds, confirming the default <c>null</c> logger path is a no-op.
    /// </summary>
    [TestMethod]
    public void Load_WhenNoLoggerSupplied_ShouldLoadWithoutLogging()
    {
        NotableDateResource resource = NotableDateResourceLoader.Load(ValidLoggingXml);

        Assert.AreEqual("data.log", resource.ResourceId);
    }
}
