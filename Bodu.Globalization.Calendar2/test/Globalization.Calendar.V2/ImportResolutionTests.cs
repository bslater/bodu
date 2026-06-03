// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ImportResolutionTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.V2;

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

        List<string> ids = service.Resolve(new DateRange(new DateOnly(2021, 1, 1), new DateOnly(2021, 12, 31)), "XX")
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

        List<string> britain = service.Resolve(year, "GB").Select(r => r.NotableDateId).Distinct().OrderBy(s => s, StringComparer.Ordinal).ToList();
        CollectionAssert.AreEqual(new[] { "christmas", "easter" }, britain, "GB sees the renamed easter and the GB-scoped christmas");

        List<string> other = service.Resolve(year, "XX").Select(r => r.NotableDateId).Distinct().ToList();
        CollectionAssert.AreEqual(new[] { "easter" }, other, "another territory sees only the un-scoped easter");
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

        Assert.IsTrue(ex.Diagnostics.Any(d => d.Code == "BODU-CAL2-IMPORT-CYCLE"));
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

        Assert.IsTrue(ex.Diagnostics.Any(d => d.Code == "BODU-CAL2-IMPORT-MISSING"));
    }
}
