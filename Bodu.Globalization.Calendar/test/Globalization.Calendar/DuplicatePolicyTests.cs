// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DuplicatePolicyTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar.RangeResolution;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the document's <see cref="DuplicatePolicy" />: applied when the document is loaded, within the document and
/// in document order, to the concepts and rules it declares, while import composition and override operations keep
/// their own rules.
/// </summary>
[TestClass]
public sealed class DuplicatePolicyTests
{
    /// <summary>
    /// Verifies that the <see cref="DuplicatePolicy.Error" /> policy reports two concepts that share an identifier with
    /// the <c>BODU-CAL-DUP-ND</c> diagnostic.
    /// </summary>
    [TestMethod]
    public void Load_WhenErrorAndConceptIdsCollide_ShouldThrowDuplicateNotableDateDiagnostic()
    {
        NotableDateValidationException ex = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = NotableDateResourceLoader.Load(DuplicateConceptsXml("Error"));
        });

        Assert.Contains(d => d.Code == "BODU-CAL-DUP-ND" && d.Severity == NotableDateValidationSeverity.Error, ex.Diagnostics, "BODU-CAL-DUP-ND");
    }

    /// <summary>
    /// Verifies that the <see cref="DuplicatePolicy.Error" /> policy reports two rules of one concept that share an
    /// identifier with the <c>BODU-CAL-DUP-RULE</c> diagnostic.
    /// </summary>
    [TestMethod]
    public void Load_WhenErrorAndRuleIdsCollide_ShouldThrowDuplicateRuleDiagnostic()
    {
        NotableDateValidationException ex = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = NotableDateResourceLoader.Load(DuplicateRulesXml("Error"));
        });

        Assert.Contains(d => d.Code == "BODU-CAL-DUP-RULE" && d.Severity == NotableDateValidationSeverity.Error, ex.Diagnostics, "BODU-CAL-DUP-RULE");
    }

    /// <summary>
    /// Verifies that the <see cref="DuplicatePolicy.KeepFirst" /> policy keeps the first of two concepts that share an
    /// identifier, at its own position, and discards the later one with its rules.
    /// </summary>
    [TestMethod]
    public void Load_WhenKeepFirstAndConceptIdsCollide_ShouldKeepTheFirstConcept()
    {
        NotableDateResource resource = NotableDateResourceLoader.Load(DuplicateConceptsXml("KeepFirst"));

        CollectionAssert.AreEqual(new[] { "dup", "other" }, resource.NotableDates.Select(d => d.Id).ToArray());
        Assert.AreEqual("First", resource.NotableDates[0].DisplayName);
        CollectionAssert.AreEqual(new[] { "a" }, resource.NotableDates[0].Rules.Select(r => r.Id).ToArray());
    }

    /// <summary>
    /// Verifies that the <see cref="DuplicatePolicy.KeepLast" /> policy keeps the last of two concepts that share an
    /// identifier, at its own position, and discards the earlier one with its rules.
    /// </summary>
    [TestMethod]
    public void Load_WhenKeepLastAndConceptIdsCollide_ShouldKeepTheLastConcept()
    {
        NotableDateResource resource = NotableDateResourceLoader.Load(DuplicateConceptsXml("KeepLast"));

        CollectionAssert.AreEqual(new[] { "other", "dup" }, resource.NotableDates.Select(d => d.Id).ToArray());
        Assert.AreEqual("Second", resource.NotableDates[1].DisplayName);
        CollectionAssert.AreEqual(new[] { "b" }, resource.NotableDates[1].Rules.Select(r => r.Id).ToArray());
    }

    /// <summary>
    /// Verifies that the <see cref="DuplicatePolicy.KeepFirst" /> policy keeps the first of two rules of one concept
    /// that share an identifier, at its own position.
    /// </summary>
    [TestMethod]
    public void Load_WhenKeepFirstAndRuleIdsCollide_ShouldKeepTheFirstRule()
    {
        NotableDateResource resource = NotableDateResourceLoader.Load(DuplicateRulesXml("KeepFirst"));
        NotableDateDefinition concept = resource.NotableDates.Single();

        CollectionAssert.AreEqual(new[] { "r", "s" }, concept.Rules.Select(r => r.Id).ToArray());
        Assert.AreEqual(1, ((Algorithms.FixedDateStrategy)concept.Rules[0].Strategy!).Month);
    }

    /// <summary>
    /// Verifies that the <see cref="DuplicatePolicy.KeepLast" /> policy keeps the last of two rules of one concept that
    /// share an identifier, at its own position.
    /// </summary>
    [TestMethod]
    public void Load_WhenKeepLastAndRuleIdsCollide_ShouldKeepTheLastRule()
    {
        NotableDateResource resource = NotableDateResourceLoader.Load(DuplicateRulesXml("KeepLast"));
        NotableDateDefinition concept = resource.NotableDates.Single();

        CollectionAssert.AreEqual(new[] { "s", "r" }, concept.Rules.Select(r => r.Id).ToArray());
        Assert.AreEqual(2, ((Algorithms.FixedDateStrategy)concept.Rules[1].Strategy!).Month);
    }

    /// <summary>
    /// Verifies that the <see cref="DuplicatePolicy.KeepFirst" /> and <see cref="DuplicatePolicy.KeepLast" /> policies
    /// settle the rules of the concept they keep, so a kept concept whose own rules collide loads.
    /// </summary>
    /// <param name="policy">The duplicate policy token.</param>
    /// <param name="month">The month of the rule the policy keeps.</param>
    /// <param name="day">The day of the rule the policy keeps.</param>
    [TestMethod]
    [DataRow("KeepFirst", 3, 1)]
    [DataRow("KeepLast", 4, 4)]
    public void Load_WhenKeptConceptHasCollidingRules_ShouldSettleItsRules(string policy, int month, int day)
    {
        NotableDateResource resource = NotableDateResourceLoader.Load(DuplicateConceptsWithDuplicateRulesXml(policy));
        NotableDateRule rule = resource.NotableDates.Single().Rules.Single();
        var strategy = (Algorithms.FixedDateStrategy)rule.Strategy!;

        Assert.AreEqual((month, day), (strategy.Month, strategy.Day));
    }

    /// <summary>
    /// Verifies that the <see cref="DuplicatePolicy.Merge" /> policy appends a later concept's rules to the first
    /// concept that shares its identifier, at the first concept's position, and keeps the first concept's own fields.
    /// </summary>
    [TestMethod]
    public void Load_WhenMergeAndConceptIdsCollide_ShouldAppendTheLaterRulesToTheFirstConcept()
    {
        NotableDateResource resource = NotableDateResourceLoader.Load(DuplicateConceptsXml("Merge"));
        NotableDateDefinition merged = resource.NotableDates[0];

        CollectionAssert.AreEqual(new[] { "dup", "other" }, resource.NotableDates.Select(d => d.Id).ToArray());
        CollectionAssert.AreEqual(new[] { "a", "b" }, merged.Rules.Select(r => r.Id).ToArray());
        Assert.AreEqual(
            ("First", NotableDateCategory.PublicHoliday, true, 2, "first-tag"),
            (merged.DisplayName, merged.Category, merged.DefaultNonWorkingDay, merged.DefaultDurationDays, merged.Tags.Single()));
    }

    /// <summary>
    /// Verifies that the <see cref="DuplicatePolicy.Merge" /> policy, which merges concepts only, still reports two rules
    /// that share an identifier with the <c>BODU-CAL-DUP-RULE</c> diagnostic, whether they were declared in one concept
    /// or brought together by the merge.
    /// </summary>
    /// <param name="mergedTogether">
    /// <see langword="true" /> for rules declared in two concepts that the merge joins; <see langword="false" /> for rules
    /// declared in one concept.
    /// </param>
    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void Load_WhenMergeAndRuleIdsCollide_ShouldThrowDuplicateRuleDiagnostic(bool mergedTogether)
    {
        string xml = mergedTogether ? DuplicateConceptsWithSharedRuleXml("Merge") : DuplicateRulesXml("Merge");

        NotableDateValidationException ex = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = NotableDateResourceLoader.Load(xml);
        });

        Assert.Contains(d => d.Code == "BODU-CAL-DUP-RULE" && d.Severity == NotableDateValidationSeverity.Error, ex.Diagnostics, "BODU-CAL-DUP-RULE");
        Assert.DoesNotContain(d => d.Code == "BODU-CAL-DUP-ND", ex.Diagnostics, "BODU-CAL-DUP-ND");
    }

    /// <summary>
    /// Verifies that a policy that settles duplicates loads the document without any duplicate diagnostic, at any
    /// severity.
    /// </summary>
    /// <param name="policy">The duplicate policy token.</param>
    [TestMethod]
    [DataRow("KeepFirst")]
    [DataRow("KeepLast")]
    [DataRow("Merge")]
    public void TryLoad_WhenPolicySettlesDuplicateConcepts_ShouldReportNoDiagnostic(string policy)
    {
        bool loaded = NotableDateResourceLoader.TryLoad(DuplicateConceptsXml(policy), _ => null, out NotableDateResource? resource, out IReadOnlyList<NotableDateValidationDiagnostic> diagnostics);

        Assert.IsTrue(loaded);
        Assert.IsNotNull(resource);
        Assert.AreEqual(0, diagnostics.Count);
    }

    /// <summary>
    /// Verifies that the JSON loader applies the policy as the XML loader does.
    /// </summary>
    /// <param name="policy">The duplicate policy token.</param>
    /// <param name="displayName">The display name of the concept the policy keeps.</param>
    [TestMethod]
    [DataRow("KeepFirst", "First")]
    [DataRow("KeepLast", "Second")]
    [DataRow("Merge", "First")]
    public void LoadJson_WhenConceptIdsCollide_ShouldApplyThePolicy(string policy, string displayName)
    {
        string json = $$"""
            { "schemaVersion": "1.0", "resourceId": "test.duplicate-json",
              "resolutionPolicy": { "duplicatePolicy": "{{policy}}" },
              "notableDates": [
                { "id": "dup", "displayName": "First", "category": "PublicHoliday",
                  "rules": [ { "id": "a", "strategy": { "fixed": { "month": 1, "day": 1 } } } ] },
                { "id": "dup", "displayName": "Second", "category": "Observance",
                  "rules": [ { "id": "b", "strategy": { "fixed": { "month": 2, "day": 2 } } } ] } ] }
            """;

        NotableDateResource resource = NotableDateResourceLoader.LoadJson(json);

        Assert.AreEqual(displayName, resource.NotableDates.Single().DisplayName);
    }

    /// <summary>
    /// Verifies that an <c>AddRule</c> override whose rule identifier already exists in its target concept is reported
    /// with the <c>BODU-CAL-DUP-RULE</c> diagnostic under every duplicate policy, since the policy governs only the
    /// concepts and rules the document declares.
    /// </summary>
    /// <param name="policy">The duplicate policy token.</param>
    [TestMethod]
    [DataRow("Error")]
    [DataRow("KeepFirst")]
    [DataRow("KeepLast")]
    [DataRow("Merge")]
    public void Load_WhenAddRuleCollidesWithDeclaredRule_ShouldThrowDuplicateRuleDiagnosticUnderEveryPolicy(string policy)
    {
        string xml = $"""
            <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.add-rule-collision">
              <ResolutionPolicy duplicatePolicy="{policy}" />
              <NotableDates>
                <NotableDate id="x" displayName="X" category="PublicHoliday">
                  <Rules><Rule id="a"><Strategy><Fixed month="January" day="1" /></Strategy></Rule></Rules>
                </NotableDate>
              </NotableDates>
              <Overrides>
                <AddRule notableDateRef="x"><Rule id="a"><Strategy><Fixed month="February" day="2" /></Strategy></Rule></AddRule>
              </Overrides>
            </NotableDateResource>
            """;

        NotableDateValidationException ex = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = NotableDateResourceLoader.Load(xml);
        });

        Assert.Contains(d => d.Code == "BODU-CAL-DUP-RULE", ex.Diagnostics, "BODU-CAL-DUP-RULE");
    }

    /// <summary>
    /// Verifies that override operations apply to the concept the policy keeps: a <c>PatchRule</c> naming a duplicated
    /// concept patches the surviving declaration.
    /// </summary>
    [TestMethod]
    public void Load_WhenKeepLastAndPatchRuleTargetsDuplicatedConcept_ShouldPatchTheKeptConcept()
    {
        string xml = """
            <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.patch-kept">
              <ResolutionPolicy duplicatePolicy="KeepLast" />
              <NotableDates>
                <NotableDate id="x" displayName="First" category="PublicHoliday">
                  <Rules><Rule id="a"><Strategy><Fixed month="January" day="1" /></Strategy></Rule></Rules>
                </NotableDate>
                <NotableDate id="x" displayName="Second" category="PublicHoliday">
                  <Rules><Rule id="a"><Strategy><Fixed month="February" day="2" /></Strategy></Rule></Rules>
                </NotableDate>
              </NotableDates>
              <Overrides>
                <PatchRule notableDateRef="x" ruleRef="a" priority="7" />
              </Overrides>
            </NotableDateResource>
            """;

        NotableDateResource resource = NotableDateResourceLoader.Load(xml);
        NotableDateDefinition concept = resource.NotableDates.Single();

        Assert.AreEqual(("Second", 7), (concept.DisplayName, concept.Rules.Single().Priority));
    }

    /// <summary>
    /// Verifies that the loaded document's policy applies to its own duplicate concepts while a local concept still
    /// replaces an imported concept with the same identifier.
    /// </summary>
    [TestMethod]
    public void Load_WhenKeepLastAndImportSharesTheDuplicatedId_ShouldKeepTheLastLocalConcept()
    {
        string xml = """
            <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.import-keep-last">
              <ResolutionPolicy duplicatePolicy="KeepLast" />
              <Imports><Import resource="lib" /></Imports>
              <NotableDates>
                <NotableDate id="x" displayName="First Local" category="PublicHoliday">
                  <Rules><Rule id="a"><Strategy><Fixed month="January" day="1" /></Strategy></Rule></Rules>
                </NotableDate>
                <NotableDate id="x" displayName="Second Local" category="PublicHoliday">
                  <Rules><Rule id="a"><Strategy><Fixed month="February" day="2" /></Strategy></Rule></Rules>
                </NotableDate>
              </NotableDates>
            </NotableDateResource>
            """;

        NotableDateResource resource = NotableDateResourceLoader.Load(xml, name => name == "lib" ? LibraryXml("Error", duplicateRules: false, duplicateConcepts: false) : null);

        Assert.AreEqual("Second Local", resource.NotableDates.Single(d => d.Id == "x").DisplayName);
    }

    /// <summary>
    /// Verifies that a local concept replaces an imported concept with the same identifier under every policy, as import
    /// composition always has, even under <see cref="DuplicatePolicy.KeepFirst" />, where the import comes first.
    /// </summary>
    /// <param name="policy">The duplicate policy token.</param>
    [TestMethod]
    [DataRow("Error")]
    [DataRow("KeepFirst")]
    [DataRow("KeepLast")]
    [DataRow("Merge")]
    public void Load_WhenLocalConceptSharesAnImportedId_ShouldKeepTheLocalConceptUnderEveryPolicy(string policy)
    {
        string xml = $"""
            <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.import-local-wins">
              <ResolutionPolicy duplicatePolicy="{policy}" />
              <Imports><Import resource="lib" /></Imports>
              <NotableDates>
                <NotableDate id="x" displayName="Local" category="PublicHoliday">
                  <Rules><Rule id="a"><Strategy><Fixed month="March" day="3" /></Strategy></Rule></Rules>
                </NotableDate>
              </NotableDates>
            </NotableDateResource>
            """;

        NotableDateResource resource = NotableDateResourceLoader.Load(xml, name => name == "lib" ? LibraryXml("Error", duplicateRules: false, duplicateConcepts: false) : null);
        NotableDateDefinition concept = resource.NotableDates.Single(d => d.Id == "x");

        Assert.AreEqual(("Local", 1), (concept.DisplayName, concept.Rules.Count));
    }

    /// <summary>
    /// Verifies that an imported document keeps today's behaviour whatever either document's policy: of its concepts
    /// that share an identifier, the first is composed.
    /// </summary>
    /// <param name="rootPolicy">The loaded document's duplicate policy token.</param>
    /// <param name="libraryPolicy">The imported document's duplicate policy token.</param>
    [TestMethod]
    [DataRow("Error", "Error")]
    [DataRow("KeepLast", "KeepLast")]
    [DataRow("Merge", "Merge")]
    [DataRow("KeepLast", "Error")]
    public void Load_WhenImportedDocumentHasCollidingConcepts_ShouldComposeTheFirst(string rootPolicy, string libraryPolicy)
    {
        string xml = ImportingXml(rootPolicy);

        NotableDateResource resource = NotableDateResourceLoader.Load(xml, name => name == "lib" ? LibraryXml(libraryPolicy, duplicateRules: false, duplicateConcepts: true) : null);
        NotableDateDefinition concept = resource.NotableDates.Single(d => d.Id == "x");

        Assert.AreEqual(("Library First", 1), (concept.DisplayName, concept.Rules.Count));
    }

    /// <summary>
    /// Verifies that two rules sharing an identifier inside an imported concept are reported with the
    /// <c>BODU-CAL-DUP-RULE</c> diagnostic whatever either document's policy, since only the loaded document's policy
    /// applies, and only to its own declarations.
    /// </summary>
    /// <param name="rootPolicy">The loaded document's duplicate policy token.</param>
    /// <param name="libraryPolicy">The imported document's duplicate policy token.</param>
    [TestMethod]
    [DataRow("Error", "Error")]
    [DataRow("KeepFirst", "KeepFirst")]
    [DataRow("KeepLast", "Error")]
    [DataRow("Error", "KeepLast")]
    public void Load_WhenImportedConceptHasCollidingRules_ShouldThrowDuplicateRuleDiagnostic(string rootPolicy, string libraryPolicy)
    {
        string xml = ImportingXml(rootPolicy);

        NotableDateValidationException ex = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = NotableDateResourceLoader.Load(xml, name => name == "lib" ? LibraryXml(libraryPolicy, duplicateRules: true, duplicateConcepts: false) : null);
        });

        Assert.Contains(d => d.Code == "BODU-CAL-DUP-RULE", ex.Diagnostics, "BODU-CAL-DUP-RULE");
    }

    /// <summary>
    /// Verifies that a policy that settles duplicates leaves resolution as if the discarded declarations had never been
    /// written.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenKeepFirstDiscardsAConcept_ShouldNotEmitIt()
    {
        NotableDateService service = new(NotableDateResourceLoader.Load(DuplicateConceptsXml("KeepFirst")));

        IReadOnlyList<NotableDate> year = service.Resolve(new DateRange(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)), "XX");

        CollectionAssert.AreEqual(
            new[] { "2026-01-01 dup/a", "2026-05-05 other/o" },
            year.Select(o => $"{o.Date:yyyy-MM-dd} {o.NotableDateId}/{o.RuleId}").ToArray());
    }

    /// <summary>
    /// Builds a document whose first concept, <c>dup</c> (rule <c>a</c>, 1 January), is declared again after another
    /// concept with different fields and rule <c>b</c> (2 February).
    /// </summary>
    /// <param name="policy">The duplicate policy token.</param>
    /// <returns>The document XML.</returns>
    private static string DuplicateConceptsXml(string policy) => $"""
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.duplicate-concepts">
          <ResolutionPolicy duplicatePolicy="{policy}" />
          <NotableDates>
            <NotableDate id="dup" displayName="First" category="PublicHoliday" defaultNonWorkingDay="true" defaultDurationDays="2">
              <Tags><Tag value="first-tag" /></Tags>
              <Rules><Rule id="a" durationDays="1"><Strategy><Fixed month="January" day="1" /></Strategy></Rule></Rules>
            </NotableDate>
            <NotableDate id="other" displayName="Other" category="Observance">
              <Rules><Rule id="o"><Strategy><Fixed month="May" day="5" /></Strategy></Rule></Rules>
            </NotableDate>
            <NotableDate id="dup" displayName="Second" category="Observance" defaultNonWorkingDay="false" defaultDurationDays="1">
              <Tags><Tag value="second-tag" /></Tags>
              <Rules><Rule id="b"><Strategy><Fixed month="February" day="2" /></Strategy></Rule></Rules>
            </NotableDate>
          </NotableDates>
        </NotableDateResource>
        """;

    /// <summary>
    /// Builds a document whose one concept declares rule <c>r</c> twice (1 January, then 2 February) around rule
    /// <c>s</c>.
    /// </summary>
    /// <param name="policy">The duplicate policy token.</param>
    /// <returns>The document XML.</returns>
    private static string DuplicateRulesXml(string policy) => $"""
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.duplicate-rules">
          <ResolutionPolicy duplicatePolicy="{policy}" />
          <NotableDates>
            <NotableDate id="x" displayName="X" category="PublicHoliday">
              <Rules>
                <Rule id="r"><Strategy><Fixed month="January" day="1" /></Strategy></Rule>
                <Rule id="s"><Strategy><Fixed month="March" day="3" /></Strategy></Rule>
                <Rule id="r"><Strategy><Fixed month="February" day="2" /></Strategy></Rule>
              </Rules>
            </NotableDate>
          </NotableDates>
        </NotableDateResource>
        """;

    /// <summary>
    /// Builds a document that declares concept <c>x</c> twice, each declaration holding two rules <c>r</c>: 1 and 3
    /// March in the first, 1 and 4 April in the second.
    /// </summary>
    /// <param name="policy">The duplicate policy token.</param>
    /// <returns>The document XML.</returns>
    private static string DuplicateConceptsWithDuplicateRulesXml(string policy) => $"""
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.duplicate-both">
          <ResolutionPolicy duplicatePolicy="{policy}" />
          <NotableDates>
            <NotableDate id="x" displayName="First" category="PublicHoliday">
              <Rules>
                <Rule id="r"><Strategy><Fixed month="March" day="1" /></Strategy></Rule>
                <Rule id="r"><Strategy><Fixed month="March" day="3" /></Strategy></Rule>
              </Rules>
            </NotableDate>
            <NotableDate id="x" displayName="Second" category="PublicHoliday">
              <Rules>
                <Rule id="r"><Strategy><Fixed month="April" day="1" /></Strategy></Rule>
                <Rule id="r"><Strategy><Fixed month="April" day="4" /></Strategy></Rule>
              </Rules>
            </NotableDate>
          </NotableDates>
        </NotableDateResource>
        """;

    /// <summary>
    /// Builds a document that declares concept <c>x</c> twice, each declaration holding a rule <c>a</c>.
    /// </summary>
    /// <param name="policy">The duplicate policy token.</param>
    /// <returns>The document XML.</returns>
    private static string DuplicateConceptsWithSharedRuleXml(string policy) => $"""
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.merge-rule-collision">
          <ResolutionPolicy duplicatePolicy="{policy}" />
          <NotableDates>
            <NotableDate id="x" displayName="First" category="PublicHoliday">
              <Rules><Rule id="a"><Strategy><Fixed month="January" day="1" /></Strategy></Rule></Rules>
            </NotableDate>
            <NotableDate id="x" displayName="Second" category="PublicHoliday">
              <Rules><Rule id="a"><Strategy><Fixed month="February" day="2" /></Strategy></Rule></Rules>
            </NotableDate>
          </NotableDates>
        </NotableDateResource>
        """;

    /// <summary>
    /// Builds a document that imports the library <c>lib</c> and declares no concept of its own.
    /// </summary>
    /// <param name="policy">The duplicate policy token.</param>
    /// <returns>The document XML.</returns>
    private static string ImportingXml(string policy) => $"""
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.importer">
          <ResolutionPolicy duplicatePolicy="{policy}" />
          <Imports><Import resource="lib" /></Imports>
          <NotableDates>
            <NotableDate id="own" displayName="Own" category="Observance">
              <Rules><Rule id="o"><Strategy><Fixed month="May" day="5" /></Strategy></Rule></Rules>
            </NotableDate>
          </NotableDates>
        </NotableDateResource>
        """;

    /// <summary>
    /// Builds the library document <c>lib</c>: a concept <c>x</c> with one rule, optionally holding a second rule with
    /// the same identifier, and optionally followed by a second concept <c>x</c>.
    /// </summary>
    /// <param name="policy">The library's duplicate policy token.</param>
    /// <param name="duplicateRules">Whether the first concept declares its rule twice.</param>
    /// <param name="duplicateConcepts">Whether the library declares concept <c>x</c> twice.</param>
    /// <returns>The library XML.</returns>
    private static string LibraryXml(string policy, bool duplicateRules, bool duplicateConcepts)
    {
        string secondRule = duplicateRules ? """<Rule id="a"><Strategy><Fixed month="July" day="7" /></Strategy></Rule>""" : string.Empty;
        string secondConcept = duplicateConcepts
            ? """
              <NotableDate id="x" displayName="Library Second" category="Observance">
                <Rules><Rule id="b"><Strategy><Fixed month="August" day="8" /></Strategy></Rule></Rules>
              </NotableDate>
              """
            : string.Empty;

        return $"""
            <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.lib">
              <ResolutionPolicy duplicatePolicy="{policy}" />
              <NotableDates>
                <NotableDate id="x" displayName="Library First" category="PublicHoliday">
                  <Rules><Rule id="a"><Strategy><Fixed month="June" day="6" /></Strategy></Rule>{secondRule}</Rules>
                </NotableDate>
                {secondConcept}
              </NotableDates>
            </NotableDateResource>
            """;
    }
}
