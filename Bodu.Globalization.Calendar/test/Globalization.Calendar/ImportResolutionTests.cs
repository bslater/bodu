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
        return name => map.TryGetValue(name, out var content) ? content : null;
    }

    /// <summary>
    /// Verifies that importing every concept of a source brings them in alongside the local concepts, and that the
    /// source's adjustment policy is merged so the imported Christmas substitute resolves.
    /// </summary>
    [TestMethod]
    public void Load_WhenImportAll_BringsInSourceConceptsAndPolicies()
    {
        const string Region = """
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

        NotableDateService service = new(NotableDateResourceLoader.Load(Region, Resolver(("global", Global))));

        var ids = service.Resolve(new DateRange(new DateOnly(2021, 1, 1), new DateOnly(2021, 12, 31)), "XX")
            .Select(r => r.NotableDateId).Distinct().OrderBy(s => s, StringComparer.Ordinal).ToList();
        CollectionAssert.AreEqual(new[] { "christmas", "easter-sunday", "local-day" }, ids);

        // 25 December 2021 is a Saturday; the imported weekend-roll policy substitutes Monday 27 December.
        NotableDate christmas = service.Resolve(new DateOnly(2021, 12, 27), "XX").Single(r => r.NotableDateId == "christmas");
        Assert.IsTrue(christmas.IsObserved);
    }

    /// <summary>
    /// Verifies that cherry-picking renames a concept and scopes another to a territory, so the territory-scoped concept
    /// is absent for other territories.
    /// </summary>
    [TestMethod]
    public void Load_WhenCherryPick_AppliesRenameAndTerritory()
    {
        const string Region = """
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

        NotableDateService service = new(NotableDateResourceLoader.Load(Region, Resolver(("global", Global))));
        DateRange year = new(new DateOnly(2024, 1, 1), new DateOnly(2024, 12, 31));

        var britain = service.Resolve(year, "GB").Select(r => r.NotableDateId).Distinct().OrderBy(s => s, StringComparer.Ordinal).ToList();
        CollectionAssert.AreEqual(new[] { "christmas", "easter" }, britain, "GB sees the renamed easter and the GB-scoped christmas");

        var other = service.Resolve(year, "XX").Select(r => r.NotableDateId).Distinct().ToList();
        CollectionAssert.AreEqual(new[] { "easter" }, other, "another territory sees only the un-scoped easter");
    }

    /// <summary>
    /// Verifies that an import use's adjustment override replaces the source rule's adjustments, so the imported
    /// concept observes the territory's own substitute rather than the source's.
    /// </summary>
    [TestMethod]
    public void Load_WhenUseOverridesAdjustments_ReplacesSourceAdjustment()
    {
        const string Region = """
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

        NotableDateService service = new(NotableDateResourceLoader.Load(Region, Resolver(("global", Global))));

        // 25 December 2021 is a Saturday; the override moves the observance back to Friday 24 December rather than the
        // source weekend-roll's next-working-day Monday 27 December.
        NotableDate christmas = service.Resolve(new DateOnly(2021, 12, 24), "US").Single(r => r.NotableDateId == "christmas");
        Assert.IsTrue(christmas.IsObserved);
        Assert.AreEqual(new DateOnly(2021, 12, 25), christmas.ActualDate);
        Assert.AreEqual(0, service.Resolve(new DateOnly(2021, 12, 27), "US").Count(r => r.NotableDateId == "christmas"), "source weekend-roll replaced");
    }

    /// <summary>
    /// Verifies that two territories importing the same shared concept can each supply their own adjustment override,
    /// so the shared Christmas observes on different days per territory.
    /// </summary>
    [TestMethod]
    public void Load_WhenRegionsOverrideAdjustmentsDifferently_EachObservesItsOwn()
    {
        static string Make(string territory, string policyId, string action, string dayOfWeek) => $$"""
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

        NotableDateService us = new(NotableDateResourceLoader.Load(Make("US", "prev-friday", "MoveToPreviousWeekday", "Friday"), Resolver(("global", Global))));
        NotableDateService gb = new(NotableDateResourceLoader.Load(Make("GB", "next-monday", "MoveToNextWeekday", "Monday"), Resolver(("global", Global))));

        // The shared Christmas (Saturday 25 December 2021) rolls back to Friday in the US and forward to Monday in GB.
        Assert.AreEqual(new DateOnly(2021, 12, 24), us.Resolve(new DateOnly(2021, 12, 24), "US").Single(r => r.NotableDateId == "christmas").Date);
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
