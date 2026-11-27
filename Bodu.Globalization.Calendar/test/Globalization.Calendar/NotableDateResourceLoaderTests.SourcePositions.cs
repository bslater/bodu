// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateResourceLoaderTests.SourcePositions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class NotableDateResourceLoaderTests
{
    /// <summary>
    /// A document whose notable date, on line 4, gives its <c>category</c> attribute, at position 66, a value outside
    /// the enumeration.
    /// </summary>
    private const string PositionedCategoryXml = """
        <?xml version="1.0" encoding="utf-8"?>
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.positions">
          <NotableDates>
            <NotableDate id="new-years-day" displayName="New Year's Day" category="NotACategory">
              <Rules>
                <Rule id="fixed">
                  <Strategy>
                    <Fixed month="January" day="1" />
                  </Strategy>
                </Rule>
              </Rules>
            </NotableDate>
          </NotableDates>
        </NotableDateResource>
        """;

    /// <summary>
    /// A document whose second rule, its element's name at line 11, position 10, references an unknown algorithm key.
    /// </summary>
    private const string PositionedAlgorithmXml = """
        <?xml version="1.0" encoding="utf-8"?>
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.positions">
          <NotableDates>
            <NotableDate id="mystery-day" displayName="Mystery Day" category="Observance">
              <Rules>
                <Rule id="first">
                  <Strategy>
                    <Fixed month="January" day="1" />
                  </Strategy>
                </Rule>
                <Rule id="second">
                  <Strategy>
                    <Algorithm key="no-such-algorithm" />
                  </Strategy>
                </Rule>
              </Rules>
            </NotableDate>
          </NotableDates>
        </NotableDateResource>
        """;

    /// <summary>
    /// A document whose imports each fail: the first, on line 4, names an absent resource; the second's
    /// <c>Use</c>, on line 6, names a concept its resource lacks; and the third, on line 8, names a resource that fails
    /// the schema.
    /// </summary>
    private const string PositionedImportsXml = """
        <?xml version="1.0" encoding="utf-8"?>
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.positions">
          <Imports>
            <Import resource="absent" />
            <Import resource="present">
              <Use notableDateRef="not-there" />
            </Import>
            <Import resource="broken-schema" />
          </Imports>
        </NotableDateResource>
        """;

    /// <summary>A one-concept document that the import tests resolve under the name <c>present</c>.</summary>
    private const string PositionedPresentXml = """
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="present">
          <NotableDates>
            <NotableDate id="x" displayName="X" category="Observance">
              <Rules><Rule id="r"><Strategy><Fixed month="January" day="1" /></Strategy></Rule></Rules>
            </NotableDate>
          </NotableDates>
        </NotableDateResource>
        """;

    /// <summary>
    /// Verifies that a schema violation in an attribute points at the attribute: line 4, position 66.
    /// </summary>
    [TestMethod]
    public void TryLoad_WhenAnAttributeFailsTheSchema_ShouldPointAtTheAttribute()
    {
        _ = NotableDateResourceLoader.TryLoad(PositionedCategoryXml, _ => null, out _, out IReadOnlyList<NotableDateValidationDiagnostic> diagnostics);

        NotableDateValidationDiagnostic schema = diagnostics.Single(d => d.Code == "BODU-CAL-SCHEMA");
        Assert.AreEqual((4, 66), (schema.LineNumber, schema.LinePosition));
    }

    /// <summary>
    /// Verifies that the reader's diagnostic for a value it does not recognize points at the element that holds the
    /// value: the notable date's name, at line 4, position 6.
    /// </summary>
    [TestMethod]
    public void TryLoad_WhenAValueIsNotRecognized_ShouldPointAtItsElement()
    {
        _ = NotableDateResourceLoader.TryLoad(PositionedCategoryXml, _ => null, out _, out IReadOnlyList<NotableDateValidationDiagnostic> diagnostics);

        NotableDateValidationDiagnostic value = diagnostics.Single(d => d.Code == "BODU-CAL-ENUM");
        Assert.AreEqual((4, 6), (value.LineNumber, value.LinePosition));
    }

    /// <summary>
    /// Verifies that a validation diagnostic about a rule points at the rule's element: line 11, position 10.
    /// </summary>
    [TestMethod]
    public void TryLoad_WhenARuleFailsValidation_ShouldPointAtTheRule()
    {
        _ = NotableDateResourceLoader.TryLoad(PositionedAlgorithmXml, _ => null, out _, out IReadOnlyList<NotableDateValidationDiagnostic> diagnostics);

        NotableDateValidationDiagnostic algorithm = diagnostics.Single(d => d.Code == "BODU-CAL-ALGORITHM");
        Assert.AreEqual((11, 10), (algorithm.LineNumber, algorithm.LinePosition));
    }

    /// <summary>
    /// Verifies that the diagnostics a throwing load carries in its <see cref="NotableDateValidationException" />
    /// point where the collect-mode load's do.
    /// </summary>
    [TestMethod]
    public void Load_WhenARuleFailsValidation_ShouldCarryThePositionInTheException()
    {
        var ex = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = NotableDateResourceLoader.Load(PositionedAlgorithmXml, _ => null);
        });

        NotableDateValidationDiagnostic algorithm = ex.Diagnostics.Single(d => d.Code == "BODU-CAL-ALGORITHM");
        Assert.AreEqual((11, 10), (algorithm.LineNumber, algorithm.LinePosition));
    }

    /// <summary>
    /// Verifies that a repeated rule identifier is reported at the repeat: the second rule, at line 7, position 10.
    /// </summary>
    [TestMethod]
    public void TryLoad_WhenARuleIdentifierRepeats_ShouldPointAtTheRepeat()
    {
        const string xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.positions">
              <NotableDates>
                <NotableDate id="twice" displayName="Twice" category="Observance">
                  <Rules>
                    <Rule id="same"><Strategy><Fixed month="January" day="1" /></Strategy></Rule>
                    <Rule id="same"><Strategy><Fixed month="January" day="2" /></Strategy></Rule>
                  </Rules>
                </NotableDate>
              </NotableDates>
            </NotableDateResource>
            """;

        _ = NotableDateResourceLoader.TryLoad(xml, _ => null, out _, out IReadOnlyList<NotableDateValidationDiagnostic> diagnostics);

        NotableDateValidationDiagnostic duplicate = diagnostics.Single(d => d.Code == "BODU-CAL-DUP-RULE");
        Assert.AreEqual((7, 10), (duplicate.LineNumber, duplicate.LinePosition));
    }

    /// <summary>
    /// Verifies that an override whose target does not exist is reported at the override's element: line 4,
    /// position 6.
    /// </summary>
    [TestMethod]
    public void TryLoad_WhenAnOverrideTargetIsMissing_ShouldPointAtTheOverride()
    {
        const string xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.positions">
              <Overrides>
                <RemoveRule notableDateRef="missing" ruleRef="r" />
              </Overrides>
            </NotableDateResource>
            """;

        _ = NotableDateResourceLoader.TryLoad(xml, _ => null, out _, out IReadOnlyList<NotableDateValidationDiagnostic> diagnostics);

        NotableDateValidationDiagnostic target = diagnostics.Single(d => d.Code == "BODU-CAL-OVERRIDE-ND");
        Assert.AreEqual((4, 6), (target.LineNumber, target.LinePosition));
    }

    /// <summary>
    /// Verifies that an import whose resource cannot be resolved is reported at its <c>Import</c> element: line 4,
    /// position 6.
    /// </summary>
    [TestMethod]
    public void TryLoad_WhenAnImportCannotBeResolved_ShouldPointAtTheImport()
    {
        _ = NotableDateResourceLoader.TryLoad(PositionedImportsXml, ResolvePositionedImports, out _, out IReadOnlyList<NotableDateValidationDiagnostic> diagnostics);

        NotableDateValidationDiagnostic missing = diagnostics.Single(d => d.Code == "BODU-CAL-IMPORT-MISSING");
        Assert.AreEqual((4, 6), (missing.LineNumber, missing.LinePosition));
    }

    /// <summary>
    /// Verifies that a <c>Use</c> naming a concept its resource does not declare is reported at the <c>Use</c>
    /// element: line 6, position 8.
    /// </summary>
    [TestMethod]
    public void TryLoad_WhenAUsedConceptIsMissing_ShouldPointAtTheUse()
    {
        _ = NotableDateResourceLoader.TryLoad(PositionedImportsXml, ResolvePositionedImports, out _, out IReadOnlyList<NotableDateValidationDiagnostic> diagnostics);

        NotableDateValidationDiagnostic concept = diagnostics.Single(d => d.Code == "BODU-CAL-IMPORT-CONCEPT");
        Assert.AreEqual((6, 8), (concept.LineNumber, concept.LinePosition));
    }

    /// <summary>
    /// Verifies that a schema violation inside an imported document is reported at the <c>Import</c> element that
    /// brought it in, line 8, position 6, rather than at its line in the imported document, and with the message a
    /// direct load of that document gives.
    /// </summary>
    [TestMethod]
    public void TryLoad_WhenAnImportedDocumentFailsTheSchema_ShouldPointAtTheImport()
    {
        _ = NotableDateResourceLoader.TryLoad(PositionedImportsXml, ResolvePositionedImports, out _, out IReadOnlyList<NotableDateValidationDiagnostic> diagnostics);
        _ = NotableDateResourceLoader.TryLoad(PositionedCategoryXml, _ => null, out _, out IReadOnlyList<NotableDateValidationDiagnostic> direct);

        NotableDateValidationDiagnostic imported = diagnostics.Single(d => d.Code == "BODU-CAL-SCHEMA");
        Assert.AreEqual((8, 6), (imported.LineNumber, imported.LinePosition));
        Assert.AreEqual(direct.Single(d => d.Code == "BODU-CAL-SCHEMA").Message, imported.Message);
    }

    /// <summary>
    /// Verifies that a validation diagnostic about a rule an import brought in is reported at the <c>Import</c>
    /// element of the loaded document: line 4, position 6.
    /// </summary>
    [TestMethod]
    public void TryLoad_WhenAnImportedRuleFailsValidation_ShouldPointAtTheImport()
    {
        const string xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.positions">
              <Imports>
                <Import resource="algorithm" />
              </Imports>
            </NotableDateResource>
            """;

        _ = NotableDateResourceLoader.TryLoad(xml, name => name == "algorithm" ? PositionedAlgorithmXml : null, out _, out IReadOnlyList<NotableDateValidationDiagnostic> diagnostics);

        NotableDateValidationDiagnostic algorithm = diagnostics.Single(d => d.Code == "BODU-CAL-ALGORITHM");
        Assert.AreEqual((4, 6), (algorithm.LineNumber, algorithm.LinePosition));
    }

    /// <summary>
    /// Verifies that a validation diagnostic about a rule that a patch rewrote is reported at the patch's element:
    /// line 7, position 6.
    /// </summary>
    [TestMethod]
    public void TryLoad_WhenAPatchMakesARuleInvalid_ShouldPointAtThePatch()
    {
        const string xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.positions">
              <Imports>
                <Import resource="present" />
              </Imports>
              <Overrides>
                <PatchRule notableDateRef="x" ruleRef="r">
                  <Strategy><Algorithm key="no-such-algorithm" /></Strategy>
                </PatchRule>
              </Overrides>
            </NotableDateResource>
            """;

        _ = NotableDateResourceLoader.TryLoad(xml, ResolvePositionedImports, out _, out IReadOnlyList<NotableDateValidationDiagnostic> diagnostics);

        NotableDateValidationDiagnostic algorithm = diagnostics.Single(d => d.Code == "BODU-CAL-ALGORITHM");
        Assert.AreEqual((7, 6), (algorithm.LineNumber, algorithm.LinePosition));
    }

    /// <summary>
    /// Verifies that XML that is not well-formed is reported at the position its reader failed at: line 4,
    /// position 3.
    /// </summary>
    [TestMethod]
    public void TryLoad_WhenXmlIsMalformed_ShouldPointAtTheReaderFailure()
    {
        _ = NotableDateResourceLoader.TryLoad("<?xml version=\"1.0\"?>\n<NotableDateResource>\n  <Unclosed>\n</NotableDateResource>", _ => null, out _, out IReadOnlyList<NotableDateValidationDiagnostic> diagnostics);

        NotableDateValidationDiagnostic syntax = diagnostics.Single(d => d.Code == "BODU-CAL-SYNTAX");
        Assert.AreEqual((4, 3), (syntax.LineNumber, syntax.LinePosition));
    }

    /// <summary>
    /// Verifies that JSON that is not well-formed is reported at the character its reader failed at, line 3,
    /// position 24, counted in characters although two before it take two UTF-8 bytes each.
    /// </summary>
    [TestMethod]
    public void TryLoadJson_WhenJsonIsMalformed_ShouldPointAtTheCharacter()
    {
        const string json = "{\n  \"schemaVersion\": \"1.0\",\n  \"resourceId\": \"été\", oops\n}";

        _ = NotableDateResourceLoader.TryLoadJson(json, _ => null, out _, out IReadOnlyList<NotableDateValidationDiagnostic> diagnostics);

        NotableDateValidationDiagnostic syntax = diagnostics.Single(d => d.Code == "BODU-CAL-SYNTAX");
        Assert.AreEqual((3, 24), (syntax.LineNumber, syntax.LinePosition));
    }

    /// <summary>
    /// Verifies that a validation diagnostic about a JSON document carries no position, since a JSON element records
    /// none.
    /// </summary>
    [TestMethod]
    public void TryLoadJson_WhenARuleFailsValidation_ShouldCarryNoPosition()
    {
        const string json = """{ "schemaVersion": "1.0", "resourceId": "test.json", "notableDates": [ { "id": "m", "displayName": "M", "category": "Observance", "rules": [ { "id": "r", "strategy": { "algorithm": { "key": "no-such-algorithm" } } } ] } ] }""";

        _ = NotableDateResourceLoader.TryLoadJson(json, _ => null, out _, out IReadOnlyList<NotableDateValidationDiagnostic> diagnostics);

        NotableDateValidationDiagnostic algorithm = diagnostics.Single(d => d.Code == "BODU-CAL-ALGORITHM");
        Assert.AreEqual(((int?)null, (int?)null), (algorithm.LineNumber, algorithm.LinePosition));
    }

    /// <summary>
    /// Resolves the resources the import position tests name: <c>present</c> to a one-concept document,
    /// <c>broken-schema</c> to a document that fails the schema, and every other name to nothing.
    /// </summary>
    /// <param name="name">The imported resource name.</param>
    /// <returns>The resource's content, or <see langword="null" /> when it is absent.</returns>
    private static string? ResolvePositionedImports(string name) =>
        name switch
        {
            "present" => PositionedPresentXml,
            "broken-schema" => PositionedCategoryXml,
            _ => null,
        };
}
