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
    /// Verifies that a document failing validation returns <see langword="false" /> with the full diagnostic list —
    /// the same codes the throwing overload reports — and no resource.
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
    /// Verifies that <see langword="null" /> arguments are rejected — the <c>Try</c> contract covers data errors, not
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
