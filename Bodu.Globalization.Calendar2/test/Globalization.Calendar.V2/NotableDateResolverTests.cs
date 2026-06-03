// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateResolverTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.V2;

/// <summary>
/// Verifies <see cref="NotableDateResolver" /> against the minimal cookbook: fixed-date resolution, observed-date
/// adjustment, query-width consistency, and territory scoping.
/// </summary>
[TestClass]
public sealed class NotableDateResolverTests
{
    /// <summary>
    /// Builds a resolver over the baseline minimal cookbook.
    /// </summary>
    /// <returns>A resolver for the minimal cookbook.</returns>
    private static NotableDateResolver CreateResolver() =>
        new(MinimalCookbook.Load());

    /// <summary>
    /// Verifies that New Year's Day resolves to its actual date with no adjustment when 1 January falls on a weekday.
    /// (T02)
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void Resolve_NewYearsDay_WhenWeekday_ReturnsActualDate()
    {
        IReadOnlyList<ResolvedNotableDate> results = CreateResolver().Resolve(new DateOnly(2026, 1, 1), "AU");

        Assert.AreEqual(1, results.Count);
        ResolvedNotableDateAssert.AssertEqual(
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
        IReadOnlyList<ResolvedNotableDate> results = CreateResolver().Resolve(new DateOnly(2022, 1, 3), "AU");

        Assert.AreEqual(1, results.Count);
        ResolvedNotableDateAssert.AssertEqual(
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
        IReadOnlyList<ResolvedNotableDate> results = CreateResolver().Resolve(new DateOnly(2022, 1, 1), "AU");

        Assert.AreEqual(0, results.Count);
    }

    /// <summary>
    /// Verifies that single-day and range queries return the same observed New Year's Day result regardless of query
    /// width. (T05)
    /// </summary>
    [TestMethod]
    public void Resolve_NewYearsDay_WhenSingleDayAndRangeQueriesUsed_ReturnsConsistentObservedResult()
    {
        NotableDateResolver resolver = CreateResolver();

        Assert.AreEqual(0, resolver.Resolve(new DateOnly(2022, 1, 1), "AU").Count, "actual-day query");

        IReadOnlyList<ResolvedNotableDate> observedDay = resolver.Resolve(new DateOnly(2022, 1, 3), "AU");
        Assert.AreEqual(1, observedDay.Count, "observed-day query");
        Assert.AreEqual(new DateOnly(2022, 1, 3), observedDay[0].Date);

        IReadOnlyList<ResolvedNotableDate> range = resolver.Resolve(new DateOnly(2022, 1, 1), new DateOnly(2022, 1, 3), "AU");
        Assert.AreEqual(1, range.Count, "range query");
        ResolvedNotableDateAssert.AssertEqual(
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
        IReadOnlyList<ResolvedNotableDate> results = CreateResolver().Resolve(new DateOnly(2026, 4, 25), "AU");

        Assert.AreEqual(1, results.Count);
        ResolvedNotableDateAssert.AssertEqual(
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
        IReadOnlyList<ResolvedNotableDate> results = CreateResolver().Resolve(new DateOnly(2026, 4, 25), "NZ");

        Assert.AreEqual(1, results.Count);
        ResolvedNotableDateAssert.AssertEqual(
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
        IReadOnlyList<ResolvedNotableDate> results = CreateResolver().Resolve(new DateOnly(2026, 4, 25), "US");

        Assert.AreEqual(0, results.Count);
    }

    /// <summary>
    /// Verifies that United States Constitution Day resolves to the 17 September rule. (T09)
    /// </summary>
    [TestMethod]
    public void Resolve_ConstitutionDay_WhenTerritoryIsUnitedStates_ReturnsSeptember17Rule()
    {
        IReadOnlyList<ResolvedNotableDate> results = CreateResolver().Resolve(new DateOnly(2026, 9, 17), "US");

        Assert.AreEqual(1, results.Count);
        ResolvedNotableDateAssert.AssertEqual(
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
        IReadOnlyList<ResolvedNotableDate> results = CreateResolver().Resolve(new DateOnly(2026, 7, 25), "PR");

        Assert.AreEqual(1, results.Count);
        ResolvedNotableDateAssert.AssertEqual(
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
        IReadOnlyList<ResolvedNotableDate> results = CreateResolver()
            .Resolve(new DateOnly(2026, 7, 1), new DateOnly(2026, 9, 30), "PR");

        Assert.AreEqual(1, results.Count);
        Assert.AreEqual(new DateOnly(2026, 7, 25), results[0].Date);
        Assert.AreEqual("pr-fixed-jul-25", results[0].RuleId);
        Assert.IsFalse(results.Any(r => r.RuleId == "us-fixed-sep-17"), "US rule must not leak into a PR query");
    }
}
