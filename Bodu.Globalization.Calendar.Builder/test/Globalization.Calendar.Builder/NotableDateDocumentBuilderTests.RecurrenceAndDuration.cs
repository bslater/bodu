// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateDocumentBuilderTests.RecurrenceAndDuration.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Extensions;

namespace Bodu.Globalization.Calendar.Builder;

/// <summary>
/// Verifies that the fluent builder authors recurrence sources and calculated end-date durations, and that both XML and
/// JSON round-trips preserve them through to a resolvable service.
/// </summary>
[TestClass]
public sealed class NotableDateDocumentBuilderRecurrenceAndDurationTests
{
    /// <summary>
    /// Authors a document with a recurrence rule and a calculated end-date duration rule.
    /// </summary>
    /// <returns>The authored builder.</returns>
    private static NotableDateDocumentBuilder AuthoredDocument() =>
        NotableDateDocumentBuilder.Create("test.builder")
            .AddNotableDate("last-friday", "Last Friday", NotableDateCategory.Other, c => c
                .AddRule("r", r => r.MonthlyWeekday(DayOfWeek.Friday, WeekOrdinal.Last)))
            .AddNotableDate("shutdown", "Shutdown", NotableDateCategory.Other, c => c
                .AsNonWorkingByDefault()
                .AddRule("r", r => r
                    .WeekdayNearDate(12, 26, DayOfWeek.Friday, WeekdayProximity.Before)
                    .UntilDate(
                        end => end.WeekdayNearDate(1, 1, DayOfWeek.Monday, WeekdayProximity.After),
                        startBoundary: DateBoundary.Exclusive,
                        endBoundary: DateBoundary.Exclusive)));

    /// <summary>
    /// Asserts the recurrence and shutdown occurrences resolve as expected for the supplied resource.
    /// </summary>
    /// <param name="resource">The resource to resolve.</param>
    private static void AssertResolves(NotableDateResource resource)
    {
        NotableDateService service = new(resource);

        List<DateOnly> lastFridays = service.Resolve(new DateRange(new DateOnly(2026, 1, 1), new DateOnly(2026, 3, 31)), "XX")
            .Where(r => r.NotableDateId == "last-friday")
            .Select(r => r.Date)
            .OrderBy(d => d)
            .ToList();
        CollectionAssert.AreEqual(
            new[] { new DateOnly(2026, 1, 30), new DateOnly(2026, 2, 27), new DateOnly(2026, 3, 27) },
            lastFridays);

        NotableDate shutdown = service.Resolve(new DateRange(new DateOnly(2024, 12, 1), new DateOnly(2024, 12, 31)), "XX")
            .Single(r => r.NotableDateId == "shutdown");
        Assert.AreEqual((new DateOnly(2024, 12, 21), 16, new DateOnly(2025, 1, 5)), (shutdown.Date, shutdown.DurationDays, shutdown.EndDate));
    }

    /// <summary>
    /// Verifies that a directly built document resolves the recurrence and calculated-duration rules.
    /// </summary>
    [TestMethod]
    public void Build_WhenRecurrenceAndCalculatedDuration_ShouldResolve()
    {
        AssertResolves(AuthoredDocument().Build());
    }

    /// <summary>
    /// Verifies that an XML round-trip preserves the recurrence and calculated-duration rules.
    /// </summary>
    [TestMethod]
    public void ToXml_WhenRoundTripped_ShouldPreserveOccurrenceSources()
    {
        string xml = AuthoredDocument().ToXml();

        AssertResolves(NotableDateDocumentBuilder.FromXml(xml).Build());
    }

    /// <summary>
    /// Verifies that a JSON round-trip preserves the recurrence and calculated-duration rules.
    /// </summary>
    [TestMethod]
    public void ToJson_WhenRoundTripped_ShouldPreserveOccurrenceSources()
    {
        string json = AuthoredDocument().ToJson();

        AssertResolves(NotableDateDocumentBuilder.FromJson(json).Build());
    }

    /// <summary>
    /// Verifies that configuring a fixed duration after a calculated duration throws.
    /// </summary>
    [TestMethod]
    public void WithDurationDays_WhenCalculatedDurationAlreadySet_ShouldThrow()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() =>
            NotableDateDocumentBuilder.Create("test")
                .AddNotableDate("n", "N", NotableDateCategory.Other, c => c
                    .AddRule("r", r => r.Fixed(1, 1).UntilDate(end => end.Fixed(1, 10)).WithDurationDays(5))));
    }
}
