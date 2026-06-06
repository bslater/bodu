// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateServiceTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies <see cref="NotableDateService" /> against the minimal notable-date document: fixed-date resolution, observed-date
/// adjustment, query-width consistency, and territory scoping.
/// </summary>
[TestClass]
public sealed class NotableDateServiceTests
{
    /// <summary>
    /// Builds a resolver over the baseline minimal notable-date document.
    /// </summary>
    /// <returns>A resolver for the minimal notable-date document.</returns>
    private static NotableDateService CreateResolver() =>
        new(MinimalNotableDates.Load());

    /// <summary>
    /// Verifies that New Year's Day resolves to its actual date with no adjustment when 1 January falls on a weekday.
    /// (T02)
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void Resolve_NewYearsDay_WhenWeekday_ReturnsActualDate()
    {
        IReadOnlyList<NotableDate> results = CreateResolver().Resolve(new DateOnly(2026, 1, 1), "AU");

        Assert.HasCount(1, results);
        NotableDateAssert.AssertEqual(
            results[0],
            date: new DateOnly(2026, 1, 1),
            actualDate: new DateOnly(2026, 1, 1),
            isObserved: false,
            notableDateId: "new-years-day",
            ruleId: "au-fixed-jan-1",
            displayName: "New Year's Day",
            territory: "AU",
            category: NotableDateCategory.PublicHoliday,
            adjustmentPolicyId: null);
    }

    /// <summary>
    /// Verifies that New Year's Day observed on the following Monday is returned when 1 January falls on a Saturday.
    /// (T03)
    /// </summary>
    [TestMethod]
    public void Resolve_NewYearsDay_WhenWeekend_ReturnsObservedDate()
    {
        IReadOnlyList<NotableDate> results = CreateResolver().Resolve(new DateOnly(2022, 1, 3), "AU");

        Assert.HasCount(1, results);
        NotableDateAssert.AssertEqual(
            results[0],
            date: new DateOnly(2022, 1, 3),
            actualDate: new DateOnly(2022, 1, 1),
            isObserved: true,
            notableDateId: "new-years-day",
            ruleId: "au-fixed-jan-1",
            displayName: "New Year's Day",
            territory: "AU",
            category: NotableDateCategory.PublicHoliday,
            adjustmentPolicyId: "weekend-to-next-monday");
    }

    /// <summary>
    /// Verifies that the observed-only emission mode suppresses the base Saturday occurrence. (T04)
    /// </summary>
    [TestMethod]
    public void Resolve_NewYearsDay_WhenObservedOnly_DoesNotReturnBaseDate()
    {
        IReadOnlyList<NotableDate> results = CreateResolver().Resolve(new DateOnly(2022, 1, 1), "AU");

        Assert.IsEmpty(results);
    }

    /// <summary>
    /// Verifies that single-day and range queries return the same observed New Year's Day result regardless of query
    /// width. (T05)
    /// </summary>
    [TestMethod]
    public void Resolve_NewYearsDay_WhenSingleDayAndRangeQueriesUsed_ReturnsConsistentObservedResult()
    {
        NotableDateService resolver = CreateResolver();

        Assert.IsEmpty(resolver.Resolve(new DateOnly(2022, 1, 1), "AU"), "actual-day query");

        IReadOnlyList<NotableDate> observedDay = resolver.Resolve(new DateOnly(2022, 1, 3), "AU");
        Assert.HasCount(1, observedDay, "observed-day query");
        Assert.AreEqual(new DateOnly(2022, 1, 3), observedDay[0].Date);

        IReadOnlyList<NotableDate> range = resolver.Resolve(new DateRange(new DateOnly(2022, 1, 1), new DateOnly(2022, 1, 3)), "AU");
        Assert.HasCount(1, range, "range query");
        NotableDateAssert.AssertEqual(
            range[0],
            date: new DateOnly(2022, 1, 3),
            actualDate: new DateOnly(2022, 1, 1),
            isObserved: true,
            notableDateId: "new-years-day",
            ruleId: "au-fixed-jan-1",
            displayName: "New Year's Day",
            territory: "AU",
            category: NotableDateCategory.PublicHoliday,
            adjustmentPolicyId: "weekend-to-next-monday");
    }

    /// <summary>
    /// Verifies that ANZAC Day resolves to the Australian rule for an Australian query. (T06)
    /// </summary>
    [TestMethod]
    public void Resolve_AnzacDay_WhenTerritoryIsAustralia_ReturnsAustralianRule()
    {
        IReadOnlyList<NotableDate> results = CreateResolver().Resolve(new DateOnly(2026, 4, 25), "AU");

        Assert.HasCount(1, results);
        NotableDateAssert.AssertEqual(
            results[0],
            date: new DateOnly(2026, 4, 25),
            actualDate: new DateOnly(2026, 4, 25),
            isObserved: false,
            notableDateId: "anzac-day",
            ruleId: "au-fixed-apr-25",
            displayName: "ANZAC Day",
            territory: "AU",
            category: NotableDateCategory.PublicHoliday,
            adjustmentPolicyId: null);
    }

    /// <summary>
    /// Verifies that ANZAC Day resolves to the New Zealand rule for a New Zealand query. (T07)
    /// </summary>
    [TestMethod]
    public void Resolve_AnzacDay_WhenTerritoryIsNewZealand_ReturnsNewZealandRule()
    {
        IReadOnlyList<NotableDate> results = CreateResolver().Resolve(new DateOnly(2026, 4, 25), "NZ");

        Assert.HasCount(1, results);
        NotableDateAssert.AssertEqual(
            results[0],
            date: new DateOnly(2026, 4, 25),
            actualDate: new DateOnly(2026, 4, 25),
            isObserved: false,
            notableDateId: "anzac-day",
            ruleId: "nz-fixed-apr-25",
            displayName: "ANZAC Day",
            territory: "NZ",
            category: NotableDateCategory.PublicHoliday,
            adjustmentPolicyId: null);
    }

    /// <summary>
    /// Verifies that ANZAC Day does not leak into a territory that has no ANZAC Day rule. (T08)
    /// </summary>
    [TestMethod]
    public void Resolve_AnzacDay_WhenTerritoryIsUnitedStates_ReturnsNoResult()
    {
        IReadOnlyList<NotableDate> results = CreateResolver().Resolve(new DateOnly(2026, 4, 25), "US");

        Assert.IsEmpty(results);
    }

    /// <summary>
    /// Verifies that United States Constitution Day resolves to the 17 September rule. (T09)
    /// </summary>
    [TestMethod]
    public void Resolve_ConstitutionDay_WhenTerritoryIsUnitedStates_ReturnsSeptember17Rule()
    {
        IReadOnlyList<NotableDate> results = CreateResolver().Resolve(new DateOnly(2026, 9, 17), "US");

        Assert.HasCount(1, results);
        NotableDateAssert.AssertEqual(
            results[0],
            date: new DateOnly(2026, 9, 17),
            actualDate: new DateOnly(2026, 9, 17),
            isObserved: false,
            notableDateId: "constitution-day",
            ruleId: "us-fixed-sep-17",
            displayName: "Constitution Day",
            territory: "US",
            category: NotableDateCategory.Observance,
            adjustmentPolicyId: null);
    }

    /// <summary>
    /// Verifies that Puerto Rico Constitution Day resolves to the 25 July rule. (T10)
    /// </summary>
    [TestMethod]
    public void Resolve_ConstitutionDay_WhenTerritoryIsPuertoRico_ReturnsJuly25Rule()
    {
        IReadOnlyList<NotableDate> results = CreateResolver().Resolve(new DateOnly(2026, 7, 25), "PR");

        Assert.HasCount(1, results);
        NotableDateAssert.AssertEqual(
            results[0],
            date: new DateOnly(2026, 7, 25),
            actualDate: new DateOnly(2026, 7, 25),
            isObserved: false,
            notableDateId: "constitution-day",
            ruleId: "pr-fixed-jul-25",
            displayName: "Constitution Day",
            territory: "PR",
            category: NotableDateCategory.Observance,
            adjustmentPolicyId: null);
    }

    /// <summary>
    /// Verifies that a Puerto Rico range query returns only the Puerto Rico Constitution Day and never the United States
    /// date, proving the resolver is not keyed by display name. (T11)
    /// </summary>
    [TestMethod]
    public void Resolve_ConstitutionDay_WhenTerritoryIsPuertoRico_DoesNotReturnUnitedStatesRule()
    {
        IReadOnlyList<NotableDate> results = CreateResolver()
            .Resolve(new DateRange(new DateOnly(2026, 7, 1), new DateOnly(2026, 9, 30)), "PR");

        Assert.HasCount(1, results);
        Assert.AreEqual(new DateOnly(2026, 7, 25), results[0].Date);
        Assert.AreEqual("pr-fixed-jul-25", results[0].RuleId);
        Assert.DoesNotContain(r => r.RuleId == "us-fixed-sep-17", results, "US rule must not leak into a PR query");
    }
}
