// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CollisionResolutionTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the same-day collision policy: two concepts emitted on the same date are kept, reduced to the
/// highest-priority occurrence, reduced by category then priority, or resolved by a custom resolver.
/// </summary>
[TestClass]
public sealed class CollisionResolutionTests
{
    /// <summary>
    /// Builds a resource document whose two 1 January concepts collide, parameterized by collision policy.
    /// </summary>
    /// <param name="policy">The same-day collision policy token.</param>
    /// <returns>The resource XML.</returns>
    private static string Document(string policy) => $"""
    <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="data.collide">
      <ResolutionPolicy duplicatePolicy="Error" sameDayCollisionPolicy="{policy}" priorityDirection="HigherWins" />
      <NotableDates>
        <NotableDate id="big-holiday" displayName="Big Holiday" category="PublicHoliday" defaultNonWorkingDay="true">
          <Rules><Rule id="x" priority="200"><Strategy><Fixed month="January" day="1" /></Strategy></Rule></Rules>
        </NotableDate>
        <NotableDate id="minor-observance" displayName="Minor Observance" category="Observance" defaultNonWorkingDay="false">
          <Rules><Rule id="x" priority="50"><Strategy><Fixed month="January" day="1" /></Strategy></Rule></Rules>
        </NotableDate>
        <NotableDate id="other-day" displayName="Other Day" category="Observance" defaultNonWorkingDay="false">
          <Rules><Rule id="x"><Strategy><Fixed month="March" day="3" /></Strategy></Rule></Rules>
        </NotableDate>
      </NotableDates>
    </NotableDateResource>
    """;

    /// <summary>
    /// Resolves 1 January 2025 for the supplied policy, returning the surviving identifiers.
    /// </summary>
    /// <param name="policy">The collision policy token.</param>
    /// <param name="resolver">An optional custom collision resolver.</param>
    /// <returns>The surviving notable-date identifiers.</returns>
    private static List<string> ResolveNewYear(string policy, INotableDateCollisionResolver? resolver = null)
    {
        NotableDateService service = new(NotableDateResourceLoader.Load(Document(policy)), new NotableDateServiceOptions { CollisionResolver = resolver });
        return service.Resolve(new DateOnly(2025, 1, 1), "XX")
            .Select(r => r.NotableDateId).OrderBy(s => s, StringComparer.Ordinal).ToList();
    }

    /// <summary>
    /// Verifies that the default keep-all policy returns both colliding occurrences.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenKeepAll_ReturnsBoth()
    {
        CollectionAssert.AreEqual(new[] { "big-holiday", "minor-observance" }, ResolveNewYear("KeepAll"));
    }

    /// <summary>
    /// Verifies that the highest-priority policy keeps only the higher-priority occurrence.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenHighestPriorityOnly_KeepsTheHigherPriority()
    {
        CollectionAssert.AreEqual(new[] { "big-holiday" }, ResolveNewYear("HighestPriorityOnly"));
    }

    /// <summary>
    /// Verifies that the category-priority policy keeps the higher-ranked category.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenCategoryPriority_KeepsTheHigherCategory()
    {
        CollectionAssert.AreEqual(new[] { "big-holiday" }, ResolveNewYear("CategoryPriority"));
    }

    /// <summary>
    /// Verifies that the custom policy defers to the supplied resolver.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenCustom_DefersToResolver()
    {
        INotableDateCollisionResolver resolver = new KeepObservanceResolver();
        CollectionAssert.AreEqual(new[] { "minor-observance" }, ResolveNewYear("Custom", resolver));
    }

    /// <summary>
    /// Verifies that a non-colliding date is unaffected by a reducing policy.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenSingleOccurrence_IsUnaffectedByPolicy()
    {
        NotableDateService service = new(NotableDateResourceLoader.Load(Document("HighestPriorityOnly")));
        IReadOnlyList<NotableDate> march = service.Resolve(new DateOnly(2025, 3, 3), "XX");

        CollectionAssert.AreEqual(new[] { "other-day" }, march.Select(r => r.NotableDateId).ToList());
    }

    /// <summary>
    /// Verifies that a window cutting a multi-day occurrence that loses the same-day collision on its emitted date,
    /// outside the window, drops the occurrence as the whole year's resolution does, rather than keeping it because the
    /// winner lies outside the window.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenWindowCutsOccurrenceThatLosesItsCollision_ShouldDropItAsTheWholeYearDoes()
    {
        NotableDateService service = new(NotableDateResourceLoader.Load(SpanCollisionDocument("HighestPriorityOnly")));

        IReadOnlyList<NotableDate> window = service.Resolve(new DateRange(new DateOnly(2026, 12, 29), new DateOnly(2026, 12, 30)), "XX");

        CollectionAssert.AreEqual(Array.Empty<string>(), Describe(window));
    }

    /// <summary>
    /// Verifies that, for every same-day collision policy, the result for each window of a sweep across two civil years
    /// equals a wide resolution restricted to the window, including windows that cut multi-day occurrences whose
    /// collision is settled on a day outside the window and occurrences that run into the next civil year.
    /// </summary>
    /// <param name="policy">The same-day collision policy token.</param>
    [TestMethod]
    [DataRow("KeepAll")]
    [DataRow("HighestPriorityOnly")]
    [DataRow("CategoryPriority")]
    [DataRow("Custom")]
    public void Resolve_WhenWindowSwept_ShouldEqualWideResolutionRestrictedToWindow(string policy)
    {
        NotableDateService service = new(
            NotableDateResourceLoader.Load(SpanCollisionDocument(policy)),
            new NotableDateServiceOptions { CollisionResolver = new KeepGreatestIdResolver() });
        IReadOnlyList<NotableDate> wide = service.Resolve(new DateRange(new DateOnly(2025, 1, 1), new DateOnly(2027, 12, 31)), "XX");
        List<string> mismatches = new();

        for (DateOnly start = new(2025, 12, 20); start <= new DateOnly(2027, 1, 10); start = start.AddDays(1))
        {
            foreach (int length in new[] { 1, 3, 12 })
            {
                var range = new DateRange(start, start.AddDays(length - 1));
                string[] expected = Describe(wide.Where(o => o.Date <= range.EndDate && o.EndDate >= range.StartDate).ToList());
                string[] actual = Describe(service.Resolve(range, "XX"));

                if (!expected.SequenceEqual(actual, StringComparer.Ordinal))
                    mismatches.Add(string.Create(CultureInfo.InvariantCulture, $"{range.StartDate:yyyy-MM-dd}..{range.EndDate:yyyy-MM-dd}"));
            }
        }

        Assert.AreEqual(0, mismatches.Count, string.Join(", ", mismatches.Take(10)));
    }

    /// <summary>
    /// Verifies that a provider occurrence emitted before the window, on the same day as a multi-day occurrence that
    /// reaches into the window, takes part in that day's collision, including one the window's own range would not ask
    /// the provider for because a longer provider occurrence widens the day's reach.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenProviderOccurrenceSharesDayBeforeWindow_ShouldSettleItAsTheWholeYearDoes()
    {
        NotableDateService service = new(
            NotableDateResourceLoader.Load(SpanCollisionDocument("HighestPriorityOnly")),
            new NotableDateServiceOptions { Providers = [new RangeBoundProvider()] });
        IReadOnlyList<NotableDate> year = service.Resolve(new DateRange(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)), "XX");
        var range = new DateRange(new DateOnly(2026, 11, 15), new DateOnly(2026, 11, 16));

        IReadOnlyList<NotableDate> window = service.Resolve(range, "XX");

        CollectionAssert.AreEqual(
            Describe(year.Where(o => o.Date <= range.EndDate && o.EndDate >= range.StartDate).ToList()),
            Describe(window));
    }

    /// <summary>
    /// Builds a resource whose multi-day occurrences share their first day with single-day occurrences, parameterized by
    /// collision policy: a ten-day break from 28 December that runs into the next year and a five-day fair from 1 June,
    /// each losing its day to a higher-priority, higher-category single-day holiday, a seven-day week from 30 March that
    /// wins its day, and a lone four-day occurrence from 10 October.
    /// </summary>
    /// <param name="policy">The same-day collision policy token.</param>
    /// <returns>The resource XML.</returns>
    private static string SpanCollisionDocument(string policy) => $"""
    <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="data.span-collide">
      <ResolutionPolicy sameDayCollisionPolicy="{policy}" priorityDirection="HigherWins" />
      <NotableDates>
        <NotableDate id="long-break" displayName="Long Break" category="Cultural">
          <Rules><Rule id="x" priority="1" durationDays="10"><Strategy><Fixed month="December" day="28" /></Strategy></Rule></Rules>
        </NotableDate>
        <NotableDate id="short-day" displayName="Short Day" category="PublicHoliday">
          <Rules><Rule id="x" priority="5"><Strategy><Fixed month="December" day="28" /></Strategy></Rule></Rules>
        </NotableDate>
        <NotableDate id="summer-fair" displayName="Summer Fair" category="Cultural">
          <Rules><Rule id="x" priority="1" durationDays="5"><Strategy><Fixed month="June" day="1" /></Strategy></Rule></Rules>
        </NotableDate>
        <NotableDate id="summer-holiday" displayName="Summer Holiday" category="PublicHoliday">
          <Rules><Rule id="x" priority="5"><Strategy><Fixed month="June" day="1" /></Strategy></Rule></Rules>
        </NotableDate>
        <NotableDate id="spring-week" displayName="Spring Week" category="Observance">
          <Rules><Rule id="x" priority="9" durationDays="7"><Strategy><Fixed month="March" day="30" /></Strategy></Rule></Rules>
        </NotableDate>
        <NotableDate id="spring-day" displayName="Spring Day" category="Observance">
          <Rules><Rule id="x" priority="0"><Strategy><Fixed month="March" day="30" /></Strategy></Rule></Rules>
        </NotableDate>
        <NotableDate id="autumn-days" displayName="Autumn Days" category="Cultural">
          <Rules><Rule id="x" durationDays="4"><Strategy><Fixed month="October" day="10" /></Strategy></Rule></Rules>
        </NotableDate>
      </NotableDates>
    </NotableDateResource>
    """;

    /// <summary>
    /// Describes each occurrence by its emitted date, identity, and span, so two resolutions compare by value.
    /// </summary>
    /// <param name="occurrences">The occurrences to describe.</param>
    /// <returns>One description per occurrence, in order.</returns>
    private static string[] Describe(IReadOnlyList<NotableDate> occurrences) =>
        [.. occurrences.Select(o => string.Create(CultureInfo.InvariantCulture, $"{o.Date:yyyy-MM-dd} {o.NotableDateId}/{o.RuleId} x{o.DurationDays}"))];

    /// <summary>
    /// A custom resolver that keeps the occurrence with the ordinally greatest notable-date identifier.
    /// </summary>
    private sealed class KeepGreatestIdResolver
        : INotableDateCollisionResolver
    {
        /// <inheritdoc />
        public IReadOnlyList<NotableDate> Resolve(DateOnly date, IReadOnlyList<NotableDate> colliding) =>
            [colliding.OrderByDescending(n => n.NotableDateId, StringComparer.Ordinal).First()];
    }

    /// <summary>
    /// A provider that returns only the occurrences intersecting the range it is asked for: a twenty-day, priority-zero
    /// occurrence from 1 November and a one-day, priority-nine occurrence on the same day.
    /// </summary>
    private sealed class RangeBoundProvider
        : INotableDateProvider
    {
        /// <inheritdoc />
        public IEnumerable<NotableDate> GetNotableDates(DateRange range, string territory)
        {
            for (int year = range.StartDate.Year; year <= range.EndDate.Year; year++)
            {
                foreach ((string id, int priority, int days) in new[] { ("provider-month", 0, 20), ("provider-day", 9, 1) })
                {
                    var occurrence = new NotableDate(
                        new DateOnly(year, 11, 1),
                        new DateOnly(year, 11, 1),
                        false,
                        new NotableDateRuleIdentity("provider", id, "x"),
                        id,
                        territory,
                        NotableDateCategory.Observance,
                        priority,
                        days,
                        false,
                        Array.Empty<string>(),
                        null,
                        null);

                    if (occurrence.Date <= range.EndDate && occurrence.EndDate >= range.StartDate)
                        yield return occurrence;
                }
            }
        }
    }

    /// <summary>
    /// A custom resolver that keeps only the observance occurrence.
    /// </summary>
    private sealed class KeepObservanceResolver
        : INotableDateCollisionResolver
    {
        /// <inheritdoc />
        public IReadOnlyList<NotableDate> Resolve(DateOnly date, IReadOnlyList<NotableDate> colliding) =>
            colliding.Where(n => n.Category == NotableDateCategory.Observance).ToList();
    }
}
