// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ImportResolutionTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies that the loader resolves <c>Imports</c> against a resource resolver: importing every concept of a source,
/// cherry-picking with rename and territory overrides, merging the source's adjustment policies, and reporting cycles
/// and missing resources.
/// </summary>
[TestClass]
public sealed class ImportResolutionTests
{
    /// <summary>
    /// A shared source resource declaring an adjustment policy and two concepts (no territory, so global).
    /// </summary>
    private const string Global = """
    <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="data.global">
      <ResolutionPolicy duplicatePolicy="Error" priorityDirection="HigherWins" />
      <AdjustmentPolicies>
        <AdjustmentPolicy id="weekend-roll" priority="100">
          <Trigger type="IfWeekend" />
          <Action type="MoveToNextWorkingDay" skipWeekends="true" skipNonWorkingDates="false" maxSearchDays="7" />
          <Emission mode="ObservedOnly" reason="Substitute" />
        </AdjustmentPolicy>
      </AdjustmentPolicies>
      <NotableDates>
        <NotableDate id="easter-sunday" displayName="Easter Sunday" category="Religious" defaultNonWorkingDay="false">
          <Rules><Rule id="g"><Applicability fromYear="1583" /><Strategy><Algorithm key="western-easter" /></Strategy></Rule></Rules>
        </NotableDate>
        <NotableDate id="christmas" displayName="Christmas Day" category="PublicHoliday" defaultNonWorkingDay="true">
          <Rules><Rule id="g"><Strategy><Fixed month="December" day="25" /></Strategy>
            <Adjustments><Adjustment policyRef="weekend-roll" /></Adjustments></Rule></Rules>
        </NotableDate>
      </NotableDates>
    </NotableDateResource>
    """;

    /// <summary>
    /// Builds a resolver over the supplied named resources.
    /// </summary>
    /// <param name="resources">The resource name/content pairs.</param>
    /// <returns>A resolver delegate.</returns>
    private static Func<string, string?> Resolver(params (string Name, string Content)[] resources)
    {
        Dictionary<string, string> map = resources.ToDictionary(r => r.Name, r => r.Content, StringComparer.Ordinal);
        return name => map.TryGetValue(name, out string? content) ? content : null;
    }

    /// <summary>
    /// An importing region resource that pulls in every concept of <see cref="Global" /> alongside one local concept.
    /// </summary>
    private const string ImportAllRegion = """
    <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="data.region">
      <ResolutionPolicy duplicatePolicy="Error" priorityDirection="HigherWins" />
      <Imports><Import resource="global" /></Imports>
      <NotableDates>
        <NotableDate id="local-day" displayName="Local Day" category="PublicHoliday" defaultNonWorkingDay="true">
          <Rules><Rule id="r"><Strategy><Fixed month="June" day="1" /></Strategy></Rule></Rules>
        </NotableDate>
      </NotableDates>
    </NotableDateResource>
    """;

    /// <summary>
    /// An importing region resource that pulls in a JSON-projected source by name.
    /// </summary>
    private const string ImportJsonRegion = """
    <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="data.region.json">
      <ResolutionPolicy duplicatePolicy="Error" priorityDirection="HigherWins" />
      <Imports><Import resource="json-source" /></Imports>
      <NotableDates>
        <NotableDate id="local-day" displayName="Local Day" category="PublicHoliday" defaultNonWorkingDay="true">
          <Rules><Rule id="r"><Strategy><Fixed month="June" day="1" /></Strategy></Rule></Rules>
        </NotableDate>
      </NotableDates>
    </NotableDateResource>
    """;

    /// <summary>
    /// Verifies that an imported JSON resource prefixed with a byte-order mark and leading white-space is still routed
    /// to the JSON parser, so its concept resolves alongside the local one.
    /// </summary>
    [TestMethod]
    public void Load_WhenImportedJsonHasLeadingBomAndWhitespace_RoutesToJsonParser()
    {
        const string jsonSource = """
        { "schemaVersion": "1.0", "resourceId": "data.jsonsource",
          "notableDates": [ { "id": "json-day", "displayName": "JSON Day", "category": "PublicHoliday", "defaultNonWorkingDay": true,
            "rules": [ { "id": "r", "strategy": { "fixed": { "month": 7, "day": 4 } } } ] } ] }
        """;

        NotableDateResource resource = NotableDateResourceLoader.Load(ImportJsonRegion, Resolver(("json-source", "\uFEFF\n    " + jsonSource)));
        IReadOnlyList<NotableDate> results = new NotableDateService(resource).Resolve(new DateOnly(2026, 7, 4), "XX");

        Assert.Contains(r => r.NotableDateId == "json-day", results);
    }

    /// <summary>
    /// Verifies that importing every concept of a source brings them in alongside the local concepts.
    /// </summary>
    [TestMethod]
    public void Load_WhenImportAll_BringsInSourceConcepts()
    {
        NotableDateService service = new(NotableDateResourceLoader.Load(ImportAllRegion, Resolver(("global", Global))));

        var ids = service.Resolve(new DateRange(new DateOnly(2021, 1, 1), new DateOnly(2021, 12, 31)), "XX")
            .Select(r => r.NotableDateId).Distinct().OrderBy(s => s, StringComparer.Ordinal).ToList();

        CollectionAssert.AreEqual(new[] { "christmas", "easter-sunday", "local-day" }, ids);
    }

    /// <summary>
    /// Verifies that importing every concept of a source also merges the source's adjustment policy, so the imported
    /// Christmas substitute resolves. 25 December 2021 is a Saturday; the imported weekend-roll substitutes Monday
    /// 27 December.
    /// </summary>
    [TestMethod]
    public void Load_WhenImportAll_MergesSourcePolicies()
    {
        NotableDateService service = new(NotableDateResourceLoader.Load(ImportAllRegion, Resolver(("global", Global))));

        NotableDate christmas = service.Resolve(new DateOnly(2021, 12, 27), "XX").Single(r => r.NotableDateId == "christmas");

        Assert.IsTrue(christmas.IsObserved);
    }

    /// <summary>
    /// A region resource that cherry-picks the global source, scoping Christmas to GB and renaming Easter to <c>easter</c>.
    /// </summary>
    private const string CherryPickRegion = """
    <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="data.region">
      <ResolutionPolicy duplicatePolicy="Error" priorityDirection="HigherWins" />
      <Imports>
        <Import resource="global">
          <Use notableDateRef="christmas" territory="GB" />
          <Use notableDateRef="easter-sunday" as="easter" />
        </Import>
      </Imports>
      <NotableDates />
    </NotableDateResource>
    """;

    /// <summary>
    /// Verifies that cherry-picking with a rename and a territory scope makes the scoping territory see both the renamed
    /// concept and the territory-scoped concept.
    /// </summary>
    [TestMethod]
    public void Load_WhenCherryPick_ScopingTerritorySeesRenamedAndScopedConcepts()
    {
        NotableDateService service = new(NotableDateResourceLoader.Load(CherryPickRegion, Resolver(("global", Global))));
        DateRange year = new(new DateOnly(2024, 1, 1), new DateOnly(2024, 12, 31));

        var britain = service.Resolve(year, "GB").Select(r => r.NotableDateId).Distinct().OrderBy(s => s, StringComparer.Ordinal).ToList();

        CollectionAssert.AreEqual(new[] { "christmas", "easter" }, britain, "GB sees the renamed easter and the GB-scoped christmas");
    }

    /// <summary>
    /// Verifies that cherry-picking with a territory scope hides the scoped concept from other territories, which see
    /// only the un-scoped renamed concept.
    /// </summary>
    [TestMethod]
    public void Load_WhenCherryPick_OtherTerritorySeesOnlyUnscopedConcept()
    {
        NotableDateService service = new(NotableDateResourceLoader.Load(CherryPickRegion, Resolver(("global", Global))));
        DateRange year = new(new DateOnly(2024, 1, 1), new DateOnly(2024, 12, 31));

        var other = service.Resolve(year, "XX").Select(r => r.NotableDateId).Distinct().ToList();

        CollectionAssert.AreEqual(new[] { "easter" }, other, "another territory sees only the un-scoped easter");
    }

    /// <summary>
    /// A region resource that imports the global Christmas for US and overrides its adjustment with a move-to-previous-Friday
    /// policy.
    /// </summary>
    private const string OverrideAdjustmentRegion = """
    <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="data.region">
      <ResolutionPolicy duplicatePolicy="Error" priorityDirection="HigherWins" />
      <AdjustmentPolicies>
        <AdjustmentPolicy id="prev-friday" priority="100">
          <Trigger type="IfWeekend" />
          <Action type="MoveToPreviousWeekday" dayOfWeek="Friday" />
          <Emission mode="ObservedOnly" reason="Observed Friday" />
        </AdjustmentPolicy>
      </AdjustmentPolicies>
      <Imports>
        <Import resource="global">
          <Use notableDateRef="christmas" territory="US">
            <Adjustments><Adjustment policyRef="prev-friday" /></Adjustments>
          </Use>
        </Import>
      </Imports>
      <NotableDates />
    </NotableDateResource>
    """;

    /// <summary>
    /// Verifies that an import use's adjustment override observes the imported concept on the territory's own substitute.
    /// 25 December 2021 is a Saturday; the override moves the observance back to Friday 24 December, carrying the actual
    /// date.
    /// </summary>
    [TestMethod]
    public void Load_WhenUseOverridesAdjustments_ObservesTerritorySubstitute()
    {
        NotableDateService service = new(NotableDateResourceLoader.Load(OverrideAdjustmentRegion, Resolver(("global", Global))));

        NotableDate christmas = service.Resolve(new DateOnly(2021, 12, 24), "US").Single(r => r.NotableDateId == "christmas");

        Assert.AreEqual(
            (true, (DateOnly?)new DateOnly(2021, 12, 25)),
            (christmas.IsObserved, christmas.ActualDate));
    }

    /// <summary>
    /// Verifies that an import use's adjustment override replaces the source rule's adjustment, so the source weekend-roll
    /// no longer emits its next-working-day Monday 27 December substitute.
    /// </summary>
    [TestMethod]
    public void Load_WhenUseOverridesAdjustments_ReplacesSourceAdjustment()
    {
        NotableDateService service = new(NotableDateResourceLoader.Load(OverrideAdjustmentRegion, Resolver(("global", Global))));

        Assert.AreEqual(0, service.Resolve(new DateOnly(2021, 12, 27), "US").Count(r => r.NotableDateId == "christmas"), "source weekend-roll replaced");
    }

    /// <summary>
    /// Builds an importing region resource that scopes the shared Christmas to <paramref name="territory" /> and overrides
    /// its adjustment with the supplied weekend-substitution policy.
    /// </summary>
    /// <param name="territory">The territory the imported Christmas is scoped to.</param>
    /// <param name="policyId">The adjustment-policy id.</param>
    /// <param name="action">The adjustment action type.</param>
    /// <param name="dayOfWeek">The target day of week for the action.</param>
    /// <returns>The region resource XML.</returns>
    private static string OverridingRegion(string territory, string policyId, string action, string dayOfWeek) => $$"""
    <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="data.region">
      <ResolutionPolicy duplicatePolicy="Error" priorityDirection="HigherWins" />
      <AdjustmentPolicies>
        <AdjustmentPolicy id="{{policyId}}" priority="100">
          <Trigger type="IfWeekend" />
          <Action type="{{action}}" dayOfWeek="{{dayOfWeek}}" />
          <Emission mode="ObservedOnly" reason="Observed" />
        </AdjustmentPolicy>
      </AdjustmentPolicies>
      <Imports>
        <Import resource="global">
          <Use notableDateRef="christmas" territory="{{territory}}">
            <Adjustments><Adjustment policyRef="{{policyId}}" /></Adjustments>
          </Use>
        </Import>
      </Imports>
      <NotableDates />
    </NotableDateResource>
    """;

    /// <summary>
    /// Verifies that a territory importing the shared concept observes it on its own substitute. The shared Christmas
    /// (Saturday 25 December 2021) rolls back to Friday 24 December under the US move-to-previous-Friday override.
    /// </summary>
    [TestMethod]
    public void Load_WhenRegionsOverrideAdjustmentsDifferently_UsRollsBackToFriday()
    {
        NotableDateService us = new(NotableDateResourceLoader.Load(OverridingRegion("US", "prev-friday", "MoveToPreviousWeekday", "Friday"), Resolver(("global", Global))));

        Assert.AreEqual(new DateOnly(2021, 12, 24), us.Resolve(new DateOnly(2021, 12, 24), "US").Single(r => r.NotableDateId == "christmas").Date);
    }

    /// <summary>
    /// Verifies that another territory importing the same shared concept observes it on its own substitute. The shared
    /// Christmas (Saturday 25 December 2021) rolls forward to Monday 27 December under the GB move-to-next-Monday override.
    /// </summary>
    [TestMethod]
    public void Load_WhenRegionsOverrideAdjustmentsDifferently_GbRollsForwardToMonday()
    {
        NotableDateService gb = new(NotableDateResourceLoader.Load(OverridingRegion("GB", "next-monday", "MoveToNextWeekday", "Monday"), Resolver(("global", Global))));

        Assert.AreEqual(new DateOnly(2021, 12, 27), gb.Resolve(new DateOnly(2021, 12, 27), "GB").Single(r => r.NotableDateId == "christmas").Date);
    }

    /// <summary>
    /// Verifies that an import cycle fails the load with a validation error.
    /// </summary>
    [TestMethod]
    public void Load_WhenImportCycle_Throws()
    {
        const string A = """
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="data.a">
          <Imports><Import resource="b" /></Imports>
          <NotableDates />
        </NotableDateResource>
        """;
        const string B = """
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="data.b">
          <Imports><Import resource="a" /></Imports>
          <NotableDates />
        </NotableDateResource>
        """;

        NotableDateValidationException ex = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = NotableDateResourceLoader.Load(A, Resolver(("a", A), ("b", B)));
        });

        Assert.Contains(d => d.Code == "BODU-CAL-IMPORT-CYCLE", ex.Diagnostics);
    }

    /// <summary>
    /// Verifies that importing an unresolvable resource fails the load with a validation error.
    /// </summary>
    [TestMethod]
    public void Load_WhenImportResourceMissing_Throws()
    {
        const string Region = """
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="data.region">
          <Imports><Import resource="does-not-exist" /></Imports>
          <NotableDates />
        </NotableDateResource>
        """;

        NotableDateValidationException ex = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = NotableDateResourceLoader.Load(Region, Resolver());
        });

        Assert.Contains(d => d.Code == "BODU-CAL-IMPORT-MISSING", ex.Diagnostics);
    }
}
