// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateResourceLoaderTests.TryLoad.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar.Algorithms;

namespace Bodu.Globalization.Calendar;

public sealed partial class NotableDateResourceLoaderTests
{
    /// <summary>A minimal valid document for the collect-mode tests.</summary>
    private const string TryLoadValidXml = """
        <?xml version="1.0" encoding="utf-8"?>
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.trylad.valid">
          <NotableDates>
            <NotableDate id="new-years-day" displayName="New Year's Day" category="PublicHoliday">
              <Rules><Rule id="x"><Strategy><Fixed month="January" day="1" /></Strategy></Rule></Rules>
            </NotableDate>
          </NotableDates>
        </NotableDateResource>
        """;

    /// <summary>A document whose single rule references an unknown algorithm key.</summary>
    private const string TryLoadUnknownAlgorithmXml = """
        <?xml version="1.0" encoding="utf-8"?>
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.trylad.algorithm">
          <NotableDates>
            <NotableDate id="mystery-day" displayName="Mystery Day" category="Observance">
              <Rules><Rule id="x"><Strategy><Algorithm key="no-such-algorithm" /></Strategy></Rule></Rules>
            </NotableDate>
          </NotableDates>
        </NotableDateResource>
        """;

    /// <summary>
    /// Verifies that a valid document loads through the collect-mode overload with a built resource and no
    /// error-severity diagnostics.
    /// </summary>
    [TestMethod]
    public void TryLoad_WhenDocumentIsValid_ShouldReturnTrueWithResource()
    {
        bool loaded = NotableDateResourceLoader.TryLoad(TryLoadValidXml, _ => null, out NotableDateResource? resource, out IReadOnlyList<NotableDateValidationDiagnostic> diagnostics);

        Assert.IsTrue(loaded);
        Assert.IsNotNull(resource);
        Assert.AreEqual("test.trylad.valid", resource!.ResourceId);
        Assert.IsFalse(diagnostics.Any(d => d.Severity == NotableDateValidationSeverity.Error));
    }

    /// <summary>
    /// Verifies that a document failing validation returns <see langword="false" /> with the full diagnostic list -
    /// the same codes the throwing overload reports - and no resource.
    /// </summary>
    [TestMethod]
    public void TryLoad_WhenDocumentFailsValidation_ShouldReturnFalseWithDiagnostics()
    {
        bool loaded = NotableDateResourceLoader.TryLoad(TryLoadUnknownAlgorithmXml, _ => null, out NotableDateResource? resource, out IReadOnlyList<NotableDateValidationDiagnostic> diagnostics);

        Assert.IsFalse(loaded);
        Assert.IsNull(resource);
        Assert.IsTrue(
            diagnostics.Any(d => d.Severity == NotableDateValidationSeverity.Error && d.Code == "BODU-CAL-ALGORITHM"),
            "The unknown algorithm key must surface as a BODU-CAL-ALGORITHM error.");
    }

    /// <summary>
    /// Verifies that the collect-mode overload reports the same diagnostic codes the throwing overload carries in its
    /// <see cref="NotableDateValidationException" /> for the same document.
    /// </summary>
    [TestMethod]
    public void TryLoad_WhenDocumentFailsValidation_ShouldMatchThrowingOverloadDiagnostics()
    {
        var thrown = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = NotableDateResourceLoader.Load(TryLoadUnknownAlgorithmXml, _ => null);
        });

        _ = NotableDateResourceLoader.TryLoad(TryLoadUnknownAlgorithmXml, _ => null, out _, out IReadOnlyList<NotableDateValidationDiagnostic> collected);

        CollectionAssert.AreEqual(
            thrown.Diagnostics.Select(d => (d.Severity, d.Code)).ToList(),
            collected.Select(d => (d.Severity, d.Code)).ToList());
    }

    /// <summary>
    /// Verifies that a custom algorithm registry admits its keys through the collect-mode overload.
    /// </summary>
    [TestMethod]
    public void TryLoad_WhenCustomRegistryDeclaresKey_ShouldReturnTrue()
    {
        NotableDateAlgorithmRegistry registry = new NotableDateAlgorithmRegistry()
            .Register("no-such-algorithm", new StubTryLoadAlgorithm());

        bool loaded = NotableDateResourceLoader.TryLoad(TryLoadUnknownAlgorithmXml, _ => null, registry, out NotableDateResource? resource, out _);

        Assert.IsTrue(loaded);
        Assert.IsNotNull(resource);
    }

    /// <summary>
    /// Verifies that a custom algorithm registry admits its keys through the JSON collect-mode overload, matching the
    /// XML overload.
    /// </summary>
    [TestMethod]
    public void TryLoadJson_WhenCustomRegistryDeclaresKey_ShouldReturnTrue()
    {
        const string json =
            """
            { "schemaVersion": "1.0", "resourceId": "test.trylad.algorithm-json",
              "notableDates": [ { "id": "mystery-day", "displayName": "Mystery Day", "category": "Observance",
                "rules": [ { "id": "x", "strategy": { "algorithm": { "key": "no-such-algorithm" } } } ] } ] }
            """;

        NotableDateAlgorithmRegistry registry = new NotableDateAlgorithmRegistry()
            .Register("no-such-algorithm", new StubTryLoadAlgorithm());

        bool loaded = NotableDateResourceLoader.TryLoadJson(json, _ => null, registry, out NotableDateResource? resource, out _);

        Assert.IsTrue(loaded);
        Assert.IsNotNull(resource);
    }

    /// <summary>
    /// Verifies that a strategy value outside the strategy's range is reported as a <c>BODU-CAL-STRATEGY</c> error
    /// diagnostic, so linting such a document needs no exception handling.
    /// </summary>
    [TestMethod]
    public void TryLoad_WhenStrategyValueIsOutOfRange_ShouldReportStrategyDiagnostic()
    {
        const string xml = """
            <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.trylad.strategy">
              <NotableDates>
                <NotableDate id="x" displayName="X" category="PublicHoliday">
                  <Rules><Rule id="r"><Strategy><OrdinalDayOfMonth month="February" ordinal="-40" /></Strategy></Rule></Rules>
                </NotableDate>
              </NotableDates>
            </NotableDateResource>
            """;

        bool loaded = NotableDateResourceLoader.TryLoad(xml, _ => null, out NotableDateResource? resource, out IReadOnlyList<NotableDateValidationDiagnostic> diagnostics);

        Assert.IsFalse(loaded);
        Assert.IsNull(resource);
        Assert.Contains(d => d.Code == "BODU-CAL-STRATEGY" && d.Severity == NotableDateValidationSeverity.Error, diagnostics, "BODU-CAL-STRATEGY");
    }

    /// <summary>
    /// Verifies that an imported resource whose content is not well-formed is reported as a <c>BODU-CAL-SYNTAX</c> error
    /// diagnostic naming the import, rather than a <see cref="FormatException" />, through both collect-mode entry
    /// points.
    /// </summary>
    [TestMethod]
    public void TryLoad_WhenImportIsNotWellFormed_ShouldReportSyntaxDiagnostic()
    {
        const string xml = """
            <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.trylad.import">
              <Imports><Import resource="broken" /></Imports>
            </NotableDateResource>
            """;
        const string json = """{ "schemaVersion": "1.0", "resourceId": "test.trylad.import-json", "imports": [ { "resource": "broken" } ] }""";

        static string? Resolve(string name) =>
            name == "broken" ? "{ not json" : null;

        bool loadedXml = NotableDateResourceLoader.TryLoad(xml, Resolve, out _, out IReadOnlyList<NotableDateValidationDiagnostic> xmlDiagnostics);
        bool loadedJson = NotableDateResourceLoader.TryLoadJson(json, Resolve, out _, out IReadOnlyList<NotableDateValidationDiagnostic> jsonDiagnostics);

        Assert.AreEqual((false, false), (loadedXml, loadedJson));
        Assert.Contains(d => d.Code == "BODU-CAL-SYNTAX" && d.Message.Contains("broken", StringComparison.Ordinal), xmlDiagnostics, "XML import");
        Assert.Contains(d => d.Code == "BODU-CAL-SYNTAX" && d.Message.Contains("broken", StringComparison.Ordinal), jsonDiagnostics, "JSON import");
    }

    /// <summary>
    /// Verifies that malformed XML is reported as a <c>BODU-CAL-SYNTAX</c> error diagnostic rather than an exception.
    /// </summary>
    [TestMethod]
    public void TryLoad_WhenXmlIsMalformed_ShouldReportSyntaxDiagnostic()
    {
        bool loaded = NotableDateResourceLoader.TryLoad("<not-closed", _ => null, out NotableDateResource? resource, out IReadOnlyList<NotableDateValidationDiagnostic> diagnostics);

        Assert.IsFalse(loaded);
        Assert.IsNull(resource);
        Assert.IsTrue(diagnostics.Any(d => d.Severity == NotableDateValidationSeverity.Error && d.Code == "BODU-CAL-SYNTAX"));
    }

    /// <summary>
    /// Verifies that malformed and empty JSON are reported as <c>BODU-CAL-SYNTAX</c> error diagnostics rather than
    /// exceptions.
    /// </summary>
    [TestMethod]
    [DataRow("{ not json")]
    [DataRow("   ")]
    public void TryLoadJson_WhenJsonIsMalformedOrEmpty_ShouldReportSyntaxDiagnostic(string json)
    {
        bool loaded = NotableDateResourceLoader.TryLoadJson(json, _ => null, out NotableDateResource? resource, out IReadOnlyList<NotableDateValidationDiagnostic> diagnostics);

        Assert.IsFalse(loaded);
        Assert.IsNull(resource);
        Assert.IsTrue(diagnostics.Any(d => d.Severity == NotableDateValidationSeverity.Error && d.Code == "BODU-CAL-SYNTAX"));
    }

    /// <summary>
    /// Verifies that a malformed resolution-policy <c>workingDays</c> is reported as a <c>BODU-CAL-WORKING-DAYS</c> error
    /// diagnostic through both collect-mode entry points, rather than escaping as a <see cref="FormatException" />.
    /// </summary>
    [TestMethod]
    public void TryLoad_WhenWorkingDaysMalformed_ShouldReportWorkingDaysDiagnostic()
    {
        const string xml = """
            <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.trylad.working-days">
              <ResolutionPolicy workingDays="MTWTF" />
            </NotableDateResource>
            """;
        const string json = """{ "schemaVersion": "1.0", "resourceId": "test.trylad.working-days-json", "resolutionPolicy": { "workingDays": "MTWTF" } }""";

        bool loadedXml = NotableDateResourceLoader.TryLoad(xml, _ => null, out _, out IReadOnlyList<NotableDateValidationDiagnostic> xmlDiagnostics);
        bool loadedJson = NotableDateResourceLoader.TryLoadJson(json, _ => null, out _, out IReadOnlyList<NotableDateValidationDiagnostic> jsonDiagnostics);

        Assert.AreEqual((false, false), (loadedXml, loadedJson));
        Assert.Contains(d => d.Code == "BODU-CAL-WORKING-DAYS" && d.Severity == NotableDateValidationSeverity.Error, xmlDiagnostics, "XML");
        Assert.Contains(d => d.Code == "BODU-CAL-WORKING-DAYS" && d.Severity == NotableDateValidationSeverity.Error, jsonDiagnostics, "JSON");
    }

    /// <summary>
    /// Verifies that a JSON integer field holding text that is not an integer is reported as a <c>BODU-CAL-VALUE</c>
    /// error diagnostic through the collect-mode entry point, so the load fails rather than reading the field's default.
    /// </summary>
    [TestMethod]
    public void TryLoadJson_WhenIntegerValueMalformed_ShouldReportValueDiagnostic()
    {
        const string json = """
            { "schemaVersion": "1.0", "resourceId": "test.trylad.priority",
              "notableDates": [ { "id": "x", "displayName": "X", "category": "PublicHoliday",
                "rules": [ { "id": "r", "priority": "high", "strategy": { "fixed": { "month": 3, "day": 1 } } } ] } ] }
            """;

        bool loaded = NotableDateResourceLoader.TryLoadJson(json, _ => null, out NotableDateResource? resource, out IReadOnlyList<NotableDateValidationDiagnostic> diagnostics);

        Assert.IsFalse(loaded);
        Assert.IsNull(resource);
        Assert.Contains(d => d.Code == "BODU-CAL-VALUE" && d.Severity == NotableDateValidationSeverity.Error, diagnostics, "BODU-CAL-VALUE");
    }

    /// <summary>
    /// Verifies that <see langword="null" /> arguments are rejected - the <c>Try</c> contract covers data errors, not
    /// usage errors.
    /// </summary>
    [TestMethod]
    public void TryLoad_WhenArgumentsAreNull_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = NotableDateResourceLoader.TryLoad(null!, _ => null, out _, out _);
        });
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = NotableDateResourceLoader.TryLoad(TryLoadValidXml, null!, out _, out _);
        });
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = NotableDateResourceLoader.TryLoadJson(null!, _ => null, out _, out _);
        });
    }

    /// <summary>A stub algorithm for registry-admission tests.</summary>
    private sealed class StubTryLoadAlgorithm
        : INotableDateAlgorithm
    {
        /// <inheritdoc />
        public DateOnly? Calculate(int year) =>
            new DateOnly(year, 5, 5);
    }
}
