// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateResourceLoaderStreamTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the stream-based load overloads on <see cref="NotableDateResourceLoader" /> and the import-concept
/// resolution diagnostic.
/// </summary>
[TestClass]
public class NotableDateResourceLoaderStreamTests
{
    private const string Xml = """
        <?xml version="1.0" encoding="utf-8"?>
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.stream">
          <NotableDates>
            <NotableDate id="nyd" displayName="New Year" category="PublicHoliday">
              <Rules><Rule id="r"><Strategy><Fixed month="January" day="1" /></Strategy></Rule></Rules>
            </NotableDate>
          </NotableDates>
        </NotableDateResource>
        """;

    private const string Json = """
        {
          "schemaVersion": "1.0",
          "resourceId": "test.stream.json",
          "notableDates": [
            { "id": "nyd", "displayName": "New Year", "category": "PublicHoliday",
              "rules": [ { "id": "r", "strategy": { "fixed": { "month": 1, "day": 1 } } } ] }
          ]
        }
        """;

    private const string ImportingXml = """
        <?xml version="1.0" encoding="utf-8"?>
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="main">
          <Imports><Import resource="src"><Use notableDateRef="not-there" /></Import></Imports>
          <NotableDates>
            <NotableDate id="local" displayName="Local" category="PublicHoliday">
              <Rules><Rule id="r"><Strategy><Fixed month="June" day="1" /></Strategy></Rule></Rules>
            </NotableDate>
          </NotableDates>
        </NotableDateResource>
        """;

    private const string SourceXml = """
        <?xml version="1.0" encoding="utf-8"?>
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="src">
          <NotableDates>
            <NotableDate id="real-concept" displayName="Real" category="PublicHoliday">
              <Rules><Rule id="r"><Strategy><Fixed month="January" day="1" /></Strategy></Rule></Rules>
            </NotableDate>
          </NotableDates>
        </NotableDateResource>
        """;

    private static MemoryStream Stream(string content) =>
        new(Encoding.UTF8.GetBytes(content));

    /// <summary>
    /// Verifies that the XML and JSON stream load overloads parse a resource.
    /// </summary>
    [TestMethod]
    public void StreamOverloads_ShouldParseResource()
    {
        Assert.IsNotNull(NotableDateResourceLoader.Load(Stream(Xml)));
        Assert.IsNotNull(NotableDateResourceLoader.Load(Stream(Xml), _ => null));
        Assert.IsNotNull(NotableDateResourceLoader.LoadJson(Stream(Json)));
    }

    /// <summary>
    /// Verifies that an import whose <c>Use</c> references a concept absent from the imported resource reports the
    /// <c>BODU-CAL-IMPORT-CONCEPT</c> diagnostic.
    /// </summary>
    [TestMethod]
    public void Load_WhenImportUsesMissingConcept_ShouldReportImportConceptDiagnostic()
    {
        NotableDateValidationException ex = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = NotableDateResourceLoader.Load(ImportingXml, name => name == "src" ? SourceXml : null);
        });

        Assert.Contains(d => d.Code == "BODU-CAL-IMPORT-CONCEPT", ex.Diagnostics);
    }
}
