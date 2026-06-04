// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CommonResourcesTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.V2;

/// <summary>
/// Verifies the bundled common notable-date catalogues and their resolver: that the embedded catalogues are
/// discoverable and that a territory resource can import the shared concepts and specialize them with its own
/// territory scope, category, non-working flag, and weekend adjustments.
/// </summary>
[TestClass]
public sealed class CommonResourcesTests
{
    /// <summary>
    /// Verifies that the resolver returns content for a bundled catalogue and <see langword="null" /> for an unknown
    /// name.
    /// </summary>
    [TestMethod]
    public void Resolve_ForBundledCatalogue_ReturnsContentForKnownAndNullForUnknown()
    {
        Assert.IsNotNull(CommonNotableDateResources.Resolve("global-core"));
        Assert.IsNotNull(CommonNotableDateResources.Resolve("christian-western"));
        Assert.IsNull(CommonNotableDateResources.Resolve("no-such-catalogue"));
    }

    /// <summary>
    /// Verifies that a territory resource importing the bundled catalogues resolves the shared dates, computes the
    /// Easter-anchored offsets, and applies the territory's own adjustment and category overrides.
    /// </summary>
    [TestMethod]
    public void Import_FromBundledCatalogues_ResolvesSharedDatesWithTerritoryOverrides()
    {
        const string Region = """
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="data.demo">
          <ResolutionPolicy duplicatePolicy="Error" priorityDirection="HigherWins" />
          <AdjustmentPolicies>
            <AdjustmentPolicy id="sat-to-fri" priority="100">
              <Trigger type="IfDayOfWeek"><Weekday value="Saturday" /></Trigger>
              <Action type="MoveToPreviousWeekday" dayOfWeek="Friday" />
              <Emission mode="ObservedOnly" reason="Observed Friday" />
            </AdjustmentPolicy>
            <AdjustmentPolicy id="sun-to-mon" priority="100">
              <Trigger type="IfDayOfWeek"><Weekday value="Sunday" /></Trigger>
              <Action type="MoveToNextWeekday" dayOfWeek="Monday" />
              <Emission mode="ObservedOnly" reason="Observed Monday" />
            </AdjustmentPolicy>
          </AdjustmentPolicies>
          <Imports>
            <Import resource="global-core">
              <Use notableDateRef="new-years-day" territory="ZZ">
                <Adjustments><Adjustment policyRef="sat-to-fri" /><Adjustment policyRef="sun-to-mon" /></Adjustments>
              </Use>
            </Import>
            <Import resource="christian-western">
              <Use notableDateRef="easter-sunday" territory="ZZ" />
              <Use notableDateRef="good-friday" territory="ZZ" category="Religious" nonWorking="false" />
              <Use notableDateRef="christmas-day" territory="ZZ">
                <Adjustments><Adjustment policyRef="sat-to-fri" /><Adjustment policyRef="sun-to-mon" /></Adjustments>
              </Use>
            </Import>
          </Imports>
          <NotableDates />
        </NotableDateResource>
        """;

        NotableDateService service = new(NotableDateResourceLoader.Load(Region, CommonNotableDateResources.Resolver));
        DateRange year2024 = new(new DateOnly(2024, 1, 1), new DateOnly(2024, 12, 31));

        // Easter Sunday 2024 is 31 March; the imported Good Friday offset resolves to 29 March.
        Assert.AreEqual(new DateOnly(2024, 3, 31), service.Resolve(year2024, "ZZ").Single(r => r.NotableDateId == "easter-sunday").Date);
        NotableDate goodFriday = service.Resolve(new DateOnly(2024, 3, 29), "ZZ").Single(r => r.NotableDateId == "good-friday");
        Assert.AreEqual(NotableDateCategory.Religious, goodFriday.Category, "category override applied");
        Assert.IsFalse(goodFriday.IsNonWorkingDay, "non-working override applied");

        // New Year's Day 2023 falls on a Sunday; the sun-to-mon override observes it on Monday 2 January.
        NotableDate newYear = service.Resolve(new DateOnly(2023, 1, 2), "ZZ").Single(r => r.NotableDateId == "new-years-day");
        Assert.IsTrue(newYear.IsObserved);
        Assert.AreEqual(new DateOnly(2023, 1, 1), newYear.ActualDate);
    }

    /// <summary>
    /// Verifies that every bundled common catalogue parses and passes semantic validation when loaded through the
    /// shared resolver, so an authoring error in any catalogue fails the build.
    /// </summary>
    [TestMethod]
    public void AllBundledCatalogues_LoadAndValidate()
    {
        System.Reflection.Assembly assembly = typeof(CommonNotableDateResources).Assembly;
        const string prefix = "Bodu.Globalization.Calendar.V2.Resources.";
        List<string> catalogues = assembly.GetManifestResourceNames()
            .Where(name => name.StartsWith(prefix, StringComparison.Ordinal) && name.EndsWith(".xml", StringComparison.Ordinal))
            .Select(name => name[prefix.Length..^4])
            .ToList();

        Assert.IsTrue(catalogues.Count >= 2, "expected the bundled catalogues to be embedded");

        foreach (string catalogue in catalogues)
        {
            string? content = CommonNotableDateResources.Resolve(catalogue);
            Assert.IsNotNull(content, $"catalogue '{catalogue}' did not resolve");

            NotableDateResource resource = NotableDateResourceLoader.Load(content!, CommonNotableDateResources.Resolver);
            Assert.IsTrue(resource.NotableDates.Count > 0, $"catalogue '{catalogue}' has no concepts");
        }
    }
}
